using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Documents;
using Thetacat.Logging;
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
        m_media.Items.PushPauseNotifications();

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

        m_media.Items.ResumeNotifications();
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
    private object m_virtualLookupTableLock = new Object();

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
        lock (m_virtualLookupTableLock)
        {
            if (m_virtualLookupTable.Count == 0)
            {
                MicroTimer timer = new MicroTimer();
                timer.Start();

                BuildVirtualLookup();
                MainWindow.LogForApp(EventType.Warning, $"BuildVirtualLookup: {timer.Elapsed()}");
            }
        }

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
    public void AddMediaToStackAtIndex(MediaStackType stackType, Guid stackId, Guid mediaId, int index)
    {
        MediaStacks stacks = m_mediaStacks[stackType];

        if (!stacks.Items.TryGetValue(stackId, out MediaStack? stack))
            throw new CatExceptionInternalFailure("can't implicitly create a stack using AddMediaToStackAtIndex");

        // see if the stack already has the mediaId (and create a map of index values
        Dictionary<int, MediaStackItem> map = new();

        MediaStackItem? itemExisting = null;
        bool renumberNeeded = false;
        int maxIndexSeen = 0;

        foreach (MediaStackItem item in stack.Items)
        {
            if (item.MediaId == mediaId)
                itemExisting = item;

            if (map.ContainsKey(item.StackIndex))
            {
                // we have duplicate indexes. need to repair
                renumberNeeded = true;
                item.StackIndex = -1; // needs a new number
            }
            else
            {
                map.Add(item.StackIndex, item);
            }

            maxIndexSeen = Math.Max(maxIndexSeen, item.StackIndex);
        }

        bool pushNeeded = map.ContainsKey(index);

        MediaItem mediaItem = Media.Items[mediaId];

        if (!pushNeeded && !renumberNeeded)
            // simplest. we're done
        {
            if (itemExisting != null)
                itemExisting.StackIndex = index;
            else
                stack.PushItem(new MediaStackItem(mediaId, index));

            if (stackType.Equals(MediaStackType.Media))
                mediaItem.SetMediaStackSafe(this, stackId);
            else if (stackType.Equals(MediaStackType.Version))
                mediaItem.SetVersionStackSafe(this, stackId);
            else
                throw new CatExceptionInternalFailure("unknown stack type");

            return;
        }

        if (maxIndexSeen >= index)
        {
            // our index is going to insert before us, so we have to adjust
            maxIndexSeen++;
        }
        else
        {
            maxIndexSeen = index;
        }

        // don't insert yet so we don't adjust our own item...

        // items with index equal to or greater than us
        // get bumped by one
        // items with -1 get the next renumber index
        foreach (MediaStackItem item in stack.Items)
        {
            if (item.StackIndex >= index)
                item.StackIndex++;
            else if (item.StackIndex == -1)
                item.StackIndex = ++maxIndexSeen;
        }

        if (itemExisting != null)
            itemExisting.StackIndex = index;
        else
            stack.PushItem(new MediaStackItem(mediaId, index));

        if (stackType.Equals(MediaStackType.Media))
            mediaItem.SetMediaStackSafe(this, stackId);
        else if (stackType.Equals(MediaStackType.Version))
            mediaItem.SetVersionStackSafe(this, stackId);
        else
            throw new CatExceptionInternalFailure("unknown stack type");
    }
}
