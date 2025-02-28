using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Thetacat.Azure;
using Thetacat.BackupRestore.Restore;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Workgroups;
using Thetacat.TcSettings;
using Thetacat.Types;
using Thetacat.UI;
using XMLIO;
using System.Collections.Concurrent;
using Thetacat.Model.Caching;
using Thetacat.ServiceClient;

namespace Thetacat.Export;

public class RestoreDatabase
{
    private string m_backupSource;
    public FullExportRestore? FullExportRestore;

    public RestoreDatabase(string backupSource)
    {
        m_backupSource = backupSource;
    }

    private IProgressReport? m_progress;

    public bool DoRestore(IProgressReport? progress)
    {
        m_progress = progress;

        using Stream stm = File.Open(m_backupSource, FileMode.Open);
        using XmlReader reader = XmlReader.Create(stm);

        if (!XmlIO.Read(reader))
            return true;

        XmlIO.SkipNonContent(reader);

        FullExportRestore = new FullExportRestore(reader);

        m_progress?.WorkCompleted();
        return true;
    }

    public static async Task MigrateAzureBlobsForRemap(Profile sourceProfile, Profile targetProfile, GuidMaps idMaps, ICatalog catalog)
    {
        try
        {
            AzureCat.EnsureCreated(App.State.AzureStorageAccount);

            TcBlobContainer targetContainer = await AzureCat._Instance.OpenContainerForCatalog(targetProfile.StorageContainer!);
            TcBlobContainer sourceContainer = await AzureCat._Instance.OpenContainerForCatalog(sourceProfile.StorageContainer!);

            List<KeyValuePair<Guid, Guid>> mediaToCopy = new();

            foreach (MediaItem item in catalog.GetMediaCollection())
            {
                if (item.State != MediaItemState.Active)
                    continue;

                //get the original id
                Guid oldId = idMaps.GetOld(IdType.Media, item.ID);
                mediaToCopy.Add(new KeyValuePair<Guid, Guid>(oldId, item.ID));
            }

            foreach (KeyValuePair<Guid, Guid> media in mediaToCopy)
            {
                await sourceContainer.CopyToContainer(targetContainer, media.Key.ToString(), media.Value.ToString());
            }
        }
        catch (Exception exc)
        {
            MessageBox.Show($"caught exception trying to migrate blobs: {exc}");
        }
    }

    public static async Task MigrateWorkgroup(Profile sourceProfile, Profile targetProfile, GuidMaps idMaps, ICatalog catalog, WorkgroupDataRestore restore)
    {
        try
        {
            Workgroup? targetWorkgroup = Workgroup.CreateWorkgroupNoCaching(targetProfile.CatalogID, targetProfile.WorkgroupId!);
            Cache cache = new Cache(targetProfile, targetWorkgroup);

            if (targetWorkgroup == null)
                throw new Exception($"invalid workgroupId: {targetProfile.WorkgroupId}");

            ConcurrentDictionary<Guid, ICacheEntry> targetEntries = new();

            targetWorkgroup.RefreshWorkgroupMedia(targetEntries);

            // restore clients
            targetWorkgroup.RestoreWorkgroupClients(restore.Clients);

            // restore filters
            List<WorkgroupFilter> filters = new();

            foreach (WorkgroupFilterData filterData in restore.Filters)
            {
                WorkgroupFilter filter = new WorkgroupFilter(filterData.Id, filterData.Name, filterData.Description, filterData.Expression);
                filters.Add(filter);
            }

            targetWorkgroup.ExecuteFilterAddsAndDeletes(new List<WorkgroupFilter>(), filters);

            // restore media.  create cache entries for everything we are restoring
            foreach (WorkgroupCacheEntryData mediaData in restore.MediaItems)
            {
                WorkgroupCacheEntry entry = new WorkgroupCacheEntry(
                    mediaData.ID,
                    mediaData.Path,
                    mediaData.CachedBy,
                    mediaData.CacheDate,
                    false /*localPending*/,
                    mediaData.VectorClock,
                    mediaData.MD5);

                cache.AddCacheItemForRestore(entry);
            }

            targetWorkgroup.RestoreCacheEntriesToDatabase(cache.Entries.Values);

            // set the workgroup clock
            targetWorkgroup.SetWorkgroupClockForRestore(restore.WorkgroupClock);
            

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
