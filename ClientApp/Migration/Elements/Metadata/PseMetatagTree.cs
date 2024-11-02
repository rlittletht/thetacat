using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Metatags;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Migration.Elements.Metadata.UI;

/*----------------------------------------------------------------------------
    %%Class: PseMetatagTree
    %%Qualified: Thetacat.Migration.Elements.PseMetatagTree

    PseMetatag tree specific to Photoshop Elements Metatags (for migration)
----------------------------------------------------------------------------*/
public class PseMetatagTree : IMetatagTreeItem
{
    private readonly Dictionary<int, PseMetatagTreeItem> IdMap = new();
    private readonly ObservableCollection<IMetatagTreeItem> RootMetatags = new();

    public string Description => string.Empty;
    public bool? Checked { get; set; }

    public PseMetatagTree()
    {
    }

    public PseMetatagTree(IEnumerable<PseMetatag> metatags)
    {
        foreach (PseMetatag metatag in metatags)
        {
            PseMetatagTreeItem treeItem;

            if (IdMap.ContainsKey(metatag.ID))
            {
                // if we already have the id, it had better have been a placeholder created for
                // a parent id we hadn't seen yet
                if (!IdMap[metatag.ID].IsPlaceholder)
                    throw new Exception($"duplicate id {metatag.ID}");

                IdMap[metatag.ID].MaterializePlaceholder(metatag);

                // reset this so we don't use the one we just created
                treeItem = IdMap[metatag.ID];
            }
            else
            {
                treeItem = PseMetatagTreeItem.CreateFromMetatag(metatag);
                IdMap.Add(int.Parse(treeItem.ItemId), treeItem);
            }

            if (!string.IsNullOrEmpty(treeItem.ParentId) && treeItem.ParentId != "0")
            {
                if (!IdMap.ContainsKey(int.Parse(treeItem.ParentId)))
                {
                    IdMap.Add(
                        int.Parse(treeItem.ParentId),
                        PseMetatagTreeItem.CreateParentPlaceholder(int.Parse(treeItem.ParentId)));
                }

                IdMap[int.Parse(treeItem.ParentId)].AddChild(treeItem);
            }
        }
        // lastly, clean up anything that is just placeholders and fixup their
        // parents to be empty
        HashSet<int> keysToDelete = new();

        foreach (int id in IdMap.Keys)
        {
            PseMetatagTreeItem item = IdMap[id];

            if (!string.IsNullOrEmpty(item.ParentId) && item.ParentId != "0")
            {
                if (item.IsPlaceholder)
                    keysToDelete.Add(id);

                if (!IdMap.ContainsKey(int.Parse(item.ParentId)) || IdMap[int.Parse(item.ParentId)].IsPlaceholder)
                    item.MakeOrphan();
            }

            if (string.IsNullOrEmpty(item.ParentId) || item.ParentId == "0")
                RootMetatags.Add(item);
        }

        foreach (int key in keysToDelete)
        {
            IdMap.Remove(key);
        }
    }

    public PseMetatag GetTagFromId(int id)
    {
        return IdMap[id].Item;
    }

    public ObservableCollection<IMetatagTreeItem> Children => RootMetatags;
    public string Name => "___Root";
    public string ID => "";

    public void SeekAndDelete(HashSet<string> delete) => MetatagTreeItem.SeekAndDelete(this, delete);
    public bool FilterTreeToMatches(MetatagTreeItemMatcher matcher) => MetatagTreeItem.FilterTreeToMatches(this, matcher);
    public IMetatagTreeItem? FindParentOfChild(IMetatagMatcher<IMetatagTreeItem> treeItemMatcher) => MetatagTreeItem.FindParentOfChild(this, treeItemMatcher);

    /*----------------------------------------------------------------------------
        %%Function: FindMatchingChild
        %%Qualified: Thetacat.Metatags.PseMetatagTree.FindMatchingChild

        Find the given named child (in this item or below). we will only
        recurse the given number of levels (-1 means recurse all levels)
    ----------------------------------------------------------------------------*/
    public IMetatagTreeItem? FindMatchingChild(IMetatagMatcher<IMetatagTreeItem> matcher, int levelsToRecurse)
    {
        if (matcher.IsMatch(this))
            return this;

        if (levelsToRecurse == 0)
            return null;

        if (levelsToRecurse != -1)
            levelsToRecurse--;

        foreach (IMetatagTreeItem item in Children)
        {
            IMetatagTreeItem? matched = item.FindMatchingChild(matcher, levelsToRecurse);
            if (matched != null)
                return matched;
        }

        return null;
    }

    public IMetatagTreeItem Clone(CloneTreeItemDelegate cloneDelegatePreChildren, CloneTreeItemChildrenDelegate? cloneDelegatePostChildren)
    {
        PseMetatagTree newItem = new PseMetatagTree();
        List<IMetatagTreeItem>? workingBuffer = cloneDelegatePostChildren != null ? new List<IMetatagTreeItem>() : null;

        cloneDelegatePreChildren(newItem);
        foreach (IMetatagTreeItem item in Children)
        {
            IMetatagTreeItem clone = item.Clone(cloneDelegatePreChildren, cloneDelegatePostChildren);

            if (workingBuffer == null)
                newItem.Children.Add(clone);
            else
                workingBuffer.Add(clone);
        }

        // if we have a postChildren delegate, then operate on the buffer and add it to the Children
        if (workingBuffer != null)
        {
            cloneDelegatePostChildren?.Invoke(workingBuffer);
            newItem.Children.AddRange(workingBuffer);
        }

        return newItem;
    }

    public void Preorder(IMetatagTreeItem? parent, VisitTreeItemDelegate visit, int depth)
    {
        MetatagTreeItem.Preorder(this, parent, visit, depth);
    }
}
