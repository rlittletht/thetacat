using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Thetacat.Model;
using Thetacat.Model.Metatags;
using Thetacat.ServiceClient;
using Thetacat.Types;

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
        
     
----------------------------------------------------------------------------*/
public class MediaImport
{
    private readonly ObservableCollection<ImportItem> ImportItems = new();

    public MediaImport(IEnumerable<IMediaItemFile> files, string source)
    {
        foreach (IMediaItemFile file in files)
        {
            string pathRoot = Path.GetPathRoot(file.FullyQualifiedPath)?.Replace("\\", "/") ?? string.Empty;
            string path = Path.GetRelativePath(pathRoot, file.FullyQualifiedPath).Replace("\\", "/");

            ImportItems.Add(new ImportItem(Guid.Empty, source, pathRoot, path));
        }
    }

    public MediaImport(IEnumerable<string> files, string source)
    {
        foreach (string file in files)
        {
            string pathRoot = Path.GetPathRoot(file) ?? string.Empty;
            string path = Path.GetRelativePath(pathRoot, file);

            ImportItems.Add(new ImportItem(Guid.Empty, source, pathRoot, path));
        }
    }

    public void CreateCatalogItemsAndUpdateImportTable(Catalog catalog, MetatagSchema metatagSchema)
    {
        foreach (ImportItem item in ImportItems)
        {
            // create a new MediaItem for this item
            MediaItem mediaItem = new(item);

            catalog.AddNewMediaItem(mediaItem);
            mediaItem.LocalPath = Path.Combine(item.SourceServer, item.SourcePath);

            List<string>? log = mediaItem.ReadMetadataFromFile(metatagSchema);

            if (log != null && log.Count != 0)
                MessageBox.Show($"Found tag differences: {string.Join(", ", log)}");

            item.ID = mediaItem.ID;


            // go ahead and mark pending upload -- we're going to create the item in the
            // catalog before we insert the import items...
            item.State = ImportItem.ImportState.PendingUpload;
        }

        // at this point, we have an ID created for the media. Go ahead and insert the
        // new media items and commit the import to the database
        catalog.FlushPendingCreates();
        ServiceInterop.InsertImportItems(ImportItems);
    }

}
