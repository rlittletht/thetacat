using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Thetacat.Types;
using Thetacat.Controls;
using System.ComponentModel;
using System.Threading;
using System.Windows.Media;
using Thetacat.Import;
using Thetacat.Model;
using Thetacat.UI.Options;
using Thetacat.Azure;
using Thetacat.Logging;
using Thetacat.UI;
using MessageBox = System.Windows.Forms.MessageBox;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Util;
using Thetacat.UI.ProgressReporting;
using Thetacat.Explorer;
using System.Windows.Media.Imaging;
using System.Globalization;
using Microsoft.Windows.EventTracing.Power;
using Thetacat.Metatags.Model;
using Thetacat.Filtering;

namespace Thetacat;

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

    public static bool InUnitTest { get; set; } = false;
    private static CatLog? s_asyncLog;
    private static CatLog? s_appLog;
    private readonly BackgroundWorkers m_mainBackgroundWorkers;

    public static CatLog _AsyncLog => s_asyncLog ?? throw new CatExceptionInitializationFailure("async log not initialized");
    public static CatLog _AppLog => s_appLog ?? throw new CatExceptionInitializationFailure("appLog not initialized");

    public static void LogForAsync(EventType eventType, string log, string? details = null, Guid? correlationId = null)
    {
        if (_AsyncLog.ShouldLog(eventType))
        {
            ILogEntry entry = new LogEntry(eventType, log, correlationId?.ToString() ?? "", details);

            _AsyncLog.Log(entry);
            _AppLog.Log(entry);
        }
    }

    public static void LogForApp(EventType eventType, string log, string? details = null, Guid? correlationId = null)
    {
        if (_AppLog.ShouldLog(eventType))
        {
            ILogEntry entry = new LogEntry(eventType, log, correlationId?.ToString() ?? "", details);
            _AppLog.Log(entry);
        }
    }

    public static string ClientName = Environment.MachineName;

    private readonly MediaExplorerCollection m_collection = new(14.0);

    public MainWindow()
    {
        InitializeComponent();
        InitializeThetacat();

        Explorer.SetExplorerItemSize(App.State.Settings.ExplorerItemSize ?? ExplorerItemSize.Medium);
        // we have to load the catalog AND the pending upload list
        // we also have to confirm that all the items int he pending
        // upload list still exist in the catalog, and if they don't
        // (or if they are marked as active in the catalog, which means
        // they are already uploaded), then remove them from the import
        // list

        App.State.RegisterWindowPlace(this, "MainWindow");
        CatalogView.ItemsSource = App.State.Catalog.GetObservableCollection();
        LocalServiceClient.LogService = LogForApp;
        DataContext = m_collection;
        if (!string.IsNullOrWhiteSpace(App.State.Settings.DefaultFilterName))
        {
            if (App.State.Settings.Filters.TryGetValue(App.State.Settings.DefaultFilterName, out FilterDefinition? filter))
                m_collection.SetFilter(filter);
        }

        m_mainBackgroundWorkers = new BackgroundWorkers(BackgroundActivity.Start, BackgroundActivity.Stop);
        App.State.DpiScale = VisualTreeHelper.GetDpi(this);
    }

    void OnClosing(object sender, EventArgs e)
    {
        App.State.Derivatives.CommitDerivatives();
        App.State.Derivatives.Close();
        App.State.Settings.ShowAsyncLogOnStart = m_asyncLogMonitor != null;
        App.State.Settings.ShowAppLogOnStart = m_appLogMonitor != null;
        m_collection.Close();
        Explorer.Close();
        App.State.PreviewImageCache.Close();
        App.State.ImageCache.Close();
        App.State.Md5Cache.Close();

        App.State.ClientDatabase.Close();

        if (m_asyncLogMonitor != null)
            CloseAsyncLog(false);

        if (m_appLogMonitor != null)
            CloseAppLog(false);

        App.State.Settings.WriteSettings();
    }

    void InitializeThetacat()
    {
        App.State.SetupLogging(CloseAsyncLog, CloseAppLog);
        App.State.SetupBackgroundWorkers(AddBackgroundWork);

        s_asyncLog = new CatLog(EventType.Information);
        s_appLog = new CatLog(EventType.Information);
    }

    private void LaunchTest(object sender, RoutedEventArgs e)
    {
        UI.Test test = new UI.Test();

        test.Show();
    }

    private void LaunchMigration(object sender, RoutedEventArgs e)
    {
        Migration.Migration migration = new();
        migration.Owner = this;
        migration.Show();
    }

    private void ManageMetatags(object sender, RoutedEventArgs e)
    {
        Metatags.ManageMetadata manage = new();
        manage.Owner = this;
        manage.Show();
    }

    public static DateTime GetLocalDateFromMedia(MediaItem item, DateTime? mediaDate)
    {
//            DateTime? mediaDate = item.OriginalMediaDate;

        if (mediaDate != null)
            return mediaDate.Value.ToLocalTime().Date;

        string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

        if (path != null)
            return File.GetCreationTime(path);

        return DateTime.Now;
    }

