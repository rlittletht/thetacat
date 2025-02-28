using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using System.Xml;
using Thetacat.Model.Workgroups;
using Thetacat.ServiceClient;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class WorkgroupDataRestore
{
    public List<ServiceWorkgroupClient> Clients = new();
    public List<WorkgroupCacheEntryData> MediaItems = new();
    public List<WorkgroupFilterData> Filters = new();
    public int WorkgroupClock = 0;
    public Guid WorkgroupId = Guid.Empty;
    public string WorkgroupName = string.Empty;

    public WorkgroupDataRestore(XmlReader reader)
    {
        XMLIO.XmlIO.FReadElement(reader, this, "workgroupData", FReadWorkgroupDataAttributes, FReadWorkgroupDataElements);
    }

    public WorkgroupDataRestore()
    {
    }

    static bool FReadWorkgroupDataAttributes(string attribute, string value, WorkgroupDataRestore data)
    {
        if (attribute == "workgroupId")
        {
            data.WorkgroupId = Guid.Parse(value);
            return true;
        }
        if (attribute == "name")
        {
            data.WorkgroupName = value;
            return true;
        }
        throw new XmlException($"Unknown attribute {attribute}");
    }

    static bool FReadWorkgroupDataElements(XmlReader reader, string element, WorkgroupDataRestore data)
    {
        if (element == "clients")
        {
            new WorkgroupClientRestore(reader, data.Clients);
            return true;
        }
        if (element == "media")
        {
            WorkgroupMediaRestore restore = new WorkgroupMediaRestore(reader);
            data.MediaItems.AddRange(restore.MediaItems);
            return true;
        }
        if (element == "filters")
        {
            WorkgroupFiltersRestore restore = new WorkgroupFiltersRestore(reader);
            data.Filters.AddRange(restore.Filters);
            return true;
        }
        if (element == "vectorClocks")
        {
            XmlIO.FReadElement(reader, data, element, null, FReadVectorClocksElements);
            return true;
        }
        throw new XmlException($"Unknown element {element}");
    }

    static bool FReadVectorClocksElements(XmlReader reader, string element, WorkgroupDataRestore data)
    {
        if (element == "workgroupClock")
        {
            XmlIO.FReadElement(reader, data, element, FReadWorkgroupClockAttributes, null);
            return true;
        }

        throw new XmlException($"Unknown element {element}");
    }

    static bool FReadWorkgroupClockAttributes(string attribute, string value, WorkgroupDataRestore data)
    {
        if (attribute == "value")
        {
            data.WorkgroupClock = int.Parse(value);
            return true;
        }

        throw new XmlException($"Unknown element {attribute}");
    }
}
