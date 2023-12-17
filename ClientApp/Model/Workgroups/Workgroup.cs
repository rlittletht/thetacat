using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Workgroups;
/*
VECTOR CLOCKS:

A note on how we use them. VC are used to enforce data coherency. We don't want to change the DB
if the version of the content is different than the version we think we are changing. (i.e. if
client 1 (C1) downloads VC=1 and C2 downloads VC=1, then C2 changes the data making VC=2, if C1
also tries to make a change, they will fail because they expect VC=1. C1 will have to download the
data again and do a merge and their base becomes VC=2.

Each client in the DB records the VC for the last database change they committed. (even if they *know* about
a more recent VC, client VC is the last VC when they updated.

The workgroup maintains the latest VC.

Each client syncs the server and knows what VC that sync represents. If they make changes, they expect to
produce VC + 1. When they update the server, the WG VC must be the same as the clients base. If yes, then 
set the new VC (VC+1) and make all the changes. All of this is done under lock.

If the WG VC is not the same as the clients base, then the client has to sync again, merge, and then the base
is reset.

So, this WG class has a base VC and a client VC. The client VC will only get set when either we initialize the client
(and it comes from the database), or when we successfully upload content.


When a client starts caching, it grabs a bunch of upload-pending items that don't have cache entries (this prevents
the client from duplicating other client work). Then it immediately sets the anticipated WG cache path, the client ID,
a NULL CacheDate and the VC for when the client uploads this. THe database is then updated with this information
(usually in a batch), that only uploaded if the base VC matches the current WG VC.

    IF IT DOES, then all of these are uploaded. For each queued cache entry:
        Download to the cache
        Set the CacheDate in the CacheItem
        Upload changes to database
        At this point the item will have the cache date set to when it was 
            actually cache, and the VC set to when the client claimed it

IF ANY UPLOAD gets a coherency failure (the WG VC doesn't match base VC)
        Download a refreshed collection from DB with new VC
        Find any changes between our collection and the new collection
            If there's a difference
                and we don't have our client name on it, just update to the WG item
                we do have our client name on it and the WG item has a VC set and that VC matches the WG item's VC, 
                 AND the WG item has our client name as well on it
                    (then this means we had already commited it before and the other client just updated it)
                    just update to match what the WG item has
                we have our client name on it, but the WG had a different client name and VC on it
                    this means another client managed to update the server before we got to "camp" the items we were going to update
                        => update our local to match the WG, remove the item from our queue
        Recalculate the items to update and insert into the database and repeat. THis will reset the VC for any
         new items to the new VC we are going to be setting to

It should not be possible for us to update the WG db with our pending cache AND for another client to update
the WG with A DIFFERENT pending cache. One of the clients should hit a coherency failure.

MAKE SURE we don't start caching items until AFTER we update the WG db with our pending queued cache items. This way we will
hit a coherency failure and remove the queued items if another client queues them first (and updates the WG db...)
            

 */
public class Workgroup: IWorkgroup
{
    private Guid m_id;

    public string Name { get; }
    public PathSegment Server { get; }
    public PathSegment CacheRoot { get; }
    private PathSegment Database => PathSegment.Join(Server, CacheRoot, new PathSegment("workgroup-cache.db"));

    private Guid m_clientId;

    public Guid ClientId => m_clientId;

    // the VC is the last version of the WG cache that we know about. if others have updated their notion
    // of the cache, then it will be different than ours. 
    protected int m_baseVectorClock;
    private int m_clientVectorClock;

    private WorkgroupDb _Database
    {
        get
        {
            if (m_db == null)
                throw new CatExceptionInitializationFailure();

            return m_db;
        }
    }

    public PathSegment FullPathToCacheRoot => PathSegment.Join(Server, CacheRoot);
    public string FullyQualifiedPath => FullPathToCacheRoot.Local;

    public Workgroup()
    {
        // for test mock only
        m_id = Guid.NewGuid();
        m_clientId = Guid.NewGuid();
        Server = new PathSegment("//mock/server");
        CacheRoot = new PathSegment("/mockroot");
        Name = "mock-workgroup";
    }

