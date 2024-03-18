using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Client;

public class DerivativeItem
{
    private PathSegment? m_pathSegment;

    public Guid MediaId { get; init; }
    public string MimeType { get; init; }
    public double ScaleFactor { get; init; }
    public PathSegment Path
    {
        get => m_pathSegment ?? throw new CatExceptionInternalFailure("accessing path segment for pending item");
        set
        {
            m_pathSegment = value;
            PendingBitmap = null;
        }
    }

    public bool Pending { get; set; }
    public bool DeletePending { get; set; }
    public BitmapSource? PendingBitmap { get; set; }
    public string TransformationsKey { get; set; }
    public bool IsSaveQueued => PendingBitmap != null;

    public DerivativeItem(Guid mediaId, string mimeType, double scaleFactor, string transformationsKey, BitmapSource pendingBitmap)
    {
        MediaId = mediaId;
        MimeType = mimeType;
        ScaleFactor = scaleFactor;
        PendingBitmap = pendingBitmap;
        Pending = true;
        TransformationsKey = transformationsKey;
    }

    public DerivativeItem(Guid mediaId, string mimeType, double scaleFactor, string transformationsKey, PathSegment path)
    {
        MediaId = mediaId;
        MimeType = mimeType;
        ScaleFactor = scaleFactor;
        m_pathSegment = path;
        Pending = true;
        TransformationsKey = transformationsKey;
    }

    public DerivativeItem(DerivativeDbItem dbItem)
    {
        MediaId = dbItem.MediaId;
        MimeType = dbItem.MimeType;
        ScaleFactor = dbItem.ScaleFactor;
        TransformationsKey = dbItem.TransformationsKey;
        m_pathSegment = new PathSegment(dbItem.Path);
    }
}
