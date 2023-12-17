using HeyRed.Mime;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
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
    public void DoForegroundCache();
}
