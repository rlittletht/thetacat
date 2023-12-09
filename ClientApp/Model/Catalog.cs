using System;
using System.Collections.Concurrent;

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
}
