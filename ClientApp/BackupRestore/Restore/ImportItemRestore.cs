using System;
using System.Xml;
using TCore.XmlSettings;
using Thetacat.ServiceClient;
using Thetacat.Util;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class ImportItemRestore
{
    public ServiceImportItem ItemData = new();

    static string ParseCollectText(XmlReader reader, string element, ImportItemRestore itemRestore)
    {
        XmlIO.ContentCollector collector = new XmlIO.ContentCollector();
        if (!XmlIO.FReadElement(reader, itemRestore, element, null, null, collector))
            return "";

        return collector.ToString();
    }

    static bool FParseElement(XmlReader reader, string element, ImportItemRestore itemRestore)
    {
        switch (element)
        {
            case "state":
                itemRestore.ItemData.State = ParseCollectText(reader, element, itemRestore);
                return true;
            case "sourcePath":
                itemRestore.ItemData.SourcePath = ParseCollectText(reader, element, itemRestore);
                return true;
            case "sourceServer":
                itemRestore.ItemData.SourceServer = ParseCollectText(reader, element, itemRestore);
                return true;
            case "source":
                itemRestore.ItemData.Source = ParseCollectText(reader, element, itemRestore);
                return true;
            case "UploadDate":
                itemRestore.ItemData.UploadDate = DateTime.Parse(ParseCollectText(reader, element, itemRestore));
                return true;
        }

        throw new XmlioExceptionSchemaFailure($"unknown element {element}");
    }

    public static bool FParseAttribute(string attribute, string value, ImportItemRestore itemRestore)
    {
        if (attribute == "mediaId")
        {
            if (Guid.TryParse(value, out Guid id))
            {
                itemRestore.ItemData.ID = id;
                return true;
            }
        }

        throw new XmlioExceptionSchemaFailure($"unknown attribute {attribute}: {value}");
    }

    public ImportItemRestore(XmlReader reader)
    {
        XmlIO.FReadElement(reader, this, "importItem", FParseAttribute, FParseElement);
    }
}
