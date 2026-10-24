using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Types;

namespace Thetacat.Triage;

public enum DownloadFilenameType
{
    LeafOnly,
    FlattenedPath,
}

public class DownloadFilenameTypeItem
{
    public DownloadFilenameTypeItem(DownloadFilenameType value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public DownloadFilenameType Value { get; }
    public string DisplayName { get; }
    
    public static string StringFromType(DownloadFilenameType value)
    {
        switch (value)
        {
            case DownloadFilenameType.LeafOnly:
                return "Leaf Only";
            case DownloadFilenameType.FlattenedPath:
                return "Flattened Path";
            default:
                throw new CatExceptionInternalFailure($"Unexpected DownloadFilenameType value: {value}");
        }
    }
}

public class DownloadMediaModel: INotifyPropertyChanged
{
    private string m_downloadTo = string.Empty;
    private bool m_triage;
    private bool m_replaceExisting;
    private DownloadFilenameTypeItem? m_filenameType;

    public DownloadMediaModel()
    {
        FilenameTypes = new List<DownloadFilenameTypeItem>()
        {
            new DownloadFilenameTypeItem(DownloadFilenameType.LeafOnly, "Leaf Only"),
            new DownloadFilenameTypeItem(DownloadFilenameType.FlattenedPath, "Flattened Path"),
        };

        FilenameType = FilenameTypes[0];
    }

    public List<DownloadFilenameTypeItem> FilenameTypes { get; }

    public DownloadFilenameTypeItem GetFilenameTypeForDisplayString(string value)
    {
        foreach (DownloadFilenameTypeItem item in FilenameTypes)
        {
            if (item.DisplayName == value)
                return item;
        }

        throw new CatExceptionInternalFailure($"Unexpected DownloadFilenameType value: {value}");
    }
    
    public DownloadFilenameTypeItem? FilenameType
    {
        get => m_filenameType;
        set => SetField(ref m_filenameType, value);
    }

    public bool ReplaceExisting
    {
        get => m_replaceExisting;
        set => SetField(ref m_replaceExisting, value);
    }

    public bool Triage
    {
        get => m_triage;
        set => SetField(ref m_triage, value);
    }

    public string DownloadTo
    {
        get => m_downloadTo;
        set => SetField(ref m_downloadTo, value);
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
