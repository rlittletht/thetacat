using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.Imaging;
using NUnit.Framework.Constraints;
using TCore.Pipeline;
using Thetacat.Logging;
using Thetacat.Model.ImageCaching;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model.Client;

/*----------------------------------------------------------------------------
    %%Class: Derivatives
    %%Qualified: Thetacat.Model.Client.Derivatives

    Derivatives are stored locally to the client and are images derived from
    the original media. A smaller version of the image for preview purposes,
    for example, or a more readily displayable version (even at the original
    resolution)

    We have some lookup dictionaries to make it easier to find the
    derivative you want
----------------------------------------------------------------------------*/
public class Derivatives
{
    private readonly ProducerConsumer<DerivativeWork>? m_derivativeWorkPipeline;

    // all of the derivatives we know about for each mediaitem. this is the master list
    private readonly Dictionary<Guid, List<DerivativeItem>> m_mediaDerivatives = new();

    // for each media item, map mime-type to the list of derivatives we have in that format
    private readonly Dictionary<Guid, Dictionary<string, List<DerivativeItem>>> m_mediaFormatDerivatives = new();

    // for each media item, an ordered list of scaled media -- smallest to largest
    private readonly Dictionary<Guid, SortedList<double, DerivativeItem>> m_scaledMediaDerivatives = new();

    private readonly object m_lock = new Object();

    public void AddDerivative(DerivativeItem item)
    {
        lock (m_lock)
        {
            List<DerivativeItem> items = ListSupport.AddItemToMappedList(m_mediaDerivatives, item.MediaId, item);
            List<DerivativeItem> mimeList = ListSupport.AddItemToMappedMapList(m_mediaFormatDerivatives, item.MediaId, item.MimeType, item);
            SortedList<double, DerivativeItem> scaledList = ListSupport.AddItemToMappedSortedList(
                m_scaledMediaDerivatives,
                item.MediaId,
                item.ScaleFactor,
                item);
        }
    }

    public void QueueSaveResampledImage(MediaItem item, BitmapImage resampledImage)
    {
        m_derivativeWorkPipeline?.Producer.QueueRecord(new DerivativeWork(item, resampledImage));
    }

    public Derivatives(ClientDatabase client)
    {
        List<DerivativeDbItem> dbItems = client.ReadDerivatives();

        foreach (DerivativeDbItem dbItem in dbItems)
        {
            DerivativeItem item = new DerivativeItem(dbItem);
            AddDerivative(item);
        }

        if (!MainWindow.InUnitTest)
        {
            // this will start the thread which will just wait for work to do...
            m_derivativeWorkPipeline = new ProducerConsumer<DerivativeWork>(null, DoDerivativeWork);
            m_derivativeWorkPipeline.Start();
        }
    }

    public void Close()
    {
        m_derivativeWorkPipeline?.Stop();
    }

    public void CommitDerivatives()
    {
        List<DerivativeItem> inserts = new();
        List<DerivativeItem> deletes = new();

        foreach (KeyValuePair<Guid, List<DerivativeItem>> dbItem in m_mediaDerivatives)
        {
            foreach (DerivativeItem item in dbItem.Value)
            {
                if (item.Pending)
                    inserts.Add(item);
                if (item.DeletePending)
                    deletes.Add(item);
            }
        }

        App.State.ClientDatabase.ExecuteDerivativeUpdates(deletes, inserts);

        foreach (DerivativeItem item in inserts)
        {
            item.Pending = false;
        }

        foreach (DerivativeItem item in deletes)
        {
            ListSupport.RemoveItemFromMappedList(m_mediaDerivatives, item.MediaId, item);
            ListSupport.RemoveItemFromMappedMapList(m_mediaFormatDerivatives, item.MediaId, item.MimeType, item);
            ListSupport.RemoveItemFromMappedSortedList(m_scaledMediaDerivatives, item.MediaId, item.ScaleFactor);
        }
    }

