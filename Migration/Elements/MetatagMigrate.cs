using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Documents;
using Thetacat.Metatags;

namespace Thetacat.Migration.Elements;

public class MetatagMigrate
{
    private ElementsMetatagTree? m_metatagTree;

    public MetatagMigrate(List<ElementsMetatag> tags)
    {
        m_metatagTree = new ElementsMetatagTree(tags);
    }

    // since we don't have an elements metatag tree, we either have to build one to do this,
    // or we need to make a new class to build on (preferred so its reusable)
    void CollectTagAndParents(Dictionary<string, ElementsMetatag> collected, ElementsMetatag tag)
    {
        if (m_metatagTree == null)
        {
            throw new Exception("not initialized");
        }

        if (collected.ContainsKey(tag.ID))
            return;

        collected.Add(tag.ID, tag);
        CollectTagAndParents(collected, m_metatagTree.GetTagFromId(tag.ParentID));
    }

    /*----------------------------------------------------------------------------
        %%Function: CollectDependentTags
        %%Qualified: Thetacat.Migration.Elements.MetatagMigrate.CollectDependentTags

        A single tag can't be uploaded to thetacat -- all of its dependent parent
        tags have to be included in case they aren't already defined
    ----------------------------------------------------------------------------*/
    public List<ElementsMetatag> CollectDependentTags(MetatagTree tree, List<ElementsMetatag> tags)
    {
        Dictionary<string, ElementsMetatag> collected = new();

        foreach (ElementsMetatag tag in tags)
            CollectTagAndParents(collected, tag);

        return new List<ElementsMetatag>(collected.Values);
    }
}
