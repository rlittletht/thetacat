using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Meziantou.Framework.WPF.Collections;
using TCore.Pipeline;
using Thetacat.Logging;
using Thetacat.Model.ImageCaching;
using Thetacat.Types;
using Thetacat.UI;
using Thetacat.UI.Explorer;
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
    private readonly DistributedObservableCollection<MediaExplorerLineModel, MediaExplorerItem> m_collection;

    public ObservableCollection<MediaExplorerLineModel> ExplorerLines => m_collection.TopCollection;
    private readonly ImageCache m_imageCache;
    private readonly Dictionary<Guid, LineItemOffset> m_mapLineItemOffsets = new();

    public MediaExplorerCollection(
        double leadingLabelWidth, double explorerHeight = 0.0, double explorerWidth = 0.0, double itemHeight = 0.0, double itemWidth = 0.0)
    {
        m_leadingLabelWidth = leadingLabelWidth;
        m_explorerWidth = explorerWidth;
        m_explorerHeight = itemHeight;
        m_panelItemWidth = itemWidth;
        m_panelItemHeight = itemHeight;

        m_collection =
            new(
                reference => { return new MediaExplorerLineModel(); },
                (from, to) =>
                {
                    string fromString = from.LineLabel;
                    from.LineLabel = "";
                    to.LineLabel = fromString;
                });
        m_imageCache = new ImageCache();
        m_imageCache.ImageCacheUpdated += OnImageCacheUpdated;
    }

    private object m_lock = new object();
    private double m_leadingLabelWidth;
    private double m_explorerWidth;
    private double m_explorerHeight;
    private double m_panelItemHeight;
    private double m_panelItemWidth;

    private int RowsPerExplorer => (int)Math.Round(m_explorerWidth / m_panelItemHeight);

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

    private static int CalculatePanelsPerLine(double leadingWidth, double panelWidth, double explorerWidth)
    {
        return (int)((explorerWidth - leadingWidth) / panelWidth);
    }

    private int PanelsPerLine => CalculatePanelsPerLine(m_leadingLabelWidth, m_panelItemWidth, m_explorerWidth);

    public void AdjustExplorerHeight(double height)
    {
        m_explorerHeight = height;
    }

    public void AdjustPanelItemWidth(double width)
    {
        m_panelItemWidth = width;
    }

    public void AdjustPanelItemHeight(double height)
    {
        m_panelItemHeight = height;
    }

    public void AdjustExplorerWidth(double width)
    {
        m_explorerWidth = width;
    }

    public void UpdateItemsPerLine()
    {
        m_collection.UpdateItemsPerLine(PanelsPerLine);
        RebuildLineOffsetsMap();
    }

    public void EnsureImagesForSurroundingRows(int row)
    {
        if (m_collection.TopCollection.Count == 0)
            return;

        MicroTimer timer = new MicroTimer();
        timer.Start();
        int minRow = Math.Max(row - RowsPerExplorer, 0);
        int maxRow = Math.Min(row + (2 * RowsPerExplorer), m_collection.TopCollection.Count - 1);
        ICatalog catalog = MainWindow._AppState.Catalog;
        ICache cache = MainWindow._AppState.Cache;

        MainWindow.LogForApp(EventType.Information, $"starting ensure images: {minRow}-{maxRow}");

        List<MediaExplorerLineModel> linesToCache = new List<MediaExplorerLineModel>();
        while (minRow <= maxRow)
        {
            linesToCache.Add(m_collection.TopCollection[minRow]);
            minRow++;
        }

        MainWindow.LogForApp(EventType.Information, $"done making line list: {timer.Elapsed()}");
        ThreadPool.QueueUserWorkItem(
            stateInfo =>
            {
                foreach (MediaExplorerLineModel line in linesToCache)
                {
                    foreach (MediaExplorerItem item in line.Items)
                    {
                        if (catalog.Media.Items.TryGetValue(item.MediaId, out MediaItem? mediaItem))
                        {
                            string? path = cache.TryGetCachedFullPath(item.MediaId);

                            if (path != null)
                                m_imageCache.TryAddItem(mediaItem, path);
                        }
                    }
                }
            });

        MainWindow.LogForApp(EventType.Information, $"done launching parallel: {timer.Elapsed()}");
    }

    public void Clear()
    {
        m_collection.Clear();
        m_explorerItems.Clear();
        m_mapLineItemOffsets.Clear();
    }
    
    public void AddToExplorerCollection(MediaItem item, bool startNewSegment, string segmentTitle)
    {
        string? path = MainWindow._AppState.Cache.TryGetCachedFullPath(item.ID);

        MediaExplorerItem explorerItem = new MediaExplorerItem(path ?? string.Empty, item.VirtualPath, item.ID);

        m_explorerItems.Add(item.ID, explorerItem);
        
        if (path != null)
        {
            ImageCacheItem? cacheItem = m_imageCache.GetAnyExistingItem(item.ID);
            explorerItem.TileImage = cacheItem?.Image;
        }

        if (startNewSegment)
        {
            m_collection.AddSegment(null);
        }

        m_collection.AddItem(explorerItem);
        if (startNewSegment)
        {
            m_collection.GetCurrentLine().LineLabel = segmentTitle;
        }

        m_mapLineItemOffsets.Add(
            explorerItem.MediaId,
            new LineItemOffset(
                m_collection.TopCollection.Count - 1,
                m_collection.TopCollection[m_collection.TopCollection.Count - 1].Items.Count - 1));
    }

    /*----------------------------------------------------------------------------
        %%Function: RebuildLineOffsetsMap
        %%Qualified: Thetacat.Model.MediaExplorerCollection.RebuildLineOffsetsMap

        Build a map of media IDs to their Line/Offset in the explorer view.
        This will help with selection
    ----------------------------------------------------------------------------*/
    public void RebuildLineOffsetsMap()
    {
        m_mapLineItemOffsets.Clear();

        int line = 0;
        int offset = 0;

        foreach (MediaExplorerLineModel explorerLine in m_collection.TopCollection)
        {
            foreach (MediaExplorerItem item in explorerLine.Items)
            {
                m_mapLineItemOffsets.Add(item.MediaId, new LineItemOffset(line, offset));
                offset++;
            }

            line++;
            offset = 0;
        }
    }

    public List<MediaExplorerItem> GetMediaItemsBetween(LineItemOffset first, LineItemOffset last)
    {
        if (m_collection.TopCollection.Count < first.Line)
            throw new ArgumentException($"first Line {first.Line} > {m_collection.TopCollection.Count}");

        List<MediaExplorerItem> result = new();

        // make clones (with proper ordering) to avoid trashing the callers data
        if (first.Line > last.Line
            || (first.Line == last.Line && first.Offset > last.Offset))
        {
            LineItemOffset temp = new LineItemOffset(first);
            first = new LineItemOffset(last);
            last = temp;
        }
        else
        {
            first = new LineItemOffset(first);
            last = new LineItemOffset(last);
        }

        while (first.Line <= last.Line)
        {
            MediaExplorerLineModel explorerLine = m_collection.TopCollection[first.Line];

            int lastOffset = (first.Line == last.Line) ? last.Offset : explorerLine.Items.Count - 1;

            while (first.Offset <= lastOffset)
            {
                MediaExplorerItem item = explorerLine.Items[first.Offset];

                result.Add(item);
                first.Offset++;
            }

            first.Offset = 0;
            first.Line++;
        }

        return result;
    }

    public LineItemOffset? GetLineItemOffsetForMediaItem(MediaExplorerItem item)
    {
        if (m_mapLineItemOffsets.TryGetValue(item.MediaId, out LineItemOffset? lineItemOffset))
            return lineItemOffset;

        return null;
    }

    public void DebugVerifySelectedItems(HashSet<MediaExplorerItem> selected)
    {
#if DEBUG
        foreach (MediaExplorerLineModel explorerLine in m_collection.TopCollection)
        {
            foreach (MediaExplorerItem item in explorerLine.Items)
            {
                if (item.Selected)
                {
                    if (!selected.Contains(item))
                        throw new CatExceptionDebugFailure($"item {item.MediaId} selected but not in selection list");
                }
            }
        }

        foreach (MediaExplorerItem item in selected)
        {
            if (!item.Selected)
                throw new CatExceptionDebugFailure($"item {item.MediaId} is in selection list, but not selected");
        }
#endif
    }
}