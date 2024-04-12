using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Import.UI.Commands;

namespace Thetacat.Import.UI;

public class VirtualRepathingModel: INotifyPropertyChanged
{
    private string m_mapFrom = string.Empty;
    private string m_mapTo = string.Empty;
    private string m_newFolder = string.Empty;

    public string NewFolder
    {
        get => m_newFolder;
        set => SetField(ref m_newFolder, value);
    }

    public ObservableCollection<VirtualRootNameItem> OriginalRoots { get; } = new();
    public ObservableCollection<RepathItem> RepathItems { get; } = new();

    public ObservableCollection<string> Maps { get; } = new();

    public string CurrentMap
    {
        get => m_currentMap;
        set => SetField(ref m_currentMap, value);
    }

    public MapStore? MapStore;
    private string m_currentMap = string.Empty;

    public string MapTo
    {
        get => m_mapTo;
        set => SetField(ref m_mapTo, value);
    }

    public string MapFrom
    {
        get => m_mapFrom;
        set => SetField(ref m_mapFrom, value);
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

    public AddPathToRepathMapCommand? AddPathToRepathMapCommand { get; set; }
    public RemoveMappingCommand? RemoveMappingCommand { get; set; }
}
