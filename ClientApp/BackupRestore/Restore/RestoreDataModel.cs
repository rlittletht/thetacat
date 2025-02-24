using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using Thetacat.ServiceClient;

namespace Thetacat.BackupRestore.Restore;

public class RestoreDataModel: INotifyPropertyChanged
{
    private string m_restorePath = "catback.xml";
    private bool m_importMediaItems = false;
    private bool m_importMediaStacks = false;
    private bool m_importVersionStacks = false;
    private bool m_importImports = false;
    private bool m_importSchema = false;
    private bool m_importWorkgroups = false;
    private bool m_importWorkgroupData = false;
    private bool m_importDeletedMedia = false;

    public ObservableCollection<string> RestoreBehaviors { get; set; } = new ObservableCollection<string>() { "Append", "Replace", "Create New" };
    private string m_currentRestoreBehavior = "Replace";
    private ServiceCatalogDefinition? m_catalogDefinition;
    private string m_catalogId = string.Empty;
    private string m_catalogName = string.Empty;
    private string m_catalogDescription = string.Empty;
    private bool m_createNewCatalog;
    private bool m_regenerateIds;
    private string m_workgroupId = string.Empty;
    private string m_workgroupName = string.Empty;

    public ObservableCollection<ServiceCatalogDefinition> CatalogDefinitions { get; set; } = new();

    public string WorkgroupId
    {
        get => m_workgroupId;
        set => SetField(ref m_workgroupId, value);
    }

    public string WorkgroupName
    {
        get => m_workgroupName;
        set => SetField(ref m_workgroupName, value);
    }

    public bool ImportDeletedMedia
    {
        get => m_importDeletedMedia;
        set => SetField(ref m_importDeletedMedia, value);
    }

    public bool ImportWorkgroupData
    {
        get => m_importWorkgroupData;
        set => SetField(ref m_importWorkgroupData, value);
    }

    public bool CreateNewCatalog
    {
        get => m_createNewCatalog;
        set => SetField(ref m_createNewCatalog, value);
    }

    public ServiceCatalogDefinition? CatalogDefinition
    {
        get => m_catalogDefinition;
        set => SetField(ref m_catalogDefinition, value);
    }

    public bool RegenerateIds
    {
        get => m_regenerateIds;
        set => SetField(ref m_regenerateIds, value);
    }

    public string CatalogID
    {
        get => m_catalogId;
        set => SetField(ref m_catalogId, value);
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

    public string CurrentRestoreBehavior
    {
        get => m_currentRestoreBehavior;
        set => SetField(ref m_currentRestoreBehavior, value);
    }

    public string RestorePath
    {
        get => m_restorePath;
        set => SetField(ref m_restorePath, value);
    }

    public bool ImportMediaItems
    {
        get => m_importMediaItems;
        set => SetField(ref m_importMediaItems, value);
    }

    public bool ImportMediaStacks
    {
        get => m_importMediaStacks;
        set => SetField(ref m_importMediaStacks, value);
    }

    public bool ImportVersionStacks
    {
        get => m_importVersionStacks;
        set => SetField(ref m_importVersionStacks, value);
    }

    public bool ImportImports
    {
        get => m_importImports;
        set => SetField(ref m_importImports, value);
    }

    public bool ImportSchema
    {
        get => m_importSchema;
        set => SetField(ref m_importSchema, value);
    }

    public bool ImportWorkgroups
    {
        get => m_importWorkgroups;
        set => SetField(ref m_importWorkgroups, value);
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
