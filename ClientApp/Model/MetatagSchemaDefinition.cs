using System.Collections.Generic;
using Thetacat.Metatags;

namespace Thetacat.Model;

/*----------------------------------------------------------------------------
    %%Class: MetatagSchemaDefinition
    %%Qualified: Thetacat.Model.MetatagSchemaDefinition

    This holds a single metatag schema definition. This allows the schema
    itself to store multiple schema definitions (for the base schema, etc)
----------------------------------------------------------------------------*/
public class MetatagSchemaDefinition
{
    public List<Metatag> Metatags { get; set; } = new List<Metatag>();
    public int SchemaVersion { get; set; } = 0;

    private MetatagTree? m_tree;

    public MetatagTree Tree => m_tree ??= new MetatagTree(Metatags);

    public MetatagSchemaDefinition Clone()
    {
        MetatagSchemaDefinition clone = new();

        clone.Metatags = new List<Metatag>();
        foreach (Metatag metatag in Metatags)
        {
            clone.Metatags.Add(metatag.Clone());
        }
        clone.SchemaVersion = SchemaVersion;

        return clone;
    }
}
