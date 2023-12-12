using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
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
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Rapid;
using Emgu.CV.Structure;
using Microsoft.Extensions.Azure;
using Microsoft.Windows.Themes;

using System.Security.Cryptography;
using NUnit.Framework;
using Thetacat.Types;
using Thetacat.Controls;
using System.ComponentModel;


namespace Thetacat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GridViewColumnHeader? sortCol = null;
        private SortAdorner? sortAdorner;

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

        private void SortType(object sender, RoutedEventArgs e)
        {
            Sort(CatalogView, sender as GridViewColumnHeader);
        }

        private AppState m_appState;

        public MainWindow()
        {
            InitializeComponent();
            m_appState = new AppState();

            m_appState.RegisterWindowPlace(this, "MainWindow");
            CatalogView.ItemsSource = m_appState.Catalog.Items;
        }

        private void LaunchTest(object sender, RoutedEventArgs e)
        {
            UI.Test test = new UI.Test();

            test.Show();
        }

        private void LaunchMigration(object sender, RoutedEventArgs e)
        {
            Migration.Migration migration = new(m_appState);

            migration.ShowDialog();
        }

        private void ManageMetatags(object sender, RoutedEventArgs e)
        {
            Metatags.ManageMetadata manage = new(m_appState);
            manage.ShowDialog();
        }

        private void LoadCatalog(object sender, RoutedEventArgs e)
        {
            m_appState.Catalog.ReadFullCatalogFromServer(m_appState.MetatagSchema);
        }
    }
}