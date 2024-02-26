using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.ServiceClient;
using Thetacat.Types;

namespace Thetacat.Model;

public class MediaStack: INotifyPropertyChanged
{
    public enum Op
    {
        Create,
        Update,
        Delete,
        None
    }

    private Guid m_stackId;

    private List<MediaStackItem> m_items = new List<MediaStackItem>();
    private MediaStackType m_type;
    private string m_description;
    private Op m_pendingOp = Op.None;
    public int VectorClock = 0;

    public Op PendingOp
    {
        get => m_pendingOp;
        set => SetField(ref m_pendingOp, value);
    }

    public MediaStack(MediaStackType type, string description)
    {
        m_stackId = Guid.NewGuid();
        m_type = type;
        m_description = description;
    }

    public MediaStack(ServiceStack serviceStack)
    {
        m_description = serviceStack.Description ?? throw new CatExceptionServiceDataFailure("description not read");
        m_type = new MediaStackType(serviceStack.StackType ?? throw new CatExceptionServiceDataFailure("type not read"));
        m_stackId = serviceStack.Id ?? throw new CatExceptionServiceDataFailure("id not read");

        foreach (ServiceStackItem item in serviceStack.StackItems ?? throw new CatExceptionServiceDataFailure("items not set"))
        {
            m_items.Add(new MediaStackItem(item));
        }
    }

    public MediaStack(MediaStack clone)
    {
        m_stackId = clone.m_stackId;
        m_type = clone.m_type;
        m_description = clone.m_description;
        foreach (MediaStackItem item in clone.m_items)
        {
            m_items.Add(new MediaStackItem(item.MediaId, item.StackIndex));
        }
    }

    public Guid StackId
    {
        get => m_stackId;
        set => SetField(ref m_stackId, value);
    }

    public string Description
    {
        get => m_description;
        set => SetField(ref m_description, value);
    }

    public MediaStackType Type
    {
        get => m_type;
        set => SetField(ref m_type, value);
    }

    public List<MediaStackItem> Items
    {
        get => m_items;
        set => SetField(ref m_items, value);
    }

    public MediaStackItem? FindMediaInStack(Guid itemId)
    {
        foreach (MediaStackItem item in m_items)
        {
            if (item.MediaId == itemId)
                return item;
        }

        return null;
    }

    public void RemoveItem(MediaStackItem item)
    {
        m_items.Remove(item);
        if (m_items.Count == 0)
            PendingOp = Op.Delete;
        else if (PendingOp == Op.None)
            PendingOp = Op.Update;
    }

    public void PushItem(MediaStackItem item)
    {
        m_items.Add(item);
        if (PendingOp == Op.None)
            PendingOp = Op.Update;
    }

    public MediaStackItem PushNewItem(Guid mediaId)
    {
        int stackIndex = Items.Count;
        MediaStackItem newStackItem = new MediaStackItem(mediaId, stackIndex);

        PushItem(newStackItem);
        return newStackItem;
    }

    public Op CompareTo(MediaStack? right)
    {
        if (right == null)
            return Op.Create;

        Dictionary<Guid, MediaStackItem> mapItems = new();

        foreach (MediaStackItem item in right.Items)
        {
            mapItems.Add(item.MediaId, item);
        }

        foreach (MediaStackItem item in m_items)
        {
            if (!mapItems.TryGetValue(item.MediaId, out MediaStackItem? otherItem))
                return Op.Update;

            if (otherItem != item)
                return Op.Update;
        }

        return Op.None;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        VectorClock++;
        if (PendingOp == Op.None)
            PendingOp = Op.Update;

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
