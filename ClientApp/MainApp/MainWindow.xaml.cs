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
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Thetacat.BackupRestore.Backup;
using Thetacat.BackupRestore.Restore;
using Thetacat.Export;
using Thetacat.Metatags.Model;
using Thetacat.Filtering;
using Thetacat.Import.UI;
using Thetacat.Model;
using Thetacat.Repair;
using Thetacat.ServiceClient;
using Thetacat.TcSettings;
using FlowDirection = System.Windows.FlowDirection;
using Thetacat.Model.Caching;

namespace Thetacat.MainApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IMainCommands
{
    public static bool InUnitTest { get; set; } = false;
    private static CatLog? s_asyncLog;
    private static CatLog? s_appLog;
    private readonly BackgroundWorkers m_mainBackgroundWorkers;
    private readonly MainWindowModel m_model = new MainWindowModel();

    private AsyncLogMonitor? m_asyncLogMonitor;
    private AppLogMonitor? m_appLogMonitor;

    public static string ClientName = Environment.MachineName;
    public static CatLog _AsyncLog => s_asyncLog ?? throw new CatExceptionInitializationFailure("async log not initialized");
    public static CatLog _AppLog => s_appLog ?? throw new CatExceptionInitializationFailure("appLog not initialized");

    public MediaExplorerCollection MediaExplorerCollection => m_model.ExplorerCollection;

    public MainWindow()
    {
        InitializeComponent();
        InitializeThetacat();

        // adding our listener here so we setup our filters, etc., for the active profile correctly

        App.State.ProfileChanged += OnProfileChanged;
        App.OnMainWindowCreated();
        App.State.RegisterWindowPlace(this, "MainWindow");
        m_model.PropertyChanged += MainWindowPropertyChanged;
        Activated += OnMainWindowActivated;
        RebuildProfileList();

        m_model.ItemSize = App.State.ActiveProfile.ExplorerItemSize ?? ExplorerItemSize.Medium;
        Explorer.SetExplorerItemSize(m_model.ItemSize);
        m_model.ExplorerCollection.SetExpandMediaStacks(App.State.ActiveProfile.ExpandMediaStacksInExplorers ?? false);

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
        AppMenuBar.AttachCommands(this);
    }

    /*----------------------------------------------------------------------------
        %%Function: InitializeThetacat
        %%Qualified: Thetacat.MainApp.MainWindow.InitializeThetacat
    ----------------------------------------------------------------------------*/
    void InitializeThetacat()
    {
        App.State.SetupLogging(CloseAsyncLog, CloseAppLog);
        App.State.SetupBackgroundWorkers(AddBackgroundWork);

        s_asyncLog = new CatLog(EventType.Information);
        s_appLog = new CatLog(EventType.Information);
    }

    #region Event Handlers

    /*----------------------------------------------------------------------------
        %%Function: OnClosing
        %%Qualified: Thetacat.MainApp.MainWindow.OnClosing
    ----------------------------------------------------------------------------*/
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

    /*----------------------------------------------------------------------------
        %%Function: OnMainWindowActivated
        %%Qualified: Thetacat.MainApp.MainWindow.OnMainWindowActivated
    ----------------------------------------------------------------------------*/
    private void OnMainWindowActivated(object? sender, EventArgs e)
    {
        Explorer.OnParentWindowActivated();
    }

