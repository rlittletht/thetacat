using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Converters;
using Thetacat.Explorer.UI;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.ImageCaching;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Explorer;

/// <summary>
/// Interaction logic for MediaItemZoom.xaml
/// </summary>
public partial class MediaItemZoom : Window
{
    public delegate void OnZoomClosingDelegate(MediaItemZoom zoom);
    public delegate MediaItem? GetNextMediaItem(MediaItem item);
    public delegate MediaItem? GetPreviousMediaItem(MediaItem item);

    private readonly SortableListViewSupport m_sortableListViewSupport;
    private void SortType(object sender, RoutedEventArgs e) => m_sortableListViewSupport.Sort(sender as GridViewColumnHeader);
    private GetNextMediaItem? m_nextDelegate;
    private GetPreviousMediaItem? m_previousDelegate;

    private MediaItemZoomModel m_model = new();

    /*----------------------------------------------------------------------------
        %%Function: OnImageCacheUpdated
        %%Qualified: Thetacat.Explorer.MediaItemZoom.OnImageCacheUpdated
    ----------------------------------------------------------------------------*/
    private void OnImageCacheUpdated(object? sender, ImageCacheUpdateEventArgs e)
    {
        ImageCache? cache = sender as ImageCache;

        if (cache == null)
            throw new CatExceptionInternalFailure("sender wasn't an image cache in OnImageCacheUpdated");

        if (m_model.MediaItem != null)
            EnsureZoomImageFromCache(null, cache, m_model.MediaItem);
    }

