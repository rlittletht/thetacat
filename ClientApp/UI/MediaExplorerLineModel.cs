using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Thetacat.Util;

namespace Thetacat.UI;

public class MediaExplorerLineModel: IObservableCollectionHolder<MediaExplorerItem>
{
    public ObservableCollection<MediaExplorerItem> Items { get; set; } = new ObservableCollection<MediaExplorerItem>();
    public string TestName { get; set; } = "";
}
