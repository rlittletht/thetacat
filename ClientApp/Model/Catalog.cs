using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Windows;
using Thetacat.Filtering;
using Thetacat.Import;
using Thetacat.Logging;
using Thetacat.Metatags.Model;
using Thetacat.Model.Caching;
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
    public event EventHandler<DirtyItemEventArgs<bool>>? OnItemDirtied;

    private readonly Dictionary<MediaStackType, MediaStacks> m_mediaStacks;
    private readonly Media m_media;

    public MediaItem GetMediaFromId(Guid id) => m_media.Items[id];
    public MediaItem GetMediaFromId(string id) => m_media.Items[Guid.Parse(id)];
    public bool TryGetMedia(Guid id, [MaybeNullWhen(false)] out MediaItem mediaItem) => m_media.Items.TryGetValue(id, out mediaItem);

    // BE CAREFUL WITH THIS! It will create a snapshot of the underlying data, which could be SLOW
    public IReadOnlyCollection<MediaItem> GetMediaCollection() => m_media.Items.Values.AsReadOnly();
    public int GetMediaCount => m_media.Items.Count;

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

    public void Reset()
    {
        m_media.Reset();
        VersionStacks.Clear();
        MediaStacks.Clear();
        TriggerItemDirtied(false);
    }

    private void TriggerItemDirtied(bool fDirty)
    {
        if (OnItemDirtied != null)
            OnItemDirtied(this, new DirtyItemEventArgs<bool>(fDirty));
    }

    public void AddNewMediaItem(MediaItem item)
    {
        item.PendingOp = MediaItem.Op.Create;
        item.OnItemDirtied += OnMediaItemDirtied;

        TriggerItemDirtied(true);
        m_media.AddNewMediaItem(item);

        if (m_virtualLookupTable.Count != 0)
            AddToVirtualLookup(m_virtualLookupTable, item);

        ThreadContext.InvokeOnUiThread(() => AddToObservableCollection(item));
    }

    /*----------------------------------------------------------------------------
        %%Function: CreateVersionBasedOn
        %%Qualified: Thetacat.Model.Catalog.CreateVersionBasedOn

        Create a new media item based on the given media item.

        plan:
            create a version stack if necessary
            create a new media item, duplicating:
                all metatags EXCEPT IsTrashItem, ImportDate
            reset ImportDate to now
            determine where in the stack the given item is
            insert the new item into the stack right after the given item
            add new media item to catalog

        TODO: How to get it marked for upload? We've done most of the import
        work already

        TODO: Watch the filesystem for file changes? Need to be able to do this
        quickly, so use last write time / file size before we check the hash
        which would be slow. (Do we cache the write time / file size in the
        workgroup DB? we need to).

        If we have a mismatch, we need to reread the metatadata (specifically
        looking for date changes and dimensions). Then we need to add all those
        changes to the database.

        This is similar to the idea of automatically adding media when the
        directory changes, but this will only look for files that are already in the
        database (look up the path and match it with the workgroup DB).

        We *could* do the dir scan as well by just getting the contents of the dir
        and those files that don't match an existing WG item can  be marked as "need
        to import"
    ----------------------------------------------------------------------------*/
    public MediaItem? CreateVersionBasedOn(ICache cache, MediaItem based)
    {
        // before we do any of this, we have to have a real local copy of the file
        string? localFile = cache.TryGetCachedFullPath(based.ID);

        if (localFile == null)
        {
            MessageBox.Show(
                "Can't create a new version without a local copy of the image. Please make sure the cache is up to date before trying to edit a new version");
            return null;
        }

        PathSegment basedPath = new PathSegment(localFile);
        PathSegment? newFile = Cache.GetUniqueLocalNameDerivative(basedPath, "edited");

        if (newFile == null)
        {
            MessageBox.Show($"Can't create a new file for {localFile}.");
            return null;
        }

        try
        {
            System.IO.File.Copy(localFile, newFile.Local);
        }
        catch (Exception exc)
        {
            MessageBox.Show($"Couldn't create new version: {newFile}. Copy failed: {exc.Message}");
            return null;
        }

        MediaStack? stack = null;

        if (based.VersionStack == null)
        {
            stack = new MediaStack(MediaStackType.Version, "version stack");
            VersionStacks.AddStack(stack);

            AddMediaToTopOfMediaStack(MediaStackType.Version, stack.StackId, based.ID);
        }

        if (based.VersionStack == null)
            throw new CatExceptionInternalFailure("no version stack after creating version stack!");

        stack = VersionStacks.Items[based.VersionStack.Value];
        MediaStackItem? versionStackItem = stack.FindMediaInStack(based.ID);

        if (versionStackItem == null)
            throw new CatExceptionInternalFailure("can't find media in stack we just put it in!");

        MediaItem newItem = MediaItem.CreateNewBasedOn(based);

        if (based.MediaStack != null)
        {
            // need to add this new item to the media stack
            MediaStack stackOther = MediaStacks.Items[based.MediaStack.Value];
            MediaStackItem? stackItem = stackOther.FindMediaInStack(based.ID);

            if (stackItem != null)
            {
                // remember stack indexes are just arbitrary values, and items will 'push away' if we try to insert
                // a duplicate item. so we can safely just add 1 here.
                AddMediaToStackAtIndex(MediaStackType.Media, stackOther.StackId, newItem.ID, stackItem.StackIndex + 1);
            }
        }

        // now update some metatags
        newItem.ImportDate = DateTime.Now;
        newItem.FRemoveMediaTag(BuiltinTags.s_IsTrashItemID);
        newItem.VirtualPath = cache.GetRelativePathToCacheRootFromFullPath(newFile);

        AddNewMediaItem(newItem);

        // we can't add it to the version stack until its part of the catalog
        AddMediaToStackAtIndex(MediaStackType.Version, stack.StackId, newItem.ID, versionStackItem.StackIndex + 1);

        MediaImporter.PrePopulateCacheForLocalPath(cache, newFile, newItem);
        // be sure to push the changes to the database!
        cache.PushChangesToDatabase(null);

        // and make sure there's an import item for it, otherwise it won't get uploaded to the catalog
        ImportItem importItem = MediaImporter.CreateNewImportItemForArbitraryPath(newItem, newFile);
        List<ImportItem> newItems = new() { importItem };

        ServiceInterop.InsertImportItems(App.State.ActiveProfile.CatalogID, newItems);

        return new MediaItem();
    }

    void OnMediaItemDirtied(object? sender, DirtyItemEventArgs<Guid> e)
    {
        if (OnItemDirtied != null)
            OnItemDirtied(this, new DirtyItemEventArgs<bool>(true));
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
    public void PushPendingChanges(Guid catalogID, Func<int, string, bool>? verify = null)
    {
        m_media.PushPendingChanges(catalogID, verify);
        foreach (KeyValuePair<MediaStackType, MediaStacks> item in m_mediaStacks)
        {
            string itemType = item.Key.ToString();

            item.Value.PushPendingChanges(
                catalogID,
                verify == null
                    ? null
                    : (count, _) => verify(count, itemType));
        }

        TriggerItemDirtied(false);
    }

    private async Task<ServiceCatalog> GetFullCatalogAsync(Guid catalogID)
    {
        Task<List<ServiceMediaItem>> taskGetMedia =
            Task.Run(() => ServiceInterop.ReadFullCatalogMedia(catalogID));

        Task<List<ServiceMediaTag>> taskGetMediaTags =
            Task.Run(() => ServiceInterop.ReadFullCatalogMediaTags(catalogID));

        List<Task> tasks = new List<Task>() { taskGetMedia, taskGetMediaTags };

        await Task.WhenAll(tasks);

        return new ServiceCatalog()
               {
                   MediaItems = taskGetMedia.Result,
                   MediaTags = taskGetMediaTags.Result
               };
    }

    public async Task ReadFullCatalogFromServer(Guid catalogID, MetatagSchema schema)
    {
        MicroTimer timer = new MicroTimer();
        timer.Reset();
        timer.Start();

        ServiceCatalog catalog = await GetFullCatalogAsync(catalogID);

        App.LogForApp(EventType.Information, $"ServiceInterop.ReadFullCatalog: {timer.Elapsed()}");

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
            mediaItem.OnItemDirtied += OnMediaItemDirtied;
            dict.TryAdd(mediaItem.ID, mediaItem);
        }

        App.LogForApp(EventType.Information, $"Populate Media Dictionary: {timer.Elapsed()}");
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
                    schema.ReplaceFromService(ServiceInterop.GetMetatagSchema(catalogID));
                    metatag = schema.GetMetatagFromId(tag.Id);
                }

                if (metatag == null)
                    throw new Exception($"media has mediatag with id {tag.Id} but that tag id doesn't exist in the schema, even after refreshing the schema");
            }

            m_media.AddMediaTagInternal(tag.MediaId, new MediaTag(metatag, tag.Value));
        }

        App.LogForApp(EventType.Information, $"MediaTags added: {timer.Elapsed()}");
        timer.Reset();
        timer.Start();

        // read all the version stacks
        MediaStacks.Clear();
        VersionStacks.Clear();

        List<ServiceStack> serviceStacks = ServiceInterop.GetAllStacks(catalogID);
        foreach (ServiceStack stack in serviceStacks)
        {
            MediaStack mediaStack = new MediaStack(stack);
            MediaStackType stackType = new MediaStackType(stack.StackType ?? throw new CatExceptionServiceDataFailure());
            MediaStacks stacks = m_mediaStacks[stackType];

            stacks.AddStack(mediaStack);
            AssociateStackWithMedia(mediaStack, stackType);
        }

        App.LogForApp(EventType.Verbose, $"Stacks associated: {timer.Elapsed()}");
        timer.Reset();
        timer.Start();

