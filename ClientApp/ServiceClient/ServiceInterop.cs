using System;
using System.Collections.Generic;
using Thetacat.Import;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.ServiceClient.LocalService;

namespace Thetacat.ServiceClient;

public class ServiceInterop
{
    public static ServiceMetatagSchema GetMetatagSchema()
    {
        return LocalService.Metatags.GetMetatagSchema();
    }

    public static void UpdateMetatagSchema(MetatagSchemaDiff schemaDiff)
    {
        LocalService.Metatags.UpdateMetatagSchema(schemaDiff);
    }

    public static void UpdateMediaItems(IEnumerable<MediaItemDiff> diffs)
    {
        LocalService.Media.UpdateMediaItems(diffs);
    }
    public static List<ServiceImportItem> GetPendingImportsForClient(string sourceClient)
    {
        return LocalService.Import.GetPendingImportsForClient(sourceClient);
    }

    public static void InsertImportItems(IEnumerable<ImportItem> items)
    {
        LocalService.Import.InsertImportItems(items);
    }

    public static void CompleteImportForItem(Guid id)
    {
        LocalService.Import.CompleteImportForItem(id);
    }

    public static void DeleteImportItem(Guid id)
    {
        LocalService.Import.DeleteImportItem(id);
    }

    public static void InsertNewMediaItems(IEnumerable<MediaItem> newItems)
    {
        LocalService.Media.InsertNewMediaItems(newItems);
    }

    public static ServiceCatalog ReadFullCatalog()
    {
        return LocalService.Media.ReadFullCatalog_OldWithJoin();
    }

    public static List<ServiceMediaTag> ReadFullCatalogMediaTags() => LocalService.Media.ReadFullCatalogMediaTags();
    public static List<ServiceMediaItem> ReadFullCatalogMedia() => LocalService.Media.ReadFullCatalogMedia();

    public static List<ServiceWorkgroup> GetAvailableWorkgroups()
    {
        return LocalService.Workgroup.ReadWorkgroups();
    }

    public static void CreateWorkgroup(ServiceWorkgroup workgroup)
    {
        LocalService.Workgroup.CreateWorkgroup(workgroup);
    }

    public static void UpdateWorkgroup(ServiceWorkgroup workgroup)
    {
        LocalService.Workgroup.UpdateWorkgroup(workgroup);
    }

    public static ServiceWorkgroup GetWorkgroupDetails(Guid id)
    {
        return LocalService.Workgroup.GetWorkgroupDetails(id);
    }

    public static List<ServiceStack> GetAllStacks()
    {
        return LocalService.Stacks.GetAllStacks();
    }

    public static void UpdateMediaStacks(List<MediaStackDiff> diffs)
    {
        LocalService.Stacks.UpdateMediaStacks(diffs);
    }

#if WG_ON_SQL
    public static List<ServiceWorkgroupItemClient> ReadWorkgroupMedia(Guid id)
    {
        return LocalService.Workgroup.ReadWorkgroupMedia(id);
    }
#endif
}
