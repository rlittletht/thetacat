﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Xps.Serialization;
using HeyRed.Mime;
using Thetacat.Azure;
using Thetacat.Import.UI;
using Thetacat.Logging;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.UI;
using Thetacat.Util;

namespace Thetacat.Import;

/*----------------------------------------------------------------------------
    %%Class: MediaImporter
    %%Qualified: Thetacat.Import.MediaImporter

    Create this in order to import media into the catalog.

    This object will take a list of files and for each file:
        Create a row in the tcat_import table
        Calculate the MD5 for the file
        Read the metadata from the file
        Create a Local MediaItem:
            Set its state to pending


    If the constructor is given just the source string, then this will
    return the pending-upload items for this source (presumably this client)
----------------------------------------------------------------------------*/
public class MediaImporter
{
    public delegate void NotifyCatalogItemCreatedOrRepairedDelegate(object? source, MediaItem newItem);
    private readonly ObservableCollection<ImportItem> ImportItems = new();
    private readonly HashSet<string> m_ignoreLogs = new();

    #region Constructors

    /*----------------------------------------------------------------------------
        %%Function: MediaImporter.

        Create an importer from a set of IMediaItemFile's (this could include
        richer path information and repair information)
    ----------------------------------------------------------------------------*/
    public MediaImporter(IEnumerable<IMediaItemFile> files, string source, NotifyCatalogItemCreatedOrRepairedDelegate? notifyDelegate)
    {
        AddMediaItemFilesToImporter(files, source, notifyDelegate);
    }

