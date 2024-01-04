using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Thetacat.Logging;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.Standards;
using Thetacat.UI.Explorer;
using Thetacat.UI.Explorer.Commands;
using Image = System.Drawing.Image;

namespace Thetacat.UI
{
    /// <summary>
    /// Interaction logic for MediaExplorer.xaml
    /// </summary>
    public partial class MediaExplorer : UserControl
    {
        private MediaExplorerCollection? m_collection;

        public MediaExplorerModel Model { get; set; } = new();
        private ExplorerItemSize m_itemSize = ExplorerItemSize.Medium;

        MetatagMRU m_metatagMRU = new MetatagMRU();

        public MediaExplorer()
        {
            InitializeComponent();
//            ExplorerBox.ItemsSource = Model.ExplorerLines;
            DataContext = Model;
            Model.ShowHideMetatagPanel = new ShowHideMetatagPanelCommand(_ShowHideMetatagPanel);
            Model.SelectPanel = new SelectPanelCommand(_SelectPanel);
            Model.ExtendSelectPanel = new SelectPanelCommand(_ExtendSelectPanel);
            Model.AddSelectPanel = new SelectPanelCommand(_AddSelectPanel);
            Model.AddExtendSelectPanel = new SelectPanelCommand(_StickyExtendSelectPanel);
        }

        public void UpdateCollectionDimensions()
        {
            m_collection?.AdjustPanelItemWidth(Model.PanelItemWidth);
            m_collection?.AdjustPanelItemHeight(Model.PanelItemHeight);
            m_collection?.AdjustExplorerWidth(ExplorerBox.ActualWidth);
            m_collection?.AdjustExplorerHeight(ExplorerBox.ActualHeight);
            m_collection?.UpdateItemsPerLine();
        }

        public void ResetContent(MediaExplorerCollection collection)
        {
            m_collection = collection;
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

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            MainWindow.LogForApp(EventType.Information, $"OnScrollChanged: Change: {e.VerticalChange}, Offset: {e.VerticalOffset}");
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
            MainWindow._AppState.Settings.ExplorerItemSize = itemSize;
        }

        private ApplyMetatag? m_applyMetatagPanel = null;

        private LineItemOffset? m_pinnedSelectionClick;
        private bool m_pinnedSelectionClickSelect = false;

        private readonly HashSet<MediaExplorerItem> m_itemsSelected = new();

        private void ClearSelectedItems()
        {
            foreach (MediaExplorerItem item in m_itemsSelected)
            {
                item.Selected = false;
            }
            m_itemsSelected.Clear();
        }

        private void SelectExplorerItem(MediaExplorerItem item)
        {
            m_itemsSelected.Add(item);
            item.Selected = true;
        }

        private void UnselectExplorerItem(MediaExplorerItem item)
        {
            if (m_itemsSelected.Contains(item))
            {
                m_itemsSelected.Remove(item);
                item.Selected = false;
            }
        }

        /*----------------------------------------------------------------------------
            %%Function: ToggleSelectExplorerItem
            %%Qualified: Thetacat.UI.MediaExplorer.ToggleSelectExplorerItem

            Toggles the selection state of the given item.  Returns whether we
            selected or unselected the item.
        ----------------------------------------------------------------------------*/
        private bool ToggleSelectExplorerItem(MediaExplorerItem item)
        {
            if (m_itemsSelected.Contains(item))
            {
                m_itemsSelected.Remove(item);
                item.Selected = false;
                return false;
            }
            else
            {
                m_itemsSelected.Add(item);
                item.Selected = true;
                return true;
            }
        }

        /*----------------------------------------------------------------------------
            %%Function: _SelectPanel
            %%Qualified: Thetacat.UI.MediaExplorer._SelectPanel

            This is just a regular mouse click. Deselect everything else and set the
            pinned click
        ----------------------------------------------------------------------------*/
        private void _SelectPanel(MediaExplorerItem? context)
        {
            if (m_collection == null)
                return;

            m_collection.DebugVerifySelectedItems(m_itemsSelected);
            ClearSelectedItems();
            m_pinnedSelectionClick = null;
            if (context != null)
            {
                SelectExplorerItem(context);
                m_pinnedSelectionClick = m_collection.GetLineItemOffsetForMediaItem(context);
                m_pinnedSelectionClickSelect = true;
            }

            m_collection.DebugVerifySelectedItems(m_itemsSelected);
        }

        /*----------------------------------------------------------------------------
            %%Function: _ExtendSelectPanel
            %%Qualified: Thetacat.UI.MediaExplorer._ExtendSelectPanel

            This is a shift+click. It extends from the pinned selection click to the
            current offset
        ----------------------------------------------------------------------------*/
        private void _ExtendSelectPanel(MediaExplorerItem? context)
        {
            if (m_collection == null)
                return;

            m_collection.DebugVerifySelectedItems(m_itemsSelected);
            if (context == null)
            {
                ClearSelectedItems();
                m_pinnedSelectionClick = null;
                m_pinnedSelectionClickSelect = false;
                m_collection.DebugVerifySelectedItems(m_itemsSelected);
                return;
            }

            m_pinnedSelectionClick ??= new LineItemOffset(0, 0);
            LineItemOffset thisItem = m_collection.GetLineItemOffsetForMediaItem(context);

            List<MediaExplorerItem> extendBy = m_collection.GetMediaItemsBetween(m_pinnedSelectionClick, thisItem);

            foreach (MediaExplorerItem extendByItem in extendBy)
            {
                SelectExplorerItem(extendByItem);
            }

            m_collection.DebugVerifySelectedItems(m_itemsSelected);
        }

