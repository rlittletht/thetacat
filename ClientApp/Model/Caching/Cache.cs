using System;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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

namespace Thetacat.Model.Caching;

/*----------------------------------------------------------------------------
    %%Class: Cache
    %%Qualified: Thetacat.Model.Cache

    This is the local cache (private or workgroup -- private is NYI) for the
    catalog. If you need to look at a copy of the real image, this is where
    you look.

    If an item is marked as "don't copy to cloud", then the cache is the only
    copy of the item. Otherwise, these are all offline copies of what is in
    azure storage.

    TODO: In the future, we will look for local changes to files that make them
    different than what is in azure storage -- this will allow us to notice
    offline editing and signal that we should update metadata, hashes, and
    upload a new copy to azure storage.
----------------------------------------------------------------------------*/
public class Cache : ICache
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
        if (string.Compare(value, "private", StringComparison.InvariantCultureIgnoreCase) == 0)
            return CacheType.Private;
        else if (string.Compare(value, "workgroup", StringComparison.InvariantCultureIgnoreCase) == 0)
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

    void ConnectToWorkgroupCache(Profile settings)
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

    public void DeleteMediaItem(Guid id)
    {
        if (Entries.TryGetValue(id, out ICacheEntry? entry))
        {
            string localPath = GetFullLocalPath(entry.Path);

            if (File.Exists(localPath))
                File.Delete(localPath);

            Entries.TryRemove(id, out ICacheEntry? _);
            _Workgroup.DeleteMediaItem(id);
        }
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
    public Cache(Profile? settings)
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

    public static bool OkToUseLocalPathForItem(PathSegment fullPath, string md5, out bool exists)
    {
        exists = false;

        if (Path.Exists(fullPath.Local))
        {
            if (string.IsNullOrEmpty(md5))
                return false;

            exists = true;
            // yikes. file already exists
            // last chance...is it already the file we want? (check the MD5 hash)

            // if the MD5 matches, then the cache is already done. its ok to use
            // this name. when we see the file already exists in the future we
            // will know its OK to assume its the same file
            return MediaItem.CalculateMD5Hash(fullPath.Local) == md5;
        }

        return true;
    }

    public static PathSegment EnsureUniqueLocalCacheVirtualPath(PathSegment localPathToCacheRoot, MediaItem item)
    {
        PathSegment? unique = EnsureUniqueLocalCacheVirtualPath(localPathToCacheRoot, item.VirtualPath, item.MD5, item.MimeType, true, out bool exists);

        if (unique == null)
            throw new CatExceptionInternalFailure("couldn't generate unique name when guid was allowed!");

        return unique;
    }

    public delegate bool IsPathOkToUseDelegate(PathSegment test, string md5, out bool exists);

    public static PathSegment? GetUniqueLocalNameDerivative(PathSegment existingFile, string? suffix, IsPathOkToUseDelegate? pathOk = null)
    {
        pathOk ??= OkToUseLocalPathForItem;

        if (pathOk(existingFile, string.Empty, out _))
            throw new CatExceptionInternalFailure("trying to get unique name for existing item that doesn't exist");

        string extension = Path.GetExtension(existingFile);

        PathSegment check = new PathSegment(existingFile);
        PathSegment baseCheck;

        if (string.IsNullOrEmpty(suffix))
            suffix = "";
        else
            suffix = $"-{suffix}";

        Regex rex = new Regex($"^(.*){suffix}\\((\\d+)\\){extension}$", RegexOptions.IgnoreCase);

        MatchCollection matches = rex.Matches(check);

        int nextCount = 1;

        if (matches.Count == 0)
        {
            // no suffix yet
            baseCheck = new PathSegment(Path.ChangeExtension(check, null));
        }
        else
        {
            if (matches[0].Groups.Count != 3)
                throw new CatExceptionInternalFailure("bad match count");

            nextCount = int.Parse(matches[0].Groups[2].Value) + 1;
            baseCheck = new PathSegment(matches[0].Groups[1].Value);
        }

        check = new PathSegment($"{baseCheck}{suffix}({nextCount}){extension}");
        while (!pathOk(check, string.Empty, out _) && nextCount < 100)
        {
            nextCount++;
            check = new PathSegment($"{baseCheck}{suffix}({nextCount}){extension}");
        }

        if (nextCount == 100)
            return null;

        return check;
    }

    public static PathSegment? EnsureUniqueLocalCacheVirtualPath(
        PathSegment localPathToCacheRoot, PathSegment virtualPath, string itemMD5, string? mimeType, bool okToUseGuid, out bool exists)
    {
        exists = false;

        if (okToUseGuid && mimeType == null)
            throw new CatExceptionInternalFailure("must provide mimetype if guid is OK");

        // easiest would be to use the virtual path

        if (localPathToCacheRoot == $"{Workgroup.s_mockServer}{Workgroup.s_mockRoot}")
            return virtualPath;

        // if the virtual path is rooted, we can't use it
        // just use a guid if we're allowed to
        if (Path.IsPathRooted(virtualPath.Local) || virtualPath == PathSegment.Empty)
        {
            if (!okToUseGuid)
                return null;

            return new PathSegment(Path.ChangeExtension(Guid.NewGuid().ToString(), MimeTypesMap.GetExtension(mimeType)));
        }

        PathSegment test = PathSegment.Join(localPathToCacheRoot, virtualPath);

        int count = 0;

        while (!OkToUseLocalPathForItem(test, itemMD5, out exists))
        {
            // if we get to 50 collisions, give up and just use a guid
            if (count > 50)
            {
                if (!okToUseGuid)
                    return null;

                exists = false;
                return new PathSegment(Path.ChangeExtension(Guid.NewGuid().ToString(), MimeTypesMap.GetExtension(mimeType)));
            }

            test = PathSegment.Join(localPathToCacheRoot, virtualPath.AppendLeafSuffix($"({++count})"));
        }

        if (count == 0)
            return virtualPath;

        return virtualPath.AppendLeafSuffix($"({count})");
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
            MainWindow.LogForAsync(EventType.Information, $"downloaded {item.ID} to {destination}");
            return true;
        }

        return false;
    }

    public PathSegment GetRelativePathToCacheRootFromFullPath(PathSegment fullLocal)
    {
        return PathSegment.GetRelativePath(LocalPathToCacheRoot, fullLocal);
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
        _Workgroup.CreateCacheEntryForItem(this, item, DateTime.Now, false);
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

    /*----------------------------------------------------------------------------
        %%Function: UpdateCacheForMd5Change
        %%Qualified: Thetacat.Model.Caching.Cache.UpdateCacheForMd5Change

        We know the MD5 value for this item has changed locally (and hence in the
        workgroup). mark this so we will update it
    ----------------------------------------------------------------------------*/
    public void UpdateEntryForMd5Change(Guid id, string md5)
    {
        // get the cache entry
        if (!Entries.TryGetValue(id, out ICacheEntry? cacheEntry))
            throw new CatExceptionInternalFailure("no cache entry for update cache entry");

        cacheEntry.MD5 = md5;
        cacheEntry.CachedBy = _Workgroup.ClientId;
        cacheEntry.CachedDate = DateTime.Now;
    }


    /*----------------------------------------------------------------------------
        %%Function: IsCachePathItemLikeVirtualPathItem
        %%Qualified: Thetacat.Model.Cache.IsCachePathItemLikeVirtualPathItem

        This is clever but unused...it will determine if the virtual path item
        and the cached path item are the same, or only tweaked by a uniquification
        (addition of (1), etc)
    ----------------------------------------------------------------------------*/
    public static bool IsCachePathItemLikeVirtualPathItem(string localPathToCacheRoot, PathSegment cachedPath, PathSegment virtualPath)
    {
        if (!localPathToCacheRoot.EndsWith('\\'))
            localPathToCacheRoot = $"{localPathToCacheRoot}\\";

        // if this local path doesn't start with our cache root, then we can't match anything
        if (!cachedPath.Local.StartsWith(localPathToCacheRoot, StringComparison.CurrentCultureIgnoreCase))
            return false;

        cachedPath = new PathSegment(cachedPath.ToString().Substring(localPathToCacheRoot.Length));

        if (string.Compare(cachedPath, virtualPath, StringComparison.InvariantCultureIgnoreCase) == 0)
            return true;

        PathSegment withoutLeaf = cachedPath.GetPathDirectory();
        PathSegment? leaf = cachedPath.GetLeafItem();

        if (leaf != null)
        {
            // remove the extension
            string extension = Path.GetExtension(leaf);

            string localLeafPathWithoutExtension = Path.ChangeExtension(leaf, null);

            // now see if we can remove a trailing (*) suffix
            int index = localLeafPathWithoutExtension.LastIndexOf('(');

            if (index != -1)
            {
                localLeafPathWithoutExtension = localLeafPathWithoutExtension.Substring(0, index);

                cachedPath = PathSegment.Join(withoutLeaf, $"{localLeafPathWithoutExtension}{extension}");

                if (string.Compare(cachedPath, virtualPath, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return true;
            }
        }

        return false;
    }

    /*----------------------------------------------------------------------------
        %%Function: IsCachePathLikeVirtualPath
        %%Qualified: Thetacat.Model.Cache.IsCachePathLikeVirtualPath

        This tests if the cachePath and the virtualPath items are in the same
        directory, so repathing is pointless
    ----------------------------------------------------------------------------*/
    public static bool IsCachePathLikeVirtualPath(string localPathToCacheRoot, PathSegment cachedPath, PathSegment virtualPath)
    {
        if (!localPathToCacheRoot.EndsWith('\\'))
            localPathToCacheRoot = $"{localPathToCacheRoot}\\";

        // if this local path doesn't start with our cache root, then we can't match anything
        if (!cachedPath.Local.StartsWith(localPathToCacheRoot, StringComparison.CurrentCultureIgnoreCase))
            return false;

        cachedPath = new PathSegment(cachedPath.ToString().Substring(localPathToCacheRoot.Length));

        return IsCachePathLikeVirtualPath(cachedPath, virtualPath);
    }

    public static bool IsCachePathLikeVirtualPath(PathSegment cachedPath, PathSegment virtualPath)
    {
        if (string.Compare(cachedPath, virtualPath, StringComparison.InvariantCultureIgnoreCase) == 0)
            return true;

        PathSegment withoutLeaf = cachedPath.GetPathDirectory();
        PathSegment virtualPathWithoutLeaf = virtualPath.GetPathDirectory();

        if (string.Compare(withoutLeaf, virtualPathWithoutLeaf, StringComparison.InvariantCultureIgnoreCase) == 0)
            return true;

        return false;
    }


    public void MoveRepathedItems()
    {
        bool fPreflight = true;

        while (true)
        {
            foreach (Guid key in Entries.Keys)
            {
                ICacheEntry entry = Entries[key];

                if (!App.State.Catalog.TryGetMedia(entry.ID, out MediaItem? item))
                {
                    MainWindow.LogForApp(EventType.Critical, $"Could not find item: {entry.ID} in catalog for repath check. skipping");
                    continue;
                }

                if (!IsCachePathLikeVirtualPath(entry.Path, item.VirtualPath))
                {
                    // move this to the remapped path

                    // take the virtualpath and use our EnsureUnique algorithm above to get the
                    // item path that we want to move it to...
                    PathSegment? repathed = EnsureUniqueLocalCacheVirtualPath(LocalPathToCacheRoot, item.VirtualPath, item.MD5, null, false, out bool exists);

                    if (repathed == null)
                    {
                        // can't move
                        continue;
                    }

                    string localSource = GetFullLocalPath(entry.Path);
                    string localTarget = GetFullLocalPath(repathed);

                    if (!exists)
                    {
                        if (!fPreflight)
                        {
                            string? directory = Path.GetDirectoryName(localTarget);
                            if (directory != null && !Directory.Exists(directory))
                                Directory.CreateDirectory(directory);
                        }

                        if (fPreflight)
                        {
                            if (!File.Exists(localSource))
                                MessageBox.Show($"preflight: could not locate local source {localSource}");
                        }
                        else
                        {
                            try
                            {
                                File.Move(localSource, localTarget);
                            }
                            catch (Exception exc)
                            {
                                MessageBox.Show($"could not move {localSource} to {localTarget}: {exc.Message}");
                            }
                        }
                    }
                    else
                    {
                        // file already exists and its the file we want. just delete the source
                        if (!fPreflight)
                            File.Delete(localSource);
                    }

                    if (!fPreflight)
                    {
                        // update the cache entry
                        WorkgroupCacheEntry _entry = entry as WorkgroupCacheEntry ?? throw new CatExceptionInternalFailure("can't repath non workgroup cache");
                        _entry.Path = repathed;
                    }
                }
            }

            if (!fPreflight)
                break;

            fPreflight = false;
        }

        _Workgroup.PushChangesToDatabase(null);
    }

    public bool DoCacheWork(IProgressReport progressReport, int chunkSize)
    {
        // this is an indeterminate progress report, so just report infinitely

        AzureCat.EnsureCreated(App.State.AzureStorageAccount);
        progressReport.SetIndeterminate();

        // first, fixup the paths of any cached items that have been virtual repathed
        MoveRepathedItems();

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
    }

    public void PushChangesToDatabase(Dictionary<Guid, MediaItem>? itemsForCache)
    {
        _Workgroup.PushChangesToDatabase(itemsForCache);
    }
}
