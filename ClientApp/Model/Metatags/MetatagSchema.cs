﻿using System;
using System.Collections.Generic;
using System.Windows.Markup;
using Emgu.CV.Features2D;
using MetadataExtractor;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Thetacat.Metatags;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Standards;
using Thetacat.Types;

namespace Thetacat.Model.Metatags;

public class MetatagSchema
{

    private MetatagSchemaDefinition m_schemaWorking = new MetatagSchemaDefinition();
    private MetatagSchemaDefinition? m_schemaBase;

    public MetatagTree WorkingTree => m_schemaWorking.Tree;
    public List<Metatag> MetatagsWorking => m_schemaWorking.Metatags;
    public int SchemaVersionWorking => m_schemaWorking.SchemaVersion;

    void EnsureBaseAndVersion()
    {
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

    Metatag CreateMetatagForStandardRoot(MetatagStandards.Standard standard)
    {
        if (standard == MetatagStandards.Standard.User)
        {
            return MetatagBuilder
               .Create()
               .SetName("user")
               .SetDescription($"user root")
               .SetStandard(standard)
               .Build();
        }

        StandardDefinitions definitions = MetatagStandards.GetStandardMappings(standard);
        string name = MetatagStandards.GetMetadataRootFromStandardTag(definitions.StandardTag);

        return MetatagBuilder
           .Create()
           .SetName(name)
           .SetDescription($"{name} root")
           .SetStandard(standard)
           .Build();
    }

    /*----------------------------------------------------------------------------
        %%Function: AddNewStandardRoot
        %%Qualified: Thetacat.Model.MetatagSchema.AddNewStandardRoot

        This will create a new standard root metatag and add it (and return it)
    ----------------------------------------------------------------------------*/
    public Metatag AddNewStandardRoot(MetatagStandards.Standard standard)
    {
        Metatag metatag = CreateMetatagForStandardRoot(standard);

        AddMetatagNoValidation(metatag);

        return metatag;
    }

    public Metatag? FindStandardItem(MetatagStandards.Standard standard, StandardDefinition item)
    {
        // get the root from the standard
        IMetatagTreeItem? root =
            WorkingTree.FindMatchingChild(
                MetatagTreeItemMatcher.CreateNameMatch(MetatagStandards.GetMetadataRootFromStandard(standard)),
                1);

        if (root == null)
            return null;

        IMetatagTreeItem? match =
            root.FindMatchingChild(
                MetatagTreeItemMatcher.CreateNameMatch(item.PropertyTagName),
                -1);

        if (match == null)
            return null;

        return FindFirstMatchingItem(MetatagMatcher.CreateIdMatch(match.ID));
    }

    public Metatag? FindStandardItemFromStandardAndType(MetatagStandards.Standard standard, int type)
    {
        StandardDefinition? def = MetatagStandards.GetDefinitionForStandardAndType(standard, type);

        if (def == null) 
            return null;

        return FindStandardItem(standard, def);
    }

    /*----------------------------------------------------------------------------
        %%Function: GetOrBuildDirectoryTag
        %%Qualified: Thetacat.Model.Metatags.MetatagSchema.GetOrBuildDirectoryTag

        Get a directory tag (a tag that is needed as a parent for a tag), or 
        create it if it doesn't exist.

        If this tag must use a predefined static id, pass that in (this is only 
        true for builtin tags like width/height/originalFileDate)
    ----------------------------------------------------------------------------*/
    public Metatag GetOrBuildDirectoryTag(
        Metatag? parent,
        MetatagStandards.Standard standard,
        string description,
        Guid? idStatic = null)
    {
        StandardDefinitions standardDefinitions = MetatagStandards.GetStandardMappings(standard);

        // match the current directory to a metatag
        Metatag? dirTag = FindByName(parent, standardDefinitions.StandardTag);

        if (dirTag == null)
        {
            // we have to create one
            dirTag = Metatag.Create(parent?.ID, standardDefinitions.StandardTag, description, standard, idStatic);
            
            if (parent == null)
                AddStandardRoot(dirTag);
            else
                AddMetatag(dirTag);
        }

        return dirTag;
    }

    public void EnsureBuiltinMetatagsDefined()
    {
        GetOrBuildDirectoryTag(null, MetatagStandards.Standard.Cat, "cat root", BuiltinTags.s_CatRootID);

        if (FindFirstMatchingItem(MetatagMatcher.CreateIdMatch(BuiltinTags.s_WidthID)) == null)
            AddMetatag(BuiltinTags.s_Width);
        if (FindFirstMatchingItem(MetatagMatcher.CreateIdMatch(BuiltinTags.s_HeightID)) == null)
            AddMetatag(BuiltinTags.s_Height);
        if (FindFirstMatchingItem(MetatagMatcher.CreateIdMatch(BuiltinTags.s_OriginalFileDateID)) == null)
            AddMetatag(BuiltinTags.s_OriginalFileDate);
    }

    public void ReplaceFromService(ServiceMetatagSchema serviceMetatagSchema)
    {
        m_schemaBase = null;
        m_schemaWorking.Metatags.Clear();

        if (serviceMetatagSchema.Metatags != null)
        {
            foreach (ServiceMetatag serviceMetatag in serviceMetatagSchema.Metatags)
            {
                m_schemaWorking.Metatags.Add(Metatag.CreateFromService(serviceMetatag));
            }
        }

        m_schemaWorking.SchemaVersion = serviceMetatagSchema.SchemaVersion ?? 0;
    }

    public MetatagSchemaDiff BuildDiffForSchemas()
    {
        EnsureBaseAndVersion();
        if (m_schemaBase == null)
            throw new Exception("no schemas");

        return MetatagSchemaDiff.CreateFromSchemas(m_schemaBase, m_schemaWorking);
    }

    public void UpdateServer()
    {
        // need to handle 3WM here if we get an exception (because schema changed)
        MetatagSchemaDiff diff = BuildDiffForSchemas();
        if (!diff.IsEmpty)
        {
            ServiceInterop.UpdateMetatagSchema(diff);
            m_schemaBase = null; // working is now the base
        }
    }
}
