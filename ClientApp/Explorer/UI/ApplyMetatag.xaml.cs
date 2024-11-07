using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Thetacat.Explorer.UI;
using Thetacat.Filtering;
using Thetacat.Import.UI;
using Thetacat.Import.UI.Commands;
using Thetacat.Logging;
using Thetacat.Metatags;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.UI.Controls;
using Thetacat.UI.Input;
using Thetacat.Util;

namespace Thetacat.Explorer;

public delegate void ApplyMetatagsDelegate(Dictionary<string, bool?> checkedUncheckedAndIndeterminate, Dictionary<string, string?> values, int vectorClock);

/// <summary>
/// Interaction logic for ApplyMetatag.xaml
/// </summary>
public partial class ApplyMetatag : Window
{
    readonly ApplyMetatagModel m_model = new();
    private ApplyMetatagsDelegate m_applyDelegate;

    public ApplyMetatagModel Model => m_model;

    public ApplyMetatag(ApplyMetatagsDelegate applyDelegate)
    {
        m_applyDelegate = applyDelegate;

        m_model.SetMediaTagValueCommand = new SetMediaTagValueCommand(_SetMediaTagValueCommand);
        DataContext = m_model;
        InitializeComponent();
        App.State.RegisterWindowPlace(this, "ApplyMetatagWindow");
    }

    // We take a mediatag for the set tags because we (might) allow values.  If all the values
    // aren't the same, then it will be in the indeterminate set
    private void Set(MetatagSchema schema, List<MediaTag> tagsSet, List<Metatag> tagsIndeterminate)
    {
        List<Metatag> metatagsSet = new List<Metatag>();

        foreach (MediaTag tag in tagsSet)
        {
            metatagsSet.Add(tag.Metatag);
        }

        MicroTimer timer = new MicroTimer();
        timer.Start();

        m_model.RootAvailable = new MetatagTree(schema.MetatagsWorking, null, null);
        m_model.RootApplied = new MetatagTree(schema.MetatagsWorking, null, metatagsSet.Union(tagsIndeterminate));

        Dictionary<string, bool?> initialState = new();
        Dictionary<string, string?> initialValues = new();

        MetatagTreeView.GetCheckedIndeterminateAndValuesFromSetsAndIndeterminates(tagsSet, tagsIndeterminate, initialState, initialValues);

        Metatags.Initialize(m_model.RootAvailable.Children, 0, MetatagStandards.Standard.User, initialState, initialValues);
        Metatags.AddSpecificTag(m_model.RootAvailable.Children, BuiltinTags.s_DontPushToCloud, initialState, initialValues);
        Metatags.AddSpecificTag(m_model.RootAvailable.Children, BuiltinTags.s_IsTrashItem, initialState, initialValues);
        Metatags.AddSpecificTag(m_model.RootAvailable.Children, BuiltinTags.s_DateSpecified, initialState, initialValues);
        Metatags.AddSpecificTag(m_model.RootAvailable.Children, BuiltinTags.s_OriginalMediaDate, initialState, initialValues);
        MetatagsApplied.Initialize(m_model.RootApplied.Children, 0, MetatagStandards.Standard.User, initialState);
        MetatagsApplied.AddSpecificTag(m_model.RootApplied.Children, BuiltinTags.s_DontPushToCloud, initialState);
        MetatagsApplied.AddSpecificTag(m_model.RootApplied.Children, BuiltinTags.s_IsTrashItem, initialState);
        MetatagsApplied.AddSpecificTag(m_model.RootApplied.Children, BuiltinTags.s_DateSpecified, initialState);
        App.LogForApp(EventType.Verbose, $"ApplyMetatag:Set elapsed {timer.Elapsed()}");
    }

