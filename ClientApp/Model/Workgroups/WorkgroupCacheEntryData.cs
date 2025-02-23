using System;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Workgroups;

public class WorkgroupCacheEntryData
{
    public Guid ID { get; set; }
    public PathSegment Path { get; set; }

    public Guid CachedBy { get; set; }
    public DateTime? CacheDate { get; set; }
    public int? VectorClock { get; set; }
    public string MD5 { get; set; }

    public WorkgroupCacheEntryData()
    {
        ID = Guid.Empty;
        Path = new PathSegment();
        CachedBy = Guid.Empty;
        CacheDate = null;
        VectorClock = null;
        MD5 = "";
    }

    public WorkgroupCacheEntryData(Guid id, PathSegment path, Guid cachedBy, DateTime? cacheDate, int? vectorClock, string md5)
    {
        ID = id;
        Path = path;
        CachedBy = cachedBy;
        CacheDate = cacheDate;
        VectorClock = vectorClock;
        MD5 = md5;
    }

    public WorkgroupCacheEntryData(WorkgroupCacheEntryData source)
    {
        ID = source.ID;
        Path = source.Path;
        CachedBy = source.CachedBy;
        CacheDate = source.CacheDate;
        VectorClock = source.VectorClock;
        MD5 = source.MD5;
    }

    public WorkgroupCacheEntryData(ServiceWorkgroupItem item)
    {
        ID = item.MediaId ?? throw new CatExceptionServiceDataFailure();
        Path = new PathSegment(item.Path ?? throw new CatExceptionServiceDataFailure());
        CachedBy = item.CachedBy ?? throw new CatExceptionServiceDataFailure();
        CacheDate = item.CachedDate;
        VectorClock = item.VectorClock;
        MD5 = item.MD5 ?? "";
    }
}
