using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.ServiceClient;

namespace Thetacat.Model;

// Each individual item tracks whether it is dirty or not
public class MediaStacks : INotifyPropertyChanged
{
    private Dictionary<Guid, MediaStack> m_items = new();
    private readonly MediaStackType m_type;

    public MediaStacks(MediaStackType type)
    {
        m_type = type;
    }

#region Public Data/Accessors

    public Dictionary<Guid, MediaStack> Items
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

    public void Clear()
    {
        m_items.Clear();
    }

    public void AddStack(MediaStack item)
    {
        item.Type = m_type;
        m_items.Add(item.StackId, item);
    }

    public MediaStack CreateNewStack(string description = "")
    {
        MediaStack newStack = new MediaStack(m_type, description);
        m_items.Add(newStack.StackId, newStack);
        newStack.PendingOp = MediaStack.Op.Create;

        return newStack;
    }

    public MediaStackItem AddToStack(Guid? stackId, MediaItem item)
    {
        stackId ??= Guid.NewGuid();

        if (!m_items.TryGetValue(stackId.Value, out MediaStack? stack))
        {
            stack = CreateNewStack();
            m_items.Add(stack.StackId, stack);
        }

        return stack.PushNewItem(item.ID);
    }

    public MediaStackEnumerator GetPendingCreates()
    {
        return new MediaStackEnumerator(m_items, (item) => item.PendingOp == MediaStack.Op.Create);
    }

    public MediaStackEnumerator GetDirtyItems()
    {
        return new MediaStackEnumerator(m_items, (item) => item.PendingOp != MediaStack.Op.None);
    }
    
    public void PushPendingChanges()
    {
        List<MediaStackDiff> stackDiffs = new();

        foreach (MediaStack stack in GetDirtyItems())
        {
            stackDiffs.Add(new MediaStackDiff(stack, stack.PendingOp));
        }

        ServiceInterop.UpdateMediaStacks(stackDiffs);

        foreach (MediaStackDiff diff in stackDiffs)
        {
            if (diff.PendingOp == MediaStack.Op.Delete)
                Items.Remove(diff.Stack.StackId);
            else if (Items.TryGetValue(diff.Stack.StackId, out MediaStack? stack))
            {
                if (stack.VectorClock == diff.VectorClock)
                    stack.PendingOp = MediaStack.Op.None;
            }
        }
    }
}
