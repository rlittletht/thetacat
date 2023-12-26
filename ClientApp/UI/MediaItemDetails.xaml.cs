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

namespace Thetacat.UI;

/// <summary>
/// Interaction logic for MediaItemDetails.xaml
/// </summary>
public partial class MediaItemDetails : Window
{
    private readonly MediaItemData m_mediaItemData;

    public MediaItemDetails(MediaItem item)
    {
        InitializeComponent();
        m_mediaItemData = item.Data;
        DataContext = m_mediaItemData;
    }
}