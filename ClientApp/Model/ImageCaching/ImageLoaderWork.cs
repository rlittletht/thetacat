using System;
using TCore.Pipeline;
using Thetacat.Model.Client;

namespace Thetacat.Model.ImageCaching;

class ImageLoaderWork : IPipelineWorkItemBase<ImageLoaderWork>
{
    public Derivatives.OnDerivativeWorkCompleteDelegate? OnDerivativeWorkComplete { get; private set; }
    public Guid Cookie => MediaKey;
    public Guid MediaKey { get; set; }
    public string MD5 { get; set; } = string.Empty;
    public string? PathToImage { get; set; }
    public double AspectRatio { get; set; }
    public int? OriginalWidth { get; set; }
    public Transformations Transformations { get; set; }
    public bool PurgeAndReload { get; set; }

    public ImageLoaderWork()
    {
        Transformations = Transformations.Empty;
    }

    public ImageLoaderWork(
        MediaItem mediaItem, string md5, ImageCacheItem cacheItem, bool isPurgeAndReload = false,
        Derivatives.OnDerivativeWorkCompleteDelegate? onDerivativeWorkComplete = null)
    {
        MediaKey = mediaItem.ID;
        PathToImage = cacheItem.LocalPath;
        AspectRatio = (double)(mediaItem.ImageWidth ?? 1.0) / (double)(mediaItem.ImageHeight ?? mediaItem.ImageWidth ?? 1.0);
        OriginalWidth = mediaItem.ImageWidth;
        Transformations = new Transformations(mediaItem);
        MD5 = md5;
        OnDerivativeWorkComplete = onDerivativeWorkComplete;
    }

    public void InitFrom(ImageLoaderWork t)
    {
        PurgeAndReload = t.PurgeAndReload;
        MediaKey = t.MediaKey;
        PathToImage = t.PathToImage;
        AspectRatio = t.AspectRatio;
        OriginalWidth = t.OriginalWidth;
        Transformations = new Transformations(t.Transformations.TransformationsKey);
        OnDerivativeWorkComplete = t.OnDerivativeWorkComplete;
        MD5 = t.MD5;
    }
}
