using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Thetacat.Util;

namespace Thetacat.Model.ImageCaching;

public class ImageCacheItem : INotifyPropertyChanged
{
    public bool IsLoadQueued { get; set; } = false;

    private PathSegment? m_derivativePath = null;
    private BitmapSource? m_image;
    public string LocalPath { get; set; }

    public PathSegment? DerivativePath
    {
        get => m_derivativePath;
        set => SetField(ref m_derivativePath, value);
    }

    public Guid MediaId { get; set; }

    public BitmapSource? ImageInternal
    {
        get => m_image;
        set => m_image = value;
    }

    public BitmapSource? Image
    {
        get => m_image;
        set => SetField(ref m_image, value);
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

    public ImageCacheItem(Guid mediaId, string localPath)
    {
        MediaId = mediaId;
        LocalPath = localPath;
    }

    public void ResetBitmapAndSetDerivativePath(PathSegment derivativePath)
    {
        Image = null;
        DerivativePath = derivativePath;
    }
}
