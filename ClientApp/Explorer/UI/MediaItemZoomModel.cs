using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Thetacat.Model;

namespace Thetacat.Explorer.UI;

public class MediaItemZoomModel : INotifyPropertyChanged
{
    public ObservableCollection<MediaTag> Tags { get; } = new ObservableCollection<MediaTag>();
    private MediaItem? m_mediaItem;
    private BitmapSource? m_image;
    private string m_pruneModeCaption = "Start Pruning";

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

    public string PruneModeCaption
    {
        get => m_pruneModeCaption;
        set => SetField(ref m_pruneModeCaption, value);
    }

    public BitmapSource? Image
    {
        get => m_image;
        set => SetField(ref m_image, value);
    }

    public MediaItem? MediaItem
    {
        get => m_mediaItem;
        set => SetField(ref m_mediaItem, value);
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
