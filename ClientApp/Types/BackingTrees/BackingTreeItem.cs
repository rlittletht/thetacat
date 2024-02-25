using System.Collections.ObjectModel;
using System.Security.Permissions;

namespace Thetacat.Types;

public class BackingTreeItem<T>: IBackingTreeItem where T:IBackingTreeItemData
{
    public ObservableCollection<IBackingTreeItem> Children { get; } = new();
    public string Name => Data.Name;
    public string Description => Data.Description;

    public bool? Checked
    {
        get => Data.Checked;
        set => Data.Checked = value;
    }

    public T Data;

    public BackingTreeItem(T t)
    {
        Data = t;
    }

    public IBackingTreeItem Clone(CloneBackingTreeItemDelegate cloneDelegate)
    {
        BackingTreeItem<T> newItem = new(Data);

        cloneDelegate(newItem);
        foreach (IBackingTreeItem item in Children)
        {
            newItem.Children.Add(item.Clone(cloneDelegate));
        }

        return newItem;
    }

    public static void Preorder(IBackingTreeItem item, IBackingTreeItem? parent, VisitBackingTreeItemDelegate visit, int depth)
    {
        visit(item, parent, depth);
        foreach (IBackingTreeItem child in item.Children)
        {
            Preorder(child, item, visit, depth + 1);
        }
    }
}
