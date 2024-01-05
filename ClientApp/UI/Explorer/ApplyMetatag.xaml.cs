using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Thetacat.Metatags;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.Standards;

namespace Thetacat.UI.Explorer;

public delegate void ApplyMetatagsDelegate(Dictionary<string, bool?> checkedUncheckedAndIndeterminate, int vectorClock);

/// <summary>
/// Interaction logic for ApplyMetatag.xaml
/// </summary>
public partial class ApplyMetatag : Window
{
    private ApplyMetatagModel model = new();
    private readonly ApplyMetatagsDelegate m_applyDelegate;

    public ApplyMetatag(ApplyMetatagsDelegate applyDelegate)
    {
        m_applyDelegate = applyDelegate;
        InitializeComponent();
        DataContext = model;
        MainWindow._AppState.RegisterWindowPlace(this, "ApplyMetatagWindow");
    }

    private void Set(MetatagSchema schema, List<Metatag> tagsSet, List<Metatag> tagsIndeterminate)
    {
        model.RootAvailable = new MetatagTree(schema.MetatagsWorking, null, null);
        model.RootApplied = new MetatagTree(schema.MetatagsWorking, null, tagsSet.Union(tagsIndeterminate));

        Dictionary<string, bool?> initialState = GetCheckedAndSetFromSetsAndIndeterminates(tagsSet, tagsIndeterminate);

        Metatags.Initialize(model.RootAvailable.Children, 0, MetatagStandards.Standard.User, initialState);
        MetatagsApplied.Initialize(model.RootApplied.Children, 0, MetatagStandards.Standard.User, initialState);
    }

    public static Dictionary<string, bool?> GetCheckedAndSetFromSetsAndIndeterminates(List<Metatag> tagsSet, List<Metatag> tagsIndeterminate)
    {
        Dictionary<string, bool?> checkedAndIndeterminate = new();
        foreach (Metatag tag in tagsSet)
        {
            checkedAndIndeterminate.Add(tag.ID.ToString(), true);
        }

        foreach (Metatag tag in tagsIndeterminate)
        {
            checkedAndIndeterminate.Add(tag.ID.ToString(), null);
        }

        return checkedAndIndeterminate;
    }

    public static Dictionary<string, bool?> GetCheckedAndIndetermineFromMediaSet(List<MediaItem> mediaItems)
    {
        List<Metatag> tagsIndeterminate = new();
        List<Metatag> tagsSet = new();

        FillSetsAndIndeterminatesFromMediaItems(mediaItems, tagsSet, tagsIndeterminate);
        return GetCheckedAndSetFromSetsAndIndeterminates(tagsSet, tagsIndeterminate);
    }

    public static void FillSetsAndIndeterminatesFromMediaItems(List<MediaItem> mediaItems, List<Metatag> tagsSet, List<Metatag> tagsIndeterminate)
    {
        // keep a running count of the number of times a tag was seen. we either see it
        // never, or the same as the number of media items. anything different and its
        // not consistently applied (hence indeterminate)
        Dictionary<Metatag, int> tagsCounts = new Dictionary<Metatag, int>();

        foreach (MediaItem mediaItem in mediaItems)
        {
            foreach (KeyValuePair<Guid, MediaTag> tag in mediaItem.Tags)
            {
                if (!tagsCounts.TryGetValue(tag.Value.Metatag, out int count))
                {
                    count = 0;
                    tagsCounts.Add(tag.Value.Metatag, count);
                }

                tagsCounts[tag.Value.Metatag] = count + 1;
            }
        }

        foreach (KeyValuePair<Metatag, int> tagCount in tagsCounts)
        {
            if (tagCount.Value == mediaItems.Count)
                tagsSet.Add(tagCount.Key);
            else if (tagCount.Value != 0)
                tagsIndeterminate.Add(tagCount.Key);
        }
    }

    public void UpdateForMedia(List<MediaItem> mediaItems, MetatagSchema schema, int vectorClock)
    {
        List<Metatag> tagsIndeterminate = new();
        List<Metatag> tagsSet = new();

        FillSetsAndIndeterminatesFromMediaItems(mediaItems, tagsSet, tagsIndeterminate);

        Set(schema, tagsSet, tagsIndeterminate);
        model.SelectedItemsVectorClock = vectorClock;
    }

    private void DoApply(object sender, RoutedEventArgs e)
    {
        // sync the checked state between the tree control and the media items
        Dictionary<string, bool?> checkedUncheckedAndIndeterminateItems = Metatags.GetCheckedUncheckedAndIndeterminateItems();

        m_applyDelegate(checkedUncheckedAndIndeterminateItems, model.SelectedItemsVectorClock);
    }

    private void DoRemove(object sender, RoutedEventArgs e)
    {
        // sync the checked state between the tree control and the media items
        Dictionary<string, bool?> checkedUncheckedAndIndeterminateItems = Metatags.GetCheckedUncheckedAndIndeterminateItems();

        m_applyDelegate(checkedUncheckedAndIndeterminateItems, model.SelectedItemsVectorClock);
    }
}