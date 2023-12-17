using System;
using System.Collections.Generic;
using MetadataExtractor;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TCore;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Workgroups;

// LocalPending means THIS client owns caching it and we haven't cached it yet
// VectorClock == 0 means that this entry exists local only and needs to be uploaded
public class WorkgroupCacheEntry : ICacheEntry
{
    private WorkgroupCacheEntryData m_currentEntry;
    private WorkgroupCacheEntryData? m_baseEntry;

    public Guid ID => m_currentEntry.ID;
    public PathSegment Path
    {
        get => m_currentEntry.Path;
        set
        {
            EnsureBase();
            m_currentEntry.Path = value;
        }
    }

    public Guid CachedBy
    {
        get => m_currentEntry.CachedBy;
        set
        {
            EnsureBase();
            m_currentEntry.CachedBy = value;
        }
    }

    public DateTime? CacheDate
    {
        get => m_currentEntry.CacheDate;
        set
        {
            EnsureBase();
            m_currentEntry.CacheDate = value;
        }
    }

    public int? VectorClock
    {
        get => m_currentEntry.VectorClock;
        set
        {
            EnsureBase();
            m_currentEntry.VectorClock= value;
        }
    }

    public bool LocalPending { get; set; }

    void EnsureBase()
    {
        if (m_baseEntry == null)
            m_baseEntry = new WorkgroupCacheEntryData(m_currentEntry);
    }

    public WorkgroupCacheEntry(Guid id, PathSegment path, Guid cachedBy, DateTime? cacheDate, bool localPending, int? vectorClock)
    {
        m_currentEntry = new WorkgroupCacheEntryData(id, path, cachedBy, cacheDate, vectorClock);
        LocalPending = localPending;
    }

    public List<KeyValuePair<string, string>> MakeUpdatePairs()
    {
        List<KeyValuePair<string, string>> updates = new List<KeyValuePair<string, string>>();

        if (m_baseEntry == null)
            return updates;

        if (string.Compare(m_baseEntry!.Path, m_currentEntry.Path, StringComparison.CurrentCultureIgnoreCase) != 0)
            updates.Add(new KeyValuePair<string, string>("path", $"'{Sql.Sqlify(m_currentEntry.Path)}'"));

        if (m_baseEntry!.CachedBy != m_currentEntry.CachedBy)
            updates.Add(new KeyValuePair<string, string>("cachedBy", $"'{m_currentEntry.CachedBy.ToString()}'"));

        if (m_baseEntry!.CacheDate != m_currentEntry.CacheDate)
            updates.Add(new KeyValuePair<string, string>("cachedDate", Sql.Nullable(m_currentEntry.CacheDate?.ToString())));

        if (m_baseEntry!.VectorClock != m_currentEntry.VectorClock)
            updates.Add(new KeyValuePair<string, string>("vectorClock", Sql.Nullable(m_currentEntry.VectorClock)));

        return updates;
    }

    public bool NeedsUpdate()
    {
        if (m_baseEntry == null)
            return false;

        // compare all items
        List<KeyValuePair<string, string>> updates = MakeUpdatePairs();

        return updates.Count != 0;
    }

    /*----------------------------------------------------------------------------
        %%Function: SetFromServiceWorkgroupItem
        %%Qualified: Thetacat.Model.Workgroups.WorkgroupCacheEntry.SetFromServiceWorkgroupItem

        Set the internals from the service workgroup item, clear IsLocal, and
        clear the base (we are making this identical to what's in the database)
    ----------------------------------------------------------------------------*/
    public void SetFromServiceWorkgroupItem(ServiceWorkgroupItem item)
    {
        m_baseEntry = null;

        m_currentEntry.Path = new PathSegment(item.Path ?? throw new CatExceptionServiceDataFailure());
        m_currentEntry.CachedBy = item.CachedBy ?? throw new CatExceptionServiceDataFailure();
        m_currentEntry.CacheDate = item.CachedDate;
        m_currentEntry.VectorClock = item.VectorClock;
        LocalPending = false;
    }

    /*----------------------------------------------------------------------------
        %%Function: DoThreeWayMerge
        %%Qualified: Thetacat.Model.Workgroups.WorkgroupCacheEntry.DoThreeWayMerge

        do a 3WM: base, local, server.

        generally, server will win since that is what got committed to the database
    ----------------------------------------------------------------------------*/
    public void DoThreeWayMerge(WorkgroupCacheEntryData server)
    {
        if (server.ID != ID)
            throw new CatExceptionInternalFailure("ID can't change in 3WM");

        if (m_baseEntry == null)
        {
            // everything is in conflict. server wins
            m_currentEntry = new WorkgroupCacheEntryData(server);
            return;
        }

        if (server.Path != m_baseEntry.Path)
        {
            m_baseEntry.Path = new PathSegment(server.Path);
            m_currentEntry.Path = new PathSegment(server.Path);
        }

        if (server.CachedBy != m_baseEntry.CachedBy)
        {
            m_baseEntry.CachedBy = server.CachedBy;
            m_currentEntry.CachedBy = server.CachedBy;
        }

        if (server.CacheDate != m_baseEntry.CacheDate)
        {
            m_baseEntry.CacheDate = server.CacheDate;
            m_currentEntry.CacheDate = server.CacheDate;
        }

        if (server.VectorClock != m_baseEntry.VectorClock)
        {
            m_baseEntry.VectorClock = server.VectorClock;
            m_currentEntry.VectorClock = m_baseEntry.VectorClock;
        }

        // now let's see if there's anything left to update now...
        if (!NeedsUpdate())
            m_baseEntry = null;
    }
}
