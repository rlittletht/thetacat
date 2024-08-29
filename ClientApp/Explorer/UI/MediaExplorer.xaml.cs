using MetadataExtractor.Formats.Xmp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Thetacat.Logging;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Explorer.Commands;
using Thetacat.Util;
using Thetacat.Explorer.UI;
using Thetacat.Import;
using Thetacat.ServiceClient;

namespace Thetacat.Explorer;

/// <summary>
/// Interaction logic for MediaExplorer.xaml
/// </summary>
public partial class MediaExplorer : UserControl
{
    private MediaExplorerCollection? m_collection;

    public MediaExplorerModel Model { get; set; } = new();
    private readonly ItemSelector m_selector;

    public MediaExplorer()
    {
        InitializeComponent();

        DataContext = Model;
        m_selector = new ItemSelector(null, UpdateMetatagPanelIfNecessary);

        Model.ShowHideMetatagPanel = new ShowHideMetatagPanelCommand(_ShowHideMetatagPanel);
        Model.DeleteItems = new DeleteCommand(_DeleteItems);
        Model.ToggleTopOfStackItems = new ToggleTopOfStackCommand(_ToggleTopOfStackItems);
        Model.OpenItemsStack = new OpenItemsStackCommand(_OpenItemsStack);
        Model.ResetCacheItems = new ResetCacheItemsCommand(_ClearCacheItems);
        Model.RotateItemsRight = new RotateItemsRightCommand(_RotateItemsRight);
        Model.MirrorItems = new MirrorItemsCommand(_MirrorItems);
        Model.SelectPanel = new SelectPanelCommand(m_selector._SelectPanel);
        Model.ExtendSelectPanel = new SelectPanelCommand(m_selector._ExtendSelectPanel);
        Model.AddSelectPanel = new SelectPanelCommand(m_selector._AddSelectPanel);
        Model.AddExtendSelectPanel = new SelectPanelCommand(m_selector._StickyExtendSelectPanel);
        Model.ContextSelectPanel = new SelectPanelCommand(m_selector._ContextSelectPanel);
        Model.LaunchItem = new LaunchItemCommand(LaunchItem);
        Model.EditNewVersion = new EditNewVersionCommand(_EditNewVersion);
        Model.RemoveMenuTag = new ProcessMenuTagCommand(RemoveMenuTagFromSelectedItems);
        Model.AddMenuTag = new ProcessMenuTagCommand(ApplyMenuTagToSelectedItems);
    }

    MediaItem? GetNextItem(MediaItem item)
    {
        MediaExplorerItem? nextItem = m_collection?.GetNextItem(item);

        if (nextItem == null)
            return null;

        App.State.Catalog.TryGetMedia(nextItem.MediaId, out MediaItem? nextMediaItem);
        return nextMediaItem;
    }

    MediaItem? GetPreviousItem(MediaItem item)
    {
        MediaExplorerItem? previousItem = m_collection?.GetPreviousItem(item);

        if (previousItem == null)
            return null;

        App.State.Catalog.TryGetMedia(previousItem.MediaId, out MediaItem? nextMediaItem);
        return nextMediaItem;
    }

    public void _EditNewVersion(MediaExplorerItem? context)
    {
        if (context == null)
            return;

        if (m_selector.SelectedItems.Count != 1)
        {
            MessageBox.Show("You must select exactly one item in order to create and edit a version");
            return;
        }

        foreach (MediaExplorerItem item in m_selector.SelectedItems)
        {
            MediaItem mediaItem = App.State.Catalog.GetMediaFromId(item.MediaId);

            MediaItem? itemNew = App.State.Catalog.CreateVersionBasedOn(App.State.Cache, mediaItem);

            if (itemNew != null)
            {
                // now we have to get this item into the cache and refresh the catalog
                List<MediaItem> itemsToQueue = new() { itemNew };

                MediaExplorerCollection.QueuePreviewImageCacheLoadForMediaItems(itemsToQueue);
            }
        }
    }

    // with a 125,000 range of vector clocks, we get 34k vector clock ranges...
    private static readonly int s_zoomVectorClockRange = 125000;
    private int m_nextZoomVectorClockBase = s_zoomVectorClockRange;

