﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Thetacat.Model;
using Thetacat.Model.Workgroups;
using Thetacat.Util;

namespace Thetacat.Types;

public interface IWorkgroup
{
    public string Name { get; }
    public PathSegment Server { get; }
    public PathSegment CacheRoot { get; }
    public Guid ClientId {get; }
    public Guid Id { get; }
    public PathSegment FullPathToCacheRoot { get; }
    public string FullyQualifiedPath { get; }
    public void RefreshWorkgroupMedia(ConcurrentDictionary<Guid, ICacheEntry> entries);
    public Dictionary<Guid, MediaItem> GetNextItemsForQueue(int count);
    public Dictionary<Guid, MediaItem> GetNextItemsForQueueFromMediaCollection(Guid catalogID, IEnumerable<MediaItem> mediaCollection, ICache cache, int count);
    public void PushChangesToDatabaseWithCache(ICache cache, Dictionary<Guid, MediaItem>? itemsForCache);
    public void PushChangesToDatabase(Dictionary<Guid, MediaItem>? itemsForCache);
    public void CreateCacheEntryForItem(ICache cache, MediaItem item, DateTime? cachedDate, bool pending);
    public void DeleteMediaItem(Guid id);
    public List<ServiceWorkgroupFilter> GetLatestWorkgroupFilters();
    public void ExecuteFilterAddsAndDeletes(IEnumerable<WorkgroupFilter> deletes, IEnumerable<WorkgroupFilter> inserts);
    public ServiceWorkgroupFilter GetWorkgroupFilter(Guid id);
    public void UpdateWorkgroupFilter(WorkgroupFilter filter, int baseClock);
    public void UpdateClientDeletedMediaClockToAtLeast(int newClock);
    public int GetMinWorkgroupDeletedMediaClock();
}
