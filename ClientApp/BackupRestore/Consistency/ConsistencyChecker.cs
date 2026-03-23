using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Thetacat.Azure;
using Thetacat.BackupRestore.Consistency;
using Thetacat.Import;
using Thetacat.Model;
using Thetacat.Model.Workgroups;
using Thetacat.Repair;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;
using Thetacat.UI;
using Thetacat.UI.Controls.MediaItemsListControl;
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

    /*----------------------------------------------------------------------------
        %%Function: CheckWorkgroupItems
        %%Qualified: Thetacat.Export.ConsistencyChecker.CheckWorkgroupItems
    ----------------------------------------------------------------------------*/
    public void CheckWorkgroupItems(List<ConsistencyResult> results, ChunkedProgressReport progress)
    {
        ObservableCollection<MediaItemsListItem> missing = new();

        int count = m_cache.Entries.Count;
        int i = 0;

        foreach (ICacheEntry entry in m_cache.Entries.Values)
        {
            progress.UpdateProgress(i++, count);
            if (!m_catalog.TryGetMedia(entry.ID, out _))
                missing.Add(MediaItemsListItem.Create(entry));
        }

        if (missing.Count > 0)
        {
            ConsistencyResult result = new(
                "Missing Cache Entries",
                "These items are in the workgroup but do not have entries in the catalog.",
                missing);

            results.Add(result);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: CheckUploadStatesAgainstAzure
        %%Qualified: Thetacat.Export.ConsistencyChecker.CheckUploadStatesAgainstAzure
    ----------------------------------------------------------------------------*/
    async Task CheckUploadStatesAgainstAzure(List<ConsistencyResult> results, ChunkedProgressReport progress)
    {
        AzureCat.EnsureCreated(App.State.AzureStorageAccount);
        ObservableCollection<MediaItemsListItem> nonGuidBlobs = new();
        ObservableCollection<MediaItemsListItem> missingBlobs = new();
        ObservableCollection<MediaItemsListItem> badHash = new();

        TcBlobContainer container = await AzureCat._Instance.OpenContainerForCatalog(App.State.ActiveProfile.StorageContainer!);

        progress.UpdateProgress(1, 10);
        List<TcBlob> blobs = await container.EnumerateBlobs();
        // build a lookup table by id
        progress.UpdateProgress(3, 10);

        Dictionary<Guid, TcBlob> blobLookup = new();

        foreach (TcBlob blob in blobs)
        {
            if (!Guid.TryParse(blob.BlobName, out Guid id))
            {
                nonGuidBlobs.Add(MediaItemsListItem.Create(blob.BlobName));
            }
            else
            {
                blobLookup[id] = blob;
            }
        }

        progress.UpdateProgress(6, 10);

        // now, confirm our catalog with the blobs
        foreach (MediaItem item in m_catalog.GetMediaCollection())
        {
            if (item.State != MediaItemState.Active)
                continue;
            if (!blobLookup.TryGetValue(item.ID, out TcBlob? blob))
            {
                missingBlobs.Add(MediaItemsListItem.Create(item));
                continue;
            }

            if (blob.ContentMd5 != item.MD5)
            {
                badHash.Add(MediaItemsListItem.Create(item, $"{blob.ContentMd5} != {item.MD5}"));
            }
        }

        progress.UpdateProgress(10, 10);

        if (nonGuidBlobs.Count > 0)
        {
            ConsistencyResult result = new(
                "Bad Blob Names",
                "These Azure blobs do not have names that are GUIDs",
                nonGuidBlobs);

            results.Add(result);
        }

        if (missingBlobs.Count > 0)
        {
            ConsistencyResult result = new(
                "Missing Blob",
                "These catalog items are marked as Active but do not have blobs in Azure.",
                missingBlobs);

            results.Add(result);
        }

        if (badHash.Count > 0)
        {
            ConsistencyResult result = new(
                "MD5 Mismath",
                "These catalog items have corresponding blobs in Azure, but the MD5 hashes are not the same",
                badHash);

            results.Add(result);
        }
    }

    bool FileExistsInWorkgroupCache(PathSegment path, string? md5)
    {
        return WorkgroupRepair.FileExistsInWorkgroupCache(m_cache, path, md5);
    }

    public void CheckCatalogAgainstWorkgroup(List<ConsistencyResult> results, ChunkedProgressReport progress)
    {
        // look for items that aren't uploaded, but also don't exist in the local cache (which means there
        // is no way for the upload to succeed)
        ObservableCollection<MediaItemsListItem> missingImportEntry = new();
        ObservableCollection<MediaItemsListItem> pendingCatalogItemsNotPendingUploadInImport = new();
        ObservableCollection<MediaItemsListItem> pendingCatalogItemsAwaitingClientUpload = new();
        ObservableCollection<MediaItemsListItem> workgroupMediaMissing = new();
        ObservableCollection<MediaItemsListItem> duplicateMD5 = new();

        IReadOnlyCollection<MediaItem> collection = m_catalog.GetMediaCollection();

        int count = collection.Count;
        int i = 0;

        Dictionary<string, List<MediaItem>> md5Map = new();
        foreach (MediaItem item in collection)
        {
            if (!md5Map.ContainsKey(item.MD5))
                md5Map[item.MD5] = new List<MediaItem>();

            md5Map[item.MD5].Add(item);
        }

        foreach(KeyValuePair<string, List<MediaItem>> pair in md5Map)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                continue;
                    
            if (pair.Value.Count > 1)
            {
                foreach (MediaItem item in pair.Value)
                {
                    duplicateMD5.Add(MediaItemsListItem.Create(item, $"hash:{pair.Key}"));
                }
            }
        }

        foreach (MediaItem item in collection)
        {
            progress.UpdateProgress(i++, count);
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
                        {
                            if (!string.IsNullOrWhiteSpace(item.MD5))
                            {
                                List<MediaItem> items = md5Map[item.MD5];

                                // first, make sure our item is in the list
                                if (items.Find(_item => _item.ID == item.ID) == null)
                                {
                                    missingImportEntry.Add(MediaItemsListItem.Create(item, "VirtualPath exists, MD5 missing from MD5 map (INTERNAL FAILURE)"));
                                }
                                else
                                {
                                    MediaItem? dupe = items.Find(_item => _item.VirtualPath.Equals(item.VirtualPath));

                                    if (dupe != null)
                                    {
                                        missingImportEntry.Add(MediaItemsListItem.Create(item, $"Duplicate: {dupe.ID}"));
                                    }
                                    else
                                    {
                                        missingImportEntry.Add(MediaItemsListItem.Create(item, "VirtualPath exists, non-dupe"));
                                    }
                                }
                            }
                        }
                        else
                        {
                            missingImportEntry.Add(MediaItemsListItem.Create(item, "VirtualPath not found"));
                        }

                        continue;
                    }

                    ImportItem.ImportState importItemState = ImportItem.StateFromString(importItem.State!);

                    if (importItemState == ImportItem.ImportState.Complete)
                    {
                        pendingCatalogItemsNotPendingUploadInImport.Add(MediaItemsListItem.Create(item));
                        continue;
                    }

                    pendingCatalogItemsAwaitingClientUpload.Add(MediaItemsListItem.Create(item, $"Source = {importItem.Source}"));
                }
            }
            else
            {
                // we have an entry in the workgroup database. validate it
                if (FileExistsInWorkgroupCache(cacheEntry.Path, null))
                {
                    if (!FileExistsInWorkgroupCache(cacheEntry.Path, cacheEntry.MD5))
                    {
                        // the file exists, but the MD5 hash doesn't match
                        workgroupMediaMissing.Add(MediaItemsListItem.Create(item, "MD5 mismatch"));
                    }
                }
                else
                {
                    workgroupMediaMissing.Add(MediaItemsListItem.Create(item));
                }
            }
        }

        if (duplicateMD5.Count > 0)
        {
            ConsistencyResult result = new(
                "Duplicate MD5",
                "These catalog items have the same MD5 hash.",
                duplicateMD5);
            results.Add(result);
        }

        if (missingImportEntry.Count > 0)
        {
            ConsistencyResult result = new(
                "Missing Import Entry",
                "These catalog items are marked as Pending, do not have entries in the Workgroup database, and have no entry in the imports table.",
                missingImportEntry);
            results.Add(result);
        }

        if (pendingCatalogItemsNotPendingUploadInImport.Count > 0)
        {
            ConsistencyResult result = new(
                "Pending Item Not Pending",
                "These catalog items are marked as Pending, do not have entries in the Workgroup database, and their corresponding entry in the imports table are not marked as pending.",
                pendingCatalogItemsNotPendingUploadInImport);
            results.Add(result);
        }

        if (pendingCatalogItemsAwaitingClientUpload.Count > 0)
        {
            ConsistencyResult result = new(
                "Pending Item Awaiting Client Upload",
                "These catalog items are marked as Pending, do not have entries in the Workgroup database, and are pending upload from a client.",
                pendingCatalogItemsAwaitingClientUpload);

            results.Add(result);
        }

        if (workgroupMediaMissing.Count > 0)
        {
            ConsistencyResult result = new(
                "Workgroup Media Missing",
                "These catalog items have entries in the Workgroup database, but the media is missing from the cache.",
                workgroupMediaMissing);
            results.Add(result);
        }
    }

    public static void CheckConsistency(Guid catalogId, ICatalog catalog, ICache cache, IWorkgroup workgroup)
    {

        App.State.AddBackgroundWork(
            "Checking for consistency errors",
            (IProgressReport progressReport) =>
            {
                ChunkedProgressReport chunkedProgress = new(progressReport);

                chunkedProgress.AddWeightedChunk("imports", 1.0);
                chunkedProgress.AddWeightedChunk("checkWorkgroup", 3.0);
                chunkedProgress.AddWeightedChunk("checkAzure", 10.0);
                chunkedProgress.AddWeightedChunk("checkCatalog", 90.0);

                chunkedProgress.StartBlock("imports");
                List<ServiceImportItem> imports = ServiceInterop.GetAllImports(catalogId);

                ConsistencyChecker checker = new(catalog, cache, workgroup, imports);

                List<ConsistencyResult> results = new();
                chunkedProgress.StartBlock("checkWorkgroup");
                checker.CheckWorkgroupItems(results, chunkedProgress);

                try
                {
                    chunkedProgress.StartBlock("checkAzure");
                    Task task = checker.CheckUploadStatesAgainstAzure(results, chunkedProgress);
                    task.Wait();
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"caught exception trying to check Azure blobs: {exc}");
                }

                chunkedProgress.StartBlock("checkCatalog");
                checker.CheckCatalogAgainstWorkgroup(results, chunkedProgress);

                ThreadContext.InvokeOnUiThread(() => ConsistencyResults.ShowResults(results));
                return true;
            });

        // look for items in the workgroup that don't exist in the catalog

    }
}
