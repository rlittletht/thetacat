using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ObjectiveC;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Thetacat.Metatags;
using Thetacat.Metatags.Model;
using Thetacat.Standards;

namespace Thetacat.UI.Controls;

/// <summary>
/// Interaction logic for MetatagTreeView.xaml
/// </summary>
public partial class MetatagTreeView : UserControl
{
    public delegate void SelectedItemChangedDelegate(object sender, RoutedPropertyChangedEventArgs<Object> e);

    public static readonly DependencyProperty SelectedItemChangedProperty =
        DependencyProperty.Register(
            name: nameof(SelectedItemChanged),
            propertyType: typeof(SelectedItemChangedDelegate),
            ownerType: typeof(MetatagTreeView),
            new PropertyMetadata(default(SelectedItemChangedDelegate)));

    public SelectedItemChangedDelegate SelectedItemChanged
    {
        get => (SelectedItemChangedDelegate)GetValue(SelectedItemChangedProperty);
        set => SetValue(SelectedItemChangedProperty, value);
    }

    public static readonly DependencyProperty ItemContextMenuProperty =
        DependencyProperty.Register(
            name: nameof(ItemContextMenu),
            propertyType: typeof(ContextMenu),
            ownerType: typeof(MetatagTreeView),
            new PropertyMetadata(default(ContextMenu)));

    public ContextMenu ItemContextMenu
    {
        get => (ContextMenu)GetValue(ItemContextMenuProperty);
        set => SetValue(CheckableProperty, value);
    }

    public static readonly DependencyProperty CheckableProperty =
        DependencyProperty.Register(
            name: nameof(Checkable),
            propertyType: typeof(bool),
            ownerType: typeof(MetatagTreeView),
            new PropertyMetadata(default(bool)));

    public bool Checkable
    {
        get => (bool)GetValue(CheckableProperty);
        set => SetValue(CheckableProperty, value);
    }

    public static readonly DependencyProperty ShowSchemaVersionProperty =
        DependencyProperty.Register(
            name: nameof(ShowSchemaVersion),
            propertyType: typeof(bool),
            ownerType: typeof(MetatagTreeView),
            new PropertyMetadata(default(bool)));

    public bool ShowSchemaVersion
    {
        get => (bool)GetValue(ShowSchemaVersionProperty);
        set => SetValue(ShowSchemaVersionProperty, value);
    }


    public MetatagTreeViewModel Model = new MetatagTreeViewModel();

    public MetatagTreeView()
    {
        InitializeComponent();
        DataContext = Model;
        Tree.ItemsSource = Model.Items;
    }

    public void SetItems(
        IEnumerable<IMetatagTreeItem>? items,
        int schemaVersion,
        Dictionary<string, bool?>? initialCheckboxState = null)
    {
        MetatagTree.CloneAndSetCheckedItems(items, Model.Items, initialCheckboxState);
        Model.SchemaVersion = schemaVersion;
    }

