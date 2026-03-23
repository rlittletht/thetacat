using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.UI.Options;

public class CreateWorkgroupModel: INotifyPropertyChanged
{
    private string m_workgroupId = string.Empty;
    private string m_workgroupName = string.Empty;
    private string m_workgroupServerPath = string.Empty;
    private string m_workgroupCacheRoot = string.Empty;

    public string WorkgroupID
    {
        get => m_workgroupId;
        set => SetField(ref m_workgroupId, value);
    }

    public string WorkgroupName
    {
        get => m_workgroupName;
        set => SetField(ref m_workgroupName, value);
    }

    public string WorkgroupServerPath
    {
        get => m_workgroupServerPath;
        set => SetField(ref m_workgroupServerPath, value);
    }

    public string WorkgroupCacheRoot
    {
        get => m_workgroupCacheRoot;
        set => SetField(ref m_workgroupCacheRoot, value);
    }

    public bool CreateNewWorkgroup => true;

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
