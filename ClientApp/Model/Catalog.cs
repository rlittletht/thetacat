using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Thetacat.ServiceClient;

namespace Thetacat.Model;

public class Catalog
{
    private readonly ObservableConcurrentDictionary<Guid, MediaItem> m_items;

    public Catalog()
    {
        m_items = new();
    }

    public void AddNewMediaItem(MediaItem item)
    {
        m_items.Add(item.ID, item);
    }

    public void FlushPendingCreates()
    {
        // collect all the items pending create -- we will create them all at once
        List<MediaItem> creating = new();

        foreach (MediaItem item in m_items.Values)
        {
            if (!item.IsCreatePending())
                continue;

            creating.Add(item);
        }

        ServiceInterop.InsertNewMediaItems(creating);

        foreach (MediaItem item in creating)
        {
            item.ClearPendingCreate();
        }
    }
}
