using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.ServiceClient;

namespace Thetacat.UI.Options;

public class AccountModel : INotifyPropertyChanged
{
    public ProfileOptions? CurrentProfile
    {
        get => m_currentProfile;
        set => SetField(ref m_currentProfile, value);
    }

    private string m_storageAccount = string.Empty;
    private string m_container = string.Empty;
    private string m_sqlConnection = string.Empty;
    private ProfileOptions? m_currentProfile;
    private ServiceCatalogDefinition? m_catalogDefinition;

    public ObservableCollection<ServiceCatalogDefinition> CatalogDefinitions { get; set; } = new();

    public ServiceCatalogDefinition? CatalogDefinition
    {
        get => m_catalogDefinition;
        set => SetField(ref m_catalogDefinition, value);
    }
    
    public string SqlConnection
    {
        get => m_sqlConnection;
        set => SetField(ref m_sqlConnection, value);
    }

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