    /*----------------------------------------------------------------------------
        %%Function: OnMediaItemUpdated
        %%Qualified: Thetacat.Explorer.MediaItemZoom.OnMediaItemUpdated
    ----------------------------------------------------------------------------*/
    private void OnMediaItemUpdated(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == "Tags")
        {
            PopulateTags();
            m_model.IsTrashItem = m_model.MediaItem?.IsTrashItem ?? false;
            m_model.IsOffline = m_model.MediaItem?.DontPushToCloud ?? false;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: OnCloseReleaseWatchers
        %%Qualified: Thetacat.Explorer.MediaItemZoom.OnCloseReleaseWatchers
    ----------------------------------------------------------------------------*/
    private void OnCloseReleaseWatchers(object? sender, CancelEventArgs e)
    {
        App.State.ImageCache.ImageCacheUpdated -= OnImageCacheUpdated;
        m_model.MediaItem!.PropertyChanged -= OnMediaItemUpdated;
    }

    /*----------------------------------------------------------------------------
        %%Function: EnsureZoomImageFromCache
        %%Qualified: Thetacat.Explorer.MediaItemZoom.EnsureZoomImageFromCache
    ----------------------------------------------------------------------------*/
    private void EnsureZoomImageFromCache(ImageCache? lowResCache, ImageCache cache, MediaItem item)
    {
        ImageCacheItem? cacheItem = cache.GetAnyExistingItem(item.ID);

        if (cacheItem == null || (cacheItem.IsLoadQueued == false && cacheItem.Image == null))
        {
            string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

            if (path != null)
                App.State.ImageCache.TryQueueBackgroundLoadToCache(item, App.State.GetMD5ForItem(item.ID), path, null);
        }

        cacheItem ??= lowResCache?.GetAnyExistingItem(item.ID);

        m_model.Image = cacheItem?.Image;
    }

    /*----------------------------------------------------------------------------
        %%Function: PopulateTags
        %%Qualified: Thetacat.Explorer.MediaItemZoom.PopulateTags
    ----------------------------------------------------------------------------*/
    void PopulateTags()
    {
        m_model.Tags.Clear();

        if (m_model.MediaItem == null)
            return;

        foreach (MediaTag tag in m_model.MediaItem.Tags.Values)
        {
            m_model.Tags.Add(tag);
        }
    }


    /*----------------------------------------------------------------------------
        %%Function: UpdateMetatagPanelIfNecessary
        %%Qualified: Thetacat.Explorer.MediaItemZoom.UpdateMetatagPanelIfNecessary
    ----------------------------------------------------------------------------*/
    void UpdateMetatagPanelIfNecessary()
    {
        if (m_model.MediaItem != null)
            App.State.WindowManager.ApplyMetatagPanel?.UpdateForMedia(
                new MediaItem[] { m_model.MediaItem },
                App.State.MetatagSchema,
                m_model.VectorClock,
                ApplyMetatagChangesFromPanel);
    }

    /*----------------------------------------------------------------------------
        %%Function: SetMediaItem
        %%Qualified: Thetacat.Explorer.MediaItemZoom.SetMediaItem
    ----------------------------------------------------------------------------*/
    void SetMediaItem(MediaItem item)
    {
        m_model.MediaItem = item;

        PopulateTags();
        DataContext = m_model;

        App.State.RegisterWindowPlace(this, "mediaItem-details");

        App.State.ImageCache.ImageCacheUpdated += OnImageCacheUpdated;
        m_model.MediaItem.PropertyChanged += OnMediaItemUpdated;
        m_model.IsTrashItem = item.IsTrashItem;
        m_model.IsOffline = item.DontPushToCloud;

        RebuildMruButtons();
        EnsureZoomImageFromCache(App.State.PreviewImageCache, App.State.ImageCache, item);
        m_model.VectorClock++;
        UpdateMetatagPanelIfNecessary();
    }

    /*----------------------------------------------------------------------------
        %%Function: ApplyMetatagChangesFromPanel
        %%Qualified: Thetacat.Explorer.MediaItemZoom.ApplyMetatagChangesFromPanel
    ----------------------------------------------------------------------------*/
    void ApplyMetatagChangesFromPanel(Dictionary<string, bool?> checkedUncheckedAndIndeterminate, int vectorClock)
    {
        if (m_model.MediaItem == null)
            return;

        MetatagSchema schema = App.State.MetatagSchema;

        if (m_model.VectorClock != vectorClock)
        {
            MessageBox.Show("Can't apply tags. Vector clock mismatch. Sorry.");
            return;
        }

        App.State.WindowManager.ApplyMetatagPanel?.UpdateMediaForMetatagChanges(
            checkedUncheckedAndIndeterminate,
            new MediaItem[] { m_model.MediaItem },
            schema);
    }

    /*----------------------------------------------------------------------------
        %%Function: MediaItemZoom
        %%Qualified: Thetacat.Explorer.MediaItemZoom.MediaItemZoom
    ----------------------------------------------------------------------------*/
    public MediaItemZoom()
    {
        m_nextDelegate = null;
        m_previousDelegate = null;
        InitializeComponent();
        m_sortableListViewSupport = new SortableListViewSupport(MetadataListView);
    }

    /*----------------------------------------------------------------------------
        %%Function: RebuildMruButtons
        %%Qualified: Thetacat.Explorer.MediaItemZoom.RebuildMruButtons
    ----------------------------------------------------------------------------*/
    void RebuildMruButtons()
    {
        HashSet<Guid> newTags = new HashSet<Guid>();
        HashSet<Guid> existingTags = new HashSet<Guid>();

        foreach (Metatag tag in App.State.MetatagMRU.RecentTags)
        {
            newTags.Add(tag.ID);
        }

        foreach (ZoomTag zoomTag in m_model.ZoomTags)
        {
            if (zoomTag.Tag != null)
                existingTags.Add(zoomTag.Tag.ID);
        }

        List<int> itemsToExpire = new List<int>();

        int i = 0;

        // now let's find which indexes we can expire (because they are no longer MRU)
        foreach (ZoomTag zoomTag in m_model.ZoomTags)
        {
            if (zoomTag.Tag == null || !newTags.Contains(zoomTag.Tag.ID))
                itemsToExpire.Add(i);

            i++;
        }

        foreach (Metatag tag in App.State.MetatagMRU.RecentTags)
        {
            // skip builtin tags that shouldn't be on the MRU
            if (tag.ID == BuiltinTags.s_IsTrashItemID)
                continue;

            if (existingTags.Contains(tag.ID))
            {
                // update the tag state
                m_model.UpdateZoomTagFromMedia(tag.ID);
                continue;
            }

            // continue until we have no more slots remaining to expire
            if (itemsToExpire.Count == 0)
                break;

            i = itemsToExpire[0];
            itemsToExpire.RemoveAt(0);

            m_model.SetQuickMetatag(i, tag);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: OnMruPropertyChanged
        %%Qualified: Thetacat.Explorer.MediaItemZoom.OnMruPropertyChanged
    ----------------------------------------------------------------------------*/
    void OnMruPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != "RecentTags")
            throw new CatExceptionInternalFailure($"unknown mru property changed: {args.PropertyName}");

        RebuildMruButtons();
    }

    /*----------------------------------------------------------------------------
        %%Function: MediaItemZoom
        %%Qualified: Thetacat.Explorer.MediaItemZoom.MediaItemZoom
    ----------------------------------------------------------------------------*/
    public MediaItemZoom(
        MediaItem item, GetNextMediaItem? getNextDelegate, GetPreviousMediaItem? getPreviousDelegate, int vectorClockBase)
    {
        m_nextDelegate = getNextDelegate;
        m_previousDelegate = getPreviousDelegate;

        m_model.VectorClock = vectorClockBase;

        Activated += OnActivated;
        this.KeyDown += DoMediaZoomKeyUp;
        this.Closing += OnCloseReleaseWatchers;

        InitializeComponent();
        //m_sortableListViewSupport = new SortableListViewSupport(MetadataListView);

        App.State.MetatagMRU.OnPropertyChanged += OnMruPropertyChanged;
        SetMediaItem(item);
        RebuildMruButtons();
        UpdateMetatagPanelIfNecessary();
        m_sortableListViewSupport = new SortableListViewSupport(MetadataListView);
    }


    /*----------------------------------------------------------------------------
        %%Function: OnGotFocus
        %%Qualified: Thetacat.Explorer.MediaItemZoom.OnGotFocus
    ----------------------------------------------------------------------------*/
    private void OnActivated(object? sender, EventArgs e)
    {
        if (m_model.MediaItem != null)
            UpdateMetatagPanelIfNecessary();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoToggleImageTrashed
        %%Qualified: Thetacat.Explorer.MediaItemZoom.DoToggleImageTrashed
    ----------------------------------------------------------------------------*/
    void DoToggleImageTrashed()
    {
        if (m_model.MediaItem != null)
        {
            m_model.MediaItem.IsTrashItem = !m_model.MediaItem.IsTrashItem;
            m_model.IsTrashItem = m_model.MediaItem.IsTrashItem;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DoMediaZoomKeyUp
        %%Qualified: Thetacat.Explorer.MediaItemZoom.DoMediaZoomKeyUp
    ----------------------------------------------------------------------------*/
    private void DoMediaZoomKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
        else if (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            TogglePruneModeCore();
        }
        else if (e.Key == Key.N || e.Key == Key.Right)
        {
            DoNextImage();
            e.Handled = true;
        }
        else if (e.Key == Key.P || e.Key == Key.Left)
        {
            DoPreviousImage();
            e.Handled = true;
        }
        else if (m_model.IsPruning)
        {
            if (e.Key == Key.D)
            {
                DoToggleImageTrashed();
                DoNextImage();
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                int tagIndex = e.Key - Key.D0;

                ZoomTag zoomTag = m_model.ZoomTags[tagIndex];
                if (zoomTag.Tag == null)
                    return;

                m_model.SetZoomTagState(zoomTag, !zoomTag.IsSet);
                SyncMediaTagStateOnMedia(tagIndex);
            }
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: TogglePruneModeCore
        %%Qualified: Thetacat.Explorer.MediaItemZoom.TogglePruneModeCore
    ----------------------------------------------------------------------------*/
    private void TogglePruneModeCore()
    {
        if (m_model.IsPruning)
        {
            m_model.PruneModeCaption = "Stop Pruning";
            m_model.IsPruning = false;
        }
        else
        {
            m_model.PruneModeCaption = "Start Pruning";
            m_model.IsPruning = true;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: TogglePruneMode
        %%Qualified: Thetacat.Explorer.MediaItemZoom.TogglePruneMode
    ----------------------------------------------------------------------------*/
    private void TogglePruneMode(object sender, RoutedEventArgs e)
    {
        TogglePruneModeCore();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoNextImage
        %%Qualified: Thetacat.Explorer.MediaItemZoom.DoNextImage
    ----------------------------------------------------------------------------*/
    void DoNextImage()
    {
        if (m_nextDelegate != null)
        {
            MediaItem? next = m_nextDelegate(m_model.MediaItem!);
            if (next != null)
                SetMediaItem(next);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DoPreviousImage
        %%Qualified: Thetacat.Explorer.MediaItemZoom.DoPreviousImage
    ----------------------------------------------------------------------------*/
    void DoPreviousImage()
    {
        if (m_previousDelegate != null)
        {
            MediaItem? next = m_previousDelegate(m_model.MediaItem!);
            if (next != null)
                SetMediaItem(next);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: NextImage
        %%Qualified: Thetacat.Explorer.MediaItemZoom.NextImage
    ----------------------------------------------------------------------------*/
    private void NextImage(object sender, RoutedEventArgs e)
    {
        DoNextImage();
    }

    /*----------------------------------------------------------------------------
        %%Function: ToggleImageTrashed
        %%Qualified: Thetacat.Explorer.MediaItemZoom.ToggleImageTrashed
    ----------------------------------------------------------------------------*/
    private void ToggleImageTrashed(object sender, RoutedEventArgs e)
    {
        DoToggleImageTrashed();
    }

    /*----------------------------------------------------------------------------
        %%Function: PreviousImage
        %%Qualified: Thetacat.Explorer.MediaItemZoom.PreviousImage
    ----------------------------------------------------------------------------*/
    private void PreviousImage(object sender, RoutedEventArgs e)
    {
        DoPreviousImage();
    }

    /*----------------------------------------------------------------------------
        %%Function: SyncMediaTagStateOnMedia
        %%Qualified: Thetacat.Explorer.MediaItemZoom.SyncMediaTagStateOnMedia

        This syncs the state of the given tag on the media to the state in the
        model for the tag.  if the tag isn't defined, then reset the model
        state for it to be unset.

        if you want to set a specific tag programmatically, first set the state
        for the zoomtag, then call this.
    ----------------------------------------------------------------------------*/
    void SyncMediaTagStateOnMedia(int tagIndex)
    {
        if (m_model.MediaItem == null)
            return;

        if (m_model.ZoomTags[tagIndex].Tag == null)
        {
            MessageBox.Show("no metatag to apply");
            m_model.SetZoomTagState(m_model.ZoomTags[tagIndex], false);
            return;
        }

        Metatag tag = m_model.ZoomTags[tagIndex].Tag!;

        MediaTag mediaTag = MediaTag.CreateMediaTag(App.State.MetatagSchema, tag.ID, null);

        if (m_model.MediaItem?.Tags.TryGetValue(tag.ID, out MediaTag? existing) ?? false)
            m_model.MediaItem!.FRemoveMediaTag(tag.ID);
        else
            m_model.MediaItem?.FAddOrUpdateMediaTag(mediaTag, true);
    }

    public void Tag1Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(0);
    public void Tag2Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(1);
    public void Tag3Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(2);
    public void Tag4Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(3);
    public void Tag5Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(4);
    public void Tag6Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(5);
    public void Tag7Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(6);
    public void Tag8Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(7);
    public void Tag9Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(8);
    public void Tag10Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(9);
}
