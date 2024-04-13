using MetadataExtractor.Formats.Xmp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Thetacat.Logging;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Explorer.Commands;
using Thetacat.Util;
using Thetacat.Explorer.UI;
using Thetacat.ServiceClient;

namespace Thetacat.Explorer;

/// <summary>
/// Interaction logic for MediaExplorer.xaml
/// </summary>
public partial class MediaExplorer : UserControl
{
    private MediaExplorerCollection? m_collection;

    public MediaExplorerModel Model { get; set; } = new();
    private ExplorerItemSize m_itemSize = ExplorerItemSize.Medium;
    private readonly ItemSelector m_selector;

    public MediaExplorer()
    {
        InitializeComponent();
           
        DataContext = Model;
        m_selector = new ItemSelector(null, UpdateMetatagPanelIfNecessary);

        Model.ShowHideMetatagPanel = new ShowHideMetatagPanelCommand(_ShowHideMetatagPanel);
        Model.DeleteItems = new DeleteCommand(_DeleteItems);
        Model.ResetCacheItems = new ResetCacheItemsCommand(_ClearCacheItems);
        Model.RotateItemsRight = new RotateItemsRightCommand(_RotateItemsRight);
        Model.MirrorItems = new MirrorItemsCommand(_MirrorItems);
        Model.SelectPanel = new SelectPanelCommand(m_selector._SelectPanel);
        Model.ExtendSelectPanel = new SelectPanelCommand(m_selector._ExtendSelectPanel);
        Model.AddSelectPanel = new SelectPanelCommand(m_selector._AddSelectPanel);
        Model.AddExtendSelectPanel = new SelectPanelCommand(m_selector._StickyExtendSelectPanel);
        Model.ContextSelectPanel = new SelectPanelCommand(m_selector._ContextSelectPanel);
        Model.LaunchItem = new LaunchItemCommand(LaunchItem);
        Model.RemoveMenuTag = new ProcessMenuTagCommand(RemoveMenuTagFromSelectedItems);
        Model.AddMenuTag = new ProcessMenuTagCommand(ApplyMenuTagToSelectedItems);
    }

    private readonly List<MediaItemZoom> m_zooms = new List<MediaItemZoom>();

    MediaItem? GetNextItem(MediaItem item)
    {
        MediaExplorerItem? nextItem = m_collection?.GetNextItem(item);

        if (nextItem == null)
            return null;

        App.State.Catalog.TryGetMedia(nextItem.MediaId, out MediaItem? nextMediaItem);
        return nextMediaItem;
    }

    public void LaunchItem(MediaExplorerItem? context)
    {
        if (context == null)
            return;

        MediaItem mediaItem = App.State.Catalog.GetMediaFromId(context.MediaId);
            
        MediaItemZoom zoom = new MediaItemZoom(mediaItem, GetNextItem);

        zoom.Closing += OnMediaZoomClosing;

        m_zooms.Add(zoom);
        zoom.Show();
    }

    public void UpdateCollectionDimensions()
    {
        m_collection?.AdjustPanelItemWidth(Model.PanelItemWidth);
        m_collection?.AdjustPanelItemHeight(Model.PanelItemHeight);
        m_collection?.AdjustExplorerWidth(ExplorerBox.ActualWidth);
        m_collection?.AdjustExplorerHeight(ExplorerBox.ActualHeight);
        m_collection?.UpdateItemsPerLine();
    }

    public void ScrollTo(int line)
    {
    }

    public void ResetContent(MediaExplorerCollection collection)
    {
        m_collection = collection;
        m_selector.ResetCollection(collection);
        UpdateCollectionDimensions();
        ExplorerBox.ItemsSource = collection.ExplorerLines;
    }

