﻿using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using NUnit.Framework.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Markup;
using Thetacat.Import;
using Thetacat.Logging;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Util;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Thetacat.Model;

public class MediaItem : INotifyPropertyChanged
{

    public enum Op
    {
        Create,
        Delete,
        MaybeUpdate
    };

    private MediaItemData? m_base;
    private readonly MediaItemData m_working;
    private bool m_isCachePending = false;
    private string m_localPath = string.Empty;

    public MediaItem()
    {
        m_working = new MediaItemData();
    }

    public MediaItem(ImportItem importItem)
    {
        m_working = new MediaItemData(importItem);
    }

    public MediaItem(ServiceMediaItem item)
    {
        m_working = new MediaItemData(item);
    }

#region Public Data / Accessors

    // the vector clock changes whenever a change is made to the data. we use this
    // clock to determine if a diffitem (when committed) should clear any changes to
    // this item

    public int VectorClock = 0;

    public MediaItemData Data => m_working;

    public string LocalPath
    {
        get => m_localPath;
        set => SetField(ref m_localPath, value);
    }

    // this means we are waiting for this item to be cached. maybe by this client,
    // maybe by another client
    public bool IsCachePending
    {
        get => m_isCachePending;
        set => SetField(ref m_isCachePending, value);
    }

    public Op PendingOp { get; set; } = Op.MaybeUpdate;

    public string MimeType
    {
        get => m_working.MimeType;
        set
        {
            EnsureBase();
            VectorClock++;
            m_working.MimeType = value;
        }
    }

    public Guid ID => m_working.ID;

    public PathSegment VirtualPath
    {
        get => m_working.VirtualPath;
        private set
        {
            EnsureBase();
            VectorClock++;
            m_working.VirtualPath = value;
        }
    }

    public string MD5
    {
        get => m_working.MD5;
        set
        {
            EnsureBase();
            VectorClock++;
            m_working.MD5 = value;
        }
    }

    public MediaItemState State
    {
        get => m_working.State;
        set
        {
            EnsureBase();
            m_working.State = value;
            VectorClock++;
            OnPropertyChanged(nameof(State));
        }
    }

    public ConcurrentDictionary<Guid, MediaTag> Tags
    {
        get => m_working.Tags;
        set
        {
            EnsureBase();
            VectorClock++;
            m_working.Tags = value;
        }
    }

    public int? ImageWidth
    {
        get
        {
            if (Tags.TryGetValue(BuiltinTags.s_Width.ID, out MediaTag? tag))
                return int.Parse(tag.Value ?? "");

            return null;
        }
        set
        {
            MediaTag tag = new MediaTag(BuiltinTags.s_Width, value.ToString());
            FAddOrUpdateMediaTag(tag);
            OnPropertyChanged(nameof(ImageWidth));
        }
    }

