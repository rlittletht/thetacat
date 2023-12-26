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
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Thetacat.Controls;
using Thetacat.Model.Metatags;
using Thetacat.Types;
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
        m_diff = MainWindow._AppState.MetatagSchema.BuildDiffForSchemas();
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

        MainWindow._AppState.RefreshMetatagSchema();
        _Migrate.MetatagMigrate.ReloadSchemas();
        RebuildSchemaDiff();
        MessageBox.Show("All changes have been uploaded to the server. All tabs have been refreshed.");
    }

    private void DoKeyDown(object sender, KeyEventArgs e) => CheckableListViewSupport<MetatagMigrationItem>.DoKeyDown(diffOpListView, sender, e);
}