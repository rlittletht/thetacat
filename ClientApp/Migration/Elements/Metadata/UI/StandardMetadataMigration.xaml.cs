using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
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
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Standards;
using Thetacat.Types;
using MetatagTree = Thetacat.Metatags.MetatagTree;

namespace Thetacat.Migration.Elements.Metadata.UI;

public partial class StandardMetadataMigration : UserControl
{
    private GridViewColumnHeader? sortCol = null;
    private SortAdorner? sortAdorner;
    private ElementsMigrate? m_migrate = null;
    private ElementsMigrate _Migrate
    {
        get
        {
            if (m_migrate == null)
                throw new Exception($"initialize never called on {this.GetType().Name}");
            return m_migrate;
        }
    }

    /// <summary>
    /// Interaction logic for UserMetatagMigration.xaml
    /// </summary>
    public StandardMetadataMigration()
    {
        InitializeComponent();
    }

    private void DoToggleSelected(object sender, RoutedEventArgs e)
    {
        ToggleItems(metadataListView.SelectedItems as IEnumerable<object?>);
    }

    private void SortType(object sender, RoutedEventArgs e)
    {
        Sort(metadataListView, sender as GridViewColumnHeader);
    }

    IMetatagTreeItem? GetRootForMetadataItem(PseMetadata item)
    {
        return MainWindow._AppState.MetatagSchema.WorkingTree.FindMatchingChild(
            MetatagTreeItemMatcher.CreateNameMatch(MetatagStandards.GetMetadataRootFromStandardTag(item.StandardTag)),
            1);
    }

    IMetatagTreeItem? GetCatMetadataForMetadataItem(IMetatagTreeItem root, PseMetadata item)
    {
        return root.FindMatchingChild(
            MetatagTreeItemMatcher.CreateNameMatch(item.PropertyTag),
            1);
    }

    void MarkExistingMetadata()
    {
        foreach (PseMetadata item in _Migrate.MetatagMigrate.MetadataSchema.MetadataItems)
        {
            if (item.StandardTag == string.Empty)
                continue;

            // see if there's a match in the database already
            IMetatagTreeItem? root = GetRootForMetadataItem(item);

            if (root == null)
                continue;

            IMetatagTreeItem? match = GetCatMetadataForMetadataItem(root, item);

            if (match == null)
                continue;

            // found a matching item in the cat database. mark it here
            item.CatID = Guid.Parse(match.ID);
            item.Migrate = false; // no need to migrate. its already there
        }
    }

    public void RefreshForSchemaChange()
    {
        if (_Migrate?.MetatagMigrate.MetadataSchema.MetadataItems == null)
            return;

        foreach (PseMetadata item in _Migrate.MetatagMigrate.MetadataSchema.MetadataItems)
        {
            item.CatID = null;
            item.Migrate = false;
        }
        
        MarkExistingMetadata();
    }

    public void Initialize(ElementsDb db, ElementsMigrate migrate)
    {
        m_migrate = migrate;

        _Migrate.MetatagMigrate.SetMetadataSchema(db.ReadMetadataSchema());
        _Migrate.MetatagMigrate.MetadataSchema.PopulateBuiltinMappings();
        MarkExistingMetadata();

        metadataListView.ItemsSource = _Migrate.MetatagMigrate.MetadataSchema.MetadataItems;

        if (MainWindow._AppState.MetatagSchema.SchemaVersionWorking == 0)
            MainWindow._AppState.RefreshMetatagSchema();
    }

    private void EditSelected(object sender, RoutedEventArgs e)
    {
        if (metadataListView.SelectedValue is PseMetadata metadata)
        {
            DefineMetadataMap define = new(MainWindow._AppState, metadata.PseIdentifier);

            bool? defined = define.ShowDialog();

            if (defined != null && defined.Value)
            {
                metadata.StandardTag = define.Standard.Text;
                metadata.PropertyTag = define.TagName.Text;
                metadata.Description = define.Description.Text;
                metadata.Migrate = true;
            }
        }
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

    private void DoMigrate(object sender, RoutedEventArgs e)
    {
        if (MainWindow._AppState?.MetatagSchema == null || _Migrate == null)
            throw new Exception("appstate or migrate uninitialized");

        foreach (PseMetadata? item in metadataListView.Items)
        {
            if (!(item?.Migrate ?? false))
                continue;

            if (item.CatID != null)
            {
                // make sure its really there
                if (MainWindow._AppState.MetatagSchema.FindFirstMatchingItem(MetatagMatcher.CreateIdMatch(item.CatID.Value)) != null)
                    continue;

                Debug.Assert(false, "strange. we had a catid, but its not in the working schema??");
            }

            MetatagStandards.Standard standard = MetatagStandards.GetStandardFromStandardTag(item.StandardTag);

            IMetatagTreeItem parent;

            // unknown items are either builtin (skip) or user defined
            if (standard == MetatagStandards.Standard.Unknown)
            {
                if (item.StandardTag == "builtin")
                    continue;

                // otherwise this is a user-define element
                standard = MetatagStandards.Standard.User;
                string rootName = MetatagStandards.GetMetadataRootFromStandard(standard);
                IMetatagTreeItem? userRoot = MainWindow._AppState.MetatagSchema.WorkingTree.FindMatchingChild(
                    MetatagTreeItemMatcher.CreateNameMatch(rootName),
                    1);

                if (userRoot == null)
                {
                    MainWindow._AppState.MetatagSchema.AddNewStandardRoot(standard);
                    userRoot = MainWindow._AppState.MetatagSchema.WorkingTree.FindMatchingChild(
                        MetatagTreeItemMatcher.CreateNameMatch(rootName),
                        1);
                }

                if (userRoot == null)
                    throw new Exception("failed to create user root");

                IMetatagTreeItem? existing = userRoot.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(item.StandardTag), -1);
                if (existing == null)
                {
                    Metatag parentTag = Metatag.Create(Guid.Parse(userRoot.ID), item.StandardTag, item.StandardTag, MetatagStandards.Standard.User);
                    MainWindow._AppState.MetatagSchema.AddMetatag(parentTag);
                    existing = userRoot.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(item.StandardTag), -1);
                }

                parent = existing ?? throw new Exception("could not find or add parent tag");
            }
            else
            {
                IMetatagTreeItem? root = GetRootForMetadataItem(item);

                if (root == null)
                {
                    MainWindow._AppState.MetatagSchema.AddNewStandardRoot(standard);
                    root = GetRootForMetadataItem(item);
                    if (root == null)
                        throw new Exception("failed to create standard root");
                }

                parent = root;
            }

            // make sure its not already there
            if (GetCatMetadataForMetadataItem(parent, item) != null)
            {
                Debug.Assert(false, "strange. we didn't already know about this item but its in the working schema...");
                continue;
            }

            Metatag newTag =
                new()
                {
                    ID = Guid.NewGuid(),
                    Description = item.Description,
                    Name = item.PropertyTag,
                    Parent = Guid.Parse(parent.ID)
                };

            MainWindow._AppState.MetatagSchema.AddMetatag(newTag);
            item.CatID = newTag.ID;

            MessageBox.Show("All checked items have been added to the working schema. Go to the summary tab to upload to the database.");
        }
    }
}