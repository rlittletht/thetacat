using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Thetacat.Model;
using Thetacat.Types;

namespace Thetacat.Explorer;

public class MediaExplorerItem : INotifyPropertyChanged
{
    private Guid m_mediaId;
    private string m_tileSrc;
    public string m_tileLabel;
    private BitmapSource? m_tileImage;
    private bool m_selected;
    private bool m_isTrashItem;
    private bool m_isTopOfStack = false;
    private bool m_isNotTopOfStack = false;
    private Visibility m_trashAdornerVisibility;
    private Visibility m_topOfStackAdornerVisibility = Visibility.Collapsed;
    private Visibility m_notTopOfStackAdornerVisibility = Visibility.Collapsed;
    private bool m_isOffline;
    private Visibility m_offlineAdornerVisibility;
    private bool m_isActiveDropTarget = false;

    public bool IsActiveDropTarget
    {
        get => m_isActiveDropTarget;
        set => SetField(ref m_isActiveDropTarget, value);
    }

    public bool IsTrashItem
    {
        get => m_isTrashItem;
        set
        {
            SetField(ref m_isTrashItem, value);
            TrashAdornerVisibility = m_isTrashItem ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool IsTopOfStack
    {
        get => m_isTopOfStack;
        set
        {
            SetField(ref m_isTopOfStack, value);
            TopOfStackAdornerVisibility = m_isTopOfStack ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool IsNotTopOfStack
    {
        get => m_isNotTopOfStack;
        set
        {
            SetField(ref m_isNotTopOfStack, value);
            NotTopOfStackAdornerVisibility = m_isNotTopOfStack ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool IsOffline
    {
        get => m_isOffline;
        set
        {
            SetField(ref m_isOffline, value);
            OfflineAdornerVisibility = m_isOffline ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public Visibility OfflineAdornerVisibility
    {
        get => m_offlineAdornerVisibility;
        private set => SetField(ref m_offlineAdornerVisibility, value);
    }

    public Visibility TrashAdornerVisibility
    {
        get => m_trashAdornerVisibility;
        private set => SetField(ref m_trashAdornerVisibility, value);
    }

    public Visibility TopOfStackAdornerVisibility
    {
        get => m_topOfStackAdornerVisibility;
        private set => SetField(ref m_topOfStackAdornerVisibility, value);
    }

    public Visibility NotTopOfStackAdornerVisibility
    {
        get => m_notTopOfStackAdornerVisibility;
        private set => SetField(ref m_notTopOfStackAdornerVisibility, value);
    }

    public bool Selected
    {
        get => m_selected;
        set => SetField(ref m_selected, value);
    }
    
    public Guid MediaId
    {
        get => m_mediaId;
        set => SetField(ref m_mediaId, value);
    }

    public string TileSrc
    {
        get => m_tileSrc;
        set => SetField(ref m_tileSrc, value);
    }

    public BitmapSource? TileImage
    {
        get
        {
            //MainWindow.LogForApp(EventType.Information, $"fetching tile {m_tileSrc}");
            return m_tileImage;
        }
        set => SetField(ref m_tileImage, value);
    }

    public string TileLabel
    {
        get
        {
            //            MainWindow.LogForApp(EventType.Information, $"getting tile image for {m_tileLabel}");
            return m_tileLabel;
        }
        set => SetField(ref m_tileLabel, value);
    }

    public MediaExplorerItem(string tileSrc, string tileLabel, Guid mediaId)
    {
        m_mediaId = mediaId;
        m_tileLabel = tileLabel;
        m_tileSrc = tileSrc;
    }

    public void UpdateStackInformation()
    {
        // if the stack changed, refetch our stack state
        MediaItem item = App.State.Catalog.GetMediaFromId(m_mediaId);

        SetStackInformation(item);
    }

    public void OnStackChanged(Object? sender, NotifyCollectionChangedEventArgs changed)
    {
        UpdateStackInformation();
    }


    public void OnMediaItemChanged(Object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "VersionStack" || e.PropertyName == "MediaStack")
            UpdateStackInformation();
    }

    /*----------------------------------------------------------------------------
        %%Function: SetStackInformation
        %%Qualified: Thetacat.Explorer.MediaExplorerItem.SetStackInformation

        If our media item has stack information, then set additional information
        in our explorer item and subscribe to changes in the stacks
    ----------------------------------------------------------------------------*/
    public void SetStackInformation(MediaItem item)
    {
        IsTopOfStack = false;
        IsNotTopOfStack = false;

        if (item.VersionStack != null)
        {
            if (!App.State.Catalog.VersionStacks.Items.TryGetValue(item.VersionStack.Value, out MediaStack? stack))
                throw new CatExceptionInternalFailure($"item has a version stack that doesn't exist");

            IsTopOfStack = stack.IsItemTopOfStack(item.ID);
            IsNotTopOfStack = !IsTopOfStack;

            // remove and re-add.  Remove is a no-op if we never registered
            stack.CollectionChanged -= OnStackChanged;
            stack.CollectionChanged += OnStackChanged;
        }

        if (item.MediaStack != null)
        {
            if (!App.State.Catalog.MediaStacks.Items.TryGetValue(item.MediaStack.Value, out MediaStack? stack))
                throw new CatExceptionInternalFailure($"item has a version stack that doesn't exist");

            bool isTopOfStack = stack.IsItemTopOfStack(item.ID);

            IsTopOfStack = IsTopOfStack || isTopOfStack;
            IsNotTopOfStack = IsNotTopOfStack || !isTopOfStack;

            // remove and re-add.  Remove is a no-op if we never registered
            stack.CollectionChanged -= OnStackChanged;
            stack.CollectionChanged += OnStackChanged;
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
