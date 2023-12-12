using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Thetacat.Metatags;
using Thetacat.Model.Metatags;
using Thetacat.Standards;
using Thetacat.Types;

namespace Thetacat.Controls;

/// <summary>
/// Interaction logic for MetatagTreeView.xaml
/// </summary>
public partial class MetatagTreeView : UserControl, INotifyPropertyChanged
{
    public MetatagTreeView()
    {
        InitializeComponent();
        DataContext = this;
    }

    public void SetItems(ObservableCollection<IMetatagTreeItem> items, int schemaVersion)
    {
        Tree.ItemsSource = items;
        SchemaVersion = schemaVersion;
    }

    private MetatagTree? m_metatagTree;
    private int m_schemaVersion;

    public MetatagTree Model
    {
        get
        {
            if (m_metatagTree == null) 
                throw new Exception("not initialized");
            return m_metatagTree;
        }
    }

    public int SchemaVersion
    {
        get => m_schemaVersion;
        set => SetField(ref m_schemaVersion, value);
    }

#if NOTUSED
    // if we ever decide to try to return the backing TreeItem for this control
    // we will have to consider that we are bound the children of the treeitem
    // of our root. this means that adds to that treeitem will be observable
    // and show up in the control. but if we build our own observable collection
    // as a virtual root, then that won't get auto updated. Hopefully we will
    // never need this code.
    private ObservableCollection<IMetatagTreeItem>? m_virtualRootMetatags;

    public ObservableCollection<IMetatagTreeItem> RootTreeItems
    {
        get
        {
            if (m_virtualRootMetatags != null)
                return m_virtualRootMetatags;
            if (m_metatagTree == null)
                throw new Exception("not initialized");
            return m_metatagTree.Children;
        }
    }
#endif
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
        m_metatagTree = schema.WorkingTree;

        SchemaVersion = schema.SchemaVersionWorking;

        if (standardRoot != null)
        {
            IMetatagTreeItem? itemMatch = m_metatagTree.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(MetatagStandards.GetStandardsTagFromStandard(standardRoot.Value)), 1);

            if (itemMatch != null)
            {
#if NOTUSED
                m_virtualRootMetatags = new ObservableCollection<IMetatagTreeItem>();
                foreach (IMetatagTreeItem item in itemMatch.Children)
                {
                    m_virtualRootMetatags.Add(item);
                }
#endif
                SetItems(itemMatch.Children, schema.SchemaVersionWorking);
            }
        }
        else
        {
            SetItems(m_metatagTree.Children, schema.SchemaVersionWorking);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}