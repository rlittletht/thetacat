using System;
using System.Collections.Generic;
using Thetacat.Import;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient.LocalService;

namespace Thetacat.ServiceClient;

public class ServiceInterop
{
    public static MetatagSchema GetMetatagSchema()
    {
        return MetatagSchema.CreateFromService(LocalService.Metatags.GetMetatagSchema());
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
}
