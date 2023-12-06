using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.Migration.Elements.Metadata.UI;

public class PseMetadata: INotifyPropertyChanged
{
    private int m_pseID;
    private string m_pseIdentifier = string.Empty;
    private string m_standard = string.Empty;
    private string m_tag = string.Empty;
    private bool m_migrate = false;
    private string m_pseDatatype = string.Empty;
    private string m_description = string.Empty;

    public int PseID
    {
        get => m_pseID;
        set => SetField(ref m_pseID, value);
    }

    public bool Migrate
    {
        get => m_migrate;
        set => SetField(ref m_migrate, value);
    }

    public string PseDatatype
    {
        get => m_pseDatatype;
        set => SetField(ref m_pseDatatype, value);
    }

    public string PseIdentifier
    {
        get => m_pseIdentifier;
        set => SetField(ref m_pseIdentifier, value);
    }

    public string Standard
    {
        get => m_standard;
        set => SetField(ref m_standard, value);
    }

    public string Tag
    {
        get => m_tag;
        set => SetField(ref m_tag, value);
    }

    public string Description
    {
        get => m_description;
        set => SetField(ref m_description, value);
    }

    public Guid ID { get; set; }

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
