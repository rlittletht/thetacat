using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using Thetacat.Explorer;
using Thetacat.Explorer.UI;
using Thetacat.Filtering;
using Thetacat.Logging;
using Thetacat.Model.ImageCaching;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model;

/*----------------------------------------------------------------------------
    %%Class: MediaExplorerCollection
    %%Qualified: Thetacat.Model.MediaExplorerCollection

    This holds all of the backing data for an explorer view and the main
    window

    The explorer view is a list of explorer lines. All are stored here

    The collections can only be added or removed from a single thread.
    Other threads can *update* items
----------------------------------------------------------------------------*/
public class MediaExplorerCollection : INotifyPropertyChanged
{
    public bool IsDirty
    {
        get => m_isDirty;
        set => SetField(ref m_isDirty, value);
    }

    public string WindowDateRange
    {
        get => m_windowDateRange;
        set => SetField(ref m_windowDateRange, value);
    }

    public string JumpDate
    {
        get => m_jumpDate;
        set => SetField(ref m_jumpDate, value);
    }

    public TimelineOrder TimelineOrder
    {
        get => m_timelineOrder;
        set
        {
            if (SetField(ref m_timelineOrder, value))
            {
                OnPropertyChanged(nameof(IsTimelineAscending));
                OnPropertyChanged(nameof(IsTimelineDescending));
            }
        }
    }

    public TimelineType TimelineType
    {
        get => m_timelineType;
        set
        {
            if (SetField(ref m_timelineType, value))
            {
                OnPropertyChanged(nameof(IsMediaDateTimeline));
                OnPropertyChanged(nameof(IsImportDateTimeline));
            }
        }
    }

    public bool IsMediaDateTimeline => TimelineType.Equals(TimelineType.MediaDate);
    public bool IsImportDateTimeline => TimelineType.Equals(TimelineType.ImportDate);
    public bool IsTimelineAscending => TimelineOrder.Equals(TimelineOrder.Ascending);
    public bool IsTimelineDescending => TimelineOrder.Equals(TimelineOrder.Descending);

    // this is the master collection of explorer items. background tasks updating images will update
    // these items
    private readonly Dictionary<Guid, MediaExplorerItem> m_explorerItems = new();
    private readonly DistributedObservableCollection<MediaExplorerLineModel, MediaExplorerItem> m_collection;
    private FilterDefinition? m_filterDefinition;

    public bool DontRebuildTimelineOnFilterChange { get; set; } = false;
    public FilterDefinition? Filter
    {
        get => m_filterDefinition;
        set
        {
            SetField(ref m_filterDefinition, value);
            if (!DontRebuildTimelineOnFilterChange)
                BuildTimelineFromMediaCatalog();
        }
    }

    public ObservableCollection<MediaExplorerLineModel> ExplorerLines => m_collection.TopCollection;
    private readonly Dictionary<Guid, LineItemOffset> m_mapLineItemOffsets = new();

