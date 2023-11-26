using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Thetacat.Metatags;

namespace Thetacat.Controls;

/// <summary>
/// Interaction logic for MetatagTree.xaml
/// </summary>
public partial class MetatagTree : UserControl
{
    public MetatagTree()
    {
        InitializeComponent();
    }

    public void SetItems(ObservableCollection<IMetatagTreeItem> items)
    {
        Tree.ItemsSource = items;
    }
}