using System.Collections.Generic;
using System.Collections.ObjectModel;
using TCore;

namespace Thetacat.Util;

public interface ICheckableTreeViewItem<T> where T : class, ICheckableTreeViewItem<T>
{
    public bool Checked { get; set; }
    public ObservableCollection<T> Children { get; set; }
}
