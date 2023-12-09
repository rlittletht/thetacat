using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Model.Metatags;
using Thetacat.Types;

namespace Thetacat.Metatags;

public class MetatagTreeItem: IMetatagTreeItem
{
    private Metatag? m_metatag;

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
}
