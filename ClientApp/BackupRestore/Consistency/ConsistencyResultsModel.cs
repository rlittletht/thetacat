using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.UI.Controls.MediaItemsListControl;

namespace Thetacat.BackupRestore.Consistency;

public class ConsistencyResultsModel
{
    public ObservableCollection<MediaItemsListItem> TestItems { get; set; } = new();
    public ObservableCollection<ConsistencyResult> Results { get; set; } = new();
}
