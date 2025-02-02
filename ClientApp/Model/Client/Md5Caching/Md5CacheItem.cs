using System;
using System.IO;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Md5Caching;

public class Md5CacheItem
{
    public PathSegment Path { get; init; }
    public string MD5 { get; init; }
    public DateTime LastModified { get; init; }
    public long Size { get; init; }
    public bool SessionOnly { get; init; }
    public ChangeState ChangeState { get; set; }
    public bool Pending => ChangeState == ChangeState.Create;
    public bool DeletePending => ChangeState == ChangeState.Delete;

    public TriState FilesystemMatched { get; set; }

    public Md5CacheItem(PathSegment path, string md5, DateTime lastModified, long size)
    {
        Path = path;
        MD5 = md5;
        LastModified = lastModified;
        Size = size;
        ChangeState = ChangeState.Create;
        FilesystemMatched = TriState.Yes;
    }

    public Md5CacheItem(Md5CacheDbItem dbItem)
    {
        Path = new PathSegment(dbItem.Path);
        MD5 = dbItem.MD5;
        LastModified = dbItem.LastModified;
        Size = dbItem.Size;
        ChangeState = ChangeState.None;
        SessionOnly = false;
        FilesystemMatched = TriState.Maybe;
    }

    /*----------------------------------------------------------------------------
        %%Function: MatchFileInfo
        %%Qualified: Thetacat.Model.Md5Caching.Md5CacheItem.MatchFileInfo

        Return true if this item matches the given file info (only checks the
        last modified time and the size)
    ----------------------------------------------------------------------------*/
    public bool MatchFileInfo(FileInfo info)
    {
        return info.Length == Size
            && Math.Abs(info.LastWriteTime.Ticks - LastModified.Ticks) < 10000000;
    }
}