    public Workgroup(Guid id)
    {
        m_id = id;

        // FUTURE: consider using cached workgroup details from settings if the
        // connection fails to the server?
        ServiceWorkgroup serviceWorkgroup = ServiceInterop.GetWorkgroupDetails(id);
        Name = serviceWorkgroup.Name ?? throw new InvalidOperationException("no name from server");
        Server = PathSegment.CreateFromString(serviceWorkgroup.ServerPath) ?? throw new InvalidOperationException("no servername from server");
        CacheRoot = PathSegment.CreateFromString(serviceWorkgroup.CacheRoot) ?? throw new InvalidOperationException("no name from server");

        m_db ??= new WorkgroupDb(Database);

        ServiceWorkgroupClient? client = m_db.GetClientDetails(MainWindow.ClientName);

        if (client == null)
        {
            client =
                new ServiceWorkgroupClient()
                {
                    ClientId = Guid.NewGuid(),
                    ClientName = MainWindow.ClientName,
                    VectorClock = 0
                };

            m_db.CreateWorkgroupClient(client);
        }

        m_clientId = client.ClientId ?? throw new CatExceptionServiceDataFailure();
        m_clientVectorClock = client.VectorClock ?? throw new CatExceptionServiceDataFailure();
    }

    private readonly WorkgroupDb? m_db;

    private void AddServiceWorkgroupMediaToCache(ConcurrentDictionary<Guid, ICacheEntry> entries, ServiceWorkgroupItem mediaItem)
    {
        ICacheEntry entry = new WorkgroupCacheEntry(
            mediaItem.MediaId ?? throw new CatExceptionServiceDataFailure(),
            PathSegment.CreateFromString(mediaItem.Path),
            mediaItem.CachedBy ?? throw new CatExceptionServiceDataFailure(),
            mediaItem.CachedDate,
            false,
            mediaItem.VectorClock ?? throw new CatExceptionServiceDataFailure()
        );

        if (!entries.TryAdd(entry.ID, entry))
            throw new CatExceptionServiceDataFailure();

    }

    protected void UpdateFromWorkgroupMediaClock(ConcurrentDictionary<Guid, ICacheEntry> entries, ServiceWorkgroupMediaClock mediaWithClock)
    {
        // only update the entries if we have a different clock
        if (m_baseVectorClock == 0 || m_baseVectorClock != mediaWithClock.VectorClock)
        {
            m_baseVectorClock = mediaWithClock.VectorClock;

            entries.Clear();
            if (mediaWithClock.Media != null)
            {
                foreach (ServiceWorkgroupItem mediaItem in mediaWithClock.Media)
                {
                    AddServiceWorkgroupMediaToCache(entries, mediaItem);
                }
            }
        }
    }

    public void RefreshWorkgroupMedia(ConcurrentDictionary<Guid, ICacheEntry> entries)
    {
        ServiceWorkgroupMediaClock mediaWithClock = _Database.GetLatestWorkgroupMediaWithClock();

        UpdateFromWorkgroupMediaClock(entries, mediaWithClock);
    }

    /*----------------------------------------------------------------------------
        %%Function: GetNextItemsForQueue
        %%Qualified: Thetacat.Model.Cache.GetNextItemsForQueue

        The cache queue is the list of items we are actively downloading into
        the cache. When we add an item to the queue, we mark it as "CachePending"
        in the local media item (so we don't queue it again), and we add an entry
        to the Entries collection for the cache (also marked as pending).

        The pending flags are both local only and don't get saved to the database

        we assume the caller is going to update the workgroup database right after
        this to prevent other clients from trying to cache the same files.
    ----------------------------------------------------------------------------*/
    public Dictionary<Guid, MediaItem> GetNextItemsForQueue(int count)
    {
        Dictionary<Guid, MediaItem> itemsToQueue = new();
        int countToSkip = 0;

        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        foreach (KeyValuePair<Guid, MediaItem> item in MainWindow._AppState.Catalog.Items)
        {
            if (!MainWindow._AppState.Cache.Entries.ContainsKey(item.Key) && !item.Value.IsCachePending)
            {
                PathSegment cacheItemPath = Cache.EnsureUniqueLocalCacheVirtualPath(FullPathToCacheRoot, item.Value);

                if (countToSkip > 0)
                {
                    countToSkip--;
                    continue;
                }
                itemsToQueue.Add(item.Key, item.Value);

                // we will create this cache entry as Pending and set the vectorClock to the current
                // client vector clock.

                // when we go to update the cache in the workgroup database, if the 
                MainWindow._AppState.Cache.Entries.TryAdd(
                    item.Key,
                    new WorkgroupCacheEntry(
                        item.Key,
                        cacheItemPath,
                        ClientId,
                        null,
                        true, // pending
                        0/*0 means we haven't uploaded yet*/));

                item.Value.IsCachePending = true;

                if (--count == 0)
                    return itemsToQueue;
            }
        }
        return itemsToQueue;
    }

