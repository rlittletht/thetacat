using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Types;

namespace Thetacat.UI.Options;

public class CacheConfigModel: INotifyPropertyChanged
{
    private string m_cacheLocation = string.Empty;
    private string _cacheType = "Workgroup";

    public string CacheLocation
    {
        get => m_cacheLocation;
        set => SetField(ref m_cacheLocation, value);
    }

    public string CacheType
    {
        get => _cacheType;
        set => SetField(ref _cacheType, value);
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
