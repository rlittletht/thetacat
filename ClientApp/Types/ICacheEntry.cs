using System;
using Thetacat.Util;

namespace Thetacat.Types;

public interface ICacheEntry
{
    public Guid ID { get; }

    // Path is the local path to this cache entry (it may not exist (yet) if
    // the entry is LocalPending
    public PathSegment Path { get; }

    public bool LocalPending { get; set; }

    public DateTime? CachedDate { get; set; }
}
