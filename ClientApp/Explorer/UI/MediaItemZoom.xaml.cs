using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Converters;
using Thetacat.Explorer.UI;
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

    private readonly SortableListViewSupport m_sortableListViewSupport;
    private void SortType(object sender, RoutedEventArgs e) => m_sortableListViewSupport.Sort(sender as GridViewColumnHeader);
    private bool m_pruning = false;

    private MediaItemZoomModel m_model = new();

    private void OnImageCacheUpdated(object? sender, ImageCacheUpdateEventArgs e)
    {
        ImageCache? cache = sender as ImageCache;

        if (cache == null)
            throw new CatExceptionInternalFailure("sender wasn't an image cache in OnImageCacheUpdated");

        if (m_model.MediaItem != null)
            EnsureZoomImageFromCache(cache, m_model.MediaItem);
    }

    private void OnMediaItemUpdated(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == "Tags")
        {
            PopulateTags();
        }
    }

    private void OnCloseReleaseWatchers(object? sender, CancelEventArgs e)
    {
        App.State.ImageCache.ImageCacheUpdated -= OnImageCacheUpdated;
        m_model.MediaItem!.PropertyChanged -= OnMediaItemUpdated;
    }

    private void EnsureZoomImageFromCache(ImageCache cache, MediaItem item)
    {
        ImageCacheItem? cacheItem = cache.GetAnyExistingItem(item.ID);

        if (cacheItem == null || (cacheItem.IsLoadQueued == false && cacheItem.Image == null))
        {
            string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

            if (path != null)
                App.State.ImageCache.TryQueueBackgroundLoadToCache(item, path);
        }

        m_model.Image = cacheItem?.Image;
    }

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

    void SetMediaItem(MediaItem item)
    {
        m_model.MediaItem = item;

        PopulateTags();
        DataContext = m_model;

        App.State.RegisterWindowPlace(this, "mediaItem-details");

        App.State.ImageCache.ImageCacheUpdated += OnImageCacheUpdated;
        m_model.MediaItem.PropertyChanged += OnMediaItemUpdated;


        EnsureZoomImageFromCache(App.State.ImageCache, item);
    }

    private GetNextMediaItem? m_nextDelegate;

    public MediaItemZoom(MediaItem item, GetNextMediaItem? getNextDelegate)
    {
        m_nextDelegate = getNextDelegate;
        Closing += OnCloseReleaseWatchers;
        this.KeyDown += DoMediaZoomKeyUp;
        InitializeComponent();
        m_sortableListViewSupport = new SortableListViewSupport(MetadataListView);

        SetMediaItem(item);
    }

    private void DoMediaZoomKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
        else if (e.Key == Key.N)
            DoNextImage();
    }

    private void TogglePruneMode(object sender, RoutedEventArgs e)
    {
        if (m_pruning)
        {
            m_model.PruneModeCaption = "Stop Pruning";
            m_pruning = false;
        }
        else
        {
            m_model.PruneModeCaption = "Start Pruning";
            m_pruning = true;
        }
    }

    void DoNextImage()
    {
        if (m_nextDelegate != null)
        {
            MediaItem? next = m_nextDelegate(m_model.MediaItem!);
            if (next != null)
                SetMediaItem(next);
        }
    }

    private void NextImage(object sender, RoutedEventArgs e)
    {
        DoNextImage();
    }
}