using System;
using System.Collections.Concurrent;
using Thetacat.Model;
using Thetacat.Model.Metatags;

namespace Thetacat.Types;

public interface ICatalog
{
    public ObservableConcurrentDictionary<Guid, MediaItem> Items { get; }
    public void AddNewMediaItem(MediaItem item);
    public void FlushPendingCreates();
    public void AddMediaTag(Guid id, MediaTag tag);
    public void ReadFullCatalogFromServer(MetatagSchema schema);
    public MediaItem? LookupItemFromVirtualPath(string virtualPath, string fullLocalPath);
}
