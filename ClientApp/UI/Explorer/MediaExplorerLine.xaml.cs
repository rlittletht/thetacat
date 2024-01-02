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
using Thetacat.UI.Explorer;

namespace Thetacat.UI
{
    /// <summary>
    /// Interaction logic for MediaExplorerLine.xaml
    /// </summary>
    public partial class MediaExplorerLine : UserControl
    {
        public static readonly DependencyProperty LineItemsProperty =
            DependencyProperty.Register(
                name: nameof(LineItems),
                propertyType: typeof(MediaExplorerLineModel),
                ownerType: typeof(MediaExplorerLine),
                new PropertyMetadata(default(MediaExplorerLineModel)));

        public static readonly DependencyProperty ImageContainerWidthProperty =
            DependencyProperty.Register(
                name: nameof(ImageContainerWidth),
                propertyType: typeof(double),
                ownerType: typeof(MediaExplorerLine),
                new PropertyMetadata(default(double)));

        public static readonly DependencyProperty ImageWidthProperty =
            DependencyProperty.Register(
                name: nameof(ImageWidth),
                propertyType: typeof(double),
                ownerType: typeof(MediaExplorerLine),
                new PropertyMetadata(default(double)));

        public static readonly DependencyProperty ImageHeightProperty =
            DependencyProperty.Register(
                name: nameof(ImageHeight),
                propertyType: typeof(double),
                ownerType: typeof(MediaExplorerLine),
                new PropertyMetadata(default(double)));

        public MediaExplorerLine()
        {
            InitializeComponent();
        }

        public MediaExplorerLineModel LineItems
        {
            get => (MediaExplorerLineModel) GetValue(LineItemsProperty);
            set => SetValue(LineItemsProperty, value);
        }

        public double ImageContainerWidth
        {
            get => (double)GetValue(ImageContainerWidthProperty);
            set => SetValue(ImageContainerWidthProperty, value);
        }

        public double ImageWidth
        {
            get => (double)GetValue(ImageWidthProperty);
            set => SetValue(ImageWidthProperty, value);
        }

        public double ImageHeight
        {
            get => (double)GetValue(ImageHeightProperty);
            set => SetValue(ImageHeightProperty, value);
        }
    }
}
