using System;
using System.Collections.ObjectModel;

namespace Thetacat.UI.Explorer;

public class ExplorerContextMenuModel
{
    public ObservableCollection<ExplorerMenuTag> AppliedTags { get; set; } = 
        new()
        {
            new ExplorerMenuTag() { MediaTagId = Guid.NewGuid(), TagName = "Tag1" },
            new ExplorerMenuTag() { MediaTagId = Guid.NewGuid(), TagName = "Tag2" },
            new ExplorerMenuTag() { MediaTagId = Guid.NewGuid(), TagName = "Tag3" },
        };
    public ObservableCollection<ExplorerMenuTag> AdvertisedTags { get; set; } = new();
}
