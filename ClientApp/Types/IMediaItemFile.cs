using System;
using Thetacat.Util;

namespace Thetacat.Types;

public interface IMediaItemFile
{
    // fully qualified paths use backslashes
    public string FullyQualifiedPath { get; }
    public PathSegment? VirtualPath { get; }
    public Guid? ExistingID { get; }
    public bool NeedsRepair { get; }
}
