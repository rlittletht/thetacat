using System;
using System.Collections.Concurrent;
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
using Thetacat.Import;
using Thetacat.Model;
using Thetacat.UI.Options;
using Thetacat.Azure;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Thetacat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#region Sort Support

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

#endregion
        private static AppState? s_appState;

        public static IAppState _AppState
        {
            get
            {
                if (s_appState == null)
                    throw new Exception($"app state uninitialized. _AppState queried too early");
                return s_appState;
            }
        }

        public static string ClientName = Environment.MachineName;

        public MainWindow()
        {
            InitializeComponent();
            InitializeThetacat();

            _AppState.RegisterWindowPlace(this, "MainWindow");
            CatalogView.ItemsSource = _AppState.Catalog.Items;
        }

        public static void SetStateForTests(AppState appState)
        {
            s_appState = appState;
        }

        void InitializeThetacat()
        {
            s_appState = new AppState();
        }

        private void LaunchTest(object sender, RoutedEventArgs e)
        {
            UI.Test test = new UI.Test();

            test.Show();
        }

        private void LaunchMigration(object sender, RoutedEventArgs e)
        {
            Migration.Migration migration = new();

            migration.ShowDialog();
        }

        private void ManageMetatags(object sender, RoutedEventArgs e)
        {
            Metatags.ManageMetadata manage = new();
            manage.ShowDialog();
        }

        private void LoadCatalog(object sender, RoutedEventArgs e)
        {
            _AppState.Catalog.ReadFullCatalogFromServer(_AppState.MetatagSchema);
        }

        private void LaunchOptions(object sender, RoutedEventArgs e)
        {
            CatOptions options = new CatOptions();
            if (options.ShowDialog() ?? false)
            {
                options.SaveToSettings();
                _AppState.Settings.WriteSettings();
            }
        }

        private void DoCacheItems(object sender, RoutedEventArgs e)
        {
            _AppState.Cache.DoForegroundCache(100);
        }

        private async void UploadItems(object sender, RoutedEventArgs e)
        {
            MediaImport import = new MediaImport(MainWindow.ClientName);
            
            await import.UploadMedia();
        }
    }
}
