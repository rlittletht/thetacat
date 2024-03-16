using System;
using System.Collections.ObjectModel;

namespace Thetacat.Explorer;

public class ExplorerContextMenuModel
{
    public int RecentTagVectorClock { get; set; } = -1;

    public ObservableCollection<ExplorerMenuTag> AppliedTags { get; set; } =
        new()
        {
            new ExplorerMenuTag() { MediaTagId = Guid.NewGuid(), TagName = "Tag1" },
            new ExplorerMenuTag() { MediaTagId = Guid.NewGuid(), TagName = "Tag2" },
            new ExplorerMenuTag() { MediaTagId = Guid.NewGuid(), TagName = "Tag3" },
        };
    public ObservableCollection<ExplorerMenuTag> AdvertisedTags { get; set; } =
        new()
        {
            new ExplorerMenuTag() { MediaTagId = Guid.NewGuid(), TagName = "Recent Tag1" },
            new ExplorerMenuTag() { MediaTagId = Guid.NewGuid(), TagName = "Recent Tag2" },
            new ExplorerMenuTag() { MediaTagId = Guid.NewGuid(), TagName = "Recent Tag3" },
        };
}
