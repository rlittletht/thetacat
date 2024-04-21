using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using System.Windows.Forms;
using Microsoft.Windows.EventTracing.Power;
using Thetacat.BackupRestore.Backup;
using Thetacat.BackupRestore.Restore;
using Thetacat.Export;
using Thetacat.Metatags.Model;
using Thetacat.Filtering;
using Thetacat.Repair;
using Thetacat.ServiceClient;
using Thetacat.TcSettings;
using FlowDirection = System.Windows.FlowDirection;

namespace Thetacat;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static bool InUnitTest { get; set; } = false;
    private static CatLog? s_asyncLog;
    private static CatLog? s_appLog;
    private readonly BackgroundWorkers m_mainBackgroundWorkers;
    private readonly MainWindowModel m_model = new MainWindowModel();

    public static string ClientName = Environment.MachineName;
    public static CatLog _AsyncLog => s_asyncLog ?? throw new CatExceptionInitializationFailure("async log not initialized");
    public static CatLog _AppLog => s_appLog ?? throw new CatExceptionInitializationFailure("appLog not initialized");

    public MainWindow()
    {
        InitializeComponent();
        InitializeThetacat();

        // adding our listener here so we setup our filters, etc., for the active profile correctly

        App.State.ProfileChanged += OnProfileChanged;
        App.OnMainWindowCreated();
        App.State.RegisterWindowPlace(this, "MainWindow");
        m_model.PropertyChanged += MainWindowPropertyChanged;
        RebuildProfileList();

        Explorer.SetExplorerItemSize(App.State.ActiveProfile.ExplorerItemSize ?? ExplorerItemSize.Medium);

        // we have to load the catalog AND the pending upload list
        // we also have to confirm that all the items int he pending
        // upload list still exist in the catalog, and if they don't
        // (or if they are marked as active in the catalog, which means
        // they are already uploaded), then remove them from the import
        // list

        LocalServiceClient.LogService = LogForApp;
        DataContext = m_model;

        m_mainBackgroundWorkers = new BackgroundWorkers(BackgroundActivity.Start, BackgroundActivity.Stop);

        App.State.DpiScale = VisualTreeHelper.GetDpi(this);
        App.State.Catalog.OnItemDirtied += SetCollectionDirtyState;
        App.State.MetatagSchema.OnItemDirtied += SetSchemaDirtyState;
    }

    private void MainWindowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "CurrentProfile" && m_model.CurrentProfile?.Name != null)
        {
            App.State.ChangeProfile(m_model.CurrentProfile.Name);
        }
    }

    void RebuildProfileList()
    {
        m_model.AvailableProfiles.Clear();
        foreach (Profile profile in App.State.Settings.Profiles.Values)
        {
            m_model.AvailableProfiles.Add(profile);
        }

        m_model.CurrentProfile = App.State.ActiveProfile;
    }

    void RebuildFilterList()
    {
        m_model.AvailableFilters.Clear();
        foreach (string filterName in App.State.ActiveProfile.Filters.Keys.ToImmutableSortedSet())
        {
            m_model.AvailableFilters.Add(App.State.ActiveProfile.Filters[filterName]);
        }
    }

    void InitializeThetacat()
    {
        App.State.SetupLogging(CloseAsyncLog, CloseAppLog);
        App.State.SetupBackgroundWorkers(AddBackgroundWork);

        s_asyncLog = new CatLog(EventType.Information);
        s_appLog = new CatLog(EventType.Information);
    }

    void OnClosing(object sender, EventArgs e)
    {
        if (m_model.IsDirty)
        {
            DialogResult result = MessageBox.Show("The main catalog is dirty. Do you want to save changes?", "Save Changes", MessageBoxButtons.YesNo);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                App.State.Catalog.PushPendingChanges(App.State.ActiveProfile.CatalogID);
                App.State.MetatagSchema.UpdateServer(App.State.ActiveProfile.CatalogID);
            }
        }

        App.State.Derivatives.CommitDerivatives();
        App.State.Derivatives.Close();
        App.State.ActiveProfile.ShowAsyncLogOnStart = m_asyncLogMonitor != null;
        App.State.ActiveProfile.ShowAppLogOnStart = m_appLogMonitor != null;

        // close all of our windows and collections (includes terminating background listeners)
        m_model.ExplorerCollection.Close();
        Explorer.Close();
        App.State.PreviewImageCache.Close();
        App.State.ImageCache.Close();
        App.State.Md5Cache.Close();
        App.State.ClientDatabase?.Close();

        if (m_asyncLogMonitor != null)
            CloseAsyncLog(false);

        if (m_appLogMonitor != null)
            CloseAppLog(false);

        App.State.Settings.WriteSettings();
    }

