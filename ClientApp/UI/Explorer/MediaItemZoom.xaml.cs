using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Shapes;
using Thetacat.Model;
using Thetacat.Model.ImageCaching;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.UI.Explorer
{
    /// <summary>
    /// Interaction logic for MediaItemZoom.xaml
    /// </summary>
    public partial class MediaItemZoom : Window
    {
        public delegate void OnZoomClosingDelegate(MediaItemZoom zoom);

        private readonly SortableListViewSupport m_sortableListViewSupport;
        private void SortType(object sender, RoutedEventArgs e) => m_sortableListViewSupport.Sort(sender as GridViewColumnHeader);

        private MediaItemZoomModel m_model = new();

        private void OnImageCacheUpdated(object? sender, ImageCacheUpdateEventArgs e)
        {
            ImageCache? cache = sender as ImageCache;

            if (cache == null)
                throw new CatExceptionInternalFailure("sender wasn't an image cache in OnImageCacheUpdated");

            if (cache.Items.TryGetValue(e.MediaId, out ImageCacheItem? cacheItem))
            {
                    m_model.Image = cacheItem?.Image;
            }
        }

        private void OnCloseReleaseImageCacheWatcher(object? sender, CancelEventArgs e)
        {
            App.State.ImageCache.ImageCacheUpdated -= OnImageCacheUpdated;
        }

        public MediaItemZoom(MediaItem item)
        {
            m_sortableListViewSupport = new SortableListViewSupport(MetadataListView);
            m_model.MediaItem = item;

            DataContext = m_model;

            App.State.RegisterWindowPlace(this, "mediaItem-details");

            ImageCacheItem? cacheItem = App.State.ImageCache.GetAnyExistingItem(item.ID);

            App.State.ImageCache.ImageCacheUpdated -= OnImageCacheUpdated;
            Closing += OnCloseReleaseImageCacheWatcher;

            if (cacheItem == null)
            {
                string? path = App.State.Cache.TryGetCachedFullPath(item.ID);

                if (path != null)
                    App.State.ImageCache.TryAddItem(item, path);

                App.State.ImageCache.ImageCacheUpdated += OnImageCacheUpdated;
            }

            m_model.Image = cacheItem?.Image;

            InitializeComponent();
        }

    }
}
