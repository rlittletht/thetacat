using System;
using Thetacat.Util;

namespace Thetacat.Types;

public interface ICacheEntry
{
    public Guid ID { get; }
    public PathSegment Path { get; }
}
