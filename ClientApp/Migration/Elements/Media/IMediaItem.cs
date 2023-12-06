using System;

namespace Thetacat.Migration.Elements.Metadata.UI.Media;

public interface IMediaItem
{
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public DateTime FileDateOriginal { get; set; }
}