    void _SetMediaTagValueCommand(IMetatagTreeItem? context)
    {
        if (context is IMetatagTreeItem nameItem)
        {
            if (InputFormats.FPrompt("Enter a value for the metatag (fill in only one)", nameItem.Value ?? "", out string? result, this))
            {
                context.Value = result;
                context.Checked = true;
            }
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
        List<MediaTag> tagsSet = new();

        HashSet<string> expandedApply = MetatagTreeView.GetExpandedTreeItems(Metatags);
        HashSet<string> expandedApplied = MetatagTreeView.GetExpandedTreeItems(MetatagsApplied);

        MetatagTreeView.FillSetsAndIndeterminatesFromMediaItems(mediaItems, tagsSet, tagsIndeterminate);

        Set(schema, tagsSet, tagsIndeterminate);
        if (vectorClock != null)
            m_model.SelectedItemsVectorClock = vectorClock.Value;

        if (expandedApply.Count > 0)
            MetatagTreeView.RestoreExpandedTreeItems(Metatags, expandedApply);

        if (expandedApplied.Count > 0)
            MetatagTreeView.RestoreExpandedTreeItems(MetatagsApplied, expandedApplied);

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

        This takes the new set of checkedUncheckedAndIndeterminate states as well
        as the values for checked metatags and applies those changes to the model
    ----------------------------------------------------------------------------*/
    public void UpdateMediaForMetatagChanges(
        Dictionary<string, bool?> checkedUncheckedAndIndeterminate,
        Dictionary<string, string?> values,
        IReadOnlyCollection<MediaItem> mediaItems,
        MetatagSchema schema)
    {
        Dictionary<string, bool?> originalState = new Dictionary<string, bool?>();
        Dictionary<string, string?> originalValues = new Dictionary<string, string?>();

        MetatagTreeView.GetCheckedAndIndetermineFromMediaSet(mediaItems, originalState, originalValues);

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

        HashSet<string> metatagValuesToChange = new HashSet<string>();

        // find all the values that have to change
        foreach (string metatagID in values.Keys)
        {
            if (originalValues.TryGetValue(metatagID, out string? originalValue))
            {
                if (originalValue == values[metatagID])
                    continue;
            }

            // values need to be updated or added
            metatagValuesToChange.Add(metatagID);
        }

        int mruClock = App.State.MetatagMRU.VectorClock;

        // find all the tags to add
        foreach (KeyValuePair<string, bool?> item in checkedUncheckedAndIndeterminate)
        {
            if (item.Value is true)
            {
                if (!originalState.TryGetValue(item.Key, out bool? checkedState)
                    || checkedState == null
                    || checkedState == false
                    || metatagValuesToChange.Contains(item.Key))
                {
                    // it was originally unset(false), was indeterminate, was false, or the value changes
                    MediaTag mediaTag = MediaTag.CreateMediaTag(schema, Guid.Parse(item.Key), values[item.Key]);
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
        Dictionary<string, bool?> checkedUncheckedAndIndeterminateItems = new Dictionary<string, bool?>();
        Dictionary<string, string?> values = new Dictionary<string, string?>();
        // sync the checked state between the tree control and the media items
        Metatags.GetCheckedUncheckedAndIndeterminateItems(checkedUncheckedAndIndeterminateItems, values);

        m_applyDelegate(checkedUncheckedAndIndeterminateItems, values, m_model.SelectedItemsVectorClock);
    }

    private void DoRemove(object sender, RoutedEventArgs e)
    {
        Dictionary<string, bool?> checkedUncheckedAndIndeterminateItems = new Dictionary<string, bool?>();
        Dictionary<string, string?> values = new Dictionary<string, string?>();

        // sync the checked state between the tree control and the media items
        Metatags.GetCheckedUncheckedAndIndeterminateItems(checkedUncheckedAndIndeterminateItems, values);

        m_applyDelegate(checkedUncheckedAndIndeterminateItems, values, m_model.SelectedItemsVectorClock);
    }

    private void DoManageMetatags(object sender, RoutedEventArgs e)
    {
        Metatags.ManageMetadata manage = new();
        manage.Owner = this;
        manage.ShowDialog();

        // and update the metatag panel
        MetatagSchema schema = App.State.MetatagSchema;

        Dictionary<string, bool?> checkedUncheckedAndIndeterminateItems = new Dictionary<string, bool?>();
        Dictionary<string, string?> values = new Dictionary<string, string?>();

        Metatags.GetCheckedUncheckedAndIndeterminateItems(checkedUncheckedAndIndeterminateItems, values);

        List<MediaTag> tagsSet = new();
        List<Metatag> tagsIndeterminate = new();

        foreach (KeyValuePair<string, bool?> state in checkedUncheckedAndIndeterminateItems)
        {
            if (state.Value == null)
                tagsIndeterminate.Add(schema.GetMetatagFromId(new Guid(state.Key))!);
            if (state.Value != null && state.Value.Value)
                tagsSet.Add(new MediaTag(schema.GetMetatagFromId(new Guid(state.Key))!, values[state.Key]));
        }

        // get the set of set tags
        Set(App.State.MetatagSchema, tagsSet, tagsIndeterminate);
    }
}
