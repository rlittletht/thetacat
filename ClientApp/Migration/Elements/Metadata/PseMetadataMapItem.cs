using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.Migration.Elements.Metadata;

public class PseMetadataMapItem: INotifyPropertyChanged
{
    private string m_pseIdentifier;
    private string m_rootTag;
    private string m_tagName;
    private string m_description;

    public string PseIdentifier
    {
        get => m_pseIdentifier;
        set => SetField(ref m_pseIdentifier, value);
    }

    public string RootTag
    {
        get => m_rootTag;
        set => SetField(ref m_rootTag, value);
    }

    public string TagName
    {
        get => m_tagName;
        set => SetField(ref m_tagName, value);
    }

    public string Description
    {
        get => m_description;
        set => SetField(ref m_description, value);
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

    public PseMetadataMapItem(string pseIdentifier)
    {
        m_pseIdentifier = pseIdentifier;
    }
}
