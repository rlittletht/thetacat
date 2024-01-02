using System;
using System.Collections.Generic;
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
using Thetacat.Migration.Elements.Media;
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
        MainWindow._AppState.RegisterWindowPlace(this, "mediaItem-details");
    }
}