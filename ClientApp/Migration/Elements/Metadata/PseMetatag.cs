using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Migration.Elements.Metadata.UI;

public class PseMetatag : INotifyPropertyChanged
{
    private bool m_isSelected = false;
    private Guid? m_catId;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ID { get; set; }
    public int ParentID { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string ElementsTypeName { get; set; } = string.Empty;

    public Guid? CatID
    {
        get => m_catId;
        set => SetField(ref m_catId, value);
    }

    public bool IsSelected
    {
        get => m_isSelected;
        set => SetField(ref m_isSelected, value);
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