using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Thetacat.UI;

public class MediaExplorerLineModel
{
    public ObservableCollection<MediaExplorerItem> Items { get; set; } = new ObservableCollection<MediaExplorerItem>();
    public string TestName { get; set; } = "";
}