    public double PanelHeight => m_panelItemHeight;

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
        App.State.PreviewImageCache.ImageCacheUpdated += OnImageCacheUpdated;
    }

    private object m_lock = new object();
    private double m_leadingLabelWidth;
    private double m_explorerWidth;
    private double m_explorerHeight;
    private double m_panelItemHeight;
    private double m_panelItemWidth;
    private string m_windowDateRange = String.Empty;
    private string m_jumpDate = String.Empty;
    private TimelineType m_timelineType = TimelineType.None;
    private TimelineOrder m_timelineOrder = TimelineOrder.None;
    private bool m_isDirty = false;

    private int ColumnsPerExplorer => (int)Math.Round(m_explorerWidth / m_panelItemWidth);
    private int RowsPerExplorer => (int)Math.Round(m_explorerHeight / m_panelItemHeight);

    public void Close()
    {
        // PreviewImageCache.Close is now handled in MainWindow
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

    public int GetLineToScrollTo(string dateString)
    {
        if (!DateTime.TryParse(dateString, out DateTime date))
        {
            MessageBox.Show($"Can't parse {dateString} as a date");
            return -1;
        }

        // scan through our items to find the first greater or equal to the date
        return m_collection.GetNearestLineGreaterOrEqualMatching(
                0,
                (line) => GetTimelineDateFromItem(line.Items[0]) >= date)
           .lineNumber;
    }

    public void NotifyTopVisibleItem(int row)
    {
        if (m_collection.IsEmpty)
            return;

        // we have a new top item. calculate the segments visible

        MediaExplorerLineModel lineTop = m_collection.GetNearestLineLessOrEqualMatching(row, (line) => !string.IsNullOrWhiteSpace(line.LineLabel)).line;
        MediaExplorerLineModel lineBottom = m_collection.GetNearestLineLessOrEqualMatching(
                row + RowsPerExplorer,
                (line) => !string.IsNullOrWhiteSpace(line.LineLabel))
           .line;

        string first = lineTop.LineLabel;
        string last = lineBottom.LineLabel;

        WindowDateRange = first == last ? first : $"{first} - {last}";
    }

    public void EnsureImagesForSurroundingRows(int row)
    {
        if (m_collection.TopCollection.Count == 0)
            return;

        MicroTimer timer = new MicroTimer();
        timer.Start();
        int minRow = Math.Max(row - RowsPerExplorer, 0);
        int maxRow = Math.Min(row + (2 * RowsPerExplorer), m_collection.TopCollection.Count - 1);
        ICatalog catalog = App.State.Catalog;
        ICache cache = App.State.Cache;

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
                        if (catalog.TryGetMedia(item.MediaId, out MediaItem? mediaItem))
                        {
                            string? path = cache.TryGetCachedFullPath(item.MediaId);

                            if (path != null)
                            {
//                                MainWindow.LogForApp(EventType.Warning, $"trying to queue {path} for load");
                                App.State.PreviewImageCache.TryAddItem(mediaItem, path);
                            }
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
        string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

        MediaExplorerItem explorerItem = new MediaExplorerItem(path ?? string.Empty, item.VirtualPath, item.ID);

        m_explorerItems.Add(item.ID, explorerItem);

        if (path != null)
        {
            ImageCacheItem? cacheItem = App.State.PreviewImageCache.GetAnyExistingItem(item.ID);
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

    public void BuildTimelineFromMediaCatalog()
    {
        MicroTimer timer = new MicroTimer();
        MainWindow.LogForApp(EventType.Information, "Beginning building timeline collection");

        // build a group by date
        Dictionary<DateTime, ICollection<KeyValuePair<DateTime, Guid>>> dateGrouping = new();

        IEnumerable<MediaItem> collection =
            m_filterDefinition == null ? App.State.Catalog.GetMediaCollection() : App.State.Catalog.GetFilteredMediaItems(m_filterDefinition);

        foreach (MediaItem item in collection)
        {
            DateTime dateTime = GetTimelineDateFromMediaItem(item);
            DateTime date = dateTime.Date;

            if (!dateGrouping.TryGetValue(date, out ICollection<KeyValuePair<DateTime, Guid>>? items))
            {
                items = new List<KeyValuePair<DateTime, Guid>>();
                dateGrouping.Add(date, items);
            }

            ((List<KeyValuePair<DateTime, Guid>>)items).Add(KeyValuePair.Create(dateTime, item.ID));
        }

        IComparer<DateTime> comparer =
            TimelineOrder.Equals(TimelineOrder.Descending)
                ? Comparer<DateTime>.Create((x, y) => y.CompareTo(x) < 0 ? y.CompareTo(x) : y.CompareTo(x) + 1)
                : Comparer<DateTime>.Create((y, x) => y.CompareTo(x) < 0 ? y.CompareTo(x) : y.CompareTo(x) + 1);

        IComparer<KeyValuePair<DateTime, Guid>> comparerKvp =
            TimelineOrder.Equals(TimelineOrder.Descending)
                ? Comparer<KeyValuePair<DateTime, Guid>>.Create((x, y) => y.Key.CompareTo(x.Key) < 0 ? y.Key.CompareTo(x.Key) : y.Key.CompareTo(x.Key) + 1)
                : Comparer<KeyValuePair<DateTime, Guid>>.Create((y, x) => y.Key.CompareTo(x.Key) < 0 ? y.Key.CompareTo(x.Key) : y.Key.CompareTo(x.Key) + 1);

        ImmutableSortedSet<DateTime> sortedDates = dateGrouping.Keys.ToImmutableSortedSet(comparer);

        Clear();

        foreach (DateTime date in sortedDates)
        {
            bool newSegment = true;

            ICollection<KeyValuePair<DateTime, Guid>> items = dateGrouping[date].ToImmutableSortedSet(comparerKvp);

            foreach (KeyValuePair<DateTime, Guid> pair in items)
            {
                MediaItem item = App.State.Catalog.GetMediaFromId(pair.Value);
                AddToExplorerCollection(item, newSegment, date.ToString("MMM dd, yyyy"));
                newSegment = false;
            }
        }

        MainWindow.LogForApp(EventType.Information, $"Done building. {timer.Elapsed()}");
    }

    public void ResetTimeline()
    {
        TimelineType = TimelineType.None;
        TimelineOrder = TimelineOrder.Ascending;
        Clear();
    }

    public void SetTimelineTypeAndOrder(TimelineType type, TimelineOrder order)
    {
        if (type.Equals(TimelineType) && order.Equals(TimelineOrder))
            return;

        if (!type.Equals(TimelineType))
        {
            App.State.Settings.TimelineType = type;
            App.State.Settings.WriteSettings();

            TimelineType = type;
        }

        if (!order.Equals(TimelineOrder))
        {
            App.State.Settings.TimelineOrder = order;
            App.State.Settings.WriteSettings();

            TimelineOrder = order;
        }

        BuildTimelineFromMediaCatalog();
    }

    public void SetTimelineType(TimelineType type)
    {
        if (type.Equals(TimelineType))
            return;

        App.State.Settings.TimelineType = type;
        App.State.Settings.WriteSettings();

        TimelineType = type;
        BuildTimelineFromMediaCatalog();
    }

    public void SetTimelineOrder(TimelineOrder order)
    {
        if (order.Equals(TimelineOrder))
            return;

        App.State.Settings.TimelineOrder = order;
        App.State.Settings.WriteSettings();

        TimelineOrder = order;
        BuildTimelineFromMediaCatalog();
    }

    public void SetFilter(FilterDefinition filter)
    {
        Filter = filter;
        BuildTimelineFromMediaCatalog();
    }

    public DateTime GetTimelineDateFromItem(MediaExplorerItem item)
    {
        MediaItem mediaItem = App.State.Catalog.GetMediaFromId(item.MediaId);

        return GetTimelineDateFromMediaItem(mediaItem);
    }

    public static DateTime GetLocalDateFromMedia(MediaItem item, DateTime? mediaDate)
    {
        if (mediaDate != null)
            return mediaDate.Value.ToLocalTime();

        string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

        if (path != null)
            return File.GetCreationTime(path);

        return DateTime.Now;
    }

    public DateTime GetTimelineDateFromMediaItem(MediaItem mediaItem)
    {
        if (TimelineType.Equals(TimelineType.ImportDate))
            return GetLocalDateFromMedia(mediaItem, mediaItem.ImportDate);

        return GetLocalDateFromMedia(mediaItem, mediaItem.OriginalMediaDate);
    }

    public void SetDirtyState(bool dirty)
    {
        IsDirty = dirty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
