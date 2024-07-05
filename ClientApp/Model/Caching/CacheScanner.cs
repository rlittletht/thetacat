using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
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

    FileInfo? GetFileInfoForFile(PathSegment filePath)
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
        %%Function: ScanForLocalChanges
        %%Qualified: Thetacat.Model.Cache.ScanForLocalChanges

        Scan all of the cached items to see if they have changed locally, and if
        so, record that for later.

        This should be suitable for a background thread
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
                    // record file missing here
                    continue;
                }

                string lastKnownMd5 = "";

                if (md5Cache.TryLookupCacheItem(fullPath.Local, out Md5CacheItem? md5Item))
                {
                    // yay, there are some quick things we can compare to see if it changed
                    if (md5Item.MatchFileInfo(fileInfo))
                    {
                        // no change
                        continue;
                    }

                    // we'll need to do an MD5 check
                    lastKnownMd5 = md5Item.MD5;
                }

                if (!string.IsNullOrEmpty(lastKnownMd5))
                {
                    // make sure the MD5 from the cache matches the media item
                    if (lastKnownMd5 != item.MD5 && !string.IsNullOrEmpty(item.MD5))
                    {
                        MessageBox.Show($"MD5 mismatch cache and mediaitem");
                        lastKnownMd5 = item.MD5;
                    }
                }
                else
                {
                    // we don't have a lastKnown, so take it from the media item
                    lastKnownMd5 = item.MD5;
                }

                // now calc the md5 synchronously -- don't use the cache since we are explicitly trying to
                // see if the file changed

                string md5Current = Checksum.GetMD5ForPathSync(fullPath.Local);

                if (md5Current == lastKnownMd5)
                {
                    md5Cache.UpdateCacheFileInfo(fullPath.Local, fileInfo, md5Current);
                    continue;
                }

                // file changed!
                MessageBox.Show($"File changed for {fullPath}");


                // TODO:  Make the list of deltas to be processed (changes and deletes/missing)
                // When processing these, we need to update the metadata and MD5 for the mediaitem
                // AND we need to update the MD5 cache for the file so the next scan is fast.
            }
        }

    }
}
