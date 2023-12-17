using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using HeyRed.Mime;
using TCore;
using Thetacat.Model.Workgroups;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model;

public class Cache
{
    public enum CacheType
    {
        Private,
        Workgroup,
        Unknown
    }

    public CacheType Type { get; private set; }

    public Workgroup _Workgroup
    {
        get
        {
            if (m_workgroup == null)
                throw new CatExceptionInitializationFailure();

            return m_workgroup;
        }
    }

    public PathSegment LocalPathToCacheRoot { get; }

    public ConcurrentDictionary<Guid, ICacheEntry> Entries = new ConcurrentDictionary<Guid, ICacheEntry>();
    private Workgroup? m_workgroup;

    public static CacheType CacheTypeFromString(string? value)
    {
        if (String.Compare(value, "private", StringComparison.InvariantCultureIgnoreCase) == 0)
            return CacheType.Private;
        else if (String.Compare(value, "workgroup", StringComparison.InvariantCultureIgnoreCase) == 0)
            return CacheType.Workgroup;

        return CacheType.Unknown;
    }

    public static string StringFromCacheType(CacheType cacheType)
    {
        switch (cacheType)
        {
            case CacheType.Private:
                return "private";
            case CacheType.Workgroup:
                return "workgroup";
        }

        throw new ArgumentException("bad cache type argument");
    }

    void ConnectToWorkgroupCache(TcSettings.TcSettings settings)
    {
        if (Type != CacheType.Workgroup)
            throw new InvalidOperationException("intializing a non-workgroup");

        if (!Guid.TryParse(settings.WorkgroupId, out Guid id))
            return;

        try
        {
            m_workgroup = new Workgroup(id);
        }
        catch (TcSqlExceptionNoResults e)
        {
            throw new CatExceptionWorkgroupNotFound(e.Crids, e, "workgroup not found");
        }

        // make sure the directory exists
        Directory.CreateDirectory(_Workgroup.FullyQualifiedPath);

        _Workgroup.RefreshWorkgroupMedia(Entries);
    }

    public bool IsItemCached(Guid id)
    {
        return Entries.ContainsKey(id);
    }

    /*----------------------------------------------------------------------------
        %%Function: Cache
        %%Qualified: Thetacat.Model.Cache.Cache

        The cache handles all local operations:
            Sync from server
            Scan cache for local changes
                Sync back to server

        The cache abstracts whether this is workgroup or private
    ----------------------------------------------------------------------------*/
    public Cache(TcSettings.TcSettings settings)
    {
        CacheType cacheType = CacheTypeFromString(settings.CacheType);

        Type = cacheType;

        if (Type == CacheType.Workgroup)
        {
            ConnectToWorkgroupCache(settings);
            LocalPathToCacheRoot = new PathSegment(_Workgroup.FullPathToCacheRoot);
        }
        else
        {
            LocalPathToCacheRoot = new PathSegment();
        }
    }

    // This is the list of media items that we are going to try to download to the 
    // cache. NOTE: We might hit a coherency failure when trying to update the workgroup
    // db (in order to tell the world which items we're going to cache). if that happens,
    // and if another client marked the same media for caching, then we need to NOT
    // try to cache them (effectively, we need to cancel this item).
    // since we can't remove from the middle of the queue, we will add to a 'cancel list'
    // when we are asked to dequeue an item

    readonly ConcurrentQueue<MediaItem> m_cacheQueue = new();
    private readonly ConcurrentDictionary<Guid, byte> m_canceledQueueItems = new();

    public static bool OkToUseLocalPathForItem(PathSegment fullPath, MediaItem item)
    {
        if (Path.Exists(fullPath.Local))
            // yikes. file already exists
            // last chance...is it already the file we want? (check the MD5 hash)

            // if the MD5 matches, then the cache is already done. its ok to use
            // this name. when we see the file already exists in the future we
            // will know its OK to assume its the same file
            return (MediaItem.CalculateMD5Hash(fullPath.Local) == item.MD5);

        return true;
    }

    public static PathSegment EnsureUniqueLocalCacheVirtualPath(PathSegment localPathToCacheRoot, MediaItem item)
    {
        // easiest would be to use the virtual path

        // if the virtual path is rooted, we can't use it
        // just use a guid.
        if (Path.IsPathRooted(item.VirtualPath.Local))
        {
            return new PathSegment(Path.ChangeExtension(Guid.NewGuid().ToString(), MimeTypesMap.GetExtension(item.MimeType)));
        }

        PathSegment test = PathSegment.Join(localPathToCacheRoot, item.VirtualPath);

        int count = 0;

        while (!OkToUseLocalPathForItem(test, item))
        {
            // if we get to 50 collisions, give up and just use a guid
            if (count > 50)
                return new PathSegment(Path.ChangeExtension(Guid.NewGuid().ToString(), MimeTypesMap.GetExtension(item.MimeType)));

            test = PathSegment.Join(localPathToCacheRoot, item.VirtualPath.AppendLeafSuffix($"({++count})"));
        }

        return item.VirtualPath.AppendLeafSuffix($"({++count})");
    }

    public void DoForegroundCache()
    {
        if (Type != CacheType.Workgroup)
        {
            MessageBox.Show("Private cachine NYI");
            return;
        }

        _Workgroup.RefreshWorkgroupMedia(Entries);

        // now let's stake our claim to some items we're going to cache
        Dictionary<Guid, MediaItem> itemsForCache = _Workgroup.GetNextItemsForQueue(100);
        _Workgroup.PushChangesToDatabase(itemsForCache);

        // lastly, queue all the items left in itemsForCache
        foreach (MediaItem item in itemsForCache.Values)
        {
            m_cacheQueue.Enqueue(item);
        }
    }
}
