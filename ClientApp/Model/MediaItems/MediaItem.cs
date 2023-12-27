using MetadataExtractor;
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
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model;

public class MediaItem: INotifyPropertyChanged
{
    private MediaItemData? m_base;
    private readonly MediaItemData m_working;

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

    public enum Op
    {
        Create,
        Delete,
        MaybeUpdate
    };

    public Op PendingOp { get; set; } = Op.MaybeUpdate;

    public bool MaybeHasChanges => m_base != null;

    #region Data Accessors

    void EnsureBase()
    {
        m_base ??= new(m_working);
    }

    public MediaItemData Base => m_base ?? throw new CatExceptionInternalFailure("no base to fetch");

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
            VectorClock++;
            m_working.Tags = value;
        }
    }
    #endregion

    public bool IsCreatePending()
    {
        return PendingOp == Op.Create;
    }

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

            MediaTag newTag = new MediaTag(metatag, value);

            if (FAddOrUpdateTag(newTag))
                changed = true;
        }

        return changed;
    }

    public bool FAddOrUpdateTag(MediaTag tag)
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
        
        foreach (MetadataExtractor.Directory directory in directories)
        {
            MigrateMetadataForDirectory(metatagSchema, null, directory, MetatagStandards.Standard.Unknown);
        }

        return log;
    }

    public void ResetPendingChanges()
    {
        PendingOp = Op.MaybeUpdate;
        m_base = null;
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
