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

    public const int TagButtonCount = 30;

    public readonly ZoomTag[] ZoomTags = new ZoomTag[TagButtonCount];

    private readonly string[] TagPropsNames =
        new string[TagButtonCount]
        {
            "Tag1Label", "Tag2Label", "Tag3Label", "Tag4Label", "Tag5Label", "Tag6Label", "Tag7Label", "Tag8Label", "Tag9Label", "Tag10Label",
            "Tag11Label", "Tag12Label", "Tag13Label", "Tag14Label", "Tag15Label", "Tag16Label", "Tag17Label", "Tag18Label", "Tag19Label", "Tag20Label",
            "Tag21Label", "Tag22Label", "Tag23Label", "Tag24Label", "Tag25Label", "Tag26Label", "Tag27Label", "Tag28Label", "Tag29Label", "Tag30Label"
        };

    private readonly string[] TagCheckedPropsNames =
        new string[TagButtonCount]
        {
            "IsTag1Checked", "IsTag2Checked", "IsTag3Checked", "IsTag4Checked", "IsTag5Checked", "IsTag6Checked", "IsTag7Checked", "IsTag8Checked", "IsTag9Checked", "IsTag10Checked",
            "IsTag11Checked", "IsTag12Checked", "IsTag13Checked", "IsTag14Checked", "IsTag15Checked", "IsTag16Checked", "IsTag17Checked", "IsTag18Checked", "IsTag19Checked", "IsTag20Checked",
            "IsTag21Checked", "IsTag22Checked", "IsTag23Checked", "IsTag24Checked", "IsTag25Checked", "IsTag26Checked", "IsTag27Checked", "IsTag28Checked", "IsTag29Checked", "IsTag30Checked"
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
    public string Tag11Label => $"{(IsPruning ? "a: " : "")}{ZoomTags[10].Tag?.Name ?? "unset"}";
    public string Tag12Label => $"{(IsPruning ? "b: " : "")}{ZoomTags[11].Tag?.Name ?? "unset"}";
    public string Tag13Label => $"{(IsPruning ? "c: " : "")}{ZoomTags[12].Tag?.Name ?? "unset"}";
    public string Tag14Label => $"{(IsPruning ? "e: " : "")}{ZoomTags[13].Tag?.Name ?? "unset"}";
    public string Tag15Label => $"{(IsPruning ? "f: " : "")}{ZoomTags[14].Tag?.Name ?? "unset"}";
    public string Tag16Label => $"{(IsPruning ? "g: " : "")}{ZoomTags[15].Tag?.Name ?? "unset"}";
    public string Tag17Label => $"{(IsPruning ? "h: " : "")}{ZoomTags[16].Tag?.Name ?? "unset"}";
    public string Tag18Label => $"{(IsPruning ? "i: " : "")}{ZoomTags[17].Tag?.Name ?? "unset"}";
    public string Tag19Label => $"{(IsPruning ? "j: " : "")}{ZoomTags[18].Tag?.Name ?? "unset"}";
    public string Tag20Label => $"{(IsPruning ? "k: " : "")}{ZoomTags[19].Tag?.Name ?? "unset"}";
    public string Tag21Label => $"{(IsPruning ? "l: " : "")}{ZoomTags[20].Tag?.Name ?? "unset"}";
    public string Tag22Label => $"{(IsPruning ? "m: " : "")}{ZoomTags[21].Tag?.Name ?? "unset"}";
    public string Tag23Label => $"{(IsPruning ? "o: " : "")}{ZoomTags[22].Tag?.Name ?? "unset"}";
    public string Tag24Label => $"{(IsPruning ? "q: " : "")}{ZoomTags[23].Tag?.Name ?? "unset"}";
    public string Tag25Label => $"{(IsPruning ? "r: " : "")}{ZoomTags[24].Tag?.Name ?? "unset"}";
    public string Tag26Label => $"{(IsPruning ? "s: " : "")}{ZoomTags[25].Tag?.Name ?? "unset"}";
    public string Tag27Label => $"{(IsPruning ? "t: " : "")}{ZoomTags[26].Tag?.Name ?? "unset"}";
    public string Tag28Label => $"{(IsPruning ? "u: " : "")}{ZoomTags[27].Tag?.Name ?? "unset"}";
    public string Tag29Label => $"{(IsPruning ? "v: " : "")}{ZoomTags[28].Tag?.Name ?? "unset"}";
    public string Tag30Label => $"{(IsPruning ? "w: " : "")}{ZoomTags[29].Tag?.Name ?? "unset"}";

    public bool IsTag1Checked  { get => ZoomTags[0].IsSet; set => SetZoomTagState(ZoomTags[0], value); }
    public bool IsTag2Checked  { get => ZoomTags[1].IsSet; set => SetZoomTagState(ZoomTags[1], value); }
    public bool IsTag3Checked  { get => ZoomTags[2].IsSet; set => SetZoomTagState(ZoomTags[2], value); }
    public bool IsTag4Checked  { get => ZoomTags[3].IsSet; set => SetZoomTagState(ZoomTags[3], value); }
    public bool IsTag5Checked  { get => ZoomTags[4].IsSet; set => SetZoomTagState(ZoomTags[4], value); }
    public bool IsTag6Checked  { get => ZoomTags[5].IsSet; set => SetZoomTagState(ZoomTags[5], value); }
    public bool IsTag7Checked  { get => ZoomTags[6].IsSet; set => SetZoomTagState(ZoomTags[6], value); }
    public bool IsTag8Checked  { get => ZoomTags[7].IsSet; set => SetZoomTagState(ZoomTags[7], value); }
    public bool IsTag9Checked  { get => ZoomTags[8].IsSet; set => SetZoomTagState(ZoomTags[8], value); }
    public bool IsTag10Checked { get => ZoomTags[9].IsSet; set => SetZoomTagState(ZoomTags[9], value); }
    public bool IsTag11Checked { get => ZoomTags[10].IsSet; set => SetZoomTagState(ZoomTags[10], value); }
    public bool IsTag12Checked { get => ZoomTags[11].IsSet; set => SetZoomTagState(ZoomTags[11], value); }
    public bool IsTag13Checked { get => ZoomTags[12].IsSet; set => SetZoomTagState(ZoomTags[12], value); }
    public bool IsTag14Checked { get => ZoomTags[13].IsSet; set => SetZoomTagState(ZoomTags[13], value); }
    public bool IsTag15Checked { get => ZoomTags[14].IsSet; set => SetZoomTagState(ZoomTags[14], value); }
    public bool IsTag16Checked { get => ZoomTags[15].IsSet; set => SetZoomTagState(ZoomTags[15], value); }
    public bool IsTag17Checked { get => ZoomTags[16].IsSet; set => SetZoomTagState(ZoomTags[16], value); }
    public bool IsTag18Checked { get => ZoomTags[17].IsSet; set => SetZoomTagState(ZoomTags[17], value); }
    public bool IsTag19Checked { get => ZoomTags[18].IsSet; set => SetZoomTagState(ZoomTags[18], value); }
    public bool IsTag20Checked { get => ZoomTags[19].IsSet; set => SetZoomTagState(ZoomTags[19], value); }
    public bool IsTag21Checked { get => ZoomTags[20].IsSet; set => SetZoomTagState(ZoomTags[20], value); }
    public bool IsTag22Checked { get => ZoomTags[21].IsSet; set => SetZoomTagState(ZoomTags[21], value); }
    public bool IsTag23Checked { get => ZoomTags[22].IsSet; set => SetZoomTagState(ZoomTags[22], value); }
    public bool IsTag24Checked { get => ZoomTags[23].IsSet; set => SetZoomTagState(ZoomTags[23], value); }
    public bool IsTag25Checked { get => ZoomTags[24].IsSet; set => SetZoomTagState(ZoomTags[24], value); }
    public bool IsTag26Checked { get => ZoomTags[25].IsSet; set => SetZoomTagState(ZoomTags[25], value); }
    public bool IsTag27Checked { get => ZoomTags[26].IsSet; set => SetZoomTagState(ZoomTags[26], value); }
    public bool IsTag28Checked { get => ZoomTags[27].IsSet; set => SetZoomTagState(ZoomTags[27], value); }
    public bool IsTag29Checked { get => ZoomTags[28].IsSet; set => SetZoomTagState(ZoomTags[28], value); }
    public bool IsTag30Checked { get => ZoomTags[29].IsSet; set => SetZoomTagState(ZoomTags[29], value); }

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
