using System;
using System.Collections.Generic;
using Thetacat.Logging;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Types.Parallel;

namespace Thetacat.Model;

public class Media: IMedia
{
    private readonly ObservableConcurrentDictionary<Guid, MediaItem> m_items;

    public ObservableConcurrentDictionary<Guid, MediaItem> Items => m_items;

    public Media()
    {
        m_items = new ObservableConcurrentDictionary<Guid, MediaItem>();
    }

    public void AddNewMediaItem(MediaItem item)
    {
        item.PendingOp = MediaItem.Op.Create;
        m_items.Add(item.ID, item);
    }

    public void PushPendingChanges()
    {
        List<MediaItemDiff> diffs = BuildUpdates();

        ServiceInterop.UpdateMediaItems(diffs);

        foreach (MediaItemDiff diff in diffs)
        {
            if (diff.DiffOp == MediaItemDiff.Op.Delete)
                Items.Remove(diff.ID);
            else if (Items.TryGetValue(diff.ID, out MediaItem item))
            {
                if (item.VectorClock == diff.VectorClock)
                    item.ResetPendingChanges();
            }
        }
    }

    public List<MediaItemDiff> BuildUpdates()
    {
        List<MediaItemDiff> diffs = new();

        foreach (KeyValuePair<Guid, MediaItem> item in m_items)
        {
            if (item.Value.MaybeHasChanges)
            {
                if (item.Value.PendingOp == MediaItem.Op.Create)
                    diffs.Add(MediaItemDiff.CreateInsert(item.Value));
                else if (item.Value.PendingOp == MediaItem.Op.Delete)
                    diffs.Add(MediaItemDiff.CreateDelete(item.Value.ID));
                else
                {
                    MediaItemDiff diff = (MediaItemDiff.CreateUpdate(item.Value));

                    // make sure something actually changed before adding it
                    if (diff.PropertiesChanged != 0)
                        diffs.Add(diff);
                }
            }
        }

        return diffs;
    }


    /*----------------------------------------------------------------------------
        %%Function: AddMediaTagInternal
        %%Qualified: Thetacat.Model.Catalog.AddMediaTagInternal

        This circumvents the normal dirtying of the item -- DO NOT use this
        directly unless you know what you are really doing (e.g. you are reading
        from the database which means its by definition not a dirtying action)
    ----------------------------------------------------------------------------*/
    public void AddMediaTagInternal(Guid id, MediaTag tag)
    {
        if (!m_items.ContainsKey(id))
            throw new Exception("media not present");

        m_items[id]
           .Tags.AddOrUpdate(
                tag.Metatag.ID,
                tag,
                (key, oldTag) =>
                {
                    oldTag.Value = tag.Value;
                    return oldTag;
                });
    }
}
