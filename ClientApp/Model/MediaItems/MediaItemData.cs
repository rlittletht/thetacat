using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Thetacat.Import;
using Thetacat.ServiceClient;
using Thetacat.Util;

namespace Thetacat.Model;

public class MediaItemData : INotifyPropertyChanged
{
    private MediaItemState m_state;
    private string m_md5;
    private PathSegment m_virtualPath;
    private Guid m_id;
    private string m_mimeType;
    private ConcurrentDictionary<Guid, MediaTag> m_tags;

#pragma warning disable format // @formatter:off

    public string MimeType                           { get => m_mimeType;    set => SetField(ref m_mimeType, value); }
    public Guid ID                                   { get => m_id;          private set => SetField(ref m_id, value); }
    public PathSegment VirtualPath                   { get => m_virtualPath; set => SetField(ref m_virtualPath, value); }
    public string MD5                                { get => m_md5;         set => SetField(ref m_md5, value); }
    public MediaItemState State                      { get => m_state;       set => SetField(ref m_state, value); }
    public ConcurrentDictionary<Guid, MediaTag> Tags { get => m_tags;        set => SetField(ref m_tags, value); }

#pragma warning restore format // @formatter:on

    public MediaItemData()
    {
        m_id = Guid.NewGuid();
        m_md5 = string.Empty;
        m_mimeType = string.Empty;
        m_virtualPath = PathSegment.Empty;
        m_tags = new ConcurrentDictionary<Guid, MediaTag>();
    }

    public MediaItemData(MediaItemData source)
    {
        m_id = source.m_id;
        m_mimeType = source.m_mimeType;
        m_md5 = source.m_md5;
        m_state = source.m_state;
        m_tags = new ConcurrentDictionary<Guid, MediaTag>(source.Tags);
        m_virtualPath = source.m_virtualPath;
    }

    public MediaItemData(ServiceMediaItem item)
    {
        if (item.Id == null || item.MimeType == null || item.MD5 == null || item.State == null || item.VirtualPath == null)
            throw new ArgumentNullException(nameof(item));

        m_id = item.Id.Value;
        m_mimeType = item.MimeType;
        m_md5 = item.MD5;
        m_state = MediaItem.StateFromString(item.State);
        m_virtualPath = new PathSegment(item.VirtualPath);
        m_tags = new ConcurrentDictionary<Guid, MediaTag>();
    }

    public MediaItemData(ImportItem importItem)
    {
        m_state = MediaItemState.Pending;
        m_md5 = string.Empty;
        m_mimeType = string.Empty;
        m_virtualPath = importItem.SourcePath;
        ID = Guid.NewGuid();
        m_tags = new ConcurrentDictionary<Guid, MediaTag>();
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
