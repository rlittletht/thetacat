using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Thetacat.Model;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Metadata.UI;

/// <summary>
/// Interaction logic for MetadataMigrateSummary.xaml
/// </summary>
public partial class MetadataMigrateSummary : UserControl
{
    private GridViewColumnHeader? sortCol = null;
    private SortAdorner? sortAdorner;

    private IAppState? m_appState;
    private MetatagMigrate? m_migrate;

    private readonly ObservableCollection<MetatagMigrationItem> m_metatagMigrationItems = new ObservableCollection<MetatagMigrationItem>();
    private MetatagSchemaDiff? m_diff;

    public MetadataMigrateSummary()
    {
        InitializeComponent();
        diffOpListView.ItemsSource = m_metatagMigrationItems;
    }

    public void Initialize(IAppState appState, MetatagMigrate migrate)
    {
        m_migrate = migrate;
        m_appState = appState;
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
        if (m_appState?.MetatagSchema == null)
            throw new Exception("not initialized");

        // build the schema differences for all the metadata and metatag migration tabs
        m_diff = m_appState.MetatagSchema.BuildDiffForSchemas();
        m_metatagMigrationItems.Clear();

        foreach (MetatagSchemaDiffOp op in m_diff.Ops)
        {
            m_metatagMigrationItems.Add(new MetatagMigrationItem(op));
        }
    }
}