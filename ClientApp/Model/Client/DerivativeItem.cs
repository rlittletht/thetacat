using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Client;

/*----------------------------------------------------------------------------
    %%Class: DerivativeItem
    %%Qualified: Thetacat.Model.Client.DerivativeItem

    This holds a single derived item. If PendingBitmap is set, then this item
    has not been commited to disk yet. When we complete the save, then the
    path will be set and PendingBitmap will be set to null.

    Saves will be queued up using QueueSaveResampledImage and
    QueueSaveReformatImage. Both of these happen when we go to actually
    do the work of loading the image (also on a background thread)
----------------------------------------------------------------------------*/
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

    public bool HasPath => m_pathSegment != null;

    public bool Pending { get; set; }
    public bool DeletePending { get; set; }
    public BitmapSource? PendingBitmap { get; set; }
    public string TransformationsKey { get; set; }
    public bool IsSaveQueued => PendingBitmap != null;

    /*----------------------------------------------------------------------------
        %%Function: DerivativeItem
        %%Qualified: Thetacat.Model.Client.DerivativeItem.DerivativeItem

        Create a pending derivative item
    ----------------------------------------------------------------------------*/
    public DerivativeItem(Guid mediaId, string mimeType, double scaleFactor, string transformationsKey, BitmapSource pendingBitmap)
    {
        MediaId = mediaId;
        MimeType = mimeType;
        ScaleFactor = scaleFactor;
        PendingBitmap = pendingBitmap;
        Pending = true;
        TransformationsKey = transformationsKey;
    }

    /*----------------------------------------------------------------------------
        %%Function: DerivativeItem
        %%Qualified: Thetacat.Model.Client.DerivativeItem.DerivativeItem

        Create a new item from a derivative from the database
    ----------------------------------------------------------------------------*/
    public DerivativeItem(DerivativeDbItem dbItem)
    {
        MediaId = dbItem.MediaId;
        MimeType = dbItem.MimeType;
        ScaleFactor = dbItem.ScaleFactor;
        TransformationsKey = dbItem.TransformationsKey;
        m_pathSegment = new PathSegment(dbItem.Path);
    }
}
