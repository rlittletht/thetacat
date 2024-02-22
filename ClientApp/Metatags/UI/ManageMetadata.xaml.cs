using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows;
using System.Windows.Forms;
using Emgu.CV.Aruco;
using TCore.PostfixText;
using Thetacat.Explorer.Commands;
using Thetacat.Filtering;
using Thetacat.Filtering.UI;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Standards;
using Thetacat.Types;
using Expression = TCore.PostfixText.Expression;
using MessageBox = System.Windows.MessageBox;

namespace Thetacat.Metatags;

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
/// Interaction logic for ManageMetadata.xaml
/// </summary>
public partial class ManageMetadata : Window
{
    public ManageMetadataModel Model { get; set; } = new();

    public ManageMetadata()
    {
        // make sure you set the commands up BEFORE calling initialize component, otherwise it won't hook
        // them up
        Model.DeleteMetatagCommand = new DeleteMetatagCommand(DeleteMetatag);

        InitializeComponent();
        App.State.RegisterWindowPlace(this, "ManageMetadata");
        DataContext = Model;

        MetatagsTree.Initialize(App.State.MetatagSchema.WorkingTree.Children, App.State.MetatagSchema.SchemaVersionWorking, MetatagStandards.Standard.User);
        InitializeAvailableParents();
    }


    private void LoadMetatags(object sender, RoutedEventArgs e)
    {
        App.State.RefreshMetatagSchema(App.State.ActiveProfile.CatalogID);
        MetatagsTree.Initialize(App.State.MetatagSchema.WorkingTree.Children, App.State.MetatagSchema.SchemaVersionWorking, MetatagStandards.Standard.User);
        m_metatagLineageMap = null;
        InitializeAvailableParents();
    }

