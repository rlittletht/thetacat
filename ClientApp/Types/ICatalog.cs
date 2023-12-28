using System;
using Thetacat.Migration.Elements.Versions;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.Types.Parallel;

namespace Thetacat.Types;

public interface ICatalog
{
    public IMedia Media { get; }
    public void AddNewMediaItem(MediaItem item);

    public MediaStacks GetStacksFromType(MediaStackType stackType);
    //public void FlushPendingCreates();
    public void PushPendingChanges();
    public void ReadFullCatalogFromServer(MetatagSchema schema);
    public MediaItem? LookupItemFromVirtualPath(string virtualPath, string fullLocalPath);
    public MediaStacks VersionStacks { get; }
    public MediaStacks MediaStacks { get; }

    public bool HasMediaItem(Guid mediaId);
}
