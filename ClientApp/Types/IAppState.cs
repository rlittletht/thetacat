using System.Windows;
using Thetacat.Explorer;
using Thetacat.Metatags.Model;
using Thetacat.Model.Client;
using Thetacat.Model.ImageCaching;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Util;
using static Thetacat.Types.AppState;

namespace Thetacat.Types;

public interface IAppState
{
    ImageCache PreviewImageCache { get; }
    ImageCache ImageCache { get; }
    ICatalog Catalog { get; }
    TcSettings.TcSettings Settings { get; }
    TcSettings.Profile ActiveProfile { get; }
    MetatagSchema MetatagSchema { get; }
    ICache Cache { get; }
    ClientDatabase ClientDatabase { get; }
    Md5Cache Md5Cache { get; }
    Derivatives Derivatives { get; }
    MetatagMRU MetatagMRU { get; }
    public string AzureStorageAccount {get;}
    public string StorageContainer { get; }
    public DpiScale DpiScale { get; set; }

    void RegisterWindowPlace(Window window, string key);
    void RefreshMetatagSchema();
    void CloseAsyncLogMonitor(bool skipClose);
    void CloseAppLogMonitor(bool skipClose);
    public void AddBackgroundWork(string description, BackgroundWorkerWork<bool> work);
    public void SetupLogging(CloseLogMonitorDelegate closeAsyncLogDelegate, CloseLogMonitorDelegate closeAppLogDelegate);
    public void SetupBackgroundWorkers(AddBackgroundWorkDelegate addWorkDelegate);
    public SetDirtyStateDelegate SetCollectionDirtyState { get; set; }
    public SetDirtyStateDelegate SetSchemaDirtyState { get; set; }
    public void ChangeProfile(string profileName);
    public void PushTemporarySqlConnection(string connectionString);
    public void PopTemporarySqlConnection();
}
