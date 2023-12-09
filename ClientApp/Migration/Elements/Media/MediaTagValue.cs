using System;

namespace Thetacat.Migration.Elements.Media;

public class MediaTagValue
{
    public int MediaId { get; set; }
    public string PseIdentifier { get; set; } = string.Empty;
    public Guid? CatId { get; set; }
    public string Value { get; set; } = string.Empty;
}
