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
using Thetacat.Model;
using Thetacat.Types;
using Thetacat.UI.ProgressReporting;

namespace Thetacat.Explorer.UI
{
    /// <summary>
    /// Interaction logic for StackExplorer.xaml
    /// </summary>
    public partial class StackExplorer : Window
    {
        private StackExplorerModel m_model = new();
        private MediaStack? m_stack;
        private List<MediaItem> m_stackItems = new();

        public StackExplorerModel Model => m_model;

        public StackExplorer()
        {
            InitializeComponent();
            App.State.RegisterWindowPlace(this, "StackExplorer");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (m_stack == null)
                return;

            m_model.ExplorerCollection.AdjustPanelItemWidth(m_model.PanelItemWidth);
            m_model.ExplorerCollection.AdjustPanelItemHeight(m_model.PanelItemHeight);
            m_model.ExplorerCollection.AdjustExplorerWidth(Explorer.ExplorerBox.ActualWidth);
            m_model.ExplorerCollection.AdjustExplorerHeight(Explorer.ExplorerBox.ActualHeight);
            m_model.ExplorerCollection.UpdateItemsPerLine();
            Explorer.SetExplorerItemSize(ExplorerItemSize.Large);

            foreach (MediaStackItem stackItem in m_stack.Items)
            {
                MediaItem item = App.State.Catalog.GetMediaFromId(stackItem.MediaId);
                m_stackItems.Add(item);
            }

            m_model.ExplorerCollection.BuildTimelineForMediaCollection(m_stackItems);

        }


        void OnClosing(object sender, EventArgs e)
        {
        }

        public static StackExplorer ShowStackExplorer(MediaStack stack)
        {
            StackExplorer explorer = new();

            explorer.m_stack = stack;

            explorer.m_model.ExplorerCollection.ResetTimeline();
            explorer.m_model.ExplorerCollection.TimelineOrder = TimelineOrder.StackOrder;
            explorer.m_model.ExplorerCollection.StackForOrdering = stack;
            explorer.Explorer.ResetContent(explorer.m_model.ExplorerCollection);

            explorer.Show();

            return explorer;
        }
    }
}
