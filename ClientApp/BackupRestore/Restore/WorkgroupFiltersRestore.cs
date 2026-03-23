using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Xml;
using Thetacat.Model.Workgroups;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class WorkgroupFiltersRestore
{
    public List<WorkgroupFilterData> Filters = new();

    public WorkgroupFiltersRestore(XmlReader reader)
    {
        XmlIO.FReadElement(reader, this, "filters", null, FReadWorkgroupFiltersElements);
    }

    static bool FReadWorkgroupFiltersElements(XmlReader reader, string element, WorkgroupFiltersRestore restore)
    {
        if (element == "filter")
        {
            WorkgroupFilterData filter = new();
            XmlIO.FReadElement(reader, filter, element, FReadWorkgroupFilterAttributes, FReadWorkgroupFilterElements);

            restore.Filters.Add(filter);
            return true;
        }
        return false;
    }

    static bool FReadWorkgroupFilterAttributes(string attribute, string value, WorkgroupFilterData item)
    {
        if (attribute == "id")
        {
            item.Id = Guid.Parse(value);
            return true;
        }
        throw new Exception($"Unknown attribute {attribute}");
    }

    static string ParseCollectText(XmlReader reader, WorkgroupFilterData client, string element)
    {
        XmlIO.ContentCollector collector = new XmlIO.ContentCollector();
        if (!XmlIO.FReadElement(reader, client, element, null, null, collector))
            return "";

        return collector.ToString();
    }

    static bool FReadWorkgroupFilterElements(XmlReader reader, string element, WorkgroupFilterData item)
    {
        if (element == "name")
        {
            item.Name = ParseCollectText(reader, item, element);
            return true;
        }
        if (element == "description")
        {
            item.Description = ParseCollectText(reader, item, element);
            return true;
        }
        if (element == "expression")
        {
            item.Expression = ParseCollectText(reader, item, element);
            return true;
        }
        if (element == "vectorClock")
        {
            item.FilterClock = Int32.Parse(ParseCollectText(reader, item, element));
            return true;
        }
        throw new Exception($"Unknown element {element}");
    }
}
