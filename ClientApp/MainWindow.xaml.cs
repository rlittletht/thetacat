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
using Thetacat.Logging;
using Thetacat.UI;
using MessageBox = System.Windows.Forms.MessageBox;
using RestoreWindowPlace;
using Thetacat.Migration.Elements.Media.UI;
using Thetacat.Migration.Elements.Media;

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
        private static IAppState? s_appState;
        private static CatLog? s_asyncLog;
        private static CatLog? s_appLog;

        public static CatLog _AsyncLog => s_asyncLog ?? throw new CatExceptionInitializationFailure("async log not initialized");
        public static CatLog _AppLog => s_appLog ?? throw new CatExceptionInitializationFailure("appLog not initialized");

        public static void LogForAsync(EventType eventType, string log, string? details = null, Guid? correlationId = null)
        {
            ILogEntry entry = new LogEntry(eventType, log, correlationId?.ToString() ?? "", details);
            _AsyncLog.Log(entry);
            _AppLog.Log(entry);
        }

        public static void LogForApp(EventType eventType, string log, string? details = null, Guid? correlationId = null)
        {
            ILogEntry entry = new LogEntry(eventType, log, correlationId?.ToString() ?? "", details);
            _AppLog.Log(entry);
        }

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

            // we have to load the catalog AND the pending upload list
            // we also have to confirm that all the items int he pending
            // upload list still exist in the catalog, and if they don't
            // (or if they are marked as active in the catalog, which means
            // they are already uploaded), then remove them from the import
            // list

            _AppState.RegisterWindowPlace(this, "MainWindow");
            CatalogView.ItemsSource = _AppState.Catalog.Media.Items;
            if (_AppState.Settings.ShowAsyncLogOnStart ?? false)
                ShowAsyncLog();
            if (_AppState.Settings.ShowAppLogOnStart ?? false)
                ShowAppLog();
        }

        void OnClosing(object sender, EventArgs e)
        {
            _AppState.Settings.ShowAsyncLogOnStart = m_asyncLogMonitor != null;
            _AppState.Settings.ShowAppLogOnStart = m_appLogMonitor != null;

            if (m_asyncLogMonitor != null)
                CloseAsyncLog(false);

            if (m_appLogMonitor != null)
                CloseAppLog(false);

            _AppState.Settings.WriteSettings();
        }

        public static void SetStateForTests(IAppState? appState)
        {
            s_appState = appState;
        }

        void InitializeThetacat()
        {
            s_appState = new AppState(CloseAsyncLog, CloseAppLog);
            s_asyncLog = new CatLog(EventType.Information);
            s_appLog = new CatLog(EventType.Warning);
        }

        private void LaunchTest(object sender, RoutedEventArgs e)
        {
            UI.Test test = new UI.Test();

            test.Show();
        }

        private void LaunchMigration(object sender, RoutedEventArgs e)
        {
            Migration.Migration migration = new();

            migration.Show();
        }

        private void ManageMetatags(object sender, RoutedEventArgs e)
        {
            Metatags.ManageMetadata manage = new();
            manage.Show();
        }

        private void ConnectToDatabase(object sender, RoutedEventArgs e)
        {
            _AppState.Catalog.ReadFullCatalogFromServer(_AppState.MetatagSchema);

            AzureCat.EnsureCreated(_AppState.AzureStorageAccount);
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

        private async void DoCacheItems(object sender, RoutedEventArgs e)
        {
            try
            {
                await _AppState.Cache.DoForegroundCache(100);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uncaught exception: {ex.Message}");
            }
        }

        private async void UploadItems(object sender, RoutedEventArgs e)
        {
            MediaImport? import = null;

            try
            {
                import = new MediaImport(MainWindow.ClientName);
            }
            catch (CatExceptionCanceled)
            {
                return;
            }

            await import.UploadMedia();
        }

        private AsyncLogMonitor? m_asyncLogMonitor;
        private AppLogMonitor? m_appLogMonitor;

        void ShowAsyncLog()
        {
            if (m_asyncLogMonitor != null)
                return;

            m_asyncLogMonitor = new AsyncLogMonitor();
            m_asyncLogMonitor.Show();
        }

        void CloseAsyncLog(bool skipClose)
        {
            if (!skipClose)
                m_asyncLogMonitor?.Close();
            m_asyncLogMonitor = null;
        }

        private void ToggleAsyncLog(object sender, RoutedEventArgs e)
        {
            if (m_asyncLogMonitor != null)
                CloseAsyncLog(false);
            else
                ShowAsyncLog();
        }

        void ShowAppLog()
        {
            if (m_appLogMonitor != null)
                return;

            m_appLogMonitor = new AppLogMonitor();
            m_appLogMonitor.Show();
        }

        void CloseAppLog(bool skipClose)
        {
            if (!skipClose)
                m_appLogMonitor?.Close();
            m_appLogMonitor = null;
        }

        private void ToggleAppLog(object sender, RoutedEventArgs e)
        {
            if (m_appLogMonitor != null)
                CloseAppLog(false);
            else
                ShowAppLog();
        }

        private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            KeyValuePair<Guid, MediaItem>? selected = CatalogView.SelectedItem as KeyValuePair<Guid, MediaItem>?;
            MediaItem? item = selected?.Value;

            if (item != null)
            {
                MediaItemDetails details = new MediaItemDetails(item);

                details.ShowDialog();
            }
        }

        private void UpdateMediaItems(object sender, RoutedEventArgs e)
        {
            _AppState.Catalog.PushPendingChanges();
        }
    }
}
