using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Types;
using Metatag = Thetacat.Model.Metatags.Metatag;

namespace Thetacat.Metatags;

/*----------------------------------------------------------------------------
    %%Class: MetatagTree
    %%Qualified: Thetacat.Metatags.MetatagTree
----------------------------------------------------------------------------*/
public class MetatagTree : IMetatagTreeItem
{
    private readonly Dictionary<Guid, MetatagTreeItem> IdMap = new();
    private readonly ObservableCollection<IMetatagTreeItem> RootMetatags = new();

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
    public string Name => "___Root";
    public string ID => "";

    /*----------------------------------------------------------------------------
        %%Function: FindMatchingChild
        %%Qualified: Thetacat.Metatags.MetatagTree.FindMatchingChild

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
