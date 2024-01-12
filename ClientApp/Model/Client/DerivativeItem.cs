using System;
using Thetacat.Util;

namespace Thetacat.Model.Client;

public class DerivativeItem
{
    public Guid MediaId { get; init; }
    public string MimeType { get; init; }
    public double ScaleFactor { get; init; }
    public PathSegment Path { get; init; }

    public DerivativeItem(Guid mediaId, string mimeType, double scaleFactor, PathSegment path)
    {
        MediaId = mediaId;
        MimeType = mimeType;
        ScaleFactor = scaleFactor;
        Path = path;
    }
}
