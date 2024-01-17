using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Client;

/*----------------------------------------------------------------------------
    %%Class: Md5Cache
    %%Qualified: Thetacat.Model.Client.Md5Cache

    This is a cache of md5 hashes, both for this session only and for
    persistent
----------------------------------------------------------------------------*/
public class Md5Cache
{
    private readonly ConcurrentDictionary<PathSegment, Md5CacheItem> m_cache = new();

    public Md5Cache(ClientDatabase client)
    {
        List<Md5CacheDbItem> dbItems = client.ReadFullMd5Cache();

        foreach (Md5CacheDbItem dbItem in dbItems)
        {
            Md5CacheItem item = new Md5CacheItem(dbItem);
            if (!m_cache.TryAdd(item.Path, item))
                throw new CatExceptionInternalFailure($"failed to add md5 cache item: {dbItem.Path}");
        }
    }

    public void CommitCacheItems()
    {
        List<Md5CacheItem> inserts = new();
        List<Md5CacheItem> deletes = new();

        foreach (KeyValuePair<PathSegment, Md5CacheItem> dbItem in m_cache)
        {
            if (dbItem.Value.Pending)
                inserts.Add(dbItem.Value);
            if (dbItem.Value.DeletePending)
                deletes.Add(dbItem.Value);
        }

        App.State.ClientDatabase.ExecuteMd5CacheUpdates(deletes, inserts);

        foreach (Md5CacheItem item in inserts)
        {
            item.Pending = false;
        }

        foreach (Md5CacheItem item in deletes)
        {
            m_cache.TryRemove(item.Path, out Md5CacheItem? removed);
        }
    }

    public void DeleteCacheItem(string localPath)
    {
        PathSegment path = new PathSegment(localPath.ToLowerInvariant());

        if (m_cache.TryGetValue(path, out Md5CacheItem? remove))
            remove.DeletePending = true;
    }

    public void AddCacheItem(string localPath, string md5)
    {
        FileInfo info = new FileInfo(localPath);

        Md5CacheItem item = new Md5CacheItem(new PathSegment(localPath.ToLowerInvariant()), md5, info.LastWriteTime, info.Length);

        m_cache.TryAdd(item.Path, item);
    }

    public bool TryLookupMd5(string localPath, out string? md5)
    {
        PathSegment path = new PathSegment(localPath.ToLowerInvariant());
        if (m_cache.TryGetValue(path, out Md5CacheItem? item))
        {
            if (!VerifyFileInfo(item))
            {
                m_cache.TryRemove(item.Path, out Md5CacheItem? removing);
                md5 = null;
                return false;
            }

            md5 = item.MD5;
            return true;
        }

        md5 = null;
        return false;
    }

    /*----------------------------------------------------------------------------
        %%Function: VerifyFileInfo
        %%Qualified: Thetacat.Model.Client.Md5Cache.VerifyFileInfo

        Return true if we can use this item's md5
    ----------------------------------------------------------------------------*/
    bool VerifyFileInfo(Md5CacheItem item)
    {
        if (item.FileInfoMatch == TriState.Maybe)
        {
            FileInfo info = new FileInfo(item.Path.Local);

            item.FileInfoMatch =
                (info.Length != item.Size
                    || (Math.Abs(info.LastWriteTime.Ticks - item.LastModified.Ticks) >= 10000000))
                    ? TriState.No
                    : TriState.Yes;
        }

        return item.FileInfoMatch == TriState.Yes;
    }

    public string GetMd5ForPathSync(string localPath)
    {
        if (TryLookupMd5(new PathSegment(localPath), out string? md5))
            return md5!;

        md5 = Checksum.GetMD5ForPathSync(localPath);
        AddCacheItem(localPath, md5);
        return md5;
    }

    public async Task<string> GetMd5ForPathAsync(string localPath)
    {
        string? md5;

        if (TryLookupMd5(localPath, out md5))
            return md5!;

        md5 = await Checksum.GetMD5ForPath(localPath);
        AddCacheItem(localPath, md5);
        return md5;
    }

    public void Close()
    {
        CommitCacheItems();
    }
}