    public static int CompareDoubles(double a, double b, int precision)
    {
        double eps = 1.0 / (Math.Pow(10, precision));
        double d = a - b;

        if (Math.Abs(d) < eps)
            return 0;

        return (d < 0) ? -1 : 1;
    }

    public void DeleteDerivativeItem(Guid mediaId, string mimeType, double scaleFactor)
    {
        if (!m_mediaDerivatives.TryGetValue(mediaId, out List<DerivativeItem>? items))
            return;

        foreach (DerivativeItem item in items)
        {
            if (CompareDoubles(item.ScaleFactor, scaleFactor, 4) == 0 && item.MimeType == mimeType)
            {
                item.DeletePending = true;
                return;
            }
        }
    }

    public bool TryGetResampledDerivative(Guid mediaId, double scaleFactor, [MaybeNullWhen(false)] out DerivativeItem matched)
    {
        if (!m_scaledMediaDerivatives.TryGetValue(mediaId, out SortedList<double, DerivativeItem>? items))
        {
            matched = null;
            return false;
        }

        DerivativeItem? itemLast = null;

        foreach (KeyValuePair<double, DerivativeItem> item in items)
        {
            if (CompareDoubles(item.Key, scaleFactor, 4) >= 0)
            {
                if (itemLast == null)
                {
                    matched = item.Value;
                    return true;
                }

                matched = itemLast;
                return true;
            }
        }

        matched = null;
        return false;
    }

    #region Derivative Work

    class DerivativeWork : IPipelineBase<DerivativeWork>
    {
        public enum WorkType
        {
            ResampleImage, // take a BitmapImage and write it as a jpg
        }

        public Guid MediaKey { get; private set; }
        public WorkType Type { get; private set; }
        public int OriginalWidth { get; private set; }
        public int OriginalHeight { get; private set; }
        public BitmapImage? Image { get; private set; }

        public DerivativeWork()
        {
        }

        public DerivativeWork(MediaItem mediaItem, BitmapImage resampledImage)
        {
            Type = WorkType.ResampleImage;
            OriginalHeight = mediaItem.ImageHeight ?? resampledImage.PixelHeight;
            OriginalWidth = mediaItem.ImageWidth ?? resampledImage.PixelWidth;
            Image = resampledImage;
            MediaKey = mediaItem.ID;
        }

        public void InitFrom(DerivativeWork t)
        {
            Type = t.Type;
            OriginalHeight = t.OriginalHeight;
            OriginalWidth = t.OriginalWidth;
            MediaKey = t.MediaKey;
            Image = t.Image;
        }
    }

    void DoDerivativeWork(IEnumerable<DerivativeWork> workItems)
    {
        foreach (DerivativeWork item in workItems)
        {
            if (item.Type != DerivativeWork.WorkType.ResampleImage || item.Image == null)
                continue;

            if (App.State.Settings.DerivativeCache == null)
                throw new CatExceptionInternalFailure("no derivative cache root set");

            PathSegment? cacheRoot = new PathSegment(App.State.Settings.DerivativeCache);

            // take the bitmapimage and write a jpg for it
            PathSegment destinationDir = PathSegment.Join(cacheRoot, "cat-derivatives/resampled");
            if (!Directory.Exists(destinationDir.Local))
                Directory.CreateDirectory(destinationDir.Local);

            double scale = (double)item.Image.PixelWidth / item.OriginalWidth;

            PathSegment destination = PathSegment.Join(destinationDir, $"{item.MediaKey}-{item.Image.PixelWidth}x{item.Image.PixelHeight}.jpg");

            using FileStream stream = new(destination.Local, FileMode.Create);

            JpegBitmapEncoder encoder =
                new()
                {
                    QualityLevel = 66
                };
            encoder.Frames.Add(BitmapFrame.Create(item.Image));
            encoder.Save(stream);

            DerivativeItem newItem = new DerivativeItem(item.MediaKey, "image/jpeg", scale, destination);
            App.State.Derivatives.AddDerivative(newItem);
        }
    }

#endregion
}
