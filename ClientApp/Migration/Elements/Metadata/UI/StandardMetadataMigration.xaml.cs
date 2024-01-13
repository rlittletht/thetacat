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
using Thetacat.Metatags.Model;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Util;
using MetatagTree = Thetacat.Metatags.MetatagTree;

namespace Thetacat.Migration.Elements.Metadata.UI;

public partial class StandardMetadataMigration : UserControl
{
    private readonly SortableListViewSupport m_sortableListViewSupport;
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
        m_sortableListViewSupport = new SortableListViewSupport(metadataListView);
    }

    private void SortType(object sender, RoutedEventArgs e) => m_sortableListViewSupport.Sort(sender as GridViewColumnHeader);

    IMetatagTreeItem? GetRootForMetadataItem(PseMetadata item)
    {
        return App.State.MetatagSchema.WorkingTree.FindMatchingChild(
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
            item.Checked = false; // no need to migrate. its already there
        }
    }

    public void RefreshForSchemaChange()
    {
        if (_Migrate?.MetatagMigrate.MetadataSchema.MetadataItems == null)
            return;

        foreach (PseMetadata item in _Migrate.MetatagMigrate.MetadataSchema.MetadataItems)
        {
            item.CatID = null;
            item.Checked = false;
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

        if (App.State.MetatagSchema.SchemaVersionWorking == 0)
            App.State.RefreshMetatagSchema();
    }

    private void EditSelected(object sender, RoutedEventArgs e)
    {
        if (metadataListView.SelectedValue is PseMetadata metadata)
        {
            DefineMetadataMap define = new(App.State, metadata.PseIdentifier);

            bool? defined = define.ShowDialog();

            if (defined != null && defined.Value)
            {
                metadata.StandardTag = define.Standard.Text;
                metadata.PropertyTag = define.TagName.Text;
                metadata.Description = define.Description.Text;
                metadata.Checked = true;
            }
        }
    }

    private void DoMigrate(object sender, RoutedEventArgs e)
    {
        if (App.State?.MetatagSchema == null || _Migrate == null)
            throw new Exception("appstate or migrate uninitialized");

        foreach (PseMetadata? item in CheckableListViewSupport<PseMetadata>.GetCheckedItems(metadataListView))
        {
            if (!(item?.Checked ?? false))
                continue;

            if (item.CatID != null)
            {
                // make sure its really there
                if (App.State.MetatagSchema.GetMetatagFromId(item.CatID.Value) != null)
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
                IMetatagTreeItem? userRoot = App.State.MetatagSchema.WorkingTree.FindMatchingChild(
                    MetatagTreeItemMatcher.CreateNameMatch(rootName),
                    1);

                if (userRoot == null)
                {
                    App.State.MetatagSchema.AddNewStandardRoot(standard);
                    userRoot = App.State.MetatagSchema.WorkingTree.FindMatchingChild(
                        MetatagTreeItemMatcher.CreateNameMatch(rootName),
                        1);
                }

                if (userRoot == null)
                    throw new Exception("failed to create user root");

                IMetatagTreeItem? existing = userRoot.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(item.StandardTag), -1);
                if (existing == null)
                {
                    Metatag parentTag = Metatag.Create(Guid.Parse(userRoot.ID), item.StandardTag, item.StandardTag, MetatagStandards.Standard.User);
                    App.State.MetatagSchema.AddMetatag(parentTag);
                    existing = userRoot.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(item.StandardTag), -1);
                }

                parent = existing ?? throw new Exception("could not find or add parent tag");
            }
            else
            {
                IMetatagTreeItem? root = GetRootForMetadataItem(item);

                if (root == null)
                {
                    App.State.MetatagSchema.AddNewStandardRoot(standard);
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
                MetatagBuilder
                   .Create()
                   .SetDescription(item.Description)
                   .SetName(item.PropertyTag)
                   .SetParentID(Guid.Parse(parent.ID))
                   .SetStandard(standard)
                   .Build();

            App.State.MetatagSchema.AddMetatag(newTag);
            item.CatID = newTag.ID;

        }

        MessageBox.Show("All checked items have been added to the working schema. Go to the summary tab to upload to the database.");
    }

    private void DoKeyDown(object sender, KeyEventArgs e) => CheckableListViewSupport<PseMetadata>.DoKeyDown(metadataListView, sender, e);
}