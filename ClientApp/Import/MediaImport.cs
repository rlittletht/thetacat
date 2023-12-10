using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Thetacat.Model;
using Thetacat.ServiceClient;

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

    public MediaImport(IEnumerable<string> files, string source)
    {
        foreach (string file in files)
        {
            string pathRoot = Path.GetPathRoot(file) ?? string.Empty;
            string path = Path.GetRelativePath(pathRoot, file);

            ImportItems.Add(new ImportItem(Guid.Empty, source, pathRoot, path));
        }
    }

    public void CreateCatalogItemsAndUpdateImportTable(Catalog catalog)
    {
        foreach (ImportItem item in ImportItems)
        {
            // create a new MediaItem for this item
            MediaItem mediaItem = new(item);

            catalog.AddNewMediaItem(mediaItem);
            item.ID = mediaItem.ID;
        }

        // at this point, we have an ID created for the media. Go ahead and commit
        // the import to the database
        ServiceInterop.InsertImportItems(ImportItems);
    }
}
