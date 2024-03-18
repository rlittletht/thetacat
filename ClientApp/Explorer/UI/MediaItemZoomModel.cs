using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Thetacat.Model;

namespace Thetacat.Explorer.UI;

public class MediaItemZoomModel : INotifyPropertyChanged
{
    public ObservableCollection<MediaTag> Tags { get; } = new ObservableCollection<MediaTag>();
    private MediaItem? m_mediaItem;
    private BitmapSource? m_image;

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
