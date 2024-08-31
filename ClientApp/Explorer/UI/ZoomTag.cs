﻿using Emgu.CV.Dnn;
using MetadataExtractor;
using Thetacat.Metatags.Model;
using Thetacat.Model;

namespace Thetacat.Explorer.UI;

public class ZoomTag
{
    public Metatag? Tag { get; set; } = null;
    public bool IsSet { get; set; } = false;
    public string CheckedControlName { get; private init; }
    public string LabelControlName { get; private init; }

    public ZoomTag(string labelControlName, string checkedControlName)
    {
        LabelControlName = labelControlName;
        CheckedControlName = checkedControlName;
    }

    public void SetTag(MediaItem mediaItem, Metatag tag)
    {
        Tag = tag;
        UpdateState(mediaItem);
    }

    public void UpdateState(MediaItem mediaItem)
    {
        IsSet = mediaItem.Tags.TryGetValue(Tag.ID, out _);
    }
}
