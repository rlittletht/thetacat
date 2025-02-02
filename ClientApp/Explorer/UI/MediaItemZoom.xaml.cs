﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Thetacat.Explorer.UI;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.ImageCaching;
using Thetacat.Model.Mediatags;
using Thetacat.Types;
using Thetacat.UI.Input;
using Thetacat.Util;

namespace Thetacat.Explorer;

/// <summary>
/// Interaction logic for MediaItemZoom.xaml
/// </summary>
public partial class MediaItemZoom : Window
{
    public delegate void OnZoomClosingDelegate(MediaItemZoom zoom);
    public delegate MediaItem? GetNextMediaItemDelegate(MediaItem item);
    public delegate MediaItem? GetPreviousMediaItemDelegate(MediaItem item);
    public delegate void SyncCatalogDelegate(MediaItem item);

    private readonly SortableListViewSupport m_sortableListViewSupport;
    private void SortType(object sender, RoutedEventArgs e) => m_sortableListViewSupport.Sort(sender as GridViewColumnHeader);
    private readonly GetNextMediaItemDelegate? m_getNextMediaItem;
    private readonly GetPreviousMediaItemDelegate? m_getPreviousMediaItem;
    private readonly SyncCatalogDelegate? m_syncCatalog;

    private readonly MediaItemZoomModel m_model = new();

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
            RebuildMruButtons();
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

        foreach (MediaTag tag in m_model.MediaItem.MediaTags)
        {
            if (tag.Deleted)
                continue;

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
        %%Function: _ShowHideMetatagPanel
        %%Qualified: Thetacat.Explorer.MediaItemZoom._ShowHideMetatagPanel
    ----------------------------------------------------------------------------*/
    private void _ShowHideMetatagPanel()
    {
        if (App.State.WindowManager.ApplyMetatagPanel == null)
            App.State.WindowManager.ApplyMetatagPanel = new ApplyMetatag(ApplyMetatagChangesFromPanel);

        if (App.State.WindowManager.ApplyMetatagPanel.IsVisible)
        {
            App.State.WindowManager.ApplyMetatagPanel.Hide();
        }
        else
        {
            UpdateMetatagPanelIfNecessary();
            App.State.WindowManager.ApplyMetatagPanel.Show();
        }
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
    void ApplyMetatagChangesFromPanel(Dictionary<string, bool?> checkedUncheckedAndIndeterminate, Dictionary<string, string?> values, int vectorClock)
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
            values,
            new MediaItem[] { m_model.MediaItem },
            schema);

        this.Activate();
    }

    /*----------------------------------------------------------------------------
        %%Function: MediaItemZoom
        %%Qualified: Thetacat.Explorer.MediaItemZoom.MediaItemZoom
    ----------------------------------------------------------------------------*/
    public MediaItemZoom()
    {
        m_getNextMediaItem = null;
        m_getPreviousMediaItem = null;
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
        MediaItem item, GetNextMediaItemDelegate? getGetNextMediaItem, GetPreviousMediaItemDelegate? getGetPreviousMediaItem, SyncCatalogDelegate? syncCatalog,
        int vectorClockBase)
    {
        m_getNextMediaItem = getGetNextMediaItem;
        m_getPreviousMediaItem = getGetPreviousMediaItem;
        m_syncCatalog = syncCatalog;

        m_model.VectorClock = vectorClockBase;

        Activated += OnActivated;
        this.KeyDown += DoMediaZoomKeyDown;
        this.Closing += OnCloseReleaseWatchers;

        InitializeComponent();

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
        %%Function: DoMediaZoomKeyDown
        %%Qualified: Thetacat.Explorer.MediaItemZoom.DoMediaZoomKeyDown
    ----------------------------------------------------------------------------*/
    private void DoMediaZoomKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
            else if ((e.Key >= Key.D0 && e.Key <= Key.D9)
                     || (e.Key >= Key.A && e.Key <= Key.W)
                     || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                int tagIndex;

                if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                    tagIndex = e.Key - Key.NumPad0;
                else if (e.Key <= Key.D9)
                    tagIndex = e.Key - Key.D0;
                else if (e.Key < Key.D)
                    tagIndex = 10 + e.Key - Key.A;
                else if (e.Key < Key.N)
                    tagIndex = 13 + e.Key - Key.E;
                else if (e.Key < Key.P)
                    tagIndex = 22 + e.Key - Key.O;
                else if (e.Key <= Key.W)
                    tagIndex = 23 + e.Key - Key.Q;
                else
                    throw new CatExceptionInternalFailure($"key out of range: {e.Key}");

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
        %%Function: DoSyncCatalog
        %%Qualified: Thetacat.Explorer.MediaItemZoom.DoSyncCatalog
    ----------------------------------------------------------------------------*/
    private void DoSyncCatalog(object sender, RoutedEventArgs e)
    {
        m_syncCatalog?.Invoke(m_model.MediaItem!);
    }

    /*----------------------------------------------------------------------------
        %%Function: DoNextImage
        %%Qualified: Thetacat.Explorer.MediaItemZoom.DoNextImage
    ----------------------------------------------------------------------------*/
    void DoNextImage()
    {
        if (m_getNextMediaItem != null)
        {
            MediaItem? next = m_getNextMediaItem(m_model.MediaItem!);
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
        if (m_getPreviousMediaItem != null)
        {
            MediaItem? next = m_getPreviousMediaItem(m_model.MediaItem!);
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

        if (m_model.MediaItem?.TryGetMediaTag(tag.ID, out _) ?? false)
            m_model.MediaItem!.FRemoveMediaTag(tag.ID);
        else
            m_model.MediaItem?.FAddOrUpdateMediaTag(mediaTag, true);

        UpdateMetatagPanelIfNecessary();
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
    public void Tag11Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(10);
    public void Tag12Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(11);
    public void Tag13Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(12);
    public void Tag14Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(13);
    public void Tag15Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(14);
    public void Tag16Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(15);
    public void Tag17Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(16);
    public void Tag18Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(17);
    public void Tag19Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(18);
    public void Tag20Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(19);
    public void Tag21Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(20);
    public void Tag22Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(21);
    public void Tag23Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(22);
    public void Tag24Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(23);
    public void Tag25Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(24);
    public void Tag26Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(25);
    public void Tag27Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(26);
    public void Tag28Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(27);
    public void Tag29Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(28);
    public void Tag30Click(object sender, RoutedEventArgs e) => SyncMediaTagStateOnMedia(29);

    private void ToggleMetatagPanel(object sender, RoutedEventArgs e)
    {
        if (App.State.WindowManager.ApplyMetatagPanel != null)
        {
            App.State.WindowManager.ApplyMetatagPanel.Close();
            App.State.WindowManager.ApplyMetatagPanel = null;
        }
        else
        {
            _ShowHideMetatagPanel();
        }
    }

    private void EditMetatagValue(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Parent: ContextMenu { PlacementTarget: ListViewItem { DataContext: MediaTag mediaTag } } })
        {
            if (InputFormats.FPrompt("Set metatag value", mediaTag.Value ?? "", out string? newValue, this))
            {
                MediaTag newMediaTag = MediaTag.CreateMediaTag(App.State.MetatagSchema, mediaTag.Metatag.ID, newValue);
                m_model.MediaItem?.FAddOrUpdateMediaTag(newMediaTag, true);
            }
        }
    }
}
