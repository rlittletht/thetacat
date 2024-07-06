using System;
using System.Collections.Generic;
using Thetacat.Import;
using Thetacat.Metatags.Model;
using Thetacat.Model;

namespace Thetacat.ServiceClient;

public class ServiceInterop
{
    public static List<ServiceCatalogDefinition> GetCatalogDefinitions() => LocalService.CatalogDefinitions.GetCatalogDefinitions();
    public static void AddCatalogDefinition(ServiceCatalogDefinition catalogDefinition) => LocalService.CatalogDefinitions.AddCatalogDefinition(catalogDefinition);

    public static ServiceMetatagSchema GetMetatagSchema(Guid catalogID)
    {
        return LocalService.Metatags.GetMetatagSchema(catalogID);
    }

    public static void UpdateMetatagSchema(Guid catalogID, MetatagSchemaDiff schemaDiff)
    {
        LocalService.Metatags.UpdateMetatagSchema(catalogID, schemaDiff);
    }

    public static void ResetMetatagSchema(Guid catalogID) => LocalService.Metatags.ResetMetatagSchema(catalogID);

    public static void UpdateMediaItems(Guid catalogID, IEnumerable<MediaItemDiff> diffs)
    {
        LocalService.Media.UpdateMediaItems(catalogID, diffs);
    }

    public static List<Guid> GetDeletedMediaItems(Guid catalogId) => LocalService.Media.GetDeletedMediaItems(catalogId);

    public static void DeleteAllMediaAndMediaTagsAndStacks(Guid catalogID) => LocalService.Media.DeleteAllMediaAndMediaTagsAndStacks(catalogID);
    public static void DeleteAllStacksAssociatedWithMedia(Guid catalogID) => LocalService.Stacks.DeleteAllStacksAssociatedWithMedia(catalogID);

    public static void DeleteMediaItem(Guid catalogID, Guid id) => LocalService.Media.DeleteMediaItem(catalogID, id);

    public static void DeleteImportsForMediaItem(Guid catalogId, Guid id) => LocalService.Import.DeleteMediaItem(catalogId, id);

    public static List<ServiceImportItem> GetImportsForClient(Guid catalogID, string sourceClient)
    {
        return LocalService.Import.GetImportsForClient(catalogID, sourceClient);
    }

    public static List<ServiceImportItem> GetAllImports(Guid catalogID)
    {
        return LocalService.Import.GetAllImports(catalogID);
    }

    public static void InsertAllServiceImportItems(Guid catalogID, IEnumerable<ServiceImportItem> items) => LocalService.Import.InsertServiceImportItems(catalogID, items);

    public static void InsertImportItems(Guid catalogID, IEnumerable<ImportItem> items)
    {
        LocalService.Import.InsertImportItems(catalogID, items);
    }

    public static List<ServiceImportItem> QueryImportedItems(Guid catalogID, IEnumerable<Guid> ids)
    {
        return LocalService.Import.QueryImportedItems(catalogID, ids);
    }

    public static void CompleteImportForItem(Guid catalogID, Guid id)
    {
        LocalService.Import.CompleteImportForItem(catalogID, id);
    }

    public static void DeleteImportItem(Guid catalogID, Guid id)
    {
        LocalService.Import.DeleteImportItem(catalogID, id);
    }

    public static void InsertNewMediaItems(Guid catalogID, IEnumerable<MediaItem> newItems)
    {
        LocalService.Media.InsertNewMediaItems(catalogID, newItems);
    }

    public static ServiceCatalog ReadFullCatalog(Guid catalogID)
    {
        return LocalService.Media.ReadFullCatalog_OldWithJoin(catalogID);
    }

    public static List<ServiceMediaTag> ReadFullCatalogMediaTags(Guid catalogID) => LocalService.Media.ReadFullCatalogMediaTags(catalogID);
    public static List<ServiceMediaItem> ReadFullCatalogMedia(Guid catalogID) => LocalService.Media.ReadFullCatalogMedia(catalogID);

    public static List<ServiceWorkgroup> GetAvailableWorkgroups(Guid catalogID)
    {
        return LocalService.Workgroup.ReadWorkgroups(catalogID);
    }

    public static void DeleteAllWorkgroups(Guid catalogID) => LocalService.Workgroup.DeleteAllWorkgroups(catalogID);

    public static void CreateWorkgroup(Guid catalogID, ServiceWorkgroup workgroup)
    {
        LocalService.Workgroup.CreateWorkgroup(catalogID, workgroup);
    }

    public static void UpdateWorkgroup(Guid catalogID, ServiceWorkgroup workgroup)
    {
        LocalService.Workgroup.UpdateWorkgroup(catalogID, workgroup);
    }

    public static ServiceWorkgroup GetWorkgroupDetails(Guid catalogID, Guid id)
    {
        return LocalService.Workgroup.GetWorkgroupDetails(catalogID, id);
    }

    public static List<ServiceStack> GetAllStacks(Guid catalogID)
    {
        return LocalService.Stacks.GetAllStacks(catalogID);
    }

    public static void UpdateMediaStacks(Guid catalogID, List<MediaStackDiff> diffs)
    {
        LocalService.Stacks.UpdateMediaStacks(catalogID, diffs);
    }

#if WG_ON_SQL
    public static List<ServiceWorkgroupItemClient> ReadWorkgroupMedia(Guid id)
    {
        return LocalService.Workgroup.ReadWorkgroupMedia(id);
    }
#endif
}
