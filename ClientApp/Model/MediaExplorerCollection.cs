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
using Thetacat.Model.ImageCaching;
using Thetacat.Types;
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
    private DistributedObservableCollection<MediaExplorerLineModel, MediaExplorerItem> m_collection;
    private readonly ConcurrentQueue<MediaExplorerItem> m_imageLoadQueue = new();

    public ObservableCollection<MediaExplorerLineModel> ExplorerLines => m_collection.TopCollection;
    private ImageCache m_imageCache;

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
        m_imageCache = new ImageCache();
        m_imageCache.ImageCacheUpdated += OnImageCacheUpdated;
    }

    private object m_lock = new object();
    private double m_explorerWidth;
    private int m_panelWidth = 212;

    public void Close()
    {
        m_imageCache.Close();
    }

    private void OnImageCacheUpdated(object? sender, ImageCacheUpdateEventArgs e)
    {
        ImageCache? cache = sender as ImageCache;

        if (cache == null)
            throw new CatExceptionInternalFailure("sender wasn't an image cache in OnImageCacheUpdated");

        if (cache.Items.TryGetValue(e.MediaId, out ImageCacheItem? cacheItem))
        {
            if (m_explorerItems.TryGetValue(e.MediaId, out MediaExplorerItem? explorerItem))
            {
                explorerItem.TileImage = cacheItem.Image;
            }
        }
    }

    private static int CalculatePanelsPerLine(int panelWidth, double explorerWidth)
    {
        return (int)(explorerWidth / panelWidth);
    }

    private int PanelsPerLine => CalculatePanelsPerLine(m_panelWidth, m_explorerWidth);

    public void AdjustExplorerWidth(double width)
    {
        m_explorerWidth = width;
        m_collection.UpdateItemsPerLine(PanelsPerLine);
    }

    public void AddToExplorerCollection(MediaItem item)
    {
        string? path = MainWindow._AppState.Cache.TryGetCachedFullPath(item.ID);

        MediaExplorerItem explorerItem = new MediaExplorerItem(path ?? string.Empty, item.VirtualPath);

        m_explorerItems.Add(item.ID, explorerItem);
        m_imageLoadQueue.Enqueue(explorerItem);

        if (path != null)
        {
            ImageCacheItem cacheItem = m_imageCache.TryAddItem(item, path);
            explorerItem.TileImage = cacheItem.Image;
        }

        m_collection.AddItem(explorerItem);
    }
}
