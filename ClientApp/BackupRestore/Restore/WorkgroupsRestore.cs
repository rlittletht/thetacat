using System.Collections.Generic;
using System.Xml;
using Thetacat.ServiceClient;
using XMLIO;

namespace Thetacat.BackupRestore.Restore;

public class WorkgroupsRestore
{
    public List<ServiceWorkgroup> Workgroups = new();

    static bool FParseWorkgroupsElement(XmlReader reader, string element, WorkgroupsRestore importsRestore)
    {
        if (element != "workgroup")
            throw new XmlioExceptionSchemaFailure($"unknown element {element}");

        WorkgroupRestore itemRestore = new WorkgroupRestore(reader);

        importsRestore.Workgroups.Add(itemRestore.ItemData);

        return true;
    }

    public WorkgroupsRestore(XmlReader reader)
    {
        XmlIO.FReadElement(reader, this, "workgroups", null, FParseWorkgroupsElement);
    }
}
