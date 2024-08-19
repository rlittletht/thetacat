using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Thetacat.Explorer;
using Thetacat.Logging;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Caching;
using Thetacat.Model.Client;
using Thetacat.Model.ImageCaching;
using Thetacat.Model.Md5Caching;
using Thetacat.Secrets;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.TcSettings;
using Thetacat.Util;

namespace Thetacat.Types;

public class AppState : IAppState
{
    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;
    public delegate void CloseLogMonitorDelegate(bool skipClose);
    public delegate void AddBackgroundWorkDelegate(string description, BackgroundWorkerWork<bool> work, OnWorkCompletedDelegate? onWorkCompleted = null);

    public DpiScale DpiScale { get; set; }
    private CloseLogMonitorDelegate? m_closeAsyncLog;
    private CloseLogMonitorDelegate? m_closeAppLog;
    private AddBackgroundWorkDelegate? m_addBackgroundWork;

    public TcSettings.TcSettings Settings { get; }
    public TcSettings.Profile ActiveProfile { get; private set; }
    public MetatagSchema MetatagSchema { get; }
    public ICache Cache { get; private set; }
    public ImageCache PreviewImageCache { get; private set; }
    public ImageCache ImageCache { get; private set; }
    public ICatalog Catalog { get; private set; }
    public void CloseAsyncLogMonitor(bool skipClose) => m_closeAsyncLog?.Invoke(skipClose);
    public void CloseAppLogMonitor(bool skipClose) => m_closeAppLog?.Invoke(skipClose);
    public string AzureStorageAccount => App.State.ActiveProfile.AzureStorageAccount ?? throw new CatExceptionInitializationFailure("no azure storage account set");
    public string StorageContainer => App.State.ActiveProfile.StorageContainer ?? throw new CatExceptionInitializationFailure("no storage container set");
    public ClientDatabase? ClientDatabase { get; private set; }
    public Md5Cache Md5Cache { get; init; }
    public Derivatives Derivatives { get; init; }
    public MetatagMRU MetatagMRU { get; init; }
    public WindowManager WindowManager { get; init; }

    public void SetupLogging(CloseLogMonitorDelegate closeAsyncLogDelegate, CloseLogMonitorDelegate closeAppLogDelegate)
    {
        m_closeAsyncLog = closeAsyncLogDelegate;
        m_closeAppLog = closeAppLogDelegate;
    }

    public void SetupBackgroundWorkers(AddBackgroundWorkDelegate addWorkDelegate)
    {
        m_addBackgroundWork = addWorkDelegate;;
    }

    public void AddBackgroundWork(string description, BackgroundWorkerWork<bool> work, OnWorkCompletedDelegate? onWorkCompleted = null)
    {
        if (m_addBackgroundWork == null)
            throw new CatExceptionInitializationFailure("no background work collection available");

        m_addBackgroundWork(description, work, onWorkCompleted);
    }

    public void RefreshMetatagSchema(Guid catalogID)
    {
        MetatagSchema.ReplaceFromService(ServiceInterop.GetMetatagSchema(catalogID));
        MetatagMRU.Set(App.State.ActiveProfile.MetatagMru);
    }

    public void OverrideCache(ICache cache)
    {
        Cache = cache;
    }

    public void OverrideCatalog(ICatalog catalog)
    {
        Catalog = catalog;
    }

    public void LoadProfiles()
    {
        bool fSetProfile = false;

        foreach (Profile profile in Settings.Profiles.Values)
        {
            if (profile.Default)
            {
                ActiveProfile = profile;
                fSetProfile = true;
                break;
            }

            if (!fSetProfile)
            {
                ActiveProfile = profile;
                fSetProfile = true; // so we at least have a profile if there is no default specified
            }
        }

        if (!fSetProfile)
        {
            // this means there wasn't a profile to set at all. Make a default one
            ActiveProfile = new Profile()
                            {
                                Name = "Default",
                                Default = true
                            };

            Settings.Profiles.Add(ActiveProfile.Name, ActiveProfile);
        }

        if (string.IsNullOrEmpty(ActiveProfile.ClientDatabaseName))
            ActiveProfile.ClientDatabaseName = "client.db";

        // make the assumed default profile the real default
        ActiveProfile.Default = true;
    }

