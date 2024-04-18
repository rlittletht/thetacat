using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Thetacat.Explorer;

public class MediaExplorerItem : INotifyPropertyChanged
{
    private Guid m_mediaId;
    private string m_tileSrc;
    public string m_tileLabel;
    private BitmapSource? m_tileImage;
    private bool m_selected;
    private bool m_isTrashItem;
    private Visibility m_trashAdornerVisibility;
    private bool m_isOffline;
    private Visibility m_offlineAdornerVisibility;

    public bool IsTrashItem
    {
        get => m_isTrashItem;
        set
        {
            SetField(ref m_isTrashItem, value);
            TrashAdornerVisibility = m_isTrashItem ? Visibility.Visible : Visibility.Collapsed;
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
