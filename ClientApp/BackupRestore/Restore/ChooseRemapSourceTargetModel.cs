using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.BackupRestore.Restore;

public class ChooseRemapSourceTargetModel: INotifyPropertyChanged
{
    private ObservableCollection<string> m_profiles = new();

    private string m_sourceSqlConnection = string.Empty;
    private string m_sourceWorkgroupName = string.Empty;
    private string m_sourceWorkgroupId = string.Empty;
    private string m_sourceCatalogId = string.Empty;
    private string m_sourceProfile = string.Empty;

    private string m_targetSqlConnection = string.Empty;
    private string m_targetWorkgroupId = string.Empty;
    private string m_targetWorkgroupName = string.Empty;
    private string m_targetCatalogId = string.Empty;
    private string m_targetProfile = string.Empty;
    private string m_guidMapExportPath = string.Empty;
    private string m_sourceAzureStorage = string.Empty;
    private string m_targetAzureStorage = string.Empty;
    private string m_sourceAzureContainer = string.Empty;
    private string m_targetAzureContainer = string.Empty;
    private bool m_migrateAzureBlobs = true;
    private bool m_migrateWorkgroup = true;

    public bool MigrateAzureBlobs
    {
        get => m_migrateAzureBlobs;
        set => SetField(ref m_migrateAzureBlobs, value);
    }

    public bool MigrateWorkgroup
    {
        get => m_migrateWorkgroup;
        set => SetField(ref m_migrateWorkgroup, value);
    }

    public string TargetAzureStorage
    {
        get => m_targetAzureStorage;
        set => SetField(ref m_targetAzureStorage, value);
    }

    public string SourceAzureStorage
    {
        get => m_sourceAzureStorage;
        set => SetField(ref m_sourceAzureStorage, value);
    }

    public string TargetAzureContainer
    {
        get => m_targetAzureContainer;
        set => SetField(ref m_targetAzureContainer, value);
    }

    public string SourceAzureContainer
    {
        get => m_sourceAzureContainer;
        set => SetField(ref m_sourceAzureContainer, value);
    }

    public string GuidMapExportPath
    {
        get => m_guidMapExportPath;
        set => SetField(ref m_guidMapExportPath, value);
    }

    public ObservableCollection<string> Profiles
    {
        get => m_profiles;
        set => SetField(ref m_profiles, value);
    }

    public string SourceProfile
    {
        get => m_sourceProfile;
        set => SetField(ref m_sourceProfile, value);
    }

    public string TargetProfile
    {
        get => m_targetProfile;
        set => SetField(ref m_targetProfile, value);
    }

    public string TargetCatalogID
    {
        get => m_targetCatalogId;
        set => SetField(ref m_targetCatalogId, value);
    }

    public string SourceCatalogID
    {
        get => m_sourceCatalogId;
        set
        {
            if (value == m_sourceCatalogId) return;
            m_sourceCatalogId = value;
            OnPropertyChanged();
        }
    }

    public string SourceSqlConnection
    {
        get => m_sourceSqlConnection;
        set => SetField(ref m_sourceSqlConnection, value);
    }

    public string SourceWorkgroupName
    {
        get => m_sourceWorkgroupName;
        set => SetField(ref m_sourceWorkgroupName, value);
    }

    public string SourceWorkgroupId
    {
        get => m_sourceWorkgroupId;
        set => SetField(ref m_sourceWorkgroupId, value);
    }

    public string TargetSqlConnection
    {
        get => m_targetSqlConnection;
        set => SetField(ref m_targetSqlConnection, value);
    }

    public string TargetWorkgroupId
    {
        get => m_targetWorkgroupId;
        set => SetField(ref m_targetWorkgroupId, value);
    }

    public string TargetWorkgroupName
    {
        get => m_targetWorkgroupName;
        set => SetField(ref m_targetWorkgroupName, value);
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
