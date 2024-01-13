using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MetadataExtractor;
using Microsoft.Identity.Client;
using Thetacat.Metatags.Model;
using Thetacat.Migration.Elements.Metadata.UI;

namespace Thetacat.Migration.Elements;

/*----------------------------------------------------------------------------
    %%Class: MetatagMigrate
    %%Qualified: Thetacat.Migration.Elements.MetatagMigrate
----------------------------------------------------------------------------*/
public class MetatagMigrate
{
    private PseMetatagTree? m_metatagTree;
    private PseMetadataSchema? m_metadataSchema = null;
    private ObservableCollection<PseMetatag>? m_metatags;
    private ObservableCollection<MetatagSchemaDiff>? m_schemaDiff;

    public PseMetadataSchema MetadataSchema
    {
        get
        {
            if (m_metadataSchema == null)
                throw new Exception("not initialized");
            return m_metadataSchema;
        }
    }

    public ObservableCollection<PseMetatag> UserMetatags
    {
        get
        {
            if (m_metatags == null)
                throw new Exception("not intialized");

            return m_metatags;
        }
    }

    readonly Dictionary<int, PseMetatag> m_metatagDictionary = new();

    public PseMetatagTree PseTree
    {
        get
        {
            if (m_metatagTree == null)
                throw new Exception("not initialized");

            return m_metatagTree;
        }
    }

    public MetatagMigrate()
    {
    }

    public void SetUserMetatags(IEnumerable<PseMetatag> metatags)
    {
        m_metatags = new ObservableCollection<PseMetatag>();

        foreach (PseMetatag metatag in metatags)
        {
            m_metatags.Add(metatag);
            m_metatagDictionary.Add(metatag.ID, metatag);
        }

        m_metatagTree = new PseMetatagTree(m_metatags);
    }

    public PseMetatag GetMetatagFromID(int ID)
    {
        return m_metatagDictionary[ID];
    }

    public PseMetatag? TryGetMetatagFromID(int ID)
    {
        if (m_metatagDictionary.TryGetValue(ID, out PseMetatag? id))
            return id;

        return null;
    }

    public void SetMetadataSchema(PseMetadataSchema schema)
    {
        m_metadataSchema = schema;
    }

    // since we don't have an elements metatag tree, we either have to build one to do this,
    // or we need to make a new class to build on (preferred so its reusable)
    void CollectTagAndParents(Dictionary<int, PseMetatag> collected, PseMetatag tag)
    {
        if (m_metatagTree == null)
        {
            throw new Exception("not initialized");
        }

        if (collected.ContainsKey(tag.ID))
            return;

        collected.Add(tag.ID, tag);

        if (tag.ParentID == 0)
            return;

        CollectTagAndParents(collected, m_metatagTree.GetTagFromId(tag.ParentID));
    }

    /*----------------------------------------------------------------------------
        %%Function: CollectDependentTags
        %%Qualified: Thetacat.Migration.Elements.MetatagMigrate.CollectDependentTags

        A single tag can't be uploaded to thetacat -- all of its dependent parent
        tags have to be included in case they aren't already defined
    ----------------------------------------------------------------------------*/
    public List<PseMetatag> CollectDependentTags(Metatags.MetatagTree tree, List<PseMetatag> tags)
    {
        Dictionary<int, PseMetatag> collected = new();

        foreach (PseMetatag tag in tags)
        {
            CollectTagAndParents(collected, tag);
        }

        return new List<PseMetatag>(collected.Values);
    }

    /*----------------------------------------------------------------------------
        %%Function: BuildSchemaDiff
        %%Qualified: Thetacat.Migration.Elements.Metadata.MetatagMigrate.BuildSchemaDiff

        There are two parts to the migrate

        USER:
        First, build the set of tags we have to migrate based on the items added
        to the UserMetatagMigration

        STANDARDS:
        Second, collect all the checked items from the StandardMetadata tab.
    ----------------------------------------------------------------------------*/
    public void BuildSchemaDiff()
    {
        m_schemaDiff = new ObservableCollection<MetatagSchemaDiff>();

    }
}
