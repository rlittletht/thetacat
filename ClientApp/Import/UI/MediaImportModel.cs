using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.Import.UI;

public class MediaImportModel: INotifyPropertyChanged
{
    private string m_sourcePath = String.Empty;
    public ObservableCollection<ImportNode> Nodes { get; set; } = new ObservableCollection<ImportNode>();
    public ObservableCollection<ImportNode> ImportItems { get; set; } = new ObservableCollection<ImportNode>();

    public string SourcePath
    {
        get => m_sourcePath;
        set => SetField(ref m_sourcePath, value);
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
