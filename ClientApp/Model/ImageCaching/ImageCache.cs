using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV.CvEnum;
using Emgu.CV;
using TCore.Pipeline;
using Thetacat.Logging;
using Thetacat.Model.Client;
using Thetacat.Types;
using Path = System.IO.Path;

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

//    private const int s_picturePreviewWidth = 512;
    private const int s_picturePreviewWidth = 260;

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

            if (existingItem.IsLoadQueued)
                return existingItem;

            item = existingItem;
        }

        item.IsLoadQueued = true;
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
            item.IsLoadQueued = false;
            TriggerImageCacheUpdatedEvent(mediaId);
        }
    }

#region Image Loading/Threading

    class ImageLoaderWork : IPipelineBase<ImageLoaderWork>
    {
        public Guid MediaKey { get; set; }
        public string? PathToImage { get; set; }
        public double AspectRatio { get; set; }
        public int? OriginalWidth { get; set; }

        public ImageLoaderWork()
        {
        }

        public ImageLoaderWork(MediaItem mediaItem, ImageCacheItem cacheItem)
        {
            MediaKey = mediaItem.ID;
            PathToImage = cacheItem.LocalPath;
            AspectRatio = (double)(mediaItem.ImageWidth ?? 1.0) / (double)(mediaItem.ImageHeight ?? mediaItem.ImageWidth ?? 1.0);
            OriginalWidth = mediaItem.ImageWidth;
        }

        public void InitFrom(ImageLoaderWork t)
        {
            MediaKey = t.MediaKey;
            PathToImage = t.PathToImage;
            AspectRatio = t.AspectRatio;
            OriginalWidth = t.OriginalWidth;
        }
    }

    public static readonly HashSet<string> SkipExtensions =
        new HashSet<string>()
        {
            ".mov", ".mp4"
        };

    public static readonly HashSet<string> ComingSoonExtensions =
        new HashSet<string>()
        {
            ".psd" // , ".jp2"
        };

    public static readonly HashSet<string> ReformatExtensions =
        new HashSet<string>()
        {
            ".nef", ".psd", ".jp2"
        };

    private BitmapImage LoadBitmapFromPath(string path, int? scaleWidth)
    {
        bool ignoreColorProfile = false;

        while (true)
        {
            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                if (scaleWidth != null)
                {
                    image.DecodePixelWidth = scaleWidth.Value;
                }

                if (ignoreColorProfile)
                {
                    image.CreateOptions |= BitmapCreateOptions.IgnoreColorProfile;
                }

                image.UriSource = new Uri(path);
                image.EndInit();
                image.Freeze();

                return image;
            }
            catch (FileFormatException)
            {
                if (ignoreColorProfile)
                    throw;
                ignoreColorProfile = true;
            }
        }
    }

    private BitmapSource LoadThroughEmgu(string filename, int? scaleWidth)
    {
        using Mat mat = Emgu.CV.CvInvoke.Imread(filename, ImreadModes.Unchanged);

  //      using Mat matOut = new Mat(512, 512, mat.Depth, mat.NumberOfChannels);
//

        return mat.ToBitmapSource();
//        Array ary = matOut.GetData(
//        CvInvoke.ResizeForFrame(mat, matOut, new System.Drawing.Size(512, 512));
//        mat.Save($"c:\\temp\\{baseName}-1.jpg");
//        string outFile = $"c:\\temp\\{baseName}-2.jpg";
//        matOut.Save(outFile);
//
//        img.Source = new BitmapImage(new Uri(outFile));
//        // PortableImage image = J2kImage.FromFile(filename);
//
//        //                BitmapSource bsrc = BitmapSource.Create(mat.Width, mat.Height, 300, 300, PixelFormats.Bgr24, ))
//        //                img.Source = new CachedBitmap()
    }

    public static BitmapSource CreatePlaceholderImage(string text, double size = 36.0)
    {
        DrawingVisual visual = new DrawingVisual();
        DrawingContext context = visual.RenderOpen();

        Point pt = new Point(10, 10);
        Brush brush = Brushes.Blue;
        DpiScale dpi = App.State.DpiScale;

        context.DrawRectangle(Brushes.White, null, new Rect(0, 0, 512, 512));
        context.DrawText(
            new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Calibri"),
                size,
                brush,
                dpi.PixelsPerDip),
            pt);

        context.Close();

        RenderTargetBitmap bitmap = new RenderTargetBitmap(512, 512, 300, 300, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();

        return bitmap;
    }

    private BitmapImage LoadBitmapThroughDecoder(string path, int? scaleWidth)
    {
        bool ignoreColorProfile = false;
        string extension = Path.GetExtension(path.ToLowerInvariant());

        while (true)
        {
            try
            {
                BitmapSource source;

                try
                {
                    if (SkipExtensions.Contains(extension))
                    {
                        source = CreatePlaceholderImage($"{extension.ToUpperInvariant()} FILE");
                    }
                    else if (ComingSoonExtensions.Contains(extension))
                    {
                        source = CreatePlaceholderImage($"{extension.ToUpperInvariant()} FILE");
                    }
                    else
                    {
                        source = LoadBitmapFromPath(path, scaleWidth);
                    }

                    // this will fail with some (all?) NEF files trying to copy the metadata, even if we specify to ignore the color
                    // profile. LoadBitmapFromPath seems to recover from this.

                    // its unclear if we lose anything by using LoadBitmapFromPath (which just loads creates the BitmapImage using
                    // the path. Some forum comments imply that you miss out on some codecs just going the BitmapImage route.

                    // if that turns out to be the case, then we can first try using BitmapDecoder, and if it fails we can
                    // fall back to BitmapImage, then call back on LoadThroughEmgu (which will be necessary for jp2 files, at the very
                    // least.

//                    BitmapCreateOptions options = ignoreColorProfile ? BitmapCreateOptions.IgnoreColorProfile : BitmapCreateOptions.None;
//                    BitmapDecoder decoder = BitmapDecoder.Create(new Uri(path), BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
//
//                    BitmapFrame frame = decoder.Frames[0];
//                    source = frame;
//
//                    if (source.Metadata != null)
//                    {
//                        ImageMetadata cloned = source.Metadata.Clone();
//                    }
                }
                catch
                {
                    // don't try to use emgu for NEF files -- it will just get the thumbnail. rethrow in this case
                    if (path.ToLowerInvariant().EndsWith(".nef"))
                        throw;
                    source = LoadThroughEmgu(path, scaleWidth);
                }

                JpegBitmapEncoder encoder = new();

                encoder.QualityLevel = 100;
                encoder.Frames.Add(BitmapFrame.Create(source));

                using MemoryStream memoryStream = new MemoryStream();
                encoder.Save(memoryStream);

                BitmapImage image = new();
                image.BeginInit();
                if (scaleWidth != null)
                {
                    image.DecodePixelWidth = scaleWidth.Value;
                }

                if (ignoreColorProfile)
                {
                    image.CreateOptions |= BitmapCreateOptions.IgnoreColorProfile;
                }

                image.StreamSource = new MemoryStream(memoryStream.ToArray());
                image.EndInit();
                image.Freeze();

                return image;
            }
            catch (FileFormatException)
            {
                if (ignoreColorProfile)
                    throw;
                ignoreColorProfile = true;
            }
        }
    }

    private static Dictionary<string, int> s_formatPriorities =
        new()
        {
            { "image/png", 0 },
            { "image/bmp", 1 },
            { "image/gif", 5 },
            { "image/jpeg", 10 },
        };

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
            if (item.PathToImage == null)
            {
                MainWindow.LogForApp(EventType.Warning, $"skipping null path");
                continue;
            }

            string lowerPath = item.PathToImage.ToLowerInvariant();
            string extension = Path.GetExtension(lowerPath);

            DerivativeItem? formatDerivative = null;
            BitmapImage? fullImage = null;
            string? pathToFullImage = null;
            int pixelsCached = 0;

            MediaItem mediaItem = App.State.Catalog.GetMediaFromId(item.MediaKey);
            try
            {
                // first get a readable source for this image
                if (ReformatExtensions.Contains(extension) || ComingSoonExtensions.Contains(extension) || SkipExtensions.Contains(extension))
                {
                    if (!App.State.Derivatives.TryGetFormatDerivative(item.MediaKey, s_formatPriorities, out formatDerivative))
                    {
                        // we have to get a full fidelity format derivative
                        fullImage = LoadBitmapThroughDecoder(item.PathToImage, null);
                        App.State.Derivatives.QueueSaveReformatImage(mediaItem, fullImage);
                    }
                    else
                    {
                        pathToFullImage = formatDerivative.Path.Local;
                    }
                }

                // figure out if we are resampling this image
                double scaleFactor =
                    (m_fFullFidelity || item.OriginalWidth == null)
                        ? 1.0
                        : (double)Math.Min(s_picturePreviewWidth, (int)Math.Floor(item.AspectRatio * s_picturePreviewWidth)) / item.OriginalWidth.Value;
                int targetWidth = 0;

                if (Derivatives.CompareDoubles(scaleFactor, 1.0, 4) != 0)
                    targetWidth = (int)Math.Floor(item.OriginalWidth!.Value * scaleFactor);

                string path = item.PathToImage;

                // if we have a resampled derivative, use it
                // else if we have a fullImage, use it to scale to a resampled derivative (and queue it as a new derivative)
                // else if we have a pathToFullImage, use that as the source to resample (and queue it as a new derivative)
                // else use the original ImagePathToSourceas the source to resample (and queue it as a new derivative)

                DerivativeItem? derivative = null;
                BitmapSource? targetBitmap = null;

                // see if there is a derivative for this
                if (!m_fFullFidelity)
                {
                    if (App.State.Derivatives.TryGetResampledDerivative(item.MediaKey, scaleFactor, out derivative, 0.02))
                    {
                        path = derivative.Path.Local;
                    }
                    else if (fullImage != null)
                    {
                        TransformedBitmap transformed = new TransformedBitmap(fullImage, new ScaleTransform(scaleFactor, scaleFactor));
                        pixelsCached = transformed.PixelHeight * transformed.PixelWidth * 4;

                        transformed.Freeze();
                        targetBitmap = transformed;
                    }
                    else if (pathToFullImage != null)
                    {
                        path = pathToFullImage;
                    }
                }
                else
                {
                    if (fullImage != null)
                        targetBitmap = fullImage;
                    else if (pathToFullImage != null)
                        path = pathToFullImage;
                }

                if (targetBitmap == null)
                {
                    BitmapImage image = LoadBitmapFromPath(path, m_fFullFidelity ? null : targetWidth);
                    pixelsCached = image.PixelHeight * image.PixelWidth * 4;
                    targetBitmap = image;
                }

                Interlocked.Add(ref m_cacheSize, pixelsCached);
                Interlocked.Add(ref m_numImages, 1);

//                MainWindow.LogForApp(EventType.Warning, $"loading image {item.PathToImage} with decode {scaleWidth}");


                if (derivative == null && !m_fFullFidelity)
                    App.State.Derivatives.QueueSaveResampledImage(mediaItem, targetBitmap);

                if (Items.TryGetValue(item.MediaKey, out ImageCacheItem? cacheItem))
                {
                    cacheItem.Image = targetBitmap;
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
