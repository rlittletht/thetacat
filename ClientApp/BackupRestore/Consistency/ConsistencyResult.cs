using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.UI.Controls.MediaItemsListControl;

namespace Thetacat.BackupRestore.Consistency;

public class ConsistencyResult:INotifyPropertyChanged
{
    private ObservableCollection<MediaItemsListItem> m_items;
    private string m_heading;
    private string m_description;

    public string Description
    {
        get => m_description;
        set => SetField(ref m_description, value);
    }

    public string Heading
    {
        get => m_heading;
        set => SetField(ref m_heading, value);
    }

    public ObservableCollection<MediaItemsListItem> Items
    {
        get => m_items;
        set => SetField(ref m_items, value);
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

    public ConsistencyResult(string heading, string description, ObservableCollection<MediaItemsListItem> items)
    {
        m_heading = heading;
        m_items = items;
        m_description = description;
    }
}
