using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Types.Parallel;

namespace Tests.Model.Workgroups;

public class CatalogMock: ICatalog
{
    public ObservableConcurrentDictionary<Guid, MediaItem> Items { get; }

    public CatalogMock(IEnumerable<ServiceMediaItem> items)
    {
        Items = new ObservableConcurrentDictionary<Guid, MediaItem>();

        foreach (ServiceMediaItem item in items)
        {
            Items.Add(item.Id ?? throw new NullReferenceException(), new MediaItem(item));
        }
    }

    public CatalogMock(IEnumerable<MediaItem> items)
    {
        Items = new ObservableConcurrentDictionary<Guid, MediaItem>();

        foreach (MediaItem item in items)
        {
            Items.Add(item.ID, item);
        }
    }

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
}
