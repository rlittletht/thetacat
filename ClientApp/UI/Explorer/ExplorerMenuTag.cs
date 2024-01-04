using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.UI.Explorer;

public class ExplorerMenuTag: INotifyPropertyChanged
{
    private Guid m_mediaTagId;
    private string m_tagName = "";

    public Guid MediaTagId
    {
        get => m_mediaTagId;
        set => SetField(ref m_mediaTagId, value);
    }

    public string TagName
    {
        get => m_tagName;
        set => SetField(ref m_tagName, value);
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
