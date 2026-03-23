using System;
using System.Collections.Generic;
using System.Xml;
using Thetacat.ServiceClient;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class DeletedMediaRestore
{
    public int WorkgroupDeletedMediaClock = 0;
    public List<ServiceDeletedItem> DeletedItems = new();

    public DeletedMediaRestore()
    {
    }

    public DeletedMediaRestore(XmlReader reader)
    {
        XmlIO.FReadElement(reader, this, "deletedMedia", FReadDeletedMediaAttributes, FReadDeletedMediaElements);
    }

    static bool FReadDeletedMediaAttributes(string attribute, string value, DeletedMediaRestore restore)
    {
        if (attribute == "workgroupDeletedMediaClock")
        {
            restore.WorkgroupDeletedMediaClock = int.Parse(value);
            return true;
        }

        throw new XmlException($"Unknown attribute {attribute} in deletedMedia element");
    }

    static bool FReadDeletedMediaElements(XmlReader reader, string element, DeletedMediaRestore restore)
    {
        if (element == "deletedMediaItem")
        {
            ServiceDeletedItem item = new();

            XmlIO.FReadElement(reader, item, element, FReadDeletedItemAttributes, null);
            restore.DeletedItems.Add(item);
            return true;
        }

        throw new XmlException($"Unknown element {element} in deletedMedia element");
    }

    static bool FReadDeletedItemAttributes(string attribute, string value, ServiceDeletedItem item)
    {
        if (attribute == "id")
        {
            item.Id = Guid.Parse(value);
            return true;
        }
        if (attribute == "minWorkgroupClock")
        {
            item.MinVectorClock = int.Parse(value);
            return true;
        }
        throw new XmlException($"Unknown attribute {attribute} in deletedMediaItem element");
    }
}

