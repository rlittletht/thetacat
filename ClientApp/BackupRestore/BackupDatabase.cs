using MetadataExtractor.Formats.Xmp;
using System;
using System.Collections.Generic;
using System.Security.RightsManagement;
using System.Threading.Tasks;
using System.Xml;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.UI;

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

    public static string s_uri = "https://schemas.thetasoft.com/thetacat/backup/2024";

    public BackupDatabase(
        string backupPath,
        bool exportMediaItems,
        bool exportMediaStacks,
        bool exportVersionStacks,
        bool exportSchema,
        bool exportImports,
        bool exportWorkgroups)
    {
        m_schema = new MetatagSchema();
        m_catalog = new Catalog();
        m_filename = backupPath;
        m_exportMediaItems = exportMediaItems;
        m_exportMediaStacks = exportMediaStacks;
        m_exportVersionStacks = exportVersionStacks;
        m_exportSchema = exportSchema;
        m_exportImports = exportImports;
        m_exportWorkgroups = exportWorkgroups;
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
        WriteElement(writer, "standard", (_writer)=>_writer.WriteString(metatag.Standard));
        if (metatag.Parent != null)
            WriteElement(writer, "parentId", (_writer)=>_writer.WriteString(metatag.Parent.ToString()));
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
        StartNextBlock(15.0);

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
            writer.WriteString(mediaTag.Value);
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

        WriteElement(writer, "mediaTags", (_writer) => WriteMediaTags(_writer, mediaItem.Tags.Values));
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
                StartNextBlock(85.0);
                if (m_exportMediaItems)
                    WriteElement(_writer, "media", (__writer) => WriteMediaItems(__writer, catalog));
                StartNextBlock(87.0);
                if (m_exportVersionStacks)
                    WriteElement(_writer, "versionStacks", (__writer) => WriteMediaStacks(__writer, catalog.VersionStacks));
                StartNextBlock(89.0);
                if (m_exportMediaStacks)
                    WriteElement(_writer, "mediaStacks", (__writer) => WriteMediaStacks(__writer, catalog.MediaStacks));
            });
    }


    /*----------------------------------------------------------------------------
        %%Function: WriteImportItem
        %%Qualified: Thetacat.Export.BackupDatabase.WriteImportItem
    ----------------------------------------------------------------------------*/
    public void WriteImportItem(XmlWriter writer, ServiceImportItem item)
    {
        writer.WriteAttributeString("mediaId", item.ID.ToString());
        WriteElement(writer, "state", (_writer) => _writer.WriteString(item.State));
        WriteElement(writer, "sourcePath", (_writer)=>_writer.WriteString(item.SourcePath));
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

        foreach(ServiceImportItem item in items)
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
        StartNextBlock(99.0);
        List<ServiceImportItem> importItems = ServiceInterop.GetAllImports();

        WriteElement(writer, "imports", (_writer) => WriteImportItems(_writer, importItems));
    }

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
        StartNextBlock(100.0);
        List<ServiceWorkgroup> workgroups = ServiceInterop.GetAvailableWorkgroups();

        WriteElement(writer, "workgroups", (_writer) => WriteWorkgroupItems(_writer, workgroups));
    }
    public bool DoBackup(IProgressReport progress)
    {
        m_progress = progress;

        StartNextBlock(5.0);
        Task task = m_catalog.ReadFullCatalogFromServer(m_schema);
        
        task.Wait();

        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Async = true;

        using XmlWriter writer = XmlWriter.Create(m_filename, settings);

        WriteElement(
            writer,
            "fullExport",
            (_writer) =>
            {
                if (m_progress != null)
                    WriteSchema(_writer, m_schema);
                WriteCatalog(_writer, m_catalog);
                if (m_exportImports)
                    WriteImports(_writer);
                if (m_exportWorkgroups)
                    WriteWorkgroups(_writer);
            });

        m_progress.WorkCompleted();
        return true;
    }
}
