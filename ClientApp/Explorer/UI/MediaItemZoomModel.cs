using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Emgu.CV.Util;
using Thetacat.Metatags.Model;
using Thetacat.Model;

namespace Thetacat.Explorer.UI;

public class MediaItemZoomModel : ItemAdorners, INotifyPropertyChanged
{
    public ObservableCollection<MediaTag> Tags { get; } = new ObservableCollection<MediaTag>();
    private MediaItem? m_mediaItem;
    private BitmapSource? m_image;
    private string m_pruneModeCaption = "Start Pruning";
    private bool m_isPruning = false;

#region QuickTags

    public const int TagButtonCount = 10;

    public readonly ZoomTag[] ZoomTags = new ZoomTag[TagButtonCount];

    private readonly string[] TagPropsNames =
        new string[TagButtonCount]
        {
            "Tag1Label", "Tag2Label", "Tag3Label", "Tag4Label", "Tag5Label", "Tag6Label", "Tag7Label", "Tag8Label",
            "Tag9Label", "Tag10Label"
        };

    private readonly string[] TagCheckedPropsNames =
        new string[TagButtonCount]
        {
            "IsTag1Checked", "IsTag2Checked", "IsTag3Checked", "IsTag4Checked", "IsTag5Checked", "IsTag6Checked", "IsTag7Checked",
            "IsTag8Checked", "IsTag9Checked", "IsTag10Checked"
        };

    public string Tag1Label => $"{(IsPruning ? "0: " : "")}{ZoomTags[0].Tag?.Name ?? "unset"}";
    public string Tag2Label => $"{(IsPruning ? "1: " : "")}{ZoomTags[1].Tag?.Name ?? "unset"}";
    public string Tag3Label => $"{(IsPruning ? "2: " : "")}{ZoomTags[2].Tag?.Name ?? "unset"}";
    public string Tag4Label => $"{(IsPruning ? "3: " : "")}{ZoomTags[3].Tag?.Name ?? "unset"}";
    public string Tag5Label => $"{(IsPruning ? "4: " : "")}{ZoomTags[4].Tag?.Name ?? "unset"}";
    public string Tag6Label => $"{(IsPruning ? "5: " : "")}{ZoomTags[5].Tag?.Name ?? "unset"}";
    public string Tag7Label => $"{(IsPruning ? "6: " : "")}{ZoomTags[6].Tag?.Name ?? "unset"}";
    public string Tag8Label => $"{(IsPruning ? "7: " : "")}{ZoomTags[7].Tag?.Name ?? "unset"}";
    public string Tag9Label => $"{(IsPruning ? "8: " : "")}{ZoomTags[8].Tag?.Name ?? "unset"}";
    public string Tag10Label => $"{(IsPruning ? "9: " : "")}{ZoomTags[9].Tag?.Name ?? "unset"}";

    public bool IsTag1Checked
    {
        get => ZoomTags[0].IsSet;
        set => SetZoomTagState(ZoomTags[0], value);
    }

    public bool IsTag2Checked
    {
        get => ZoomTags[1].IsSet;
        set => SetZoomTagState(ZoomTags[1], value);
    }

    public bool IsTag3Checked
    {
        get => ZoomTags[2].IsSet;
        set => SetZoomTagState(ZoomTags[2], value);
    }

    public bool IsTag4Checked
    {
        get => ZoomTags[3].IsSet;
        set => SetZoomTagState(ZoomTags[3], value);
    }

    public bool IsTag5Checked
    {
        get => ZoomTags[4].IsSet;
        set => SetZoomTagState(ZoomTags[4], value);
    }

    public bool IsTag6Checked
    {
        get => ZoomTags[5].IsSet;
        set => SetZoomTagState(ZoomTags[5], value);
    }

    public bool IsTag7Checked
    {
        get => ZoomTags[6].IsSet;
        set => SetZoomTagState(ZoomTags[6], value);
    }

    public bool IsTag8Checked
    {
        get => ZoomTags[7].IsSet;
        set => SetZoomTagState(ZoomTags[7], value);
    }

    public bool IsTag9Checked
    {
        get => ZoomTags[8].IsSet;
        set => SetZoomTagState(ZoomTags[8], value);
    }

    public bool IsTag10Checked
    {
        get => ZoomTags[9].IsSet;
        set => SetZoomTagState(ZoomTags[9], value);
    }

    public void NotifyAllTagsDirty()
    {
        for (int i = 0; i < TagButtonCount; i++)
        {
            OnPropertyChanged(TagPropsNames[i]);
            OnPropertyChanged(TagCheckedPropsNames[i]);
        }
    }

#endregion

    public int VectorClock { get; set; } = 0;

    public string PruneModeCaption
    {
        get => m_pruneModeCaption;
        set => SetField(ref m_pruneModeCaption, value);
    }

    public bool IsPruning
    {
        get => m_isPruning;
        set
        {
            SetField(ref m_isPruning, value);
            OnPropertyChanged(nameof(PruningVisibility));
            NotifyAllTagsDirty();
        }
    }

    public Visibility PruningVisibility => m_isPruning ? Visibility.Visible : Visibility.Collapsed;

    public BitmapSource? Image
    {
        get => m_image;
        set => SetField(ref m_image, value);
    }

    public MediaItem? MediaItem
    {
        get => m_mediaItem;
        set => SetField(ref m_mediaItem, value);
    }

    public void UpdateZoomTagFromMedia(Guid id)
    {
        foreach (ZoomTag tag in ZoomTags)
        {
            if (tag.Tag == null)
                continue;

            if (tag?.Tag.ID == id)
            {
                tag.UpdateState(m_mediaItem!);
                OnPropertyChanged(tag.CheckedControlName);
            }
        }
    }

    public void SetZoomTagState(ZoomTag tag, bool isSet, bool dontNotifyChanged = false)
    {
        tag.IsSet = isSet;
        if (!dontNotifyChanged)
            OnPropertyChanged(tag.CheckedControlName);
    }

    public void SetQuickMetatag(int tagIndex, Metatag tag)
    {
        ZoomTags[tagIndex].SetTag(m_mediaItem!, tag);
        OnPropertyChanged(TagPropsNames[tagIndex]);
        OnPropertyChanged(TagCheckedPropsNames[tagIndex]);
    }

    public MediaItemZoomModel()
    {
        for (int i = 0; i < TagButtonCount; i++)
        {
            ZoomTags[i] = new ZoomTag(TagPropsNames[i], TagCheckedPropsNames[i]);
        }
    }

#region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected override bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

#endregion
}
