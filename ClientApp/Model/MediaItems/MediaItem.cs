﻿using MetadataExtractor;
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
using System.Windows;
using System.Windows.Markup;
using Thetacat.Import;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model;

public class MediaItem: INotifyPropertyChanged
{
    private MediaItemData? m_base;
    private readonly MediaItemData m_working;

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

    public enum Op
    {
        Create,
        Update,
        Delete,
        None
    };

    public Op PendingOp { get; set; } = Op.None;

    public enum PendingOpDeprecated
    {
        Create,
        Delete,
        ChangeMimeType,
        ChangePath,
        ChangeMD5,
        ChangeState,
        ChangeTags
    }

    public bool MaybeHasChanges => m_base != null;

    #region Data Accessors

    void EnsureBase()
    {
        m_base ??= new(m_working);
    }

    public MediaItemData Base => m_base ?? throw new CatExceptionInternalFailure("no base to fetch");
    void PushOp(PendingOpDeprecated opDeprecated)
    {
        m_pendingOps.Add(opDeprecated);
    }

    public string MimeType
    {
        get => m_working.MimeType;
        set
        {
            EnsureBase();
            PushOp(PendingOpDeprecated.ChangeMimeType);
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
            PushOp(PendingOpDeprecated.ChangePath);
            m_working.VirtualPath = value;
        }
    }

    public string MD5
    {
        get => m_working.MD5;
        set
        {
            EnsureBase();
            PushOp(PendingOpDeprecated.ChangeMD5);
            m_working.MD5 = value;
        }
    }

    public MediaItemState State
    {
        get => m_working.State;
        set
        {
            EnsureBase();
            PushOp(PendingOpDeprecated.ChangeState);
            m_working.State = value;
            OnPropertyChanged(nameof(State));
        }
    }

    public string CacheStatus => MainWindow._AppState.Cache.IsItemCached(ID) ? "cached" : "<No Cache>";

    public void NotifyCacheStatusChanged()
    {
        OnPropertyChanged(nameof(CacheStatus));
    }

    public ConcurrentDictionary<Guid, MediaTag> Tags
    {
        get => m_working.Tags;
        set
        {
            EnsureBase();
            PushOp(PendingOpDeprecated.ChangeTags);
            m_working.Tags = value;
        }
    }
    #endregion

    public void PushChangeTagPending()
    {
        PushOp(PendingOpDeprecated.ChangeTags);
    }

    public List<PendingOpDeprecated> m_pendingOps = new();

    public bool IsCreatePending()
    {
        return PendingOp == Op.Create;
    }

    public void ClearPendingCreate()
    {
#if TAGS_NOT_ON_CREATE // we upload tags on create now
        // Tags aren't uploaded during create, so if there are any tags on this item,
        // we need to mark a pending change...

        for (int i = m_pendingOps.Count - 1; i >= 0; i--)
        {
            PendingOpDeprecated opDeprecated = m_pendingOps[i];
            if (opDeprecated != PendingOpDeprecated.ChangeTags)
                m_pendingOps.RemoveAt(i);
        }

        // at this point, the only pending opDeprecated that could be left is ChangeTags...
        // if we need one and we have it, good. otherwise, add one
        if (Tags.Count > 0 && m_pendingOps.Count == 0)
            PushChangeTagPending();
#else
        m_pendingOps.Clear();
#endif
    }

    public MediaItem()
    {
        m_working = new MediaItemData();
    }

    public MediaItem(ImportItem importItem)
    {
        m_working = new MediaItemData(importItem);
        m_pendingOps.Add(PendingOpDeprecated.Create);
    }

    public MediaItem(ServiceMediaItem item)
    {
        m_working = new MediaItemData(item);
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

    /*----------------------------------------------------------------------------
        %%Function: MigrateMetadataForDirectory
        %%Qualified: Thetacat.Model.MediaItem.MigrateMetadataForDirectory

        Returns true if there were any changes made to this items tags.
        (schema changes will be caught later)
    ----------------------------------------------------------------------------*/
    public bool MigrateMetadataForDirectory(
        MetatagSchema metatagSchema,
        Metatag? parent,
        MetadataExtractor.Directory directory,
        MetatagStandards.Standard standard)
    {
        bool changed = false;

        if (parent == null && standard == MetatagStandards.Standard.Unknown)
            standard = MetatagStandards.GetStandardFromType(directory.GetType().Name);

        if (standard == MetatagStandards.Standard.Unknown)
        {
#if verbose_standard
            log?.Add($"unknown standard directory {directory.Name}");
#endif
            return false;
        }

        StandardDefinitions standardDefinitions = MetatagStandards.GetStandardMappings(standard);

        // match the current directory to a metatag
        Metatag? dirTag = metatagSchema.FindByName(parent, standardDefinitions.StandardTag);

        if (dirTag == null)
        {
            // we have to create one
            dirTag = Metatag.Create(parent?.ID, standardDefinitions.StandardTag, directory.Name, standard);
            if (parent == null)
                metatagSchema.AddStandardRoot(dirTag);
            else
                metatagSchema.AddMetatag(dirTag);
        }

        foreach (Tag tag in directory.Tags)
        {
            if (!standardDefinitions.Properties.TryGetValue(tag.Type, out StandardDefinition? def))
            {
#if verbose_standard
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


            bool identicalExisting = false;

            Tags.AddOrUpdate(
                metatag.ID,
                new MediaTag(metatag, value),
                (key, oldMediaTag) =>
                {
                    if (oldMediaTag.Value != value)
                    {
                        log?.Add($"Different metatag value for {key}: {oldMediaTag.Value} != {value}");
                        oldMediaTag.Value = value;
                    }
                    else
                    {
                        identicalExisting = true;
                    }

                    return oldMediaTag;
                });

            changed |= !identicalExisting;
        }

        return changed;
    }

    private List<string>? log;
    private bool m_isCachePending = false;
    private string m_localPath = string.Empty;

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

    public List<string>? SetMetadataFromFile(MetatagSchema metatagSchema)
    {
        log = new List<string>();

        string file = LocalPath;

        // load exif and other data from this item.
        IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(file);
            
        this.MD5 = CalculateMD5Hash(file);
        
        bool changed = false;

        foreach (MetadataExtractor.Directory directory in directories)
        {
            if (MigrateMetadataForDirectory(metatagSchema, null, directory, MetatagStandards.Standard.Unknown))
                changed = true;
        }

        if (changed)
            PushChangeTagPending();

        return log;
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
