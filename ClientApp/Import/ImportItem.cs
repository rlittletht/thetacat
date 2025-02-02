﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.Util;

namespace Thetacat.Import;

public class ImportItem: INotifyPropertyChanged
{
    public enum ImportState
    {
        PendingMediaCreate,
        PendingUpload,
        Complete,
        MissingFromCatalog,
        PendingRepair,
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
            case "PENDING-REPAIR":
                return ImportState.PendingRepair;
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
            case ImportState.PendingRepair:
                return "pending-repair";
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
    private object? m_sourceObject;
    private PathSegment m_virtualPath;

    public bool SkipWorkgroupOnlyItem
    {
        get => m_skipWorkgroupOnlyItem;
        set => SetField(ref m_skipWorkgroupOnlyItem, value);
    }

    public PathSegment VirtualPath { get => m_virtualPath; set => SetField(ref m_virtualPath, value); }
    public string Source { get => m_source; set => SetField(ref m_source, value); }
    public DateTime UploadDate { get => m_uploadDate; set => SetField(ref m_uploadDate, value); }
    public PathSegment SourceServer { get => m_sourceServer; set => SetField(ref m_sourceServer, value); }
    public PathSegment SourcePath { get => m_sourcePath; set => SetField(ref m_sourcePath, value); }
    public PathSegment FullSourcePath => PathSegment.Join(SourceServer, SourcePath);

    public ImportState State { get => m_state; set => SetField(ref m_state, value); }
    public Guid ID { get => m_id; set => SetField(ref m_id, value); }
    public MediaImporter.NotifyCatalogItemCreatedOrRepairedDelegate? m_onCatalogItemCreated;
    private bool m_skipWorkgroupOnlyItem;

    public ImportItem(ServiceImportItem item)
    {
        ID = item.ID;
        m_source = new PathSegment(item.Source ?? "");
        m_sourceServer = new PathSegment(item.SourceServer ?? "");
        m_sourcePath = new PathSegment(item.SourcePath ?? "");
        m_state = StateFromString(item.State ?? "");
        m_virtualPath = m_sourcePath;
    }

    public ImportItem(Guid id, string source, PathSegment sourceServer, PathSegment sourcePath, ImportState state, object? sourceObject = null, MediaImporter.NotifyCatalogItemCreatedOrRepairedDelegate? onCatalogItemCreated = null)
    {
        ID = id;
        m_source = source;
        m_sourceServer = sourceServer;
        m_sourcePath = sourcePath;
        m_state = state;
        m_onCatalogItemCreated = onCatalogItemCreated;
        m_sourceObject = sourceObject;
        m_virtualPath = sourcePath;
    }

    public ImportItem(Guid id, string source, PathSegment sourceServer, PathSegment sourcePath, PathSegment virtualPath, ImportState state, object? sourceObject = null, MediaImporter.NotifyCatalogItemCreatedOrRepairedDelegate? onCatalogItemCreated = null)
    {
        ID = id;
        m_source = source;
        m_sourceServer = sourceServer;
        m_sourcePath = sourcePath;
        m_state = state;
        m_onCatalogItemCreated = onCatalogItemCreated;
        m_sourceObject = sourceObject;
        m_virtualPath = virtualPath;
    }

    public void SetPathsFromFullPath(PathSegment fullPath)
    {
        SourceServer = fullPath.GetPathRoot() ?? new PathSegment();
        SourcePath = fullPath.GetRelativePath(SourceServer);
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

    public void NotifyMediaItemCreated(MediaItem item)
    {
        m_onCatalogItemCreated?.Invoke(m_sourceObject, item);
    }
}
