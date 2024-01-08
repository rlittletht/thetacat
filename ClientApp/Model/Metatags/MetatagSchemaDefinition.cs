using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Thetacat.Metatags;
using Thetacat.Types;

namespace Thetacat.Model.Metatags;

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

    public void Clear()
    {
        m_metatags.Clear();
        m_metatagLookup.Clear();
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
