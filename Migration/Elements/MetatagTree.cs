using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Thetacat.Migration.Elements;

public class MetatagTree
{
    private readonly Dictionary<string, MetatagTreeItem> IdMap = new();
    private readonly List<MetatagTreeItem> RootMetatags = new();

    public MetatagTree(List<Metatag> metatags)
    {
        foreach (Metatag metatag in metatags)
        {
            MetatagTreeItem treeItem;

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
                treeItem = MetatagTreeItem.CreateFromMetatag(metatag);
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
                        MetatagTreeItem.CreateParentPlaceholder(treeItem.ParentId));
                }

                IdMap[treeItem.ParentId].AddChild(treeItem);
            }
        }
    }

    public Metatag GetTagFromId(string id)
    {
        return IdMap[id].Item;
    }

    public List<MetatagTreeItem> Children => RootMetatags;
    public string Name => "Root";
    public string ID => "";
}
