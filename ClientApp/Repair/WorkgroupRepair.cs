using System;
using System.Collections.Generic;
using System.Windows;
using NUnit.Framework;
using Thetacat.Model;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;

namespace Thetacat.Repair;

public class WorkgroupRepair
{
    
    /*----------------------------------------------------------------------------
        %%Function: FindMissingWorkgroupEntries
        %%Qualified: Thetacat.Repair.WorkgroupRepair.FindMissingWorkgroupEntries

        When an item is added to the catalog, it will be added with 
        state = pending.  This means that the client still has to upload the
        media to the cloud. The client that added it will ALSO add an entry to
        their workgroup with their client name in the 'cachedBy' field.

        this way, the client that added the item retains responsibility for
        uploading the media to the cloud (and marking the media as active).

        If the import process crashes between the catalog update and the
        workgroup update, then the items will stay in the cloud forever in a
        pending state. no workgroup will know about the media, so no client will
        ever complete the process.

        This is hard to recover from since we don't really know which client is
        responsible for uploading, but there are a few things we can do to try
        to reattach.
    ----------------------------------------------------------------------------*/
    public static void FixMissingWorkgroupEntries(ICatalog catalog)
    {
        int cPendingMediaItems = 0;
        List<MediaItem> missingItems = new();

        App.State.Cache._Workgroup.RefreshWorkgroupMedia(App.State.Cache.Entries);

        foreach (MediaItem item in catalog.GetMediaCollection())
        {
            if (item.State == MediaItemState.Pending)
            {
                cPendingMediaItems++;
                if (!App.State.Cache.Entries.TryGetValue(item.ID, out ICacheEntry? entry))
                    missingItems.Add(item);
            }
        }

        // now see if we match up with any virtual paths

        MessageBox.Show($"Total pending items: {cPendingMediaItems}, Potentially Broken: {missingItems.Count}");
    }

    public static bool IsMediaItemInBrokenWorkgroupState(Guid mediaId)
    {
        if (!App.State.Catalog.TryGetMedia(mediaId, out MediaItem? mediaItem))
            return false;

        if (mediaItem.State != MediaItemState.Pending)
            return false;

        // try to find it in our workgroup
        if (App.State.Cache.Entries.TryGetValue(mediaId, out ICacheEntry? entry))
            return false;

        return true;
    }
}
