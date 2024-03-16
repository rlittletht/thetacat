using System.Collections.Generic;
using System.Collections.ObjectModel;
using TCore.PostfixText;
using Thetacat.Controls.BackedTreeViewControl;

namespace Thetacat.Types;

// splitter should take an aggregate data and break into parts. if the last part is a "leaf", its key should be null
// e.g. ["foo/bar/"] => { "foo": [foo], "bar": [bar] }
// e.g. ["foo/bar/baz.txt"] => { "foo": [foo], "bar": [bar], null: [baz.txt] }
public delegate KeyValuePair<TPart?, T>[] BackingTreeItemSplitterDelegate<T, TPart>(T data);
public delegate bool? GetTreeItemStateDelegate(IBackingTreeItem item);

public class BackingTree: IBackingTreeItem 
{
    public ObservableCollection<IBackingTreeItem> Children { get; } = new ();

    public string Name => "__ROOT";
    public string Description => "Tree Root";
    public bool? Checked { get; set; }

    public IBackingTreeItem Clone(CloneBackingTreeItemDelegate cloneDelegate)
    {
        BackingTree newItem = new();

        cloneDelegate(newItem);
        foreach (IBackingTreeItem item in Children)
        {
            newItem.Children.Add(item.Clone(cloneDelegate));
        }

        return newItem;
    }

    public static void CloneAndSetCheckedItems(
        IEnumerable<IBackingTreeItem>? items,
        ObservableCollection<IBackingTreeItem> cloneInto,
        GetTreeItemStateDelegate? checkDelegate = null)
    {
        cloneInto.Clear();

        if (items == null)
            return;

        foreach (IBackingTreeItem item in items)
        {
            IBackingTreeItem newItem = item.Clone(
                innerItem =>
                {
                    if (checkDelegate == null)
                        innerItem.Checked = false; // no entry means its not indeterminate and its not true...
                    else
                    {
                        bool? value = checkDelegate(innerItem);
                        innerItem.Checked = value;
                    }
                });
            cloneInto.Add(newItem);
        }
    }

    class Partition<T, TPart> where TPart : notnull
    {
        public Dictionary<TPart, Partition<T, TPart>> parts = new();
        public List<T> data = new();
        public T PartData;

        public Partition(T partData)
        {
            PartData = partData;
        }

        public Partition<T, TPart> AddChildPart(TPart part, T t)
        {
            if (!parts.TryGetValue(part, out Partition<T, TPart>? newPart))
            {
                newPart = new Partition<T, TPart>(t);
                parts.Add(part, newPart);
            }

            return newPart;
        }

        public void AddChildParts(KeyValuePair<TPart?, T>[] partsToAdd)
        {
            if (partsToAdd.Length == 0)
                return;

            if (partsToAdd[0].Key == null)
            {
                data.Add(partsToAdd[0].Value);
                return;
            }

            Partition<T, TPart> newPart = AddChildPart(partsToAdd[0].Key!, partsToAdd[0].Value);
            newPart.AddChildParts(partsToAdd[1..]);
        }
    }

    static void AddPartToTreeItem<T, TPart>(
        Partition<T, TPart> part,
        IBackingTreeItem treeItem) 
        where T: IBackingTreeItemData
        where TPart : notnull
    {
        foreach (T t in part.data)
        {
            treeItem.Children.Add(new BackingTreeItem<T>(t));
        }

        foreach (KeyValuePair<TPart, Partition<T, TPart>> child in part.parts)
        {
            BackingTreeItem<T> newItem = new(child.Value.PartData);

            treeItem.Children.Add(newItem);
            AddPartToTreeItem(child.Value, newItem);
        }
    }

    public static BackingTree CreateFromList<T, TPart>(
        IEnumerable<T> items,
        BackingTreeItemSplitterDelegate<T, TPart> splitter,
        T tRoot) 
        where T: IBackingTreeItemData
        where TPart : notnull
    {
        BackingTree tree = new();

        Partition<T, TPart> root = new Partition<T, TPart>(tRoot);

        foreach (T item in items)
        {
            root.AddChildParts(splitter(item));
        }

        // now that have all the parts, create the tree
        AddPartToTreeItem(root, tree);
        return tree;
    }
}
