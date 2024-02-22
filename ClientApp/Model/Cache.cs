using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using HeyRed.Mime;
using TCore;
using TCore.SqlCore;
using Thetacat.Azure;
using Thetacat.Logging;
using Thetacat.Model.Workgroups;
using Thetacat.TcSettings;
using Thetacat.Types;
using Thetacat.UI;
using Thetacat.Util;

namespace Thetacat.Model;

public class Cache: ICache
{
    public enum CacheType
    {
        Private,
        Workgroup,
        Unknown
    }

    public virtual CacheType Type { get; private set; }

    public virtual IWorkgroup _Workgroup
    {
        get
        {
            if (m_workgroup == null)
                throw new CatExceptionInitializationFailure();

            return m_workgroup;
        }
    }

    public PathSegment LocalPathToCacheRoot { get; private set; }

    public ConcurrentDictionary<Guid, ICacheEntry> Entries { get; } = new ConcurrentDictionary<Guid, ICacheEntry>();

    private IWorkgroup? m_workgroup;

    public Cache()
    {
        // only used for tests
        LocalPathToCacheRoot = new PathSegment("//mock/server/mockroot");
    }

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

    void ConnectToWorkgroupCache(TcSettings.Profile settings)
    {
        if (Type != CacheType.Workgroup)
            throw new InvalidOperationException("intializing a non-workgroup");

        if (!Guid.TryParse(settings.WorkgroupId, out Guid id))
            return;

        try
        {
            m_workgroup = new Workgroup(settings.CatalogID, id);
        }
        catch (SqlExceptionNoResults e)
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

    public string? TryGetCachedFullPath(Guid id)
    {
        if (!Entries.TryGetValue(id, out ICacheEntry? value))
            return null;

        return GetFullLocalPath(value.Path);
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
#pragma warning disable CS8618 // we set these in a method
    public Cache(TcSettings.Profile? settings)
    {
        ResetCache(settings);
    }
#pragma warning restore CS8618

    public void ResetCache(Profile? settings)
    {
        Entries.Clear();

        if (settings == null)
        {
            Type = CacheType.Unknown;
            LocalPathToCacheRoot = PathSegment.Empty;
            m_workgroup = null;
            return;
        }

        CacheType cacheType = CacheTypeFromString(settings.CacheType);

        Type = cacheType;

        if (Type == CacheType.Workgroup && settings.WorkgroupId != null)
        {
            try
            {
                ConnectToWorkgroupCache(settings);
                LocalPathToCacheRoot = new PathSegment(_Workgroup.FullPathToCacheRoot);
            }
            catch (CatExceptionWorkgroupNotFound)
            {
                MessageBox.Show($"Failed to connect to workgroup {settings.WorkgroupName}.");
                LocalPathToCacheRoot = new PathSegment();
            }
            catch (CatExceptionNoSqlConnection)
            {
                MessageBox.Show($"Failed to connect to workgroup. No SQL connection available.");
                LocalPathToCacheRoot = new PathSegment();
            }
        }
        else
        {
            LocalPathToCacheRoot = new PathSegment();
        }
    }

    // This is the list of media items that we are going to try to download to the 
    // cache.
    protected readonly ConcurrentQueue<MediaItem> m_cacheQueue = new();

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

        if (localPathToCacheRoot == $"{Workgroup.s_mockServer}{Workgroup.s_mockRoot}")
            return item.VirtualPath;

        // if the virtual path is rooted, we can't use it
        // just use a guid.
        if (Path.IsPathRooted(item.VirtualPath.Local) || item.VirtualPath == PathSegment.Empty)
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

        if (count == 0)
            return item.VirtualPath;

        return item.VirtualPath.AppendLeafSuffix($"({count})");
    }

    public void QueueCacheDownloadsFromMedia(IEnumerable<MediaItem> mediaCollection, ICache cache, int chunkSize)
    {
        if (Type != CacheType.Workgroup)
        {
            MessageBox.Show("Private caching NYI");
            return;
        }

        _Workgroup.RefreshWorkgroupMedia(Entries);

        // first, find items in the WG DB that belong to our client and haven't been download
        // and then put them in our queue (we claimed them in a previous session and didn't finish
        // downloading them...)
        foreach (KeyValuePair<Guid, ICacheEntry> entry in Entries)
        {
            if (entry.Value.LocalPending || entry.Value.CachedDate != null)
                continue;

            WorkgroupCacheEntry wgEntry = (WorkgroupCacheEntry)entry.Value;

            if (wgEntry.CachedBy == _Workgroup.ClientId)
            {
                // add this to our queue
                entry.Value.LocalPending = true;
                MediaItem item = App.State.Catalog.GetMediaFromId(entry.Key);
                item.IsCachePending = true;
                m_cacheQueue.Enqueue(item);
                chunkSize--; // since we just added one
            }
        }

        if (chunkSize <= 0)
            return;

        // now let's stake our claim to some items we're going to cache
        Dictionary<Guid, MediaItem> itemsForCache = _Workgroup.GetNextItemsForQueueFromMediaCollection(mediaCollection, cache, chunkSize);
        _Workgroup.PushChangesToDatabaseWithCache(cache, itemsForCache);

        // lastly, queue all the items left in itemsForCache
        foreach (MediaItem item in itemsForCache.Values)
        {
            m_cacheQueue.Enqueue(item);
        }
    }

    public void QueueCacheDownloads(int chunkSize)
    {
        QueueCacheDownloadsFromMedia(App.State.Catalog.GetMediaCollection(), App.State.Cache, chunkSize);
    }

    async Task<bool> FEnsureMediaItemDownloadedToCache(MediaItem item, string destination)
    {
        if (item.IsCachePending && item.State != MediaItemState.Pending)
        {
            TcBlob blob = await AzureCat._Instance.DownloadMedia(destination, item.ID.ToString(), item.MD5);
            MainWindow.LogForAsync(EventType.Information, $"downloaded {item.ID} to {item.LocalPath}");
            return true;
        }

        return false;
    }

    public string GetFullLocalPath(PathSegment localSegment)
    {
        return PathSegment.Join(LocalPathToCacheRoot, localSegment).Local;
    }

    /*----------------------------------------------------------------------------
        %%Function: PrimeCacheFromImport
        %%Qualified: Thetacat.Model.Cache.PrimeCacheFromImport

        We are importing which means we have a local copy of the media. Take
        advantage of this to populate our cache. (we will still have to upload
        to azure)
    ----------------------------------------------------------------------------*/
    public void PrimeCacheFromImport(MediaItem item, PathSegment importSource)
    {
        // we still have to make an entry in the cache db
        // since we are manually caching it right now, set the time to now and pending to false)
        _Workgroup.CreateCacheEntryForItem(App.State.Cache, item, DateTime.Now, false);
        // now get the destination path it wants us to use
        if (!Entries.TryGetValue(item.ID, out ICacheEntry? entry))
            throw new CatExceptionInternalFailure("we just added a cache entry and its not there!?");

        string fullLocalPath = GetFullLocalPath(entry.Path);

        // if the path already exists, then it is already done
        if (Path.Exists(fullLocalPath))
            return;

        string? directory = Path.GetDirectoryName(fullLocalPath);
        if (directory != null && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.Copy(importSource.Local, fullLocalPath);
    }

    public bool DoCacheWork(IProgressReport progressReport, int chunkSize)
    {
        // this is an indeterminate progress report, so just report infinately

        AzureCat.EnsureCreated(App.State.AzureStorageAccount);
        progressReport.SetIndeterminate();

        QueueCacheDownloads(chunkSize);

        while (m_cacheQueue.IsEmpty == false)
        {
            // and now download what we queued
            while (m_cacheQueue.TryDequeue(out MediaItem? item))
            {
                ICacheEntry cacheEntry = Entries[item.ID];

                if (cacheEntry.LocalPending)
                {
                    // first thing, unmark it since its no longer in our queue to download
                    // (if it fails, we will want to do it again...)
                    cacheEntry.LocalPending = false;
                    string fullLocalPath = GetFullLocalPath(cacheEntry.Path);
                    Task<bool> task = FEnsureMediaItemDownloadedToCache(item, fullLocalPath);

                    task.Wait();
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        MainWindow.LogForAsync(EventType.Warning, $"cache download canceled or failed: {task.Exception}");
                        _Workgroup.PushChangesToDatabase(null);
                        return false;
                    }
                    if (task.Result)
                    {

                        item.LocalPath = fullLocalPath;
                        item.IsCachePending = false;
                        cacheEntry.LocalPending = false;
                        cacheEntry.CachedDate = DateTime.Now;
                        item.NotifyCacheStatusChanged();
                    }
                }
            }

            _Workgroup.PushChangesToDatabase(null);

            // and do it again, until we don't have any left to cache
            QueueCacheDownloads(chunkSize);
        }
        progressReport.WorkCompleted();
        return true;
    }

    public void StartBackgroundCaching(int chunkSize)
    {
        AzureCat.EnsureCreated(App.State.AzureStorageAccount);

        App.State.AddBackgroundWork(
            "Populating cache from Azure",
            (progress) => DoCacheWork(progress, chunkSize));
//
//        QueueCacheDownloads(chunkSize);
//
//        // and now download
//        while (m_cacheQueue.TryDequeue(out MediaItem? item))
//        {
//            ICacheEntry cacheEntry = Entries[item.ID];
//
//            if (cacheEntry.LocalPending)
//            {
//                // first thing, unmark it since its no longer in our queue to download
//                // (if it fails, we will want to do it again...)
//                cacheEntry.LocalPending = false;
//                string fullLocalPath = GetFullLocalPath(cacheEntry.Path);
//                if (await FEnsureMediaItemDownloadedToCache(item, fullLocalPath))
//                {
//
//                    item.LocalPath = fullLocalPath;
//                    item.IsCachePending = false;
//                    cacheEntry.LocalPending = false;
//                    cacheEntry.CachedDate = DateTime.Now;
//                    item.NotifyCacheStatusChanged();
//                }
//            }
//        }
//
//        _Workgroup.PushChangesToDatabase(null);
    }

    public void PushChangesToDatabase(Dictionary<Guid, MediaItem>? itemsForCache)
    {
        _Workgroup.PushChangesToDatabase(itemsForCache);
    }
}
