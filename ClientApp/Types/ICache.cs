using HeyRed.Mime;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using TCore;
using Thetacat.Model;
using Thetacat.Model.Workgroups;
using Thetacat.Util;

namespace Thetacat.Types;

public interface ICache
{
    public Cache.CacheType Type { get; }
    public IWorkgroup _Workgroup { get; }
    public PathSegment LocalPathToCacheRoot { get; }
    public ConcurrentDictionary<Guid, ICacheEntry> Entries { get; }
    public bool IsItemCached(Guid id);
    public Task DoForegroundCache(int chunkSize);
    public void PrimeCacheFromImport(MediaItem item, PathSegment importSource);
    public void PushChangesToDatabase(Dictionary<Guid, MediaItem>? itemsForCache);
}
