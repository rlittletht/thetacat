using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Thetacat.Model;

namespace Thetacat.UI.Explorer;

public class MediaItemZoomModel:INotifyPropertyChanged
{
    private MediaItem? m_mediaItem;
    private BitmapImage? m_image;

    public BitmapImage? Image
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
