using System;
using System.Collections.Generic;
using System.Windows.Markup;
using Emgu.CV.Features2D;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Thetacat.Metatags;
using Thetacat.ServiceClient;
using Thetacat.Standards;
using Thetacat.Types;

namespace Thetacat.Model;

public class MetatagSchema
{
    private MetatagSchemaDefinition? m_schemaWorking;
    private MetatagSchemaDefinition? m_schemaBase;

    public MetatagTree WorkingTree
    {
        get
        {
            if (m_schemaWorking == null)
                throw new Exception("not initialized");

            return m_schemaWorking.Tree;
        }
    }

    public List<Metatag> MetatagsWorking
    {
        get
        {
            if (m_schemaWorking == null)
                throw new Exception("not initialized");

            return m_schemaWorking.Metatags;
        }
    }

    public int SchemaVersionWorking
    {
        get
        {
            if (m_schemaWorking == null)
                throw new Exception("not initialized");

            return m_schemaWorking.SchemaVersion;
        }
    }

    void EnsureBaseAndVersion()
    {
        if (m_schemaWorking == null)
        {
            throw new Exception("not initialized");
        }

        if (m_schemaBase == null)
        {
            m_schemaBase = m_schemaWorking.Clone();
            m_schemaWorking.SchemaVersion++;
        }
    }

    static Metatag? FindFirstMatchingItemInSchemaDefinition(MetatagSchemaDefinition schemaDef, IMetatagMatcher<IMetatag> matcher)
    {
        foreach (Metatag metatag in schemaDef.Metatags)
        {
            if (matcher.IsMatch(metatag))
                return metatag;
        }

        return null;
    }

    /*----------------------------------------------------------------------------
        %%Function: FindId
        %%Qualified: Thetacat.Model.MetatagSchema.FindId

        Find the given metatag by its id
    ----------------------------------------------------------------------------*/
    public Metatag? FindFirstMatchingItem(IMetatagMatcher<IMetatag> matcher)
    {
        if (m_schemaWorking == null)
            return null;

        return FindFirstMatchingItemInSchemaDefinition(m_schemaWorking, matcher);
    }

    /*----------------------------------------------------------------------------
        %%Function: FindByName
        %%Qualified: Thetacat.Model.MetatagSchema.FindByName

        Find the first metatag that matches the given name. Search only under
        parent (if given)
    ----------------------------------------------------------------------------*/
    public static Metatag? FindByNameInSchemaDefinition(MetatagSchemaDefinition schemaDef, Metatag? parent, string name)
    {
        if (parent != null)
        {
            IMetatagTreeItem? item = schemaDef.Tree.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(name), -1);

            return item != null ? FindFirstMatchingItemInSchemaDefinition(schemaDef, MetatagMatcher.CreateIdMatch(item.ID)) : null;
        }

        // otherwise, just return the first matching name
        return FindFirstMatchingItemInSchemaDefinition(schemaDef, MetatagMatcher.CreateNameMatch(name));
    }

    public Metatag? FindByName(Metatag? parent, string name)
    {
        if (m_schemaWorking == null)
            return null;

        return FindByNameInSchemaDefinition(m_schemaWorking, parent, name);
    }

    void AddMetatagNoValidation(Metatag metatag)
    {
        if (m_schemaWorking == null)
            throw new Exception("not initialized");

        EnsureBaseAndVersion();

        m_schemaWorking.Metatags.Add(metatag);

        IMetatagTreeItem newItem = MetatagTreeItem.CreateFromMetatag(metatag);

        if (metatag.Parent == null)
        {
            m_schemaWorking.Tree.Children.Add(newItem);
        }
        else
        {
            IMetatagTreeItem? parent = m_schemaWorking.Tree.FindMatchingChild(MetatagTreeItemMatcher.CreateIdMatch(metatag.Parent.Value.ToString()), -1);

            if (parent == null)
                throw new Exception($"must provide an existing parent ID when adding a metatag: ${metatag}");

            parent.Children.Add(newItem);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: AddMetatag
        %%Qualified: Thetacat.Model.MetatagSchema.AddMetatag

        This is the most core AddMetatag. It requires that you have a parent
        set. You CANNOT add a root standard tag with this function
    ----------------------------------------------------------------------------*/
    public void AddMetatag(Metatag metatag)
    {
        if (metatag.Parent == null)
            throw new ArgumentException("must specify parent for metatag");

        AddMetatagNoValidation(metatag);
    }

    /*----------------------------------------------------------------------------
        %%Function: AddStandardRoot
        %%Qualified: Thetacat.Model.MetatagSchema.AddStandardRoot

        This is really the same as AddMetatag, but it allows null for the parent

    ----------------------------------------------------------------------------*/
    public void AddStandardRoot(Metatag metatag)
    {
        AddMetatagNoValidation(metatag);
    }

    /*----------------------------------------------------------------------------
        %%Function: AddNewStandardRoot
        %%Qualified: Thetacat.Model.MetatagSchema.AddNewStandardRoot

        This will create a new standard root metatag and add it (and return it)
    ----------------------------------------------------------------------------*/
    public Metatag AddNewStandardRoot(MetatagStandards.Standard standard)
    {
        StandardDefinitions definitions = MetatagStandards.GetStandardMappings(standard);

        Metatag metatag = MetatagBuilder
           .Create()
           .SetName(definitions.Tag)
           .SetDescription($"{definitions.Tag} root")
           .Build();

        AddMetatagNoValidation(metatag);

        return metatag;
    }

    public static MetatagSchema CreateFromService(ServiceMetatagSchema serviceMetatagSchema)
    {
        MetatagSchema schema =
            new()
            {
                m_schemaWorking = new MetatagSchemaDefinition()
            };

        if (serviceMetatagSchema.Metatags != null)
        {
            foreach (ServiceMetatag serviceMetatag in serviceMetatagSchema.Metatags)
            {
                schema.m_schemaWorking.Metatags.Add(Metatag.CreateFromService(serviceMetatag));
            }
        }

        schema.m_schemaWorking.SchemaVersion = serviceMetatagSchema.SchemaVersion ?? 0;
        return schema;
    }
}
