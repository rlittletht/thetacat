using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.Util;

namespace Thetacat.Types;

public interface IAppState
{
    ICatalog Catalog { get; }
    TcSettings.TcSettings Settings { get; }
    MetatagSchema MetatagSchema { get; }
    ICache Cache { get; }
    public string AzureStorageAccount {get;}
    public string StorageContainer { get; }

    void RegisterWindowPlace(Window window, string key);
    void RefreshMetatagSchema();
    void CloseAsyncLogMonitor(bool skipClose);
    void CloseAppLogMonitor(bool skipClose);
    public void AddBackgroundWork(string description, BackgroundWorkerWork work);
}