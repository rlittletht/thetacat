using MetadataExtractor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;
using Thetacat.Import;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Model;

public class MediaItem
{
    private MediaItemData? m_base;
    private readonly MediaItemData m_working;
    public string LocalPath { get; set; } = string.Empty;

    public enum PendingOp
    {
        Create,
        Delete,
        ChangeMimeType,
        ChangePath,
        ChangeSha5,
        ChangeState,
        ChangeTags
    }

    #region Data Accessors

    void EnsureBase()
    {
        m_base ??= new(m_working);
    }

    void PushOp(PendingOp op)
    {
        m_pendingOps.Add(op);
    }

    public string MimeType
    {
        get => m_working.MimeType;
        set
        {
            EnsureBase();
            PushOp(PendingOp.ChangeMimeType);
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
            PushOp(PendingOp.ChangePath);
            m_working.VirtualPath = value;
        }
    }

    public string Sha5
    {
        get => m_working.Sha5;
        set
        {
            EnsureBase();
            PushOp(PendingOp.ChangeSha5);
            m_working.Sha5 = value;
        }
    }

    public MediaItemState State
    {
        get => m_working.State;
        set
        {
            EnsureBase();
            PushOp(PendingOp.ChangeState);
            m_working.State = value;
        }
    }

    public ConcurrentDictionary<Guid, MediaTag> Tags
    {
        get => m_working.Tags;
        set
        {
            EnsureBase();
            PushOp(PendingOp.ChangeTags);
            m_working.Tags = value;
        }
    }
    #endregion

    public void PushChangeTagPending()
    {
        PushOp(PendingOp.ChangeTags);
    }

    public List<PendingOp> m_pendingOps = new();

    public bool IsCreatePending()
    {
        foreach (PendingOp op in m_pendingOps)
        {
            if (op == PendingOp.Create)
                return true;
        }

        return false;
    }

    public void ClearPendingCreate()
    {
#if TAGS_NOT_ON_CREATE // we upload tags on create now
        // Tags aren't uploaded during create, so if there are any tags on this item,
        // we need to mark a pending change...

        for (int i = m_pendingOps.Count - 1; i >= 0; i--)
        {
            PendingOp op = m_pendingOps[i];
            if (op != PendingOp.ChangeTags)
                m_pendingOps.RemoveAt(i);
        }

        // at this point, the only pending op that could be left is ChangeTags...
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
        m_pendingOps.Add(PendingOp.Create);
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
        Metatag? dirTag = metatagSchema.FindByName(parent, directory.Name);

        if (dirTag == null)
        {
            // we have to create one
            dirTag = Metatag.Create(parent?.ID, directory.Name, directory.Name, standard);
            if (parent == null)
                metatagSchema.AddStandardRoot(dirTag);
            else
                metatagSchema.AddMetatag(dirTag);
        }

        foreach (Tag tag in directory.Tags)
        {
            Metatag? metatag = metatagSchema.FindByName(dirTag, tag.Name);

            if (!standardDefinitions.Properties.TryGetValue(tag.Type, out StandardDefinition? def))
            {
#if verbose_standard
                log?.Add($"unknown metatag {tag.Name} in standard {standardDefinitions.StandardId}");
#endif
                continue;
            }

            if (!def.Include)
                continue;

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

    public List<string>? ReadMetadataFromFile(MetatagSchema metatagSchema)
    {
        log = new List<string>();

        string file = LocalPath;

        // load exif and other data from this item.
        IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(file);

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
}