    private void OnExplorerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // notify the collection of the change
        if (e.WidthChanged)
        {
            m_collection?.AdjustExplorerHeight(e.NewSize.Height);
            m_collection?.AdjustExplorerWidth(e.NewSize.Width);
            m_collection?.UpdateItemsPerLine();
        }
    }

    private void OnExplorerLoaded(object sender, RoutedEventArgs e)
    {
        UpdateCollectionDimensions();
    }

    private void OnMediaZoomClosing(object? sender, CancelEventArgs e)
    {
        if (sender != null)
            m_zooms.Remove((MediaItemZoom)sender);
    }

    public void Close()
    {
        foreach (MediaItemZoom zoom in m_zooms)
        {
            zoom.Closing -= OnMediaZoomClosing;
            zoom.Close();
        }

        if (m_collection != null)
        {
            if (m_applyMetatagPanel != null)
            {
                m_applyMetatagPanel.Close();
                m_applyMetatagPanel = null;
            }
            m_collection.Close();
            m_collection = null;
        }
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        MainWindow.LogForApp(EventType.Information, $"OnScrollChanged: Change: {e.VerticalChange}, Offset: {e.VerticalOffset}");
        m_collection?.NotifyTopVisibleItem((int)e.VerticalOffset);
        m_collection?.EnsureImagesForSurroundingRows((int)e.VerticalOffset);
    }

    private static readonly double baseItemWidth = 148.0;
    private static readonly double baseItemHeight = 96.0;

    private static readonly Dictionary<ExplorerItemSize, double> s_itemSizeAdjusts =
        new()
        {
            { ExplorerItemSize.Medium, 1.0 },
            { ExplorerItemSize.Large, 1.75 },
            { ExplorerItemSize.Small, 0.66 }
        };

    void SetModelFromExplorerItemSize(ExplorerItemSize itemSize)
    {
        double adjust = s_itemSizeAdjusts[itemSize];

        Model.ImageWidth = baseItemWidth * adjust;
        Model.ImageHeight = baseItemHeight * adjust;
        Model.PanelItemHeight = Model.ImageHeight + 16.0;
        Model.PanelItemWidth = Model.ImageWidth;
        UpdateCollectionDimensions();
    }

    public void SetExplorerItemSize(ExplorerItemSize itemSize)
    {
        m_itemSize = itemSize;
        SetModelFromExplorerItemSize(m_itemSize);
        App.State.ActiveProfile.ExplorerItemSize = itemSize;
    }

    private ApplyMetatag? m_applyMetatagPanel = null;

    private List<MediaItem> GetSelectedMediaItems(IEnumerable<MediaExplorerItem> selectedItems)
    {
        List<MediaItem> mediaItems = new();
        ICatalog catalog = App.State.Catalog;

        foreach (MediaExplorerItem item in selectedItems)
        {
            mediaItems.Add(catalog.GetMediaFromId(item.MediaId));
        }

        return mediaItems;
    }

    private void UpdateMetatagPanelIfNecessary(IEnumerable<MediaExplorerItem> selectedItems)
    {
        if (m_applyMetatagPanel != null)
        {
            MicroTimer timer = new MicroTimer();
            timer.Start();
            List<MediaItem> mediaItems = GetSelectedMediaItems(selectedItems);

            m_applyMetatagPanel.UpdateForMedia(mediaItems, App.State.MetatagSchema, m_selector.VectorClock);
            MainWindow.LogForApp(EventType.Warning, $"UpdateMetatagPanelIfNecessary: {timer.Elapsed()}");
        }

    }

    void RemoveMediatagFromMedia(Guid mediaTagID, IEnumerable<MediaItem> selectedItems)
    {
        foreach (MediaItem item in selectedItems)
        {
            item.FRemoveMediaTag(mediaTagID);
        }
    }

    void SetMediatagForMedia(MediaTag mediaTag, IEnumerable<MediaItem> selectedItems)
    {
        foreach (MediaItem item in selectedItems)
        {
            item.FAddOrUpdateMediaTag(mediaTag, true);
        }
    }

    void ApplySyncMetatags(Dictionary<string, bool?> checkedUncheckedAndIndeterminate, int vectorClock)
    {
        MetatagSchema schema = App.State.MetatagSchema;

        if (m_selector.VectorClock != vectorClock)
        {
            MessageBox.Show("Can't apply tags. Vector clock mismatch. Sorry.");
            return;
        }

        List<MediaItem> mediaItems = GetSelectedMediaItems(m_selector.SelectedItems);
        Dictionary<string, bool?> originalState = ApplyMetatag.GetCheckedAndIndetermineFromMediaSet(mediaItems);

        // find all the tags to remove
        foreach (KeyValuePair<string, bool?> item in originalState)
        {
            // if its indeterminate, then there is no chang
            if (!checkedUncheckedAndIndeterminate.TryGetValue(item.Key, out bool? checkedState)
                || checkedState == null)
            {
                continue;
            }

            // if it was true and now its false, remove it
            if (item.Value == true && checkedState == false)
            {
                RemoveMediatagFromMedia(Guid.Parse(item.Key), mediaItems);
            }

            if (item.Value == false)
                MessageBox.Show("Strange. We have a false in the checked/indeterminate");
        }

        int mruClock = App.State.MetatagMRU.VectorClock;

        // find all the tags to add
        foreach (KeyValuePair<string, bool?> item in checkedUncheckedAndIndeterminate)
        {
            if (item.Value is true)
            {
                if (!originalState.TryGetValue(item.Key, out bool? checkedState) 
                    || checkedState == null
                    || checkedState == false)
                {
                    // it was originally unset(false), was indeterminate, or was false
                    MediaTag mediaTag = MediaTag.CreateMediaTag(schema, Guid.Parse(item.Key), null);
                    SetMediatagForMedia(mediaTag, mediaItems);

                    App.State.MetatagMRU.TouchMetatag(mediaTag.Metatag);
                }
            }
        }

        if (mruClock != App.State.MetatagMRU.VectorClock)
        {
            App.State.ActiveProfile.MetatagMru.Clear();
            foreach (Metatag tag in App.State.MetatagMRU.RecentTags)
            {
                App.State.ActiveProfile.MetatagMru.Add(tag.ID.ToString());
            }

            App.State.Settings.WriteSettings();
        }

        UpdateMetatagPanelIfNecessary(m_selector.SelectedItems);
    }

    private void UnloadItemCaches(MediaExplorerItem explorerItem)
    {
        MediaItem item = App.State.Catalog.GetMediaFromId(explorerItem.MediaId);

        BitmapSource? imageSource = explorerItem.TileImage;
        explorerItem.TileImage = null;
        App.State.PreviewImageCache.ResetImageForKey(item.ID);
        App.State.ImageCache.ResetImageForKey(item.ID);
    }

    private void _ClearCacheItems(MediaExplorerItem? context)
    {
        List<MediaItem> itemsToQueue = new();

        foreach (MediaExplorerItem explorerItem in m_selector.SelectedItems)
        {
            MediaItem item = App.State.Catalog.GetMediaFromId(explorerItem.MediaId);

            try
            {
                UnloadItemCaches(explorerItem);
                itemsToQueue.Add(item);
                App.State.Derivatives.DeleteMediaItem(item.ID);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete item: {item.ID}: {item.VirtualPath}: {ex}");
            }
        }

        MediaExplorerCollection.QueueImageCacheLoadForMediaItems(itemsToQueue);
    }

    private void _RotateItemsRight(MediaExplorerItem? context)
    {
        List<MediaItem> itemsToQueue = new();

        foreach (MediaExplorerItem explorerItem in m_selector.SelectedItems)
        {
            MediaItem item = App.State.Catalog.GetMediaFromId(explorerItem.MediaId);

            try
            {
                int rotate = item.TransformRotate ?? 0;
                rotate = (rotate + 90) % 360;

                if (rotate == 0)
                    item.TransformRotate = null;
                else
                    item.TransformRotate = rotate;

                // when we unload the caches, someone might immediately queue a reload
                // of the cache. make sure we've already changed the mediaitem
                UnloadItemCaches(explorerItem);
                itemsToQueue.Add(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not rotate item: {item.ID}: {item.VirtualPath}: {ex}");
            }
        }

        MediaExplorerCollection.QueueImageCacheLoadForMediaItems(itemsToQueue);
    }

    private void _MirrorItems(MediaExplorerItem? context)
    {
        List<MediaItem> itemsToQueue = new();

        foreach (MediaExplorerItem explorerItem in m_selector.SelectedItems)
        {
            MediaItem item = App.State.Catalog.GetMediaFromId(explorerItem.MediaId);

            try
            {
                item.TransformMirror = !item.TransformMirror;
                // when we unload the caches, someone might immediately queue a reload
                // of the cache. make sure we've already changed the mediaitem
                UnloadItemCaches(explorerItem);
                itemsToQueue.Add(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not mirror item: {item.ID}: {item.VirtualPath}: {ex}");
            }
        }

        MediaExplorerCollection.QueueImageCacheLoadForMediaItems(itemsToQueue);
    }

    private void _DeleteItems(MediaExplorerItem? context)
    {
        List<MediaItem> mediaItems = GetSelectedMediaItems(m_selector.SelectedItems);

        if (mediaItems.Count == 0)
            return;

        if (MessageBox.Show($"Are you sure you want to delete {mediaItems.Count} items? This cannot be undone.", "Confirm delete", MessageBoxButton.YesNo)
            != MessageBoxResult.Yes)
        {
            return;
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
    }

    private void _ShowHideMetatagPanel(MediaExplorerItem? context)
    {
        if (m_applyMetatagPanel == null)
        {
            m_applyMetatagPanel = new ApplyMetatag(ApplySyncMetatags);
            m_applyMetatagPanel.Closing += ((_, _) => m_applyMetatagPanel = null);
        }

        if (m_applyMetatagPanel.IsVisible)
        {
            m_applyMetatagPanel.Hide();
        }
        else
        {
            UpdateMetatagPanelIfNecessary(m_selector.SelectedItems);
            m_applyMetatagPanel.Show();
        }
    }

    private void ItemMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // figure out either the current item, or the selected items

        if (e.OriginalSource is System.Windows.Controls.Image { Parent: StackPanel { DataContext: MediaExplorerItem item } })
        {
            Model.ExplorerContextMenu.AppliedTags.Clear();

            if (App.State.Catalog.TryGetMedia(item.MediaId, out MediaItem? mediaItem))
            {
                foreach (KeyValuePair<Guid, MediaTag> tag in mediaItem.Tags)
                {
                    if (MetatagStandards.GetStandardFromStandardTag(tag.Value.Metatag.Standard) != MetatagStandards.Standard.User)
                        continue;

                    Model.ExplorerContextMenu.AppliedTags.Add(
                        new ExplorerMenuTag()
                        {
                            MediaTagId = tag.Value.Metatag.ID,
                            TagDescription = tag.Value.Metatag.Description,
                            TagName = tag.Value.Metatag.Name
                        });
                }
            }

            MainWindow.LogForApp(EventType.Information, $"hit test result: {item.TileSrc}, {item.TileLabel}");
        }

        if (Model.ExplorerContextMenu.RecentTagVectorClock != App.State.MetatagMRU.VectorClock)
        {
            Model.ExplorerContextMenu.AdvertisedTags.Clear();
            foreach (Metatag tag in App.State.MetatagMRU.RecentTags)
            {
                Model.ExplorerContextMenu.AdvertisedTags.Add(
                    new ExplorerMenuTag()
                    {
                        MediaTagId = tag.ID,
                        TagDescription = tag.Description,
                        TagName = tag.Name
                    });
            }
        }
    }

    private void RemoveMenuTagFromSelectedItems(ExplorerMenuTag? menuTag)
    {
        if (menuTag == null) return;
        List<MediaItem> mediaItems = GetSelectedMediaItems(m_selector.SelectedItems);

        RemoveMediatagFromMedia(menuTag.MediaTagId, mediaItems);
    }

    private void ApplyMenuTagToSelectedItems(ExplorerMenuTag? menuTag)
    {
        if (menuTag == null) return;

        MetatagSchema schema = App.State.MetatagSchema;
        List<MediaItem> mediaItems = GetSelectedMediaItems(m_selector.SelectedItems);
        MediaTag mediaTag = MediaTag.CreateMediaTag(schema, menuTag.MediaTagId, null);
        SetMediatagForMedia(mediaTag, mediaItems);
    }
}