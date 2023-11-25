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
using Thetacat.Controls;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;

namespace Thetacat.Migration.Elements
{
    public partial class MetatagMigration : UserControl
    {
        private GridViewColumnHeader? sortCol = null;
        private SortAdorner? sortAdorner;
        private ObservableCollection<ElementsMetaTag>? m_metatags;
        IAppState? m_appState;

        /// <summary>
        /// Interaction logic for MetatagMigration.xaml
        /// </summary>
        public MetatagMigration()
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

        public void Initialize(IAppState appState, ElementsDb db)
        {
            m_appState = appState;
            ObservableCollection<ElementsMetaTag> tags = new();
            foreach (ElementsMetaTag metaTag in db.ReadMetadataTags())
            {
                tags.Add(metaTag);
            }

            m_metatags = tags;
            metaTagsListView.ItemsSource = m_metatags;
        }

        private void RemoveSelected(object sender, RoutedEventArgs e)
        {
            RemoveItems(metaTagsListView.SelectedItems as IEnumerable<object?>);
        }

        public void ToggleItems(IEnumerable<object?>? items)
        {
            if (items == null)
                return;

            foreach (ElementsMetaTag? item in items)
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
            if (items == null || m_metatags == null) 
                return;

            List<object> removeList = new();
            foreach (ElementsMetaTag? item in items)
            {
                if (item != null)
                    removeList.Add(item);
            }

            foreach (ElementsMetaTag tag in removeList)
            {
                m_metatags.Remove(tag);
            }
        }

        private void DoMigrate(object sender, RoutedEventArgs e)
        {

        }
    }
}