﻿using Thetacat.Util;

namespace Thetacat.Types;

public interface IMediaItemFile
{
    // fully qualified paths use backslashes
    public string FullyQualifiedPath { get; }
    public PathSegment? VirtualPath { get; }
}
