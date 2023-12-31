﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using HeyRed.Mime;
using Thetacat.Azure;
using Thetacat.Logging;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Import;

/*----------------------------------------------------------------------------
    %%Class: MediaImport
    %%Qualified: Thetacat.Import.MediaImport

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
public class MediaImport
{
    public delegate void NotifyCatalogItemCreatedDelegate(object? source, MediaItem newItem);
    private readonly ObservableCollection<ImportItem> ImportItems = new();

    public MediaImport(IEnumerable<IMediaItemFile> files, string source, NotifyCatalogItemCreatedDelegate? notifyDelegate)
    {
        foreach (IMediaItemFile file in files)
        {
            PathSegment? pathRoot = PathSegment.CreateFromString(Path.GetPathRoot(file.FullyQualifiedPath)) ?? PathSegment.Empty;
            PathSegment path = PathSegment.GetRelativePath(pathRoot, file.FullyQualifiedPath);

            ImportItems.Add(new ImportItem(Guid.Empty, source, pathRoot, path, ImportItem.ImportState.PendingMediaCreate, file, notifyDelegate));
        }
    }

    public MediaImport(IEnumerable<string> files, string source, NotifyCatalogItemCreatedDelegate? notifyDelegate)
    {
        foreach (string file in files)
        {
            PathSegment pathRoot = PathSegment.GetPathRoot(file) ?? PathSegment.Empty;
            PathSegment path = PathSegment.GetRelativePath(pathRoot, file);

            ImportItems.Add(new ImportItem(Guid.Empty, source, pathRoot, path, ImportItem.ImportState.PendingMediaCreate, file, notifyDelegate));
        }
    }

    public MediaImport(string source)
    {
        List<ServiceImportItem> items = ServiceInterop.GetPendingImportsForClient(source);
        bool skippedItems = false;

        foreach (ServiceImportItem item in items)
        {
            if (!MainWindow._AppState.Catalog.HasMediaItem(item.ID))
            {
                MainWindow.LogForApp(EventType.Error, $"import item {item.ID} not found in catalog");

                skippedItems = true;
                ImportItems.Add(
                    new ImportItem(
                        item.ID,
                        source,
                        PathSegment.CreateFromString(item.SourceServer),
                        PathSegment.CreateFromString(item.SourcePath),
                        ImportItem.ImportState.MissingFromCatalog));
            }
            else
            {
                ImportItems.Add(
                    new ImportItem(
                        item.ID,
                        source,
                        PathSegment.CreateFromString(item.SourceServer),
                        PathSegment.CreateFromString(item.SourcePath),
                        ImportItem.StateFromString(item.State ?? string.Empty)));
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

    public void CreateCatalogItemsAndUpdateImportTable(ICatalog catalog, MetatagSchema metatagSchema)
    {
        foreach (ImportItem item in ImportItems)
        {
            // create a new MediaItem for this item
            MediaItem mediaItem = new(item);

            catalog.AddNewMediaItem(mediaItem);
            item.NotifyMediaItemCreated(mediaItem);
            mediaItem.LocalPath = PathSegment.Join(item.SourceServer, item.SourcePath).Local;
            mediaItem.MimeType = MimeTypesMap.GetMimeType(mediaItem.LocalPath);

            List<string>? log = mediaItem.SetMediaTagsFromFileMetadata(metatagSchema);

            if (log != null && log.Count != 0)
                MessageBox.Show($"Found tag differences: {string.Join(", ", log)}");

            item.ID = mediaItem.ID;


            // go ahead and mark pending upload -- we're going to create the item in the
            // catalog before we insert the import items...
            item.State = ImportItem.ImportState.PendingUpload;
        }

        metatagSchema.UpdateServer();

        // at this point, we have an ID created for the media. Go ahead and insert the
        // new media items and commit the import to the database
        catalog.PushPendingChanges();
        // also flush any pending schema changes now

        ServiceInterop.InsertImportItems(ImportItems);
    }

    public async Task UploadMedia()
    {
        AzureCat.EnsureCreated(MainWindow._AppState.AzureStorageAccount);

        foreach (ImportItem item in ImportItems)
        {
            if (item.State == ImportItem.ImportState.PendingUpload)
            {
                PathSegment path = PathSegment.Join(item.SourceServer, item.SourcePath);
                
                TcBlob blob = await AzureCat._Instance.UploadMedia(item.ID.ToString(), path.Local);
                MediaItem media = MainWindow._AppState.Catalog.Media.Items[item.ID];

                if (media.MD5 != blob.ContentMd5)
                {
                    MessageBox.Show($"Strange. MD5 was wrong for {path}: was {media.MD5} but blob calculated {blob.ContentMd5}");
                    media.MD5 = blob.ContentMd5;
                }

                item.State = ImportItem.ImportState.Complete;
                media.State = MediaItemState.Active;
                item.UploadDate = DateTime.Now;

                ServiceInterop.CompleteImportForItem(item.ID);
                MainWindow.LogForAsync(EventType.Information, $"uploaded item {item.ID} ({item.SourcePath}");
            }

            if (item.State == ImportItem.ImportState.MissingFromCatalog)
            {
                ServiceInterop.DeleteImportItem(item.ID);
                MainWindow.LogForAsync(EventType.Information, $"removed missing catalog item {item.ID} ({item.SourcePath}");
            }
        }
    }
}
