using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.UI.Options;

public class AccountModel : INotifyPropertyChanged
{
    private string m_storageAccount = string.Empty;
    private string m_container = string.Empty;

    public string StorageAccount
    {
        get => m_storageAccount;
        set => SetField(ref m_storageAccount, value);
    }

    public string Container
    {
        get => m_container;
        set => SetField(ref m_container, value);
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
