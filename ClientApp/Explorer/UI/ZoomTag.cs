using Emgu.CV.Dnn;
using MetadataExtractor;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Types;

namespace Thetacat.Explorer.UI;

/*----------------------------------------------------------------------------
    %%Class: ZoomTag
    %%Qualified: Thetacat.Explorer.UI.ZoomTag

    This is a single tag that is applied to the media item in the zoom
    window (allows easy population of the list of tags)
----------------------------------------------------------------------------*/
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
        if (Tag == null)
            throw new CatExceptionInternalFailure("tag not set on updatestate");

        IsSet = mediaItem.TryGetMediaTag(Tag.ID, out _);
    }
}
