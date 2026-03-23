using System.Collections.Generic;
using System;
using System.Windows;
using Thetacat.Import;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.UI.Controls.MediaItemsListControl;
namespace Thetacat.Repair;

public class CatalogRepair
{
    /*----------------------------------------------------------------------------
        %%Function: DeleteOrphanedDuplicateMedia
        %%Qualified: Thetacat.Repair.CatalogRepair.DeleteOrphanedDuplicateMedia
    ----------------------------------------------------------------------------*/
    public static void DeleteOrphanedDuplicateMedia(Guid catalogID, ICatalog catalog,  ICache cache, Dictionary<Guid, ServiceImportItem> imports)
    {
        IReadOnlyCollection<MediaItem> collection = catalog.GetMediaCollection();

        int count = collection.Count;
        int i = 0;

        Dictionary<string, List<MediaItem>> md5Map = new();
        foreach (MediaItem item in collection)
        {
            if (!md5Map.ContainsKey(item.MD5))
                md5Map[item.MD5] = new List<MediaItem>();

            md5Map[item.MD5].Add(item);
        }

        List<MediaItem> dupes = new();

        // Find all the catalog items that DO NOT have an entry in the workgroup,
        // and DO NOT have an imports entry, and DO have a duplicate media item
        // already in the catalog (that has the same MD5 and the same VirtualPath)
        foreach (MediaItem item in collection)
        {
            imports.TryGetValue(item.ID, out ServiceImportItem? importItem);
            if (importItem != null)
                continue;

            if (cache.Entries.ContainsKey(item.ID))
                continue;

            if (item.State != MediaItemState.Pending)
                continue;

            if (string.IsNullOrWhiteSpace(item.MD5))
                continue;

            List<MediaItem> items = md5Map[item.MD5];

            // first, make sure our item is in the list
            if (items.Find(_item => _item.ID == item.ID) == null)
                throw new CatExceptionInternalFailure("our own md5 isn't in the list?");

            MediaItem? dupe = null;

            foreach (MediaItem dupeCheck in items)
            {
                // in order to be a real dupe, it must match our virtual path
                // AND it must exist in the workgroup
                if (!dupeCheck.VirtualPath.Equals(item.VirtualPath)
                    || !cache.Entries.ContainsKey(dupeCheck.ID))
                {
                    continue;
                }

                dupe = dupeCheck;
            }

            if (dupe == null)
                continue;

            dupes.Add(item);
        }

        if (dupes.Count == 0)
        {
            MessageBox.Show("There are no dupes we can delete");
            return;
        }

        if (MessageBox.Show(
                $"There are {dupes.Count} duplicate media items that have no workgroup entries and no import entries. These should be deleted\n\nDelete these items?",
                "Delete Dupes",
                MessageBoxButton.YesNo)
            == MessageBoxResult.No)
        {
            return;
        }

        foreach (MediaItem dupe in dupes)
        {
            catalog.DeleteItem(catalogID, dupe.ID);
        }
    }
}
