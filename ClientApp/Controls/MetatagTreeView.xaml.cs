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
using Thetacat.Standards;
using Thetacat.Types;

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

    /*----------------------------------------------------------------------------
        %%Function: Initialize
        %%Qualified: Thetacat.Controls.MetatagTreeView.Initialize

        If you don't specify a standardRoot, then you will get all the root
        items, and they will automatically update if you add new root items.

        If you specify a standardRoot, then the root items will not automatically
        update (which is intuitively obvious since you will only have the one
        matched root to start with...)
    ----------------------------------------------------------------------------*/
    public void Initialize(MetatagSchema schema, MetatagStandards.Standard? standardRoot = null)
    {
        m_metatagTree = new MetatagTree(schema.Metatags);
        m_metatagSchemaVersion = schema.SchemaVersion;

        if (standardRoot != null)
        {
            IMetatagTreeItem? itemMatch = m_metatagTree.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(MetatagStandards.GetStandardsTagFromStandard(standardRoot.Value)), 1);

            if (itemMatch != null)
                SetItems(itemMatch.Children, schema.SchemaVersion);
        }
        else
        {
            SetItems(m_metatagTree.Children, schema.SchemaVersion);
        }
    }
}