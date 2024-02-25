using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.Import.UI;

public class MediaImportModel: INotifyPropertyChanged
{
    private string m_sourcePath = String.Empty;
    private bool m_includeSubdirInVirtualPath = true;
    private string m_virtualPathSuffix = string.Empty;
    private string m_virtualPathPreview = string.Empty;
    public ObservableCollection<ImportNode> Nodes { get; set; } = new();
    public ObservableCollection<ImportNode> ImportItems { get; set; } = new();
    public ObservableCollection<string> FileExtensions { get; set; } = new();

    public ObservableCollection<VirtualRootNameItem> VirtualPathRoots { get; }= new();
    private VirtualRootNameItem? m_virtualPathRoot;

    public VirtualRootNameItem? VirtualPathRoot
    {
        get => m_virtualPathRoot;
        set => SetField(ref m_virtualPathRoot, value);
    }

    public string VirtualPathPreview
    {
        get => m_virtualPathPreview;
        set => SetField(ref m_virtualPathPreview, value);
    }

    public bool IncludeSubdirInVirtualPath
    {
        get => m_includeSubdirInVirtualPath;
        set => SetField(ref m_includeSubdirInVirtualPath, value);
    }

    public string VirtualPathSuffix
    {
        get => m_virtualPathSuffix;
        set => SetField(ref m_virtualPathSuffix, value);
    }

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
