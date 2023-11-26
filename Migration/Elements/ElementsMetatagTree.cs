using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Thetacat.Migration.Elements;

public class ElementsMetatagTree
{
    private readonly Dictionary<string, ElementsMetatagTreeItem> IdMap = new();
    private readonly List<ElementsMetatagTreeItem> RootMetatags = new();

    public ElementsMetatagTree(List<ElementsMetatag> metatags)
    {
        foreach (ElementsMetatag metatag in metatags)
        {
            ElementsMetatagTreeItem treeItem;

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
                treeItem = ElementsMetatagTreeItem.CreateFromMetatag(metatag);
                IdMap.Add(treeItem.ItemId, treeItem);
            }

            if (treeItem.ParentId == null)
            {
                RootMetatags.Add(treeItem);
            }
            else
            {
                if (!IdMap.ContainsKey(treeItem.ParentId))
                {
                    IdMap.Add(
                        treeItem.ParentId,
                        ElementsMetatagTreeItem.CreateParentPlaceholder(treeItem.ParentId));
                }

                IdMap[treeItem.ParentId].AddChild(treeItem);
            }
        }
    }

    public ElementsMetatag GetTagFromId(string id)
    {
        return IdMap[id].Item;
    }

    public List<ElementsMetatagTreeItem> Children => RootMetatags;
    public string Name => "Root";
    public string ID => "";
}
