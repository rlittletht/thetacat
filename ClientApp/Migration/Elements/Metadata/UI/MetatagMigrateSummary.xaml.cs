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

namespace Thetacat.Migration.Elements.Metadata.UI;

/// <summary>
/// Interaction logic for MetadataMigrateSummary.xaml
/// </summary>
public partial class MetadataMigrateSummary : UserControl
{
    private GridViewColumnHeader? sortCol = null;
    private SortAdorner? sortAdorner;
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
        diffOpListView.ItemsSource = m_metatagMigrationItems;
    }

    public void Initialize(ElementsMigrate migrate)
    {
        m_migrate = migrate;
    }

    private void SortType(object sender, RoutedEventArgs e)
    {
        Sort(diffOpListView, sender as GridViewColumnHeader);
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
        MessageBox.Show("All changes have been uploaded to the server. All tabs have been refreshed.");
    }
}