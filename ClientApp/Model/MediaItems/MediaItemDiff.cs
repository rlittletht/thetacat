﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Thetacat.Model;

/*----------------------------------------------------------------------------
    %%Class: MediaItemDiff
    %%Qualified: Thetacat.Model.MediaItemDiff

    Represents a diff to a media item
----------------------------------------------------------------------------*/
public class MediaItemDiff: INotifyPropertyChanged
{
    public enum Op
    {
        Insert,
        Update,
        Delete
    }

    [Flags]
    public enum UpdatedValues
    {
        MimeType = 1,
        Path,
        MD5,
        State,
        Tags
    }

    public bool IsMimeTypeChanged => (PropertiesChanged & UpdatedValues.MimeType) != 0;
    public bool IsPathChanged => (PropertiesChanged & UpdatedValues.Path) != 0;
    public bool IsMD5Changed => (PropertiesChanged & UpdatedValues.MD5) != 0;
    public bool IsStateChanged => (PropertiesChanged & UpdatedValues.State) != 0;
    public bool IsTagsChanged => (PropertiesChanged & UpdatedValues.Tags) != 0;


    public Op DiffOp { get; set; }
    public UpdatedValues PropertiesChanged { get; set; }

    public Guid ID { get; set; }
    public MediaItemData? ItemData { get; set; }
    
    public List<MediaTagDiff>? TagDiffs { get; set; }

    public MediaItemDiff(Guid id)
    {
        ID = id;
    }

    public static MediaItemDiff CreateDelete(Guid id)
    {
        return
            new MediaItemDiff(id)
            {
                DiffOp = Op.Delete
            };
    }

    public static MediaItemDiff CreateInsert(MediaItem item)
    {
        return
            new MediaItemDiff(item.ID)
            {
                DiffOp = Op.Insert,
                ItemData = item.Data
            };
    }

    public static MediaItemDiff CreateUpdate(MediaItem item)
    {
        MediaItemDiff diff =
            new MediaItemDiff(item.ID)
            {
                DiffOp = Op.Insert,
                ItemData = item.Data
            };

        if (item.Base.MD5 != item.MD5)
            diff.PropertiesChanged |= UpdatedValues.MD5;
        if (string.Compare(item.Base.MimeType, item.MimeType, StringComparison.InvariantCultureIgnoreCase) != 0)
            diff.PropertiesChanged |= UpdatedValues.MimeType;
        if (item.Base.State != item.State)
            diff.PropertiesChanged |= UpdatedValues.State;
        if (item.Base.VirtualPath != item.VirtualPath)
            diff.PropertiesChanged |= UpdatedValues.Path;

        // diff the metatags to find differences
        diff.TagDiffs = new List<MediaTagDiff>();

        bool tagDifferences = false;
        // find the inserts and the updates
        foreach (KeyValuePair<Guid, MediaTag> tag in item.Tags)
        {
            if (!item.Base.Tags.ContainsKey(tag.Key))
            {
                diff.TagDiffs.Add(MediaTagDiff.CreateInsert(tag.Value));
                tagDifferences = true;
            }
            else
            {
                if (string.Compare(tag.Value.Value, item.Base.Tags[tag.Key].Value, StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    diff.TagDiffs.Add(MediaTagDiff.CreateUpdate(tag.Value));
                    tagDifferences = true;
                }
            }
        }

        // find the deletes
        foreach (KeyValuePair<Guid, MediaTag> tag in item.Base.Tags)
        {
            if (!item.Tags.ContainsKey(tag.Key))
            {
                diff.TagDiffs.Add(MediaTagDiff.CreateDelete(tag.Key));
                tagDifferences = true;
            }
        }

        if (tagDifferences)
            diff.PropertiesChanged |= UpdatedValues.Tags;
        return diff;
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
