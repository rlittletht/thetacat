using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using Thetacat.Import;
using static System.Windows.Forms.AxHost;

namespace Thetacat.Model;

public class MediaItem
{
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

    private MediaItemData? m_base;
    private readonly MediaItemData m_working;

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

    public string VirtualPath
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

    public List<MediaTag> Tags
    {
        get => m_working.Tags;
        set
        {
            EnsureBase();
            PushOp(PendingOp.ChangeTags);
            m_working.Tags = value;
        }
    }

    public void PushChangeTagPending()
    {
        PushOp(PendingOp.ChangeTags);
    }

    public List<PendingOp> m_pendingOps = new();

    public MediaItem()
    {
        m_working = new MediaItemData();
    }

    public MediaItem(ImportItem importItem)
    {
        m_working = new MediaItemData(importItem);
        m_pendingOps.Add(PendingOp.Create);
    }
}
