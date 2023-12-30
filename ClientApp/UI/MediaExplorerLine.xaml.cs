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
        public static readonly DependencyProperty LineItemsProperty =
            DependencyProperty.Register(
                name: "LineItems",
                propertyType: typeof(MediaExplorerLineModel),
                ownerType: typeof(MediaExplorerLine),
                new PropertyMetadata(default(MediaExplorerLineModel)));

        public MediaExplorerLine()
        {
            InitializeComponent();
        }

        public MediaExplorerLineModel LineItems
        {
            get => (MediaExplorerLineModel) GetValue(LineItemsProperty);
            set => SetValue(LineItemsProperty, value);
        }
    }
}
