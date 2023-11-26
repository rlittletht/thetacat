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
using Thetacat.Model;

namespace Thetacat.Controls;

/// <summary>
/// Interaction logic for MetatagTreeView.xaml
/// </summary>
public partial class MetatagTreeView : UserControl
{
    public MetatagTreeView()
    {
        InitializeComponent();
    }

    public void SetItems(ObservableCollection<IMetatagTreeItem> items)
    {
        Tree.ItemsSource = items;
    }

    private MetatagTree? m_metatagTree;

    public MetatagTree Model
    {
        get
        {
            if (m_metatagTree == null) 
                throw new Exception("not initialized");
            return m_metatagTree;
        }
    }

    public void Initialize(List<Metatag> tags)
    {
        m_metatagTree = new MetatagTree(tags);
        SetItems(m_metatagTree.Children);
    }
}