    MessageBoxResult ConfirmDelete(int count, IMetatagTreeItem item)
    {
        if (count == 0)
        {
            return MessageBox.Show(
                $"No media uses this metatag. Are you sure you want to delete {item.Name} ({item.Description})?",
                "Delete metatag",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
        }
        else
        {
            return MessageBox.Show(
                $"WARNING: {count} media items currently use this metatag! Deleting this tag will remove it from all of these media items!\n\nAre you sure you want to delete {item.Name} ({item.Description})?",
                "Delete metatag",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation);
        }

    }

    private void DeleteMetatag(IMetatagTreeItem? item)
    {
        if (item == null)
            return;

        FilterDefinition filterToThisTag = new FilterDefinition();
        Guid metatagId = Guid.Parse(item.ID);

        filterToThisTag.Expression.AddExpression(
            Expression.Create(
                Value.CreateForField(metatagId.ToString("B")),
                    Value.Create("$true"),
                    new ComparisonOperator(ComparisonOperator.Op.Eq)));


        List<MediaItem> matchingItems = App.State.Catalog.GetFilteredMediaItems(filterToThisTag);

        IMetatagTreeItem? treeItem = (App.State.MetatagSchema.WorkingTree.FindMatchingChild(MetatagTreeItemMatcher.CreateIdMatch(metatagId), -1));

        if (treeItem == null)
            throw new CatExceptionInternalFailure($"couldn't find tree item for metatag in schema: {item.Name}");

        if (treeItem.Children.Count > 0)
        {
            MessageBox.Show(
                $"Cannot delete a metatag that has child metatags. If you really want to do this, you have to delete each child first",
                "Delete metatag",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        if (ConfirmDelete(matchingItems.Count, item) != MessageBoxResult.Yes)
            return;

        foreach (MediaItem mediaItem in matchingItems)
        {
            mediaItem.FRemoveMediaTag(metatagId);
        }

        if (App.State.MetatagSchema.FRemoveMetatag(metatagId))
        {
            MetatagsTree.Initialize(App.State.MetatagSchema.WorkingTree.Children, App.State.MetatagSchema.SchemaVersionWorking);
        }
    }

    private void SelectParentMetatag(Guid parentId)
    {
        foreach (FilterModelMetatagItem parent in Model.AvailableParents)
        {
            if (parent.Metatag.ID == parentId)
            {
                ParentTag.SelectedItem = parent;
                break;
            }
        }
    }

    private void DoSelectedParentChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is MetatagTreeItem newItem)
        {
            ParentTag.Text = newItem.Name;
            SelectParentMetatag(newItem.ItemId);
        }

        ParentPickerPopup.IsOpen = false;
    }


    private void DoSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is MetatagTreeItem newItem)
        {
            Model.SelectedMetatag = new ManageMetadataMetatag(App.State.MetatagSchema.GetMetatagFromId(newItem.ItemId));
            Model.MetatagBase = new ManageMetadataMetatag(Model.SelectedMetatag);

            if (Model.SelectedMetatag.Parent == null)
            {
                Model.CurrentParent = null;
            }
            else
            {
                SelectParentMetatag(Model.SelectedMetatag.Parent.Value);
            }

            Model.SelectedTreeItem = newItem;
        }
    }

    private void SelectParent(object sender, RoutedEventArgs e)
    {
        ParentPickerPopup.IsOpen = true;
    }

    void InitializeAvailableParents()
    {
        if (m_metatagLineageMap == null)
            m_metatagLineageMap = EditFilter.BuildLineageMap(MetatagStandards.Standard.User);

        IComparer<KeyValuePair<Guid, string>> comparer =
            Comparer<KeyValuePair<Guid, string>>.Create((x, y) => String.Compare(x.Value, y.Value, StringComparison.Ordinal));
        ImmutableSortedSet<KeyValuePair<Guid, string>> sorted = m_metatagLineageMap.ToImmutableSortedSet(comparer);

        foreach (KeyValuePair<Guid, string> item in sorted)
        {
            Model.AvailableParents.Add(new FilterModelMetatagItem(App.State.MetatagSchema.GetMetatagFromId(item.Key)!, item.Value));
        }

        ParentMetatagsTree.Initialize(
            App.State.MetatagSchema.WorkingTree.Children, 
            App.State.MetatagSchema.SchemaVersionWorking,
            MetatagStandards.Standard.User);
    }

    private Dictionary<Guid, string>? m_metatagLineageMap;

    private void CreateNew(object sender, RoutedEventArgs e)
    {
        if (Model.SelectedMetatag != null && Model.MetatagBase != null)
        {
            if (!Model.MetatagBase.CompareTo(Model.SelectedMetatag))
            {
                if (MessageBox.Show(
                        "If you create a new tag, you will lose any changes you have made to this tag. Continue",
                        "Create new tag",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question)
                    != MessageBoxResult.Yes)
                {
                    return;
                }
            }
        }

        Model.SelectedMetatag = new ManageMetadataMetatag();
        Model.MetatagBase = new ManageMetadataMetatag(Model.SelectedMetatag);
    }

    private void DoSave(object sender, RoutedEventArgs e)
    {
        if (Model.SelectedMetatag == null)
        {
            MessageBox.Show("You must first select and define a metatag");
            return;
        }

        // find the metatag and update it
        Metatag? metatag = App.State.MetatagSchema.GetMetatagFromId(Model.SelectedMetatag.ID);

        bool needNewTree = false;

        if (metatag == null)
        {
            metatag = Metatag.Create(
                Model.CurrentParent?.Metatag.ID,
                Model.SelectedMetatag.Name,
                Model.SelectedMetatag.Description,
                MetatagStandards.Standard.User,
                Model.SelectedMetatag.ID);

            App.State.MetatagSchema.AddMetatag(metatag);
            needNewTree = true;
        }
        else
        {
            App.State.MetatagSchema.NotifyChanging();

            metatag.Name = Model.SelectedMetatag.Name;
            metatag.Description = Model.SelectedMetatag.Description;
            needNewTree = metatag.Parent != Model.CurrentParent?.Metatag.ID;
            metatag.Parent = Model.CurrentParent?.Metatag.ID;
        }

        if (needNewTree)
        {
            App.State.MetatagSchema.RebuildWorkingTree();
            MetatagsTree.Initialize(App.State.MetatagSchema.WorkingTree.Children, App.State.MetatagSchema.SchemaVersionWorking, MetatagStandards.Standard.User);
            m_metatagLineageMap = null;
            InitializeAvailableParents();
        }
    }

}