using System;
using System.Collections.Generic;
using System.Windows.Forms.VisualStyles;
using NUnit.Framework.Constraints;
using Thetacat.Util;

namespace Thetacat.Model.Client;

/*----------------------------------------------------------------------------
    %%Class: Derivatives
    %%Qualified: Thetacat.Model.Client.Derivatives

    Derivatives are stored locally to the client and are images derived from
    the original media. A smaller version of the image for preview purposes,
    for example, or a more readily displayable version (even at the original
    resolution)

    We have some lookup dictionaries to make it easier to find the
    derivative you want
----------------------------------------------------------------------------*/
public class Derivatives
{
    // all of the derivatives we know about for each mediaitem. this is the master list
    private readonly Dictionary<Guid, List<DerivativeItem>> m_mediaDerivatives = new();

    // for each media item, map mime-type to the list of derivatives we have in that format
    private readonly Dictionary<Guid, Dictionary<string, List<DerivativeItem>>> m_mediaFormatDerivatives = new();

    // for each media item, an ordered list of scaled media -- smallest to largest
    private readonly Dictionary<Guid, SortedList<double, DerivativeItem>> m_scaledMediaDerivatives = new();

    public void AddDerivative(DerivativeItem item)
    {
        List<DerivativeItem> items = ListSupport.AddItemToMappedList(m_mediaDerivatives, item.MediaId, item);
        List<DerivativeItem> mimeList = ListSupport.AddItemToMappedMapList(m_mediaFormatDerivatives, item.MediaId, item.MimeType, item);
        SortedList<double, DerivativeItem> scaledList = ListSupport.AddItemToMappedSortedList(m_scaledMediaDerivatives, item.MediaId, item.ScaleFactor, item);
    }
}
