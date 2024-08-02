using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using Thetacat.Import;
using Thetacat.Logging;
using Thetacat.Model.Md5Caching;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Caching;

public class CacheScanner
{
    private Dictionary<PathSegment, IReadOnlyCollection<FileInfo>> m_dirMap = new();

    public void EnsureDirectoryLoaded(PathSegment dir, bool fRecurse = false)
    {
        if (m_dirMap.ContainsKey(dir))
            return;

        string local = dir.Local;

        if (!Directory.Exists(local))
            return;

        DirectoryInfo info = new DirectoryInfo(local);
        FileInfo[] files = info.GetFiles();

        m_dirMap.Add(dir, files);

        if (fRecurse)
        {
            DirectoryInfo[] dirs = info.GetDirectories();

            foreach (DirectoryInfo subdir in dirs)
            {
                EnsureDirectoryLoaded(new PathSegment(subdir.FullName));
            }
        }
    }

    public void EnsureDirectoryLoadedForFile(PathSegment fullFilePath, bool fRecurse = false)
    {
        PathSegment dirPath = fullFilePath.GetPathDirectory();

        EnsureDirectoryLoaded(dirPath, fRecurse);
    }

    public FileInfo? GetFileInfoForFile(PathSegment filePath)
    {
        PathSegment dir = filePath.GetPathDirectory();
        PathSegment? fileName = filePath.GetFilename();

        if (fileName == null)
            return null;

        if (!m_dirMap.TryGetValue(dir, out IReadOnlyCollection<FileInfo>? files))
            return null;

        foreach (FileInfo file in files)
        {
            if (string.Compare(file.Name, fileName.Local, StringComparison.OrdinalIgnoreCase) == 0)
                return file;
        }

        return null;
    }


    /*----------------------------------------------------------------------------
        %%Function: ProcessCacheDeltas
        %%Qualified: Thetacat.Model.Caching.CacheScanner.ProcessCacheDeltas

        Here's the plan. When an item changes, the content in the cache location
        is now different (with a new MD5). Any places with derivatives or copies
        of this picture are now wrong:

        * tcat_imports - Holds info when this was imported. We are making a
          "new import", so we have to flip the state back to upload pending

        * tcat_workgroup_media - The workgroup cache is the thing we noticed
          changed. need to update the workgroup MD5 to match the new value. we do
          this immediately so our workgroup has the updated content

        (done)
        * tcat_media - This is the master media store reflecting the latest source
          of truth, but MUST reflect what is in azure (if its there). If the 


        PRIVATE CACHES: (done)
        * tcat_md5cache - This has already been dealt with. Other clients will lazily
          update their MD5 cache because the file size/last modified time won't match
        * tcat_derivatives - once the MD5 for the media item changes,  the derivatives
          will automatically 'expire'.

        RUNTIME CACHES: (done)
        * App.State.ImageCache
        * App.State.PreviewImageCache
             Both of these hold on to cached images for explorer and zoom windows.
             We need to purge them of any changed items and force them to be
             reloaded

        DONE: We have to make sure that other workgroups cache the new item.
        (GetNextItemsForQueueFromMediaCollection trys to find the next items that
        need to be cached. Need to extend this to allow items that have already
        been cached, but have a different MD5 -- so even if the cache item exists
        and its not pending and the media is active, if the MD5 mismatches, then
        we need to treat it as if it didn't exist. (Flip the cache state back
        to pending, and set the clientID to the current client as the one responsible
        for downloading it.  This probably means extending the "stake claim on this"
        to not just add items to the cache, but also to reset items in the cache.
    ----------------------------------------------------------------------------*/
    void ProcessCacheDeltas(IReadOnlyCollection<CacheItemDelta> deltas)
    {
        MediaImporter importer = new MediaImporter(App.State.ActiveProfile.CatalogID);

        foreach (CacheItemDelta delta in deltas)
        {
            if (delta.DeltaType == DeltaType.Changed)
            {
                // must update the workgroup item before purging caches -- otherwise
                // we don't properly invalidate the caches (the derivative items won't
                // notice an MD5 change)
                App.State.Cache.UpdateEntryForMd5Change(delta.MediaItem.ID, delta.MD5);

                // purge image caches for this id (force reload in this session)
                App.State.ImageCache.ResetImageForKey(delta.MediaItem.ID);
                App.State.PreviewImageCache.ResetImageForKey(delta.MediaItem.ID);

                // find the import item for this and mark it upload pending
                // also update the media item to be in a pending state as well
                importer.UpdateImportItemForMd5Change(delta.MediaItem, delta.FullPath);

                if (delta.MediaItem.State == MediaItemState.Pending)
                    // it hasn't been uploaded yet. update the MD5c
                    delta.MediaItem.MD5 = delta.MD5;
                
                // otherwise, leave the MD5 in the catalog alone so we can notice
                // to update it later
            }
        }
    }


