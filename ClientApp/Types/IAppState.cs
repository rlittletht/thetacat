using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Thetacat.Metatags.Model;
using Thetacat.Model;
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
    MetatagSchema MetatagSchema { get; }
    ICache Cache { get; }
    ClientDatabase ClientDatabase { get; }
    Md5Cache Md5Cache { get; }
    Derivatives Derivatives { get; }

    public string AzureStorageAccount {get;}
    public string StorageContainer { get; }

    void RegisterWindowPlace(Window window, string key);
    void RefreshMetatagSchema();
    void CloseAsyncLogMonitor(bool skipClose);
    void CloseAppLogMonitor(bool skipClose);
    public void AddBackgroundWork(string description, BackgroundWorkerWork<bool> work);
    public void SetupLogging(CloseLogMonitorDelegate closeAsyncLogDelegate, CloseLogMonitorDelegate closeAppLogDelegate);
    public void SetupBackgroundWorkers(AddBackgroundWorkDelegate addWorkDelegate);
}
