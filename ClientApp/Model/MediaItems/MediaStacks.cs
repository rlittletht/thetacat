﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.ServiceClient;

namespace Thetacat.Model;

public class MediaStacks : INotifyPropertyChanged
{
    private Guid m_stackId;
    private Dictionary<Guid, MediaStack> m_items = new();
    private string m_type;

    public MediaStacks(string type)
    {
        m_type = type;
    }

#region Public Data/Accessors

    public Guid StackId
    {
        get => m_stackId;
        set => SetField(ref m_stackId, value);
    }

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
}
