using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;

namespace Thetacat.BackupRestore.Restore;

public class RestoreDataModel: INotifyPropertyChanged
{
    private string m_restorePath = "catback.xml";
    private bool m_importMediaItems = true;
    private bool m_importMediaStacks = true;
    private bool m_importVersionStacks = true;
    private bool m_importImports = true;
    private bool m_importSchema = true;
    private bool m_importWorkgroups = false;

    public ObservableCollection<string> RestoreBehaviors { get; set; } = new ObservableCollection<string>() { "Append", "Replace" };
    private string m_currentRestoreBehavior = "Replace";

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
