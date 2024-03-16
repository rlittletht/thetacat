using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Thetacat.BackupRestore.Backup;

public class ExportDataModel: INotifyPropertyChanged
{
    private string m_exportPath = "catback.xml";
    private bool m_exportMediaItems = true;
    private bool m_exportMediaStacks = true;
    private bool m_exportVersionStacks = true;
    private bool m_exportImports = true;
    private bool m_exportSchema = true;
    private bool m_exportWorkgroups = false;

    public string ExportPath
    {
        get => m_exportPath;
        set => SetField(ref m_exportPath, value);
    }

    public bool ExportMediaItems
    {
        get => m_exportMediaItems;
        set => SetField(ref m_exportMediaItems, value);
    }

    public bool ExportMediaStacks
    {
        get => m_exportMediaStacks;
        set => SetField(ref m_exportMediaStacks, value);
    }

    public bool ExportVersionStacks
    {
        get => m_exportVersionStacks;
        set => SetField(ref m_exportVersionStacks, value);
    }

    public bool ExportImports
    {
        get => m_exportImports;
        set => SetField(ref m_exportImports, value);
    }

    public bool ExportSchema
    {
        get => m_exportSchema;
        set => SetField(ref m_exportSchema, value);
    }

    public bool ExportWorkgroups
    {
        get => m_exportWorkgroups;
        set => SetField(ref m_exportWorkgroups, value);
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
