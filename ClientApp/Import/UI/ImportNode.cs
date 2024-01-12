﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Util;

namespace Thetacat.Import.UI;

public class ImportNode: INotifyPropertyChanged, ICheckableTreeViewItem<ImportNode>
{
    private bool m_checked;
    private string m_name;
    private string m_md5;
    private string m_path;
    private bool m_isDirectory;

    public ObservableCollection<ImportNode> Children { get; set; } = new ObservableCollection<ImportNode>();

    public bool Checked
    {
        get => m_checked;
        set => SetField(ref m_checked, value);
    }

    public string Name
    {
        get => m_name;
        set => SetField(ref m_name, value);
    }

    public string MD5
    {
        get => m_md5;
        set => SetField(ref m_md5, value);
    }

    public string Path
    {
        get => m_path;
        set => SetField(ref m_path, value);
    }

    public bool IsDirectory
    {
        get => m_isDirectory;
        set => SetField(ref m_isDirectory, value);
    }

    public ImportNode(bool _checked, string name, string md5, string path, bool isDirectory)
    {
        m_checked = _checked;
        m_name = name;
        m_md5 = md5;
        m_path = path;
        m_isDirectory = isDirectory;
    }

    public string FullPath => System.IO.Path.Combine(m_path, m_name);

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