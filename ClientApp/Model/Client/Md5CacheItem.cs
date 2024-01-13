using System;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Client;

public class Md5CacheItem
{
    public PathSegment Path { get; init; }
    public string MD5 { get; init; }
    public DateTime LastModified { get; init; }
    public long Size { get; init; }
    public bool SessionOnly { get; init; }
    public bool Pending { get; set; }
    public bool DeletePending { get; set; }
    public TriState FileInfoMatch { get; set; }

    public Md5CacheItem(PathSegment path, string md5, DateTime lastModified, long size)
    {
        Path = path;
        MD5 = md5;
        LastModified = lastModified;
        Size = size;
        Pending = true;
        FileInfoMatch = TriState.Yes;
    }

    public Md5CacheItem(Md5CacheDbItem dbItem)
    {
        Path = new PathSegment(dbItem.Path);
        MD5 = dbItem.MD5;
        LastModified = dbItem.LastModified;
        Size = dbItem.Size;
        Pending = false;
        DeletePending = false;
        SessionOnly = false;
        FileInfoMatch = TriState.Maybe;
    }
}
