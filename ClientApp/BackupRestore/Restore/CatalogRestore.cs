using System.Xml;
using MetadataExtractor.Formats.Xmp;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.Model.Mediatags;
using Thetacat.ServiceClient;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class CatalogRestore
{
    public MetatagSchema Schema;
    public Catalog Catalog;

    static bool FParseMediaElement(XmlReader reader, string element, CatalogRestore catalogRestore)
    {
        if (element == "mediaItem")
        {
            MediaItemRestore itemRestore = new MediaItemRestore(reader, catalogRestore.Schema);

            MediaItem mediaItem = new MediaItem(itemRestore.ItemData);

            foreach (MediaTag tag in itemRestore.MediaTags)
            {
                mediaItem.FAddOrUpdateMediaTag(tag, true);
            }

            catalogRestore.Catalog.AddNewMediaItem(mediaItem);
            return true;
        }

        return false;
    }

    static bool FParseCatalogElement(XmlReader reader, string element, CatalogRestore catalogRestore)
    {
        switch (element)
        {
            case "media":
                return XmlIO.FReadElement(reader, catalogRestore, "media", null, FParseMediaElement);
            case "versionStacks":
            {
                StacksRestore stacksRestore = new StacksRestore(reader, "versionStacks", MediaStackType.Version);

                foreach (ServiceStack stack in stacksRestore.Stacks)
                {
                    catalogRestore.Catalog.VersionStacks.AddStack(new MediaStack(stack));
                }

                return true;
            }
            case "mediaStacks":
            {
                StacksRestore stacksRestore = new StacksRestore(reader, "mediaStacks", MediaStackType.Media);
                foreach (ServiceStack stack in stacksRestore.Stacks)
                {
                    catalogRestore.Catalog.MediaStacks.AddStack(new MediaStack(stack));
                }

                return true;
            }
        }

        return false;
    }

    public CatalogRestore(XmlReader reader, MetatagSchema schema)
    {
        Schema = schema;
        Catalog = new Catalog();

        XmlIO.FReadElement(reader, this, "catalog", null, FParseCatalogElement);
    }
}
