using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Image = System.Drawing.Image;

namespace Thetacat.UI
{
    /// <summary>
    /// Interaction logic for MediaExplorer.xaml
    /// </summary>
    public partial class MediaExplorer : UserControl
    {
        public ObservableCollection<MediaExplorerItem> ExplorerItems = new();

        public MediaExplorer()
        {
            InitializeComponent();
            ExplorerBox.ItemsSource = ExplorerItems;
        }

        public void ResetContent(IEnumerable<MediaExplorerItem> newItems)
        {
            int c = 0;

            ExplorerItems.Clear();
            foreach (MediaExplorerItem item in newItems)
            {
//                if (c++ < 10)
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.DecodePixelWidth = 92;
                    image.DecodePixelHeight = 92;
                    image.UriSource = new Uri(item.TileSrc);
                    image.EndInit();
                    item.TileImage = image;
                }

                ExplorerItems.Add(item);
            }
        }
    }
}
