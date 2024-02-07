using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.TcSettings;

namespace Thetacat.UI.Options;

public class ProfileOptions: INotifyPropertyChanged
{
    private string m_profileName = string.Empty;
    private bool m_default = false;

    public Profile Profile { get; set; }
    public string ProfileName
    {
        get => m_profileName;
        set => SetField(ref m_profileName, value);
    }

    public bool Default
    {
        get => m_default;
        set => SetField(ref m_default, value);
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

    public ProfileOptions(Profile profile)
    {
        m_default = profile.Default;
        m_profileName = profile.Name ?? "";
        Profile = profile;
    }
}
