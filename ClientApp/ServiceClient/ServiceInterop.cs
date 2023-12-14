﻿using System;
using System.Collections.Generic;
using Thetacat.Import;
using Thetacat.Model;
using Thetacat.Model.Metatags;
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

    public static List<ServiceImportItem> GetPendingImportsForClient(string sourceClient)
    {
        return LocalService.Import.GetPendingImportsForClient(sourceClient);
    }

    public static void InsertImportItems(IEnumerable<ImportItem> items)
    {
        LocalService.Import.InsertImportItems(items);
    }

    public static void UpdateImportStateForItem(Guid id, ImportItem item, ImportItem.ImportState newState)
    {
        LocalService.Import.UpdateImportStateForItem(id, item, newState);
    }

    public static void InsertNewMediaItems(IEnumerable<MediaItem> newItems)
    {
        LocalService.Media.InsertNewMediaItems(newItems);
    }

    public static ServiceCatalog ReadFullCatalog()
    {
        return LocalService.Media.ReadFullCatalog();
    }

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

    public static List<ServiceWorkgroupItemClient> ReadWorkgroupMedia(Guid id)
    {
        return LocalService.Workgroup.ReadWorkgroupMedia(id);
    }

}
