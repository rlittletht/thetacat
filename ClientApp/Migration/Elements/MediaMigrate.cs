using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Migration.Elements.Metadata.UI.Media;

namespace Thetacat.Migration.Elements.Media;

public class MediaMigrate
{
    private List<MediaItem>? m_mediaItems;
    private Dictionary<int, IMediaItem>? m_mapPseMedia;
    private Dictionary<Guid, IMediaItem>? m_mapCatMedia;
    private List<PseMediaStackItem>? m_mediaStackItems;

    public readonly ObservableCollection<PathSubstitution> PathSubstitutions = new();

    public List<MediaItem> MediaItems => m_mediaItems ??= new List<MediaItem>();
    public List<PseMediaStackItem> MediaStacks => m_mediaStackItems ??= new List<PseMediaStackItem>();
    
    public void SetMediaItems(List<MediaItem> media)
    {
        m_mediaItems = media;
    }

    public void SetMediaStacks(List<PseMediaStackItem> stacks)
    {
        m_mediaStackItems = stacks;
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
