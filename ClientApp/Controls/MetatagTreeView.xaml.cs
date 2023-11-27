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
        DataContext = this;
    }

    public void SetItems(ObservableCollection<IMetatagTreeItem> items, int schemaVersion)
    {
        Tree.ItemsSource = items;
        m_metatagSchemaVersion = schemaVersion;
    }

    private MetatagTree? m_metatagTree;
    private int m_metatagSchemaVersion = 0;

    public MetatagTree Model
    {
        get
        {
            if (m_metatagTree == null) 
                throw new Exception("not initialized");
            return m_metatagTree;
        }
    }

    public int SchemaVersion => m_metatagSchemaVersion;

    public void Initialize(MetatagSchema schema)
    {
        m_metatagTree = new MetatagTree(schema.Metatags);
        m_metatagSchemaVersion = schema.SchemaVersion;

        SetItems(m_metatagTree.Children, schema.SchemaVersion);
    }
}