using MetadataExtractor.Formats.Xmp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Threading.Tasks;
using System.Xml;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.Model.Workgroups;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;
using Thetacat.UI;
using Workgroup = Thetacat.Model.Workgroups.Workgroup;

namespace Thetacat.Export;

public class BackupDatabase
{
    private readonly Catalog m_catalog;
    private readonly MetatagSchema m_schema;
    private readonly string m_filename;
    private readonly bool m_exportMediaItems = false;
    private readonly bool m_exportMediaStacks = false;
    private readonly bool m_exportVersionStacks = false;
    private readonly bool m_exportSchema = false;
    private readonly bool m_exportImports = false;
    private readonly bool m_exportWorkgroups = false;
    private readonly bool m_exportWorkgroupData = false;
    private readonly bool m_exportDeletedMedia = false;

    public static string s_uri = "https://schemas.thetasoft.com/thetacat/backup/2024";
    private ProgressChunks m_progressChunks = new();

    public BackupDatabase(
        string backupPath,
        bool exportMediaItems,
        bool exportMediaStacks,
        bool exportVersionStacks,
        bool exportSchema,
        bool exportImports,
        bool exportDeletedMedia,
        bool exportWorkgroups,
        bool exportWorkgroupData)
    {
        m_schema = new MetatagSchema(false);
        m_catalog = new Catalog();
        m_filename = backupPath;
        m_exportMediaItems = exportMediaItems;
        m_exportMediaStacks = exportMediaStacks;
        m_exportVersionStacks = exportVersionStacks;
        m_exportSchema = exportSchema;
        m_exportImports = exportImports;
        m_exportWorkgroups = exportWorkgroups;
        m_exportWorkgroupData = exportWorkgroupData;
        m_exportDeletedMedia = exportDeletedMedia;
    }

    public delegate void WriteChildrenDelegate(XmlWriter writer);

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

#region Progress Reporting

    private IProgressReport? m_progress;
    private double m_blockStart = 0.0;
    private double m_blockEnd = 0.0;

    /*----------------------------------------------------------------------------
        %%Function: StartNextBlock
        %%Qualified: Thetacat.Export.BackupDatabase.StartNextBlock
    ----------------------------------------------------------------------------*/
    void StartNextBlock(double pctEnd)
    {
        m_progress?.UpdateProgress(m_blockEnd);
        m_blockStart = m_blockEnd;
        m_blockEnd = pctEnd;
    }

