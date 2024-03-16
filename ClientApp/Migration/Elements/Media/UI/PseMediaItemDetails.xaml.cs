using System.Windows;

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
