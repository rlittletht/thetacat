using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Thetacat.Filtering;
using Thetacat.Metatags.Model;
using Thetacat.Model;

namespace Thetacat.Types;

public interface ICatalog
{
    public event EventHandler<DirtyItemEventArgs<bool>>? OnItemDirtied;
    public void AddNewMediaItem(MediaItem item);
    public MediaItem GetMediaFromId(Guid id);
    public MediaItem GetMediaFromId(string id);
    public bool TryGetMedia(Guid id, [MaybeNullWhen(false)] out MediaItem mediaItem);

    public IEnumerable<MediaItem> GetMediaCollection();
    public List<MediaItem> GetFilteredMediaItems(FilterDefinition filter);
    public ObservableCollection<MediaItem> GetObservableCollection();
    public MediaItem? CreateVersionBasedOn(ICache cache, MediaItem based);

    public MediaStacks GetStacksFromType(MediaStackType stackType);
    public void PushPendingChanges(Guid catalogID, Func<int, string, bool>? verify = null);
    /* async */ public Task ReadFullCatalogFromServer(Guid catalogID, MetatagSchema schema);
    public MediaItem? LookupItemFromVirtualPath(string virtualPath, string fullLocalPath, bool verifyMd5);
    public MediaItem? FindMatchingMediaByMD5(string md5);
    public MediaStacks VersionStacks { get; }
    public MediaStacks MediaStacks { get; }
    public void AddMediaToTopOfMediaStack(MediaStackType stackType, Guid stackId, Guid mediaId);
    public void AddMediaToStackAtIndex(MediaStackType stackType, Guid stackId, Guid mediaId, int? index);
    public void DeleteItem(Guid catalogId, Guid id);
    public string GetMD5ForItem(Guid id, ICache cache);
    public bool HasMediaItem(Guid mediaId);
    public void Reset();
}
