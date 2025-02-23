using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Xml;
using Thetacat.Model.Workgroups;
using Thetacat.ServiceClient;
using Thetacat.Types;
using Thetacat.Util;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class WorkgroupMediaRestore
{
    public List<WorkgroupCacheEntryData> MediaItems = new();

    public WorkgroupMediaRestore(XmlReader reader)
    {
        XmlIO.FReadElement(reader, this, "media", null, FReadWorkgroupMediaElements);
    }

    static bool FReadWorkgroupMediaElements(XmlReader reader, string element, WorkgroupMediaRestore restore)
    {
        if (element == "mediaItem")
        {
            WorkgroupCacheEntryData item = new();

            if (!XmlIO.FReadElement(reader, item, "mediaItem", FReadMediaItemAttributes, FReadMediaItemElements))
                return false;

            restore.MediaItems.Add(item);

            return true;
        }
        return false;
    }

    static bool FReadMediaItemAttributes(string attribute, string value, WorkgroupCacheEntryData item)
    {
        if (attribute == "id")
        {
            item.ID = Guid.Parse(value);
            return true;
        }

        throw new Exception($"Unknown attribute {attribute} in mediaItem");
    }

    static string ParseCollectText(XmlReader reader, WorkgroupCacheEntryData client, string element)
    {
        XmlIO.ContentCollector collector = new XmlIO.ContentCollector();
        if (!XmlIO.FReadElement(reader, client, element, null, null, collector))
            return "";

        return collector.ToString();
    }

    static bool FReadMediaItemElements(XmlReader reader, string element, WorkgroupCacheEntryData item)
    {
        if (element == "path")
        {
            item.Path = new PathSegment(ParseCollectText(reader, item, element));
            return true;
        }
        if (element == "cacheBy")
        {
            item.CachedBy = Guid.Parse(ParseCollectText(reader, item, element));
            return true;
        }
        if (element == "cachedDate")
        {
            item.CacheDate = DateTime.Parse(ParseCollectText(reader, item, element));
            return true;
        }
        if (element == "md5")
        {
            item.MD5 = ParseCollectText(reader, item, element);
            return true;
        }
        throw new Exception($"Unknown element {element} in mediaItem");
    }
}