        /*----------------------------------------------------------------------------
            %%Function: _AddSelectPanel
            %%Qualified: Thetacat.UI.MediaExplorer._AddSelectPanel

            This is a control click. It toggles the item in the current collection
        ----------------------------------------------------------------------------*/
        private void _AddSelectPanel(MediaExplorerItem? context)
        {
            if (m_collection == null)
                return;

            m_collection.DebugVerifySelectedItems(m_itemsSelected);

            if (context == null)
            {
                ClearSelectedItems();
                m_pinnedSelectionClick = null;
                m_pinnedSelectionClickSelect = false;
                m_collection.DebugVerifySelectedItems(m_itemsSelected);
                return;
            }

            // remember whether the last control+click selected or deselected the item
            m_pinnedSelectionClickSelect = ToggleSelectExplorerItem(context);
            m_pinnedSelectionClick = m_collection.GetLineItemOffsetForMediaItem(context);
            m_collection.DebugVerifySelectedItems(m_itemsSelected);
        }

        private void _StickyExtendSelectPanel(MediaExplorerItem? context)
        {
            if (m_collection == null)
                return;

            m_collection.DebugVerifySelectedItems(m_itemsSelected);
            if (context == null)
            {
                ClearSelectedItems();
                m_pinnedSelectionClick = null;
                m_pinnedSelectionClickSelect = false;
                m_collection.DebugVerifySelectedItems(m_itemsSelected);
                return;
            }

            // if there's no pinned selection, then assume from start, selecting
            if (m_pinnedSelectionClick == null)
            {
                m_pinnedSelectionClick = new LineItemOffset(0, 0);
                m_pinnedSelectionClickSelect = true;
            }

            LineItemOffset thisItem = m_collection.GetLineItemOffsetForMediaItem(context);

            List<MediaExplorerItem> extendBy = m_collection.GetMediaItemsBetween(m_pinnedSelectionClick, thisItem);

            foreach (MediaExplorerItem extendByItem in extendBy)
            {
                if (m_pinnedSelectionClickSelect)
                    SelectExplorerItem(extendByItem);
                else
                    UnselectExplorerItem(extendByItem);
            }

            m_collection.DebugVerifySelectedItems(m_itemsSelected);
        }

        private void _ShowHideMetatagPanel(MediaExplorerItem? context)
        {
            if (m_applyMetatagPanel == null)
            {
                m_applyMetatagPanel = new ApplyMetatag();
            }

            if (m_applyMetatagPanel.IsVisible)
            {
                m_applyMetatagPanel.Hide();
            }
            else
            {
                List<MediaItem> selected = new();

                if (context != null)
                {
                    selected.Add(MainWindow._AppState.Catalog.Media.Items[context.MediaId]);
                }

                m_applyMetatagPanel.UpdateForMedia(selected, MainWindow._AppState.MetatagSchema);

                m_applyMetatagPanel.Show();
            }
        }

        private void ItemMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // figure out either the current item, or the selected items

            if (e.OriginalSource is System.Windows.Controls.Image { Parent: StackPanel { DataContext: MediaExplorerItem item } })
            {
                Model.ExplorerContextMenu.AppliedTags.Clear();

                if (MainWindow._AppState.Catalog.Media.Items.TryGetValue(item.MediaId, out MediaItem? mediaItem))
                {
                    foreach (KeyValuePair<Guid, MediaTag> tag in mediaItem.Tags)
                    {
                        if (MetatagStandards.GetStandardFromStandardTag(tag.Value.Metatag.Standard) != MetatagStandards.Standard.User)
                            continue;

                        Model.ExplorerContextMenu.AppliedTags.Add(
                            new ExplorerMenuTag()
                            {
                                MediaTagId = tag.Value.Metatag.ID,
                                TagName = tag.Value.Metatag.Description
                            });
                    }
                }

                MainWindow.LogForApp(EventType.Information, $"hit test result: {item.TileSrc}, {item.TileLabel}");
            }

            if (Model.ExplorerContextMenu.RecentTagVectorClock != m_metatagMRU.VectorClock)
            {
                Model.ExplorerContextMenu.AdvertisedTags.Clear();
                foreach (Metatag tag in m_metatagMRU.RecentTags)
                {
                    Model.ExplorerContextMenu.AdvertisedTags.Add(
                        new ExplorerMenuTag()
                        {
                            MediaTagId = tag.ID,
                            TagName = tag.Description
                        });
                }
            }
        }
    }
}
