using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Thetacat.Model;
using Thetacat.Util;

namespace Thetacat.Types;

public interface IWorkgroup
{
    public string Name { get; }
    public PathSegment Server { get; }
    public PathSegment CacheRoot { get; }
    public Guid ClientId {get; }
    public PathSegment FullPathToCacheRoot { get; }
    public string FullyQualifiedPath { get; }
    public void RefreshWorkgroupMedia(ConcurrentDictionary<Guid, ICacheEntry> entries);
    public Dictionary<Guid, MediaItem> GetNextItemsForQueue(int count);
    public void PushChangesToDatabase(Dictionary<Guid, MediaItem>? itemsForCache);
}
