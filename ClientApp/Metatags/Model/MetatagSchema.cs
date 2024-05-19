using System;
using System.Collections.Generic;
using System.Windows;
using Thetacat.Logging;
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

        // before we add the metatag, make sure we update the working tree...otherwise if we on-demand
        // build the tree after adding the metatag, the tree will have this new metatag, and adding it
        // to the tree will end up adding a duplicate item.
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

        m_schemaWorking.AddMetatag(metatag);
        TriggerItemDirtied(true);
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
        lock (BuiltinTags.s_BuiltinTags)
        {
            MainWindow.LogForApp(EventType.Warning, "ensure builtin defined");
            GetOrBuildDirectoryTag(null, MetatagStandards.Standard.User, "user root", BuiltinTags.s_UserRootID);
            GetOrBuildDirectoryTag(null, MetatagStandards.Standard.Cat, "cat root", BuiltinTags.s_CatRootID);

            foreach (Metatag metatag in BuiltinTags.s_BuiltinTags)
            {
                if (GetMetatagFromId(metatag.ID) == null)
                {
                    MainWindow.LogForApp(EventType.Warning, $"adding {metatag.Description}: {metatag.ID}");
                    AddMetatag(metatag);
                }
            }

            MainWindow.LogForApp(EventType.Warning, "ensure builtin done");
        }
    }

    public void ReplaceFromService(Guid catalogID)
    {
        ReplaceFromService(ServiceInterop.GetMetatagSchema(catalogID));
    }

    public void ReadNewBaseFromService(ServiceMetatagSchema serviceMetatagSchema)
    {
        m_schemaBase = new MetatagSchemaDefinition(serviceMetatagSchema);

        // and bump the schemaversion in versionworking
        m_schemaWorking.SchemaVersion = m_schemaBase.SchemaVersion + 1;
    }

    /*----------------------------------------------------------------------------
        %%Function: ReplaceFromService
        %%Qualified: Thetacat.Metatags.Model.MetatagSchema.ReplaceFromService

        NOTE: this is replaces in-place AND it ensures builtin tags are defined
        (unlike ReadNewBaseFromService() which doesn't ensure builtin tags)
    ----------------------------------------------------------------------------*/
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

    public static MetatagSchemaDiffOp? DoMetatagThreeWayMerge(
        Metatag? metatagBase,
        Metatag? metatagServer,
        Metatag? metatagLocal)
    {
        if (metatagServer == metatagLocal
            || metatagBase == metatagLocal)
        {
            // we are the same as the latest server, or the same as the base
            // we aren't changing anything (either server or base should win)
            return null;
        }

        // look for insert
        if (metatagBase == null && metatagServer == null && metatagLocal != null)
            return MetatagSchemaDiffOp.CreateInsert(metatagLocal);

        if (metatagBase == metatagServer)
        {
            if (metatagLocal == null)
                return MetatagSchemaDiffOp.CreateDelete(metatagBase!.ID);

            // whatever we are changing from the new server is the diff
            return MetatagSchemaDiffOp.CreateUpdate(metatagServer!, metatagLocal);
        }

        // all 3 changed. do a 3wm on the values
        MetatagSchemaDiffOp op = MetatagSchemaDiffOp.CreateUpdate3WM(metatagBase!, metatagServer!, metatagLocal!);

        // check if this is an update with no values being updated
        if (op is { Action: MetatagSchemaDiffOp.ActionType.Update, IsParentChanged: false } 
            && !op.IsDescriptionChanged 
            && !op.IsNameChanged 
            && !op.IsStandardChanged)
        {
            return null;
        }

        return op;
    }

    public static MetatagSchemaDiff DoThreeWayMergeFromDefinitions(
        MetatagSchemaDefinition schemaBase,
        MetatagSchemaDefinition schemaServer,
        MetatagSchemaDefinition schemaLocal)
    {
        MetatagSchemaDiff diff = new MetatagSchemaDiff(schemaServer.SchemaVersion, schemaServer.SchemaVersion + 1);

        // first, the tags that exist in base
        foreach (Metatag tagBase in schemaBase.Metatags)
        {
            Metatag? tagServer = schemaServer.GetMetatagFromId(tagBase.ID);
            Metatag? tagLocal = schemaLocal.GetMetatagFromId(tagBase.ID);

            MetatagSchemaDiffOp? op = DoMetatagThreeWayMerge(tagBase, tagServer, tagLocal);
            if (op != null)
                diff.AddDiffOp(op);
        }

        // now all the tags in server, but not in base
        foreach (Metatag tagServer in schemaServer.Metatags)
        {
            Metatag? tagBase = schemaBase.GetMetatagFromId(tagServer.ID);
            Metatag? tagLocal = schemaLocal.GetMetatagFromId(tagServer.ID);

            // if base exists, then we've already handled it above
            if (tagBase != null)
                continue;

            MetatagSchemaDiffOp? op = DoMetatagThreeWayMerge(tagBase, tagServer, tagLocal);
            if (op != null)
                diff.AddDiffOp(op);
        }

        // lastly, the local tags not in base and not in server
        // now all the tags in server, but not in base
        foreach (Metatag tagLocal in schemaLocal.Metatags)
        {
            Metatag? tagBase = schemaBase.GetMetatagFromId(tagLocal.ID);
            Metatag? tagServer = schemaServer.GetMetatagFromId(tagLocal.ID);

            // if base or server exists, then we've already handled it above
            if (tagBase != null || tagServer != null)
                continue;

            MetatagSchemaDiffOp? op = DoMetatagThreeWayMerge(tagBase, tagServer, tagLocal);
            if (op != null)
                diff.AddDiffOp(op);
        }

        return diff;
    }

    private MetatagSchemaDiff DoThreeWayMerge(Guid catalogID)
    {
        ServiceMetatagSchema current = ServiceInterop.GetMetatagSchema(catalogID);

        if (m_schemaBase == null || m_schemaBase.Count == 0)
        {
            // had no base, so server becomes base
            ReadNewBaseFromService(current);
            return BuildDiffForSchemas();
        }

        // we have (B)ase, (S)erver, and (L)ocal.
        // if B == S != L, then L wins
        // if B == L != S, then S wins
        // if B != S != L then L wins
        MetatagSchemaDefinition schemaServer = new MetatagSchemaDefinition(current);

        return DoThreeWayMergeFromDefinitions(m_schemaBase, schemaServer, m_schemaWorking);
    }

    public void UpdateServer(Guid catalogID, Func<int, bool>? verify = null)
    {
        int retriesLeft = 3;
        bool fRequeryServerWhenDone = false;

        MetatagSchemaDiff diff = BuildDiffForSchemas();

        while (retriesLeft-- > 0)
        {
            try
            {
                // need to handle 3WM here if we get an exception (because schema changed)
                if (!diff.IsEmpty)
                {
                    if (verify != null && verify(diff.GetDiffCount) == false)
                        return;

                    ServiceInterop.UpdateMetatagSchema(catalogID, diff);
                    m_schemaWorking.SchemaVersion = diff.TargetSchemaVersion;

                    m_schemaBase = null; // working is now the base
                }

                if (fRequeryServerWhenDone)
                {
                    // lastly, requery from the server (this will take care of updating with 3WM results)
                    ReplaceFromService(catalogID);
                }

                TriggerItemDirtied(false);
                return;
            }
            catch (CatExceptionSchemaUpdateFailed)
            {
                MessageBox.Show("Failed to update server metatags. Doing three-way merge");
                diff = DoThreeWayMerge(catalogID);
                fRequeryServerWhenDone = true;
            }
        }

        MessageBox.Show("Failed to update server metatags after 3 retries. Giving up.");
    }

    public void Reset()
    {
        m_schemaBase = null;
        m_schemaWorking.Reset();
        TriggerItemDirtied(false);
    }
}