    public void LaunchItem(MediaExplorerItem? context)
    {
        if (context == null)
            return;

        MediaItem mediaItem = App.State.Catalog.GetMediaFromId(context.MediaId);

        MediaItemZoom zoom = new MediaItemZoom(mediaItem, GetNextItem, GetPreviousItem, m_nextZoomVectorClockBase);
        m_nextZoomVectorClockBase += s_zoomVectorClockRange;

        App.State.WindowManager.AddZoom(zoom);
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

    public void ClearSelection()
    {
        m_selector.ResetSelection();
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

    public void Close()
    {
        if (m_collection != null)
        {
            App.State.WindowManager.OnCloseCollection();

            m_collection.Close();
            m_collection = null;
        }
    }

    public void ToggleMetatagPanel()
    {
        if (App.State.WindowManager.ApplyMetatagPanel != null)
        {
            App.State.WindowManager.ApplyMetatagPanel.Close();
            App.State.WindowManager.ApplyMetatagPanel = null;
        }
        else
        {
            _ShowHideMetatagPanel(null);
        }
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        App.LogForApp(EventType.Verbose, $"OnScrollChanged: Change: {e.VerticalChange}, Offset: {e.VerticalOffset}");
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
            { ExplorerItemSize.ExtraLarge, 2.5 },
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
        Model.ItemSize = itemSize;
        SetModelFromExplorerItemSize(Model.ItemSize);
        App.State.ActiveProfile.ExplorerItemSize = itemSize;
    }

    private void UpdateMetatagPanelIfNecessary(IReadOnlyCollection<MediaItem> mediaItems)
    {
        App.State.WindowManager.ApplyMetatagPanel?.UpdateForMedia(mediaItems, App.State.MetatagSchema, m_selector.VectorClock, ApplySyncMetatags);
    }

    public void OnParentWindowActivated()
    {
        UpdateMetatagPanelIfNecessary(ItemSelector.GetSelectedMediaItems(m_selector.SelectedItems));
    }

    /*----------------------------------------------------------------------------
        %%Function: ApplySyncMetatags
        %%Qualified: Thetacat.Explorer.MediaExplorer.ApplySyncMetatags

        Apply the given tags to the media item(s). Use vectorClock to validate
        that the MetatagPanel and this model change function agree on the model
        that should be updated (both the set of items and the version of the
        items)
    ----------------------------------------------------------------------------*/
    void ApplySyncMetatags(Dictionary<string, bool?> checkedUncheckedAndIndeterminate, int vectorClock)
    {
        MetatagSchema schema = App.State.MetatagSchema;

        if (m_selector.VectorClock != vectorClock)
        {
            MessageBox.Show("Can't apply tags. Vector clock mismatch. Sorry.");
            return;
        }

        List<MediaItem> mediaItems = ItemSelector.GetSelectedMediaItems(m_selector.SelectedItems);

        App.State.WindowManager.ApplyMetatagPanel?.UpdateMediaForMetatagChanges(checkedUncheckedAndIndeterminate, mediaItems, schema);
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

        MediaExplorerCollection.QueuePreviewImageCacheLoadForMediaItems(itemsToQueue);
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

        MediaExplorerCollection.QueuePreviewImageCacheLoadForMediaItems(itemsToQueue);
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

        MediaExplorerCollection.QueuePreviewImageCacheLoadForMediaItems(itemsToQueue);
    }

    private void _ToggleTopOfStackItems(MediaExplorerItem? context)
    {
        foreach (MediaExplorerItem item in m_selector.SelectedItems)
        {
            item.IsTopOfStack = !item.IsTopOfStack;
        }
    }

    private void _OpenItemsStack(MediaExplorerItem? context)
    {
        foreach (MediaExplorerItem item in m_selector.SelectedItems)
        {
            MediaItem mediaItem = App.State.Catalog.GetMediaFromId(item.MediaId);

            MediaStack? stack =
                mediaItem.MediaStack != null ? App.State.Catalog.MediaStacks.Items[mediaItem.MediaStack.Value] :
                mediaItem.VersionStack != null ? App.State.Catalog.VersionStacks.Items[mediaItem.VersionStack.Value] : null;

            if (stack != null)
                StackExplorer.ShowStackExplorer(stack);
        }
    }

    private void _DeleteItems(MediaExplorerItem? context)
    {
        List<MediaItem> mediaItems = ItemSelector.GetSelectedMediaItems(m_selector.SelectedItems);

        m_collection?.FDoDeleteItems(mediaItems);
    }

    private void _ShowHideMetatagPanel(MediaExplorerItem? context)
    {
        if (App.State.WindowManager.ApplyMetatagPanel == null)
            App.State.WindowManager.ApplyMetatagPanel = new ApplyMetatag(ApplySyncMetatags);

        if (App.State.WindowManager.ApplyMetatagPanel.IsVisible)
        {
            App.State.WindowManager.ApplyMetatagPanel.Hide();
        }
        else
        {
            UpdateMetatagPanelIfNecessary(ItemSelector.GetSelectedMediaItems(m_selector.SelectedItems));
            App.State.WindowManager.ApplyMetatagPanel.Show();
        }
    }

    private void ItemMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // figure out either the current item, or the selected items

        if (e.OriginalSource is System.Windows.Controls.Image { Parent: Grid { DataContext: MediaExplorerItem item } })
        {
            Model.ExplorerContextMenu.AppliedTags.Clear();

            if (App.State.Catalog.TryGetMedia(item.MediaId, out MediaItem? mediaItem))
            {
                foreach (KeyValuePair<Guid, MediaTag> tag in mediaItem.Tags)
                {
                    if (MetatagStandards.GetStandardFromStandardTag(tag.Value.Metatag.Standard) != MetatagStandards.Standard.User
                        && tag.Value.Metatag.ID != BuiltinTags.s_DontPushToCloudID
                        && tag.Value.Metatag.ID != BuiltinTags.s_IsTrashItemID)
                    {
                        continue;
                    }

                    Model.ExplorerContextMenu.AppliedTags.Add(
                        new ExplorerMenuTag()
                        {
                            MediaTagId = tag.Value.Metatag.ID,
                            TagDescription = tag.Value.Metatag.Description,
                            TagName = tag.Value.Metatag.Name
                        });
                }
            }

            App.LogForApp(EventType.Verbose, $"hit test result: {item.TileSrc}, {item.TileLabel}");
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
        List<MediaItem> mediaItems = ItemSelector.GetSelectedMediaItems(m_selector.SelectedItems);

        ApplyMetatag.RemoveMediatagFromMedia(menuTag.MediaTagId, mediaItems);

        // and now invalidate the selection to make sure we update the metatag panel
        m_selector.NotifySelectionChanged();
    }

    private void ApplyMenuTagToSelectedItems(ExplorerMenuTag? menuTag)
    {
        if (menuTag == null) return;

        MetatagSchema schema = App.State.MetatagSchema;
        List<MediaItem> mediaItems = ItemSelector.GetSelectedMediaItems(m_selector.SelectedItems);
        MediaTag mediaTag = MediaTag.CreateMediaTag(schema, menuTag.MediaTagId, null);
        ApplyMetatag.SetMediatagForMedia(mediaTag, mediaItems);

        // and now invalidate the selection to make sure we update the metatag panel
        m_selector.NotifySelectionChanged();
    }
}
