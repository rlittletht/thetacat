using System;
using System.Windows;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.Secrets;
using Thetacat.ServiceClient;
using Thetacat.Util;

namespace Thetacat.Types;

public class AppState : IAppState
{
    public delegate void CloseLogMonitorDelegate(bool skipClose);
    public delegate void AddBackgroundWorkDelegate(string description, BackgroundWorkerWork work);
    private CloseLogMonitorDelegate? m_closeAsyncLog;
    private CloseLogMonitorDelegate? m_closeAppLog;
    private AddBackgroundWorkDelegate? m_addBackgroundWork;

    public TcSettings.TcSettings Settings { get; }
    public MetatagSchema MetatagSchema { get; }
    public ICache Cache { get; private set; }
    public ICatalog Catalog { get; private set; }
    public void CloseAsyncLogMonitor(bool skipClose) => m_closeAsyncLog?.Invoke(skipClose);
    public void CloseAppLogMonitor(bool skipClose) => m_closeAppLog?.Invoke(skipClose);
    public string AzureStorageAccount => App.State.Settings.AzureStorageAccount ?? throw new CatExceptionInitializationFailure("no azure storage account set");
    public string StorageContainer => App.State.Settings.StorageContainer ?? throw new CatExceptionInitializationFailure("no storage container set");

    public void SetupLogging(CloseLogMonitorDelegate closeAsyncLogDelegate, CloseLogMonitorDelegate closeAppLogDelegate)
    {
        m_closeAsyncLog = closeAsyncLogDelegate;
        m_closeAppLog = closeAppLogDelegate;
    }

    public void SetupBackgroundWorkers(AddBackgroundWorkDelegate addWorkDelegate)
    {
        m_addBackgroundWork = addWorkDelegate;;
    }

    public void AddBackgroundWork(string description, BackgroundWorkerWork work)
    {
        if (m_addBackgroundWork == null)
            throw new CatExceptionInitializationFailure("no background work collection available");

        m_addBackgroundWork(description, work);
    }

    public void RefreshMetatagSchema()
    {
        MetatagSchema.ReplaceFromService(ServiceInterop.GetMetatagSchema());
    }

    public void OverrideCache(ICache cache)
    {
        Cache = cache;
    }

    public void OverrideCatalog(ICatalog catalog)
    {
        Catalog = catalog;
    }

    public AppState()
    {
        Settings = new TcSettings.TcSettings();
        AppSecrets.MasterSqlConnectionString = Settings.SqlConnection ?? String.Empty;

        Catalog = new Catalog();
        MetatagSchema = new MetatagSchema();
        Cache = new Cache(Settings);
        m_closeAsyncLog = null;
        m_closeAppLog = null;
        m_addBackgroundWork = null;
    }

    public void RegisterWindowPlace(Window window, string key)
    {
        ((App)Application.Current).WindowPlace.Register(window, key);
    }
}