    /*----------------------------------------------------------------------------
        %%Function: MediaImporter
        %%Qualified: Thetacat.Import.MediaImporter.MediaImporter

        Create an import from a list of filenames
    ----------------------------------------------------------------------------*/
    public MediaImporter(IEnumerable<string> files, string source, NotifyCatalogItemCreatedOrRepairedDelegate? notifyDelegate)
    {
        foreach (string file in files)
        {
            PathSegment pathRoot = PathSegment.GetPathRoot(file) ?? PathSegment.Empty;
            PathSegment path = PathSegment.GetRelativePath(pathRoot, file);

            ImportItems.Add(new ImportItem(Guid.Empty, source, pathRoot, path, ImportItem.ImportState.PendingMediaCreate, file, notifyDelegate));
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: MediaImporter
        %%Qualified: Thetacat.Import.MediaImporter.MediaImporter

        This is the interactive version, intended to be attached to an import
        dialog
    ----------------------------------------------------------------------------*/
    public MediaImporter()
    {
    }

    /*----------------------------------------------------------------------------
        %%Function: MediaImporter
        %%Qualified: Thetacat.Import.MediaImporter.MediaImporter

        Create an importer for this client -- it will have all the items that
        are pending upload and owned by this client
    ----------------------------------------------------------------------------*/
    public MediaImporter(string clientSource)
    {
        List<ServiceImportItem> items = ServiceInterop.GetPendingImportsForClient(App.State.ActiveProfile.CatalogID, clientSource);
        bool skippedItems = false;

        foreach (ServiceImportItem item in items)
        {
            if (!App.State.Catalog.TryGetMedia(item.ID, out MediaItem? mediaItem))
            {
                MainWindow.LogForApp(EventType.Error, $"import item {item.ID} not found in catalog");

                skippedItems = true;
                ImportItems.Add(
                    new ImportItem(
                        item.ID,
                        clientSource,
                        PathSegment.CreateFromString(item.SourceServer),
                        PathSegment.CreateFromString(item.SourcePath),
                        ImportItem.ImportState.MissingFromCatalog));
            }
            else
            {
                ImportItem newItem = new ImportItem(
                    item.ID,
                    clientSource,
                    PathSegment.CreateFromString(item.SourceServer),
                    PathSegment.CreateFromString(item.SourcePath),
                    ImportItem.StateFromString(item.State ?? string.Empty));

                newItem.SkipWorkgroupOnlyItem = mediaItem.DontPushToCloud;

                ImportItems.Add(newItem);
            }
        }

        if (skippedItems)
        {
            if (MessageBox.Show(
                    "One or more pending items was missing in the catalog. Do you want to continue and automatically remove these items?",
                    "Thetacat",
                    MessageBoxButton.OKCancel)
                == MessageBoxResult.Cancel)
            {
                throw new CatExceptionCanceled();
            }
        }
    }
    #endregion

    #region Interactive
    /*----------------------------------------------------------------------------
        %%Function: ClearItems
        %%Qualified: Thetacat.Import.MediaImporter.ClearItems

        Clear all the import items
    ----------------------------------------------------------------------------*/
    public void ClearItems()
    {
        ImportItems.Clear();
    }

    /*----------------------------------------------------------------------------
        %%Function: AddMediaItemFilesToImporter
        %%Qualified: Thetacat.Import.MediaImporter.AddMediaItemFilesToImporter

        Add the set of files to the importer. Interactive version
    ----------------------------------------------------------------------------*/
    public void AddMediaItemFilesToImporter(
        IEnumerable<IMediaItemFile> files, 
        string source, 
        NotifyCatalogItemCreatedOrRepairedDelegate? notifyDelegate)
    {
        foreach (IMediaItemFile file in files)
        {
            PathSegment? pathRoot = PathSegment.CreateFromString(Path.GetPathRoot(file.FullyQualifiedPath)) ?? PathSegment.Empty;
            PathSegment path = PathSegment.GetRelativePath(pathRoot, file.FullyQualifiedPath);

            ImportItem newItem =
                file.VirtualPath != null
                    ? new ImportItem(Guid.Empty, source, pathRoot, path, file.VirtualPath, ImportItem.ImportState.PendingMediaCreate, file, notifyDelegate)
                    : new ImportItem(Guid.Empty, source, pathRoot, path, ImportItem.ImportState.PendingMediaCreate, file, notifyDelegate);

            // check to see if this is a repair item
            if (file.NeedsRepair && file.ExistingID != null)
            {
                newItem.ID = file.ExistingID.Value;
                newItem.State = ImportItem.ImportState.PendingRepair;
            }

            ImportItems.Add(newItem);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: LaunchImporter
        %%Qualified: Thetacat.Import.MediaImporter.LaunchImporter
    ----------------------------------------------------------------------------*/
    public static void LaunchImporter(Window parentWindow)
    {
        MediaImporter importer = new MediaImporter();
        MediaImport import = new(importer);
        import.Owner = parentWindow;
        import.ShowDialog();
    }
    #endregion

    #region Import Media
    /*----------------------------------------------------------------------------
        %%Function: PrePopulateCacheForItem
        %%Qualified: Thetacat.Import.MediaImporter.PrePopulateCacheForItem

        Do any core prepopulation work on import. Since we are the client
        doing the importing, we have the media already cached locally. we can
        populate the workgroup cache saving a download.
    ----------------------------------------------------------------------------*/
    void PrePopulateCacheForItem(ImportItem item, MediaItem mediaItem)
    {
        // here we can pre-populate our cache.
        App.State.Cache.PrimeCacheFromImport(mediaItem, PathSegment.Join(item.SourceServer, item.SourcePath));
        mediaItem.NotifyCacheStatusChanged();
    }


    /*----------------------------------------------------------------------------
        %%Function: FTryPopulateMediaTagsForImport
        %%Qualified: Thetacat.Import.MediaImporter.FTryPopulateMediaTagsForImport

        try to populate the media tags for this media. If we fail to access the
        media, then return false so we know we can't import this item.
    ----------------------------------------------------------------------------*/
    bool FTryPopulateMediaTagsForImport(ImportItem item, MediaItem mediaItem, MetatagSchema metatagSchema, string localPath)
    {
        try
        {
            List<string>? log = mediaItem.SetMediaTagsFromFileMetadata(metatagSchema, localPath);

            if (log != null && log.Count != 0)
            {
                string joined = string.Join(", ", log);

                if (!m_ignoreLogs.Contains(joined))
                {
                    if (MessageBox.Show($"Found tag differences: {joined}. Ignore future logs like this?", "Ignore Logs", MessageBoxButton.YesNo)
                        == MessageBoxResult.Yes)
                    {
                        m_ignoreLogs.Add(joined);
                    }
                }
            }
        }

        catch (Exception)
        {
            MessageBox.Show($"Failed to read metadata for item: {item.VirtualPath}. Skipping");
            return false;
        }

        return true;
    }

    /*----------------------------------------------------------------------------
        %%Function: CreateCatalogAndUpdateImportTableWork
        %%Qualified: Thetacat.Import.MediaImporter.CreateCatalogAndUpdateImportTableWork

        The core work of importing and prepopulating the workgroup cache.

        For each item, try to add it to the catalog (if not repairing), populate
        its media tags, prepopulate the cache.

        When done, update the metatag schema, commit the catalog, and update the
        imports table

        BE VERY CAREFUL - check for exceptions carefully to ensure we don't leave
        things in an incoherent state.
    ----------------------------------------------------------------------------*/
    private void CreateCatalogAndUpdateImportTableWork(
        Guid catalogID, 
        IProgressReport report, 
        ICatalog catalog, 
        MetatagSchema metatagSchema)
    {
        m_ignoreLogs.Clear();
        int total = ImportItems.Count;
        int current = 0;
        List<Guid> repairedItems = new();
        List<Guid> skippedItems = new();

        // before we do this, make sure we have the latest metatag schema (so we don't try to create metatags
        // that are already there
        metatagSchema.ReplaceFromService(catalogID);

        foreach (ImportItem item in ImportItems)
        {
            report.UpdateProgress((current * 100.0) / total);

            try
            {
                MediaItem? mediaItem;

                string localPath = PathSegment.Join(item.SourceServer, item.SourcePath).Local;

                if (item.State == ImportItem.ImportState.PendingRepair)
                {
                    repairedItems.Add(item.ID);
                    if (!catalog.TryGetMedia(item.ID, out mediaItem))
                    {
                        MessageBox.Show("Could not find item for repair in catalog");
                        continue;
                    }
                }
                else
                {
                    // create a new MediaItem for this item
                    mediaItem = new MediaItem(item);

                    mediaItem.MimeType = MimeTypesMap.GetMimeType(localPath);
                }

                bool fSkip = false;

                fSkip = !FTryPopulateMediaTagsForImport(item, mediaItem, metatagSchema, localPath);
                if (fSkip)
                    skippedItems.Add(item.ID);

                if (!fSkip)
                {
                    if (item.State != ImportItem.ImportState.PendingRepair)
                        catalog.AddNewMediaItem(mediaItem);

                    // even if we are repairing, notify. they will get the item, so they can
                    // query if PendingRepair
                    item.NotifyMediaItemCreated(mediaItem);
                    item.ID = mediaItem.ID;

                    // go ahead and mark pending upload -- we're going to create the item in the
                    // catalog before we insert the import items...
                    item.State = ImportItem.ImportState.PendingUpload;

                    // and handle prepopulating the cache since we have the media locally
                    PrePopulateCacheForItem(item, mediaItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Caught exception trying to add media item {item.VirtualPath}. Skipping. {ex.Message}");
            }

            current++;
        }

        // also flush any pending schema changes now
        metatagSchema.UpdateServer(catalogID);

        // at this point, we have an ID created for the media. Go ahead and insert the
        // new media items and commit the import to the database
        catalog.PushPendingChanges(catalogID);

        // and update the imports table (but don't insert anything we skipped, or any repair items that already
        // exist in the imports table)
        if (repairedItems.Count > 0 || skippedItems.Count > 0)
        {
            HashSet<Guid> skipIds = new(skippedItems);

            if (repairedItems.Count > 0)
            {
                // we might have already marked items as imported, so check first
                List<ServiceImportItem> existingImports = ServiceInterop.QueryImportedItems(catalogID, repairedItems);

                foreach (ServiceImportItem importedItem in existingImports)
                {
                    skipIds.Add(importedItem.ID);
                }
            }

            List<ImportItem> newImports = new();
            foreach (ImportItem item in ImportItems)
            {
                if (!skipIds.Contains(item.ID))
                    newImports.Add(item);
            }

            ServiceInterop.InsertImportItems(catalogID, newImports);
        }
        else
        {
            ServiceInterop.InsertImportItems(catalogID, ImportItems);
        }

        report.WorkCompleted();
    }

    /*----------------------------------------------------------------------------
        %%Function: CreateCatalogItemsAndUpdateImportTable
        %%Qualified: Thetacat.Import.MediaImporter.CreateCatalogItemsAndUpdateImportTable

        Do the actual import on a background thread with progress
    ----------------------------------------------------------------------------*/
    public void CreateCatalogItemsAndUpdateImportTable(Guid catalogID, ICatalog catalog, MetatagSchema metatagSchema)
    {
        ProgressDialog.DoWorkWithProgress(
            (report) => CreateCatalogAndUpdateImportTableWork(catalogID, report, catalog, metatagSchema));
    }
    #endregion

    #region Upload Media
    /*----------------------------------------------------------------------------
        %%Function: UploadPendingMediaWork
        %%Qualified: Thetacat.Import.MediaImporter.UploadPendingMediaWork

        This will upload the items we have locally to the cloud.
    ----------------------------------------------------------------------------*/
    private bool UploadPendingMediaWork(IProgressReport progress)
    {
        try
        {
            int i = 0, iMax = ImportItems.Count;

            foreach (ImportItem item in ImportItems)
            {
                i++;
                progress.UpdateProgress((i * 100.0) / iMax);

                if (item.State == ImportItem.ImportState.PendingUpload
                    && !item.SourcePath.Local.EndsWith("MOV")
                    && !item.SkipWorkgroupOnlyItem)
                {
                    PathSegment path = PathSegment.Join(item.SourceServer, item.SourcePath);
                    Task<TcBlob> task = AzureCat._Instance.UploadMedia(item.ID.ToString(), path.Local);

                    task.Wait();

                    if (task.IsCanceled || task.IsFaulted)
                    {
                        MainWindow.LogForAsync(EventType.Warning, "Task was cancelled in UploadPendingMediaWork. Aborting upload");
                        return false;
                    }

                    TcBlob blob = task.Result;
                    MediaItem media = App.State.Catalog.GetMediaFromId(item.ID);

                    if (media.MD5 != blob.ContentMd5)
                    {
                        MessageBox.Show($"Strange. MD5 was wrong for {path}: was {media.MD5} but blob calculated {blob.ContentMd5}");
                        media.MD5 = blob.ContentMd5;
                    }

                    item.State = ImportItem.ImportState.Complete;
                    media.State = MediaItemState.Active;
                    item.UploadDate = DateTime.Now;

                    ServiceInterop.CompleteImportForItem(App.State.ActiveProfile.CatalogID, item.ID);
                    MainWindow.LogForAsync(EventType.Information, $"uploaded item {item.ID} ({item.SourcePath}");
                }

                if (item.State == ImportItem.ImportState.MissingFromCatalog)
                {
                    ServiceInterop.DeleteImportItem(App.State.ActiveProfile.CatalogID, item.ID);
                    MainWindow.LogForAsync(EventType.Information, $"removed missing catalog item {item.ID} ({item.SourcePath}");
                }
            }
        }
        finally
        {
            progress.WorkCompleted();
        }

        return true;
    }

    /*----------------------------------------------------------------------------
        %%Function: UploadMedia
        %%Qualified: Thetacat.Import.MediaImporter.UploadMedia

        No benefit to doing multiple of these in parallel -- the upload bandwidth
        is likely the limiting factor. But do do this in the background to remain
        responsive.
    ----------------------------------------------------------------------------*/
    public void UploadMedia()
    {
        AzureCat.EnsureCreated(App.State.AzureStorageAccount);

        App.State.AddBackgroundWork("Uploading pending media", UploadPendingMediaWork);
    }
    #endregion
}
