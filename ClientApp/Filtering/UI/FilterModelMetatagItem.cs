using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Metatags.Model;

namespace Thetacat.Filtering.UI;

public class FilterModelMetatagItem: INotifyPropertyChanged
{
    private Metatag m_metatag;
    private string m_dropdownName;

    public string DropdownName
    {
        get => m_dropdownName;
        set => SetField(ref m_dropdownName, value);
    }

    public Metatag Metatag
    {
        get => m_metatag;
        set => SetField(ref m_metatag, value);
    }

    public FilterModelMetatagItem(Metatag metatag, string dropdownName)
    {
        m_dropdownName = dropdownName;
        m_metatag = metatag;
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
