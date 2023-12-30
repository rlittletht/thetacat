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

namespace Thetacat.UI
{
    /// <summary>
    /// Interaction logic for MediaExplorerLine.xaml
    /// </summary>
    public partial class MediaExplorerLine : UserControl
    {
        public static readonly DependencyProperty TestItemsProperty =
            DependencyProperty.Register(
                name: "TestItems",
                propertyType: typeof(ObservableCollection<MediaExplorerItem>),
                ownerType: typeof(MediaExplorerLine),
                new PropertyMetadata(default(ObservableCollection<MediaExplorerItem>)));

        public MediaExplorerLine()
        {
            InitializeComponent();
        }

        public ObservableCollection<MediaExplorerItem> TestItems
        {
            get => (ObservableCollection<MediaExplorerItem>) GetValue(TestItemsProperty);
            set => SetValue(TestItemsProperty, value);
        }

//        public void SetItems(IEnumerable<MediaExplorerItem> items)
//        {
//            foreach (MediaExplorerItem item in items)
//            {
//                m_items.Add(item);
//            }
//        }
    }
}
