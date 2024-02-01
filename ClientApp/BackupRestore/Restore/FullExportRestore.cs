using System.Windows.Documents;
using System.Xml;
using System.Xml.Schema;

namespace Thetacat.BackupRestore.Restore;

public class FullExportRestore
{
    public MetatagSchemaRestore? SchemaRestore;
    public CatalogRestore? CatalogRestore;
    public ImportsRestore? ImportsRestore;

    static bool FParseFullExport(XmlReader reader, string element, FullExportRestore fullExport)
    {
        if (element == "metatagSchema")
        {
            fullExport.SchemaRestore = new MetatagSchemaRestore(reader);
            return true;
        }

        if (element == "catalog")
        {
            fullExport.CatalogRestore = new CatalogRestore(reader, fullExport.SchemaRestore!.Schema);
            return true;
        }

        if (element == "imports")
        {
            fullExport.ImportsRestore = new ImportsRestore(reader);
            return true;
        }
        throw new XmlSchemaException($"unknown element {element}");
    }

    public FullExportRestore(XmlReader reader)
    {
        XMLIO.XmlIO.FReadElement(reader, this, "fullExport", null, FParseFullExport);
    }
}
