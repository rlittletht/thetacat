using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Thetacat.Migration.Elements.Versions;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types.Parallel;

namespace Thetacat.Types;

public interface ICatalog
{
    public void AddNewMediaItem(MediaItem item);
    public MediaItem GetMediaFromId(Guid id);
    public MediaItem GetMediaFromId(string id);
    public bool TryGetMedia(Guid id, [MaybeNullWhen(false)] out MediaItem mediaItem);

    public IEnumerable<MediaItem> GetMediaCollection();
    public ObservableCollection<MediaItem> GetObservableCollection();

    public MediaStacks GetStacksFromType(MediaStackType stackType);
    public void PushPendingChanges();
    /* async */ public Task ReadFullCatalogFromServer(MetatagSchema schema);
    public MediaItem? LookupItemFromVirtualPath(string virtualPath, string fullLocalPath);
    public MediaStacks VersionStacks { get; }
    public MediaStacks MediaStacks { get; }
    public void AddMediaToStackAtIndex(MediaStackType stackType, Guid stackId, Guid mediaId, int index);

    public bool HasMediaItem(Guid mediaId);
}
