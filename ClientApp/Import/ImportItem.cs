using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Util;

namespace Thetacat.Import;

public class ImportItem: INotifyPropertyChanged
{
    public enum ImportState
    {
        PendingMediaCreate,
        PendingUpload,
        Complete,
        Unknown
    }

    public static ImportState StateFromString(string state)
    {
        switch (state.ToUpper())
        {
            case "PENDING-CREATE":
                return ImportState.PendingMediaCreate;
            case "PENDING-UPLOAD":
                return ImportState.PendingUpload;
            case "COMPLETE":
                return ImportState.Complete;
        }

        return ImportState.Unknown;
    }

    public static string StringFromState(ImportState state)
    {
        switch (state)
        {
            case ImportState.PendingMediaCreate:
                return "pending-create";
            case ImportState.PendingUpload:
                return "pending-upload";
            case ImportState.Complete:
                return "complete";
            default:
                return "unknown";
        }
    }

    private Guid m_id;
    private ImportState m_state;
    private PathSegment m_sourcePath;
    private PathSegment m_sourceServer;
    private DateTime m_uploadDate;
    private string m_source;

    public string Source { get => m_source; set => SetField(ref m_source, value); }
    public DateTime UploadDate { get => m_uploadDate; set => SetField(ref m_uploadDate, value); }
    public PathSegment SourceServer { get => m_sourceServer; set => SetField(ref m_sourceServer, value); }
    public PathSegment SourcePath { get => m_sourcePath; set => SetField(ref m_sourcePath, value); }
    public ImportState State { get => m_state; set => SetField(ref m_state, value); }
    public Guid ID { get => m_id; set => SetField(ref m_id, value); }

    public ImportItem(Guid id, string source, PathSegment sourceServer, PathSegment sourcePath)
    {
        ID = id;
        m_source = source;
        m_sourceServer = sourceServer;
        m_sourcePath = sourcePath;
        m_state = ImportState.PendingMediaCreate;
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
