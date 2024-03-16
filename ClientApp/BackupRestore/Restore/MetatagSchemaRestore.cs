using System.Xml;
using Thetacat.Metatags.Model;
using Thetacat.Standards;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class MetatagSchemaRestore
{
    public MetatagSchema Schema;

    static bool FParseElement(XmlReader reader, string element, MetatagSchemaRestore schemaRestore)
    {
        if (element == "metatag")
        {
            MetatagRestore metatag = new MetatagRestore(reader);

            if (metatag.ID == null || metatag.Name == null || metatag.Description == null || metatag.Standard == null)
                return false;

            schemaRestore.Schema.AddMetatag(
                Metatag.Create(
                    metatag.ParentId,
                    metatag.Name,
                    metatag.Description,
                    MetatagStandards.GetStandardFromStandardTag(metatag.Standard),
                    metatag.ID));

            return true;
        }

        return false;
    }

    public MetatagSchemaRestore(XmlReader reader)
    {
        Schema = new MetatagSchema();
        Schema.DontBuildTree = true;

        XmlIO.FReadElement(reader, this, "metatagSchema", null, FParseElement);
        Schema.DontBuildTree = false;
        Schema.RebuildWorkingTree();
    }
}
