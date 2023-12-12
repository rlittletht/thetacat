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
    private string m_standardTag = string.Empty;
    private string m_propertyTag = string.Empty;
    private bool m_migrate = false;
    private string m_pseDatatype = string.Empty;
    private string m_description = string.Empty;
    private Guid? m_catId;

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

    public string StandardTag
    {
        get => m_standardTag;
        set => SetField(ref m_standardTag, value);
    }

    public string PropertyTag
    {
        get => m_propertyTag;
        set => SetField(ref m_propertyTag, value);
    }

    public string Description
    {
        get => m_description;
        set => SetField(ref m_description, value);
    }

    public Guid? CatID
    {
        get => m_catId;
        set => SetField(ref m_catId, value);
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
