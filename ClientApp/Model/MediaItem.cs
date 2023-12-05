using System;
using System.Collections.Generic;

namespace Thetacat.Model;

public class MediaItem
{
    public Guid Id { get; init; } = Guid.Empty;
    public string VirtualPath { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;

    public List<MediaTag> Tags { get; init; } = new();

}