    public int? ImageHeight
    {
        get
        {
            if (Tags.TryGetValue(BuiltinTags.s_Height.ID, out MediaTag? tag))
                return int.Parse(tag.Value ?? "");

            return null;
        }
        set
        {
            MediaTag tag = new MediaTag(BuiltinTags.s_Height, value.ToString());
            FAddOrUpdateMediaTag(tag);
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

    public DateTime? OriginalFileDate
    {
        get
        {
            if (Tags.TryGetValue(BuiltinTags.s_OriginalFileDateID, out MediaTag? tag))
                return tag.Value == null ? null : DateTime.Parse(tag.Value);

            return null;
        }
        set
        {
            MediaTag tag = new MediaTag(BuiltinTags.s_OriginalFileDate, value?.ToUniversalTime().ToString("u"));
            FAddOrUpdateMediaTag(tag);
            OnPropertyChanged(nameof(OriginalFileDate));
        }
    }

    #endregion

    #region Changes/Versions

    public bool MaybeHasChanges => m_base != null;

    void EnsureBase()
    {
        m_base ??= new(m_working);
    }

    public MediaItemData Base => m_base ?? throw new CatExceptionInternalFailure("no base to fetch");

    public void ResetPendingChanges()
    {
        PendingOp = Op.MaybeUpdate;
        m_base = null;
    }

#endregion

#region Cache Status/Media State

    public string CacheStatus => MainWindow._AppState.Cache.IsItemCached(ID) ? "cached" : "<No Cache>";

    public void NotifyCacheStatusChanged()
    {
        OnPropertyChanged(nameof(CacheStatus));
    }

    public static string StringFromState(MediaItemState state)
    {
        switch (state)
        {
            case MediaItemState.Active:
                return "active";
            case MediaItemState.Pending:
                return "pending";
        }

        return "unknown";
    }

    public static MediaItemState StateFromString(string state)
    {
        switch (state.ToUpper())
        {
            case "ACTIVE":
                return MediaItemState.Active;
            case "PENDING":
                return MediaItemState.Pending;
        }

        return MediaItemState.Unknown;
    }

#endregion

#region MediaTags

    /*----------------------------------------------------------------------------
        %%Function: PopulateMediaTagsFromMetadataDirectory
        %%Qualified: Thetacat.Model.MediaItem.PopulateMediaTagsFromMetadataDirectory

        Returns true if there were any changes made to this items tags.
        (schema changes will be caught later)
    ----------------------------------------------------------------------------*/
    public bool PopulateMediaTagsFromMetadataDirectory(
        MetatagSchema metatagSchema,
        Metatag? parent,
        MetadataExtractor.Directory directory,
        MetatagStandards.Standard standard,
        List<string>? log = null)
    {
        bool changed = false;

        if (parent == null && standard == MetatagStandards.Standard.Unknown)
            standard = MetatagStandards.GetStandardFromType(directory.GetType().Name);

        if (standard == MetatagStandards.Standard.Unknown)
        {
#if verbose_standard
            MainWindow.LogForApp(EventType.Verbose, $"unknown standard directory {directory.Name}");
            log?.Add($"unknown standard directory {directory.Name}");
#endif
            return false;
        }

        StandardDefinitions standardDefinitions = MetatagStandards.GetStandardMappings(standard);

        // match the current directory to a metatag
        Metatag dirTag = metatagSchema.GetOrBuildDirectoryTag(parent, standard, directory.Name);

        foreach (Tag tag in directory.Tags)
        {
            if (!standardDefinitions.Properties.TryGetValue(tag.Type, out StandardDefinition? def))
            {
#if verbose_standard
                MainWindow.LogForApp(EventType.Verbose, $"unknown metatag {tag.Name} in standard {standardDefinitions.StandardId}");
                log?.Add($"unknown metatag {tag.Name} in standard {standardDefinitions.StandardId}");
#endif
                continue;
            }

            if (!def.Include)
                continue;
            Metatag? metatag = metatagSchema.FindByName(dirTag, def.PropertyTagName);

            string value = tag.Description ?? string.Empty;

            if (string.IsNullOrEmpty(value) || value == " " || value == "  ")
                continue;

            value = value.Replace(" pixels", "");

            if (metatag == null)
            {
                // need to create a new one
                metatag = Metatag.Create(dirTag?.ID, def.PropertyTagName, tag.Name, standard);
                metatagSchema.AddMetatag(metatag);
            }

            MediaTag newTag = new MediaTag(metatag, value);

            if (FAddOrUpdateMediaTag(newTag, log))
                changed = true;
        }

        return changed;
    }

    /*----------------------------------------------------------------------------
        %%Function: FAddOrUpdateMediaTag
        %%Qualified: Thetacat.Model.MediaItem.FAddOrUpdateMediaTag

        Add or updated a media tag
    ----------------------------------------------------------------------------*/
    public bool FAddOrUpdateMediaTag(MediaTag tag, List<string>? log = null)
    {
        bool ensuredBase = m_base == null;

        EnsureBase();

        bool identicalExisting = false;

        Tags.AddOrUpdate(
            tag.Metatag.ID,
            tag,
            (key, oldMediaTag) =>
            {
                if (oldMediaTag.Value != tag.Value)
                {
                    MainWindow.LogForApp(EventType.Verbose, $"Different metatag value for {key}: {oldMediaTag.Value} != {tag.Value}");
                    log?.Add($"Different metatag value for {key}: {oldMediaTag.Value} != {tag.Value}");
                    oldMediaTag.Value = tag.Value;
                }
                else
                {
                    identicalExisting = true;
                }

                return oldMediaTag;
            });

        if (identicalExisting && ensuredBase)
        {
            // well drat, we ended up not needing to ensure the base (because the tag was
            // identical). free the base so we don't think there's a difference
            ResetPendingChanges();
        }

        if (!identicalExisting)
            VectorClock++;

        return !identicalExisting;
    }


    Tuple<int, int>? TryGetWidthFromStandardAndType(MetatagSchema schema, MetatagStandards.Standard standard, int widthType, int heightType)
    {
        Metatag? tagWidth = schema.FindStandardItemFromStandardAndType(standard, widthType);
        if (tagWidth == null)
            return null;

        Metatag? tagHeight = schema.FindStandardItemFromStandardAndType(standard, heightType);

        if (tagHeight == null)
            return null;

        if (!Tags.TryGetValue(tagWidth.ID, out MediaTag? matchWidth))
            return null;

        if (!Tags.TryGetValue(tagHeight.ID, out MediaTag? matchHeight))
            return null;

        return new Tuple<int, int>(int.Parse(matchWidth.Value ?? ""), int.Parse(matchHeight.Value ?? ""));
    }

    string? TryGetMediaTagValueFromStandardAndType(MetatagSchema schema, MetatagStandards.Standard standard, int type)
    {
        Metatag? tag = schema.FindStandardItemFromStandardAndType(standard, type);
        if (tag == null)
            return null;

        if (!Tags.TryGetValue(tag.ID, out MediaTag? match))
            return null;

        return match.Value;
    }

    /*----------------------------------------------------------------------------
        %%Function: FindWidthHeightFromMediaTags
        %%Qualified: Thetacat.Model.MediaItem.FindWidthHeightFromMediaTags

        There are a lot of different mediatag items for width. Find at least one
        of them so we can promote to our builtin width/height
    ----------------------------------------------------------------------------*/
    Tuple<int, int>? FindWidthHeightFromMediaTags(MetatagSchema schema)
    {
        Tuple<int, int>? widthHeight;

        widthHeight = TryGetWidthFromStandardAndType(
            schema,
            MetatagStandards.Standard.Jpeg,
            MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageWidth,
            MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageHeight);

        if (widthHeight != null)
            return widthHeight;

        widthHeight = TryGetWidthFromStandardAndType(
            schema,
            MetatagStandards.Standard.Exif,
            MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageWidth,
            MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageHeight);

        if (widthHeight != null)
            return widthHeight;

        return null;
    }

    /*----------------------------------------------------------------------------
        %%Function: SetMediaTagsFromFileMetadata
        %%Qualified: Thetacat.Model.MediaItem.SetMediaTagsFromFileMetadata

        Parse the file for this media item and extract all the mediatags
        (that we have mappings for)
    ----------------------------------------------------------------------------*/
    public List<string>? SetMediaTagsFromFileMetadata(MetatagSchema metatagSchema)
    {
        List<string> log = new List<string>();

        string file = LocalPath;

        // load exif and other data from this item.
        IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(file);

        this.MD5 = CalculateMD5Hash(file);

        foreach (MetadataExtractor.Directory directory in directories)
        {
            PopulateMediaTagsFromMetadataDirectory(metatagSchema, null, directory, MetatagStandards.Standard.Unknown, log);
        }

        metatagSchema.EnsureBuiltinMetatagsDefined();

        // lastly, set the builtin items
        Tuple<int, int>? widthHeight = FindWidthHeightFromMediaTags(metatagSchema);
        if (widthHeight != null)
        {
            FAddOrUpdateMediaTag(new MediaTag(BuiltinTags.s_Width, widthHeight.Item1.ToString()));
            FAddOrUpdateMediaTag(new MediaTag(BuiltinTags.s_Height, widthHeight.Item2.ToString()));
        }

        DateTime createTime = File.GetCreationTime(file);
        FAddOrUpdateMediaTag(new MediaTag(BuiltinTags.s_OriginalFileDate, createTime.ToUniversalTime().ToString("u")));

        return log;
    }

#endregion

#region INotifyPropertyChanged

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

#endregion

    /*----------------------------------------------------------------------------
        %%Function: CalculateMD5Hash
        %%Qualified: Thetacat.Model.MediaItem.CalculateMD5Hash

        Calculate the MD5 hash for the given file.
    ----------------------------------------------------------------------------*/
    public static string CalculateMD5Hash(string localPath)
    {
        using FileStream fs = File.Open(
            localPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        using MD5 md5 = System.Security.Cryptography.MD5.Create();

        byte[] hash = md5.ComputeHash(fs);

        string fullContentMd5 = Convert.ToBase64String(hash);

        return fullContentMd5;
    }
}
