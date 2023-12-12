using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Metatags;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Metadata.UI;

/*----------------------------------------------------------------------------
    %%Class: PseMetatagTree
    %%Qualified: Thetacat.Migration.Elements.PseMetatagTree

    PseMetatag tree specific to Photoshop Elements Metatags (for migration)
----------------------------------------------------------------------------*/
public class PseMetatagTree : IMetatagTreeItem
{
    private readonly Dictionary<int, PseMetatagTreeItem> IdMap = new();
    private readonly ObservableCollection<IMetatagTreeItem> RootMetatags = new();

    public string Description => string.Empty;

    public PseMetatagTree(IEnumerable<PseMetatag> metatags)
    {
        foreach (PseMetatag metatag in metatags)
        {
            PseMetatagTreeItem treeItem;

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
                treeItem = PseMetatagTreeItem.CreateFromMetatag(metatag);
                IdMap.Add(int.Parse(treeItem.ItemId), treeItem);
            }

            if (!string.IsNullOrEmpty(treeItem.ParentId) && treeItem.ParentId != "0")
            {
                if (!IdMap.ContainsKey(int.Parse(treeItem.ParentId)))
                {
                    IdMap.Add(
                        int.Parse(treeItem.ParentId),
                        PseMetatagTreeItem.CreateParentPlaceholder(int.Parse(treeItem.ParentId)));
                }

                IdMap[int.Parse(treeItem.ParentId)].AddChild(treeItem);
            }
        }
        // lastly, clean up anything that is just placeholders and fixup their
        // parents to be empty
        HashSet<int> keysToDelete = new();

        foreach (int id in IdMap.Keys)
        {
            PseMetatagTreeItem item = IdMap[id];

            if (!string.IsNullOrEmpty(item.ParentId) && item.ParentId != "0")
            {
                if (item.IsPlaceholder)
                    keysToDelete.Add(id);

                if (!IdMap.ContainsKey(int.Parse(item.ParentId)) || IdMap[int.Parse(item.ParentId)].IsPlaceholder)
                    item.MakeOrphan();
            }

            if (string.IsNullOrEmpty(item.ParentId) || item.ParentId == "0")
                RootMetatags.Add(item);
        }

        foreach (int key in keysToDelete)
        {
            IdMap.Remove(key);
        }
    }

    public PseMetatag GetTagFromId(int id)
    {
        return IdMap[id].Item;
    }

    public ObservableCollection<IMetatagTreeItem> Children => RootMetatags;
    public string Name => "___Root";
    public string ID => "";

    /*----------------------------------------------------------------------------
        %%Function: FindMatchingChild
        %%Qualified: Thetacat.Metatags.PseMetatagTree.FindMatchingChild

        Find the given named child (in this item or below). we will only
        recurse the given number of levels (-1 means recurse all levels)
    ----------------------------------------------------------------------------*/
    public IMetatagTreeItem? FindMatchingChild(IMetatagMatcher<IMetatagTreeItem> matcher, int levelsToRecurse)
    {
        if (matcher.IsMatch(this))
            return this;

        if (levelsToRecurse == 0)
            return null;

        if (levelsToRecurse != -1)
            levelsToRecurse--;

        foreach (IMetatagTreeItem item in Children)
        {
            IMetatagTreeItem? matched = item.FindMatchingChild(matcher, levelsToRecurse);
            if (matched != null)
                return matched;
        }

        return null;
    }
}
