using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Thetacat.Types;

namespace Thetacat.Metatags.Model;

/*----------------------------------------------------------------------------
    %%Class: MetatagSchemaDefinition
    %%Qualified: Thetacat.Model.MetatagSchemaDefinition

    This holds a single metatag schema definition. This allows the schema
    itself to store multiple schema definitions (for the base schema, etc)
----------------------------------------------------------------------------*/
public class MetatagSchemaDefinition
{
    private List<Metatag> m_metatags = new List<Metatag>();

    public IEnumerable<Metatag> Metatags => m_metatags;
    public int SchemaVersion { get; set; } = 0;

    private MetatagTree? m_tree;

    public MetatagTree Tree => m_tree ??= new MetatagTree(Metatags);

    private readonly ConcurrentDictionary<Guid, Metatag> m_metatagLookup = new();

    public Metatag? GetMetatagFromId(Guid id)
    {
        if (m_metatagLookup.TryGetValue(id, out Metatag? metatag))
            return metatag;

        return null;
    }

    public void AddMetatag(Metatag metatag)
    {
        m_metatags.Add(metatag);
        if (!m_metatagLookup.TryAdd(metatag.ID, metatag))
            throw new CatExceptionInternalFailure($"failed to add metatag {metatag} to lookup table. duplicate ID?");
    }

    /*----------------------------------------------------------------------------
        %%Function: RemoveMetatag
        %%Qualified: Thetacat.Metatags.Model.MetatagSchemaDefinition.RemoveMetatag

    ----------------------------------------------------------------------------*/
    public void RemoveMetatag(Guid metatagId)
    {
        Metatag? tag = GetMetatagFromId(metatagId);

        if (tag == null)
            return;

        IMetatagTreeItem? treeItem = Tree.FindMatchingChild(MetatagTreeItemMatcher.CreateIdMatch(metatagId), -1);

        if (treeItem == null)
            throw new CatExceptionInternalFailure("tag was found in list but not in tree?");

        if (treeItem.Children.Count != 0)
            throw new CatExceptionInternalFailure("Caller should have verified children cound was 0 before calling");

        IMetatagTreeItem? parent = Tree.FindParentOfChild(MetatagTreeItemMatcher.CreateIdMatch(metatagId));

        if (parent == null)
            throw new CatExceptionInternalFailure("couldn't find parent of metatag tree item");

        m_metatags.Remove(tag);
        parent.Children.Remove(treeItem);
        m_tree = null;
    }

    public void Clear()
    {
        m_metatags.Clear();
        m_metatagLookup.Clear();
        m_tree = null;
    }

    public MetatagSchemaDefinition Clone()
    {
        MetatagSchemaDefinition clone = new();

        foreach (Metatag metatag in Metatags)
        {
            clone.AddMetatag(metatag.Clone());
        }
        clone.SchemaVersion = SchemaVersion;

        return clone;
    }
}
