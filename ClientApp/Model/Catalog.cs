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

// The catalog manages the following data (from the database):
//  - Media Items (MediaItem)
public class Catalog: ICatalog
{
    private readonly Dictionary<MediaStackType, MediaStacks> m_mediaStacks;
    private readonly Media m_media;

    public IMedia Media => (IMedia)m_media;

    public MediaStacks VersionStacks => m_mediaStacks[MediaStackType.Version];
    public MediaStacks MediaStacks => m_mediaStacks[MediaStackType.Media];

    //    public void Clear()
    //    {
    //        m_items.Clear();
    //    }

    public Catalog()
    {
        m_media = new Media();
        m_mediaStacks =
            new Dictionary<MediaStackType, MediaStacks>()
            {
                { MediaStackType.Media, new MediaStacks(MediaStackType.Media) },
                { MediaStackType.Version, new MediaStacks(MediaStackType.Version) }
            };
    }

    public MediaStacks GetStacksFromType(MediaStackType stackType) => m_mediaStacks[stackType];

    public void AddNewMediaItem(MediaItem item)
    {
        item.PendingOp = MediaItem.Op.Create;
        m_media.AddNewMediaItem(item);

        if (m_virtualLookupTable.Count != 0)
            AddToVirtualLookup(m_virtualLookupTable, item);
    }

    public bool HasMediaItem(Guid mediaId)
    {
        return m_media.Items.ContainsKey(mediaId);
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
        m_media.PushPendingChanges();
        foreach (MediaStacks stacks in m_mediaStacks.Values)
        {
            stacks.PushPendingChanges();
        }
    }


    public void ReadFullCatalogFromServer(MetatagSchema schema)
    {
        ServiceCatalog catalog = ServiceInterop.ReadFullCatalog();

        IObservableConcurrentDictionary<Guid, MediaItem> dict = m_media.Items;

        dict.Clear();
        m_virtualLookupTable.Clear();
        if (catalog.MediaItems == null || catalog.MediaTags == null)
            return;
        
        foreach (ServiceMediaItem item in catalog.MediaItems)
        {
            MediaItem mediaItem = new MediaItem(item);
            dict.Add(mediaItem.ID, mediaItem);
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

            m_media.AddMediaTagInternal(tag.MediaId, new MediaTag(metatag, tag.Value));
        }

        // read all the version stacks
        MediaStacks.Clear();
        VersionStacks.Clear();

        List<ServiceStack> serviceStacks = ServiceInterop.GetAllStacks();
        foreach (ServiceStack stack in serviceStacks)
        {
            MediaStack mediaStack = new MediaStack(stack);
            MediaStackType stackType = new MediaStackType(stack.StackType ?? throw new CatExceptionServiceDataFailure());
            MediaStacks stacks = m_mediaStacks[stackType];

            stacks.AddStack(mediaStack);
            AssociateStackWithMedia(mediaStack, stackType);
        }
    }

    private void AssociateStackWithMedia(MediaStack stack, MediaStackType stackType)
    {
        foreach (MediaStackItem item in stack.Items)
        {
            if (Media.Items.TryGetValue(item.MediaId, out MediaItem? mediaItem))
            {
                switch ((int)stackType)
                {
                    case MediaStackType.s_MediaType:
                        mediaItem.MediaStack = stack.StackId;
                        break;
                    case MediaStackType.s_VersionType:
                        mediaItem.VersionStack = stack.StackId;
                        break;
                    default:
                        throw new CatExceptionInternalFailure($"unknown stack type {stackType}");
                }
            }
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
        foreach (KeyValuePair<Guid, MediaItem> item in Media.Items)
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

    /*----------------------------------------------------------------------------
        %%Function: AddMediaToStackAtIndex
        %%Qualified: Thetacat.Model.Catalog.AddMediaToStackAtIndex

        Add at the given index. If the index isn't already occupied, then we're
        done. otherwise, all items are pushed to make room
    ----------------------------------------------------------------------------*/
    public void AddMediaToStackAtIndex(Guid stackId, Guid mediaId, int index)
    {
        //MediaStack
    }
}
