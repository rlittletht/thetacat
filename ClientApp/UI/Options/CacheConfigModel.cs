using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;

namespace Thetacat.UI.Options;

public class CacheConfigModel: INotifyPropertyChanged
{
    private string m_cacheLocation = string.Empty;
    private string m_cacheType = "Private";

    private List<ServiceWorkgroup>? m_workgroups;

    public ObservableCollection<string> Workgroups = new ObservableCollection<string>();
    private string m_workgroupId = string.Empty;
    private string m_workgroupName = string.Empty;
    private string m_workgroupServerPath = string.Empty;
    private string m_workgroupCacheRoot = string.Empty;

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

    public string CacheType
    {
        get => m_cacheType;
        set => SetField(ref m_cacheType, value);
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
        m_workgroups = ServiceInterop.GetAvailableWorkgroups();
        Workgroups.Clear();
        foreach (ServiceWorkgroup workgroup in m_workgroups)
        {
            Workgroups.Add(workgroup?.Name ?? "<unknown>");
        }
    }

    public Guid? GetWorkgroupIdFromName(string name)
    {
        if (m_workgroups == null)
            return null;

        foreach (ServiceWorkgroup workgroup in m_workgroups)
        {
            if (string.Compare(workgroup.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0)
                return workgroup.ID;
        }

        return null;
    }
}