//        void BuildTimelineCollectionFromMedia()
//        {
//            MicroTimer timer = new MicroTimer();
//            LogForApp(EventType.Information, "Beginning building timeline collection");
//
//            // build a group by date
//            Dictionary<DateTime, List<Guid>> dateGrouping = new();
//
//            foreach (MediaItem item in App.State.Catalog.GetMediaCollection())
//            {
//                DateTime date = GetLocalDateFromMedia(item);
//
//                if (!dateGrouping.TryGetValue(date, out List<Guid>? items))
//                {
//                    items = new List<Guid>();
//                    dateGrouping.Add(date, items);
//                }
//
//                items.Add(item.ID);
//            }
//
//            ImmutableSortedSet<DateTime> sortedDates = dateGrouping.Keys.ToImmutableSortedSet();
//
//            m_collection.Clear();
//
//            foreach (DateTime date in sortedDates)
//            {
//                bool newSegment = true;
//
//                List<Guid> items = dateGrouping[date];
//                foreach (Guid id in items)
//                {
//                    MediaItem item = App.State.Catalog.GetMediaFromId(id);
//                    m_collection.AddToExplorerCollection(item, newSegment, date.ToString("MMM dd, yyyy"));
//                    newSegment = false;
//                }
//            }
//
//            LogForApp(EventType.Information, $"Done building. {timer.Elapsed()}");
//        }

    private async void ConnectToDatabase(object sender, RoutedEventArgs e)
    {
        LogForApp(EventType.Information, "Beginning read catalog");
        MicroTimer timer = new MicroTimer();

        await App.State.Catalog.ReadFullCatalogFromServer(App.State.MetatagSchema);

        LogForApp(EventType.Information, $"Done after ReadFullCatalogFromServer. {timer.Elapsed()}");
        timer.Reset();
        timer.Start();

        List<MediaExplorerItem> explorerItems = new();

        m_collection.AdjustPanelItemWidth(Explorer.Model.PanelItemWidth);
        m_collection.AdjustPanelItemHeight(Explorer.Model.PanelItemHeight);
        m_collection.AdjustExplorerWidth(Explorer.ExplorerBox.ActualWidth);
        m_collection.AdjustExplorerHeight(Explorer.ExplorerBox.ActualHeight);
        m_collection.UpdateItemsPerLine();

        LogForApp(EventType.Information, $"Done reading catalog. {timer.Elapsed()}");
        timer.Reset();
        timer.Start();

        TimelineType timelineType = m_collection.TimelineType;
        if (timelineType.Equals(TimelineType.None))
        {
            if (App.State.Settings.TimelineType != null)
                timelineType = App.State.Settings.TimelineType;

            if (timelineType.Equals(TimelineType.None))
                timelineType = TimelineType.MediaDate;
        }

        TimelineOrder timelineOrder = m_collection.TimelineOrder;
        if (timelineOrder.Equals(TimelineOrder.None))
        {
            if (App.State.Settings.TimelineOrder != null)
                timelineOrder = App.State.Settings.TimelineOrder;

            if (timelineOrder.Equals(TimelineOrder.None))
                timelineOrder = TimelineOrder.Ascending;
        }

        m_collection.ResetTimeline();
        m_collection.SetTimelineTypeAndOrder(timelineType, timelineOrder);

        LogForApp(EventType.Information, $"Done building timeline. {timer.Elapsed()}");

        timer.Reset();
        timer.Start();
        LogForApp(EventType.Information, "Beginning reset content");
        Explorer.ResetContent(m_collection); // explorerItems);

        AzureCat.EnsureCreated(App.State.AzureStorageAccount);
        LogForApp(EventType.Information, $"Done reset. {timer.Elapsed()}");
    }

    private void LaunchOptions(object sender, RoutedEventArgs e)
    {
        CatOptions options = new CatOptions();
        if (options.ShowDialog() ?? false)
        {
            options.SaveToSettings();
            App.State.Settings.WriteSettings();
        }
    }

    private void DoCacheItems(object sender, RoutedEventArgs e)
    {
        try
        {
            App.State.Cache.StartBackgroundCaching(100);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Uncaught exception: {ex.Message}");
        }
    }

    private void UploadItems(object sender, RoutedEventArgs e)
    {
        MediaImporter? import = null;

        try
        {
            import = new MediaImporter(MainWindow.ClientName);
        }
        catch (CatExceptionCanceled)
        {
            return;
        }

        import.UploadMedia();
    }

    private AsyncLogMonitor? m_asyncLogMonitor;
    private AppLogMonitor? m_appLogMonitor;

    void ShowAsyncLog()
    {
        if (m_asyncLogMonitor != null)
            return;

        m_asyncLogMonitor = new AsyncLogMonitor();
        m_asyncLogMonitor.Owner = this;
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
        m_appLogMonitor.Owner = this;
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
        MediaItem? item = CatalogView.SelectedItem as MediaItem;

        if (item != null)
        {
            MediaItemDetails details = new MediaItemDetails(item);

            details.Owner = this;
            details.ShowDialog();
        }
    }

    private void CommitPendingChanges(object sender, RoutedEventArgs e)
    {
        App.State.Catalog.PushPendingChanges();
        App.State.MetatagSchema.UpdateServer();
    }

    private void SelectLargePreview(object sender, RoutedEventArgs e)
    {
        Explorer.SetExplorerItemSize(ExplorerItemSize.Large);
    }

    private void SelectMediumPreview(object sender, RoutedEventArgs e)
    {
        Explorer.SetExplorerItemSize(ExplorerItemSize.Medium);
    }

    private void SelectSmallPreview(object sender, RoutedEventArgs e)
    {
        Explorer.SetExplorerItemSize(ExplorerItemSize.Small);
    }

    void DoWork(IProgressReport report)
    {
        for (int i = 0; i < 100; i++)
        {
            Thread.Sleep(50);
            report.UpdateProgress(i);
        }
        report.WorkCompleted();
    }

    private void TestProgressDialog(object sender, RoutedEventArgs e)
    {
        ProgressDialog.DoWorkWithProgress(DoWork, this);
    }

    private bool BackgroundTestTask(IProgressReport progressReport, int totalMsec)
    {
        bool fIndeterminate = true;

        if (totalMsec < 0)
        {
            progressReport.SetIndeterminate();
            totalMsec = -totalMsec;
        }

        int interval = Math.Max(1, totalMsec / 50); // we want 50 updates
        int elapsed = 0;

        while (elapsed < totalMsec)
        {
            Thread.Sleep(interval);
            elapsed += interval;
            if (!fIndeterminate)
                progressReport.UpdateProgress((elapsed * 100.0) / totalMsec);
        }
        progressReport.WorkCompleted();
        return true;
    }

    private void StartBackground5s(object sender, RoutedEventArgs e)
    {
        App.State.AddBackgroundWork(
            "background 5s test task",
            (progress) => BackgroundTestTask(progress, 5000));
    }

    private void StartBackground1m(object sender, RoutedEventArgs e)
    {
        App.State.AddBackgroundWork(
            "background 1m test task",
            (progress) => BackgroundTestTask(progress, 60000));
    }

    private void StartBackground10sIndet(object sender, RoutedEventArgs e)
    {
        App.State.AddBackgroundWork(
            "background 1m test task",
            (progress) => BackgroundTestTask(progress, -10000));
    }

    public void AddBackgroundWork<T>(string description, BackgroundWorkerWork<T> work)
    {
        m_mainBackgroundWorkers.AddWork(description, work);
    }

    private ProgressListDialog? m_backgroundProgressDialog;

    private void HandleSpinnerDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (m_backgroundProgressDialog == null)
        {
            m_backgroundProgressDialog = new ProgressListDialog();
            m_backgroundProgressDialog.ProgressReports.ItemsSource = m_mainBackgroundWorkers.Workers;
            m_backgroundProgressDialog.Owner = this;
            m_backgroundProgressDialog.Show();
            m_backgroundProgressDialog.Closing +=
                (_, _) =>
                {
                    m_backgroundProgressDialog = null;
                };
            m_backgroundProgressDialog.Show();
        }
    }

    private int m_lastSpinnerClick = 0;

    private void HandleSpinnerMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Timestamp - m_lastSpinnerClick < 200)
            HandleSpinnerDoubleClick(sender, e);

        m_lastSpinnerClick = e.Timestamp;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (App.State.Settings.ShowAsyncLogOnStart ?? false)
            ShowAsyncLog();
        if (App.State.Settings.ShowAppLogOnStart ?? false)
            ShowAppLog();
    }

    private void LaunchImport(object sender, RoutedEventArgs e)
    {
        MediaImporter.LaunchImporter(this);
        m_collection.BuildTimelineFromMediaCatalog();
    }

    private void JumpToDate(object sender, RoutedEventArgs e)
    {
        int line = m_collection.GetLineToScrollTo(m_collection.JumpDate);

        if (line != -1)
        {
            if (VisualTreeHelper.GetChild(Explorer.ExplorerBox, 0) is ScrollViewer scrollViewer)
            {
                double scrollTo = line;
                scrollViewer.ScrollToVerticalOffset(scrollTo);
            }

        }
    }

    private void ShowCacheInfo(object sender, RoutedEventArgs e)
    {
        CacheInfo cacheInfo = new CacheInfo();
        cacheInfo.Owner = this;
        cacheInfo.ShowDialog();
    }

    private void ChoosemMediaDateTimeline(object sender, RoutedEventArgs e) => m_collection.SetTimelineType(TimelineType.MediaDate);
    private void ChooseImportDateTimeline(object sender, RoutedEventArgs e) => m_collection.SetTimelineType(TimelineType.ImportDate);

    private void ChooseAscending(object sender, RoutedEventArgs e) => m_collection.SetTimelineOrder(TimelineOrder.Ascending);
    private void ChooseDescending(object sender, RoutedEventArgs e) => m_collection.SetTimelineOrder(TimelineOrder.Descending);

    private void DoChooseFilter(object sender, RoutedEventArgs e)
    {
        ChooseFilter filter = new ChooseFilter(m_collection.GetCurrentFilter());

        filter.Owner = this;

        if (filter.ShowDialog() is true)
        {
            FilterDefinition filterDef = filter.GetFilterDefinition();

            m_collection.SetFilter(filterDef);
        }
    }

    private void TestRenderImage(object sender, RoutedEventArgs e)
    {
        string outputFile = "c:/temp/test.png";
        // Create the Rectangle
        DrawingVisual visual = new DrawingVisual();
        using DrawingContext context = visual.RenderOpen();

        Point pt = new Point(10, 10);
        double size = 36;
        string text = "PSD FILE";
        Brush brush = Brushes.Yellow;
        DpiScale dpi = App.State.DpiScale;

        context.DrawRectangle(Brushes.White, null, new Rect(0, 0, 512, 512));
        context.DrawText(
            new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Calibri"),
                size,
                brush,
                dpi.PixelsPerDip),
            pt);

        context.DrawText(
            new FormattedText(
                "PSD FILE",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Calibri"),
                36,
                Brushes.Yellow,
                App.State.DpiScale.PixelsPerDip),
            new Point(10, 10));

        context.Close();

        RenderTargetBitmap bitmap = new RenderTargetBitmap(512, 512, 300, 300, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        // Save the image to a location on the disk.
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream fs = new FileStream(outputFile, FileMode.Create);
        encoder.Save(fs);
        fs.Close();
    }

    private void DoRebuildTimeline(object sender, RoutedEventArgs e)
    {
        m_collection.BuildTimelineFromMediaCatalog();
    }
}