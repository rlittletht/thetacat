using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Animation;
using System.Xml;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.Model.Workgroups;
using Thetacat.ServiceClient;
using Thetacat.Types;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public enum IdType
{
    Catalog,
    Metatag,
    Media,
    Stack,
    WorkgroupClient,
    Filter
}

public class GuidMaps
{
    public Dictionary<IdType, Dictionary<Guid, Guid>> Maps { get; } =
        new()
        {
            { IdType.Catalog, new() },
            { IdType.Metatag, new() },
            { IdType.Media, new() },
            { IdType.Stack, new() },
            { IdType.WorkgroupClient, new() },
            { IdType.Filter, new() }
        };

    public static string GetStringForIdType(IdType idType)
    {
        return idType switch
        {
            IdType.Catalog => "catalog",
            IdType.Metatag => "metatag",
            IdType.Media => "media",
            IdType.Stack => "stack",
            IdType.WorkgroupClient => "workgroup-client",
            IdType.Filter => "filter",
            _ => throw new ArgumentOutOfRangeException(nameof(idType), idType, null)
        };
    }

    public static IdType IdTypeFromString(string idTypeString)
    {
        return idTypeString switch
        {
            "catalog" => IdType.Catalog,
            "metatag" => IdType.Metatag,
            "media" => IdType.Media,
            "stack" => IdType.Stack,
            "workgroup-client" => IdType.WorkgroupClient,
            "filter" => IdType.Filter,
            _ => throw new ArgumentOutOfRangeException(nameof(idTypeString), idTypeString, null)
        };
    }

