﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using Microsoft.Identity.Client;
using Thetacat.Model;
using Thetacat.TcSettings;
using Thetacat.Util;
using Thetacat.Model.Caching;

namespace Thetacat.Types;

public interface ICache
{
    public void ResetCache(Profile profile, IWorkgroup? workgroup);
    public IWorkgroup _Workgroup { get; }
    public PathSegment LocalPathToCacheRoot { get; }
    public ConcurrentDictionary<Guid, ICacheEntry> Entries { get; }
    public bool IsItemCached(Guid id);
    public void StartBackgroundCaching(int chunkSize);
    public void PrimeCacheFromImport(MediaItem item, PathSegment importSource);
    public void PushChangesToDatabase(Dictionary<Guid, MediaItem>? itemsForCache);
    public string? TryGetCachedFullPath(Guid id);
    public void DeleteMediaItem(Guid id);
    public string GetFullLocalPath(PathSegment itemPath);
    public PathSegment GetRelativePathToCacheRootFromFullPath(PathSegment fullLocal);
    public void UpdateEntryForMd5Change(Guid id, string md5);
}
