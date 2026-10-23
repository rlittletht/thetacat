using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Thetacat.Explorer;
using Thetacat.Explorer.UI;
using Thetacat.Model;
using Thetacat.Types;

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

    public TriageDownload()
    {
    }

    void DownloadFullFidelityItems(IEnumerable<MediaItem> items, string targetDirectory)
    {
        List<string> itemsToDownload = new();

        foreach (MediaItem item in items)
        {
            string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

            if (path == null)
                throw new CatExceptionInternalFailure($"Failed to get cached full path for item {item.VirtualPath}");

            itemsToDownload.Add(path);
        }

        // copy all the items we have in the cache to the directory specified
        System.IO.Directory.CreateDirectory(targetDirectory);

        List<string> exists = new();

        foreach (string path in itemsToDownload)
        {
            string target = System.IO.Path.Combine(targetDirectory, System.IO.Path.GetFileName(path));

            if (Path.Exists(target))
            {
                if (!m_fReplaceExisting)
                {
                    exists.Add(target);
                    continue;
                }
            }

            System.IO.File.Copy(path, target, m_fReplaceExisting);
        }
        
        if (exists.Count > 0)
        {
            string joined = string.Join(",", exists);
            MessageBox.Show($"The following files already exist and were not replaced: {joined}", "Download", MessageBoxButton.OK);
        }
    }

    void CreateAndDownloadTriageItems(IEnumerable<MediaItem> items, string targetDirectory)
    {
        throw new CatExceptionInternalFailure("NYI");
    }

    public void DoDownload(IEnumerable<MediaItem> mediaItems, Window? parent)
    {
        string downloads = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        if (!DownloadMedia.FPrompt(
                App.State.ActiveProfile.LastDownloadLocation ?? downloads,
                parent,
                out string? directory,
                out m_fTriage,
                out m_fReplaceExisting))
            return;

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
            DownloadFullFidelityItems(itemsToDownload, directory!);
            return;
        }

        CreateAndDownloadTriageItems(itemsToDownload, directory!);
    }

    public static void DownloadItems(IEnumerable<MediaItem> mediaItems, Window? parent)
    {
        TriageDownload download = new TriageDownload();

        download.DoDownload(mediaItems, parent);
    }
}
