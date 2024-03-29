﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.TcSettings;

namespace Thetacat.UI.Options;

public class CatOptionsModel: INotifyPropertyChanged
{
    private ProfileOptions? m_currentProfile;
    public ObservableCollection<ProfileOptions> ProfileOptions { get; set; } = new();

    public ProfileOptions? CurrentProfile
    {
        get => m_currentProfile;
        set => SetField(ref m_currentProfile, value);
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
