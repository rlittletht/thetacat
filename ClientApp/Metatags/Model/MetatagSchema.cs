using System;
using System.Collections.Generic;
using Thetacat.ServiceClient;
using Thetacat.Standards;
using Thetacat.Types;

namespace Thetacat.Metatags.Model;

public class MetatagSchema
{
    public event EventHandler<DirtyItemEventArgs<bool>>? OnItemDirtied;
    private readonly MetatagSchemaDefinition m_schemaWorking = new MetatagSchemaDefinition();
    private MetatagSchemaDefinition? m_schemaBase;

    public MetatagTree WorkingTree => m_schemaWorking.Tree;
    public IEnumerable<Metatag> MetatagsWorking => m_schemaWorking.Metatags;
    public int MetatagCount => m_schemaWorking.Count;

    public int SchemaVersionWorking => m_schemaWorking.SchemaVersion;
    public bool DontBuildTree = false;

    void EnsureBaseAndVersion()
    {
        if (m_schemaBase == null)
        {
            m_schemaBase = m_schemaWorking.Clone();
            m_schemaWorking.SchemaVersion++;
        }
    }

    public Metatag? GetMetatagFromId(Guid id) => m_schemaWorking.GetMetatagFromId(id);

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
            // first find the parent
            IMetatagTreeItem? parentItem = schemaDef.Tree.FindMatchingChild(MetatagTreeItemMatcher.CreateIdMatch(parent.ID.ToString()), -1);

            if (parentItem == null)
                throw new CatExceptionInternalFailure($"couldn't find metatag parent: {parent}");

            IMetatagTreeItem? item = parentItem.FindMatchingChild(MetatagTreeItemMatcher.CreateNameMatch(name), -1);

            return item != null
                ? schemaDef.GetMetatagFromId(Guid.Parse(item.ID))
                : null;
        }

        // otherwise, just return the first matching name
        return FindFirstMatchingItemInSchemaDefinition(schemaDef, MetatagMatcher.CreateNameMatch(name));
    }

    public Metatag? FindByName(Metatag? parent, string name)
    {
        return FindByNameInSchemaDefinition(m_schemaWorking, parent, name);
    }

    private void TriggerItemDirtied(bool fDirty)
    {
        if (OnItemDirtied != null)
            OnItemDirtied(this, new DirtyItemEventArgs<bool>(fDirty));
    }

    public void NotifyChanging()
    {
        EnsureBaseAndVersion();
        TriggerItemDirtied(true);
    }

    public void RebuildWorkingTree()
    {
        m_schemaWorking?.RebuildTree();
    }

    void AddMetatagNoValidation(Metatag metatag)
    {
        if (m_schemaWorking == null)
            throw new Exception("not initialized");

        NotifyChanging();

        m_schemaWorking.AddMetatag(metatag);
        TriggerItemDirtied(true);

        IMetatagTreeItem newItem = MetatagTreeItem.CreateFromMetatag(metatag);

        if (!DontBuildTree)
        {
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
    }

    /*----------------------------------------------------------------------------
        %%Function: AddMetatag
        %%Qualified: Thetacat.Model.MetatagSchema.AddMetatag

        This is the most core AddMetatag. It requires that you have a parent
        set. You CANNOT add a root standard tag with this function
    ----------------------------------------------------------------------------*/
    public void AddMetatag(Metatag metatag)
    {
        if (metatag.Parent == null && metatag.Name.ToLowerInvariant() != metatag.Standard.ToLowerInvariant())
            throw new ArgumentException("must specify parent for metatag");

        AddMetatagNoValidation(metatag);
    }

    public bool FRemoveMetatag(Guid metatagId)
    {
        NotifyChanging();

        return m_schemaWorking.FRemoveMetatag(metatagId);
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

    public IMetatagTreeItem? GetStandardRootItem(MetatagStandards.Standard standard)
    {
        return WorkingTree.FindMatchingChild(
            MetatagTreeItemMatcher.CreateNameMatch(MetatagStandards.GetMetadataRootFromStandard(standard)),
            1);
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

        if (GetMetatagFromId(BuiltinTags.s_WidthID) == null)
            AddMetatag(BuiltinTags.s_Width);
        if (GetMetatagFromId(BuiltinTags.s_HeightID) == null)
            AddMetatag(BuiltinTags.s_Height);
        if (GetMetatagFromId(BuiltinTags.s_OriginalMediaDateID) == null)
            AddMetatag(BuiltinTags.s_OriginalMediaDate);
        if (GetMetatagFromId(BuiltinTags.s_ImportDateID) == null)
            AddMetatag(BuiltinTags.s_ImportDate);
    }

    public void ReplaceFromService(Guid catalogID)
    {
        ReplaceFromService(ServiceInterop.GetMetatagSchema(catalogID));
    }

    public void ReadNewBaseFromService(ServiceMetatagSchema serviceMetatagSchema)
    {
        m_schemaBase =
            new MetatagSchemaDefinition
            {
                SchemaVersion = serviceMetatagSchema.SchemaVersion ?? 0
            };

        if (serviceMetatagSchema.Metatags != null)
        {
            foreach (ServiceMetatag serviceMetatag in serviceMetatagSchema.Metatags)
            {
                Metatag metatag = Metatag.CreateFromService(serviceMetatag);
                m_schemaBase.AddMetatag(metatag);
            }
        }

        // and bump the schemaversion in versionworking
        m_schemaWorking.SchemaVersion = m_schemaBase.SchemaVersion + 1;
    }

    public void ReplaceFromService(ServiceMetatagSchema serviceMetatagSchema)
    {
        m_schemaBase = null;
        m_schemaWorking.Clear();
        
        if (serviceMetatagSchema.Metatags != null)
        {
            foreach (ServiceMetatag serviceMetatag in serviceMetatagSchema.Metatags)
            {
                Metatag metatag = Metatag.CreateFromService(serviceMetatag);
                m_schemaWorking.AddMetatag(metatag);
            }
        }

        EnsureBuiltinMetatagsDefined();

        m_schemaWorking.SchemaVersion = serviceMetatagSchema.SchemaVersion ?? 0;
        TriggerItemDirtied(false);
    }

    public MetatagSchemaDiff BuildDiffForSchemas()
    {
        EnsureBaseAndVersion();
        if (m_schemaBase == null)
            throw new Exception("no schemas");

        return MetatagSchemaDiff.CreateFromSchemas(m_schemaBase, m_schemaWorking);
    }

    public void UpdateServer(Guid catalogID, Func<int, bool>? verify = null)
    {
        // need to handle 3WM here if we get an exception (because schema changed)
        MetatagSchemaDiff diff = BuildDiffForSchemas();
        if (!diff.IsEmpty)
        {
            if (verify != null && verify(diff.GetDiffCount) == false)
                return;

            ServiceInterop.UpdateMetatagSchema(catalogID, diff);
            m_schemaBase = null; // working is now the base
        }
        TriggerItemDirtied(false);
    }

    public void Reset()
    {
        m_schemaBase = null;
        m_schemaWorking.Reset();
        TriggerItemDirtied(false);
    }
}
