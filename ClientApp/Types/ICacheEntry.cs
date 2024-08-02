using System;
using System.Security.RightsManagement;
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

    public string MD5 { get; set; }

    public Guid CachedBy { get; set; }
}
