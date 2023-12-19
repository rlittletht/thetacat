using System;
using Microsoft.Identity.Client.Cache;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Workgroups;

public class WorkgroupCacheEntryData
{
    public Guid ID { get; init; }
    public PathSegment Path { get; set; }

    public Guid CachedBy { get; set; }
    public DateTime? CacheDate { get; set; }
    public int? VectorClock { get; set; }

    public WorkgroupCacheEntryData(Guid iD, PathSegment path, Guid cachedBy, DateTime? cacheDate, int? vectorClock)
    {
        ID = iD;
        Path = path;
        CachedBy = cachedBy;
        CacheDate = cacheDate;
        VectorClock = vectorClock;
    }

    public WorkgroupCacheEntryData(WorkgroupCacheEntryData source)
    {
        ID = source.ID;
        Path = source.Path;
        CachedBy = source.CachedBy;
        CacheDate = source.CacheDate;
        VectorClock = source.VectorClock;
    }

    public WorkgroupCacheEntryData(ServiceWorkgroupItem item)
    {
        ID = item.MediaId ?? throw new CatExceptionServiceDataFailure();
        Path = new PathSegment(item.Path ?? throw new CatExceptionServiceDataFailure());
        CachedBy = item.CachedBy ?? throw new CatExceptionServiceDataFailure();
        CacheDate = item.CachedDate;
        VectorClock = item.VectorClock;
    }
}
