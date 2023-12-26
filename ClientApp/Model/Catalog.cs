using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Documents;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Types.Parallel;
using Thetacat.Util;

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
        item.PendingOp = MediaItem.Op.Create;
        m_items.Add(item.ID, item);

        if (m_virtualLookupTable.Count != 0)
            AddToVirtualLookup(m_virtualLookupTable, item);
    }

    /*----------------------------------------------------------------------------
        %%Function: PushPendingChanges
        %%Qualified: Thetacat.Model.Catalog.PushPendingChanges

        This will push any pending changes to the database.

        This currently does not deal with any kind of coherency failure. Whoever
        is committing last wins.
    ----------------------------------------------------------------------------*/
    public void PushPendingChanges()
    {
        List<MediaItemDiff> diffs = BuildUpdates();

        ServiceInterop.UpdateMediaItems(diffs);
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

    public List<MediaItemDiff> BuildUpdates()
    {
        List<MediaItemDiff> diffs = new();

        foreach (KeyValuePair<Guid, MediaItem> item in m_items)
        {
            if (item.Value.MaybeHasChanges)
            {
                if (item.Value.PendingOp == MediaItem.Op.None)
                    continue;

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
        m_virtualLookupTable.Clear();
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

    private ConcurrentDictionary<string, MediaItem> m_virtualLookupTable = new ConcurrentDictionary<string, MediaItem>();

    private void AddToVirtualLookup(ConcurrentDictionary<string, MediaItem> lookupTable, MediaItem item)
    {
        if (string.IsNullOrEmpty(item.VirtualPath))
            return;

        String lookupValue = item.VirtualPath.ToString().ToUpperInvariant();
        if (lookupTable.TryGetValue(lookupValue, out MediaItem? existing))
        {
            if (item.MD5 != existing.MD5)
                MessageBox.Show($"duplicate virtual path: {item.VirtualPath} with different MD5");

            // in either case we're going to skip it
            return;
        }

        lookupTable.TryAdd(lookupValue, item);
    }

    private void BuildVirtualLookup()
    {
        foreach (KeyValuePair<Guid, MediaItem> item in m_items)
        {
            AddToVirtualLookup(m_virtualLookupTable, item.Value);
        }
    }

    public MediaItem? LookupItemFromVirtualPath(string virtualPath, string fullLocalPath)
    {
        if (m_virtualLookupTable.Count == 0)
            BuildVirtualLookup();

        string lookup = virtualPath.ToUpperInvariant();
        if (lookup.StartsWith("/"))
            lookup = lookup.Substring(1);

        if (m_virtualLookupTable.TryGetValue(lookup, out MediaItem? item))
        {
            // since we found a matching virtualPath, let's see if the MD5 matches
            string md5 = Checksum.GetMD5ForPathSync(fullLocalPath);
            if (md5 == item.MD5)
                return item;
        }

        return null;
    }
}
