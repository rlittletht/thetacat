using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Thetacat.Metatags.Model;
using Thetacat.Types;

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

    public IMetatagTreeItem Clone(CloneTreeItemDelegate cloneDelegate)
    {
        MetatagTreeItem newItem =
            new MetatagTreeItem()
            {
                m_metatag = m_metatag
            };

        cloneDelegate(newItem);
        foreach (IMetatagTreeItem item in Children)
        {
            newItem.Children.Add(item.Clone(cloneDelegate));
        }

        return newItem;
    }

    public static void Preorder(IMetatagTreeItem item, VisitTreeItemDelegate visit, int depth)
    {
        visit(item, depth);
        foreach (IMetatagTreeItem child in item.Children)
        {
            child.Preorder(visit, depth + 1);
        }
    }

    public void Preorder(VisitTreeItemDelegate visit, int depth)
    {
        Preorder(this, visit, depth);
    }
}
