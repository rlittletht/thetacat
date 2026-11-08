using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Thetacat.Explorer;
using Thetacat.Explorer.UI;
using Thetacat.Logging;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Client;
using Thetacat.Model.ImageCaching;
using Thetacat.Model.Mediatags;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.UI;
using Path = System.IO.Path;

// the plan here is to get all the items from the Triage cache. for every item we couldn't get, we will queue it (like queuepreviewimagecacheloadformediaitems)
// and then we wait for all the callbacks to happen. we will do this on a background thread and either:
// wait for the producer/consumer queue to be empty (probably need to expose that method)
// have a count of itmes we queued and as each item gets an image update cache notification we decrement it
// or we start with a mapping of MediaItem -> Derivative path and update those paths as we get notifications and when we've got all
//    the paths, we're done.

// the interesting thing here is we don't actually need the bitmap (so we don't actually need the image in the image cache)
// we just need a path to the derivative item. so we *could* work directly with the derivatives and see if we can bypass loading
// the image entirely. 

// but what if we have to create a new derivative? we don't need it in the image cache, but that's the easiest way to 
// get it to populate the derivatives. maybe let it populate, then purge the cache when we're done?

// if we write the code to first check the derivatives for a match, then the next time shouldn't have to do any image work...

namespace Thetacat.Triage;

public class TriageDownload
{
    private ConcurrentDictionary<Guid, MediaItem> m_pendingDerivatives = new ConcurrentDictionary<Guid, MediaItem>();
    private bool m_fTriage = false;
    private bool m_fReplaceExisting = false;
    private bool m_fAppendMetadata = false;
    private List<Metatag> m_metadataToExport;
    
    List<string> m_exists = new();
    private string m_targetDirectory = string.Empty;
    private DownloadFilenameType m_filenameType = DownloadFilenameType.LeafOnly;

    private ConcurrentBag<string> m_hashtags = new();
    
    public TriageDownload()
    {
    }

    private static void UpdateCacheUpdateForDerivativeItemSaved(OnDerivativeWorkCompleteArgs args)
    {
        if (App.State.TriageCache.Items.TryGetValue(args.MediaId, out ImageCacheItem? cacheItem))
        {
            if (cacheItem.IsLoadQueued || cacheItem.Image == null)
            {
                App.LogForApp(EventType.Critical, $"trying to purge and reload an item that isn't loaded {args.MediaId}");
                return;
            }
            
            // all we have to do is clear the bitmap since we have a derivative path
            if (args.DerivativePath == null)
                throw new CatExceptionInternalFailure("should have derivative path on reload in triage cache");

            cacheItem.ResetBitmapAndSetDerivativePath(args.DerivativePath);
        }
    }

    public void QueueTriageCacheUpdate(IEnumerable<MediaItem> mediaItems)
    {
        ICache cache = App.State.Cache;

        foreach (MediaItem mediaItem in mediaItems)
        {
            string? path = cache.TryGetCachedFullPath(mediaItem.ID);

            if (path == null)
                throw new CatExceptionInternalFailure($"couldn't get cached full path for media item {mediaItem}");

            if (!m_pendingDerivatives.TryAdd(mediaItem.ID, mediaItem))
            {
                throw new CatExceptionInternalFailure($"added media item {mediaItem} more than once to pending derivatives");
            }

            App.State.TriageCache.TryQueueBackgroundLoadToCache(
                mediaItem,
                App.State.GetMD5ForItem(mediaItem.ID),
                path,
                UpdateCacheUpdateForDerivativeItemSaved);
        }
    }

    bool IsStandardUserOrIptc(string standard)
    {
        return standard == MetatagStandards.User.StandardTag || standard == MetatagStandards.KnownStandards[MetatagStandards.Standard.Iptc].StandardTag;
    }
    
