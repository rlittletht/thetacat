using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Thetacat.Migration.Elements;

public class MetatagTreeItem
{
    private Metatag? m_metatag;

    public string ItemId => m_metatag?.ID ?? string.Empty;
    public string? ParentId => m_metatag?.ParentID;
    public List<MetatagTreeItem> Children { get; } = new();

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
}
