using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Util;

namespace Thetacat.Import.UI;

public class RepathItem: INotifyPropertyChanged
{
    private PathSegment m_from = PathSegment.Empty;
    private PathSegment m_to = PathSegment.Empty;
    private RepathItemType m_type;

    public RepathItemType Type
    {
        get => m_type;
        set
        {
            if (value == m_type) return;
            m_type = value;
            OnPropertyChanged();
        }
    }

    public string TypeString => "Remap";

    public PathSegment From
    {
        get => m_from;
        set => SetField(ref m_from, value);
    }

    public PathSegment To
    {
        get => m_to;
        set => SetField(ref m_to, value);
    }

    public RepathItem(PathSegment from, PathSegment to, RepathItemType type)
    {
        From = from;
        To = to;
        Type = type;
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
