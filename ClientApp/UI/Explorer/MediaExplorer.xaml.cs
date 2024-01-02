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
using Thetacat.UI.Explorer;
using Image = System.Drawing.Image;

namespace Thetacat.UI
{
    /// <summary>
    /// Interaction logic for MediaExplorer.xaml
    /// </summary>
    public partial class MediaExplorer : UserControl
    {
        private MediaExplorerCollection? m_collection;

        public MediaExplorerModel Model = new();
        private ExplorerItemSize m_itemSize = ExplorerItemSize.Medium;

        public MediaExplorer()
        {
            InitializeComponent();
            ExplorerBox.ItemsSource = Model.ExplorerLines;
            DataContext = Model;
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

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            MainWindow.LogForApp(EventType.Information, $"OnScrollChanged: Change: {e.VerticalChange}, Offset: {e.VerticalOffset}");
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
    }
}
