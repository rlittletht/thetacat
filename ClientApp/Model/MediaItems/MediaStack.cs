using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.ServiceClient;
using Thetacat.Types;

namespace Thetacat.Model;

public class MediaStack : INotifyPropertyChanged, INotifyCollectionChanged
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
        set
        {
            SetField(ref m_items, value);
            OnCollectionChanged();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: IsItemTopOfStack
        %%Qualified: Thetacat.Model.MediaStack.IsItemTopOfStack

        Return true if the given mediaId is both in the stack, and is the top of
        the stack
    ----------------------------------------------------------------------------*/
    public bool IsItemTopOfStack(Guid mediaId)
    {
        if (FindMediaInStack(mediaId, out int beforeCount, out _) != null)
            return beforeCount == 0;

        return false;
    }

    /*----------------------------------------------------------------------------
        %%Function: FindMediaInStack
        %%Qualified: Thetacat.Model.MediaStack.FindMediaInStack

        NOTE: this will always traverse the entire stack (in order to get the
        before/after count
    ----------------------------------------------------------------------------*/
    public MediaStackItem? FindMediaInStack(Guid itemId, out int beforeCount, out int afterCount)
    {
        beforeCount = 0;
        afterCount = 0;

        Dictionary<int, int> counts = new Dictionary<int, int>();

        MediaStackItem? match = null;

        foreach (MediaStackItem item in m_items)
        {
            counts.TryAdd(item.StackIndex, 0);
            counts[item.StackIndex]++;

            if (item.MediaId == itemId)
                match = item;
        }

        if (match != null)
        {
            foreach (KeyValuePair<int, int> count in counts)
            {
                // if we are a duplicate, we will always consider ourselves to be
                // last of the dupes
                if (count.Key <= match.StackIndex)
                    beforeCount += count.Value;
                else
                    afterCount += count.Value;
            }

            beforeCount--; // remove ourselves from the count
        }

        return match;
    }

    public MediaStackItem? FindMediaInStack(Guid itemId)
    {
        return FindMediaInStack(itemId, out int _, out int _);
    }

    public void RemoveItem(MediaStackItem item)
    {
        m_items.Remove(item);
        if (m_items.Count == 0)
            PendingOp = Op.Delete;
        else if (PendingOp == Op.None)
            PendingOp = Op.Update;
        OnCollectionChanged();
    }

    public void PushItem(MediaStackItem item)
    {
        m_items.Add(item);
        if (PendingOp == Op.None)
            PendingOp = Op.Update;
        OnCollectionChanged();
    }

    public MediaStackItem PushNewItem(Guid mediaId)
    {
        int stackIndex = Items.Count;
        MediaStackItem newStackItem = new MediaStackItem(mediaId, stackIndex);

        PushItem(newStackItem);
        // PushItem will notify about the collection changing
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

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    protected virtual void OnCollectionChanged()
    {
        // the only action we support is Reset
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}