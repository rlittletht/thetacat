﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Thetacat.Types;
using Thetacat.Util;
using Metatag = Thetacat.Metatags.Model.Metatag;

namespace Thetacat.Metatags;

/*----------------------------------------------------------------------------
    %%Class: MetatagTree
    %%Qualified: Thetacat.Metatags.MetatagTree
----------------------------------------------------------------------------*/
public class MetatagTree : IMetatagTreeItem
{
    private readonly ObservableCollection<IMetatagTreeItem> RootMetatags = new();

    public MetatagTree(IEnumerable<Metatag> metatags, IEnumerable<Metatag>? metatagsExclude, IEnumerable<Metatag>? metatagsInclude)
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

    public MetatagTree(IEnumerable<Metatag> metatags)
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
    public string? Value
    {
        get => null;
        set => throw new CatExceptionInternalFailure("can't set tree value");
    }

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

    public IMetatagTreeItem? FindParentOfChild(IMetatagMatcher<IMetatagTreeItem> treeItemMatcher) => MetatagTreeItem.FindParentOfChild(this, treeItemMatcher);

    public IMetatagTreeItem Clone(CloneTreeItemDelegate cloneDelegatePreChildren, CloneTreeItemDelegate? cloneDelegatePostChildren)
    {
        MetatagTree newItem = new MetatagTree();

        cloneDelegatePreChildren(newItem);
        foreach (IMetatagTreeItem item in Children)
        {
            IMetatagTreeItem clone = item.Clone(cloneDelegatePreChildren, cloneDelegatePostChildren);

            newItem.Children.Add(clone);
        }

        cloneDelegatePostChildren?.Invoke(newItem);

        return newItem;
    }

    public void Preorder(IMetatagTreeItem? parent, VisitTreeItemDelegate visit, int depth)
    {
        MetatagTreeItem.Preorder(this, parent, visit, depth);
    }

    public static void CloneAndAddCheckedItems(
        IEnumerable<IMetatagTreeItem>? items,
        ObservableCollection<IMetatagTreeItem> cloneInto,
        bool fSort,
        Dictionary<string, bool?>? initialCheckboxState = null,
        Dictionary<string, string?>? initialValues = null)
    {
        IComparer<IMetatagTreeItem?> comparer = Comparer<IMetatagTreeItem?>.Create(
            (left, right) => string.Compare(left?.Name ?? "", right?.Name ?? "", StringComparison.CurrentCultureIgnoreCase));

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

                    if (initialValues != null && initialValues.TryGetValue(innerItem.ID, out string? itemValue))
                        innerItem.Value = itemValue;
                },
                fSort
                    ? innerItem => { innerItem.Children.Sort(_item => _item.Name); }
                    : null);
            
            cloneInto.Add(newItem);
        }

        if (fSort)
            cloneInto.Sort(item => item.Name);
    }

    public static void CloneAndSetCheckedItems(
        IEnumerable<IMetatagTreeItem>? items,
        ObservableCollection<IMetatagTreeItem> cloneInto,
        Dictionary<string, bool?>? initialCheckboxState = null,
        Dictionary<string, string?>? initialValues = null)
    {
        cloneInto.Clear();

        CloneAndAddCheckedItems(items, cloneInto, true /*fSort*/, initialCheckboxState, initialValues);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
