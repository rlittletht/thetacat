using System;
using System.Collections.Generic;
using System.Xml;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class MediaItemRestore
{
    public MetatagSchema Schema;
    public ServiceMediaItem ItemData;
    public List<MediaTag> MediaTags = new();

    static string ParseCollectText(XmlReader reader, string element, MediaItemRestore itemRestore)
    {
        XmlIO.ContentCollector collector = new XmlIO.ContentCollector();
        if (!XmlIO.FReadElement(reader, itemRestore, element, null, null, collector))
            return "";

        return collector.ToString();
    }

    static bool FParseMediaTags(XmlReader reader, string element, MediaItemRestore itemRestore)
    {
        if (element == "tag")
        {
            MediaTagRestore tagRestore = new MediaTagRestore(reader);
            itemRestore.MediaTags.Add(MediaTag.CreateMediaTag(itemRestore.Schema, tagRestore.MetatagID, tagRestore.Value));

            return true;
        }

        return false;
    }

    static bool FParseElement(XmlReader reader, string element, MediaItemRestore itemRestore)
    {
        switch (element)
        {
            case "md5":
                itemRestore.ItemData.MD5 = ParseCollectText(reader, element, itemRestore);
                return true;
            case "virtualPath":
                itemRestore.ItemData.VirtualPath = new PathSegment(ParseCollectText(reader, element, itemRestore));
                return true;
            case "mimeType":
                itemRestore.ItemData.MimeType = ParseCollectText(reader, element, itemRestore);
                return true;
            case "state":
                itemRestore.ItemData.State = ParseCollectText(reader, element, itemRestore);
                return true;
            case "mediaTags":
                return XmlIO.FReadElement(reader, itemRestore, "mediaTags", null, FParseMediaTags);
        }

        return false;
    }

    public static bool FParseAttribute(string attribute, string value, MediaItemRestore itemRestore)
    {
        if (attribute == "id")
        {
            if (Guid.TryParse(value, out Guid id))
            {
                itemRestore.ItemData.Id = id;
                return true;
            }
        }

        return false;
    }

    public MediaItemRestore(XmlReader reader, MetatagSchema schema)
    {
        ItemData = new ServiceMediaItem();

        Schema = schema;
        XmlIO.FReadElement(reader, this, "mediaItem", FParseAttribute, FParseElement);
    }
}
