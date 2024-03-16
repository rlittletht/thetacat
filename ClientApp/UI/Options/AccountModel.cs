using System;
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
    private Guid m_currentCatalogId;
    private string m_catalogName = string.Empty;
    private string m_catalogDescription = string.Empty;
    private bool m_createNewCatalog = false;

    public ObservableCollection<ServiceCatalogDefinition> CatalogDefinitions { get; set; } = new();

    public bool CreateNewCatalog
    {
        get => m_createNewCatalog;
        set => SetField(ref m_createNewCatalog, value);
    }

    public string CatalogName
    {
        get => m_catalogName;
        set => SetField(ref m_catalogName, value);
    }

    public string CatalogDescription
    {
        get => m_catalogDescription;
        set => SetField(ref m_catalogDescription, value);
    }

    // we have to duplicate this because we might not have loaded the
    // catalog definitions in time to set the catalogID (and we will need to know
    // what the current catalog ID is when we load the catalog definitions)
    public Guid CurrentCatalogID
    {
        get => m_currentCatalogId;
        set
        {
            MatchCatalogDefinition(value);
            SetField(ref m_currentCatalogId, value);
        }
    }

    public ServiceCatalogDefinition? CatalogDefinition
    {
        get => m_catalogDefinition;
        set
        {
            if (value != null)
                SetField(ref m_currentCatalogId, value.ID, "CurrentCatalogID");

            SetField(ref m_catalogDefinition, value);
        }
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

    private void MatchCatalogDefinition(Guid? catalogID)
    {
        foreach (ServiceCatalogDefinition item in CatalogDefinitions)
        {
            if (item.ID == catalogID)
            {
                CatalogDefinition = item;
                return;
            }
        }

        // if we get here, we failed to match the definition
        CatalogDefinition = null;
    }

}
