using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TCore.Pipeline;
using Thetacat.Logging;
using Thetacat.Types;
using Thetacat.UI;

namespace Thetacat.Model.ImageCaching;

/*----------------------------------------------------------------------------
    %%Class: PreviewImageCache
    %%Qualified: Thetacat.Model.PreviewImageCache

    This caches images for the explorer windows. 

    If an item exists in this cache, then it either has a cache image already
    (because of a previous cache item create), or it is queued in the
    background to create the image.

    conversely, if there is no entry in the cache, then there is no queued
    create (or at least, don't worry about any orphaned queued creates...its
    rare -- only when you queue the create and (currently NYI) the cache item
    is expired.
----------------------------------------------------------------------------*/
public class ImageCache
{
    public ConcurrentDictionary<Guid, ImageCacheItem> Items = new ConcurrentDictionary<Guid, ImageCacheItem>();
    private readonly ProducerConsumer<ImageLoaderWork>? m_imageLoaderPipeline;
    private const int s_picturePreviewWidth = 512;

    private bool m_fFullFidelity = false;

    public event EventHandler<ImageCacheUpdateEventArgs>? ImageCacheUpdated;

    public ImageCache(bool fFullFidelity = false)
    {
        // don't start the pipeline thread if we're under a unit test.
        if (!MainWindow.InUnitTest)
        {
            // this will start the thread which will just wait for work to do...
            m_imageLoaderPipeline = new ProducerConsumer<ImageLoaderWork>(null, DoImageLoaderWork);
            m_imageLoaderPipeline.Start();
        }
        m_fFullFidelity = fFullFidelity;
    }

    public void Close()
    {
        m_imageLoaderPipeline?.Stop();
    }

    public ImageCacheItem TryAddItem(MediaItem mediaItem, string localPath)
    {
        ImageCacheItem item = new ImageCacheItem(mediaItem.ID, localPath);

        if (!Items.TryAdd(mediaItem.ID, item))
        {
            if (!Items.TryGetValue(mediaItem.ID, out ImageCacheItem? existingItem))
            {
                throw new CatExceptionInternalFailure("Couldn't add the cache image item but then couldn't retrieve it. wicked race conditions?");
                //return item;
            }

            return existingItem;
        }

        m_imageLoaderPipeline?.Producer.QueueRecord(new ImageLoaderWork(mediaItem, item));
        return item;
    }

    public ImageCacheItem? GetAnyExistingItem(Guid key)
    {
        Items.TryGetValue(key, out ImageCacheItem? item);
        return item;
    }

    private void TriggerImageCacheUpdatedEvent(Guid mediaId)
    {
        if (ImageCacheUpdated != null)
            ImageCacheUpdated(this, new ImageCacheUpdateEventArgs() { MediaId = mediaId });
    }
    public void ResetImageForKey(Guid mediaId)
    {
        if (Items.TryGetValue(mediaId, out ImageCacheItem? item))
        {
            item.Image = null;
            TriggerImageCacheUpdatedEvent(mediaId);
        }
    }

    #region Image Loading/Threading

    class ImageLoaderWork : IPipelineBase<ImageLoaderWork>
    {
        public Guid MediaKey { get; set; }
        public string? PathToImage { get; set; }
        public double AspectRatio { get; set; }

        public ImageLoaderWork()
        {
        }

        public ImageLoaderWork(MediaItem mediaItem, ImageCacheItem cacheItem)
        {
            MediaKey = mediaItem.ID;
            PathToImage = cacheItem.LocalPath;
            AspectRatio = (double)(mediaItem.ImageWidth ?? 1.0) / (double)(mediaItem.ImageHeight ?? mediaItem.ImageWidth ?? 1.0);
        }

        public void InitFrom(ImageLoaderWork t)
        {
            MediaKey = t.MediaKey;
            PathToImage = t.PathToImage;
            AspectRatio = t.AspectRatio;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DoImageLoaderWork
        %%Qualified: Thetacat.Model.MediaExplorerCollection.DoImageLoaderWork

        This will create a bitmapimage for the path and assign it to the
        MediaExplorerItem
    ----------------------------------------------------------------------------*/
    void DoImageLoaderWork(IEnumerable<ImageLoaderWork> workItems)
    {
        foreach (ImageLoaderWork item in workItems)
        {
            if (item.PathToImage == null || item.PathToImage.Contains(".nef") || item.PathToImage.Contains(".psd"))
                continue;

            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                if (!m_fFullFidelity)
                {
                    image.DecodePixelWidth = Math.Min(s_picturePreviewWidth, (int)Math.Floor(item.AspectRatio * s_picturePreviewWidth));
                }

                image.UriSource = new Uri(item.PathToImage);
                image.EndInit();
                image.Freeze();

                Interlocked.Add(ref m_cacheSize, image.PixelHeight * image.PixelWidth * 4);
                Interlocked.Add(ref m_numImages, 1);

                MainWindow.LogForApp(EventType.Warning, $"loading image {item.PathToImage} with decode {image.DecodePixelWidth}");
                if (Items.TryGetValue(item.MediaKey, out ImageCacheItem? cacheItem))
                {
                    cacheItem.Image = image;
                    TriggerImageCacheUpdatedEvent(cacheItem.MediaId);
                }
            }
            catch (Exception e)
            {
                MainWindow.LogForApp(EventType.Critical, $"can't load image: {item.PathToImage}: {e}");
            }
        }
    }

    private long m_cacheSize = 0;
    private long m_numImages = 0;

    public long CacheSize => m_cacheSize;
    public long NumImages => m_numImages;

#endregion
}
