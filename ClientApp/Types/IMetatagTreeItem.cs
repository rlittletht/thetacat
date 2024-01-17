using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Types;

namespace Thetacat.Metatags;

public delegate void CloneTreeItemDelegate(IMetatagTreeItem item);
public delegate void VisitTreeItemDelegate(IMetatagTreeItem item, int depth);
public interface IMetatagTreeItem
{
    public ObservableCollection<IMetatagTreeItem> Children { get; }
    public string Description { get; }
    public string Name { get; }
    public string ID { get; }
    public bool? Checked { get; set; }
    public IMetatagTreeItem? FindMatchingChild(IMetatagMatcher<IMetatagTreeItem> matcher, int levelsToRecurse);
    public void SeekAndDelete(HashSet<string> delete);
    public bool FilterTreeToMatches(MetatagTreeItemMatcher matcher);
    public IMetatagTreeItem Clone(CloneTreeItemDelegate cloneDelegate);
    public void Preorder(VisitTreeItemDelegate visit, int depth);
}
