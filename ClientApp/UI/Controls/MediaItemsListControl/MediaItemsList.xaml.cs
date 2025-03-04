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

namespace Thetacat.UI.Controls.MediaItemsListControl;

/// <summary>
/// Interaction logic for MediaItemsList.xaml
/// </summary>
public partial class MediaItemsList : UserControl
{
    public static readonly DependencyProperty CheckableProperty =
        DependencyProperty.Register(
            name: nameof(Items),
            propertyType: typeof(ObservableCollection<MediaItemsListItem>),
            ownerType: typeof(MediaItemsList),
            new PropertyMetadata(default(ObservableCollection<MediaItemsListItem>)));


    public ObservableCollection<MediaItemsListItem> Items
    {
        get => (ObservableCollection<MediaItemsListItem>)GetValue(CheckableProperty);
        set => SetValue(CheckableProperty, value);
    }

    public MediaItemsList()
    {
        InitializeComponent();
    }
}