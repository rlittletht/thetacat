using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Metatags.Model;

namespace Thetacat.Metatags;

public class ManageMetadataMetatag: INotifyPropertyChanged
{
    private Guid m_id ;
    private string m_name;
    private string m_description;
    private string m_standard;
    private Guid? m_parent;

    public Guid? Parent
    {
        get => m_parent;
        set => SetField(ref m_parent, value);
    }

    public Guid ID
    {
        get => m_id;
        set => SetField(ref m_id, value);
    }

    public string Name
    {
        get => m_name;
        set
        {
            if (SetField(ref m_name, value))
            {
                OnPropertyChanged(nameof(StandardName));
            }
        }
    }

    public string Description
    {
        get => m_description;
        set => SetField(ref m_description, value);
    }

    public string Standard
    {
        get => m_standard;
        set
        {
            if (SetField(ref m_standard, value))
            {
                OnPropertyChanged(nameof(StandardName));
            }
        }
    }

    public string StandardName => $"{Standard}:{Name}";

    public ManageMetadataMetatag(Metatag? metatag)
    {
        if (metatag == null)
        {
            m_id = Guid.NewGuid();
            m_parent = null;
            m_name = string.Empty;
            m_description = string.Empty;
            m_standard = "user";
        }
        else
        {
            m_id = metatag.ID;
            m_name = metatag.Name;
            m_description = metatag.Description;
            m_standard = metatag.Standard;
            m_parent = metatag.Parent;
        }
    }

    public ManageMetadataMetatag(ManageMetadataMetatag clone)
    {
        m_id = clone.m_id;
        m_name = clone.m_name;
        m_description = clone.m_description;
        m_standard = clone.m_standard;
        m_parent = clone.m_parent;
    }

    public ManageMetadataMetatag()
    {
        m_id = Guid.NewGuid();
        m_parent = null;
        m_name = string.Empty;
        m_description = string.Empty;
        m_standard = "user";
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

    public bool CompareTo(ManageMetadataMetatag other)
    {
        if (m_id != other.m_id) return false;
        if (m_parent != other.m_parent) return false;
        if (m_name !=  other.m_name) return false;
        if (m_description != other.m_description) return false;
        if (m_standard != other.m_standard) return false;

        return true;
    }
}
