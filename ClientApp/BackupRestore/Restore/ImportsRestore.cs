using System.Collections.Generic;
using System.Xml;
using Thetacat.ServiceClient;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class ImportsRestore
{
    private List<ServiceImportItem> ImportItems = new();

    static bool FParseImportsElement(XmlReader reader, string element, ImportsRestore importsRestore)
    {
        if (element != "importItem")
            throw new XmlioExceptionSchemaFailure($"unknown element {element}");

        ImportItemRestore itemRestore = new ImportItemRestore(reader);

        importsRestore.ImportItems.Add(itemRestore.ItemData);

        return true;
    }

    public ImportsRestore(XmlReader reader)
    {
        XmlIO.FReadElement(reader, this, "imports", null, FParseImportsElement);
    }
}