    /*----------------------------------------------------------------------------
        %%Function: OnLoaded
        %%Qualified: Thetacat.MainApp.MainWindow.OnLoaded
    ----------------------------------------------------------------------------*/
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (App.State.ActiveProfile.ShowAsyncLogOnStart ?? false)
            ShowAsyncLog();
        if (App.State.ActiveProfile.ShowAppLogOnStart ?? false)
            ShowAppLog();
    }

    /*----------------------------------------------------------------------------
        %%Function: MainWindowPropertyChanged
        %%Qualified: Thetacat.MainApp.MainWindow.MainWindowPropertyChanged
    ----------------------------------------------------------------------------*/
    private void MainWindowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "CurrentProfile" && m_model.CurrentProfile?.Name != null)
        {
            App.State.ChangeProfile(m_model.CurrentProfile.Name);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: OnProfileChanged
        %%Qualified: Thetacat.MainApp.MainWindow.OnProfileChanged
    ----------------------------------------------------------------------------*/
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

    /*----------------------------------------------------------------------------
        %%Function: SetCollectionDirtyState
        %%Qualified: Thetacat.MainApp.MainWindow.SetCollectionDirtyState
    ----------------------------------------------------------------------------*/
    public void SetCollectionDirtyState(object? sender, DirtyItemEventArgs<bool> e)
    {
        m_model.IsExplorerCollectionDirty = e.Item;
    }

    /*----------------------------------------------------------------------------
        %%Function: SetSchemaDirtyState
        %%Qualified: Thetacat.MainApp.MainWindow.SetSchemaDirtyState
    ----------------------------------------------------------------------------*/
    public void SetSchemaDirtyState(object? sender, DirtyItemEventArgs<bool> e)
    {
        m_model.IsSchemaDirty = e.Item;
    }

#endregion

#region Public Commands

    public Window Window => (Window)this;
    public FilterDefinition? CurrentFilterDefinition => m_model.ExplorerCollection.Filter;
    public MediaExplorer MediaExplorer => Explorer;

    /*----------------------------------------------------------------------------
        %%Function: ShowAsyncLog
        %%Qualified: Thetacat.MainApp.MainWindow.ShowAsyncLog
    ----------------------------------------------------------------------------*/
    void ShowAsyncLog()
    {
        if (m_asyncLogMonitor != null)
            return;

        m_asyncLogMonitor = new AsyncLogMonitor();
        m_asyncLogMonitor.Owner = this;
        m_asyncLogMonitor.Show();
    }

    /*----------------------------------------------------------------------------
        %%Function: CloseAsyncLog
        %%Qualified: Thetacat.MainApp.MainWindow.CloseAsyncLog
    ----------------------------------------------------------------------------*/
    void CloseAsyncLog(bool skipClose)
    {
        if (!skipClose)
            m_asyncLogMonitor?.Close();
        m_asyncLogMonitor = null;
    }

    /*----------------------------------------------------------------------------
        %%Function: ShowAppLog
        %%Qualified: Thetacat.MainApp.MainWindow.ShowAppLog
    ----------------------------------------------------------------------------*/
    void ShowAppLog()
    {
        if (m_appLogMonitor != null)
            return;

        m_appLogMonitor = new AppLogMonitor();
        m_appLogMonitor.Owner = this;
        m_appLogMonitor.Show();
    }

    /*----------------------------------------------------------------------------
        %%Function: CloseAppLog
        %%Qualified: Thetacat.MainApp.MainWindow.CloseAppLog
    ----------------------------------------------------------------------------*/
    void CloseAppLog(bool skipClose)
    {
        if (!skipClose)
            m_appLogMonitor?.Close();
        m_appLogMonitor = null;
    }

    public void ToggleAppLog()
    {
        if (m_appLogMonitor != null)
            CloseAppLog(false);
        else
            ShowAppLog();
    }

    /*----------------------------------------------------------------------------
        %%Function: ToggleAsyncLog
        %%Qualified: Thetacat.MainApp.MainWindow.ToggleAsyncLog
    ----------------------------------------------------------------------------*/
    public void ToggleAsyncLog()
    {
        if (m_asyncLogMonitor != null)
            CloseAsyncLog(false);
        else
            ShowAsyncLog();
    }


    /*----------------------------------------------------------------------------
        %%Function: RebuildProfileList
        %%Qualified: Thetacat.MainApp.MainWindow.RebuildProfileList

        Rebuild the current list of profiles
    ----------------------------------------------------------------------------*/
    public void RebuildProfileList()
    {
        m_model.AvailableProfiles.Clear();
        foreach (Profile profile in App.State.Settings.Profiles.Values)
        {
            m_model.AvailableProfiles.Add(profile);
        }

        m_model.CurrentProfile = App.State.ActiveProfile;
    }

    /*----------------------------------------------------------------------------
        %%Function: RebuildFilterList
        %%Qualified: Thetacat.MainApp.MainWindow.RebuildFilterList

        Rebuild the filter list
    ----------------------------------------------------------------------------*/
    void RebuildFilterList()
    {
        m_model.AvailableFilters.Clear();
        foreach (string filterName in App.State.ActiveProfile.Filters.Keys.ToImmutableSortedSet())
        {
            m_model.AvailableFilters.Add(App.State.ActiveProfile.Filters[filterName]);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: SetPreviewSize
        %%Qualified: Thetacat.MainApp.MainWindow.SetPreviewSize
    ----------------------------------------------------------------------------*/
    public void SetPreviewSize(ExplorerItemSize size)
    {
        m_model.ItemSize = size;
        Explorer.SetExplorerItemSize(size);
    }

    /*----------------------------------------------------------------------------
        %%Function: AddBackgroundWork
        %%Qualified: Thetacat.MainApp.MainWindow.AddBackgroundWork<T>
    ----------------------------------------------------------------------------*/
    public void AddBackgroundWork<T>(string description, BackgroundWorkerWork<T> work, OnWorkCompletedDelegate? onWorkCompleted = null)
    {
        m_mainBackgroundWorkers.AddWork(description, work, null, onWorkCompleted);
    }

    /*----------------------------------------------------------------------------
    %%Function: RebuildTimeline
    %%Qualified: Thetacat.MainApp.MainWindow.RebuildTimeline
----------------------------------------------------------------------------*/
    public void RebuildTimeline()
    {
        m_model.ExplorerCollection.BuildTimelineFromMediaCatalog();
    }

    public void SetTimelineType(TimelineType type)
    {
        m_model.ExplorerCollection.SetTimelineType(type);
    }

    public void SetTimelineOrder(TimelineOrder order)
    {
        m_model.ExplorerCollection.SetTimelineOrder(order);
    }

    public void ChooseFilterOrCurrent(string? filterName)
    {
        filterName = filterName ?? m_model.ExplorerCollection.Filter?.FilterName;

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

    /*----------------------------------------------------------------------------
        %%Function: EmptyTrash
        %%Qualified: Thetacat.MainApp.MainWindow.EmptyTrash
    ----------------------------------------------------------------------------*/
    public void EmptyTrash()
    {
        // get a collection of all the items marked for the trash
        FilterDefinition trashFilter = new FilterDefinition("", "", $"[{BuiltinTags.s_IsTrashItemID:B}] == '$true'");

        if (m_model.ExplorerCollection.FDoDeleteItems(App.State.Catalog.GetFilteredMediaItems(trashFilter)))
            m_model.ExplorerCollection.BuildTimelineFromMediaCatalog();
    }

#endregion

#region Logging

    public static void LogForAsync(EventType eventType, string log, string? details = null, Guid? correlationId = null)
    {
        if (InUnitTest)
            return;

        if (_AsyncLog.ShouldLog(eventType))
        {
            ILogEntry entry = new LogEntry(eventType, log, correlationId?.ToString() ?? "", details);

            _AsyncLog.Log(entry);
            _AppLog.Log(entry);
        }
    }

    public static void LogForApp(EventType eventType, string log, string? details = null, Guid? correlationId = null)
    {
        if (InUnitTest)
            return;

        if (_AppLog.ShouldLog(eventType))
        {
            ILogEntry entry = new LogEntry(eventType, log, correlationId?.ToString() ?? "", details);
            _AppLog.Log(entry);
        }
    }

#endregion


    private ProgressListDialog? m_backgroundProgressDialog;

    #region UI Commands

    /*----------------------------------------------------------------------------
        %%Function: ConnectToDatabase
        %%Qualified: Thetacat.MainApp.MainWindow.ConnectToDatabase
    ----------------------------------------------------------------------------*/
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
                timelineOrder = TimelineOrder.DateAscending;
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

    /*----------------------------------------------------------------------------
        %%Function: HandleSpinnerDoubleClick
        %%Qualified: Thetacat.MainApp.MainWindow.HandleSpinnerDoubleClick
    ----------------------------------------------------------------------------*/
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

    /*----------------------------------------------------------------------------
        %%Function: DoToggleMetatagPanel
        %%Qualified: Thetacat.MainApp.MainWindow.DoToggleMetatagPanel
    ----------------------------------------------------------------------------*/
    private void DoToggleMetatagPanel(object sender, RoutedEventArgs e)
    {
        Explorer.ToggleMetatagPanel();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoRebuildTimeline
        %%Qualified: Thetacat.MainApp.MainWindow.DoRebuildTimeline
    ----------------------------------------------------------------------------*/
    private void DoRebuildTimeline(object sender, RoutedEventArgs e)
    {
        RebuildTimeline();
    }

    /*----------------------------------------------------------------------------
        %%Function: CommitPendingChanges
        %%Qualified: Thetacat.MainApp.MainWindow.CommitPendingChanges
    ----------------------------------------------------------------------------*/
    private void CommitPendingChanges(object sender, RoutedEventArgs e)
    {
        App.State.Catalog.PushPendingChanges(App.State.ActiveProfile.CatalogID);
        App.State.MetatagSchema.UpdateServer(App.State.ActiveProfile.CatalogID);
    }

    private int m_lastSpinnerClick = 0;

    /*----------------------------------------------------------------------------
        %%Function: HandleSpinnerMouseDown
        %%Qualified: Thetacat.MainApp.MainWindow.HandleSpinnerMouseDown
    ----------------------------------------------------------------------------*/
    private void HandleSpinnerMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Timestamp - m_lastSpinnerClick < 200)
            HandleSpinnerDoubleClick(sender, e);

        m_lastSpinnerClick = e.Timestamp;
    }

    /*----------------------------------------------------------------------------
        %%Function: JumpToDate
        %%Qualified: Thetacat.MainApp.MainWindow.JumpToDate
    ----------------------------------------------------------------------------*/
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

#endregion
}