    public void AddItems(
        IEnumerable<IMetatagTreeItem>? items,
        Dictionary<string, bool?>? initialCheckboxState = null)
    {
        MetatagTree.CloneAndAddCheckedItems(items, Model.Items, initialCheckboxState);
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

    public void InitializeFromExistingTree(
        IMetatagTreeItem treeRoot,
        Dictionary<string, bool?>? initialCheckboxState = null)
    {
        Model.SchemaVersion = 0;

        SetItems(treeRoot.Children, 0, initialCheckboxState);
    }

    /*----------------------------------------------------------------------------
        %%Function: Initialize
        %%Qualified: Thetacat.Controls.MetatagTreeView.Initialize

        If you don't specify a standardRoot, then you will get all the root
        items, and they will automatically update if you add new root items.

        If you specify a standardRoot, then the root items will not automatically
        update (which is intuitively obvious since you will only have the one
        matched root to start with...)
    ----------------------------------------------------------------------------*/
    public void Initialize(
        IEnumerable<IMetatagTreeItem> roots,
        int schemaVersion,
        MetatagStandards.Standard? standardRoot = null,
        Dictionary<string, bool?>? initialCheckboxState = null)
    {
        Model.SchemaVersion = schemaVersion;

        if (standardRoot != null)
        {
            IMetatagTreeItem? itemMatch = MetatagTree.FindMatchingChild(
                roots,
                MetatagTreeItemMatcher.CreateNameMatch(MetatagStandards.GetStandardsTagFromStandard(standardRoot.Value)),
                1);

#if NOTUSED
                m_virtualRootMetatags = new ObservableCollection<IMetatagTreeItem>();
                foreach (IMetatagTreeItem item in itemMatch.Children)
                {
                    m_virtualRootMetatags.Add(item);
                }
#endif
            SetItems(itemMatch?.Children, schemaVersion, initialCheckboxState);
        }
        else
        {
            SetItems(roots, schemaVersion, initialCheckboxState);
        }
    }

    public void AddSpecificTag(
        IEnumerable<IMetatagTreeItem> roots,
        Metatag metatag, 
        Dictionary<string, bool?>? initialCheckboxState = null)
    {
        IMetatagTreeItem? itemMatch = MetatagTree.FindMatchingChild(
            roots,
            MetatagTreeItemMatcher.CreateIdMatch(metatag.ID),
            -1);

        // its ok for this to be null -- this will happen when we are trying to add a specific
        // tag that *might* be applied (like dontPushToCloud/isTrashItem). in this case, we will
        // go ahead and try to add the item, but the roots passed in will only be the actually
        // applied items, which means it won't be present if its not already applied.
        if (itemMatch != null)
            AddItems(new IMetatagTreeItem[] { itemMatch }, initialCheckboxState);
    }

    /*----------------------------------------------------------------------------
        %%Function: GetCheckedUncheckedAndIndeterminateItems
        %%Qualified: Thetacat.Controls.MetatagTreeView.GetCheckedUncheckedAndIndeterminateItems

        This will have an entry for everything in the tree. If its not in this
        dictionary, then assume its indeterminate (and shouldn't be changed)
    ----------------------------------------------------------------------------*/
    public Dictionary<string, bool?> GetCheckedUncheckedAndIndeterminateItems()
    {
        Dictionary<string, bool?> checkedUncheckedAndIndeterminedItems = new();
        List<string> containersMarked = new();

        foreach (IMetatagTreeItem item in Model.Items)
        {
            item.Preorder(
                null,
                (visiting, parent, depth) =>
                {
                    if (visiting.Children.Count > 0)
                    {
                        if (visiting.Checked is true)
                            containersMarked.Add(visiting.Name);
                        return;
                    }

                    checkedUncheckedAndIndeterminedItems.Add(visiting.ID, visiting.Checked);
                },
                0);
        }

        if (containersMarked.Count > 0)
        {
            MessageBox.Show(
                $"At least one container metatag was checked. This isn't supported. No tags applied or removed. Please uncheck: {string.Join(",", containersMarked)} and try again.");
            return new Dictionary<string, bool?>();
        }

        return checkedUncheckedAndIndeterminedItems;
    }

    /*----------------------------------------------------------------------------
        %%Function: GetCheckedAndUncheckedItems
        %%Qualified: Thetacat.Controls.MetatagTreeView.GetCheckedAndUncheckedItems

        This will have an entry for everything checked and unchecked in the tree

        If its not present in the return dictionary, then assume it is
        indeterminate
    ----------------------------------------------------------------------------*/
    public Dictionary<Guid, bool> GetCheckedAndUncheckedItems(bool okToMarkContainer)
    {
        Dictionary<Guid, bool> checkedAndUncheckedItems = new();
        List<string> containersMarked = new();

        foreach (IMetatagTreeItem item in Model.Items)
        {
            item.Preorder(
                null,
                (visiting, parent, depth) =>
                {
                    if (visiting.Children.Count > 0)
                    {
                        if (!okToMarkContainer && visiting.Checked is true)
                        {
                            containersMarked.Add(visiting.Name);
                            return;
                        }
                    }

                    if (visiting.Checked != null)
                        checkedAndUncheckedItems.Add(Guid.Parse(visiting.ID), visiting.Checked.Value);
                },
                0);
        }

        if (containersMarked.Count > 0)
        {
            MessageBox.Show(
                $"At least one container metatag was checked. This isn't supported. No tags applied or removed. Please uncheck: {string.Join(",", containersMarked)} and try again.");
            return new Dictionary<Guid, bool>();
        }

        return checkedAndUncheckedItems;
    }

    private void TreeViewItem_SelectItemOnRightMouseClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem item)
        {
            item.IsSelected = true;
            item.Focus();
            e.Handled = true;
        }
    }

    private void DoSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        SelectedItemChanged?.Invoke(sender, e);
    }
}
