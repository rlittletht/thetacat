using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Thetacat.Model.Metatags;

/*----------------------------------------------------------------------------
    %%Class: MetatagSchemaDiff
    %%Qualified: Thetacat.Model.MetatagSchemaDiff

    Represents a change to a schema -- insertions, updates, and deletions

    this could be generated from a 3-way merge, or it could be manually
    created in the case of migration, user edits, etc.
----------------------------------------------------------------------------*/
public class MetatagSchemaDiff
{
    private readonly List<MetatagSchemaDiffOp> m_ops = new();
    private readonly int m_baseSchemaVersion;

    public IEnumerable<MetatagSchemaDiffOp> Ops => m_ops;
    public int BaseSchemaVersion => m_baseSchemaVersion;

    public MetatagSchemaDiff(int baseSchemaVersion)
    {
        m_baseSchemaVersion = baseSchemaVersion;
    }

    public void DeleteMetatag(Metatag metatag)
    {
        m_ops.Add(MetatagSchemaDiffOp.CreateDelete(metatag.ID));
    }

    public void InsertMetatag(Metatag metatag)
    {
        m_ops.Add(MetatagSchemaDiffOp.CreateInsert(metatag));
    }

    public void UpdateMetatag(Metatag original, Metatag updated)
    {
        m_ops.Add(MetatagSchemaDiffOp.CreateUpdate(original, updated));
    }

    public static Dictionary<Guid, Metatag> BuildMetatagDictionary(IEnumerable<Metatag> metatags)
    {
        Dictionary<Guid, Metatag> dictionary = new();

        foreach (Metatag metatag in metatags)
        {
            dictionary.Add(metatag.ID, metatag);
        }

        return dictionary;
    }

    public static MetatagSchemaDiff CreateFromSchemas(MetatagSchemaDefinition baseSchema, MetatagSchemaDefinition working)
    {
        MetatagSchemaDiff diff = new MetatagSchemaDiff(baseSchema.SchemaVersion);

        // build dictionaries for both for faster access
        Dictionary<Guid, Metatag> baseDictionary = BuildMetatagDictionary(baseSchema.Metatags);
        Dictionary<Guid, Metatag> workingDictionary = BuildMetatagDictionary(working.Metatags);

        // find all deleted items (tags in base not in working)
        foreach (Guid key in baseDictionary.Keys)
        {
            if (!workingDictionary.ContainsKey(key))
                diff.DeleteMetatag(baseDictionary[key]);
        }

        // find all inserted items (tags in working not in base)
        foreach (Guid key in workingDictionary.Keys)
        {
            if (!baseDictionary.ContainsKey(key))
                diff.InsertMetatag(workingDictionary[key]);
        }

        // lastly, find all changed items
        foreach (Guid key in baseDictionary.Keys)
        {
            if (!workingDictionary.ContainsKey(key))
                continue;

            Metatag baseTag = baseDictionary[key];
            Metatag workingTag = workingDictionary[key];

            if (baseTag == workingTag)
                continue;

            // we have a difference
            diff.UpdateMetatag(baseTag, workingTag);
        }

        return diff;
    }
}
