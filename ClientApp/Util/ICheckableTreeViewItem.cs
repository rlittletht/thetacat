using System.Collections.ObjectModel;

namespace Thetacat.Util;

public interface ICheckableTreeViewItem<T> where T : class, ICheckableTreeViewItem<T>
{
    public bool Checked { get; set; }
    public ObservableCollection<T> Children { get; set; }
}
