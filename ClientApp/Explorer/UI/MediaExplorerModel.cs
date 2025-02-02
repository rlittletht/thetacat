﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using Thetacat.Explorer.Commands;
using Thetacat.Logging;

namespace Thetacat.Explorer.UI;

public class MediaExplorerModel : INotifyPropertyChanged
{
    private double m_panelItemHeight = 112.0;
    private double m_panelItemWidth = 148.8;
    private double m_imageHeight = 96.0;
    private double m_imageWidth = 148.0;

    public ExplorerContextMenuModel ExplorerContextMenu { get; set; } = new ExplorerContextMenuModel();
    public ShowHideMetatagPanelCommand? ShowHideMetatagPanel { get; set; }
    public DeleteCommand? DeleteItems { get; set; }
    public ToggleTopOfStackCommand? ToggleTopOfStackItems { get; set; }
    public OpenItemsStackCommand? OpenItemsStack { get; set; }
    public ResetCacheItemsCommand? ResetCacheItems { get; set; }
    public RotateItemsRightCommand? RotateItemsRight{ get; set; }
    public MirrorItemsCommand? MirrorItems { get; set; }
    public SelectPanelCommand? SelectPanel { get; set; }
    public SelectPanelCommand? ExtendSelectPanel { get; set; }
    public SelectPanelCommand? AddSelectPanel { get; set; }
    public SelectPanelCommand? AddExtendSelectPanel { get; set; }
    public SelectPanelCommand? ContextSelectPanel { get; set; }
    public ProcessMenuTagCommand? RemoveMenuTag { get; set; }
    public ProcessMenuTagCommand? AddMenuTag { get; set; }

    public LaunchItemCommand? LaunchItem { get; set; }
    public EditNewVersionCommand? EditNewVersion { get; set; }

    public ExplorerItemSize ItemSize
    {
        get => m_itemSize;
        set => SetField(ref m_itemSize, value);
    }

    public double PanelItemHeight
    {
        get => m_panelItemHeight;
        set => SetField(ref m_panelItemHeight, value);
    }

    public double PanelItemWidth
    {
        get => m_panelItemWidth;
        set => SetField(ref m_panelItemWidth, value);
    }

    public double ImageHeight
    {
        get => m_imageHeight;
        set => SetField(ref m_imageHeight, value);
    }

    public double ImageWidth
    {
        get => m_imageWidth;
        set => SetField(ref m_imageWidth, value);
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

    public ObservableCollection<MediaExplorerLineModel> ExplorerLines = new();
    private ExplorerItemSize m_itemSize = ExplorerItemSize.Medium;
}
