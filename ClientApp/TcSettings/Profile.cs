using System;
using static Thetacat.TcSettings.TcSettings;
using System.Collections.Generic;
using System.Drawing;
using Thetacat.Filtering;

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

    public string? DerivativeCache;

    public List<string> MetatagMru = new();
    public List<MapPair> ElementsSubstitutions = new();
    private IEnumerator<KeyValuePair<string, Rectangle>>? PlacementsEnumerator { get; set; }

    public string? DefaultFilterName;

    public Dictionary<string, FilterDefinition> Filters = new();

    public Profile() {}

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
        TimelineOrder = basedOn.TimelineOrder;
        DerivativeCache = basedOn.DerivativeCache;
    }

    public override string ToString() => Name ?? string.Empty;
}