#region Logging

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

#endregion

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
    
    private async void ConnectToDatabase(object sender, RoutedEventArgs e)
    {
        LogForApp(EventType.Information, "Beginning read catalog");
        MicroTimer timer = new MicroTimer();

        List<Guid> deletedItems = ServiceInterop.GetDeletedMediaItems(App.State.ActiveProfile.CatalogID);

        App.State.EnsureDeletedItemsCollateralRemoved(deletedItems);
        await App.State.Catalog.ReadFullCatalogFromServer(App.State.ActiveProfile.CatalogID, App.State.MetatagSchema);
        // good time to refresh the MRU now that we loaded the catalog and the schema
        App.State.MetatagMRU.Set(App.State.ActiveProfile.MetatagMru);
        SetCollectionDirtyState(null, new DirtyItemEventArgs<bool>(false));

        LogForApp(EventType.Information, $"Done after ReadFullCatalogFromServer. {timer.Elapsed()}");
        timer.Reset();
        timer.Start();

        List<MediaExplorerItem> explorerItems = new();

        m_model.ExplorerCollection.AdjustPanelItemWidth(Explorer.Model.PanelItemWidth);
        m_model.ExplorerCollection.AdjustPanelItemHeight(Explorer.Model.PanelItemHeight);
        m_model.ExplorerCollection.AdjustExplorerWidth(Explorer.ExplorerBox.ActualWidth);
        m_model.ExplorerCollection.AdjustExplorerHeight(Explorer.ExplorerBox.ActualHeight);
        m_model.ExplorerCollection.UpdateItemsPerLine();

        LogForApp(EventType.Information, $"Done reading catalog. {timer.Elapsed()}");
        timer.Reset();
        timer.Start();

        TimelineType timelineType = m_model.ExplorerCollection.TimelineType;
        if (timelineType.Equals(TimelineType.None))
        {
            if (App.State.ActiveProfile.TimelineType != null)
                timelineType = App.State.ActiveProfile.TimelineType;

            if (timelineType.Equals(TimelineType.None))
                timelineType = TimelineType.MediaDate;
        }

        TimelineOrder timelineOrder = m_model.ExplorerCollection.TimelineOrder;
        if (timelineOrder.Equals(TimelineOrder.None))
        {
            if (App.State.ActiveProfile.TimelineOrder != null)
                timelineOrder = App.State.ActiveProfile.TimelineOrder;

            if (timelineOrder.Equals(TimelineOrder.None))
                timelineOrder = TimelineOrder.Ascending;
        }

        m_model.ExplorerCollection.ResetTimeline();
        m_model.ExplorerCollection.SetTimelineTypeAndOrder(timelineType, timelineOrder);

        LogForApp(EventType.Information, $"Done building timeline. {timer.Elapsed()}");

        timer.Reset();
        timer.Start();
        LogForApp(EventType.Information, "Beginning reset content");
        Explorer.ResetContent(m_model.ExplorerCollection); // explorerItems);

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

        RebuildProfileList();
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

//    private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
//    {
//        MediaItem? item = CatalogView.SelectedItem as MediaItem;
//
//        if (item != null)
//        {
//            MediaItemDetails details = new MediaItemDetails(item);
//
//            details.Owner = this;
//            details.ShowDialog();
//        }
//    }

    private void CommitPendingChanges(object sender, RoutedEventArgs e)
    {
        App.State.Catalog.PushPendingChanges(App.State.ActiveProfile.CatalogID);
        App.State.MetatagSchema.UpdateServer(App.State.ActiveProfile.CatalogID);
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
        bool fIndeterminate = false;

        if (totalMsec < 0)
        {
            progressReport.SetIndeterminate();
            totalMsec = -totalMsec;
            fIndeterminate = true;
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

    private void StartBackground5sWithDoneDialog(object sender, RoutedEventArgs e)
    {
        App.State.AddBackgroundWork(
            "background 5s test task",
            (progress) => BackgroundTestTask(progress, 5000),
            (worker) => MessageBox.Show($"Task done: {worker.Description}")
            );
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

    public void AddBackgroundWork<T>(string description, BackgroundWorkerWork<T> work, OnWorkCompletedDelegate? onWorkCompleted = null)
    {
        m_mainBackgroundWorkers.AddWork(description, work, null, onWorkCompleted);
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
                (_, _) => { m_backgroundProgressDialog = null; };
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
        if (App.State.ActiveProfile.ShowAsyncLogOnStart ?? false)
            ShowAsyncLog();
        if (App.State.ActiveProfile.ShowAppLogOnStart ?? false)
            ShowAppLog();
    }

    private void LaunchImport(object sender, RoutedEventArgs e)
    {
        MediaImporter.LaunchImporter(this);
        m_model.ExplorerCollection.BuildTimelineFromMediaCatalog();
    }

    private void LaunchRepather(object sender, RoutedEventArgs e)
    {
        Repather.LaunchRepather(this);
    }

    private void JumpToDate(object sender, RoutedEventArgs e)
    {
        int line = m_model.ExplorerCollection.GetLineToScrollTo(m_model.ExplorerCollection.JumpDate);

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

    private void ChoosemMediaDateTimeline(object sender, RoutedEventArgs e) => m_model.ExplorerCollection.SetTimelineType(TimelineType.MediaDate);
    private void ChooseImportDateTimeline(object sender, RoutedEventArgs e) => m_model.ExplorerCollection.SetTimelineType(TimelineType.ImportDate);

    private void ChooseAscending(object sender, RoutedEventArgs e) => m_model.ExplorerCollection.SetTimelineOrder(TimelineOrder.Ascending);
    private void ChooseDescending(object sender, RoutedEventArgs e) => m_model.ExplorerCollection.SetTimelineOrder(TimelineOrder.Descending);

    private void DoChooseFilter(object sender, RoutedEventArgs e)
    {
        ManageFilters filter = new ManageFilters(m_model.ExplorerCollection.Filter);

        filter.Owner = this;
        string? filterName = null;

        if (filter.ShowDialog() is true)
        {
            filterName = filter.GetFilterName() ?? m_model.ExplorerCollection.Filter?.FilterName;
        }

        if (filterName == null)
        {
            filterName = m_model.ExplorerCollection.Filter?.FilterName;
            m_model.ExplorerCollection.DontRebuildTimelineOnFilterChange = true; // we are just going to set it to the same filter
        }
        // regardless, rebuild from the settings (they might night be applying a new filter, but they
        // might have redefined some filters)
        RebuildFilterList();

        if (filterName != null)
            m_model.ExplorerCollection.Filter = App.State.ActiveProfile.Filters[filterName];

        m_model.ExplorerCollection.DontRebuildTimelineOnFilterChange = false; // reset it (regardless of whether we set it)
    }

    private void OnProfileChanged(object? sender, ProfileChangedEventArgs e)
    {
        RebuildFilterList();
        m_model.ExplorerCollection.Clear();
        if (App.State?.ActiveProfile?.DefaultFilterName != null)
        {
            if (App.State.ActiveProfile.Filters.TryGetValue(App.State.ActiveProfile.DefaultFilterName, out FilterDefinition? filter))
                m_model.ExplorerCollection.SetFilter(filter);
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
        m_model.ExplorerCollection.BuildTimelineFromMediaCatalog();
    }

    public void SetCollectionDirtyState(object? sender,DirtyItemEventArgs<bool> e)
    {
        m_model.IsExplorerCollectionDirty = e.Item;
    }

    public void SetSchemaDirtyState(object? sender, DirtyItemEventArgs<bool> e)
    {
        m_model.IsSchemaDirty = e.Item;
    }

    void DoRestoreDatabase(object sender, RoutedEventArgs e)
    {
        RestoreData restoreData = new RestoreData();
        restoreData.Owner = this;

        restoreData.ShowDialog();
    }

    private void DoBackupDatabase(object sender, RoutedEventArgs e)
    {
        ExportData exportData = new();

        exportData.Owner = this;
        exportData.ShowDialog();
    }

    private void DoEmptyTrash(object sender, RoutedEventArgs e)
    {
        // get a collection of all the items marked for the trash
        FilterDefinition trashFilter = new FilterDefinition("", "", $"[{BuiltinTags.s_IsTrashItemID:B}] == '$true'");
        
        if (m_model.ExplorerCollection.FDoDeleteItems(App.State.Catalog.GetFilteredMediaItems(trashFilter)))
            m_model.ExplorerCollection.BuildTimelineFromMediaCatalog();
    }

    private void DoPurgeCache(object sender, RoutedEventArgs e)
    {
        App.State.ImageCache.Purge();
    }

    private void DoRepairWorkgroup(object sender, RoutedEventArgs e)
    {
        WorkgroupRepair.FixMissingWorkgroupEntries(App.State.Catalog);
    }
}