//        m_media.Items.ResumeNotifications();
        if (m_observableView != null)
        {
            m_observableView.Clear();
            m_observableView.ReplaceCollection(GetMediaCollection());
        }

        App.LogForApp(EventType.Information, $"ObservableView populated: {timer.Elapsed()}");
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

    public void SetBaseFromBaseCatalog(Catalog other)
    {
        m_media.SetBaseFromOtherMedia(other.m_media);
        VersionStacks.SetPendingChangesFromBase(other.VersionStacks);
        MediaStacks.SetPendingChangesFromBase(other.MediaStacks);
    }

    public void DeleteItem(Guid catalogId, Guid id)
    {
        MediaItem item = GetMediaFromId(id);

        // delete from the service
        ServiceInterop.DeleteMediaItem(catalogId, id);

        // now delete all remnants from ourselves
        if (item.MediaStack != null)
            MediaStacks.RemoveFromStack(item.MediaStack.Value, item);

        if (item.VersionStack != null)
            VersionStacks.RemoveFromStack(item.VersionStack.Value, item);

        m_media.DeleteMediaItem(item);
        TriggerItemDirtied(true);
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
                App.LogForApp(EventType.Information, $"BuildVirtualLookup: {timer.Elapsed()}");
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
                        mediaItem.SetMediaStackVerify(this, stack.StackId);
                        break;
                    case MediaStackType.s_VersionType:
                        mediaItem.SetVersionStackVerify(this, stack.StackId);
                        break;
                    default:
                        throw new CatExceptionInternalFailure($"unknown stack type {stackType}");
                }
            }
        }
    }

    public void AddMediaToTopOfMediaStack(MediaStackType stackType, Guid stackId, Guid mediaId)
    {
        AddMediaToStackAtIndex(stackType, stackId, mediaId, null);
    }

    /*----------------------------------------------------------------------------
        %%Function: AddMediaToStackAtIndex
        %%Qualified: Thetacat.Model.Catalog.AddMediaToStackAtIndex

        Add at the given index. If the index isn't already occupied, then we're
        done. otherwise, all items are pushed to make room
    ----------------------------------------------------------------------------*/
    public void AddMediaToStackAtIndex(MediaStackType stackType, Guid stackId, Guid mediaId, int? indexRequested)
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

        int index = indexRequested ?? 0;

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
                mediaItem.SetMediaStackVerify(this, stackId);
            else if (stackType.Equals(MediaStackType.Version))
                mediaItem.SetVersionStackVerify(this, stackId);
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
            mediaItem.SetMediaStackVerify(this, stackId);
        else if (stackType.Equals(MediaStackType.Version))
            mediaItem.SetVersionStackVerify(this, stackId);
        else
            throw new CatExceptionInternalFailure("unknown stack type");
    }


    /*----------------------------------------------------------------------------
        %%Function: GetMD5ForItem
        %%Qualified: Thetacat.Model.Catalog.GetMD5ForItem

        Get the best MD5 we have for this item, most likely from the given cache
        but if the local cache doesn't know about it, then from the media itself
    ----------------------------------------------------------------------------*/
    public string GetMD5ForItem(Guid id, ICache cache)
    {
        if (cache.Entries.TryGetValue(id, out ICacheEntry? entry))
        {
            return entry.MD5;
        }

        return m_media.Items[id].MD5;
    }

#endregion
}
