using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ObjectiveC;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Thetacat.Metatags;
using Thetacat.Standards;
using Thetacat.Types;

namespace Thetacat.UI.Controls;

/// <summary>
/// Interaction logic for BackedTreeView.xaml
/// </summary>
public partial class BackedTreeView: UserControl
{
    public delegate void SelectedItemChangedDelegate(object sender, RoutedPropertyChangedEventArgs<Object> e);

    public static readonly DependencyProperty SelectedItemChangedProperty =
        DependencyProperty.Register(
            name: nameof(SelectedItemChanged),
            propertyType: typeof(SelectedItemChangedDelegate),
            ownerType: typeof(BackedTreeView),
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
            ownerType: typeof(BackedTreeView),
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
            ownerType: typeof(BackedTreeView),
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
            ownerType: typeof(BackedTreeView),
            new PropertyMetadata(default(bool)));

    public bool ShowSchemaVersion
    {
        get => (bool)GetValue(ShowSchemaVersionProperty);
        set => SetValue(ShowSchemaVersionProperty, value);
    }


    public BackedTreeViewModel Model = new BackedTreeViewModel();

    public BackedTreeView()
    {
        InitializeComponent();
        DataContext = Model;
        Tree.ItemsSource = Model.Items;
    }

    public void Clear()
    {
        Model.Items.Clear();
    }

    public void SetItems(
        IEnumerable<IBackingTreeItem>? items,
        GetTreeItemStateDelegate? getStateDelegate = null)
    {
        BackingTree.CloneAndSetCheckedItems(items, Model.Items, getStateDelegate);
    }

    public void InitializeFromExistingTree(
        IBackingTreeItem treeRoot,
        GetTreeItemStateDelegate? getStateDelegate = null)
    {
        SetItems(treeRoot.Children, getStateDelegate);
    }

    /*----------------------------------------------------------------------------
        %%Function: Initialize
        %%Qualified: Thetacat.Controls.BackedTreeView.Initialize
    ----------------------------------------------------------------------------*/
    public void Initialize(
        IEnumerable<IBackingTreeItem> roots,
        GetTreeItemStateDelegate? getStateDelegate = null)
    {
        SetItems(roots, getStateDelegate);
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