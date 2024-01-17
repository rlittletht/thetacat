using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Thetacat.Metatags.Model;
using Thetacat.Util;

namespace Thetacat.Migration.Elements.Metadata.UI;

/// <summary>
/// Interaction logic for MetadataMigrateSummary.xaml
/// </summary>
public partial class MetadataMigrateSummary : UserControl
{
    private readonly SortableListViewSupport m_sortableListViewSupport;
    private ElementsMigrate? m_migrate;

    private ElementsMigrate _Migrate
    {
        get
        {
            if (m_migrate == null)
                throw new Exception($"initialize never called on {this.GetType().Name}");
            return m_migrate;
        }
    }

    private readonly ObservableCollection<MetatagMigrationItem> m_metatagMigrationItems = new();
    private MetatagSchemaDiff? m_diff;

    public MetadataMigrateSummary()
    {
        InitializeComponent();
        m_sortableListViewSupport = new SortableListViewSupport(diffOpListView);
        diffOpListView.ItemsSource = m_metatagMigrationItems;
    }

    public void Initialize(ElementsMigrate migrate)
    {
        m_migrate = migrate;
    }

    private void SortType(object sender, RoutedEventArgs e) => m_sortableListViewSupport.Sort(sender as GridViewColumnHeader);

    public void RebuildSchemaDiff()
    {
        // build the schema differences for all the metadata and metatag migration tabs
        m_diff = App.State.MetatagSchema.BuildDiffForSchemas();
        m_metatagMigrationItems.Clear();

        foreach (MetatagSchemaDiffOp op in m_diff.Ops)
        {
            m_metatagMigrationItems.Add(new MetatagMigrationItem(op));
        }
    }

    private void DoMigrate(object sender, RoutedEventArgs e)
    {
        if (m_diff == null)
            return;

        // commit all the diff ops
        ServiceClient.LocalService.Metatags.UpdateMetatagSchema(m_diff);

        App.State.RefreshMetatagSchema();
        _Migrate.ReloadSchemas();
        RebuildSchemaDiff();
        MessageBox.Show("All changes have been uploaded to the server. All tabs have been refreshed.");
    }

    private void DoKeyDown(object sender, KeyEventArgs e) => CheckableListViewSupport<MetatagMigrationItem>.DoKeyDown(diffOpListView, sender, e);
}