using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TCore;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model;

public class Cache
{
    public enum CacheType
    {
        Private,
        Workgroup,
        Unknown
    }

    public CacheType Type { get; private set; }
    public Workgroup? Workgroup { get; private set; }
    public string LocalPath { get; private set; } = string.Empty;
    public ConcurrentDictionary<Guid, ICacheEntry> Entries = new ConcurrentDictionary<Guid, ICacheEntry>();

    public static CacheType CacheTypeFromString(string? value)
    {
        if (String.Compare(value, "private", StringComparison.InvariantCultureIgnoreCase) == 0)
            return CacheType.Private;
        else if (String.Compare(value, "workgroup", StringComparison.InvariantCultureIgnoreCase) == 0)
            return CacheType.Workgroup;

        return CacheType.Unknown;
    }

    public static string StringFromCacheType(CacheType cacheType)
    {
        switch (cacheType)
        {
            case CacheType.Private:
                return "private";
            case CacheType.Workgroup:
                return "workgroup";
        }

        throw new ArgumentException("bad cache type argument");
    }

    void InitializeWorkgroupCache(TcSettings.TcSettings settings)
    {
        if (Type != CacheType.Workgroup)
            throw new InvalidOperationException("intializing a non-workgroup");

        if (!Guid.TryParse(settings.WorkgroupId, out Guid id))
            return;

        try
        {
            Workgroup = new Workgroup(id);
        }
        catch (TcSqlExceptionNoResults e)
        {
            throw new CatExceptionWorkgroupNotFound(e.Crids, e, "workgroup not found");
        }

        LocalPath = Workgroup.FullyQualifiedPath;

        // read the media items we know about
        List<ServiceWorkgroupItemClient> media = ServiceInterop.ReadWorkgroupMedia(id);

        foreach (ServiceWorkgroupItemClient mediaItem in media)
        {
            ICacheEntry entry = new WorkgroupCacheEntry(
                mediaItem.Item.MediaId ?? throw new CatExceptionServiceDataFailure(),
                PathSegment.CreateFromString(mediaItem.Item.Path),
                mediaItem.Item.CachedBy ?? throw new CatExceptionServiceDataFailure(),
                mediaItem.Item.CachedDate ?? throw new CatExceptionServiceDataFailure());

            // if we have a duplicate ID its a service failure
            if (!Entries.TryAdd(entry.ID, entry))
                throw new CatExceptionServiceDataFailure();
        }
    }

    public bool IsItemCached(Guid id)
    {
        return Entries.ContainsKey(id);
    }

    /*----------------------------------------------------------------------------
        %%Function: Cache
        %%Qualified: Thetacat.Model.Cache.Cache

        The cache handles all local operations:
            Sync from server
            Scan cache for local changes
                Sync back to server

        The cache abstracts whether this is workgroup or private
    ----------------------------------------------------------------------------*/
    public Cache(TcSettings.TcSettings settings)
    {
        CacheType cacheType = CacheTypeFromString(settings.CacheType);

        Type = cacheType;

        if (Type == CacheType.Unknown)
            return;

        if (Type == CacheType.Workgroup)
            InitializeWorkgroupCache(settings);
    }
}
