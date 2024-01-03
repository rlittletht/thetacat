using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Metatags;

namespace Thetacat.Controls.MetatagTreeViewControl;

public class MetatagTreeViewModel: INotifyPropertyChanged
{
    public ObservableCollection<CheckableMetatagTreeItem> Items = new();
    private MetatagTree? m_metatagTree;
    private int m_schemaVersion;

    public int SchemaVersion
    {
        get => m_schemaVersion;
        set => SetField(ref m_schemaVersion, value);
    }

    public MetatagTree? MetatagTree
    {
        get => m_metatagTree;
        set => SetField(ref m_metatagTree, value);
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