    /*----------------------------------------------------------------------------
        %%Function: ScanForLocalChanges
        %%Qualified: Thetacat.Model.Cache.ScanForLocalChanges

        Scan all of the cached items to see if they have changed locally, and if
        so, record that for later.

        This should be suitable for a background thread

        Here's the plan. Scan every item we know about in our workgroup cache.
        Check the local file's MD5 (via the MD5 cache, or if that isn't suitable,
        calculate a new one and store it in the MD5 cache).

        If the workgroup MD5 value doesn't match the file, then:
        * If the local MD5 matches media MD5, then just update the workgroup DB
          (this is weird -- somehow the workgroup DB got set wrong? maybe an
           aborted download refreshing the media but didn't get to update the
           the workgroup db? in any case, fix the workgroup DB)
        * If the local MD5 is different than the media, then we need to update
          the workgroup DB.  A future process will notice that the workgroup DB
          does't match the media DB
    ----------------------------------------------------------------------------*/
    public void ScanForLocalChanges(ICache cache, Md5Cache md5Cache, ScanCacheType scanType)
    {
        // grab a snapshot of the item id's in the cache
        Dictionary<int, List<MediaItem>> scanBuckets = new();

        scanBuckets[0] = new List<MediaItem>();
        scanBuckets[1] = new List<MediaItem>();
        scanBuckets[2] = new List<MediaItem>();

        // since Entries is a concurrent dictionary, this enumeration will
        // automatically grab a snapshot
        foreach (Guid mediaId in cache.Entries.Keys)
        {
            if (!App.State.Catalog.TryGetMedia(mediaId, out MediaItem? item))
                continue;

            if (item.VersionStack != null)
                scanBuckets[0].Add(item);
            else if (item.MediaStack != null)
                scanBuckets[1].Add(item);
            else if (scanType != ScanCacheType.Predictive)
                // don't add pri 2 items if we are doing a predictive scan
                scanBuckets[2].Add(item);
        }

        List<CacheItemDelta> deltas = new();

        for (int iBucket = 0; iBucket < scanBuckets.Count; iBucket++)
        {
            List<MediaItem> bucket = scanBuckets[iBucket];

            foreach (MediaItem item in bucket)
            {
                // get the cache entry for this
                string? localfile = cache.TryGetCachedFullPath(item.ID);

                if (localfile == null)
                    continue;

                PathSegment fullPath = new PathSegment(localfile);

                EnsureDirectoryLoadedForFile(fullPath, false);

                FileInfo? fileInfo = GetFileInfoForFile(fullPath);

                if (fileInfo == null)
                {
                    deltas.Add(new CacheItemDelta(DeltaType.Deleted, item, fullPath, string.Empty));
                    continue;
                }

                string md5Current = "";

                if (md5Cache.TryLookupCacheItem(fullPath.Local, out Md5CacheItem? md5Item)
                    && md5Item.MatchFileInfo(fileInfo))
                { 
                    // we can use the current MD5 from the md5cache -- don't need to recalc
                    md5Current = md5Item.MD5;
                }
                else
                {
                    md5Current = Checksum.GetMD5ForPathSync(fullPath.Local);
                }


                if (md5Current != item.MD5)
                {
                    // don't alert if the media didn't have an md5 before
                    if (!string.IsNullOrEmpty(item.MD5))
                        MainWindow.LogForApp(EventType.Error, $"md5 mismatch on scan for media: {item.VirtualPath}");

                    deltas.Add(new CacheItemDelta(DeltaType.Changed, item, fullPath, md5Current));
                }

                // no matter what, update the md5 cache since we might have a new md5 hash
                md5Cache.UpdateCacheFileInfoIfNecessary(fullPath.Local, fileInfo, md5Current);
            }
        }

        // at this point we have a list of all the changes in the database. need to deal with them
        ProcessCacheDeltas(deltas);

        App.State.Cache.PushChangesToDatabase(null /*itemsForCache*/);
    }
}
