using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Model.Metatags;
using Thetacat.Types;
using Metatag = Thetacat.Model.Metatags.Metatag;

namespace Thetacat.Metatags;

/*----------------------------------------------------------------------------
    %%Class: MetatagTree
    %%Qualified: Thetacat.Metatags.MetatagTree
----------------------------------------------------------------------------*/
public class MetatagTree : IMetatagTreeItem
{
    private readonly ObservableCollection<IMetatagTreeItem> RootMetatags = new();

    public MetatagTree(List<Metatag> metatags, IEnumerable<Metatag>? metatagsExclude, IEnumerable<Metatag>? metatagsInclude)
    {
        Dictionary<Guid, MetatagTreeItem> IdMap = new();

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

        if (metatagsExclude != null)
        {
            HashSet<string> exclude = new HashSet<string>();
            foreach (Metatag metatag in metatagsExclude)
            {
                exclude.Add(metatag.ID.ToString());
            }

            // walk through the tree and delete any exclusions
            SeekAndDelete(exclude);
        }

        if (metatagsInclude != null)
        {
            FilterTreeToMatches(MetatagTreeItemMatcher.CreateIdSetMatch(metatagsInclude));
        }
    }

    public void SeekAndDelete(HashSet<string> delete) => MetatagTreeItem.SeekAndDelete(this, delete);
    public bool FilterTreeToMatches(MetatagTreeItemMatcher matcher) => MetatagTreeItem.FilterTreeToMatches(this, matcher);

    public MetatagTree(List<Metatag> metatags)
    {
        Dictionary<Guid, MetatagTreeItem> IdMap = new();

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
    public MetatagTree()
    {
    }

    public ObservableCollection<IMetatagTreeItem> Children => RootMetatags;
    public string Description => "Metatags";
    public string Name => "___Root";
    public string ID => "";
    public bool? Checked { get; set; }


    public static IMetatagTreeItem? FindMatchingChild(IEnumerable<IMetatagTreeItem> Children, IMetatagMatcher<IMetatagTreeItem> matcher, int levelsToRecurse)
    {
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

        return FindMatchingChild(Children, matcher, levelsToRecurse);
    }

    public IMetatagTreeItem Clone(CloneTreeItemDelegate cloneDelegate)
    {
        MetatagTree newItem = new MetatagTree();

        cloneDelegate(newItem);
        foreach (IMetatagTreeItem item in Children)
        {
            newItem.Children.Add(item.Clone(cloneDelegate));
        }

        return newItem;
    }

    public void Preorder(VisitTreeItemDelegate visit, int depth)
    {
        MetatagTreeItem.Preorder(this, visit, depth);
    }

    public static void CloneAndSetCheckedItems(
        IEnumerable<IMetatagTreeItem>? items, 
        ObservableCollection<IMetatagTreeItem> cloneInto,
        Dictionary<string, bool?>? initialCheckboxState = null)
    {
        cloneInto.Clear();

        if (items == null)
            return;

        foreach (IMetatagTreeItem item in items)
        {
            IMetatagTreeItem newItem = item.Clone(
                innerItem =>
                {
                    if (initialCheckboxState == null || !initialCheckboxState.TryGetValue(innerItem.ID, out bool? value))
                        innerItem.Checked = false; // no entry means its not indeterminate and its not true...
                    else
                        innerItem.Checked = value;
                });
            cloneInto.Add(newItem);
        }
    }
}
