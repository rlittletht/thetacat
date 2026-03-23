using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Xml;
using Thetacat.ServiceClient;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class WorkgroupClientRestore
{
    private List<ServiceWorkgroupClient> Clients = new();
    private ServiceWorkgroupClient? Building;

    /*----------------------------------------------------------------------------
        %%Function: WorkgroupClientRestore
        %%Qualified: Thetacat.BackupRestore.Restore.WorkgroupClientRestore.WorkgroupClientRestore

        read the workgroupClients element
    ----------------------------------------------------------------------------*/
    public WorkgroupClientRestore(XmlReader reader, List<ServiceWorkgroupClient> clients)
    {
        XmlIO.FReadElement(reader, this, "clients", null, FReadWorkgroupClientsElements);
        clients.AddRange(Clients);
    }

    static bool FReadWorkgroupClientsElements(XmlReader reader, string element, WorkgroupClientRestore client)
    {
        if (element == "client")
        {
            client.Building = new ServiceWorkgroupClient();

            XmlIO.FReadElement(reader, client.Building, "client", FReadWorkgroupClientAttributes, FReadWorkgroupClientElements);
            client.Clients.Add(client.Building);
            return true;
        }

        throw new XmlException($"Unknown element {element}");
    }

    static bool FReadWorkgroupClientAttributes(string attribute, string value, ServiceWorkgroupClient client)
    {
        if (attribute == "id")
        {
            client.ClientId = Guid.Parse(value);
            return true;
        }

        throw new XmlException($"Unknown attribute {attribute}");
    }

    static string ParseCollectText(XmlReader reader, ServiceWorkgroupClient client, string element)
    {
        XmlIO.ContentCollector collector = new XmlIO.ContentCollector();
        if (!XmlIO.FReadElement(reader, client, element, null, null, collector))
            return "";

        return collector.ToString();
    }

    static bool FReadWorkgroupClientElements(XmlReader reader, string element, ServiceWorkgroupClient client)
    {
        if (element == "name")
        {
            client.ClientName = ParseCollectText(reader, client, element);
            return true;
        }
        if (element == "vectorClock")
        {
            client.VectorClock = Int32.Parse(ParseCollectText(reader, client, element));
            return true;
        }
        if (element == "deletedMediaClock")
        {
            client.DeletedMediaClock = Int32.Parse(ParseCollectText(reader, client, element));
            return true;
        }

        throw new XmlException($"Unknown element {element}");
    }
}
