﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using TCore.Pipeline;
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
    private ProducerConsumer<DerivativeWork>? m_derivativeWorkPipeline;

    // all of the derivatives we know about for each mediaitem. this is the master list
    private readonly Dictionary<Guid, List<DerivativeItem>> m_mediaDerivatives = new();

    // for each media item, map mime-type to the list of derivatives we have in that format
    private readonly Dictionary<Guid, Dictionary<string, List<DerivativeItem>>> m_mediaFormatDerivatives = new();

    // for each media item, an ordered list of scaled media -- smallest to largest
    private readonly Dictionary<Guid, SortedList<double, List<DerivativeItem>>> m_scaledMediaDerivatives = new();

    private readonly object m_lock = new Object();

    public void AddDerivative(DerivativeItem item)
    {
        lock (m_lock)
        {
            List<DerivativeItem> items = ListSupport.AddItemToMappedList(m_mediaDerivatives, item.MediaId, item);
            List<DerivativeItem> mimeList = ListSupport.AddItemToMappedMapList(m_mediaFormatDerivatives, item.MediaId, item.MimeType, item);

            SortedList<double, List<DerivativeItem>> scaledList = ListSupport.AddItemToMappedSortedListOfList(
                m_scaledMediaDerivatives,
                item.MediaId,
                item.ScaleFactor,
                item);
        }
    }

    DerivativeItem CreateDerivativeItemForWork(DerivativeWork item)
    {
        if (item.Image == null)
            throw new CatExceptionInternalFailure("null image");

        double scale;

        scale = item.RequestedScaleFactor;

        return new DerivativeItem(item.MediaKey, "image/jpeg", scale, item.TransformationsKey, item.Image);
    }

    public DerivativeItem GetMatchingDerivativeItem(DerivativeItem match)
    {
        lock (m_lock)
        {
            if (!m_mediaDerivatives.TryGetValue(match.MediaId, out List<DerivativeItem>? items))
                throw new CatExceptionInternalFailure("can't find matching derivative item");

            foreach (DerivativeItem item in items)
            {
                if (item.IsSaveQueued
                    && item.TransformationsKey == match.TransformationsKey
                    && Math.Abs(item.ScaleFactor - match.ScaleFactor) < 0.0001
                    && item.MimeType == match.MimeType)
                {
                    return item;
                }
            }

            throw new CatExceptionInternalFailure("can't find matching derivative item");
        }
    }


    void QueueDerivativeWork(DerivativeWork work)
    {
        DerivativeItem item = CreateDerivativeItemForWork(work);

        App.State.Derivatives.AddDerivative(item);
        m_derivativeWorkPipeline?.Producer.QueueRecord(work);
    }

    public void QueueSaveResampledImage(MediaItem item, Transformations transformations, BitmapSource resampledImage, double requestedScaleFactor)
    {
        QueueDerivativeWork(new DerivativeWork(item, resampledImage, false, transformations.TransformationsKey, requestedScaleFactor));
    }

    public void QueueSaveReformatImage(MediaItem item, Transformations transformations, BitmapSource reformattedImage)
    {
        QueueDerivativeWork(new DerivativeWork(item, reformattedImage, true, transformations.TransformationsKey, 1.0));
    }

    public void DeleteMediaItem(Guid id)
    {
        if (m_mediaDerivatives.TryGetValue(id, out List<DerivativeItem>? items))
        {
            foreach (DerivativeItem item in items)
            {
                try
                {
                    if (File.Exists(item.Path.Local))
                        File.Delete(item.Path.Local);
                }
                catch
                {
                    // we're going to ignore a file deletion failure...we'll just overwrite it if we encounter it again.
                }
            }
        }

        m_mediaDerivatives.Remove(id);
        m_mediaFormatDerivatives.Remove(id);
        m_scaledMediaDerivatives.Remove(id);

        App.State.ClientDatabase?.DeleteMediaDerivatives(id);
    }

    public void ResetDerivatives(ClientDatabase? client)
    {
        // clear all the derivatives we have, but leave the pipeline running
        m_mediaDerivatives.Clear();
        m_mediaFormatDerivatives.Clear();
        m_scaledMediaDerivatives.Clear();

        if (client == null)
            return;

        List<DerivativeDbItem> dbItems = client.ReadDerivatives();

        foreach (DerivativeDbItem dbItem in dbItems)
        {
            DerivativeItem item = new(dbItem);
            AddDerivative(item);
        }
    }

    public Derivatives(ClientDatabase? client)
    {
        ResetDerivatives(client);

        if (!MainWindow.InUnitTest)
        {
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

        App.State.ClientDatabase?.ExecuteDerivativeUpdates(deletes, inserts);

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

    public static int CompareDoublesWithEpsilon(double a, double b, double eps)
    {
        double d = a - b;

        if (Math.Abs(d) < eps)
            return 0;

        return (d < 0) ? -1 : 1;
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

    private Guid bad = new Guid("9686e696-6cfa-46ec-99fd-baba3342c053");

    /*----------------------------------------------------------------------------
        %%Function: TryGetFormatDerivative
        %%Qualified: Thetacat.Model.Client.Derivatives.TryGetFormatDerivative

        Try to get a format derivative for this media item. mimeTypeAccepted is
        a mapping of mime types to their priority, with 0 being highest priority

        this requires a scaleFactor of at least 1.0
    ----------------------------------------------------------------------------*/
    public bool TryGetFormatDerivative(Guid mediaId, Dictionary<string, int> mimeTypesAccepted, Transformations transformations, [MaybeNullWhen(false)] out DerivativeItem matched, out BitmapSource? pendingBitmap)
    {
        Debug.Assert(mediaId != bad);

        pendingBitmap = null;
        if (!m_mediaDerivatives.TryGetValue(mediaId, out List<DerivativeItem>? items))
        {
            matched = null;
            return false;
        }

        DerivativeItem? lastMatch = null;
        int lastPriority = -1;

        foreach (DerivativeItem item in items)
        {
            if (!transformations.IsEqualTransformations(item.TransformationsKey))
                continue;

            if (CompareDoubles(item.ScaleFactor, 1.0, 4) < 0)
                continue;

            if (mimeTypesAccepted.TryGetValue(item.MimeType, out int priority))
            {
                if (lastMatch == null)
                {
                    lastMatch = item;
                    lastPriority = priority;
                }

                if (priority == 0)
                    break;

                if (priority < lastPriority)
                {
                    lastMatch = item;
                    lastPriority = priority;
                }
            }
        }

        matched = lastMatch;
        pendingBitmap = matched?.PendingBitmap;
        return matched != null;
    }

    /*----------------------------------------------------------------------------
        %%Function: TryGetResampledDerivative
        %%Qualified: Thetacat.Model.Client.Derivatives.TryGetResampledDerivative

        Find a resampled derivative for this media item. If an epsilon is not
        provided, then it will return first item matching or greater than the
        scaling factor.

        if an epsilon is provided, then return a scaling factor within that
        epsilon or false.
    ----------------------------------------------------------------------------*/
    public bool TryGetResampledDerivative(Guid mediaId, double scaleFactor, Transformations transformations, [MaybeNullWhen(false)] out DerivativeItem matched, out BitmapSource? pendingBitmap, double? epsilon = null)
    {
        pendingBitmap = null;

        if (!m_scaledMediaDerivatives.TryGetValue(mediaId, out SortedList<double, List<DerivativeItem>>? items))
        {
            matched = null;
            return false;
        }

        // this is the most recent returnable value (if epsilon is set, then it becomes the highest
        // usable value; else its the nearest equal or greatest value)
        DerivativeItem? itemLast = null;

        foreach (KeyValuePair<double, List<DerivativeItem>> kvpItems in items)
        {
            DerivativeItem? transformedMatch = null;

            foreach (DerivativeItem item in kvpItems.Value)
            {
                if (!transformations.IsEqualTransformations(item.TransformationsKey))
                    continue;

                transformedMatch = item;
            }

            if (transformedMatch == null)
                continue;

            if (epsilon != null)
            {
                int comp = CompareDoublesWithEpsilon(kvpItems.Key, scaleFactor, epsilon.Value);
                if (comp == 0)
                {
                    itemLast = transformedMatch;
                    continue;
                }

                if (comp > 0)
                {
                    matched = itemLast;
                    pendingBitmap = matched?.PendingBitmap;

                    return matched != null;
                }
            }
            else
            {
                if (CompareDoubles(kvpItems.Key, scaleFactor, 4) >= 0)
                {
                    if (itemLast == null)
                    {
                        matched = transformedMatch;
                        return true;
                    }

                    matched = itemLast;
                    pendingBitmap = matched.PendingBitmap;
                    return true;
                }

                itemLast = transformedMatch;
            }
        }

        matched = itemLast;
        pendingBitmap = matched?.PendingBitmap;
        return matched != null;
    }

    #region Derivative Work

    class DerivativeWork : IPipelineBase<DerivativeWork>
    {
        public enum WorkType
        {
            ResampleImage, // take a BitmapImage and write it as a jpg
            Transcode
        }

        public Guid MediaKey { get; private set; }
        public WorkType Type { get; private set; }
        public int OriginalWidth { get; private set; }
        public int OriginalHeight { get; private set; }
        public double RequestedScaleFactor { get; private set; }
        public bool FullBitmapFidelity { get; private set; }
        public BitmapSource? Image { get; private set; }
        public string TransformationsKey { get; private set; } = string.Empty;


        public DerivativeWork()
        {
        }

        /*----------------------------------------------------------------------------
            %%Function: DerivativeWork
            %%Qualified: Thetacat.Model.Client.Derivatives.DerivativeWork.DerivativeWork

            We allow a specific requested scale factor because we might be resampling
            a resampled image, which can lead to scale factors much smaller than we
            asked for (causing us to save to the derivative cache with a difference
            scale factor, causing us to "miss" when asked for the requested factor,
            leading us to create a new one which (again) resamples the resample, and
            when we go to save it, its a duplicate because the real scale factor is
            different than the requested.

            this happens for things like PSD files where we get a thumbnail for the
            transcoded image, thus its already "resampled" to start with.
        ----------------------------------------------------------------------------*/
        public DerivativeWork(MediaItem mediaItem, BitmapSource resampledImage, bool fullBitmapFidelity, string transformationsKey, double requestedScaleFactor)
        {
            OriginalHeight = mediaItem.ImageHeight ?? resampledImage.PixelHeight;
            OriginalWidth = mediaItem.ImageWidth ?? resampledImage.PixelWidth;
            Image = resampledImage;
            MediaKey = mediaItem.ID;
            FullBitmapFidelity = fullBitmapFidelity;
            Type = fullBitmapFidelity ? WorkType.Transcode : WorkType.ResampleImage;
            RequestedScaleFactor = requestedScaleFactor;
            
            TransformationsKey = transformationsKey;
        }

        public void InitFrom(DerivativeWork t)
        {
            Type = t.Type;
            OriginalHeight = t.OriginalHeight;
            OriginalWidth = t.OriginalWidth;
            MediaKey = t.MediaKey;
            Image = t.Image;
            FullBitmapFidelity = t.FullBitmapFidelity;
            TransformationsKey = t.TransformationsKey;
            RequestedScaleFactor = t.RequestedScaleFactor;
        }
    }

    void DoDerivativeWork(IEnumerable<DerivativeWork> workItems)
    {
        foreach (DerivativeWork item in workItems)
        {
            if ((item.Type != DerivativeWork.WorkType.ResampleImage && item.Type != DerivativeWork.WorkType.Transcode)
                || item.Image == null)
            {
                continue;
            }

            if (App.State.ActiveProfile.DerivativeCache == null)
                throw new CatExceptionInternalFailure("no derivative cache root set");

            PathSegment? cacheRoot = new PathSegment(App.State.ActiveProfile.DerivativeCache);

            // take the bitmapimage and write a jpg for it
            PathSegment destinationDir =
                item.Type == DerivativeWork.WorkType.Transcode
                    ? PathSegment.Join(cacheRoot, "cat-derivatives/transcoded")
                    : PathSegment.Join(cacheRoot, "cat-derivatives/resampled");

            if (!Directory.Exists(destinationDir.Local))
                Directory.CreateDirectory(destinationDir.Local);

            string transformationsSuffix = item.TransformationsKey == string.Empty ? "" : $"-{item.TransformationsKey}";

            PathSegment destination = PathSegment.Join(
                destinationDir,
                $"{item.MediaKey}-{item.Image.PixelWidth}x{item.Image.PixelHeight}{transformationsSuffix}.jpg");

            DerivativeItem derivative = CreateDerivativeItemForWork(item);
            int quality = item.FullBitmapFidelity ? 100 : 90;

            try
            {
                   using FileStream stream = new(destination.Local, FileMode.Create);

                    JpegBitmapEncoder encoder =
                        new()
                        {
                            QualityLevel = quality
                        };
                    encoder.Frames.Add(BitmapFrame.Create(item.Image));
                    encoder.Save(stream);

                    DerivativeItem match = GetMatchingDerivativeItem(derivative);
                    // this is no longer pending save
                    match.Path = destination;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Can't save derivate item {destination}: {ex.Message}");
            }
        }
    }

#endregion
}
