using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Thetacat.Metatags;
using Thetacat.Model.Metatags;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Metadata.UI;

/*----------------------------------------------------------------------------
    Thetacat.Migration.Elements.PseMetatagTreeItem
----------------------------------------------------------------------------*/
public class PseMetatagTreeItem : IMetatagTreeItem
{
    private PseMetatag? m_metatag;

    public string ItemId => m_metatag?.ID.ToString() ?? string.Empty;
    public string? ParentId => m_metatag?.ParentID.ToString();
    public ObservableCollection<IMetatagTreeItem> Children { get; } = new();
    public string Description => m_metatag?.Description ?? string.Empty;

    public string Name => m_metatag?.Name ?? string.Empty;
    public string ID => m_metatag?.ID.ToString() ?? string.Empty;
    public bool? Checked { get; set; }

    public PseMetatag Item => m_metatag ?? new PseMetatag();

    public bool IsPlaceholder { get; private set; }

    public void MakeOrphan()
    {
        Debug.Assert(m_metatag != null, nameof(m_metatag) + " != null");
        m_metatag.ParentID = 0;
    }

    public static PseMetatagTreeItem CreateFromMetatag(PseMetatag item)
    {
        PseMetatagTreeItem pseMetatag = new()
        {
            m_metatag = item
        };
        return pseMetatag;
    }

    public static PseMetatagTreeItem CreateParentPlaceholder(int id)
    {
        PseMetatagTreeItem pseMetatag = new()
        {
            m_metatag = new PseMetatag
            {
                ID = id
            },
            IsPlaceholder = true
        };

        return pseMetatag;
    }

    public void MaterializePlaceholder(PseMetatag pseMetatag)
    {
        m_metatag = pseMetatag;
        IsPlaceholder = false;
    }

    public void AddChild(PseMetatagTreeItem treeItem)
    {
        Children.Add(treeItem);
    }

    public void SeekAndDelete(HashSet<string> delete) => MetatagTreeItem.SeekAndDelete(this, delete);
    public bool FilterTreeToMatches(MetatagTreeItemMatcher matcher) => MetatagTreeItem.FilterTreeToMatches(this, matcher);

    /*----------------------------------------------------------------------------
        %%Function: FindMatchingChild
        %%Qualified: Thetacat.Metatags.PseMetatagTree.FindMatchingChild

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
        PseMetatagTreeItem newItem =
            new PseMetatagTreeItem()
            {
                m_metatag = m_metatag,
                IsPlaceholder = IsPlaceholder
            };

        cloneDelegate(newItem);
        foreach (IMetatagTreeItem item in Children)
        {
            newItem.Children.Add(item.Clone(cloneDelegate));
        }

        return newItem;
    }

    public void Preorder(VisitTreeItemDelegate visit, int depth)
    {
        MetatagTreeItem.Preorder(this, visit, depth);
    }
}
