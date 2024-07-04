using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using Thetacat.Explorer;
using Thetacat.Explorer.UI;
using Thetacat.Filtering;
using Thetacat.Logging;
using Thetacat.Model.ImageCaching;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;
using MessageBox = System.Windows.MessageBox;

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

    public bool ExpandMediaStacks
    {
        get => m_expandMediaStacks;
        set => SetField(ref m_expandMediaStacks, value);
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

    public MediaStack StackForOrdering
    {
        get => m_stackForOrdering ?? throw new Exception("No stack for ordering");
        set => m_stackForOrdering = value;
    }

    public bool IsMediaDateTimeline => TimelineType.Equals(TimelineType.MediaDate);
    public bool IsImportDateTimeline => TimelineType.Equals(TimelineType.ImportDate);
    public bool IsTimelineAscending => TimelineOrder.Equals(TimelineOrder.DateAscending);
    public bool IsTimelineDescending => TimelineOrder.Equals(TimelineOrder.DateDescending);

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
    private bool m_expandMediaStacks;

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

    private MediaStack? m_stackForOrdering;
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
            MainWindow.LogForAsync(EventType.Critical, $"Done caching {cacheItem.MediaId}");

            cacheItem.IsLoadQueued = false;
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

        List<MediaItem> itemsToQueue = new();
        foreach (MediaExplorerLineModel line in linesToCache)
        {
            foreach (MediaExplorerItem item in line.Items)
            {
                if (catalog.TryGetMedia(item.MediaId, out MediaItem? mediaItem))
                    itemsToQueue.Add(mediaItem);
            }
        }

        MainWindow.LogForApp(EventType.Information, $"done making line list: {timer.Elapsed()}");
        QueueImageCacheLoadForMediaItems(itemsToQueue);

        MainWindow.LogForApp(EventType.Information, $"done launching parallel: {timer.Elapsed()}");
    }

    /*----------------------------------------------------------------------------
        %%Function: QueueImageCacheLoadForMediaItems
        %%Qualified: Thetacat.Model.MediaExplorerCollection.QueueImageCacheLoadForMediaItems

        This queues an image load into the cache for the media item. This
    ----------------------------------------------------------------------------*/
    public static void QueueImageCacheLoadForMediaItems(IReadOnlyCollection<MediaItem> mediaItems)
    {
        ICache cache = App.State.Cache;

        // these are pushed onto the pool with no guarantee about what order they
        // will get run with respect to other pushes to the pool.

        // we know that the work willbe done as a unit, but it might get done at the same time 
        // as other work, which means the QueueBackgroundLoadToCache might happen at the same time
        // as other BackgroundLoadToCache items

        // the image loader work will check for existing derivative items and create them if they 
        // don't exist.. BUT the create is done on a worker thread and might execute AFTER
        // another ImageLoaderWork checks to see if they are loading.

        // we need to populate the derivative item IMMEDIATELY with the image that can be used,
        // and the queued task can be the (slower) save to disk. If the save fails, then we can live
        // with the derivative item living in memory and it will just go away when we exit (we will
        // have no durable derivative there).

        ThreadPool.QueueUserWorkItem(
            stateInfo =>
            {
                foreach (MediaItem mediaItem in mediaItems)
                {
                    string? path = cache.TryGetCachedFullPath(mediaItem.ID);

                    if (path != null)
                    {
                        App.State.PreviewImageCache.TryQueueBackgroundLoadToCache(mediaItem, path);
                    }
                }
            });
    }

    public void Clear()
    {
        m_collection.Clear();
        m_explorerItems.Clear();
        m_mapLineItemOffsets.Clear();
    }


    public MediaExplorerItem? GetNextItem(MediaItem item)
    {
        // find this item in the collection and get the next item
        if (m_mapLineItemOffsets.TryGetValue(item.ID, out LineItemOffset? location))
        {
            // now get the next item
            return m_collection.GetNextItem(location.Line, location.Offset);
        }

        return null;
    }

    public MediaExplorerItem? GetPreviousItem(MediaItem item)
    {
        // find this item in the collection and get the next item
        if (m_mapLineItemOffsets.TryGetValue(item.ID, out LineItemOffset? location))
        {
            // now get the previous item
            return m_collection.GetPreviousItem(location.Line, location.Offset);
        }

        return null;
    }


    public void AddToExplorerCollection(MediaItem item, bool startNewSegment, string segmentTitle)
    {
        string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

        MediaExplorerItem explorerItem = new MediaExplorerItem(path ?? string.Empty, item.VirtualPath, item.ID);

        explorerItem.IsTrashItem = item.IsTrashItem;
        explorerItem.IsOffline = item.DontPushToCloud;
        if (item.MediaStack != null || item.VersionStack != null)
            explorerItem.SetStackInformation(item);

        item.PropertyChanged += ItemOnPropertyChanged;
        m_explorerItems.Add(item.ID, explorerItem);

        if (path != null)
        {
            ImageCacheItem? cacheItem = App.State.PreviewImageCache.GetAnyExistingItem(item.ID);
            explorerItem.TileImage = cacheItem?.Image;
        }
        else
        {
            explorerItem.TileImage = ImageCache.CreatePlaceholderImage($"uncached: '{item.VirtualPath}'", 11.0);
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

    private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MediaItem item)
        {
            if (e.PropertyName == "Tags")
            {
                if (m_mapLineItemOffsets.TryGetValue(item.ID, out LineItemOffset? location))
                {
                    MediaExplorerItem explorerItem = m_collection.GetItem(location.Line, location.Offset);

                    explorerItem.IsTrashItem = item.IsTrashItem;
                    explorerItem.IsOffline = item.DontPushToCloud;
                }
            }
            else if (e.PropertyName == "VersionStack" || e.PropertyName == "MediaStack")
            {
                if (m_mapLineItemOffsets.TryGetValue(item.ID, out LineItemOffset? location))
                {
                    MediaExplorerItem explorerItem = m_collection.GetItem(location.Line, location.Offset);

                    explorerItem.UpdateStackInformation();
                }
            }
        }
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

    class ItemShort
    {
        public DateTime Date;
        public Guid ID;
        public string Leaf;

        public ItemShort(DateTime date, MediaItem item)
        {
            Date = date;
            ID = item.ID;
            Leaf = item.VirtualPath.GetLeafItem() ?? "";
        }
    }

    int ItemShortComparer(ItemShort left, ItemShort right)
    {
        int n = left.Date.CompareTo(right.Date);
        if (n != 0)
            return n;

        n = string.CompareOrdinal(left.Leaf, right.Leaf);
        if (n != 0)
            return n;

        return 1;
    }

    public void ToggleExpandMediaStacks()
    {
        m_expandMediaStacks = !m_expandMediaStacks;
        App.State.ActiveProfile.ExpandMediaStacksInExplorers = m_expandMediaStacks;
        App.State.Settings.WriteSettings();

        BuildTimelineFromMediaCatalog();
    }

    public void BuildTimelineFromMediaCatalog()
    {
        IEnumerable<MediaItem> collection =
            m_filterDefinition == null ? App.State.Catalog.GetMediaCollection() : App.State.Catalog.GetFilteredMediaItems(m_filterDefinition);

        BuildTimelineForMediaCollection(collection);
    }

    public void BuildTimelineForMediaCollection(IEnumerable<MediaItem> collection)
    {
        MicroTimer timer = new MicroTimer();
        MainWindow.LogForApp(EventType.Information, "Beginning building timeline collection");

        Clear();

        if (TimelineOrder.Equals(TimelineOrder.DateDescending) || TimelineOrder.Equals(TimelineOrder.DateAscending))
        {
            // build a group by date
            Dictionary<DateTime, ICollection<ItemShort>> dateGrouping = new();

            foreach (MediaItem item in collection)
            {
                if (m_expandMediaStacks == false)
                {
                    bool? isMediaTop = null;
                    bool? isVersionTop = null;

                    // see if we should filter this out
                    if (item.MediaStack != null)
                    {
                        if (App.State.Catalog.MediaStacks.Items.TryGetValue(item.MediaStack.Value, out MediaStack? stack))
                            isMediaTop = stack.IsItemTopOfStack(item.ID);
                    }

                    if (item.VersionStack != null)
                    {
                        if (App.State.Catalog.VersionStacks.Items.TryGetValue(item.VersionStack.Value, out MediaStack? stack))
                            isVersionTop = stack.IsItemTopOfStack(item.ID);
                    }

                    if (isMediaTop != null || isVersionTop != null)
                    {
                        if (isMediaTop is false && isVersionTop is not true)
                            continue;

                        if (isVersionTop is false && isMediaTop is not true)
                            continue;
                    }
                }

                DateTime dateTime = GetTimelineDateFromMediaItem(item);
                DateTime date = dateTime.Date;

                if (!dateGrouping.TryGetValue(date, out ICollection<ItemShort>? items))
                {
                    items = new List<ItemShort>();
                    dateGrouping.Add(date, items);
                }

                items.Add(new ItemShort(dateTime, item));
            }

            IComparer<DateTime> comparer;
            IComparer<ItemShort> comparerKvp;

            if (TimelineOrder.Equals(TimelineOrder.DateDescending))
            {
                comparer = Comparer<DateTime>.Create((x, y) => y.CompareTo(x) < 0 ? y.CompareTo(x) : y.CompareTo(x) + 1);
                comparerKvp = Comparer<ItemShort>.Create((y, x) => ItemShortComparer(x, y));
            }
            else
            {
                comparer = Comparer<DateTime>.Create((y, x) => y.CompareTo(x) < 0 ? y.CompareTo(x) : y.CompareTo(x) + 1);
                comparerKvp = Comparer<ItemShort>.Create((x, y) => ItemShortComparer(x, y));
            }

            ImmutableSortedSet<DateTime> sortedDates = dateGrouping.Keys.ToImmutableSortedSet(comparer);

            foreach (DateTime date in sortedDates)
            {
                bool newSegment = true;

                ICollection<ItemShort> items = dateGrouping[date].ToImmutableSortedSet(comparerKvp);

                foreach (ItemShort pair in items)
                {
                    MediaItem item = App.State.Catalog.GetMediaFromId(pair.ID);
                    AddToExplorerCollection(item, newSegment, date.ToString("MMM dd, yyyy"));
                    newSegment = false;
                }
            }
        }
        else if (TimelineOrder == TimelineOrder.StackOrder)
        {
            IComparer<MediaItem> mediaComparer = Comparer<MediaItem>.Create
            (
                (x, y) =>
                {
                    int iLeft = StackForOrdering.FindMediaInStack(x.ID)?.StackIndex ?? 0;
                    int iRight = StackForOrdering.FindMediaInStack(y.ID)?.StackIndex ?? 0;

                    int d = iLeft - iRight;
                    return d < 0 ? d : d + 1;
                });

            ImmutableSortedSet<MediaItem> sortedItems = collection.ToImmutableSortedSet(mediaComparer);

            // stack order just sorts by the stack index. no grouping
            bool firstSegment = true;

            foreach (MediaItem item in sortedItems)
            {
                MediaStackItem? stackItem = StackForOrdering.FindMediaInStack(item.ID);
                AddToExplorerCollection(item, firstSegment, $"stack {StackForOrdering.Description}");
                firstSegment = false;
            }
        }

        MainWindow.LogForApp(EventType.Information, $"Done building. {timer.Elapsed()}");
    }

    public void ResetTimeline()
    {
        TimelineType = TimelineType.None;
        TimelineOrder = TimelineOrder.DateAscending;
        Clear();
    }

    public void SetTimelineTypeAndOrder(TimelineType type, TimelineOrder order)
    {
        if (type.Equals(TimelineType) && order.Equals(TimelineOrder))
            return;

        if (!type.Equals(TimelineType))
        {
            App.State.ActiveProfile.TimelineType = type;
            App.State.Settings.WriteSettings();

            TimelineType = type;
        }

        if (!order.Equals(TimelineOrder))
        {
            App.State.ActiveProfile.TimelineOrder = order;
            App.State.Settings.WriteSettings();

            TimelineOrder = order;
        }

        BuildTimelineFromMediaCatalog();
    }

    public void SetTimelineType(TimelineType type)
    {
        if (type.Equals(TimelineType))
            return;

        App.State.ActiveProfile.TimelineType = type;
        App.State.Settings.WriteSettings();

        TimelineType = type;
        BuildTimelineFromMediaCatalog();
    }

    public void SetTimelineOrder(TimelineOrder order)
    {
        if (order.Equals(TimelineOrder))
            return;

        App.State.ActiveProfile.TimelineOrder = order;
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

    public bool FDoDeleteItems(IReadOnlyCollection<MediaItem> mediaItems)
    {
        if (mediaItems.Count == 0)
            return false;

        if (MessageBox.Show($"Are you sure you want to delete {mediaItems.Count} items? This cannot be undone.", "Confirm delete", MessageBoxButton.YesNo)
            != MessageBoxResult.Yes)
        {
            return false;
        }

        foreach (MediaItem item in mediaItems)
        {
            try
            {
                App.State.Catalog.DeleteItem(App.State.ActiveProfile.CatalogID, item.ID);
                ServiceInterop.DeleteImportsForMediaItem(App.State.ActiveProfile.CatalogID, item.ID);
                App.State.EnsureDeletedItemCollateralRemoved(item.ID);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete item: {item.ID}: {item.VirtualPath}: {ex}");
            }
        }

        return true;
    }
}
