using System;
using System.Collections.ObjectModel;

namespace Thetacat.Explorer;

public class ExplorerContextMenuModel
{
    public int RecentTagVectorClock { get; set; } = -1;

    public ObservableCollection<ExplorerMenuTag> AppliedTags { get; set; } = new();
    public ObservableCollection<ExplorerMenuTag> AdvertisedTags { get; set; } = new();
}
