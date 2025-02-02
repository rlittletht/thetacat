using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
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

    public static ServiceDeletedItemsClock GetDeletedMediaItems(Guid catalogId) => LocalService.Media.GetDeletedMediaItems(catalogId);

    public static void DeleteAllMediaAndMediaTagsAndStacks(Guid catalogID) => LocalService.Media.DeleteAllMediaAndMediaTagsAndStacks(catalogID);
    public static void DeleteAllStacksAssociatedWithMedia(Guid catalogID) => LocalService.Stacks.DeleteAllStacksAssociatedWithMedia(catalogID);

    public static void UpdateMediatagsWithNoClockAndincrementVectorClock(Guid catalogID)
    {
        // first, figure out how many we are going to update
        int count = LocalService.Mediatags.GetMediatagsPendingClockCount(catalogID);
        bool fNeedRebuildIndex = false;

        if (count > 175000)
        {
            // better to disable the index and rebuild later
            LocalService.Mediatags.DisableClockIndex();
            fNeedRebuildIndex = true;
        }

        try
        {
            while (LocalService.Mediatags.UpdateMediatagsWithNoClockAndincrementVectorClockBatched(catalogID, 5000) > 0)
            {
                Thread.Sleep(100);
            }
        }
        finally
        {
            if (fNeedRebuildIndex)
                LocalService.Mediatags.RebuildClockIndex();
        }
    }

    public static void DeleteMediaItem(Guid catalogID, Guid id) => LocalService.Media.DeleteMediaItem(catalogID, id);

    public static void DeleteImportsForMediaItem(Guid catalogId, Guid id) => LocalService.Import.DeleteMediaItem(catalogId, id);

    public static List<ServiceImportItem> GetImportsForClient(Guid catalogID, string sourceClient)
    {
        return LocalService.Import.GetImportsForClient(catalogID, sourceClient);
    }

    public static List<ServiceImportItem> GetAllImportsPendingUpload(Guid catalogID)
    {
        return LocalService.Import.GetAllImportsPendingUpload(catalogID);
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

    public static void ResetImportToPendingForItem(Guid catalogID, Guid id, string clientName)
    {
        LocalService.Import.ResetImportToPendingForItem(catalogID, id, clientName);
    }

    public static void DeleteImportItem(Guid catalogID, Guid id)
    {
        LocalService.Import.DeleteImportItem(catalogID, id);
    }

    public static void InsertNewMediaItems(Guid catalogID, IEnumerable<MediaItem> newItems)
    {
        LocalService.Media.InsertNewMediaItems(catalogID, newItems);
    }

//    public static ServiceMediaTagsWithClocks ReadFullCatalogMediaTags(Guid catalogID) => LocalService.Mediatags.ReadFullCatalogMediaTags(catalogID);

    public static void RemoveDeletedMediatagsAndResetTagClock(Guid CatalogID) => LocalService.Mediatags.RemoveDeletedMediatagsAndResetTagClock(CatalogID);

    public static ServiceMediaTagsWithClocks ReadMediaTagsForClock(Guid catalogID, int tagClock) => LocalService.Mediatags.ReadMediaTagsForClock(catalogID, tagClock);

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

    public static void UpdateWorkgroupDeleteMediaClockToAtLeast(Guid catalogID, Guid id, int newClock)
    {
        LocalService.Workgroup.UpdateWorkgroupDeleteMediaClockToAtLeast(catalogID, id, newClock);
    }

    public static void ExpireDeletedMediaItems(Guid catalogID)
    {
        LocalService.Media.ExpireDeletedMediaItems(catalogID);
    }

    public static void UpdateDeletedMediaWithNoClockAndIncrementVectorClock(Guid catalogID)
    {
        LocalService.Media.UpdateDeletedMediaWithNoClockAndIncrementVectorClock(catalogID);
    }

#if WG_ON_SQL
    public static List<ServiceWorkgroupItemClient> ReadWorkgroupMedia(Guid id)
    {
        return LocalService.Workgroup.ReadWorkgroupMedia(id);
    }
#endif
}
