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
using Thetacat.Migration.Elements.Metadata;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;
using MetatagTree = Thetacat.Metatags.MetatagTree;

namespace Thetacat.Migration.Elements;

public partial class StandardMetadataMigration : UserControl
{
    private GridViewColumnHeader? sortCol = null;
    private SortAdorner? sortAdorner;
    private MetatagMigrate? m_migrate = null;

    IAppState? m_appState;

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

    public void Initialize(IAppState appState, ElementsDb db, MetatagMigrate migrate)
    {
        m_appState = appState;
        m_migrate = migrate;

        if (m_appState == null)
            throw new ArgumentNullException(nameof(appState));

        m_appState = appState;
        m_migrate.SetMetatagSchema(db.ReadMetadataSchema());
        m_migrate.Schema.PopulateBuiltinMappings();

        metadataListView.ItemsSource = m_migrate.Schema.MetadataItems;

        if (m_appState.MetatagSchema == null)
            m_appState.RefreshMetatagSchema();
    }

    private void EditSelected(object sender, RoutedEventArgs e)
    {
        if (metadataListView.SelectedValue is PseMetadata metadata)
        {
            Debug.Assert(m_appState != null, nameof(m_appState) + " != null");
            DefineMetadataMap define = new(m_appState, metadata.PseIdentifier);

            bool? defined = define.ShowDialog();

            if (defined != null && defined.Value)
            {
                metadata.Standard = define.Standard.Text;
                metadata.Tag = define.TagName.Text;
                metadata.Migrate = true;
            }
        }

        RemoveItems(metadataListView.SelectedItems as IEnumerable<object?>);
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
    }

    private void DoMigrate(object sender, RoutedEventArgs e)
    {

    }
}