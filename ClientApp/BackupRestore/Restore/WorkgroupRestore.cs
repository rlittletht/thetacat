using System;
using System.Xml;
using TCore.XmlSettings;
using Thetacat.ServiceClient;
using Thetacat.Util;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class WorkgroupRestore
{
    public ServiceWorkgroup ItemData = new();

    static string ParseCollectText(XmlReader reader, string element, WorkgroupRestore itemRestore)
    {
        XmlIO.ContentCollector collector = new XmlIO.ContentCollector();
        if (!XmlIO.FReadElement(reader, itemRestore, element, null, null, collector))
            return "";

        return collector.ToString();
    }

    static bool FParseElement(XmlReader reader, string element, WorkgroupRestore itemRestore)
    {
        switch (element)
        {
            case "name":
                itemRestore.ItemData.Name = ParseCollectText(reader, element, itemRestore);
                return true;
            case "serverPath":
                itemRestore.ItemData.ServerPath = ParseCollectText(reader, element, itemRestore);
                return true;
            case "cacheRoot":
                itemRestore.ItemData.CacheRoot = ParseCollectText(reader, element, itemRestore);
                return true;
        }

        throw new XmlioExceptionSchemaFailure($"unknown element {element}");
    }

    public static bool FParseAttribute(string attribute, string value, WorkgroupRestore itemRestore)
    {
        if (attribute == "id")
        {
            if (Guid.TryParse(value, out Guid id))
            {
                itemRestore.ItemData.ID = id;
                return true;
            }
        }

        throw new XmlioExceptionSchemaFailure($"unknown attribute {attribute}: {value}");
    }

    public WorkgroupRestore(XmlReader reader)
    {
        XmlIO.FReadElement(reader, this, "workgroup", FParseAttribute, FParseElement);
    }
}
