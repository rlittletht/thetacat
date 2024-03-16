using System;
using System.Windows.Documents;
using System.Xml;
using System.Xml.Schema;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class FullExportRestore
{
    public MetatagSchemaRestore? SchemaRestore;
    public CatalogRestore? CatalogRestore;
    public ImportsRestore? ImportsRestore;
    public WorkgroupsRestore? WorkgroupsRestore;
    public Guid? CatalogID;

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

        if (element == "workgroups")
        {
            fullExport.WorkgroupsRestore = new WorkgroupsRestore(reader);
            return true;
        }

        throw new XmlSchemaException($"unknown element {element}");
    }

    public FullExportRestore(XmlReader reader)
    {
        XMLIO.XmlIO.FReadElement(reader, this, "fullExport", FParseFullExportAttribute, FParseFullExport);
    }

    private bool FParseFullExportAttribute(string attribute, string value, FullExportRestore fullExport)
    {
        if (attribute == "catalogId")
        {
            if (Guid.TryParse(value, out Guid id))
            {
                fullExport.CatalogID = id;
                return true;
            }
        }

        throw new XmlioExceptionSchemaFailure($"unknown attribute {attribute}: {value}");
    }
}
