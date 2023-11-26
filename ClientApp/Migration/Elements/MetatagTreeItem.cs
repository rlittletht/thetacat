using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Metatags;

namespace Thetacat.Migration.Elements;

/*----------------------------------------------------------------------------
    Thetacat.Migration.Elements.MetatagTreeItem
----------------------------------------------------------------------------*/
public class MetatagTreeItem: IMetatagTreeItem
{
    private Metatag? m_metatag;

    public string ItemId => m_metatag?.ID ?? string.Empty;
    public string? ParentId => m_metatag?.ParentID;
    public ObservableCollection<IMetatagTreeItem> Children { get; } = new();
    public string Description => string.Empty;

    public string Name => m_metatag?.Name ?? string.Empty;
    public string ID => m_metatag?.ID ?? string.Empty;

    public Metatag Item => m_metatag ?? new Metatag();

    public bool IsPlaceholder { get; private init; }

    public static MetatagTreeItem CreateFromMetatag(Metatag item)
    {
        MetatagTreeItem metatag = new()
        {
            m_metatag = item
        };
        return metatag;
    }

    public static MetatagTreeItem CreateParentPlaceholder(string id)
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

    public IMetatagTreeItem? FindChildByName(string name)
    {
        foreach (IMetatagTreeItem item in Children)
        {
            if (string.Compare(item.Name, name, StringComparison.CurrentCultureIgnoreCase) == 0)
                return item;
        }

        return null;
    }
}
