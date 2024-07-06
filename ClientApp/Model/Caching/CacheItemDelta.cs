using System;
using Thetacat.Util;

namespace Thetacat.Model.Caching;

public class CacheItemDelta
{
    public DeltaType DeltaType { get; init; }
    public Guid Id { get; init; }

    public CacheItemDelta(DeltaType deltaType, Guid id)
    {
        DeltaType = deltaType;
        Id = id;
    }
}
