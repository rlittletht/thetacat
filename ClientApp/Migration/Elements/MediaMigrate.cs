using System;
using System.Collections.Generic;
using Thetacat.Migration.Elements.Metadata.UI.Media;

namespace Thetacat.Migration.Elements.Media;

public class MediaMigrate
{
    private List<MediaItem>? m_mediaItems;
    private Dictionary<int, IMediaItem>? m_mapPseMedia;
    private Dictionary<Guid, IMediaItem>? m_mapCatMedia;

    public List<MediaItem> MediaItems => m_mediaItems ??= new List<MediaItem>();

    public void SetMediaItems(List<MediaItem> media)
    {
        m_mediaItems = media;
    }

    Dictionary<int, IMediaItem> BuildPseMap()
    {
        Dictionary<int, IMediaItem>? mapPseMedia = new();

        foreach (MediaItem item in MediaItems)
        {
            mapPseMedia.Add(item.ID, item);
        }

        return mapPseMedia;
    }

    Dictionary<Guid, IMediaItem> BuildCatMap()
    {
        Dictionary<Guid, IMediaItem> mapCatMedia = new();

        foreach (MediaItem item in MediaItems)
        {
            mapCatMedia.Add(item.CatID, item);
        }

        return mapCatMedia;
    }

    public IMediaItem? GetMediaFromPseId(int id)
    {
        m_mapPseMedia ??= BuildPseMap();

        if (m_mapPseMedia.TryGetValue(id, out IMediaItem? media))
            return media;

        return null;
    }

    public IMediaItem? GetMediaFromCatId(Guid id)
    {
        m_mapCatMedia ??= BuildCatMap();

        if (m_mapCatMedia.TryGetValue(id, out IMediaItem? media))
            return media;

        return null;
    }
}
