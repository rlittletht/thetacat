using System;
using System.Collections.Generic;
using System.Windows;
using Thetacat.Explorer;
using Thetacat.Filtering;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Client;
using Thetacat.Model.ImageCaching;
using Thetacat.Model.Md5Caching;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Util;
using static Thetacat.Types.AppState;

namespace Thetacat.Types;

public interface IAppState
{
    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;
    ImageCache PreviewImageCache { get; }
    ImageCache ImageCache { get; }
    ICatalog Catalog { get; }
    TcSettings.TcSettings Settings { get; }
    TcSettings.Profile ActiveProfile { get; }
    WindowManager WindowManager { get; }

    MetatagSchema MetatagSchema { get; }
    ICache Cache { get; }
    ClientDatabase? ClientDatabase { get; }
    IWorkgroup? Workgroup { get; }
    Md5Cache Md5Cache { get; }
    Derivatives Derivatives { get; }
    MetatagMRU MetatagMRU { get; }
    Filters Filters { get; }
    public string AzureStorageAccount {get;}
    public string StorageContainer { get; }
    public DpiScale DpiScale { get; set; }
    
    void RegisterWindowPlace(Window window, string key);
    void RefreshMetatagSchema(Guid catalogID);
    void CloseAsyncLogMonitor(bool skipClose);
    void CloseAppLogMonitor(bool skipClose);
    public void AddBackgroundWork(string description, BackgroundWorkerWork<bool> work, OnWorkCompletedDelegate? onWorkCompleted = null);
    public void SetupLogging(CloseLogMonitorDelegate closeAsyncLogDelegate, CloseLogMonitorDelegate closeAppLogDelegate);
    public void SetupBackgroundWorkers(AddBackgroundWorkDelegate addWorkDelegate);
    public void ChangeProfile(string profileName);
    public void PushTemporarySqlConnection(string connectionString);
    public void PopTemporarySqlConnection();

    public void EnsureDeletedItemCollateralRemoved(Guid id);
    public void EnsureDeletedItemsCollateralRemoved(List<Guid> items);
    public string GetMD5ForItem(Guid id);
}
