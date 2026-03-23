using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Model;
using Thetacat.Types;

namespace Thetacat.UI.Controls.MediaItemsListControl;

public class MediaItemsListItem: INotifyPropertyChanged
{
    private Guid? m_id = null;
    private string m_path = string.Empty;

    private string m_details = string.Empty;
    private string m_mimeType = string.Empty;

    public Guid? ID
    {
        get => m_id;
        set => SetField(ref m_id, value);
    }

    public string Path
    {
        get => m_path;
        set => SetField(ref m_path, value);
    }

    public string Details
    {
        get => m_details;
        set => SetField(ref m_details, value);
    }

    public string MimeType
    {
        get => m_mimeType;
        set
        {
            if (value == m_mimeType) return;
            m_mimeType = value;
            OnPropertyChanged();
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

    public static MediaItemsListItem Create(string path)
    {
        return
            new()
            {
                Path = path,
            };
    }

    public static MediaItemsListItem Create(ICacheEntry cacheEntry)
    {
        return
            new()
            {
                ID = cacheEntry.ID,
                Path = cacheEntry.Path,
            };
    }

    public static MediaItemsListItem Create(MediaItem mediaItem, string details = "")
    {
        return
            new()
            {
                ID = mediaItem.ID,
                Path = mediaItem.VirtualPath,
                MimeType = mediaItem.MimeType,
                Details = details
            };
    }
}
