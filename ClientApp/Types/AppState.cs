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
    private readonly CloseLogMonitorDelegate? m_closeAsyncLog;
    private readonly CloseLogMonitorDelegate? m_closeAppLog;
    private readonly AddBackgroundWorkDelegate m_addBackgroundWork;

    public TcSettings.TcSettings Settings { get; }
    public MetatagSchema MetatagSchema { get; }
    public ICache Cache { get; private set; }
    public ICatalog Catalog { get; private set; }
    public void CloseAsyncLogMonitor(bool skipClose) => m_closeAsyncLog?.Invoke(skipClose);
    public void CloseAppLogMonitor(bool skipClose) => m_closeAppLog?.Invoke(skipClose);
    public string AzureStorageAccount => MainWindow._AppState.Settings.AzureStorageAccount ?? throw new CatExceptionInitializationFailure("no azure storage account set");
    public string StorageContainer => MainWindow._AppState.Settings.StorageContainer ?? throw new CatExceptionInitializationFailure("no storage container set");
    public void AddBackgroundWork(string description, BackgroundWorkerWork work) => m_addBackgroundWork(description, work);

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

    public AppState(CloseLogMonitorDelegate closeAsyncLogDelegate, CloseLogMonitorDelegate closeAppLogDelegate, AddBackgroundWorkDelegate addBackgroundWork)
    {
        Settings = new TcSettings.TcSettings();
        AppSecrets.MasterSqlConnectionString = Settings.SqlConnection ?? String.Empty;

        Catalog = new Catalog();
        MetatagSchema = new MetatagSchema();
        Cache = new Cache(Settings);
        m_closeAsyncLog = closeAsyncLogDelegate;
        m_closeAppLog = closeAppLogDelegate;
        m_addBackgroundWork = addBackgroundWork;
    }

    public void RegisterWindowPlace(Window window, string key)
    {
        ((App)Application.Current).WindowPlace.Register(window, key);
    }
}
