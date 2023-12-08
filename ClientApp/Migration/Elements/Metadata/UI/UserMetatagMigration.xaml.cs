using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV.Features2D;
using Thetacat.Controls;
using Thetacat.Metatags;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Standards;
using Thetacat.Types;
using MetatagTree = Thetacat.Metatags.MetatagTree;

namespace Thetacat.Migration.Elements.Metadata.UI;

public partial class UserMetatagMigration : UserControl
{
    private GridViewColumnHeader? sortCol = null;
    private SortAdorner? sortAdorner;
    private MetatagMigrate? m_migrate = null;

    public delegate void SwitchToSummaryDelegate();

    IAppState? m_appState;
    SwitchToSummaryDelegate? m_switchToSummaryDelegate;

    /// <summary>
    /// Interaction logic for UserMetatagMigration.xaml
    /// </summary>
    public UserMetatagMigration()
    {
        InitializeComponent();
    }

    private void DoToggleSelected(object sender, RoutedEventArgs e)
    {
        ToggleItems(metaTagsListView.SelectedItems as IEnumerable<object?>);
    }

    private void SortType(object sender, RoutedEventArgs e)
    {
        Sort(metaTagsListView, sender as GridViewColumnHeader);
    }

    public void Initialize(IAppState appState, ElementsDb db, MetatagMigrate migrate, SwitchToSummaryDelegate switchDelegate)
    {
        m_appState = appState;
        m_migrate = migrate;
        m_switchToSummaryDelegate = switchDelegate;

        if (m_appState == null)
            throw new ArgumentNullException(nameof(appState));

        m_appState = appState;

        if (m_appState.MetatagSchema == null)
            m_appState.RefreshMetatagSchema();

        m_migrate.SetUserMetatags(db.ReadMetadataTags());
        MarkExistingMetatags();

        metaTagsListView.ItemsSource = m_migrate.UserMetatags;

        Debug.Assert(m_appState.MetatagSchema != null, "m_appState.MetatagSchema != null");
    }

    private void RemoveSelected(object sender, RoutedEventArgs e)
    {
        RemoveItems(metaTagsListView.SelectedItems as IEnumerable<object?>);
    }

    public void ToggleItems(IEnumerable<object?>? items)
    {
        if (items == null)
            return;

        foreach (PseMetatag? item in items)
        {
            if (item != null)
                item.IsSelected = !item.IsSelected;
        }
    }

    public void Sort(ListView listView, GridViewColumnHeader? column)
    {
        if (column == null)
            return;

        string sortBy = column.Tag?.ToString() ?? string.Empty;

        if (sortAdorner != null && sortCol != null)
        {
            AdornerLayer.GetAdornerLayer(sortCol)?.Remove(sortAdorner);
            listView.Items.SortDescriptions.Clear();
        }

        ListSortDirection newDir = ListSortDirection.Ascending;
        if (sortCol == column && sortAdorner?.Direction == newDir)
            newDir = ListSortDirection.Descending;

        sortCol = column;
        sortAdorner = new SortAdorner(sortCol, newDir);
        AdornerLayer.GetAdornerLayer(sortCol)?.Add(sortAdorner);
        listView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
    }

    public void RemoveItems(IEnumerable<object?>? items)
    {
        if (items == null|| m_migrate == null)
            return;

        List<object> removeList = new();
        foreach (PseMetatag? item in items)
        {
            if (item != null)
                removeList.Add(item);
        }

        foreach (PseMetatag tag in removeList)
        {
            m_migrate.UserMetatags.Remove(tag);
        }
    }

    private void DoMigrate(object sender, RoutedEventArgs e)
    {
        m_switchToSummaryDelegate?.Invoke();
    }

    delegate Guid UnmatchedDelegate(List<string> nameHistory, IMetatagTreeItem item, Guid? idParent);
    delegate void MatchedDelegate(IMetatagTreeItem item, IMetatagTreeItem matchedItem);

