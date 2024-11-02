using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Thetacat.Metatags.Model;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Metatags;

public class MetatagTreeItem: IMetatagTreeItem
{
    private Metatag? m_metatag;

    public bool? Checked { get; set; }
    public bool IsLocalOnly { get; set; }
    public Guid ItemId => m_metatag?.ID ?? Guid.Empty;
    public Guid? ParentId => m_metatag?.Parent;
    public ObservableCollection<IMetatagTreeItem> Children { get; } = new();

    public string Description => m_metatag?.Description ?? String.Empty;
    public string Name => m_metatag?.Name ?? String.Empty;
    public string ID => m_metatag?.ID.ToString() ?? String.Empty;

    public bool IsPlaceholder { get; private init; }

    public static MetatagTreeItem CreateFromMetatag(Metatag item)
    {
        MetatagTreeItem metatag = new()
        {
            m_metatag = item
        };
        return metatag;
    }

    public static MetatagTreeItem CreateParentPlaceholder(Guid id)
    {
        MetatagTreeItem metatag = new()
        {
            m_metatag = new Metatag
            {
                ID = id
            },
            IsPlaceholder = true
        };

        return metatag;
    }

    public void MaterializePlaceholder(Metatag metatag)
    {
        m_metatag = metatag;
    }

    public void AddChild(MetatagTreeItem treeItem)
    {
        Children.Add(treeItem);
    }

    public static void SeekAndDelete(IMetatagTreeItem tree, HashSet<string> delete)
    {
        for (int i = tree.Children.Count - 1; i >= 0; i--)
        {
            if (delete.Contains(tree.Children[i].ID))
            {
                tree.Children.RemoveAt(i);
                continue;
            }

            tree.Children[i].SeekAndDelete(delete);
        }
    }

    public static bool FilterTreeToMatches(IMetatagTreeItem tree, MetatagTreeItemMatcher matcher)
    {
        bool fMatched = matcher.IsMatch(tree);

        // even if we matched, we might have other matches in other children
        for (int i = tree.Children.Count - 1; i >= 0; i--)
        {
            if (!tree.Children[i].FilterTreeToMatches(matcher))
            {
                // this item didn't have a match. delete it
                tree.Children.RemoveAt(i);
            }
            else
            {
                fMatched = true;
            }
        }

        return fMatched; // if we matched this item, or any of our descendents
    }

    public void SeekAndDelete(HashSet<string> delete) => MetatagTreeItem.SeekAndDelete(this, delete);
    public bool FilterTreeToMatches(MetatagTreeItemMatcher matcher) => MetatagTreeItem.FilterTreeToMatches(this, matcher);

    /*----------------------------------------------------------------------------
        %%Function: FindMatchingChild
        %%Qualified: Thetacat.Metatags.MetatagTree.FindMatchingChild

        Find the given named child (in this item or below). we will only
        recurse the given number of levels (-1 means recurse all levels)
    ----------------------------------------------------------------------------*/
    public IMetatagTreeItem? FindMatchingChild(IMetatagMatcher<IMetatagTreeItem> treeItemMatcher, int levelsToRecurse)
    {
        if (treeItemMatcher.IsMatch(this))
            return this;

        if (levelsToRecurse == 0)
            return null;

        if (levelsToRecurse != -1)
            levelsToRecurse--;

        foreach (IMetatagTreeItem item in Children)
        {
            IMetatagTreeItem? matched = item.FindMatchingChild(treeItemMatcher, levelsToRecurse);
            if (matched != null)
                return matched;
        }

        return null;
    }

    public static IMetatagTreeItem? FindParentOfChild(IMetatagTreeItem item, IMetatagMatcher<IMetatagTreeItem> treeItemMatcher)
    {
        if (treeItemMatcher.IsMatch(item))
            throw new CatExceptionInternalFailure("should never match <this> when looking for parent");

        foreach (IMetatagTreeItem child in item.Children)
        {
            if (treeItemMatcher.IsMatch(child))
                return item;

            IMetatagTreeItem? parent = child.FindParentOfChild(treeItemMatcher);

            if (parent != null)
                return parent;
        }

        return null;
    }
    
    public IMetatagTreeItem? FindParentOfChild(IMetatagMatcher<IMetatagTreeItem> treeItemMatcher) => FindParentOfChild(this, treeItemMatcher);

    public IMetatagTreeItem Clone(CloneTreeItemDelegate cloneDelegatePreChildren, CloneTreeItemChildrenDelegate? cloneDelegatePostChildren)
    {
        MetatagTreeItem newItem =
            new MetatagTreeItem()
            {
                m_metatag = m_metatag
            };

        cloneDelegatePreChildren(newItem);
        List<IMetatagTreeItem>? workingBuffer = cloneDelegatePostChildren != null ? new List<IMetatagTreeItem>() : null;

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

    public static void Preorder(IMetatagTreeItem item, IMetatagTreeItem? parent, VisitTreeItemDelegate visit, int depth)
    {
        visit(item, parent, depth);
        foreach (IMetatagTreeItem child in item.Children)
        {
            child.Preorder(item, visit, depth + 1);
        }
    }

    public void Preorder(IMetatagTreeItem? parent, VisitTreeItemDelegate visit, int depth)
    {
        Preorder(this, parent, visit, depth);
    }
}
