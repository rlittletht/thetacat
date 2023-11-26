using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Thetacat.Migration.Elements;

public class ElementsMetatagTreeItem
{
    private ElementsMetatag? m_metatag;

    public string ItemId => m_metatag?.ID ?? string.Empty;
    public string? ParentId => m_metatag?.ParentID;
    public List<ElementsMetatagTreeItem> Children { get; } = new();

    public string Name => m_metatag?.Name ?? string.Empty;
    public string ID => m_metatag?.ID ?? string.Empty;

    public ElementsMetatag Item => m_metatag ?? new ElementsMetatag();

    public bool IsPlaceholder { get; private init; }

    public static ElementsMetatagTreeItem CreateFromMetatag(ElementsMetatag item)
    {
        ElementsMetatagTreeItem metatag = new()
        {
            m_metatag = item
        };
        return metatag;
    }

    public static ElementsMetatagTreeItem CreateParentPlaceholder(string id)
    {
        ElementsMetatagTreeItem metatag = new()
        {
            m_metatag = new ElementsMetatag
            {
                ID = id
            },
            IsPlaceholder = true
        };

        return metatag;
    }

    public void MaterializePlaceholder(ElementsMetatag metatag)
    {
        m_metatag = metatag;
    }

    public void AddChild(ElementsMetatagTreeItem treeItem)
    {
        Children.Add(treeItem);
    }
}
