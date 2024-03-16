using System;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Util;

namespace Thetacat.Model.Client;

public class DerivativeItem
{
    public Guid MediaId { get; init; }
    public string MimeType { get; init; }
    public double ScaleFactor { get; init; }
    public PathSegment Path { get; init; }
    public bool Pending { get; set; }
    public bool DeletePending { get; set; }

    public DerivativeItem(Guid mediaId, string mimeType, double scaleFactor, PathSegment path)
    {
        MediaId = mediaId;
        MimeType = mimeType;
        ScaleFactor = scaleFactor;
        Path = path;
        Pending = true;
    }

    public DerivativeItem(DerivativeDbItem dbItem)
    {
        MediaId = dbItem.MediaId;
        MimeType = dbItem.MimeType;
        ScaleFactor = dbItem.ScaleFactor;
        Path = new PathSegment(dbItem.Path);
    }
}
