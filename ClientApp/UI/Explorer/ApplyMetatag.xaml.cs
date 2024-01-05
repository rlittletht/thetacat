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

namespace Thetacat.UI.Explorer
{
    /// <summary>
    /// Interaction logic for ApplyMetatag.xaml
    /// </summary>
    public partial class ApplyMetatag : Window
    {
        private ApplyMetatagModel model = new();

        private void Set(MetatagSchema schema, List<Metatag> tagsSet, List<Metatag> tagsIndeterminate)
        {
            model.RootAvailable = new MetatagTree(schema.MetatagsWorking, null, null);
            model.RootApplied = new MetatagTree(schema.MetatagsWorking, null, tagsSet.Union(tagsIndeterminate));

            Dictionary<string, bool?> initialState = new();
            foreach (Metatag tag in tagsSet)
            {
                initialState.Add(tag.ID.ToString(), true);
            }

            foreach (Metatag tag in tagsIndeterminate)
            {
                initialState.Add(tag.ID.ToString(), null);
            }

            Metatags.Initialize(model.RootAvailable.Children, 0, MetatagStandards.Standard.User, initialState);
            MetatagsApplied.Initialize(model.RootApplied.Children, 0, MetatagStandards.Standard.User, initialState);
        }

        public void UpdateForMedia(List<MediaItem> mediaItems, MetatagSchema schema)
        {
            // keep a running count of the number of times a tag was seen. we either see it
            // never, or the same as the number of media items. anything different and its
            // not consistently applied (hence indeterminate)
            Dictionary<Metatag, int> tagsCounts = new Dictionary<Metatag, int>();
            List<Metatag> tagsIndeterminate = new();
            List<Metatag> tagsSet = new();

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

            Set(schema, tagsSet, tagsIndeterminate);
        }

        public ApplyMetatag()
        {
            InitializeComponent();
            DataContext = model;
            MainWindow._AppState.RegisterWindowPlace(this, "ApplyMetatagWindow");
        }

        private void DoApply(object sender, RoutedEventArgs e)
        {

        }

        private void DoRemove(object sender, RoutedEventArgs e)
        {

        }


    }
}
