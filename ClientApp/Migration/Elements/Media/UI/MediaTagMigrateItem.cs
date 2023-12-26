using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.Util;

namespace Thetacat.Migration.Elements.Media.UI;

public class MediaTagMigrateItem: INotifyPropertyChanged, ICheckableListViewItem
{
    private Guid m_mediaId;
    private PathSegment m_virtualPath;
    private Metatag m_metatagSetting;
    private string? m_value;
    private bool m_include;

    public bool Checked
    {
        get => m_include;
        set => SetField(ref m_include, value);
    }

    public Guid MediaID
    {
        get => m_mediaId;
        set => SetField(ref m_mediaId, value);
    }

    public PathSegment VirtualPath
    {
        get => m_virtualPath;
        set => SetField(ref m_virtualPath, value);
    }

    public Metatag MetatagSetting
    {
        get => m_metatagSetting;
        set => SetField(ref m_metatagSetting, value);
    }

    public string? Value
    {
        get => m_value;
        set => SetField(ref m_value, value);
    }

    public MediaTagMigrateItem(MediaItem catItem, Metatag metatagSetting, string? value)
    {
        m_mediaId = catItem.ID;
        m_virtualPath = catItem.VirtualPath;
        m_metatagSetting = metatagSetting;
        m_value = value;
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
