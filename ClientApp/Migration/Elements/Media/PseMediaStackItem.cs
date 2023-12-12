using System;
using System.Security.RightsManagement;

namespace Thetacat.Migration.Elements.Media;

public class PseMediaStackItem
{
    public int StackId { get; }
    public int MediaId { get; }
    public int MediaIndex { get; }

    public PseMediaStackItem(int stackId, int mediaId, int mediaIndex)
    {
        StackId = stackId;
        MediaId = mediaId;
        MediaIndex = mediaIndex;
    }
}
