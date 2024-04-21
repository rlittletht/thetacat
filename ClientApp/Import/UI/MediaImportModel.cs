using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Filtering.UI;
using Thetacat.Import.UI.Commands;
using Thetacat.Metatags.Model;

namespace Thetacat.Import.UI;

public class MediaImportModel: INotifyPropertyChanged
{
    public ObservableCollection<FilterModelMetatagItem> AvailableTags { get; set; } = new ObservableCollection<FilterModelMetatagItem>();

    private string m_sourcePath = String.Empty;
    private bool m_includeSubdirInVirtualPath = true;
    private string m_virtualPathSuffix = string.Empty;
    private string m_virtualPathPreview = string.Empty;
    public ObservableCollection<ImportNode> Nodes { get; set; } = new();
    public ObservableCollection<ImportNode> ImportItems { get; set; } = new();
    public ObservableCollection<string> FileExtensions { get; set; } = new();

    public ObservableCollection<VirtualRootNameItem> VirtualPathRoots { get; }= new();
    private VirtualRootNameItem? m_virtualPathRoot;
    private bool m_includeParentDirInVirtualPath;
    private bool m_importInPlace;
    private string m_importStatus = String.Empty;
    private bool m_isMediaCheckedAgainstCatalog = false;

    public bool IsMediaCheckedAgainstCatalog
    {
        get => m_isMediaCheckedAgainstCatalog;
        set => SetField(ref m_isMediaCheckedAgainstCatalog, value);
    }

    public string ImportStatus
    {
        get => m_importStatus;
        set => SetField(ref m_importStatus, value);
    }

    public ObservableCollection<FilterModelMetatagItem> InitialTags { get; set; } = new ObservableCollection<FilterModelMetatagItem>();

    public bool ImportInPlace
    {
        get => m_importInPlace;
        set
        {
            SetField(ref m_importInPlace, value);
            OnPropertyChanged(nameof(EnableRepathControls));
        }
    }

    public bool EnableRepathControls => !ImportInPlace;

    public bool IncludeParentDirInVirtualPath
    {
        get => m_includeParentDirInVirtualPath;
        set => SetField(ref m_includeParentDirInVirtualPath, value);
    }

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

    public RemoveInitialTagCommand? RemoveInitialTagCommand { get; set; }

}
