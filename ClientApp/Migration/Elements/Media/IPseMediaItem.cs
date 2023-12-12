using System;
using System.Collections.Generic;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Model;

namespace Thetacat.Migration.Elements.Media;

public interface IPseMediaItem
{
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public DateTime FileDateOriginal { get; set; }

    public int ID { get; set; }
    public string FullPath { get; set; }
    public IEnumerable<PseMediaTagValue> Metadata { get; }
    public IEnumerable<PseMetatag> Tags { get; }

}
