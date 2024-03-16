using System.Windows;
using System.Windows.Controls;
using Thetacat.Model;
using Thetacat.Util;

namespace Thetacat.UI;

/// <summary>
/// Interaction logic for MediaItemDetails.xaml
/// </summary>
public partial class MediaItemDetails : Window
{
    private readonly SortableListViewSupport m_sortableListViewSupport;
    private void SortType(object sender, RoutedEventArgs e) => m_sortableListViewSupport.Sort(sender as GridViewColumnHeader);

    private readonly MediaItem? m_mediaItem;

    public MediaItemDetails(MediaItem item)
    {
        InitializeComponent();
        m_sortableListViewSupport = new SortableListViewSupport(MetadataListView);
        m_mediaItem = item;
        DataContext = m_mediaItem;
        App.State.RegisterWindowPlace(this, "mediaItem-details");
    }
}