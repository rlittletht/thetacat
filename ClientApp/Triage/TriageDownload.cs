using System;
using System.Collections.Generic;
using System.Windows;
using Thetacat.Explorer;
using Thetacat.Explorer.UI;
using Thetacat.Model;

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
    public static void DownloadItems(IEnumerable<MediaItem> mediaItems, Window? parent)
    {
        string downloads = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        if (DownloadMedia.FPrompt(App.State.ActiveProfile.LastDownloadLocation ?? downloads, parent, out string? directory, out bool triage))
        {
            // first, preflight to make sure all the media is in the cache
            List<string> uncachedItems = new();
            List<string> itemsToDownload = new();

            foreach (MediaItem item in mediaItems)
            {
                string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

                if (path == null)
                {
                    uncachedItems.Add(item.VirtualPath);
                }
                else
                {
                    itemsToDownload.Add(path);
                }
            }

            if (uncachedItems.Count > 0)
            {
                string joined = string.Join(",", uncachedItems);

                if (MessageBox.Show($"Cannot download uncached items: {joined}", "Download", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                    return;
            }

            // now make sure we have triage cache entries for every item
            if (triage)
            {
                
            }
            // copy all the items we have in the cache to the directory specified
            System.IO.Directory.CreateDirectory(directory);

            foreach (string path in itemsToDownload)
            {
                string target = System.IO.Path.Combine(directory, System.IO.Path.GetFileName(path));
                System.IO.File.Copy(path, target);
            }
        }
    }
}
