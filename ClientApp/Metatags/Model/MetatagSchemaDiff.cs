﻿using System;
using System.Collections.Generic;

namespace Thetacat.Metatags.Model;

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
    private readonly int m_targetSchemaVersion;

    public int GetDiffCount => m_ops.Count;
    public IEnumerable<MetatagSchemaDiffOp> Ops => m_ops;
    public int BaseSchemaVersion => m_baseSchemaVersion;
    public int TargetSchemaVersion => m_targetSchemaVersion;

    public MetatagSchemaDiff(int baseSchemaVersion, int targetSchemaVersion)
    {
        m_baseSchemaVersion = baseSchemaVersion;
        m_targetSchemaVersion = targetSchemaVersion;
    }

    public void AddDiffOp(MetatagSchemaDiffOp op)
    {
        m_ops.Add(op);
    }

    public void DeleteMetatag(Metatag metatag)
    {
        AddDiffOp(MetatagSchemaDiffOp.CreateDelete(metatag.ID));
    }

    public void InsertMetatag(Metatag metatag)
    {
        AddDiffOp(MetatagSchemaDiffOp.CreateInsert(metatag));
    }

    public void UpdateMetatag(Metatag original, Metatag updated)
    {
        AddDiffOp(MetatagSchemaDiffOp.CreateUpdate(original, updated));
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
        MetatagSchemaDiff diff = new MetatagSchemaDiff(baseSchema.SchemaVersion, working.SchemaVersion);

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

            if (baseTag.Equals(workingTag))
                continue;

            // we have a difference
            diff.UpdateMetatag(baseTag, workingTag);
        }

        return diff;
    }

    public bool IsEmpty => m_ops.Count == 0;
}
