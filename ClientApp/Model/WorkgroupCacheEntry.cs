using System;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model;

public class WorkgroupCacheEntry: ICacheEntry
{
    public Guid ID { get; init; }
    public PathSegment Path { get; }

    public Guid CachedBy { get; }
    public DateTime CacheDate { get; }

    public WorkgroupCacheEntry(Guid iD, PathSegment path, Guid cachedBy, DateTime cacheDate)
    {
        ID = iD;
        Path = path;
        CachedBy = cachedBy;
        CacheDate = cacheDate;
    }
}
