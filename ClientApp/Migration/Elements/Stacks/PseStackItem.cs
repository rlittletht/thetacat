using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.Migration.Elements.Versions;

public class PseStackItem: INotifyPropertyChanged
{
    private int m_mediaId;
    private int m_stackId;
    private int m_mediaIndex;
    private Guid? m_catStackId;
    private Guid? m_catMediaId;

    public int MediaID
    {
        get => m_mediaId;
        set => SetField(ref m_mediaId, value);
    }

    public int StackID
    {
        get => m_stackId;
        set => SetField(ref m_stackId, value);
    }

    public int MediaIndex
    {
        get => m_mediaIndex;
        set => SetField(ref m_mediaIndex, value);
    }

    public Guid? CatStackId
    {
        get => m_catStackId;
        set => SetField(ref m_catStackId, value);
    }

    public Guid? CatMediaId
    {
        get => m_catMediaId;
        set => SetField(ref m_catMediaId, value);
    }

    public PseStackItem(int mediaID, int stackID, int mediaIndex)
    {
        MediaID = mediaID;
        StackID = stackID;
        MediaIndex = mediaIndex;
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
