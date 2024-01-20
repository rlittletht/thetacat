using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.Types;

namespace Tests.Model.Workgroups;

public class CatalogMock : ICatalog
{
    private Media m_media;

    public CatalogMock(IEnumerable<ServiceMediaItem> items)
    {
        m_media = new Media();

        foreach (ServiceMediaItem item in items)
        {
            m_media.Items.TryAdd(item.Id ?? throw new NullReferenceException(), new MediaItem(item));
        }
    }

    public CatalogMock(IEnumerable<MediaItem> items)
    {
        m_media = new Media();

        foreach (MediaItem item in items)
        {
            m_media.Items.TryAdd(item.ID, item);
        }
    }

    public Media Media => m_media;

    public void AddNewMediaItem(MediaItem item)
    {
        throw new NotImplementedException();
    }

    public MediaItem GetMediaFromId(Guid id) => m_media.Items[id];
    public MediaItem GetMediaFromId(string id) => m_media.Items[Guid.Parse(id)];
    public bool TryGetMedia(Guid id, [MaybeNullWhen(false)] out MediaItem mediaItem) => m_media.Items.TryGetValue(id, out mediaItem);

    public IEnumerable<MediaItem> GetMediaCollection() => m_media.Items.Values;
    public List<MediaItem> GetFilteredMediaItems(Dictionary<Guid, bool> filter) => throw new NotImplementedException();

    public ObservableCollection<MediaItem> GetObservableCollection() => throw new NotImplementedException();

    public MediaStacks GetStacksFromType(MediaStackType stackType) => throw new NotImplementedException();

    public void PushPendingChanges()
    {
        throw new NotImplementedException();
    }

    public void AddMediaTagInternal(Guid id, MediaTag tag)
    {
        throw new NotImplementedException();
    }

    public Task ReadFullCatalogFromServer(MetatagSchema schema)
    {
        throw new NotImplementedException();
    }

    public MediaItem? LookupItemFromVirtualPath(string virtualPath, string fullLocalPath, bool verifyMd5) => throw new NotImplementedException();
    public MediaItem? FindMatchingMediaByMD5(string md5) => throw new NotImplementedException();

    public MediaStacks VersionStacks => throw new NotImplementedException();
    public MediaStacks MediaStacks => throw new NotImplementedException();
    public void AddMediaToStackAtIndex(MediaStackType stackType, Guid stackId, Guid mediaId, int index)
    {
        throw new NotImplementedException();
    }

    public bool HasMediaItem(Guid mediaId) => m_media.Items.ContainsKey(mediaId);
}