    public Guid CreateForId(IdType idType, Guid oldId)
    {
        if (idType == IdType.Metatag)
        {
            Guid? builtin = BuiltinTags.MapDeprecatedIdToCurrentId(oldId);

            if (builtin != null)
            {
                Add(idType, oldId, builtin.Value);
                return builtin.Value;
            }
        }

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

    public Guid? GetNew(IdType idType, Guid? oldId, bool missingIsOk = false)
    {
        if (oldId == null)
            return oldId;

        if (Maps[idType].TryGetValue(oldId.Value, out Guid newId))
        {
            return newId;
        }

        if (missingIsOk)
            return null;

        throw new CatExceptionInternalFailure("can't find new guid given old guid");
    }

    public string RemapStringWithMetatagGuids(string oldString)
    {
        Regex regex = new(@"\{([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\}");
        string newString = oldString;
        MatchCollection matches = regex.Matches(newString);

        for (int i = 0; i < matches.Count; i++)
        {
            if (Guid.TryParse(matches[i].Value, out Guid oldGuid))
            {
                Guid newGuid = GetNew(IdType.Metatag, oldGuid)!.Value;
                newString = newString.Replace(matches[i].Value, $"{{{newGuid}}}");
            }
            else
            {
                throw new CatExceptionInternalFailure("can't parse guid in string");
            }
        }

        return newString;
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

    public void RemapStackItemsInPlace(MediaStacks stacks)
    {
        List<MediaStack> newStacks = new();

        foreach (MediaStack stack in stacks.Items.Values)
        {
            foreach (MediaStackItem item in stack.Items)
            {
                item.MediaId = GetNew(IdType.Media, item.MediaId)!.Value;
            }
        }
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
                Id = CreateForId(IdType.Media, item.ID),
                VirtualPath = item.VirtualPath,
                MimeType = item.MimeType,
                MD5 = item.MD5,
                State = MediaItem.StringFromState(item.State)
            };

        MediaItem newItem = new(newServiceItem, false/*useDeprecatedTagIds*/);

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
            newImports.ImportItems.Add(
                new ServiceImportItem()
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

        // lastly, now that we have the media item id maps, map all the stack items
        RemapStackItemsInPlace(catalogNew.MediaStacks);
        RemapStackItemsInPlace(catalogNew.VersionStacks);

        return catalogNew;
    }

    public WorkgroupDataRestore RemapWorkgroupDataRestore(WorkgroupDataRestore restore)
    {
        WorkgroupDataRestore restoreNew = new();

        restoreNew.WorkgroupClock = restore.WorkgroupClock;

        // we don't make the workgroup id becuase the restore will be happening into an already created
        // workgroup, so the mapping will implicitly happen
        restoreNew.WorkgroupId = restore.WorkgroupId;
        restoreNew.WorkgroupName = restore.WorkgroupName;

        foreach (ServiceWorkgroupClient client in restore.Clients)
        {
            ServiceWorkgroupClient clientNew = new()
                                               {
                                                   ClientId = CreateForId(IdType.WorkgroupClient, client.ClientId!.Value),
                                                   ClientName = client.ClientName,
                                                   DeletedMediaClock = client.DeletedMediaClock,
                                                   VectorClock = client.VectorClock
                                               };

            restoreNew.Clients.Add(clientNew);
        }

        foreach (WorkgroupCacheEntryData cacheEntry in restore.MediaItems)
        {
            Guid? newId = GetNew(IdType.Media, cacheEntry.ID, true /*missingIsOk*/);

            if (newId == null)
            {
                MessageBox.Show($"Could not find media mapping for media ID {cacheEntry.ID} for virtual path {cacheEntry.Path}");
            }
            else
            {
                WorkgroupCacheEntryData cacheEntryNew =
                    new()
                    {
                        ID = GetNew(IdType.Media, cacheEntry.ID)!.Value,
                        MD5 = cacheEntry.MD5,
                        Path = cacheEntry.Path,
                        CachedBy = GetNew(IdType.WorkgroupClient, cacheEntry.CachedBy)!.Value,
                        CacheDate = cacheEntry.CacheDate,
                        VectorClock = cacheEntry.VectorClock
                    };

                restoreNew.MediaItems.Add(cacheEntryNew);
            }
        }

        foreach (WorkgroupFilterData filter in restore.Filters)
        {
            WorkgroupFilterData filterNew = new()
                                            {
                                                Id = CreateForId(IdType.Filter, filter.Id),
                                                Name = filter.Name,
                                                Description = filter.Description,
                                                Expression = RemapStringWithMetatagGuids(filter.Expression)
                                            };

            restoreNew.Filters.Add(filterNew);
        }

        return restoreNew;
    }

    public DeletedMediaRestore? RemapDeletedMedia(DeletedMediaRestore? deletedMedia)
    {
        if (deletedMedia == null)
            return null;

        DeletedMediaRestore deletedMediaNew = new();

        deletedMediaNew.WorkgroupDeletedMediaClock = deletedMedia.WorkgroupDeletedMediaClock;

        foreach (ServiceDeletedItem item in deletedMedia.DeletedItems)
        {
            Guid? newId = GetNew(IdType.Media, item.Id, true);

            // if the mapping fails, that's ok. that means its been deleted from the catalog
            // already and just waiting for local workgroups to delete. they will use the old
            // id.
            if (newId == null)
            {
                newId = item.Id;
            }

            ServiceDeletedItem newItem = new()
                                         {
                                             Id = newId,
                                             MinVectorClock = item.MinVectorClock
                                         };

            deletedMediaNew.DeletedItems.Add(newItem);
        }

        return deletedMediaNew;
    }

    public FullExportRestore RemapFullRestore(FullExportRestore restore)
    {
        MetatagSchema schemaNew = RemapMetatagSchema(restore.SchemaRestore!.Schema);
        Catalog catalogNew = RemapCatalog(schemaNew, restore.CatalogRestore!.Catalog);
        ImportsRestore importsNew = RemapImports(restore.ImportsRestore!);
        DeletedMediaRestore? deletedMediaNew = RemapDeletedMedia(restore.DeletedMediaRestore);
        WorkgroupDataRestore workgroupDataNew = RemapWorkgroupDataRestore(restore.WorkgroupDataRestore!);

        return new FullExportRestore(schemaNew, catalogNew, importsNew, deletedMediaNew, workgroupDataNew);
    }

    public delegate void WriteChildrenDelegate(XmlWriter writer);
    public static string s_uri = "https://schemas.thetasoft.com/thetacat/backup/2024/guidMap";

    public void WriteElement(XmlWriter writer, string element, WriteChildrenDelegate? children)
    {
        if (children != null)
        {
            writer.WriteStartElement(element, s_uri);
            children(writer);
            writer.WriteEndElement();
        }
        else
        {
            writer.WriteElementString(element, s_uri);
        }
    }

    public void SaveToFile(string exportFile)
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Async = true;

        using XmlWriter writer = XmlWriter.Create(exportFile, settings);

        WriteElement(
            writer,
            "guidMaps",
            _writer =>
            {
                foreach (KeyValuePair<IdType, Dictionary<Guid, Guid>> map in Maps)
                {
                    WriteElement(
                        _writer,
                        "map",
                        __writer =>
                        {
                            __writer.WriteAttributeString("type", GetStringForIdType(map.Key));
                            foreach (KeyValuePair<Guid, Guid> pair in map.Value)
                            {
                                WriteElement(
                                    __writer,
                                    "mapping",
                                    ___writer =>
                                    {
                                        ___writer.WriteAttributeString("old", pair.Key.ToString());
                                        ___writer.WriteAttributeString("new", pair.Value.ToString());
                                    });
                            }
                        });
                }
            });
    }

    public static GuidMaps CreateFromFile(string importFile)
    {
        GuidMaps maps = new();

        using Stream stm = File.Open(importFile, FileMode.Open);
        using XmlReader reader = XmlReader.Create(stm);

        if (!XmlIO.Read(reader))
            return maps;

        XmlIO.SkipNonContent(reader);

        XmlIO.FReadElement(reader, maps, "guidMaps", null, FParseGuidMapsElement);
        return maps;
    }

    class GuidMapImport
    {
        public IdType idType;
        public Dictionary<Guid, Guid> map = new();
        public Guid buildingOld = Guid.Empty;
        public Guid buildingNew = Guid.Empty;
    }

    static bool FParseGuidMapsElement(XmlReader reader, string element, GuidMaps maps)
    {
        if (element != "map")
            throw new XmlioExceptionSchemaFailure($"unknown element {element}");

        GuidMapImport map = new();

        if (XmlIO.FReadElement(reader, map, "map", FParseGuidMapAttribute, FParseGuidMapElement))
        {
            maps.Maps[map.idType] = map.map;
            return true;
        }

        return false;
    }

    static bool FParseGuidMapAttribute(string attribnute, string value, GuidMapImport map)
    {
        if (attribnute == "type")
        {
            map.idType = IdTypeFromString(value);
            return true;
        }

        return false;
    }

    static bool FParseGuidMapElement(XmlReader reader, string element, GuidMapImport map)
    {
        if (element != "mapping")
            throw new XmlioExceptionSchemaFailure($"unknown element {element}");

        if (!XmlIO.FReadElement(reader, map, "mapping", FParseGuidMappingAttribute, null))
            return false;

        map.map.Add(map.buildingOld, map.buildingNew);
        return true;
    }

    static bool FParseGuidMappingAttribute(string attribute, string value, GuidMapImport map)
    {
        if (attribute == "old")
        {
            map.buildingOld = Guid.Parse(value);
            return true;
        }

        if (attribute == "new")
        {
            map.buildingNew = Guid.Parse(value);
            return true;
        }

        return false;
    }
}
