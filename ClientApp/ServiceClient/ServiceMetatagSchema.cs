using System.Collections.Generic;

namespace Thetacat.ServiceClient;

public class ServiceMetatagSchema
{
    public List<ServiceMetatag>? Metatags { get; set; } = new();
    public int? SchemaVersion { get; set; }
}
