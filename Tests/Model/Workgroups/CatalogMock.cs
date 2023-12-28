using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Types.Parallel;

namespace Tests.Model.Workgroups;

public class CatalogMock: ICatalog
{
    private Media m_media;

    public CatalogMock(IEnumerable<ServiceMediaItem> items)
    {
        m_media = new Media();

        foreach (ServiceMediaItem item in items)
        {
            m_media.Items.Add(item.Id ?? throw new NullReferenceException(), new MediaItem(item));
        }
    }

    public CatalogMock(IEnumerable<MediaItem> items)
    {
        m_media = new Media();

        foreach (MediaItem item in items)
        {
            m_media.Items.Add(item.ID, item);
        }
    }

    public IMedia Media => m_media;

    public void AddNewMediaItem(MediaItem item)
    {
        throw new NotImplementedException();
    }

    public void PushPendingChanges()
    {
        throw new NotImplementedException();
    }

    public void AddMediaTagInternal(Guid id, MediaTag tag)
    {
        throw new NotImplementedException();
    }

    public void ReadFullCatalogFromServer(MetatagSchema schema)
    {
        throw new NotImplementedException();
    }

    public MediaItem? LookupItemFromVirtualPath(string virtualPath, string fullLocalPath) => throw new NotImplementedException();
    public MediaStacks VersionStacks => throw new NotImplementedException();
    public MediaStacks MediaStacks => throw new NotImplementedException();
    public bool HasMediaItem(Guid mediaId) => m_media.Items.ContainsKey(mediaId);
}
