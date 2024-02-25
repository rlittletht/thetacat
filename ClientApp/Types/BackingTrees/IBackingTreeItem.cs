using System.Collections.ObjectModel;
using Thetacat.Controls.BackedTreeViewControl;

namespace Thetacat.Types;

public delegate void CloneBackingTreeItemDelegate(IBackingTreeItem item);
public delegate void VisitBackingTreeItemDelegate(IBackingTreeItem item, IBackingTreeItem? parent, int depth);

public interface IBackingTreeItem: IBackingTreeItemData
{
    public ObservableCollection<IBackingTreeItem> Children { get; }
    public IBackingTreeItem Clone(CloneBackingTreeItemDelegate cloneDelegate);
}
