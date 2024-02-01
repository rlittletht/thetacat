using System;
using System.Windows;
using System.Windows.Media;
using Thetacat.Explorer;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Client;
using Thetacat.Model.ImageCaching;
using Thetacat.Secrets;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.TcSettings;
using Thetacat.Util;

namespace Thetacat.Types;

public class AppState : IAppState
{
    public delegate void CloseLogMonitorDelegate(bool skipClose);
    public delegate void AddBackgroundWorkDelegate(string description, BackgroundWorkerWork<bool> work);
    public delegate void SetDirtyStateDelegate(bool isDirty);

    public DpiScale DpiScale { get; set; }
    private CloseLogMonitorDelegate? m_closeAsyncLog;
    private CloseLogMonitorDelegate? m_closeAppLog;
    private AddBackgroundWorkDelegate? m_addBackgroundWork;
    private SetDirtyStateDelegate? m_setCollectionDirtyState;
    private SetDirtyStateDelegate? m_setSchemaDirtyState;

    public TcSettings.TcSettings _Settings { get; }
    public TcSettings.Profile Settings { get; }
    public MetatagSchema MetatagSchema { get; }
    public ICache Cache { get; private set; }
    public ImageCache PreviewImageCache { get; private set; }
    public ImageCache ImageCache { get; private set; }
    public ICatalog Catalog { get; private set; }
    public void CloseAsyncLogMonitor(bool skipClose) => m_closeAsyncLog?.Invoke(skipClose);
    public void CloseAppLogMonitor(bool skipClose) => m_closeAppLog?.Invoke(skipClose);
    public string AzureStorageAccount => App.State.Settings.AzureStorageAccount ?? throw new CatExceptionInitializationFailure("no azure storage account set");
    public string StorageContainer => App.State.Settings.StorageContainer ?? throw new CatExceptionInitializationFailure("no storage container set");
    public ClientDatabase ClientDatabase { get; init; }
    public Md5Cache Md5Cache { get; init; }
    public Derivatives Derivatives { get; init; }
    public MetatagMRU MetatagMRU { get; init; }

    public SetDirtyStateDelegate SetCollectionDirtyState
    {
        get => m_setCollectionDirtyState ?? new SetDirtyStateDelegate((_) => { });
        set => m_setCollectionDirtyState = value;
    }

    public SetDirtyStateDelegate SetSchemaDirtyState
    {
        get => m_setSchemaDirtyState ?? new SetDirtyStateDelegate((_) => { });
        set => m_setSchemaDirtyState = value;
    }

    public void SetupLogging(CloseLogMonitorDelegate closeAsyncLogDelegate, CloseLogMonitorDelegate closeAppLogDelegate)
    {
        m_closeAsyncLog = closeAsyncLogDelegate;
        m_closeAppLog = closeAppLogDelegate;
    }

    public void SetupBackgroundWorkers(AddBackgroundWorkDelegate addWorkDelegate)
    {
        m_addBackgroundWork = addWorkDelegate;;
    }

    public void AddBackgroundWork(string description, BackgroundWorkerWork<bool> work)
    {
        if (m_addBackgroundWork == null)
            throw new CatExceptionInitializationFailure("no background work collection available");

        m_addBackgroundWork(description, work);
    }

    public void RefreshMetatagSchema()
    {
        MetatagSchema.ReplaceFromService(ServiceInterop.GetMetatagSchema());
        MetatagMRU.Set(App.State.Settings.MetatagMru);
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
        _Settings = new TcSettings.TcSettings();

        bool fSetProfile = false;

        foreach (Profile profile in _Settings.Profiles.Values)
        {
            if (profile.Default)
            {
                Settings = profile;
                break;
            }

            if (!fSetProfile)
            {
                Settings = profile;
                fSetProfile = true; // so we at least have a profile if there is no default specified
            }
        }

        if (Settings == null)
        {
            // this means there wasn't a profile to set at all. Make a default one
            Settings = new Profile()
                       {
                           Name = "Default",
                           Default = true
                       };

            _Settings.Profiles.Add(Settings.Name, Settings);
        }

        // make the assumed default profile the real default
        Settings.Default = true;

        AppSecrets.MasterSqlConnectionString = Settings.SqlConnection ?? String.Empty;

        Catalog = new Catalog();
        MetatagSchema = new MetatagSchema();
        Cache = new Cache(Settings);
        m_closeAsyncLog = null;
        m_closeAppLog = null;
        m_addBackgroundWork = null;
        // this will start the caching pipelines
        PreviewImageCache = new ImageCache();
        ImageCache = new ImageCache(true);
        ClientDatabase = new ClientDatabase(App.ClientDatabasePath);
        Md5Cache = new Md5Cache(ClientDatabase);
        Derivatives = new Derivatives(ClientDatabase);
        MetatagMRU = new MetatagMRU();
    }

    public void RegisterWindowPlace(Window window, string key)
    {
        ((App)Application.Current).WindowPlace.Register(window, key);
    }
}
