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
}
