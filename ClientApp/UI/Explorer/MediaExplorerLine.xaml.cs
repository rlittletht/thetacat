using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Thetacat.Logging;
using Thetacat.UI.Explorer;

namespace Thetacat.UI
{

    public class MediaExplorerItemTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
        {
            FrameworkElement? element = container as FrameworkElement;

            if (element != null && item != null && item is MediaExplorerItem myItem)
            {
                if (myItem.Selected)
                    return element.FindResource("SelectedTemplate") as DataTemplate;
                else
                    return element.FindResource("NonSelectedTemplate") as DataTemplate;
            }

            return null; // or provide a default template
        }
    }

    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }

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

        private void RemoveTagFromItem(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is ExplorerMenuTag tag)
            {
                //tag.
            }
        }
    }
}
