﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Filtering;
using Thetacat.Model;
using Thetacat.TcSettings;

namespace Thetacat;

public class MainWindowModel: INotifyPropertyChanged
{
    private bool m_isExplorerCollectionDirty;
    private bool m_isSchemaDirty;
    private Profile? m_currentProfile;
    public MediaExplorerCollection ExplorerCollection { get; } = new(14.0);
    public ObservableCollection<FilterDefinition> AvailableFilters { get; } = new ();
    public ObservableCollection<Profile> AvailableProfiles { get; } = new ();

    public Profile? CurrentProfile
    {
        get => m_currentProfile;
        set => SetField(ref m_currentProfile, value);
    }

    public bool IsExplorerCollectionDirty
    {
        get => m_isExplorerCollectionDirty;
        set
        {
            if (SetField(ref m_isExplorerCollectionDirty, value))
            {
                OnPropertyChanged(nameof(IsExplorerCollectionDirty));
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public bool IsSchemaDirty
    {
        get => m_isSchemaDirty;
        set
        {
            if (SetField(ref m_isSchemaDirty, value))
            {
                OnPropertyChanged(nameof(IsSchemaDirty));
                OnPropertyChanged(nameof(IsDirty));
            }
        }
    }

    public bool IsDirty => IsExplorerCollectionDirty || IsSchemaDirty;

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
