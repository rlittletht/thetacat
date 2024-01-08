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
using Thetacat.ServiceClient.LocalService;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.UI.Explorer;
using Thetacat.UI.Explorer.Commands;
using Thetacat.Util;
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
            App.State.Settings.ExplorerItemSize = itemSize;
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
                item.FAddOrUpdateMediaTag(mediaTag);
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

                        m_metatagMRU.TouchMetatag(mediaTag.Metatag);
                    }
                }
            }

            UpdateMetatagPanelIfNecessary(m_selector.SelectedItems);
        }

        private void _ShowHideMetatagPanel(MediaExplorerItem? context)
        {
            if (m_applyMetatagPanel == null)
            {
                m_applyMetatagPanel = new ApplyMetatag(ApplySyncMetatags);
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
