using MetadataExtractor.Formats.Xmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Thetacat.Explorer.UI;
using Thetacat.Filtering;
using Thetacat.Logging;
using Thetacat.Metatags;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.UI.Controls;
using Thetacat.Util;

namespace Thetacat.Explorer;


/// <summary>
/// Interaction logic for QuickFilterPanel.xaml
/// </summary>
public partial class QuickFilterPanel : Window
{
    private QuickFilterPanelModel model = new();

    public QuickFilterPanel()
    {
        InitializeComponent();
        DataContext = model;
        App.State.RegisterWindowPlace(this, "QuickFilterPanelWindow");
        RefreshTags();
    }

    private void RefreshTags()
    {
        // since there are no media items to populate from, the set and indeterminate values
        // will be empty.

        // FUTURE: We could populate these from the current filter (or try to)
        List<Metatag> tagsIndeterminate = new();
        List<Metatag> tagsSet = new();

        HashSet<string> expandedApply = MetatagTreeView.GetExpandedTreeItems(Metatags);

        Set(App.State.MetatagSchema, tagsSet, tagsIndeterminate);

        if (expandedApply.Count > 0)
            MetatagTreeView.RestoreExpandedTreeItems(Metatags, expandedApply);
    }

    private void Set(MetatagSchema schema, List<Metatag> tagsSet, List<Metatag> tagsIndeterminate)
    {
        MicroTimer timer = new MicroTimer();
        timer.Start();

        model.RootAvailable = new MetatagTree(schema.MetatagsWorking, null, null);
        model.RootApplied = new MetatagTree(schema.MetatagsWorking, null, tagsSet.Union(tagsIndeterminate));

        Dictionary<string, bool?> initialState = MetatagTreeView.GetCheckedAndSetFromSetsAndIndeterminates(tagsSet, tagsIndeterminate);

        Metatags.Initialize(model.RootAvailable.Children, 0, MetatagStandards.Standard.User, initialState);
        Metatags.AddSpecificTag(model.RootAvailable.Children, BuiltinTags.s_DontPushToCloud, initialState);
        Metatags.AddSpecificTag(model.RootAvailable.Children, BuiltinTags.s_IsTrashItem, initialState);
        App.LogForApp(EventType.Verbose, $"QuickFilterPanel:Set elapsed {timer.Elapsed()}");
    }


    private void DoQuickFilterToAll(object sender, RoutedEventArgs e)
    {
        // sync the checked state between the tree control and the media items
        Dictionary<string, bool?> checkedUncheckedAndIndeterminateItems = Metatags.GetCheckedUncheckedAndIndeterminateItems();

        Filter tempFilter = Filters.CreateFromSelectedMetatags(checkedUncheckedAndIndeterminateItems, false);
        App.State.ChooseFilterOrCurrent(tempFilter);
    }

    private void DoQuickFilterToAny(object sender, RoutedEventArgs e)
    {
        // sync the checked state between the tree control and the media items
        Dictionary<string, bool?> checkedUncheckedAndIndeterminateItems = Metatags.GetCheckedUncheckedAndIndeterminateItems();

        Filter tempFilter = Filters.CreateFromSelectedMetatags(checkedUncheckedAndIndeterminateItems, true);
        App.State.ChooseFilterOrCurrent(tempFilter);
    }
}
