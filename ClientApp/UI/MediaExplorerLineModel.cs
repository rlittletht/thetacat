using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Thetacat.Util;

namespace Thetacat.UI;

public class MediaExplorerLineModel: IObservableSegmentableCollectionHolder<MediaExplorerItem>
{
    public ObservableCollection<MediaExplorerItem> Items { get; set; } = new ObservableCollection<MediaExplorerItem>();
    public bool EndSegmentAfter { get; set; } = false;
    public string TestName { get; set; } = "";
}
