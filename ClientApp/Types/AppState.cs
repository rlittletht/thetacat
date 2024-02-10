﻿using System;
using System.Collections.Generic;
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
    public ClientDatabase ClientDatabase { get; init; }
    public Md5Cache Md5Cache { get; init; }
    public Derivatives Derivatives { get; init; }
    public MetatagMRU MetatagMRU { get; init; }

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

    public void RefreshMetatagSchema()
    {
        MetatagSchema.ReplaceFromService(ServiceInterop.GetMetatagSchema());
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

    public AppState()
    {
        Settings = new TcSettings.TcSettings();

        bool fSetProfile = false;

        foreach (Profile profile in Settings.Profiles.Values)
        {
            if (profile.Default)
            {
                ActiveProfile = profile;
                break;
            }

            if (!fSetProfile)
            {
                ActiveProfile = profile;
                fSetProfile = true; // so we at least have a profile if there is no default specified
            }
        }

        if (ActiveProfile == null)
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

        AppSecrets.MasterSqlConnectionString = ActiveProfile.SqlConnection ?? String.Empty;

        Catalog = new Catalog();
        MetatagSchema = new MetatagSchema();
        Cache = new Cache(ActiveProfile);
        m_closeAsyncLog = null;
        m_closeAppLog = null;
        m_addBackgroundWork = null;
        // this will start the caching pipelines
        PreviewImageCache = new ImageCache();
        ImageCache = new ImageCache(true);
        ClientDatabase = new ClientDatabase(App.ClientDatabasePath(ActiveProfile.ClientDatabaseName));
        Md5Cache = new Md5Cache(ClientDatabase);
        Derivatives = new Derivatives(ClientDatabase);
        MetatagMRU = new MetatagMRU();
    }

    public void ChangeProfile(string profileName)
    {
        ActiveProfile = Settings.Profiles[profileName];
        AppSecrets.MasterSqlConnectionString = ActiveProfile.SqlConnection ?? String.Empty;
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
}
