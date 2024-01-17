using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.Types;

namespace Thetacat.UI.Options;

public class CacheConfigModel: INotifyPropertyChanged
{
    public string DerivativeLocation
    {
        get => m_derivativeLocation;
        set => SetField(ref m_derivativeLocation, value);
    }

    public class CacheTypeItem
    {
        public Cache.CacheType Type;
        public string Name;

        public CacheTypeItem(Cache.CacheType cacheType, string name)
        {
            Type = cacheType;
            Name = name;
        }

        public override string ToString() => Name;
    }

    public class WorkgroupItem
    {
        public ServiceWorkgroup Workgroup;

        public override string ToString() => Workgroup.Name ?? "";

        public WorkgroupItem(ServiceWorkgroup workgroup)
        {
            Workgroup = workgroup;
        }
    }

    public WorkgroupItem? CurrentWorkgroup
    {
        get => m_currentWorkgroup;
        set => SetField(ref m_currentWorkgroup, value);
    }

    public CacheTypeItem CurrentCacheType
    {
        get => m_cacheType;
        set => SetField(ref m_cacheType, value);
    }

    private static readonly CacheTypeItem s_cacheTypePrivate =new CacheTypeItem(Cache.CacheType.Private, "Private");
    private static readonly CacheTypeItem s_cacheTypeWorkgroyup = new CacheTypeItem(Cache.CacheType.Workgroup, "Workgroup");

    private static readonly List<CacheTypeItem> s_cacheTypes =
        new()
        {
            s_cacheTypePrivate,
            s_cacheTypeWorkgroyup
        };

    public List<CacheTypeItem> CacheTypes => s_cacheTypes;
        
    private string m_cacheLocation = string.Empty;

    private ObservableCollection<WorkgroupItem> m_workgroups = new ObservableCollection<WorkgroupItem>();

    public ObservableCollection<WorkgroupItem> Workgroups => m_workgroups;

    private string m_workgroupId = string.Empty;
    private string m_workgroupName = string.Empty;
    private string m_workgroupServerPath = string.Empty;
    private string m_workgroupCacheRoot = string.Empty;
    private CacheTypeItem m_cacheType = s_cacheTypePrivate;
    private WorkgroupItem? m_currentWorkgroup;
    private string m_workgroupItemName = string.Empty;
    private string m_derivativeLocation = string.Empty;

    public string WorkgroupCacheRoot
    {
        get => m_workgroupCacheRoot;
        set => SetField(ref m_workgroupCacheRoot, value);
    }

    public string WorkgroupServerPath
    {
        get => m_workgroupServerPath;
        set => SetField(ref m_workgroupServerPath, value);
    }

    public string WorkgroupName
    {
        get => m_workgroupName;
        set => SetField(ref m_workgroupName, value);
    }

    public string WorkgroupID
    {
        get => m_workgroupId;
        set => SetField(ref m_workgroupId, value);
    }

    public string CacheLocation
    {
        get => m_cacheLocation;
        set => SetField(ref m_cacheLocation, value);
    }

    public string WorkgroupItemName
    {
        get => m_workgroupItemName;
        set => SetField(ref m_workgroupItemName, value);
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

    public void PopulateWorkgroups()
    {
        foreach (ServiceWorkgroup workgroup in ServiceInterop.GetAvailableWorkgroups())
        {
            Workgroups.Clear();
            Workgroups.Add(new WorkgroupItem(workgroup));
        }
    }

    WorkgroupItem? GetWorkgroupInfoFromId(Guid id)
    {
        foreach (WorkgroupItem workgroup in m_workgroups)
        {
            if (id == workgroup.Workgroup.ID)
                return workgroup;
        }

        MessageBox.Show($"workgroup {id} not found but we knew about it?");
        return null;
    }

    public Guid? GetWorkgroupIdFromName(string name)
    {
        foreach (WorkgroupItem workgroup in m_workgroups)
        {
            if (string.Compare(workgroup.Workgroup.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0)
                return workgroup.Workgroup.ID;
        }

        return null;
    }

    public void SetWorkgroup(Guid? id)
    {
        if (id == null)
        {
            CurrentWorkgroup = null;
            WorkgroupItemName = string.Empty;
            WorkgroupID = string.Empty;
            WorkgroupCacheRoot = string.Empty;
            WorkgroupName = string.Empty;
            WorkgroupServerPath = string.Empty;
            return;
        }

        if (string.IsNullOrWhiteSpace(App.State.Settings.SqlConnection))
            return;

        try
        {
            WorkgroupItem? workgroup = GetWorkgroupInfoFromId(id.Value);

            if (workgroup == null)
                return;

            if (CurrentWorkgroup != workgroup)
                CurrentWorkgroup = workgroup;

            WorkgroupItemName = workgroup.Workgroup?.Name ?? throw new CatExceptionServiceDataFailure();
            WorkgroupID = workgroup.Workgroup.ID?.ToString() ?? throw new CatExceptionServiceDataFailure();
            WorkgroupCacheRoot = workgroup.Workgroup.CacheRoot ?? throw new CatExceptionServiceDataFailure();
            WorkgroupName = workgroup.Workgroup.Name ?? throw new CatExceptionServiceDataFailure();
            WorkgroupServerPath = workgroup.Workgroup.ServerPath ?? throw new CatExceptionServiceDataFailure();
        }
        catch (CatExceptionNoSqlConnection)
        {
            return;
        }
    }

    public void SetCacheTypeFromString(string type)
    {
        Cache.CacheType cacheType = Cache.CacheTypeFromString(type);

        foreach (CacheTypeItem item in CacheTypes)
        {
            if (item.Type == cacheType)
            {
                CurrentCacheType = item;
                return;
            }
        }
    }
}
