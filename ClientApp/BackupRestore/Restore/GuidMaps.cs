using System;
using System.Collections.Generic;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.ServiceClient;
using Thetacat.Types;

namespace Thetacat.BackupRestore.Restore;

public enum IdType
{
    Catalog,
    Metatag,
    Media,
    Stack
}

public class GuidMaps
{
    public Dictionary<IdType, Dictionary<Guid, Guid>> Maps { get; } =
        new()
        {
            { IdType.Catalog, new() },
            { IdType.Metatag, new() },
            { IdType.Media, new() },
            { IdType.Stack, new() }
        };

    public Guid CreateForId(IdType idType, Guid oldId)
    {
        Guid newId = RT.Comb.Provider.Sql.Create();
        Add(idType, oldId, newId);
        return newId;
    }

    public void Add(IdType idType, Guid oldId, Guid newId)
    {
        Maps[idType].Add(oldId, newId);
    }

    public bool TryGetNew(IdType idType, Guid oldId, out Guid newId)
    {
        return Maps[idType].TryGetValue(oldId, out newId);
    }

    public Guid GetOld(IdType idType, Guid newId)
    {
        foreach (KeyValuePair<Guid, Guid> pair in Maps[idType])
        {
            if (pair.Value == newId)
            {
                return pair.Key;
            }
        }

        throw new CatExceptionInternalFailure("can't find old guid given new guid");
    }

    public Guid? GetNew(IdType idType, Guid? oldId)
    {
        if (oldId == null)
            return oldId;

        if (Maps[idType].TryGetValue(oldId.Value, out Guid newId))
        {
            return newId;
        }

        throw new CatExceptionInternalFailure("can't find new guid given old guid");
    }

    /*----------------------------------------------------------------------------
        %%Function: RemapMetatagSchema
        %%Qualified: Thetacat.BackupRestore.Restore.GuidMaps.RemapMetatagSchema
    ----------------------------------------------------------------------------*/
    public MetatagSchema RemapMetatagSchema(MetatagSchema schema)
    {
        // first pass remap all the metatag ids so we know how to map the parents in pass 2
        foreach (Metatag metatag in schema.MetatagsWorking)
        {
            CreateForId(IdType.Metatag, metatag.ID);
        }

        // second pass make a new schema
        MetatagSchema schemaNew = new(false);

        schemaNew.DontBuildTree = true;
        foreach (Metatag metatag in schema.MetatagsWorking)
        {
            Metatag metatagNew = Metatag.Create(
                GetNew(IdType.Metatag, metatag.Parent),
                metatag.Name,
                metatag.Description,
                metatag.Standard,
                GetNew(IdType.Metatag, metatag.ID));

            schemaNew.AddMetatag(metatagNew);
        }

        schemaNew.DontBuildTree = false;
        return schemaNew;
    }

    public List<MediaStack> RemapStacks(MediaStacks stacks)
    {
        List<MediaStack> newStacks = new();

        foreach (MediaStack stack in stacks.Items.Values)
        {
            MediaStack newStack = new MediaStack(stack);
            newStack.StackId = CreateForId(IdType.Stack, stack.StackId);

            newStacks.Add(newStack);
        }

        return newStacks;
    }

    public MediaTag RemapMediaTag(MetatagSchema schema, MediaTag tag)
    {
        Metatag metatag = schema.GetMetatagFromId(GetNew(IdType.Metatag, tag.Metatag.ID)!.Value)!;

        return new MediaTag(metatag, tag.Value, tag.Deleted);
    }

    public MediaItem RemapMediaItem(MetatagSchema schema, MediaItem item)
    {
        ServiceMediaItem newServiceItem =
            new()
            {
                Id = GetNew(IdType.Media, item.ID),
                VirtualPath = item.VirtualPath,
                MimeType = item.MimeType,
                MD5 = item.MD5
            };

        MediaItem newItem = new(newServiceItem);

        newItem.VersionStack = GetNew(IdType.Stack, newItem.VersionStack);
        newItem.MediaStack = GetNew(IdType.Stack, newItem.MediaStack);

        foreach (MediaTag tag in item.MediaTags)
        {
            newItem.FAddOrUpdateMediaTag(RemapMediaTag(schema, tag), true);
        }

        return newItem;
    }

    public ImportsRestore RemapImports(ImportsRestore imports)
    {
        ImportsRestore newImports = new();

        foreach (ServiceImportItem import in imports.ImportItems)
        {
            newImports.ImportItems.Add(new ServiceImportItem()
            {
                ID = GetNew(IdType.Media, import.ID)!.Value,
                State = import.State,
                SourcePath = import.SourcePath,
                SourceServer = import.SourceServer,
                UploadDate = import.UploadDate,
                Source = import.Source
            });
        }

        return newImports;
    }

    public Catalog RemapCatalog(MetatagSchema schema, Catalog catalog)
    {
        Catalog catalogNew = new();

        // remap the stacks
        foreach (MediaStack stack in RemapStacks(catalog.VersionStacks))
        {
            catalogNew.VersionStacks.AddStack(stack);
        }

        foreach (MediaStack stack in RemapStacks(catalog.MediaStacks))
        {
            catalogNew.MediaStacks.AddStack(stack);
        }

        foreach (MediaItem item in catalog.GetMediaCollection())
        {
            catalogNew.AddNewMediaItem(RemapMediaItem(schema, item));
        }
        
        return catalogNew;
    }

    public FullExportRestore RemapFullRestore(FullExportRestore restore)
    {
        MetatagSchema schemaNew = RemapMetatagSchema(restore.SchemaRestore!.Schema);
        Catalog catalogNew = RemapCatalog(schemaNew, restore.CatalogRestore!.Catalog);
        ImportsRestore importsNew = RemapImports(restore.ImportsRestore!);

        return new FullExportRestore(schemaNew, catalogNew, importsNew);
    }
}
