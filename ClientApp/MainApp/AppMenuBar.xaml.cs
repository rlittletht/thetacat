using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Thetacat.BackupRestore.Backup;
using Thetacat.BackupRestore.Restore;
using Thetacat.Explorer;
using Thetacat.Import;
using Thetacat.Model.Caching;
using Thetacat.Repair;
using Thetacat.Types;
using Thetacat.UI;
using Thetacat.UI.Options;

namespace Thetacat.MainApp;

/// <summary>
/// Interaction logic for AppMenuBar.xaml
/// </summary>
public partial class AppMenuBar : UserControl
{
    private IMainCommands? m_commands;

    public AppMenuBar()
    {
        InitializeComponent();
    }

    /*----------------------------------------------------------------------------
        %%Function: AttachCommands
        %%Qualified: Thetacat.MainApp.AppMenuBar.AttachCommands
    ----------------------------------------------------------------------------*/
    public void AttachCommands(IMainCommands commands)
    {
        m_commands = commands;
    }

    #region Menu Handlers

    /*----------------------------------------------------------------------------
        %%Function: UploadItems
        %%Qualified: Thetacat.MainApp.AppMenuBar.UploadItems
    ----------------------------------------------------------------------------*/
    private void UploadItems(object sender, RoutedEventArgs e)
    {
        MediaImporter? import = null;

        try
        {
            import = new MediaImporter(App.State.Cache, MainWindow.ClientName);
        }
        catch (CatExceptionCanceled)
        {
            return;
        }

        import.UploadMedia();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoCacheItems
        %%Qualified: Thetacat.MainApp.AppMenuBar.DoCacheItems
    ----------------------------------------------------------------------------*/
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

    /*----------------------------------------------------------------------------
        %%Function: LaunchOptions
        %%Qualified: Thetacat.MainApp.AppMenuBar.LaunchOptions
    ----------------------------------------------------------------------------*/
    private void LaunchOptions(object sender, RoutedEventArgs e)
    {
        CatOptions options = new CatOptions();
        if (options.ShowDialog() ?? false)
        {
            options.SaveToSettings();
            App.State.Settings.WriteSettings();
        }

        m_commands?.RebuildProfileList();
    }

    /*----------------------------------------------------------------------------
        %%Function: LaunchMigration
        %%Qualified: Thetacat.MainApp.AppMenuBar.LaunchMigration
    ----------------------------------------------------------------------------*/
    private void LaunchMigration(object sender, RoutedEventArgs e)
    {
        Migration.Migration migration = new();
        migration.Owner = m_commands?.Window;
        migration.Show();
    }

    /*----------------------------------------------------------------------------
        %%Function: ManageMetatags
        %%Qualified: Thetacat.MainApp.AppMenuBar.ManageMetatags
    ----------------------------------------------------------------------------*/
    private void ManageMetatags(object sender, RoutedEventArgs e)
    {
        Metatags.ManageMetadata manage = new();
        manage.Owner = m_commands?.Window;
        manage.Show();
    }

    /*----------------------------------------------------------------------------
        %%Function: ToggleAsyncLog
        %%Qualified: Thetacat.MainApp.AppMenuBar.ToggleAsyncLog
    ----------------------------------------------------------------------------*/
    private void ToggleAsyncLog(object sender, RoutedEventArgs e)
    {
        m_commands?.ToggleAsyncLog();
    }

    /*----------------------------------------------------------------------------
        %%Function: ToggleAppLog
        %%Qualified: Thetacat.MainApp.AppMenuBar.ToggleAppLog
    ----------------------------------------------------------------------------*/
    private void ToggleAppLog(object sender, RoutedEventArgs e)
    {
        m_commands?.ToggleAppLog();
    }

    /*----------------------------------------------------------------------------
        %%Function: SelectExtraLargePreview
        %%Qualified: Thetacat.MainApp.AppMenuBar.SelectExtraLargePreview
    ----------------------------------------------------------------------------*/
    private void SelectExtraLargePreview(object sender, RoutedEventArgs e)
    {
        m_commands?.SetPreviewSize(ExplorerItemSize.ExtraLarge);
    }

    /*----------------------------------------------------------------------------
        %%Function: SelectLargePreview
        %%Qualified: Thetacat.MainApp.AppMenuBar.SelectLargePreview
    ----------------------------------------------------------------------------*/
    private void SelectLargePreview(object sender, RoutedEventArgs e)
    {
        m_commands?.SetPreviewSize(ExplorerItemSize.Large);
    }

    /*----------------------------------------------------------------------------
        %%Function: SelectMediumPreview
        %%Qualified: Thetacat.MainApp.AppMenuBar.SelectMediumPreview
    ----------------------------------------------------------------------------*/
    private void SelectMediumPreview(object sender, RoutedEventArgs e)
    {
        m_commands?.SetPreviewSize(ExplorerItemSize.Medium);
    }

    /*----------------------------------------------------------------------------
        %%Function: SelectSmallPreview
        %%Qualified: Thetacat.MainApp.AppMenuBar.SelectSmallPreview
    ----------------------------------------------------------------------------*/
    private void SelectSmallPreview(object sender, RoutedEventArgs e)
    {
        m_commands?.SetPreviewSize(ExplorerItemSize.Small);
    }

    /*----------------------------------------------------------------------------
        %%Function: LaunchImport
        %%Qualified: Thetacat.MainApp.AppMenuBar.LaunchImport
    ----------------------------------------------------------------------------*/
    private void LaunchImport(object sender, RoutedEventArgs e)
    {
        MediaImporter.LaunchImporter(m_commands?.Window!);
        m_commands?.RebuildTimeline();
    }

    /*----------------------------------------------------------------------------
        %%Function: LaunchRepather
        %%Qualified: Thetacat.MainApp.AppMenuBar.LaunchRepather
    ----------------------------------------------------------------------------*/
    private void LaunchRepather(object sender, RoutedEventArgs e)
    {
        Repather.LaunchRepather(m_commands?.Window!);
    }

    /*----------------------------------------------------------------------------
        %%Function: ShowCacheInfo
        %%Qualified: Thetacat.MainApp.AppMenuBar.ShowCacheInfo
    ----------------------------------------------------------------------------*/
    private void ShowCacheInfo(object sender, RoutedEventArgs e)
    {
        CacheInfo cacheInfo = new CacheInfo();
        cacheInfo.Owner = m_commands?.Window;
        cacheInfo.ShowDialog();
    }

    /*----------------------------------------------------------------------------
        %%Function: ChoosemMediaDateTimeline
        %%Qualified: Thetacat.MainApp.AppMenuBar.ChoosemMediaDateTimeline
    ----------------------------------------------------------------------------*/
    private void ChoosemMediaDateTimeline(object sender, RoutedEventArgs e)
    {
        m_commands?.SetTimelineType(TimelineType.MediaDate);
    }

    /*----------------------------------------------------------------------------
        %%Function: ChooseImportDateTimeline
        %%Qualified: Thetacat.MainApp.AppMenuBar.ChooseImportDateTimeline
    ----------------------------------------------------------------------------*/
    private void ChooseImportDateTimeline(object sender, RoutedEventArgs e)
    {
        m_commands?.SetTimelineType(TimelineType.ImportDate);
    }

    /*----------------------------------------------------------------------------
        %%Function: ChooseAscending
        %%Qualified: Thetacat.MainApp.AppMenuBar.ChooseAscending
    ----------------------------------------------------------------------------*/
    private void ChooseAscending(object sender, RoutedEventArgs e)
    {
        m_commands?.SetTimelineOrder(TimelineOrder.DateAscending);
    }

    /*----------------------------------------------------------------------------
        %%Function: ChooseDescending
        %%Qualified: Thetacat.MainApp.AppMenuBar.ChooseDescending
    ----------------------------------------------------------------------------*/
    private void ChooseDescending(object sender, RoutedEventArgs e)
    {
        m_commands?.SetTimelineOrder(TimelineOrder.DateDescending);
    }

    /*----------------------------------------------------------------------------
        %%Function: DoChooseFilter
        %%Qualified: Thetacat.MainApp.AppMenuBar.DoChooseFilter
    ----------------------------------------------------------------------------*/
    private void DoChooseFilter(object sender, RoutedEventArgs e)
    {
        ManageFilters filter = new ManageFilters(m_commands?.CurrentFilter);

        filter.Owner = m_commands?.Window;

        if (filter.ShowDialog() is true)
        {
            m_commands?.ChooseFilterOrCurrent(filter.GetFilter());
        }
        else
        {
            // still want to refresh the list
            m_commands?.ChooseFilterOrCurrent(m_commands?.CurrentFilter);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DoRestoreDatabase
        %%Qualified: Thetacat.MainApp.AppMenuBar.DoRestoreDatabase
    ----------------------------------------------------------------------------*/
    void DoRestoreDatabase(object sender, RoutedEventArgs e)
    {
        RestoreData restoreData = new RestoreData();
        restoreData.Owner = m_commands?.Window;

        restoreData.ShowDialog();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoBackupDatabase
        %%Qualified: Thetacat.MainApp.AppMenuBar.DoBackupDatabase
    ----------------------------------------------------------------------------*/
    private void DoBackupDatabase(object sender, RoutedEventArgs e)
    {
        ExportData exportData = new();

        exportData.Owner = m_commands?.Window;
        exportData.ShowDialog();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoEmptyTrash
        %%Qualified: Thetacat.MainApp.AppMenuBar.DoEmptyTrash
    ----------------------------------------------------------------------------*/
    private void DoEmptyTrash(object sender, RoutedEventArgs e)
    {
        m_commands?.EmptyTrash();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoPurgeCache
        %%Qualified: Thetacat.MainApp.AppMenuBar.DoPurgeCache
    ----------------------------------------------------------------------------*/
    private void DoPurgeCache(object sender, RoutedEventArgs e)
    {
        App.State.ImageCache.Purge();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoRepairWorkgroup
        %%Qualified: Thetacat.MainApp.AppMenuBar.DoRepairWorkgroup
    ----------------------------------------------------------------------------*/
    private void DoRepairWorkgroup(object sender, RoutedEventArgs e)
    {
        WorkgroupRepair.FixMissingWorkgroupEntries(App.State.Catalog);
    }

    /*----------------------------------------------------------------------------
        %%Function: ToggleExpandMediaStacks
        %%Qualified: Thetacat.MainApp.AppMenuBar.ToggleExpandMediaStacks
    ----------------------------------------------------------------------------*/
    private void ToggleExpandMediaStacks(object sender, RoutedEventArgs e)
    {
        m_commands?.MediaExplorerCollection.ToggleExpandMediaStacks();
    }

    /*----------------------------------------------------------------------------
        %%Function: ForceSyncMediaScan
        %%Qualified: Thetacat.MainApp.AppMenuBar.ForceSyncMediaScan
    ----------------------------------------------------------------------------*/
    private void ForceSyncMediaScan(object sender, RoutedEventArgs e)
    {
        CacheScanner scanner = new CacheScanner();

        scanner.ScanForLocalChanges(App.State.Cache, App.State.Md5Cache, ScanCacheType.Predictive);
        MessageBox.Show("Scan complete!");
    }

    /*----------------------------------------------------------------------------
        %%Function: DoRepairImportTables
        %%Qualified: Thetacat.MainApp.AppMenuBar.DoRepairImportTables
    ----------------------------------------------------------------------------*/
    private void DoRepairImportTables(object sender, RoutedEventArgs e)
    {
        MediaImporter import = new MediaImporter(App.State.ActiveProfile.CatalogID);

        import.RepairImportTables(App.State.Catalog, App.State.Cache);
    }
    #endregion

    #region Debug Tools

    /*----------------------------------------------------------------------------
        %%Function: BackgroundTestTask
        %%Qualified: Thetacat.MainApp.AppMenuBar.BackgroundTestTask
    ----------------------------------------------------------------------------*/
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

    /*----------------------------------------------------------------------------
        %%Function: StartBackground5s
        %%Qualified: Thetacat.MainApp.AppMenuBar.StartBackground5s
    ----------------------------------------------------------------------------*/
    private void StartBackground5s(object sender, RoutedEventArgs e)
    {
        App.State.AddBackgroundWork(
            "background 5s test task",
            (progress) => BackgroundTestTask(progress, 5000));
    }

    /*----------------------------------------------------------------------------
        %%Function: StartBackground5sWithDoneDialog
        %%Qualified: Thetacat.MainApp.AppMenuBar.StartBackground5sWithDoneDialog
    ----------------------------------------------------------------------------*/
    private void StartBackground5sWithDoneDialog(object sender, RoutedEventArgs e)
    {
        App.State.AddBackgroundWork(
            "background 5s test task",
            (progress) => BackgroundTestTask(progress, 5000),
            (worker) => System.Windows.Forms.MessageBox.Show($"Task done: {worker.Description}")
        );
    }

    /*----------------------------------------------------------------------------
        %%Function: StartBackground1m
        %%Qualified: Thetacat.MainApp.AppMenuBar.StartBackground1m
    ----------------------------------------------------------------------------*/
    private void StartBackground1m(object sender, RoutedEventArgs e)
    {
        App.State.AddBackgroundWork(
            "background 1m test task",
            (progress) => BackgroundTestTask(progress, 60000));
    }

    /*----------------------------------------------------------------------------
        %%Function: StartBackground10sIndet
        %%Qualified: Thetacat.MainApp.AppMenuBar.StartBackground10sIndet
    ----------------------------------------------------------------------------*/
    private void StartBackground10sIndet(object sender, RoutedEventArgs e)
    {
        App.State.AddBackgroundWork(
            "background 1m test task",
            (progress) => BackgroundTestTask(progress, -10000));
    }

    /*----------------------------------------------------------------------------
        %%Function: TestRenderImage
        %%Qualified: Thetacat.MainApp.AppMenuBar.TestRenderImage
    ----------------------------------------------------------------------------*/
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

    /*----------------------------------------------------------------------------
        %%Function: DoWork
        %%Qualified: Thetacat.MainApp.AppMenuBar.DoWork
    ----------------------------------------------------------------------------*/
    void DoWork(IProgressReport report)
    {
        for (int i = 0; i < 100; i++)
        {
            Thread.Sleep(50);
            report.UpdateProgress(i);
        }

        report.WorkCompleted();
    }

    /*----------------------------------------------------------------------------
        %%Function: TestProgressDialog
        %%Qualified: Thetacat.MainApp.AppMenuBar.TestProgressDialog
    ----------------------------------------------------------------------------*/
    private void TestProgressDialog(object sender, RoutedEventArgs e)
    {
        ProgressDialog.DoWorkWithProgress(DoWork, m_commands?.Window);
    }
    #endregion
}
