using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Meziantou.Framework.WPF.Collections;
using TCore.Pipeline;
using Thetacat.UI;
using Thetacat.Util;

namespace Thetacat.Model;

/*----------------------------------------------------------------------------
    %%Class: MediaExplorerCollection
    %%Qualified: Thetacat.Model.MediaExplorerCollection

    This holds all of the backing data for an explorer view

    The explorer view is a list of explorer lines. All are stored here

    The collections can only be added or removed from a single thread.
    Other threads can *update* items
----------------------------------------------------------------------------*/
public class MediaExplorerCollection
{
    // this is the master collection of explorer items. background tasks updating images will update
    // these items
    private readonly Dictionary<Guid, MediaExplorerItem> m_explorerItems = new();
    private readonly ProducerConsumer<ImageLoaderWork> m_imageLoaderPipeline;
    private DistributedObservableCollection<MediaExplorerLineModel, MediaExplorerItem> m_collection;
    private readonly ConcurrentQueue<MediaExplorerItem> m_imageLoadQueue = new();

    public ObservableCollection<MediaExplorerLineModel> ExplorerLines => m_collection.TopCollection;
    
    public MediaExplorerCollection()
    {
        m_collection =
            new(
                reference =>
                {
                    return new MediaExplorerLineModel();
                },
                (from, to) =>
                {

                });
                    
        // this will start the thread which will just wait for work to do...
        m_imageLoaderPipeline = new ProducerConsumer<ImageLoaderWork>(null, DoImageLoaderWork);
        m_imageLoaderPipeline.Start();
    }

    public void Close()
    {
        m_imageLoaderPipeline.Stop();
    }

    private object m_lock = new object();
    private double m_explorerWidth;
    private int m_panelWidth = 212;

//    private int PictureWidth => m_panelWidth - m_picturePadding;

    private static int CalculatePanelsPerLine(int panelWidth, double explorerWidth)
    {
        return (int)(explorerWidth / panelWidth);
    }

    private int PanelsPerLine => CalculatePanelsPerLine(m_panelWidth, m_explorerWidth);

    private const int s_picturePreviewWidth = 512;

    public void AdjustExplorerWidth(double width)
    {
        int panelsPerLineOriginal = PanelsPerLine;

        m_explorerWidth = width;
        m_collection.UpdateItemsPerLine(PanelsPerLine);

        if (PanelsPerLine != panelsPerLineOriginal)
        {
            // need to recalculate the collection
            AdjustCollectionForPanelChange(panelsPerLineOriginal);
        }

    }

    void AdjustCollectionForPanelChange(int panelsPerLineOriginal)
    {
//        if (m_explorerLines.Count == 0)
//            return;
//
//        int deltaPerLine = PanelsPerLine - panelsPerLineOriginal;
//        int currentLineDelta = deltaPerLine;
//
//        if (deltaPerLine < 0)
//        {
//            // we have to shrink each line, which means pushing items to subsequent
//            // lines
//            
////            int itemCount = panelsPer
//
//
//        }
//        // we just need to shuffle things around in the lines
//        for (int i = 0; i < m_explorerLines.Count; i++)
//        {
//            if (currentLineDelta < 0)
//            {
//                // need to move an item from this line to the next line
//            }
//            MediaExplorerLineModel line = m_explorerLines[i];
//
//        }
    }



    public void AddToExplorerCollection(MediaItem item)
    {
        string? path = MainWindow._AppState.Cache.TryGetCachedFullPath(item.ID);

        MediaExplorerItem explorerItem = new MediaExplorerItem(path ?? string.Empty, item.VirtualPath);

        m_explorerItems.Add(item.ID, explorerItem);
        m_imageLoadQueue.Enqueue(explorerItem);

        m_collection.AddToDistributedCollection(explorerItem);
        m_imageLoaderPipeline.Producer.QueueRecord(new ImageLoaderWork(item, explorerItem));

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

        public ImageLoaderWork(MediaItem mediaItem, MediaExplorerItem explorerItem)
        {
            MediaKey = mediaItem.ID;
            PathToImage = explorerItem.TileSrc;
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
            if (item.PathToImage == null)
                continue;

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.DecodePixelWidth = Math.Min(s_picturePreviewWidth, (int)Math.Floor(item.AspectRatio * s_picturePreviewWidth));
            image.UriSource = new Uri(item.PathToImage);
            image.EndInit();
            image.Freeze();
            if (m_explorerItems.TryGetValue(item.MediaKey, out MediaExplorerItem? explorerItem))
//                Application.Current.Dispatcher.Invoke(
//                    new Action(() =>
//                    {
                        explorerItem.TileImage = image;
//                    }));
        }
    }

#endregion
}
