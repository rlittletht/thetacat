using System;
using System.Collections.Generic;

namespace Thetacat.Migration.Elements;

/*----------------------------------------------------------------------------
    %%Class: MetatagMigrate
    %%Qualified: Thetacat.Migration.Elements.MetatagMigrate
----------------------------------------------------------------------------*/
public class MetatagMigrate
{
    private readonly MetatagTree? m_metatagTree;

    public MetatagMigrate(IEnumerable<Metatag> tags)
    {
        m_metatagTree = new MetatagTree(tags);
    }

    // since we don't have an elements metatag tree, we either have to build one to do this,
    // or we need to make a new class to build on (preferred so its reusable)
    void CollectTagAndParents(Dictionary<string, Metatag> collected, Metatag tag)
    {
        if (m_metatagTree == null)
        {
            throw new Exception("not initialized");
        }

        if (collected.ContainsKey(tag.ID))
            return;

        collected.Add(tag.ID, tag);

        if (string.IsNullOrEmpty(tag.ParentID) || tag.ParentID == "0")
            return;

        CollectTagAndParents(collected, m_metatagTree.GetTagFromId(tag.ParentID));
    }

    /*----------------------------------------------------------------------------
        %%Function: CollectDependentTags
        %%Qualified: Thetacat.Migration.Elements.MetatagMigrate.CollectDependentTags

        A single tag can't be uploaded to thetacat -- all of its dependent parent
        tags have to be included in case they aren't already defined
    ----------------------------------------------------------------------------*/
    public List<Metatag> CollectDependentTags(Metatags.MetatagTree tree, List<Metatag> tags)
    {
        Dictionary<string, Metatag> collected = new();

        foreach (Metatag tag in tags)
        {
            CollectTagAndParents(collected, tag);
        }

        return new List<Metatag>(collected.Values);
    }
}
