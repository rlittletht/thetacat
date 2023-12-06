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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

    IAppState? m_appState;

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

    public void Initialize(IAppState appState, ElementsDb db, MetatagMigrate migrate)
    {
        m_appState = appState;
        m_migrate = migrate;

        if (m_appState == null)
            throw new ArgumentNullException(nameof(appState));

        m_appState = appState;
        m_migrate.SetUserMetatags(db.ReadMetadataTags());
        
        metaTagsListView.ItemsSource = m_migrate.UserMetatags;

        if (m_appState.MetatagSchema == null)
            m_appState.RefreshMetatagSchema();

        Debug.Assert(m_appState.MetatagSchema != null, "m_appState.MetatagSchema != null");
        LiveMetatags.Initialize(m_appState.MetatagSchema, MetatagStandards.Standard.User);
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

    }

    static void MatchAndInsertChildrenIfNeeded(
        IMetatagTreeItem? liveParent,
        IMetatagTreeItem parent,
        List<Model.Metatag> tagsToInsert,
        Guid? idParent,
        List<string> nameHistory)
    {
        // we have to build the tags to sync from the parent to the leaf in order to make sure we
        // build the correct relationships (we may have duplicate names in the tree, but they might
        // be unique because of hierarchy.  "Dog" might be under Pets/Dog and also Toys/Dog. We need
        // two Dog nodes in this case.

        foreach (IMetatagTreeItem item in parent.Children)
        {
            // look for a matching root in the current schema
            IMetatagTreeItem? match = liveParent?.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(item.Name), 1/*levelsToRecurse*/);
            Guid parentId;

            nameHistory.Add(item.Name);

            if (match == null)
            {
                Model.Metatag newTag = new()
                {
                    ID = Guid.NewGuid(),
                    Description = string.Join(":", nameHistory.ToArray()),
                    Name = item.Name,
                    Parent = idParent
                };

                tagsToInsert.Add(newTag);
                parentId = newTag.ID;
            }
            else
            {
                parentId = Guid.Parse(match.ID);
            }

            MatchAndInsertChildrenIfNeeded(match, item, tagsToInsert, parentId, nameHistory);
            nameHistory.RemoveAt(nameHistory.Count - 1);
        }
    }

    public static List<Model.Metatag> BuildTagsToInsert(Metatags.MetatagTree liveTree, List<PseMetatag> tagsToSync)
    {
        // build a hierchical tree for the tags to sync
        PseMetatagTree treeToSync = new(tagsToSync);

        List<Model.Metatag> tagsToInsert = new();
        MatchAndInsertChildrenIfNeeded(liveTree, treeToSync, tagsToInsert, null, new List<string>());

        return tagsToInsert;
    }

    /*----------------------------------------------------------------------------
        %%Function: MigrateSelected
        %%Qualified: Thetacat.Migration.Elements.UserMetatagMigration.MigrateSelected

        Migrate the selected elements tags (and their parents)
    ----------------------------------------------------------------------------*/
    private void MigrateSelected(object sender, RoutedEventArgs e)
    {
        if (m_appState == null || m_migrate == null)
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
        m_migrate.BuildMetatagTree(m_migrate.UserMetatags);

        Metatags.MetatagTree liveTree = LiveMetatags.Model;

        // now figure out what items (if any) we have to add to the live schema
        List<PseMetatag> tagsToSync = m_migrate.CollectDependentTags(liveTree, metatags);
        List<Metatag> tagsToInsert = BuildTagsToInsert(liveTree, tagsToSync);

        MetatagSchemaDiff diff = new(LiveMetatags.SchemaVersion);

        foreach (Model.Metatag metatag in tagsToInsert)
        {
            diff.InsertMetatag(metatag);
        }

        ServiceClient.LocalService.Metatags.UpdateMetatagSchema(diff);
        m_appState.RefreshMetatagSchema();

        Debug.Assert(m_appState.MetatagSchema != null, "m_appState.MetatagSchema != null");
        LiveMetatags.Initialize(m_appState.MetatagSchema, MetatagStandards.Standard.User);
    }
}