    public AppState()
    {
        Settings = new TcSettings.TcSettings();

        ActiveProfile = new Profile()
                        {
                            Name = "Default",
                            Default = true
                        };

        Catalog = new Catalog();
        MetatagSchema = new MetatagSchema();
        Cache = new Cache(null);
        m_closeAsyncLog = null;
        m_closeAppLog = null;
        m_addBackgroundWork = null;
        // this will start the caching pipelines
        PreviewImageCache = new ImageCache();
        ImageCache = new ImageCache(true);
        ClientDatabase = null;
        Md5Cache = new Md5Cache(ClientDatabase);
        Derivatives = new Derivatives(ClientDatabase);
        MetatagMRU = new MetatagMRU();
        ProfileChanged += OnProfileChanged;
        WindowManager = new WindowManager();
    }

    /*----------------------------------------------------------------------------
        %%Function: OnProfileChanged
        %%Qualified: Thetacat.Types.AppState.OnProfileChanged

        This is our own private OnProfileChanged. Resets our AppSecrets
        and the 
    ----------------------------------------------------------------------------*/
    private void OnProfileChanged(object? sender, ProfileChangedEventArgs e)
    {
        string clientDatabasePath = App.ClientDatabasePath(ActiveProfile.ClientDatabaseName);

        string? directory = Path.GetDirectoryName(clientDatabasePath);
        if (directory != null)
            Directory.CreateDirectory(directory);

        AppSecrets.MasterSqlConnectionString = ActiveProfile.SqlConnection ?? String.Empty;
        ClientDatabase = new ClientDatabase(clientDatabasePath);

        ClientDatabase.AdjustDatabaseIfNecessary();

        Cache.ResetCache(ActiveProfile);
        Catalog.Reset();
        MetatagSchema.Reset();
        Derivatives.ResetDerivatives(ClientDatabase);
        Md5Cache.ResetMd5Cache(ClientDatabase);
    }

    public void ChangeProfile(string profileName)
    {
        ActiveProfile = Settings.Profiles[profileName];
        if (ProfileChanged != null)
            ProfileChanged(this, new ProfileChangedEventArgs(ActiveProfile));
    }

    private readonly List<string> m_stashedSqConnections = new();

    public void PushTemporarySqlConnection(string sqlConnection)
    {
        m_stashedSqConnections.Add(AppSecrets.MasterSqlConnectionString);
        AppSecrets.MasterSqlConnectionString = sqlConnection;
    }

    public void PopTemporarySqlConnection()
    {
        AppSecrets.MasterSqlConnectionString = m_stashedSqConnections[m_stashedSqConnections.Count - 1];
        m_stashedSqConnections.RemoveAt(m_stashedSqConnections.Count - 1);
    }

    public void RegisterWindowPlace(Window window, string key)
    {
        ((App)Application.Current).WindowPlace.Register(window, key);
    }

    public void EnsureDeletedItemCollateralRemoved(Guid item)
    {
        try
        {
            Cache.DeleteMediaItem(item);
            Derivatives.DeleteMediaItem(item);
        }
        catch
        {
            App.LogForApp(EventType.Warning, $"Couldn't remove collateral for item {item}. Will try again later.");
        }
    }

    public void EnsureDeletedItemsCollateralRemoved(List<Guid> items)
    {
        foreach (Guid item in items)
        {
            EnsureDeletedItemCollateralRemoved(item);
        }
    }

    public string GetMD5ForItem(Guid id)
    {
        return Catalog.GetMD5ForItem(id, Cache);
    }
}
