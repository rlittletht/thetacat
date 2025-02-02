﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Thetacat.Metatags;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Migration.Elements.Metadata.UI;

/*----------------------------------------------------------------------------
    Thetacat.Migration.Elements.PseMetatagTreeItem
----------------------------------------------------------------------------*/
public class PseMetatagTreeItem : IMetatagTreeItem
{
    private PseMetatag? m_metatag;

    public string ItemId => m_metatag?.ID.ToString() ?? string.Empty;
    public string? ParentId => m_metatag?.ParentID.ToString();
    public ObservableCollection<IMetatagTreeItem> Children { get; } = new();
    public string Description => m_metatag?.Description ?? string.Empty;

    public string Name => m_metatag?.Name ?? string.Empty;
    public string ID => m_metatag?.ID.ToString() ?? string.Empty;
    public bool? Checked { get; set; }
    public string? Value
    {
        get => throw new CatExceptionInternalFailure("NYI in PSE metatags");
        set => throw new CatExceptionInternalFailure("NYI in PSE metatags");
    }
    public PseMetatag Item => m_metatag ?? new PseMetatag();

    public bool IsPlaceholder { get; private set; }

    public void MakeOrphan()
    {
        Debug.Assert(m_metatag != null, nameof(m_metatag) + " != null");
        m_metatag.ParentID = 0;
    }

    public static PseMetatagTreeItem CreateFromMetatag(PseMetatag item)
    {
        PseMetatagTreeItem pseMetatag = new()
                                        {
                                            m_metatag = item
                                        };
        return pseMetatag;
    }

    public static PseMetatagTreeItem CreateParentPlaceholder(int id)
    {
        PseMetatagTreeItem pseMetatag = new()
                                        {
                                            m_metatag = new PseMetatag
                                                        {
                                                            ID = id
                                                        },
                                            IsPlaceholder = true
                                        };

        return pseMetatag;
    }

    public void MaterializePlaceholder(PseMetatag pseMetatag)
    {
        m_metatag = pseMetatag;
        IsPlaceholder = false;
    }

    public void AddChild(PseMetatagTreeItem treeItem)
    {
        Children.Add(treeItem);
    }

    public void SeekAndDelete(HashSet<string> delete) => MetatagTreeItem.SeekAndDelete(this, delete);
    public bool FilterTreeToMatches(MetatagTreeItemMatcher matcher) => MetatagTreeItem.FilterTreeToMatches(this, matcher);
    public IMetatagTreeItem? FindParentOfChild(IMetatagMatcher<IMetatagTreeItem> treeItemMatcher) => MetatagTreeItem.FindParentOfChild(this, treeItemMatcher);

    /*----------------------------------------------------------------------------
        %%Function: FindMatchingChild
        %%Qualified: Thetacat.Metatags.PseMetatagTree.FindMatchingChild

        Find the given named child (in this item or below). we will only
        recurse the given number of levels (-1 means recurse all levels)
    ----------------------------------------------------------------------------*/
    public IMetatagTreeItem? FindMatchingChild(IMetatagMatcher<IMetatagTreeItem> treeItemMatcher, int levelsToRecurse)
    {
        if (treeItemMatcher.IsMatch(this))
            return this;

        if (levelsToRecurse == 0)
            return null;

        if (levelsToRecurse != -1)
            levelsToRecurse--;

        foreach (IMetatagTreeItem item in Children)
        {
            IMetatagTreeItem? matched = item.FindMatchingChild(treeItemMatcher, levelsToRecurse);
            if (matched != null)
                return matched;
        }

        return null;
    }

    public IMetatagTreeItem Clone(CloneTreeItemDelegate cloneDelegatePreChildren, CloneTreeItemDelegate? cloneDelegatePostChildren)
    {
        PseMetatagTreeItem newItem =
            new PseMetatagTreeItem()
            {
                m_metatag = m_metatag,
                IsPlaceholder = IsPlaceholder
            };

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