    void StartBlock(string name)
    {
        double pctEnd = m_progressChunks.GetChunkPercent(name);
        StartNextBlock(pctEnd);
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateProgress
        %%Qualified: Thetacat.Export.BackupDatabase.UpdateProgress
    ----------------------------------------------------------------------------*/
    void UpdateProgress(int idxCur, int idxMax)
    {
        m_progress?.UpdateProgress(m_blockStart + ((idxCur * 100.0) / idxMax) * (m_blockEnd - m_blockStart));
    }

#endregion

#region Metatags/Schema

    /*----------------------------------------------------------------------------
        %%Function: WriteMetatag
        %%Qualified: Thetacat.Export.BackupDatabase.WriteMetatag
    ----------------------------------------------------------------------------*/
    public void WriteMetatag(XmlWriter writer, Metatag metatag)
    {
        writer.WriteAttributeString("id", metatag.ID.ToString());
        WriteElement(writer, "name", (_writer) => _writer.WriteString(metatag.Name));
        WriteElement(writer, "description", (_writer) => _writer.WriteString(metatag.Description));
        WriteElement(writer, "standard", (_writer) => _writer.WriteString(metatag.Standard));
        if (metatag.Parent != null)
            WriteElement(writer, "parentId", (_writer) => _writer.WriteString(metatag.Parent.ToString()));
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteMetatagDefinitions
        %%Qualified: Thetacat.Export.BackupDatabase.WriteMetatagDefinitions
    ----------------------------------------------------------------------------*/
    public void WriteMetatagDefinitions(XmlWriter writer, MetatagSchema schema)
    {
        int count = schema.MetatagCount;
        int i = 0;

        foreach (Metatag metatag in schema.MetatagsWorking)
        {
            UpdateProgress(i++, count);
            WriteElement(
                writer,
                "metatag",
                (_writer) => WriteMetatag(_writer, metatag));
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteSchema
        %%Qualified: Thetacat.Export.BackupDatabase.WriteSchema
    ----------------------------------------------------------------------------*/
    public void WriteSchema(XmlWriter writer, MetatagSchema schema)
    {
        StartBlock("schema");

        WriteElement(
            writer,
            "metatagSchema",
            (_writer) => WriteMetatagDefinitions(_writer, schema));
    }

#endregion

#region Media

    /*----------------------------------------------------------------------------
        %%Function: WriteMediaTag
        %%Qualified: Thetacat.Export.BackupDatabase.WriteMediaTag
    ----------------------------------------------------------------------------*/
    public void WriteMediaTag(XmlWriter writer, MediaTag mediaTag)
    {
        writer.WriteAttributeString("metatagId", mediaTag.Metatag.ID.ToString());

        if (mediaTag.Value != null)
        {
            if (string.IsNullOrWhiteSpace(mediaTag.Value))
                writer.WriteAttributeString("xml", "space", null, "preserve");

            writer.WriteString(mediaTag.Value);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteMediaTags
        %%Qualified: Thetacat.Export.BackupDatabase.WriteMediaTags
    ----------------------------------------------------------------------------*/
    public void WriteMediaTags(XmlWriter writer, IEnumerable<MediaTag> tags)
    {
        foreach (MediaTag tag in tags)
        {
            WriteElement(writer, "tag", (_writer) => WriteMediaTag(_writer, tag));
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteMediaItem
        %%Qualified: Thetacat.Export.BackupDatabase.WriteMediaItem
    ----------------------------------------------------------------------------*/
    public void WriteMediaItem(XmlWriter writer, MediaItem mediaItem)
    {
        writer.WriteAttributeString("id", mediaItem.ID.ToString());
        WriteElement(writer, "md5", (_writer) => _writer.WriteString(mediaItem.MD5));
        WriteElement(writer, "virtualPath", (_writer) => _writer.WriteString(mediaItem.VirtualPath));
        WriteElement(writer, "mimeType", (_writer) => _writer.WriteString(mediaItem.MimeType));
        WriteElement(writer, "state", (_writer) => _writer.WriteString(MediaItem.StringFromState(mediaItem.State)));

        WriteElement(writer, "mediaTags", (_writer) => WriteMediaTags(_writer, mediaItem.MediaTags));
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteMediaItems
        %%Qualified: Thetacat.Export.BackupDatabase.WriteMediaItems
    ----------------------------------------------------------------------------*/
    public void WriteMediaItems(XmlWriter writer, Catalog catalog)
    {
        int count = catalog.GetMediaCount;
        int i = 0;

        foreach (MediaItem item in catalog.GetMediaCollection())
        {
            UpdateProgress(i++, count);

            WriteElement(
                writer,
                "mediaItem",
                (_writer) => WriteMediaItem(_writer, item));
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteMediaStackItem
        %%Qualified: Thetacat.Export.BackupDatabase.WriteMediaStackItem
    ----------------------------------------------------------------------------*/
    public void WriteMediaStackItem(XmlWriter writer, MediaStackItem item)
    {
        writer.WriteAttributeString("mediaId", item.MediaId.ToString());
        writer.WriteAttributeString("stackIndex", item.StackIndex.ToString());
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteMediaStack
        %%Qualified: Thetacat.Export.BackupDatabase.WriteMediaStack
    ----------------------------------------------------------------------------*/
    public void WriteMediaStack(XmlWriter writer, MediaStack stack)
    {
        writer.WriteAttributeString("id", stack.StackId.ToString());

        foreach (MediaStackItem item in stack.Items)
        {
            WriteElement(writer, "stackItem", (_writer) => WriteMediaStackItem(_writer, item));
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteMediaStacks
        %%Qualified: Thetacat.Export.BackupDatabase.WriteMediaStacks
    ----------------------------------------------------------------------------*/
    public void WriteMediaStacks(XmlWriter writer, MediaStacks stacks)
    {
        int count = stacks.Items.Count;
        int i = 0;

        foreach (MediaStack stack in stacks.Items.Values)
        {
            UpdateProgress(i++, count);
            WriteElement(writer, "stack", (_writer) => WriteMediaStack(_writer, stack));
        }
    }

#endregion

    /*----------------------------------------------------------------------------
        %%Function: WriteCatalog
        %%Qualified: Thetacat.Export.BackupDatabase.WriteCatalog
    ----------------------------------------------------------------------------*/
    public void WriteCatalog(XmlWriter writer, Catalog catalog)
    {
        WriteElement(
            writer,
            "catalog",
            (_writer) =>
            {
                if (m_exportMediaItems)
                {
                    StartBlock("media");
                    WriteElement(_writer, "media", (__writer) => WriteMediaItems(__writer, catalog));
                }

                if (m_exportVersionStacks)
                {
                    StartBlock("versionStacks");
                    WriteElement(_writer, "versionStacks", (__writer) => WriteMediaStacks(__writer, catalog.VersionStacks));
                }

                if (m_exportMediaStacks)
                {
                    StartBlock("mediaStacks");
                    WriteElement(_writer, "mediaStacks", (__writer) => WriteMediaStacks(__writer, catalog.MediaStacks));
                }
            });
    }

    #region Imports

    /*----------------------------------------------------------------------------
        %%Function: WriteImportItem
        %%Qualified: Thetacat.Export.BackupDatabase.WriteImportItem
    ----------------------------------------------------------------------------*/
    public void WriteImportItem(XmlWriter writer, ServiceImportItem item)
    {
        writer.WriteAttributeString("mediaId", item.ID.ToString());
        WriteElement(writer, "state", (_writer) => _writer.WriteString(item.State));
        WriteElement(writer, "sourcePath", (_writer) => _writer.WriteString(item.SourcePath));
        WriteElement(writer, "sourceServer", (_writer) => _writer.WriteString(item.SourceServer));
        if (item.Source != null)
            WriteElement(writer, "source", (_writer) => _writer.WriteString(item.Source));
        if (item.UploadDate != null)
            WriteElement(writer, "uploadDate", (_writer) => _writer.WriteString(item.UploadDate.Value.ToString("u")));
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteImportItems
        %%Qualified: Thetacat.Export.BackupDatabase.WriteImportItems
    ----------------------------------------------------------------------------*/
    public void WriteImportItems(XmlWriter writer, List<ServiceImportItem> items)
    {
        int count = items.Count;
        int i = 0;

        foreach (ServiceImportItem item in items)
        {
            UpdateProgress(i, count);

            WriteElement(writer, "importItem", (_writer) => WriteImportItem(_writer, item));
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: WriteImports
        %%Qualified: Thetacat.Export.BackupDatabase.WriteImports
    ----------------------------------------------------------------------------*/
    public void WriteImports(XmlWriter writer)
    {
        StartBlock("imports");

        List<ServiceImportItem> importItems = ServiceInterop.GetAllImports(App.State.ActiveProfile.CatalogID);

        WriteElement(writer, "imports", (_writer) => WriteImportItems(_writer, importItems));
    }

    #endregion Imports

    #region Deleted Media

    public void WriteDeletedMedia(XmlWriter writer, Guid catalogId)
    {
        StartBlock("deletedMedia");
        ServiceDeletedItemsClock itemsWithClock =  ServiceInterop.GetDeletedMediaItems(catalogId);

        if (itemsWithClock.DeletedItems.Count == 0)
            return;

        WriteElement(
            writer,
            "deletedMedia",
            _writer =>
            {
                _writer.WriteAttributeString("workgroupDeletedMediaClock", itemsWithClock.VectorClock.ToString());
                WriteDeletedMediaItems(_writer, itemsWithClock);
            });
    }

    public void WriteDeletedMediaItems(XmlWriter writer, ServiceDeletedItemsClock itemsWithClock)
    {
        int count = itemsWithClock.DeletedItems.Count;
        int i = 0;

        foreach (ServiceDeletedItem item in itemsWithClock.DeletedItems)
        {
            UpdateProgress(i, count);
            WriteElement(
                writer, 
                "deletedMediaItem", 
                _writer =>
                {
                    _writer.WriteAttributeString("id", item.Id.ToString());
                    _writer.WriteAttributeString("minWorkgroupClock", item.MinVectorClock.ToString());
                });
        }
    }

    #endregion

    #region Workgroup Data

    public void WriteWorkgroupData(XmlWriter writer, Guid catalogId, Guid workgroupId)
    {
        Workgroup workgroup = new Workgroup(catalogId, workgroupId);

        WriteElement(
            writer,
            "workgroupData",
            _writer =>
            {
                _writer.WriteAttributeString("workgroupId", workgroup.Id.ToString());
                _writer.WriteAttributeString("name", workgroup.Name);

                StartBlock("workgroupClients");
                WriteElement(_writer, "clients", __writer => WriteWorkgroupClients(__writer, workgroup));
                StartBlock("workgroupMedia");
                WriteElement(_writer, "media", __writer => WriteWorkgroupMediaItems(__writer, workgroup));
                StartBlock("workgroupFilters");
                WriteElement(_writer, "filters", __writer => WriteWorkgroupFilters(__writer, workgroup));
                StartBlock("workgroupClocks");
                WriteElement(_writer, "vectorClocks", __writer => WriteWorkgroupVectorClocks(__writer, workgroup));
            });
    }

    public void WriteWorkgroupClients(XmlWriter writer, Workgroup workgroup)
    {
        IReadOnlyCollection<ServiceWorkgroupClient> clients = workgroup.GetWorkgroupClients();
        int count = clients.Count;
        int i = 0;

        foreach (ServiceWorkgroupClient client in clients)
        {
            UpdateProgress(i, count);
            WriteElement(
                writer,
                "client",
                (_writer) =>
                {
                    _writer.WriteAttributeString("id", client.ClientId.ToString());
                    WriteElement(_writer, "name", __writer => __writer.WriteString(client.ClientName));
                    WriteElement(_writer, "vectorClock", __writer => __writer.WriteString(client.VectorClock.ToString()));
                    WriteElement(_writer, "deletedMediaClock", __writer => __writer.WriteString(client.DeletedMediaClock.ToString()));
                });
        }
    }

    public void WriteWorkgroupMediaItems(XmlWriter writer, Workgroup workgroup)
    {
        ConcurrentDictionary<Guid, ICacheEntry> mediaItems = new();

        workgroup.RefreshWorkgroupMedia(mediaItems);

        int count = mediaItems.Count;
        int i = 0;

        foreach (ICacheEntry media in mediaItems.Values)
        {
            UpdateProgress(i, count);
            WriteElement(
                writer,
                "mediaItem",
                (_writer) =>
                {
                    _writer.WriteAttributeString("id", media.ID.ToString());
                    WriteElement(_writer, "path", __writer => __writer.WriteString(media.Path));
                    WriteElement(_writer, "cacheBy", __writer => __writer.WriteString(media.CachedBy.ToString()));
                    WriteElement(_writer, "cachedDate", __writer => __writer.WriteString(media.CachedDate.ToString()));
                    WriteElement(_writer, "md5", __writer => __writer.WriteString(media.MD5));
                });
        }
    }

    public void WriteWorkgroupFilters(XmlWriter writer, Workgroup workgroup)
    {
        IReadOnlyCollection<ServiceWorkgroupFilter> filters = workgroup.GetLatestWorkgroupFilters();
        int count = filters.Count;
        int i = 0;

        foreach (ServiceWorkgroupFilter filter in filters)
        {
            UpdateProgress(i, count);
            WriteElement(
                writer,
                "filter",
                (_writer) =>
                {
                    _writer.WriteAttributeString("id", filter.Id.ToString());
                    WriteElement(_writer, "name", __writer => __writer.WriteString(filter.Name));
                    WriteElement(_writer, "description", __writer => __writer.WriteString(filter.Description));
                    WriteElement(_writer, "expression", __writer => __writer.WriteString(filter.Expression));
                    WriteElement(_writer, "vectorClock", __writer => __writer.WriteString(filter.FilterClock.ToString()));
                });
        }
    }

    public void WriteWorkgroupVectorClocks(XmlWriter writer, Workgroup workgroup)
    {
        UpdateProgress(1, 1);
        WriteElement(
            writer,
            "workgroupClock",
            _writer => _writer.WriteAttributeString("value", workgroup.BaseVectorClock.ToString()));
    }

#endregion

#region Workgroups

    public void WriteWorkgroupItem(XmlWriter writer, ServiceWorkgroup item)
    {
        writer.WriteAttributeString("id", item.ID.ToString());
        WriteElement(writer, "name", (_writer) => _writer.WriteString(item.Name));
        WriteElement(writer, "serverPath", (_writer) => _writer.WriteString(item.ServerPath));
        WriteElement(writer, "cacheRoot", (_writer) => _writer.WriteString(item.CacheRoot));
    }

    public void WriteWorkgroupItems(XmlWriter writer, List<ServiceWorkgroup> items)
    {
        int count = items.Count;
        int i = 0;

        foreach (ServiceWorkgroup item in items)
        {
            UpdateProgress(i, count);

            WriteElement(writer, "workgroup", (_writer) => WriteWorkgroupItem(_writer, item));
        }
    }

    public void WriteWorkgroups(XmlWriter writer)
    {
        StartBlock("workgroups");

        List<ServiceWorkgroup> workgroups = ServiceInterop.GetAvailableWorkgroups(App.State.ActiveProfile.CatalogID);

        WriteElement(writer, "workgroups", (_writer) => WriteWorkgroupItems(_writer, workgroups));
    }

#endregion

    bool WriteCatalogData()
    {
        return m_exportMediaItems
            || m_exportMediaStacks
            || m_exportVersionStacks
            || m_exportSchema
            || m_exportImports
            || m_exportWorkgroups
            || m_exportDeletedMedia;
    }

    public bool DoBackup(IProgressReport progress)
    {
        m_progressChunks.AddWeightedChunk("Read", 5.0);
        if (m_exportSchema)
            m_progressChunks.AddWeightedChunk("schema", 10.0);
        if (m_exportMediaItems)
            m_progressChunks.AddWeightedChunk("media", 70.0);
        if (m_exportVersionStacks)
            m_progressChunks.AddWeightedChunk("versionStacks", 2.0);
        if (m_exportMediaStacks)
            m_progressChunks.AddWeightedChunk("mediaStacks", 2.0);
        if (m_exportImports)
            m_progressChunks.AddWeightedChunk("imports", 6.0);
        if (m_exportDeletedMedia)
            m_progressChunks.AddWeightedChunk("deletedMedia", 5.0);
        if (m_exportWorkgroups)
            m_progressChunks.AddWeightedChunk("workgroups", 1.0);
        if (m_exportWorkgroupData)
        {
            m_progressChunks.AddWeightedChunk("workgroupClients", 1.0);
            m_progressChunks.AddWeightedChunk("workgroupMedia", 10.0);
            m_progressChunks.AddWeightedChunk("workgroupFilters", 1.0);
            m_progressChunks.AddWeightedChunk("workgroupClocks", 1.0);
        }

        Guid catalogID = App.State.ActiveProfile.CatalogID;
        m_progress = progress;

        StartBlock("Read");

        Task task = m_catalog.ReadFullCatalogFromServer(App.State.ActiveProfile.CatalogID, m_schema);

        task.Wait();

        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Async = true;

        using XmlWriter writer = XmlWriter.Create(m_filename, settings);

        WriteElement(
            writer,
            "fullExport",
            (_writer) =>
            {
                _writer.WriteAttributeString("catalogID", catalogID.ToString());

                if (m_exportSchema)
                    WriteSchema(_writer, m_schema);
                if (WriteCatalogData())
                    WriteCatalog(_writer, m_catalog);
                if (m_exportImports)
                    WriteImports(_writer);
                if (m_exportDeletedMedia)
                    WriteDeletedMedia(_writer, catalogID);
                if (m_exportWorkgroups)
                    WriteWorkgroups(_writer);
                if (m_exportWorkgroupData)
                    WriteWorkgroupData(_writer, catalogID, Guid.Parse(App.State.ActiveProfile.WorkgroupId!));
            });

        m_progress.WorkCompleted();
        return true;
    }
}
