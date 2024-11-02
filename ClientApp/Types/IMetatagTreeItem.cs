using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Types;

namespace Thetacat.Metatags;

public delegate void CloneTreeItemDelegate(IMetatagTreeItem item);
public delegate void CloneTreeItemChildrenDelegate(List<IMetatagTreeItem> children);
public delegate void VisitTreeItemDelegate(IMetatagTreeItem item, IMetatagTreeItem? parent, int depth);
public interface IMetatagTreeItem
{
    public ObservableCollection<IMetatagTreeItem> Children { get; }
    public string Description { get; }
    public string Name { get; }
    public string ID { get; }
    public bool? Checked { get; set; }
    public IMetatagTreeItem? FindMatchingChild(IMetatagMatcher<IMetatagTreeItem> matcher, int levelsToRecurse);
    public IMetatagTreeItem? FindParentOfChild(IMetatagMatcher<IMetatagTreeItem> matcher);
    public void SeekAndDelete(HashSet<string> delete);
    public bool FilterTreeToMatches(MetatagTreeItemMatcher matcher);
    public IMetatagTreeItem Clone(CloneTreeItemDelegate cloneDelegatePreChildren, CloneTreeItemChildrenDelegate? cloneDelegatePostChildren);
    public void Preorder(IMetatagTreeItem? parent, VisitTreeItemDelegate visit, int depth);
}