    static void MatchAndInsertChildrenIfNeeded(
        IMetatagTreeItem? liveParent,
        IMetatagTreeItem parent,
        Guid? idParent,
        List<string> nameHistory,
        UnmatchedDelegate? unmatchedDelegate,
        MatchedDelegate? matchedDelegate)
    {
        // we have to build the tags to sync from the parent to the leaf in order to make sure we
        // build the correct relationships (we may have duplicate names in the tree, but they might
        // be unique because of hierarchy.  "Dog" might be under Pets/Dog and also Toys/Dog. We need
        // two Dog nodes in this case.

        foreach (IMetatagTreeItem item in parent.Children)
        {
            // look for a matching root in the current schema
            IMetatagTreeItem? match = liveParent?.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(item.Name), 1/*levelsToRecurse*/);
            Guid? parentId;

            nameHistory.Add(item.Name);

            if (match == null)
            {
                Guid? newID = unmatchedDelegate?.Invoke(nameHistory, item, idParent);

                // if we didn't get a new ID inserted, then we can't recurse
                parentId = newID;
            }
            else
            {
                matchedDelegate?.Invoke(item, match);
                parentId = Guid.Parse(match.ID);
            }

            // if we don't have a parentId (because we didn't have a match and we didn't insert a new item),
            // then we can't recurse (the parent chain would be broken). just continue with the siblings...
            if (parentId != null)
                MatchAndInsertChildrenIfNeeded(match, item, parentId, nameHistory, unmatchedDelegate, matchedDelegate);
            nameHistory.RemoveAt(nameHistory.Count - 1);
        }
    }

    public void MarkExistingMetatags()
    {
        Debug.Assert(m_appState != null, nameof(m_appState) + " != null");
        Debug.Assert(m_appState.MetatagSchema != null, "m_appState.MetatagSchema != null");
        Debug.Assert(m_appState.MetatagSchema.WorkingTree != null, "m_appState.MetatagSchema.WorkingTree != null");
        Debug.Assert(m_migrate != null, nameof(m_migrate) + " != null");

        string userTagName = MetatagStandards.GetStandardsTagFromStandard(MetatagStandards.Standard.User);
        IMetatag? userRoot = m_appState.MetatagSchema.FindFirstMatchingItem(MetatagMatcher.CreateNameMatch(userTagName));

        // if there's no user root, then no tags are already in the cat
        if (userRoot == null)
            return;

        IMetatagTreeItem? userTreeItem = m_appState.MetatagSchema.WorkingTree.FindMatchingChild(MetatagTreeItemMatcher.CreateIdMatch(userRoot.ID.ToString()), -1);

        if (userTreeItem == null)
            throw new Exception("no user root found");

        MatchAndInsertChildrenIfNeeded(
            userTreeItem,
            m_migrate.PseTree,
            userRoot.ID,
            new List<string>(),
            null /*unmatchedDelegate*/,
            (item, match) =>
            {
                m_migrate.GetMetatagFromID(item.ID).CatID = Guid.Parse(match.ID);
            }
        );
    }

    /*----------------------------------------------------------------------------
        %%Function: BuildTagsToInsert
        %%Qualified: Thetacat.Migration.Elements.Metadata.UI.UserMetatagMigration.BuildTagsToInsert

        This takes the current tag tree for the live database. The tree starts
        with all the tags under the "user" root, so every top level item we want
        to add is actually parented to the 'user' standard root.
    ----------------------------------------------------------------------------*/
    public static List<MetatagPair> BuildTagsToInsert(Metatags.MetatagTree currentUserMetatagTree, PseMetatagTree treeToSync, IMetatag userRoot)
    {
        List<MetatagPair> tagsToInsert = new();
        IMetatagTreeItem? userTreeItem = currentUserMetatagTree.FindMatchingChild(MetatagTreeItemMatcher.CreateIdMatch(userRoot.ID.ToString()), -1);

        if (userTreeItem == null)
            throw new Exception("no user root found");

        MatchAndInsertChildrenIfNeeded(
            userTreeItem,
            treeToSync,
            userRoot.ID,
            new List<string>(),
            (nameHistory, item, idParent) =>
            {
                Model.Metatag newTag = new()
                                       {
                                           ID = Guid.NewGuid(),
                                           Description = string.Join(":", nameHistory.ToArray()),
                                           Name = item.Name,
                                           Parent = idParent
                                       };

                tagsToInsert.Add(new MetatagPair(newTag, item.ID));
                return newTag.ID;
            },
            null /*matchedDelegate*/
        );

        return tagsToInsert;
    }

    /*----------------------------------------------------------------------------
        %%Function: MigrateSelected
        %%Qualified: Thetacat.Migration.Elements.UserMetatagMigration.MigrateSelected

        Migrate the selected elements tags (and their parents)
    ----------------------------------------------------------------------------*/
    private void MigrateSelected(object sender, RoutedEventArgs e)
    {
        if (m_appState == null || m_migrate == null || m_appState.MetatagSchema == null)
            throw new Exception("appstate or migrate uninitialized");

        // build a list of selected items
        List<PseMetatag> metatags = new();

        foreach (PseMetatag? item in metaTagsListView.Items)
        {
            if (item?.IsSelected ?? false)
                metatags.Add(item);
        }

        // instead of building the ops manually and live updating
        // and requerying, we want to keep an older schema (original base)
        // and modify the livetree. then we can do a diff of new livetree
        // against the base to get the ops.

        // this way, when we bring up the schema diff summary tab,
        // it can do the diff AND build the list of adds from the schema
        // metadata tab to get the live list of schema diffenences.

        // actually, we should make the metadata tab ALSO update
        // the live schema tree -- that way the summary tab
        // is only a diff summary of what we will upload. Basically
        // its just a user control that takes two SchemaModels (base and new)
        // and build the diff ops and lists those in the control.

        Metatags.MetatagTree liveTree = m_appState.MetatagSchema.WorkingTree;

        // now figure out what items (if any) we have to add to the live schema
        List<PseMetatag> tagsToSync = m_migrate.CollectDependentTags(liveTree, metatags);
        string userTagName = MetatagStandards.GetStandardsTagFromStandard(MetatagStandards.Standard.User);
        IMetatag userRoot = m_appState.MetatagSchema.FindFirstMatchingItem(MetatagMatcher.CreateNameMatch(userTagName)) ?? m_appState.MetatagSchema.AddNewStandardRoot(MetatagStandards.Standard.User);

        PseMetatagTree treeToSync = new(tagsToSync);

        List<MetatagPair> tagsToInsert = BuildTagsToInsert(liveTree, treeToSync, userRoot);

        foreach (MetatagPair pair in tagsToInsert)
        {
            m_appState.MetatagSchema.AddMetatag(pair.Metatag);
            m_migrate.GetMetatagFromID(pair.PseId).CatID = pair.Metatag.ID;
        }

//        m_appState.RefreshMetatagSchema();
//
//        Debug.Assert(m_appState.MetatagSchema != null, "m_appState.MetatagSchema != null");
//        LiveMetatags.Initialize(m_appState.MetatagSchema, MetatagStandards.Standard.User);
    }
}