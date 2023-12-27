using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Util;

namespace Thetacat.Migration.Elements.Versions;

public class StackMigrateSummaryItem: INotifyPropertyChanged, ICheckableListViewItem
{
    private bool m_checked;
    private Guid m_mediaId;
    private Guid m_stackId;
    private int m_stackIndex;
    private string m_stackType;
    private string m_mediaDescription;

    public bool Checked
    {
        get => m_checked;
        set => SetField(ref m_checked, value);
    }

    public Guid MediaID
    {
        get => m_mediaId;
        set => SetField(ref m_mediaId, value);
    }

    public Guid StackID
    {
        get => m_stackId;
        set => SetField(ref m_stackId, value);
    }

    public int StackIndex
    {
        get => m_stackIndex;
        set => SetField(ref m_stackIndex, value);
    }

    public string StackType
    {
        get => m_stackType;
        set => SetField(ref m_stackType, value);
    }

    public string MediaDescription
    {
        get => m_mediaDescription;
        set => SetField(ref m_mediaDescription, value);
    }

    public StackMigrateSummaryItem(Guid mediaID, Guid stackID, int stackIndex, string stackType, string mediaDescription)
    {
        m_mediaId = mediaID;
        m_stackId = stackID;
        m_stackIndex = stackIndex;
        m_stackType = stackType;
        m_mediaDescription = mediaDescription;
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