    List<string> ConvertMetadataToHashtags(MediaItem item)
    {
        List<string> hashtags = new();
        foreach (MediaTag tag in item.MediaTags)
        {
            if (!IsStandardUserOrIptc(tag.Metatag.Standard))
                continue;

            if (m_metadataToExport.Count > 0)
            {
                // must match one of these items
                bool matched = false;
                
                foreach (Metatag tagToExport in m_metadataToExport)
                {
                    if (tag.Metatag.ID == tagToExport.ID)
                    {
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                    continue;
            }
            
            string value = tag.Value != null ? $"({tag.Value})" : string.Empty;
            string hashtag = $"#{tag.Metatag.Standard}:{tag.Metatag.Name}{value}";

            hashtags.Add(hashtag);
        }

        return hashtags;
    }

    void DownloadCacheItem(ImageCacheItem cacheItem, MediaItem item)
    {
        string? savedFilename;
        
        if (cacheItem.DerivativePath != null)
        {
            savedFilename = DownloadSinglePathItem(cacheItem.DerivativePath.Local, m_targetDirectory, item);
        }
        else
        {
            // we only have a bitmap. need to save it...
            savedFilename = SaveBitmapSourceToTarget(cacheItem.Image!, m_targetDirectory, item);
        }

        if (m_fAppendMetadata && savedFilename != null)
        {
            List<string> hashtags = ConvertMetadataToHashtags(item);

            string leaf = Path.GetFileName(savedFilename);
            string line = $"\"{leaf}\",\"{string.Join(",", hashtags)}\"";

            m_hashtags.Add(line);
        }
    }

    private void OnImageCacheUpdated(object? sender, ImageCacheUpdateEventArgs e)
    {
        ImageCache? cache = sender as ImageCache;
        if (cache == null)
            throw new CatExceptionInternalFailure("sender wasn't an image cache in OnImageCacheUpdated");

        if (cache.Items.TryGetValue(e.MediaId, out ImageCacheItem? cacheItem))
        {
            App.LogForAsync(EventType.Verbose, $"Done caching {cacheItem.MediaId}");

            cacheItem.IsLoadQueued = false;
            if (m_pendingDerivatives.TryGetValue(e.MediaId, out MediaItem? item))
            {
                DownloadCacheItem(cacheItem, item);

                // either way, remove it from our remaining list
                m_pendingDerivatives.TryRemove(e.MediaId, out MediaItem? itemDeleted);
            }
        }
    }

    string GetTargetFilename(string targetDirectory, MediaItem item)
    {
        if (m_filenameType == DownloadFilenameType.LeafOnly)
        {
            return System.IO.Path.Combine(targetDirectory, item.VirtualPath.GetLeafItem()!);
        }
        else if (m_filenameType == DownloadFilenameType.FlattenedPath)
        {
            string flattenedName = item.VirtualPath.CreateFlatFilename();
            return System.IO.Path.Combine(targetDirectory, flattenedName);
        }
        else
        {
            throw new CatExceptionInternalFailure($"Unsupported filename type: {m_filenameType}");
        }
    }

    string GetTargetFilenameFromParts(string targetDirectory, MediaItem item, string? ext = null)
    {
        string target = GetTargetFilename(targetDirectory, item);
        // make sure the extensions match

        if (ext != null)
            target = Path.ChangeExtension(target, ext);

        return target;

    }
    
    string? DownloadSinglePathItem(string path, string targetDirectory, MediaItem item)
    {
        string? ext = Path.GetExtension(path);
        string target = GetTargetFilenameFromParts(targetDirectory, item, ext);

        if (Path.Exists(target))
        {
            if (!m_fReplaceExisting)
            {
                m_exists.Add(target);
                return null;
            }
        }

        System.IO.File.Copy(path, target, m_fReplaceExisting);
        return target;
    }

    string? SaveBitmapSourceToTarget(BitmapSource bitmap, string targetDirectory, MediaItem item)
    {
        string target = GetTargetFilenameFromParts(targetDirectory, item, "jpg");

        if (Path.Exists(target))
        {
            if (!m_fReplaceExisting)
            {
                m_exists.Add(target);
                return null;
            }

            File.Delete(target);
        }

        Derivatives.SaveBitmapSourceDerivativeToFile(bitmap, target, 90 /*quality*/);
        return target;
    }

    void DownloadFullFidelityItems(IEnumerable<MediaItem> items)
    {
        // copy all the items we have in the cache to the directory specified
        System.IO.Directory.CreateDirectory(m_targetDirectory);

        foreach (MediaItem item in items)
        {
            string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

            if (path == null)
                throw new CatExceptionInternalFailure($"Failed to get cached full path for item {item.VirtualPath}");

            DownloadSinglePathItem(path, m_targetDirectory, item);
        }
    }

    void CreateAndDownloadTriageItems(IEnumerable<MediaItem> items, Window? parent)
    {
        App.State.TriageCache.ImageCacheUpdated += OnImageCacheUpdated;

        List<MediaItem> itemsToCache = new();

        foreach (MediaItem item in items)
        {
            ImageCacheItem? cacheItem = App.State.TriageCache.GetAnyExistingItem(item.ID);

            if (cacheItem != null)
            {
                DownloadCacheItem(cacheItem!, item);
            }
            else
            {
                itemsToCache.Add(item);
            }
        }

        QueueTriageCacheUpdate(itemsToCache);

        int startingSize = m_pendingDerivatives.Count;

        ProgressDialog.DoWorkWithProgress((IProgressReport progress) =>
                                          {
                                              // loop until our derivative list is empty
                                              while (!this.m_pendingDerivatives.IsEmpty)
                                              {
                                                  progress.UpdateProgress(((double)(startingSize - this.m_pendingDerivatives.Count) * 100.0) / (double)startingSize);
                                                  Thread.Sleep(100);
                                              }

                                              App.State.TriageCache.ImageCacheUpdated -= OnImageCacheUpdated;
                                              progress.WorkCompleted();
                                          });
    }

    public void DoDownload(IEnumerable<MediaItem> mediaItems, Window? parent)
    {
        if (!DownloadMedia.FPrompt(
                parent,
                out string? directory,
                out m_fTriage,
                out m_fReplaceExisting,
                out m_fAppendMetadata,
                out m_filenameType,
                out m_metadataToExport))
        {
            return;
        }

        m_targetDirectory = directory!;

        // first, preflight to make sure all the media is in the cache
        List<string> uncachedItems = new();
        List<MediaItem> itemsToDownload = new();

        foreach (MediaItem item in mediaItems)
        {
            string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

            if (path == null)
            {
                uncachedItems.Add(item.VirtualPath);
            }
            else
            {
                itemsToDownload.Add(item);
            }
        }

        if (uncachedItems.Count > 0)
        {
            string joined = string.Join(",", uncachedItems);

            if (MessageBox.Show($"Cannot download uncached items: {joined}", "Download", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
        }

        if (!m_fTriage)
        {
            DownloadFullFidelityItems(itemsToDownload);
        }
        else
        {
            CreateAndDownloadTriageItems(itemsToDownload, parent);

            if (m_fAppendMetadata)
            {
                string metadataPath = System.IO.Path.Combine(m_targetDirectory, "metadata.csv");
                System.IO.File.AppendAllLines(metadataPath, m_hashtags);
            }
        }

        if (m_exists.Count > 0)
        {
            string joined = string.Join(",", m_exists);
            MessageBox.Show($"The following files already exist and were not replaced: {joined}", "Download", MessageBoxButton.OK);
        }
    }

    public static void DownloadItems(IEnumerable<MediaItem> mediaItems, Window? parent)
    {
        TriageDownload download = new TriageDownload();

        download.DoDownload(mediaItems, parent);
    }
}
