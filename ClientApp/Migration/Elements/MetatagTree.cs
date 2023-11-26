using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Metatags;

namespace Thetacat.Migration.Elements;

/*----------------------------------------------------------------------------
    %%Class: MetatagTree
    %%Qualified: Thetacat.Migration.Elements.MetatagTree
----------------------------------------------------------------------------*/
public class MetatagTree: IMetatagTreeItem
{
    private readonly Dictionary<string, MetatagTreeItem> IdMap = new();
    private readonly ObservableCollection<IMetatagTreeItem> RootMetatags = new();

    public string Description => string.Empty;

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

    public ObservableCollection<IMetatagTreeItem> Children => RootMetatags;
    public string Name => "___Root";
    public string ID => "";

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
