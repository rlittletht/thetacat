using System;
using System.Collections.Generic;
using System.Xml;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class MetatagRestore
{
    public Guid? ID;
    public string? Name;
    public string? Description;
    public string? Standard;
    public Guid? ParentId;

    public static bool FParseAttribute(string attribute, string value, MetatagRestore metatag)
    {
        if (attribute == "id")
        {
            if (!Guid.TryParse(value, out Guid id))
                return false;

            metatag.ID = id;
            return true;
        }

        return false;
    }

    private static HashSet<string> children = new() { "name", "description", "standard", "parentId" };

    public static bool FParseElement(XmlReader reader, string element, MetatagRestore metatag)
    {
        if (!children.Contains(element))
            return false;

        XmlIO.ContentCollector collector = new();
        if (!XmlIO.FReadElement(reader, metatag, element, null, null, collector))
            return false;

        switch (element)
        {
            case "name":
                metatag.Name = collector.ToString();
                return true;
            case "description":
                metatag.Description = collector.ToString();
                return true;
            case "standard":
                metatag.Standard = collector.ToString();
                return true;
            case "parentId":
                if (!Guid.TryParse(collector.ToString(), out Guid parentId))
                    return false;
                metatag.ParentId = parentId;
                return true;
        }

        return false;
    }

    public MetatagRestore(XmlReader reader)
    {
        XmlIO.FReadElement(reader, this, "metatag", FParseAttribute, FParseElement);
    }
}
