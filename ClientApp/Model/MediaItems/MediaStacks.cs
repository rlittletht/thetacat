using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.Model;

public class MediaStacks : INotifyPropertyChanged
{
    private Guid m_stackId;
    private List<MediaStackItem> m_items = new();

#region Public Data/Accessors

    public Guid StackId
    {
        get => m_stackId;
        set => SetField(ref m_stackId, value);
    }

    public List<MediaStackItem> Items
    {
        get => m_items;
        set => SetField(ref m_items, value);
    }

#endregion

#region INotifyPropertyChanged

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

#endregion

//    public MediaStackItem AddToStack(Guid? stackId, MediaItem item)
//    {
//        stackId ??= Guid.NewGuid();
//        MediaStackItem newStackItem = new MediaStackItem();
//    }
}
