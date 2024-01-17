using System;
using System.Collections.Generic;
using Thetacat.Migration.Elements.Metadata.UI;

namespace Thetacat.Migration.Elements.Media;

public interface IPseMediaItem
{
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public DateTime? FileDateOriginal { get; set; }

    public int ID { get; set; }
    public string FullPath { get; set; }
    public string? MD5 { get; set; }
    public bool InCatalog { get; set; }
    public IEnumerable<PseMediaTagValue> Metadata { get; }
    public IEnumerable<PseMetatag> Tags { get; }
    public Guid CatID { get; }
}