    /*----------------------------------------------------------------------------
        %%Function: DoThreeWayMerge
        %%Qualified: Thetacat.Model.Workgroups.Workgroup.DoThreeWayMerge

        We've gotten a coherency failure. We need to get the latest changes
        and merge them in

        optionally takes itemsForCache which are the items we are about to
        queue for caching. if passed in, then we will remove any items that
        were LocalPending but no longer are (because another client claimed them)
    ----------------------------------------------------------------------------*/
    void DoThreeWayMerge(Dictionary<Guid, MediaItem>? itemsForCache)
    {
        // first, get the latest workgroup media
        ServiceWorkgroupMediaClock mediaWithClock = _Database.GetLatestWorkgroupMediaWithClock();

        if (mediaWithClock.VectorClock == m_baseVectorClock)
            throw new CatExceptionServiceDataFailure("vector clock matches base, but we got a coherency failure. this can't be");

        if (mediaWithClock.Media != null)
        {
            // now update our local items
            foreach (ServiceWorkgroupItem media in mediaWithClock.Media)
            {
                Guid mediaId = media.MediaId ?? throw new CatExceptionServiceDataFailure();

                if (!MainWindow._AppState.Cache.Entries.ContainsKey(mediaId))
                {
                    // database has an item we didn't have. that's fine. just add it
                    AddServiceWorkgroupMediaToCache(MainWindow._AppState.Cache.Entries, media);
                    continue;
                }

                // we have the item. is it different?
                WorkgroupCacheEntry wgEntry = (WorkgroupCacheEntry)MainWindow._AppState.Cache.Entries[mediaId];
                if (wgEntry.LocalPending)
                {
                    // this was one that we were going to cache ourselves, but we don't have to now
                    if (itemsForCache != null)
                        itemsForCache.Remove(wgEntry.ID);

                    wgEntry.SetFromServiceWorkgroupItem(media);
                    wgEntry.LocalPending = false;
                }
                else if (!wgEntry.NeedsUpdate())
                {
                    wgEntry.SetFromServiceWorkgroupItem(media);
                }
                else
                {
                    // our local version is dirty. Do a 3WM
                    WorkgroupCacheEntryData server = new WorkgroupCacheEntryData(media);

                    wgEntry.DoThreeWayMerge(server);
                }
            }
        }
        // lastly, change our base to be what we just fetched
        m_baseVectorClock = mediaWithClock.VectorClock;
    }

    /*----------------------------------------------------------------------------
        %%Function: PushChangesToDatabase
        %%Qualified: Thetacat.Model.Workgroups.Workgroup.PushChangesToDatabase

        Build up a set of changes we need to make on the server
    ----------------------------------------------------------------------------*/
    public void PushChangesToDatabase(Dictionary<Guid, MediaItem>? itemsForCache)
    {
        int retryCount = 10; // retry for coherency failures 10 times

        while (retryCount-- > 0)
        {
            Dictionary<Guid, List<KeyValuePair<string, string>>> cacheChanges = new();
            List<WorkgroupCacheEntry> inserts = new();

            foreach (KeyValuePair<Guid, ICacheEntry> entry in MainWindow._AppState.Cache.Entries)
            {
                WorkgroupCacheEntry wgEntry = (WorkgroupCacheEntry)entry.Value;

                if (!wgEntry.NeedsUpdate() && !wgEntry.LocalPending)
                    continue;

                // update the vectorclock to what the new clock will be
                wgEntry.VectorClock = m_baseVectorClock + 1;

                // we're going to update here
                if (wgEntry.LocalPending)
                {
                    inserts.Add(wgEntry);
                }
                else
                {
                    cacheChanges.Add(wgEntry.ID, wgEntry.MakeUpdatePairs());
                }
            }

            if (cacheChanges.Count == 0 && inserts.Count == 0)
                return;

            // at this point we have all the changes
            try
            {
                _Database.UpdateInsertCacheEntries(m_baseVectorClock, ClientId, cacheChanges, inserts);
                m_baseVectorClock++;
                return;
            }
            catch (CatExceptionDataCoherencyFailure)
            {
                if (retryCount == 0)
                    throw;  // retry if we have failed too many times

                DoThreeWayMerge(itemsForCache);
                // fall through to continue
            }
        }
    }
}
