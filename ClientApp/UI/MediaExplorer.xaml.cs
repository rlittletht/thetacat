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
using Image = System.Drawing.Image;

namespace Thetacat.UI
{
    /// <summary>
    /// Interaction logic for MediaExplorer.xaml
    /// </summary>
    public partial class MediaExplorer : UserControl
    {
        public ObservableCollection<MediaExplorerItem> ExplorerItems = new();
        public ObservableCollection<MediaExplorerLineModel> ExplorerLines = new ();

        private MediaExplorerCollection? m_collection;

        public MediaExplorer()
        {
            InitializeComponent();
//            ExplorerBox.ItemsSource = ExplorerItems;
            ExplorerBox.ItemsSource = ExplorerLines;
        }

        public void ResetContent(MediaExplorerCollection collection)
        {
            m_collection = collection;
            m_collection.AdjustExplorerWidth(ActualWidth);
            ExplorerBox.ItemsSource = collection.ExplorerLines;
        }

        private void OnExplorerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // notify the collection of the change
            if (e.WidthChanged)
                m_collection?.AdjustExplorerWidth(e.NewSize.Width);
        }

        private void OnExplorerLoaded(object sender, RoutedEventArgs e)
        {
            m_collection?.AdjustExplorerWidth(ActualWidth);
        }

#if old
        public void ResetContent(IEnumerable<MediaExplorerItem> newItems)
        {
            int c = 0;
            int line = 0;
            int rawCount = 0;

            ExplorerLines.Clear();

            MediaExplorerLineModel? currentLine = null;

            foreach (MediaExplorerItem item in newItems)
            {
                MainWindow.LogForApp(EventType.Information, $"populating item: {item.TileLabel}");
                if (c == 0)
                {
                    line++;
                    currentLine = new MediaExplorerLineModel();
                    currentLine.TestName = $"line {line}";
                    ExplorerLines.Add(currentLine);
                }

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
                item.TileLabel = $"{rawCount++}: {item.m_tileLabel}";
                ExplorerItems.Add(item);
                Debug.Assert(currentLine != null, nameof(currentLine) + " != null");
                currentLine.Items.Add(item);
                c = (c + 1) % 4;
//                ExplorerItems.Add(item);
            }
        }
#endif // old
    }
}
