using Emgu.CV.PpfMatch3d;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Metatags.Model;
using Thetacat.ServiceClient;
using Thetacat.Types;

namespace Thetacat.Model;

public class MediaStackItem: INotifyPropertyChanged
{
    private Guid m_mediaId;
    private int m_stackIndex;

    public MediaStackItem(Guid mediaId, int stackIndex)
    {
        m_mediaId = mediaId;
        m_stackIndex = stackIndex;
    }

    public Guid MediaId
    {
        get => m_mediaId;
        set => SetField(ref m_mediaId, value);
    }

    public int StackIndex
    {
        get => m_stackIndex;
        set => SetField(ref m_stackIndex, value);
    }

    public MediaStackItem(ServiceStackItem serviceStackItem)
    {
        m_mediaId = serviceStackItem.MediaId ?? throw new CatExceptionServiceDataFailure("media id not read");
        m_stackIndex = serviceStackItem.OrderHint ?? throw new CatExceptionServiceDataFailure("stack index not read");
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

    public static bool operator ==(MediaStackItem? left, MediaStackItem? right)
    {
        if (object.ReferenceEquals(left, null) && object.ReferenceEquals(right, null)) return true;
        if (object.ReferenceEquals(left, null) || object.ReferenceEquals(right, null)) return false;

        if (left.MediaId != right.MediaId) return false;
        if (left.StackIndex != right.StackIndex) return false;

        return true;
    }

    public static bool operator !=(MediaStackItem? left, MediaStackItem? right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        MediaStackItem? right = obj as MediaStackItem;

        if (obj == null)
            throw new ArgumentException(nameof(obj));

        return this == right;
    }

    public override int GetHashCode() => ToString().GetHashCode();
    public override string ToString() => $"{MediaId}:{StackIndex})";
}
