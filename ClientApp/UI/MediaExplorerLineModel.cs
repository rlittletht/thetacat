using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Util;

namespace Thetacat.UI;

public class MediaExplorerLineModel: IObservableSegmentableCollectionHolder<MediaExplorerItem>, INotifyPropertyChanged
{
    private string m_lineLabel = "";
    public ObservableCollection<MediaExplorerItem> Items { get; set; } = new ObservableCollection<MediaExplorerItem>();
    public bool EndSegmentAfter { get; set; } = false;

    public string LineLabel
    {
        get => m_lineLabel;
        set => SetField(ref m_lineLabel, value);
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
