using System;
using static Thetacat.TcSettings.TcSettings;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Thetacat.Filtering;
using Thetacat.Types;

namespace Thetacat.TcSettings;

public class Profile
{
    public string? Name;
    public bool Default = false;

    public string? ElementsDatabase;
    public string? CacheLocation;
    public string ClientDatabaseName = string.Empty;

    public Guid CatalogID;
    public string? CacheType;
    public string? WorkgroupId;
    public string? WorkgroupCacheServer;
    public string? WorkgroupCacheRoot;
    public string? WorkgroupName;

    public string? AzureStorageAccount;
    public string? StorageContainer;
    public string? SqlConnection;

    public bool? ShowAsyncLogOnStart;
    public bool? ShowAppLogOnStart;
    public string? ExplorerItemSize;

    public string? TimelineType;
    public string? TimelineOrder;
    public bool? ExpandMediaStacksInExplorers;

    public string? _DerivativeCache;
    public string? DerivativeCache => LocalCatalogCache;
    public string? LocalCatalogCache;

    public List<string> MetatagMru = new();
    public List<MapPair> ElementsSubstitutions = new();
    private IEnumerator<KeyValuePair<string, Rectangle>>? PlacementsEnumerator { get; set; }

    public string? DefaultFilterName;

    public Dictionary<string, FilterDefinition> Filters = new();

    public Profile()
    {
        ClientDatabaseName = "client.db";
    }

    public Profile(Profile basedOn)
    {
        CatalogID = basedOn.CatalogID;
        Name = basedOn.Name;
        ElementsDatabase = basedOn.ElementsDatabase;
        CacheLocation = basedOn.CacheLocation;
        ClientDatabaseName = basedOn.ClientDatabaseName;
        CacheType = basedOn.CacheType;
        WorkgroupId = basedOn.WorkgroupId;
        WorkgroupCacheServer = basedOn.WorkgroupCacheServer;
        WorkgroupCacheRoot = basedOn.WorkgroupCacheRoot;
        WorkgroupName = basedOn.WorkgroupName;
        AzureStorageAccount = basedOn.AzureStorageAccount;
        StorageContainer = basedOn.StorageContainer;
        SqlConnection = basedOn.SqlConnection;
        ShowAsyncLogOnStart = basedOn.ShowAsyncLogOnStart;
        ShowAppLogOnStart = basedOn.ShowAppLogOnStart;
        ExplorerItemSize = basedOn.ExplorerItemSize;
        TimelineType = basedOn.TimelineType;
        ExpandMediaStacksInExplorers = basedOn.ExpandMediaStacksInExplorers;
        TimelineOrder = basedOn.TimelineOrder;
        _DerivativeCache = basedOn._DerivativeCache;
        LocalCatalogCache = basedOn.LocalCatalogCache;
    }

    public override string ToString() => Name ?? string.Empty;

    public string RootForCatalogCache()
    {
        if (string.IsNullOrEmpty((LocalCatalogCache)))
            throw new CatException("no root path for catalog cache");

        string path = Path.Combine(LocalCatalogCache, CatalogID.ToString());
        Directory.CreateDirectory(path);

        return path;
    }

    public void MigrateToLatest()
    {
        if (string.IsNullOrEmpty(LocalCatalogCache) && !string.IsNullOrEmpty(_DerivativeCache))
        {
            LocalCatalogCache = _DerivativeCache;
        }

        _DerivativeCache = null;
    }
}
