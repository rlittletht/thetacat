using System;
using Thetacat.Util;

namespace Thetacat.Model.Caching;

public class CacheItemDelta
{
    public DeltaType DeltaType { get; init; }
    public MediaItem MediaItem { get; init; }
    public PathSegment FullPath { get; init; }
    public string MD5 { get; init; }

    public CacheItemDelta(DeltaType deltaType, MediaItem item, PathSegment fullPath, string mD5)
    {
        DeltaType = deltaType;
        MediaItem = item;
        FullPath = fullPath;
        MD5 = mD5;
    }
}
