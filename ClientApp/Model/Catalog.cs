using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Documents;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.Types;

namespace Thetacat.Model;

public class Catalog: ICatalog
{
    private readonly ObservableConcurrentDictionary<Guid, MediaItem> m_items;

    public ObservableConcurrentDictionary<Guid, MediaItem> Items => m_items;

//    public void Clear()
//    {
//        m_items.Clear();
//    }

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

    public void AddMediaTag(Guid id, MediaTag tag)
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

    public void ReadFullCatalogFromServer(MetatagSchema schema)
    {
        ServiceCatalog catalog = ServiceInterop.ReadFullCatalog();

        IObservableConcurrentDictionary<Guid, MediaItem> dict = m_items;

        dict.Clear();

        if (catalog.MediaItems == null || catalog.MediaTags == null)
            return;
        
        foreach (ServiceMediaItem item in catalog.MediaItems)
        {
            MediaItem mediaItem = new MediaItem(item);
            m_items.Add(mediaItem.ID, mediaItem);
        }

        bool refreshedSchema = false;
        foreach (ServiceMediaTag tag in catalog.MediaTags)
        {
            Metatag? metatag = schema.FindFirstMatchingItem(MetatagMatcher.CreateIdMatch(tag.Id));

            if (metatag == null)
            {
                if (!refreshedSchema)
                {
                    schema.ReplaceFromService(ServiceInterop.GetMetatagSchema());
                    metatag = schema.FindFirstMatchingItem(MetatagMatcher.CreateIdMatch(tag.Id));
                }

                if (metatag == null)
                    throw new Exception($"media has mediatag with id {tag.Id} but that tag id doesn't exist in the schema, even after refreshing the schema");
            }

            AddMediaTag(tag.MediaId, new MediaTag(metatag, tag.Value));
        }
    }
}
