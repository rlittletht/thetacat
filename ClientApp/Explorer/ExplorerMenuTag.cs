using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.Explorer;

public class ExplorerMenuTag : INotifyPropertyChanged
{
    private Guid m_mediaTagId;
    private string m_tagName = "";
    private string m_tagDescription = "";

    public Guid MediaTagId
    {
        get => m_mediaTagId;
        set => SetField(ref m_mediaTagId, value);
    }

    public string TagDescription
    {
        get => m_tagDescription;
        set => SetField(ref m_tagDescription, value);
    }
    public string TagName
    {
        get => m_tagName;
        set => SetField(ref m_tagName, value);
    }

    public string TagMenuText => $"{m_tagName} ({m_tagDescription})";

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
