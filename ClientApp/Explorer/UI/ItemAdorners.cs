using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Thetacat.Types;

namespace Thetacat.Explorer.UI;

public class ItemAdorners
{
    private Visibility m_topOfStackAdornerVisibility = Visibility.Collapsed;
    private Visibility m_notTopOfStackAdornerVisibility = Visibility.Collapsed;
    private bool m_isTopOfStack = false;
    private bool m_isNotTopOfStack = false;
    private bool m_isTrashItem;
    private bool m_isUploadPending;

    private Visibility m_trashAdornerVisibility;
    private bool m_isOffline;
    private Visibility m_offlineAdornerVisibility;
    private Visibility m_pendingUploadAdornerVisibility;

    public bool IsUploadPending
    {
        get => m_isUploadPending;
        set
        {
            SetField(ref m_isUploadPending, value);
            PendingUploadAdornerVisibility = m_isUploadPending ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public Visibility PendingUploadAdornerVisibility
    {
        get => m_pendingUploadAdornerVisibility;
        private set => SetField(ref m_pendingUploadAdornerVisibility, value);
    }

    public bool IsTrashItem
    {
        get => m_isTrashItem;
        set
        {
            SetField(ref m_isTrashItem, value);
            TrashAdornerVisibility = m_isTrashItem ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool IsOffline
    {
        get => m_isOffline;
        set
        {
            SetField(ref m_isOffline, value);
            OfflineAdornerVisibility = m_isOffline ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public Visibility OfflineAdornerVisibility
    {
        get => m_offlineAdornerVisibility;
        private set => SetField(ref m_offlineAdornerVisibility, value);
    }

    public Visibility TrashAdornerVisibility
    {
        get => m_trashAdornerVisibility;
        private set => SetField(ref m_trashAdornerVisibility, value);
    }
    public bool IsTopOfStack
    {
        get => m_isTopOfStack;
        set
        {
            SetField(ref m_isTopOfStack, value);
            TopOfStackAdornerVisibility = m_isTopOfStack ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool IsNotTopOfStack
    {
        get => m_isNotTopOfStack;
        set
        {
            SetField(ref m_isNotTopOfStack, value);
            NotTopOfStackAdornerVisibility = m_isNotTopOfStack ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public Visibility TopOfStackAdornerVisibility
    {
        get => m_topOfStackAdornerVisibility;
        private set => SetField(ref m_topOfStackAdornerVisibility, value);
    }

    public Visibility NotTopOfStackAdornerVisibility
    {
        get => m_notTopOfStackAdornerVisibility;
        private set => SetField(ref m_notTopOfStackAdornerVisibility, value);
    }

    protected virtual bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        return true;
    }
}
