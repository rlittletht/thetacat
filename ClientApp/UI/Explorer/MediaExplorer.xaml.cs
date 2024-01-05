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
using Thetacat.Types;
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
        private readonly ItemSelector m_selector;

        public MediaExplorer()
        {
            InitializeComponent();
           
            DataContext = Model;
            m_selector = new ItemSelector(null, UpdateMetatagPanelIfNecessary);

            Model.ShowHideMetatagPanel = new ShowHideMetatagPanelCommand(_ShowHideMetatagPanel);
            Model.SelectPanel = new SelectPanelCommand(m_selector._SelectPanel);
            Model.ExtendSelectPanel = new SelectPanelCommand(m_selector._ExtendSelectPanel);
            Model.AddSelectPanel = new SelectPanelCommand(m_selector._AddSelectPanel);
            Model.AddExtendSelectPanel = new SelectPanelCommand(m_selector._StickyExtendSelectPanel);
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

        public void Close()
        {
            if (m_collection != null)
            {
                m_collection.Close();
                m_collection = null;
            }
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


        private void UpdateMetatagPanelIfNecessary(IEnumerable<MediaExplorerItem> selectedItems)
        {
            if (m_applyMetatagPanel != null)
            {
                List<MediaItem> mediaItems = new();
                ICatalog catalog = MainWindow._AppState.Catalog;

                foreach (MediaExplorerItem item in selectedItems)
                {
                    mediaItems.Add(catalog.Media.Items[item.MediaId]);
                }

                m_applyMetatagPanel.UpdateForMedia(mediaItems, MainWindow._AppState.MetatagSchema, m_selector.VectorClock);
            }

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
