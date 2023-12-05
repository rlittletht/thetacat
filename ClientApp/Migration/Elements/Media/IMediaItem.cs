using System;

namespace Thetacat.Migration.Elements.Media;

public interface IMediaItem
{
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public DateTime FileDateOriginal { get; set; }
}
