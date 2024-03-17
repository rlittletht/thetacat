using System;
using System.Collections.Generic;
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
    public string TransformationsKey { get; set; }

    public DerivativeItem(Guid mediaId, string mimeType, double scaleFactor, string transformationsKey, PathSegment path)
    {
        MediaId = mediaId;
        MimeType = mimeType;
        ScaleFactor = scaleFactor;
        Path = path;
        Pending = true;
        TransformationsKey = transformationsKey;
    }

    public DerivativeItem(DerivativeDbItem dbItem)
    {
        MediaId = dbItem.MediaId;
        MimeType = dbItem.MimeType;
        ScaleFactor = dbItem.ScaleFactor;
        TransformationsKey = dbItem.TransformationsKey;
        Path = new PathSegment(dbItem.Path);
    }
}
