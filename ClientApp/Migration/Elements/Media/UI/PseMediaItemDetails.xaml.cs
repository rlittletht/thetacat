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

namespace Thetacat.Migration.Elements.Media.UI
{
    /// <summary>
    /// Interaction logic for PseMediaItemDetails.xaml
    /// </summary>
    public partial class PseMediaItemDetails : Window
    {
        private readonly IPseMediaItem m_mediaItem;

        public PseMediaItemDetails(IPseMediaItem pseMediaItem)
        {
            InitializeComponent();
            m_mediaItem = pseMediaItem;
            DataContext = m_mediaItem;
        }
    }
}
