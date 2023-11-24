using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Model;

namespace Thetacat.Metatags;

public class MetatagTree: IMetatagTreeItem
{
    private readonly Dictionary<Guid, MetatagTreeItem> IdMap = new();
    private readonly ObservableCollection<IMetatagTreeItem> RootMetatags = new();

    public MetatagTree(List<Metatag> metatags)
    {
        foreach (Metatag metatag in metatags)
        {
            MetatagTreeItem treeItem = MetatagTreeItem.CreateFromMetatag(metatag);

            if (IdMap.ContainsKey(treeItem.ItemId))
            {
                // if we already have the id, it had better have been a placeholder created for
                // a parent id we hadn't seen yet
                if (!IdMap[treeItem.ItemId].IsPlaceholder)
                    throw new Exception($"duplicate id {treeItem.ItemId}");
            }
            else
            {
                IdMap.Add(treeItem.ItemId, treeItem);
            }

            if (treeItem.ParentId == null)
            {
                RootMetatags.Add(treeItem);
            }
            else
            {
                if (!IdMap.ContainsKey(treeItem.ParentId.Value))
                {
                    IdMap.Add(
                        treeItem.ParentId.Value,
                        MetatagTreeItem.CreateParentPlaceholder(treeItem.ParentId.Value));
                }
                IdMap[treeItem.ParentId.Value].AddChild(treeItem);
            }
        }
    }

    public ObservableCollection<IMetatagTreeItem> Children => RootMetatags;
    public string Description => "Metatags";
    public string Name => "Root";
    public string ID => "";
}
