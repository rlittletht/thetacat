using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Thetacat.Explorer.UI;
using Thetacat.Logging;
using Thetacat.Model;

namespace Thetacat.Explorer;

public class BindingProxy : Freezable
{
    protected override Freezable CreateInstanceCore()
    {
        return new BindingProxy();
    }

    public object Data
    {
        get
        {
            App.LogForApp(EventType.Warning, "got data");
            return (object)GetValue(DataProperty);
        }
        set { SetValue(DataProperty, value); }
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
}

/// <summary>
/// Interaction logic for MediaExplorerLine.xaml
/// </summary>
public partial class MediaExplorerLine : UserControl
{
    public static readonly DependencyProperty LineItemsProperty =
        DependencyProperty.Register(
            name: nameof(LineItems),
            propertyType: typeof(MediaExplorerLineModel),
            ownerType: typeof(MediaExplorerLine),
            new PropertyMetadata(default(MediaExplorerLineModel)));

    public static readonly DependencyProperty ImageContainerWidthProperty =
        DependencyProperty.Register(
            name: nameof(ImageContainerWidth),
            propertyType: typeof(double),
            ownerType: typeof(MediaExplorerLine),
            new PropertyMetadata(default(double)));

    public static readonly DependencyProperty ImageWidthProperty =
        DependencyProperty.Register(
            name: nameof(ImageWidth),
            propertyType: typeof(double),
            ownerType: typeof(MediaExplorerLine),
            new PropertyMetadata(default(double)));

    public static readonly DependencyProperty ImageHeightProperty =
        DependencyProperty.Register(
            name: nameof(ImageHeight),
            propertyType: typeof(double),
            ownerType: typeof(MediaExplorerLine),
            new PropertyMetadata(default(double)));

    public MediaExplorerLine()
    {
        InitializeComponent();
    }

    public MediaExplorerLineModel LineItems
    {
        get => (MediaExplorerLineModel)GetValue(LineItemsProperty);
        set => SetValue(LineItemsProperty, value);
    }

    public double ImageContainerWidth
    {
        get => (double)GetValue(ImageContainerWidthProperty);
        set => SetValue(ImageContainerWidthProperty, value);
    }

    public double ImageWidth
    {
        get => (double)GetValue(ImageWidthProperty);
        set => SetValue(ImageWidthProperty, value);
    }

    public double ImageHeight
    {
        get => (double)GetValue(ImageHeightProperty);
        set => SetValue(ImageHeightProperty, value);
    }

    public void OnItemMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (sender is System.Windows.Controls.Image { DataContext: MediaExplorerItem item } image)
            {
                MediaItem mediaItem = App.State.Catalog.GetMediaFromId(item.MediaId);

                mediaItem.PropertyChanged += item.OnMediaItemChanged;
                // also subscribe to any changes to this item, in case we drop it into a stack

                DragDrop.DoDragDrop(image, item.MediaId.ToString(), DragDropEffects.Copy);

                mediaItem.PropertyChanged -= item.OnMediaItemChanged;

                App.LogForApp(EventType.Information, $"mouse move for image");
            }
        }
    }

    private Guid m_itemBeingDragged;

    private void OnItemDragEnter(object sender, DragEventArgs e)
    {
        if (sender is System.Windows.Controls.Image { DataContext: MediaExplorerItem item } image)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat)
                && Guid.TryParse(((string?)e.Data.GetData(DataFormats.StringFormat)) ?? "", out m_itemBeingDragged))
            {
                // don't allow dragging and dropping onto same image
                if (m_itemBeingDragged == item.MediaId)
                {
                    m_itemBeingDragged = Guid.Empty;
                    return;
                }

                item.IsActiveDropTarget = true;
            }
        }
    }

    private void OnItemDragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;

        if (sender is System.Windows.Controls.Image { DataContext: MediaExplorerItem item } image)
        {
            if (m_itemBeingDragged == Guid.Empty)
                return;

            if (e.Data.GetDataPresent(DataFormats.StringFormat)
                && Guid.TryParse(((string?)e.Data.GetData(DataFormats.StringFormat)) ?? "", out m_itemBeingDragged))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }
    }

    private void OnItemDragLeave(object sender, DragEventArgs e)
    {
        if (sender is System.Windows.Controls.Image { DataContext: MediaExplorerItem item } image)
        {
            item.IsActiveDropTarget = false;
            if (e.Data.GetDataPresent(DataFormats.StringFormat)
                && Guid.TryParse(((string?)e.Data.GetData(DataFormats.StringFormat)) ?? "", out m_itemBeingDragged))
            {
                m_itemBeingDragged = Guid.Empty;
            }
        }
    }

    private void OnItemDragDrop(object sender, DragEventArgs e)
    {
        if (m_itemBeingDragged == Guid.Empty)
            return;

        if (sender is System.Windows.Controls.Image { DataContext: MediaExplorerItem item } image)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat)
                && Guid.TryParse(((string?)e.Data.GetData(DataFormats.StringFormat)) ?? "", out m_itemBeingDragged))
            {
                item.IsActiveDropTarget = false;
                List<MediaStack> stackItems = new List<MediaStack>();

                MediaItem mediaItem = App.State.Catalog.GetMediaFromId(item.MediaId);
                if (mediaItem.VersionStack != null)
                    stackItems.Add(App.State.Catalog.VersionStacks.Items[mediaItem.VersionStack.Value]);
                if (mediaItem.MediaStack != null)
                    stackItems.Add(App.State.Catalog.MediaStacks.Items[mediaItem.MediaStack.Value]);

                MediaStack? stack = SelectStack.GetMediaStack(Window.GetWindow(image), mediaItem);

                if (stack != null)
                {
                    MediaItem mediaItemBeingDragged = App.State.Catalog.GetMediaFromId(m_itemBeingDragged);

                    stack.PushNewItem(m_itemBeingDragged);
                    if (stack.Type.Equals(MediaStackType.Media))
                        mediaItemBeingDragged.SetMediaStackVerify(App.State.Catalog, stack.StackId);
                    else
                        mediaItemBeingDragged.SetVersionStackVerify(App.State.Catalog, stack.StackId);

                    item.UpdateStackInformation();
                }
            }
        }
    }
}
