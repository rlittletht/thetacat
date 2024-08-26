using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                App.State.ImageCache.TryQueueBackgroundLoadToCache(item, App.State.GetMD5ForItem(item.ID), path);
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

        EnsureZoomImageFromCache(App.State.PreviewImageCache, App.State.ImageCache, item);
        m_model.VectorClock++;
        UpdateMetatagPanelIfNecessary();
    }

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


    public MediaItemZoom()
    {
        m_nextDelegate = null;
        m_previousDelegate = null;
        InitializeComponent();
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
        InitializeComponent();
        //m_sortableListViewSupport = new SortableListViewSupport(MetadataListView);

        SetMediaItem(item);
        UpdateMetatagPanelIfNecessary();
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
            Close();
        else if (e.Key == Key.N || e.Key == Key.Right)
            DoNextImage();
        else if (e.Key == Key.P || e.Key == Key.Left)
            DoPreviousImage();
        else if (e.Key == Key.D && m_model.IsPruning)
        {
            DoToggleImageTrashed();
            DoNextImage();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: TogglePruneMode
        %%Qualified: Thetacat.Explorer.MediaItemZoom.TogglePruneMode
    ----------------------------------------------------------------------------*/
    private void TogglePruneMode(object sender, RoutedEventArgs e)
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
}
