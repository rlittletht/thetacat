﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Md5Caching;

/*----------------------------------------------------------------------------
    %%Class: Md5Cache
    %%Qualified: Thetacat.Model.Client.Md5Cache

    This is a cache of md5 hashes, both for this session only and for
    persistent
----------------------------------------------------------------------------*/
public class Md5Cache
{
    private readonly ConcurrentDictionary<PathSegment, Md5CacheItem> m_cache = new();

    public Md5Cache(ClientDatabase? client)
    {
        ResetMd5Cache(client);
    }

    public void ResetMd5Cache(ClientDatabase? client)
    {
        m_cache.Clear();

        if (client == null)
        {
            return;
        }

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
        List<Md5CacheItem> changes = new();

        foreach (KeyValuePair<PathSegment, Md5CacheItem> dbItem in m_cache)
        {
            if (dbItem.Value.ChangeState != ChangeState.None)
                changes.Add(dbItem.Value);
        }

        App.State.ClientDatabase?.ExecuteMd5CacheUpdates(changes);

        foreach (Md5CacheItem item in changes)
        {
            bool fDelete = item.DeletePending;
            item.ChangeState = ChangeState.None;

            if (fDelete)
                m_cache.TryRemove(item.Path, out _);
        }
    }

    public void DeleteCacheItem(string localPath)
    {
        PathSegment path = new PathSegment(localPath.ToLowerInvariant());

        if (m_cache.TryGetValue(path, out Md5CacheItem? remove))
            remove.ChangeState = ChangeState.Delete;
    }

    public void AddCacheItem(string localPath, string md5)
    {
        FileInfo info = new FileInfo(localPath);

        AddCacheFileInfo(localPath, info, md5);
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateCacheFileInfoIfNecessary
        %%Qualified: Thetacat.Model.Md5Caching.Md5Cache.UpdateCacheFileInfoIfNecessary

        Update the md5 cache for this item. if it already exists and doesn't
        differ, then
    ----------------------------------------------------------------------------*/
    public void UpdateCacheFileInfoIfNecessary(string localPath, FileInfo info, string md5)
    {
        Md5CacheItem item = new Md5CacheItem(new PathSegment(localPath.ToLowerInvariant()), md5, info.LastWriteTime, info.Length);

        if (m_cache.TryGetValue(item.Path, out Md5CacheItem? existingItem))
        {
            if (existingItem.MatchFileInfo(info) && existingItem.MD5 == md5)
                return;

            item.ChangeState = ChangeState.Update;
            m_cache.TryUpdate(item.Path, item, existingItem);
        }
        else
        {
            m_cache.TryAdd(item.Path, item);
        }
    }

    public void AddCacheFileInfo(string localPath, FileInfo info, string md5)
    {
        Md5CacheItem item = new Md5CacheItem(new PathSegment(localPath.ToLowerInvariant()), md5, info.LastWriteTime, info.Length);

        m_cache.TryAdd(item.Path, item);
    }

    public bool TryLookupCacheItem(string localPath, [MaybeNullWhen(false)] out Md5CacheItem cacheItem)
    {
        PathSegment path = new PathSegment(localPath.ToLowerInvariant());
        if (m_cache.TryGetValue(path, out cacheItem))
            return true;

        return false;
    }

    public bool TryLookupMd5(string localPath, out string? md5)
    {
        if (TryLookupCacheItem(localPath, out Md5CacheItem? item))
        {
            if (!VerifyItemAgainstFilesystem(item))
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
        %%Function: VerifyItemAgainstFilesystem
        %%Qualified: Thetacat.Model.Client.Md5Cache.VerifyItemAgainstFilesystem

        Return true if we can use this item's md5
    ----------------------------------------------------------------------------*/
    bool VerifyItemAgainstFilesystem(Md5CacheItem item)
    {
        if (item.FilesystemMatched == TriState.Maybe)
        {
            FileInfo info = new FileInfo(item.Path.Local);

            item.FilesystemMatched = item.MatchFileInfo(info) ? TriState.Yes : TriState.No;
        }

        return item.FilesystemMatched == TriState.Yes;
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
