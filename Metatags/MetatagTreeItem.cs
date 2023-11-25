using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Model;

namespace Thetacat.Metatags;

public class MetatagTreeItem: IMetatagTreeItem
{
    private Metatag? m_metatag;

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
}
