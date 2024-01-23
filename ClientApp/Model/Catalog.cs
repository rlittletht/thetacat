using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows;
using Thetacat.Filtering;
using Thetacat.Logging;
using Thetacat.Metatags.Model;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model;

// The catalog manages the following data (from the database):
//  - Media Items (MediaItem)
//  - Meditags (Mediatag)

// It provides threadsafe access to the media
// To bind to an observable collection, this will be maintained as an additional
// dataset and lazily updated only on the UI thread (so it will not be suitable for enumerating
// the true state of the catalog. its only a view)
public class Catalog : ICatalog
{
    private readonly Dictionary<MediaStackType, MediaStacks> m_mediaStacks;
    private readonly Media m_media;

    public MediaItem GetMediaFromId(Guid id) => m_media.Items[id];
    public MediaItem GetMediaFromId(string id) => m_media.Items[Guid.Parse(id)];
    public bool TryGetMedia(Guid id, [MaybeNullWhen(false)] out MediaItem mediaItem) => m_media.Items.TryGetValue(id, out mediaItem);

    // BE CAREFUL WITH THIS! It will create a snapshot of the underlying data, which could be SLOW
    public IEnumerable<MediaItem> GetMediaCollection() => m_media.Items.Values;

    public MediaStacks VersionStacks => m_mediaStacks[MediaStackType.Version];
    public MediaStacks MediaStacks => m_mediaStacks[MediaStackType.Media];

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

        ThreadContext.InvokeOnUiThread(() => AddToObservableCollection(item));
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

    private async Task<ServiceCatalog> GetFullCatalogAsync()
    {
        Task<List<ServiceMediaItem>> taskGetMedia =
            Task.Run(ServiceInterop.ReadFullCatalogMedia);

        Task<List<ServiceMediaTag>> taskGetMediaTags =
            Task.Run(ServiceInterop.ReadFullCatalogMediaTags);

        List<Task> tasks = new List<Task>() { taskGetMedia, taskGetMediaTags };

        await Task.WhenAll(tasks);

        return new ServiceCatalog()
               {
                   MediaItems = taskGetMedia.Result,
                   MediaTags = taskGetMediaTags.Result
               };
    }

    public async Task ReadFullCatalogFromServer(MetatagSchema schema)
    {
        MicroTimer timer = new MicroTimer();
        timer.Reset();
        timer.Start();

        ServiceCatalog catalog = await GetFullCatalogAsync();

        MainWindow.LogForApp(EventType.Warning, $"ServiceInterop.ReadFullCatalog: {timer.Elapsed()}");

        timer.Reset();
        timer.Start();

        ConcurrentDictionary<Guid, MediaItem> dict = m_media.Items;
        // m_media.Items.PushPauseNotifications();

        dict.Clear();
        m_virtualLookupTable.Clear();
        if (catalog.MediaItems == null || catalog.MediaTags == null)
            return;

        foreach (ServiceMediaItem item in catalog.MediaItems)
        {
            MediaItem mediaItem = new MediaItem(item);
            dict.TryAdd(mediaItem.ID, mediaItem);
        }

        MainWindow.LogForApp(EventType.Warning, $"Populate Media Dictionary: {timer.Elapsed()}");
        timer.Reset();
        timer.Start();

        bool refreshedSchema = false;
        foreach (ServiceMediaTag tag in catalog.MediaTags)
        {
            Metatag? metatag = schema.GetMetatagFromId(tag.Id);

            if (metatag == null)
            {
                if (!refreshedSchema)
                {
                    schema.ReplaceFromService(ServiceInterop.GetMetatagSchema());
                    metatag = schema.GetMetatagFromId(tag.Id);
                }

                if (metatag == null)
                    throw new Exception($"media has mediatag with id {tag.Id} but that tag id doesn't exist in the schema, even after refreshing the schema");
            }

            m_media.AddMediaTagInternal(tag.MediaId, new MediaTag(metatag, tag.Value));
        }

        MainWindow.LogForApp(EventType.Warning, $"MediaTags added: {timer.Elapsed()}");
        timer.Reset();
        timer.Start();

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

        MainWindow.LogForApp(EventType.Warning, $"Stacks associated: {timer.Elapsed()}");
        timer.Reset();
        timer.Start();

//        m_media.Items.ResumeNotifications();
        if (m_observableView != null)
        {
            m_observableView.Clear();
            m_observableView.ReplaceCollection(GetMediaCollection());
        }

        // good time to refresh the MRU now that we loaded the catalog and the schema
        App.State.MetatagMRU.Set(App.State.Settings.MetatagMru);
        MainWindow.LogForApp(EventType.Warning, $"ObservableView populated: {timer.Elapsed()}");
    }

    /*----------------------------------------------------------------------------
        %%Function: GetFilteredMediaItems
        %%Qualified: Thetacat.Model.Catalog.GetFilteredMediaItems

        return all the items matching the filter (if filter is set to true, then
        the item must have the tag; if set to false, it must not. if unset, then
        no requirement
    ----------------------------------------------------------------------------*/
    public List<MediaItem> GetFilteredMediaItems(FilterDefinition filter)
    {
        List<MediaItem> matched = new();

        foreach (MediaItem item in m_media.Items.Values)
        {
            if (item.MatchesMetatagFilter(filter))
                matched.Add(item);
        }

        return matched;
    }

    #region Observable Collection Support

    private ObservableCollection<MediaItem>? m_observableView;

    public ObservableCollection<MediaItem> GetObservableCollection()
    {
        if (m_observableView == null)
        {
            m_observableView = new ObservableCollection<MediaItem>(GetMediaCollection());
        }

        return m_observableView;
    }

    private void AddToObservableCollection(MediaItem item)
    {
        if (m_observableView == null)
            return;

        m_observableView.Add(item);
    }

    private void RemoveFromObservableCollection(MediaItem item)
    {
        if (m_observableView == null)
            return;

        m_observableView.Remove(item);
    }

#endregion

    #region Virtual Paths

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
        foreach (MediaItem item in GetMediaCollection())
        {
            AddToVirtualLookup(m_virtualLookupTable, item);
        }
    }

    public MediaItem? FindMatchingMediaByMD5(string md5)
    {
        // slow full item search
        foreach (MediaItem item in GetMediaCollection())
        {
            if (!string.IsNullOrEmpty(item.MD5) && item.MD5 == md5)
                return item;
        }

        return null;
    }

    public MediaItem? LookupItemFromVirtualPath(string virtualPath, string fullLocalPath, bool verifyMd5)
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
            if (!verifyMd5)
            {
                // we're going to assume the MD5 hash will match
                return item;
            }

            // since we found a matching virtualPath, let's see if the MD5 matches
            string md5 = App.State.Md5Cache.GetMd5ForPathSync(fullLocalPath);
            if (md5 == item.MD5)
                return item;
        }

        return null;
    }

    #endregion

    #region Media Stacks

    private void AssociateStackWithMedia(MediaStack stack, MediaStackType stackType)
    {
        foreach (MediaStackItem item in stack.Items)
        {
            if (TryGetMedia(item.MediaId, out MediaItem? mediaItem))
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

        MediaItem mediaItem = GetMediaFromId(mediaId);

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

#endregion
}
