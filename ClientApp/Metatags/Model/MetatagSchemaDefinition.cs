using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Thetacat.ServiceClient;
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
    public int Count => m_metatags.Count;

    public int SchemaVersion { get; set; } = 0;

    private MetatagTree? m_tree;

    public MetatagTree Tree => m_tree ??= new MetatagTree(Metatags);

    // a mediatag can be a container tag (in the schema), so we will cache this information
    // so we don't search the schema over and over. this will get reset when the schema is modified

    private Dictionary<Guid, IMetatagTreeItem?>? m_containerCache;

    public Dictionary<Guid, IMetatagTreeItem?> ContainerCache => m_containerCache ??= new Dictionary<Guid, IMetatagTreeItem?>();

    private readonly ConcurrentDictionary<Guid, Metatag> m_metatagLookup = new();

    public MetatagSchemaDefinition(){}

    public MetatagSchemaDefinition(ServiceMetatagSchema serviceMetatagSchema)
    {
        SchemaVersion = serviceMetatagSchema.SchemaVersion ?? 0;

        if (serviceMetatagSchema.Metatags != null)
        {
            foreach (ServiceMetatag serviceMetatag in serviceMetatagSchema.Metatags)
            {
                Metatag metatag = Metatag.CreateFromService(serviceMetatag);
                AddMetatag(metatag);
            }
        }
    }

    public void RebuildTree()
    {
        m_tree = null;
    }

    public Metatag? GetMetatagFromId(Guid id)
    {
        if (m_metatagLookup.TryGetValue(id, out Metatag? metatag))
            return metatag;

        return null;
    }

    public void Reset()
    {
        m_metatags.Clear();
        m_tree = null;
        m_containerCache = null;
        SchemaVersion = 0;
        m_metatagLookup.Clear();
    }

    public void AddMetatag(Metatag metatag)
    {
        // this is a cheap rebuild, so just reset
        m_containerCache = null;

        m_metatags.Add(metatag);
        if (!m_metatagLookup.TryAdd(metatag.ID, metatag))
            throw new CatExceptionInternalFailure($"failed to add metatag {metatag} to lookup table. duplicate ID?");
    }

    /*----------------------------------------------------------------------------
        %%Function: FRemoveMetatag
        %%Qualified: Thetacat.Metatags.Model.MetatagSchemaDefinition.FRemoveMetatag

    ----------------------------------------------------------------------------*/
    public bool FRemoveMetatag(Guid metatagId)
    {
        Metatag? tag = GetMetatagFromId(metatagId);

        if (tag == null)
            return false;

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

        // this is a cheap rebuild, so just reset
        m_containerCache = null;

        return true;
    }

    public void Clear()
    {
        m_metatags.Clear();
        m_metatagLookup.Clear();
        m_containerCache = null;
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
