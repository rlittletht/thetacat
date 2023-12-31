using System;

namespace Thetacat.Model.ImageCaching;

public class ImageCacheUpdateEventArgs: EventArgs
{
    public Guid MediaId { get; set; }
}
