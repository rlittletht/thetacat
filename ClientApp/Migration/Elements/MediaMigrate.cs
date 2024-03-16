using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Migration.Elements.Metadata.UI.Media;

namespace Thetacat.Migration.Elements.Media;

public class MediaMigrate
{
    private List<PseMediaItem>? m_mediaItems;
    private Dictionary<int, IPseMediaItem>? m_mapPseMedia;
    private Dictionary<Guid, IPseMediaItem>? m_mapCatMedia;

    public readonly ObservableCollection<PathSubstitution> PathSubstitutions = new();

    public List<PseMediaItem> MediaItems => m_mediaItems ??= new List<PseMediaItem>();
    
    public void SetMediaItems(List<PseMediaItem> media)
    {
        m_mediaItems = media;
    }

    /*----------------------------------------------------------------------------
        %%Function: PropagateMetadataToBuiltins
        %%Qualified: Thetacat.Migration.Elements.Media.MediaMigrate.PropagateMetadataToBuiltins

        Some metadata items map directly to builtin properties. Propagate those
        here
    ----------------------------------------------------------------------------*/
    public void PropagateMetadataToBuiltins(MetatagMigrate metatagMigrate)
    {
        if (m_mediaItems == null) 
            return;

        foreach (PseMediaItem item in m_mediaItems)
        {
            metatagMigrate.MetadataSchema.PropagateMetadataToBuiltins(item);
        }
    }

    Dictionary<int, IPseMediaItem> BuildPseMap()
    {
        Dictionary<int, IPseMediaItem>? mapPseMedia = new();

        foreach (PseMediaItem item in MediaItems)
        {
            mapPseMedia.Add(item.ID, item);
        }

        return mapPseMedia;
    }

    Dictionary<Guid, IPseMediaItem> BuildCatMap()
    {
        Dictionary<Guid, IPseMediaItem> mapCatMedia = new();

        foreach (PseMediaItem item in MediaItems)
        {
            mapCatMedia.Add(item.CatID, item);
        }

        return mapCatMedia;
    }

    public IPseMediaItem? GetMediaFromPseId(int id)
    {
        m_mapPseMedia ??= BuildPseMap();

        if (m_mapPseMedia.TryGetValue(id, out IPseMediaItem? media))
            return media;

        return null;
    }

    public IPseMediaItem? GetMediaFromCatId(Guid id)
    {
        m_mapCatMedia ??= BuildCatMap();

        if (m_mapCatMedia.TryGetValue(id, out IPseMediaItem? media))
            return media;

        return null;
    }
}
