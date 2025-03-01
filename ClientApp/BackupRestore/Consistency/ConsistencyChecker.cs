using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Thetacat.Azure;
using Thetacat.Import;
using Thetacat.Model;
using Thetacat.Model.Workgroups;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Export;

/*----------------------------------------------------------------------------
    %%Class: ConsistencyChecker

    Check for all the known consistency issues that could happen in the
    database
----------------------------------------------------------------------------*/
public class ConsistencyChecker
{
    private readonly IWorkgroup m_workgroup;
    private readonly ICache m_cache;
    private readonly ICatalog m_catalog;
    private readonly Dictionary<Guid, ServiceImportItem> m_imports = new();
    private readonly Dictionary<Guid, ServiceWorkgroupClient> m_clients = new();

    public ConsistencyChecker(ICatalog catalog, ICache cache, IWorkgroup workgroup, List<ServiceImportItem> imports)
    {
        m_workgroup = workgroup;
        m_cache = cache;
        m_catalog = catalog;

        foreach (ServiceImportItem import in imports)
        {
            m_imports.Add(import.ID, import);
        }
        
        foreach (ServiceWorkgroupClient client in workgroup.GetWorkgroupClients())
        {
            m_clients.Add(client.ClientId!.Value, client);
        }
    }

    public void CheckWorkgroupItems()
    {
        List<ICacheEntry> missing = new();

        foreach (ICacheEntry entry in m_cache.Entries.Values)
        {
            if (!m_catalog.TryGetMedia(entry.ID, out _))
                missing.Add(entry);
        }

        if (missing.Count > 0)
        {
            List<string> missingDescriptions = new();

            foreach (ICacheEntry entry in missing)
            {
                missingDescriptions.Add($"{entry.ID}: {entry.Path}");
            }

            MessageBox.Show($"The following items are in the workgroup but not in the catalog:\n\n{string.Join("\n", missingDescriptions)}");
        }
    }

    public async Task CheckUploadStatesAgainstAzure()
    {
        AzureCat.EnsureCreated(App.State.AzureStorageAccount);
        List<string> nonGuidBlob = new();
        List<MediaItem> missingBlobs = new();
        List<MediaItem> badHash = new();
        TcBlobContainer container = await AzureCat._Instance.OpenContainerForCatalog(App.State.ActiveProfile.StorageContainer!);

        List<TcBlob> blobs = await container.EnumerateBlobs();
        // build a lookup table by id

        Dictionary<Guid, TcBlob> blobLookup = new();

        foreach (TcBlob blob in blobs)
        {
            if (!Guid.TryParse(blob.BlobName, out Guid id))
            {
                nonGuidBlob.Add(blob.BlobName);
            }
            else
            {
                blobLookup[id] = blob;
            }
        }

        // now, confirm our catalog with the blobs
        foreach (MediaItem item in m_catalog.GetMediaCollection())
        {
            if (item.State != MediaItemState.Active)
                continue;
            if (!blobLookup.TryGetValue(item.ID, out TcBlob? blob))
            {
                missingBlobs.Add(item);
                continue;
            }

            if (blob.ContentMd5 != item.MD5)
            {
                badHash.Add(item);
            }
        }

        if (nonGuidBlob.Count > 0)
        {
            MessageBox.Show($"The following blobs are not guids:\n\n{string.Join("\n", nonGuidBlob)}");
        }

        if (missingBlobs.Count > 0)
        {
            List<string> missingDescriptions = new();
            foreach (MediaItem item in missingBlobs)
            {
                missingDescriptions.Add($"{item.ID}: {item.VirtualPath}");
            }
            MessageBox.Show($"The following items are in the catalog but not in the blobs:\n\n{string.Join("\n", missingDescriptions)}");
        }

        if (badHash.Count > 0)
        {
            List<string> badHashDescriptions = new();
            foreach (MediaItem item in badHash)
            {
                badHashDescriptions.Add($"{item.ID}: {item.VirtualPath}");
            }
            MessageBox.Show($"The following items have bad MD5 hashes:\n\n{string.Join("\n", badHashDescriptions)}");
        }
    }

    bool FileExistsInWorkgroupCache(PathSegment path, string? md5)
    {
        string fullPath = m_cache.GetFullLocalPath(path);

        bool exists = File.Exists(fullPath);

        if (!exists || string.IsNullOrEmpty(md5))
            return exists;

        string md5Local = App.State.Md5Cache.GetMd5ForPathSync(fullPath);

        return md5Local == md5;
    }

    public void CheckCatalogAgainstWorkgroup()
    {
        // look for items that aren't uploaded, but also don't exist in the local cache (which means there
        // is no way for the upload to succeed)
        List<MediaItem> missingImportEntryNoLocalMediaForUpload = new();
        List<MediaItem> missingImportEntryHasLocalMedia = new();
        List<MediaItem> pendingCatalogItemsNotPendingUploadInImport = new();
        List<MediaItem> pendingCatalogItemsAwaitingClientUpload = new();

        foreach (MediaItem item in m_catalog.GetMediaCollection())
        {
            m_imports.TryGetValue(item.ID, out ServiceImportItem? importItem);

            if (!m_cache.Entries.TryGetValue(item.ID, out ICacheEntry? cacheEntry))
            {
                // this isn't in the workgroup database
                if (item.State == MediaItemState.Active)
                {
                    // this item is on the server and hasn't been cached locally. this is an expected state
                    continue;
                }

                if (item.State == MediaItemState.Pending)
                {
                    // this item is waiting to be uploaded. if it doesn't exist in the cache, then it still
                    // might be ok -- if this was added by a different workgroup. Check to see which client
                    // owns uploading this item

                    if (importItem == null)
                    {
                        // since we have no cache entry, we don't know what the real local path should be
                        // the best guess is the virtual path
                        if (FileExistsInWorkgroupCache(item.VirtualPath, item.MD5))
                            missingImportEntryHasLocalMedia.Add(item);
                        else
                            missingImportEntryNoLocalMediaForUpload.Add(item);

                        continue;
                    }

                    ImportItem.ImportState importItemState = ImportItem.StateFromString(importItem.State!);

                    if (importItemState == ImportItem.ImportState.Complete)
                    {
                        pendingCatalogItemsNotPendingUploadInImport.Add(item);
                        continue;
                    }

                    pendingCatalogItemsAwaitingClientUpload.Add(item);
                }
            }
        }
    }

    public static async Task CheckConsistency(Guid catalogId, ICatalog catalog, ICache cache, IWorkgroup workgroup)
    {
        List<ServiceImportItem> imports = ServiceInterop.GetAllImports(catalogId);

        ConsistencyChecker checker = new(catalog, cache, workgroup, imports);

        // look for items in the workgroup that don't exist in the catalog
        checker.CheckWorkgroupItems();
        await checker.CheckUploadStatesAgainstAzure();
        checker.CheckCatalogAgainstWorkgroup();
    }
}
