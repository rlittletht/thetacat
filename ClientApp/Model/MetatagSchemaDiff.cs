using System.Collections.Generic;
using NUnit.Framework;

namespace Thetacat.Model;

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
}
