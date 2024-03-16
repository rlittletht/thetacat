using System;
using System.Security.Permissions;
using System.Xml;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class MediaTagRestore
{
    public Guid MetatagID;
    public string? Value;

    public static bool FParseAttribute(string attribute, string value, MediaTagRestore tagRestore)
    {
        if (attribute == "metatagId")
        {
            if (Guid.TryParse(value, out Guid id))
            {
                tagRestore.MetatagID = id;
                return true;
            }
        }

        return false;
    }

    public MediaTagRestore(XmlReader reader)
    {
        XmlIO.ContentCollector collector = new XmlIO.ContentCollector();
        // how to tel if this is null?
        XmlIO.FReadElement(reader, this, "tag", FParseAttribute, null, collector);

        Value = collector.NullContent ? null : collector.ToString();
    }
}
