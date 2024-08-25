using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Thetacat.Explorer.UI;
using Thetacat.Logging;
using Thetacat.Metatags;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Standards;
using Thetacat.UI.Controls;
using Thetacat.Util;

namespace Thetacat.Explorer;

public delegate void ApplyMetatagsDelegate(Dictionary<string, bool?> checkedUncheckedAndIndeterminate, int vectorClock);

/// <summary>
/// Interaction logic for ApplyMetatag.xaml
/// </summary>
public partial class ApplyMetatag : Window
{
    private ApplyMetatagModel model = new();
    private ApplyMetatagsDelegate m_applyDelegate;

    public ApplyMetatag(ApplyMetatagsDelegate applyDelegate)
    {
        m_applyDelegate = applyDelegate;
        InitializeComponent();
        DataContext = model;
        App.State.RegisterWindowPlace(this, "ApplyMetatagWindow");
    }

    private void Set(MetatagSchema schema, List<Metatag> tagsSet, List<Metatag> tagsIndeterminate)
    {
        MicroTimer timer = new MicroTimer();
        timer.Start();

        model.RootAvailable = new MetatagTree(schema.MetatagsWorking, null, null);
        model.RootApplied = new MetatagTree(schema.MetatagsWorking, null, tagsSet.Union(tagsIndeterminate));

        Dictionary<string, bool?> initialState = GetCheckedAndSetFromSetsAndIndeterminates(tagsSet, tagsIndeterminate);

        Metatags.Initialize(model.RootAvailable.Children, 0, MetatagStandards.Standard.User, initialState);
        Metatags.AddSpecificTag(model.RootAvailable.Children, BuiltinTags.s_DontPushToCloud, initialState);
        Metatags.AddSpecificTag(model.RootAvailable.Children, BuiltinTags.s_IsTrashItem, initialState);
        MetatagsApplied.Initialize(model.RootApplied.Children, 0, MetatagStandards.Standard.User, initialState);
        MetatagsApplied.AddSpecificTag(model.RootApplied.Children, BuiltinTags.s_DontPushToCloud, initialState);
        MetatagsApplied.AddSpecificTag(model.RootApplied.Children, BuiltinTags.s_IsTrashItem, initialState);
        App.LogForApp(EventType.Verbose, $"ApplyMetatag:Set elapsed {timer.Elapsed()}");
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

    public HashSet<string> GetExpandedTreeItems(MetatagTreeView metatagTree)
    {
        HashSet<string> expandedTreeItems = new HashSet<string>();

        foreach (object? item in metatagTree.Tree.Items)
        {
            if (item is IMetatagTreeItem metatagTreeItem)
            {
                metatagTreeItem.Preorder(
                    null,
                    (child, parent, depth) =>
                    {
                        if (metatagTree.Tree.ItemContainerGenerator.ContainerFromItem(child) is TreeViewItem { IsExpanded: true })
                            expandedTreeItems.Add(child.ID);
                    },
                    0);
            }
        }

        return expandedTreeItems;
    }

    public void RestoreExpandedTreeItems(MetatagTreeView metatagTree, HashSet<string> expandedTreeItems)
    {
        foreach (object? item in metatagTree.Tree.Items)
        {
            if (item is IMetatagTreeItem metatagTreeItem)
            {
                metatagTreeItem.Preorder(
                    null,
                    (child, parent, depth) =>
                    {
                        if (expandedTreeItems.Contains(child.ID))
                        {
                            if (metatagTree.Tree.ItemContainerGenerator.ContainerFromItem(child) is TreeViewItem treeItem)
                                treeItem.IsExpanded = true;
                        }
                    },
                    0);
            }
        }
    }


    public static Dictionary<string, bool?> GetCheckedAndIndetermineFromMediaSet(IReadOnlyCollection<MediaItem> mediaItems)
    {
        List<Metatag> tagsIndeterminate = new();
        List<Metatag> tagsSet = new();

        FillSetsAndIndeterminatesFromMediaItems(mediaItems, tagsSet, tagsIndeterminate);
        return GetCheckedAndSetFromSetsAndIndeterminates(tagsSet, tagsIndeterminate);
    }

    public static void FillSetsAndIndeterminatesFromMediaItems(
        IReadOnlyCollection<MediaItem> mediaItems, List<Metatag> tagsSet, List<Metatag> tagsIndeterminate)
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

    /*----------------------------------------------------------------------------
        %%Function: InternalUpdateForMedia
        %%Qualified: Thetacat.Explorer.ApplyMetatag.InternalUpdateForMedia

        Used internally and allows for vectorClock and applyDelegate to not
        change (not something we want regular users to be able to do, but useful
        during the application process)
    ----------------------------------------------------------------------------*/
    private void InternalUpdateForMedia(
        IReadOnlyCollection<MediaItem> mediaItems,
        MetatagSchema schema,
        int? vectorClock,
        ApplyMetatagsDelegate? applyDelegate)
    {
        MicroTimer timer = new MicroTimer();
        timer.Start();

        List<Metatag> tagsIndeterminate = new();
        List<Metatag> tagsSet = new();

        HashSet<string> expandedApply = GetExpandedTreeItems(Metatags);
        HashSet<string> expandedApplied = GetExpandedTreeItems(MetatagsApplied);

        FillSetsAndIndeterminatesFromMediaItems(mediaItems, tagsSet, tagsIndeterminate);

        Set(schema, tagsSet, tagsIndeterminate);
        if (vectorClock != null)
            model.SelectedItemsVectorClock = vectorClock.Value;

        if (expandedApply.Count > 0)
            RestoreExpandedTreeItems(Metatags, expandedApply);

        if (expandedApplied.Count > 0)
            RestoreExpandedTreeItems(MetatagsApplied, expandedApplied);

        App.LogForApp(EventType.Verbose, $"UpdateMetatagPanelIfNecessary: {timer.Elapsed()}");
        if (applyDelegate != null)
            m_applyDelegate = applyDelegate;
    }


    /*----------------------------------------------------------------------------
        %%Function: UpdateForMedia
        %%Qualified: Thetacat.Explorer.ApplyMetatag.UpdateForMedia

        Update the panel for the set of mediaItems with the given schema.

        The VectorClock can be used to validate both the version of the data
        AND the set of media items. It will be used when we call the
        ApplyDelegate for validation.

        ApplyDelegate will update the ApplyDelegate for the panel
    ----------------------------------------------------------------------------*/
    public void UpdateForMedia(
        IReadOnlyCollection<MediaItem> mediaItems, 
        MetatagSchema schema, 
        int vectorClock, 
        ApplyMetatagsDelegate applyDelegate)
    {
        InternalUpdateForMedia(mediaItems, schema, vectorClock, applyDelegate);
    }

    public static void RemoveMediatagFromMedia(Guid mediaTagID, IEnumerable<MediaItem> selectedItems)
    {
        foreach (MediaItem item in selectedItems)
        {
            item.FRemoveMediaTag(mediaTagID);
        }
    }

    public static void SetMediatagForMedia(MediaTag mediaTag, IEnumerable<MediaItem> selectedItems)
    {
        foreach (MediaItem item in selectedItems)
        {
            item.FAddOrUpdateMediaTag(mediaTag, true);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateMediaForMetatagChanges
        %%Qualified: Thetacat.Explorer.ApplyMetatag.UpdateMediaForMetatagChanges

        The caller is responsible for validating the vector clock BEFORE calling
        us
    ----------------------------------------------------------------------------*/
    public void UpdateMediaForMetatagChanges(
        Dictionary<string, bool?> checkedUncheckedAndIndeterminate, 
        IReadOnlyCollection<MediaItem> mediaItems, 
        MetatagSchema schema)
    {
        Dictionary<string, bool?> originalState = ApplyMetatag.GetCheckedAndIndetermineFromMediaSet(mediaItems);

        // find all the tags to remove
        foreach (KeyValuePair<string, bool?> item in originalState)
        {
            // if its indeterminate, then there is no change
            if (!checkedUncheckedAndIndeterminate.TryGetValue(item.Key, out bool? checkedState)
                || checkedState == null)
            {
                continue;
            }

            // if it was true and now its false, remove it
            if (item.Value == true && checkedState == false)
            {
                RemoveMediatagFromMedia(Guid.Parse(item.Key), mediaItems);
            }

            if (item.Value == false)
                MessageBox.Show("Strange. We have a false in the checked/indeterminate");
        }

        int mruClock = App.State.MetatagMRU.VectorClock;

        // find all the tags to add
        foreach (KeyValuePair<string, bool?> item in checkedUncheckedAndIndeterminate)
        {
            if (item.Value is true)
            {
                if (!originalState.TryGetValue(item.Key, out bool? checkedState)
                    || checkedState == null
                    || checkedState == false)
                {
                    // it was originally unset(false), was indeterminate, or was false
                    MediaTag mediaTag = MediaTag.CreateMediaTag(schema, Guid.Parse(item.Key), null);
                    SetMediatagForMedia(mediaTag, mediaItems);

                    App.State.MetatagMRU.TouchMetatag(mediaTag.Metatag);
                }
            }
        }

        if (mruClock != App.State.MetatagMRU.VectorClock)
        {
            App.State.ActiveProfile.MetatagMru.Clear();
            foreach (Metatag tag in App.State.MetatagMRU.RecentTags)
            {
                App.State.ActiveProfile.MetatagMru.Add(tag.ID.ToString());
            }

            App.State.Settings.WriteSettings();
        }

        InternalUpdateForMedia(mediaItems, schema, null, null);
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

    private void DoManageMetatags(object sender, RoutedEventArgs e)
    {
        Metatags.ManageMetadata manage = new();
        manage.Owner = this;
        manage.ShowDialog();

        // and update the metatag panel
        MetatagSchema schema = App.State.MetatagSchema;

        Dictionary<string, bool?> checkedUncheckedAndIndeterminateItems = Metatags.GetCheckedUncheckedAndIndeterminateItems();

        List<Metatag> tagsSet = new();
        List<Metatag> tagsIndeterminate = new();

        foreach (KeyValuePair<string, bool?> state in checkedUncheckedAndIndeterminateItems)
        {
            if (state.Value == null)
                tagsIndeterminate.Add(schema.GetMetatagFromId(new Guid(state.Key))!);
            if (state.Value != null && state.Value.Value)
                tagsSet.Add(schema.GetMetatagFromId(new Guid(state.Key))!);
        }

        // get the set of set tags
        Set(App.State.MetatagSchema, tagsSet, tagsIndeterminate);
    }
}
