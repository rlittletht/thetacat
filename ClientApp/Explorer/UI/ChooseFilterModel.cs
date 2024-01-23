using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using Thetacat.Filtering;
using Thetacat.Metatags;

namespace Thetacat.Explorer.UI;

public class ChooseFilterModel: INotifyPropertyChanged
{
    public ObservableCollection<FilterDefinition> AvailableFilters { get; set; }= new();
    private string m_name = string.Empty;
    private string m_description = string.Empty;
    private FilterDefinition? m_selectedFilterDefinition;

    public FilterDefinition? SelectedFilterDefinition
    {
        get => m_selectedFilterDefinition;
        set => SetField(ref m_selectedFilterDefinition, value);
    }

    public ObservableCollection<string> QueryText { get; set; } = new();

    public string Description
    {
        get => m_description;
        set => SetField(ref m_description, value);
    }

    public string Name
    {
        get => m_name;
        set => SetField(ref m_name, value);
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
