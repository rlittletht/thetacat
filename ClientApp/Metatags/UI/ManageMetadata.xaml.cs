using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using Emgu.CV.Aruco;
using Thetacat.Explorer.Commands;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.Types;
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

        MetatagsTree.Initialize(App.State.MetatagSchema.WorkingTree.Children, App.State.MetatagSchema.SchemaVersionWorking);
    }


    private void LoadMetatags(object sender, RoutedEventArgs e)
    {
        App.State.RefreshMetatagSchema();
        MetatagsTree.Initialize(App.State.MetatagSchema.WorkingTree.Children, App.State.MetatagSchema.SchemaVersionWorking);
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

        Dictionary<Guid, bool> filter = new();

        Guid metatagId = Guid.Parse(item.ID);

        filter.Add(metatagId, true);

        List<MediaItem> matchingItems = App.State.Catalog.GetFilteredMediaItems(filter);

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

        //App.State.MetatagSchema.
    }
}