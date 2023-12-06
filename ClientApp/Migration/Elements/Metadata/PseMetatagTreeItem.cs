using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Thetacat.Metatags;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Metadata.UI;

/*----------------------------------------------------------------------------
    Thetacat.Migration.Elements.PseMetatagTreeItem
----------------------------------------------------------------------------*/
public class PseMetatagTreeItem : IMetatagTreeItem
{
    private PseMetatag? m_metatag;

    public string ItemId => m_metatag?.ID ?? string.Empty;
    public string? ParentId => m_metatag?.ParentID;
    public ObservableCollection<IMetatagTreeItem> Children { get; } = new();
    public string Description => string.Empty;

    public string Name => m_metatag?.Name ?? string.Empty;
    public string ID => m_metatag?.ID ?? string.Empty;

    public PseMetatag Item => m_metatag ?? new PseMetatag();

    public bool IsPlaceholder { get; private set; }

    public void MakeOrphan()
    {
        Debug.Assert(m_metatag != null, nameof(m_metatag) + " != null");
        m_metatag.ParentID = string.Empty;
    }

    public static PseMetatagTreeItem CreateFromMetatag(PseMetatag item)
    {
        PseMetatagTreeItem pseMetatag = new()
        {
            m_metatag = item
        };
        return pseMetatag;
    }

    public static PseMetatagTreeItem CreateParentPlaceholder(string id)
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
}
