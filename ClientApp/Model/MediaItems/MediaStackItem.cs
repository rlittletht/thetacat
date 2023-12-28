using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
}
