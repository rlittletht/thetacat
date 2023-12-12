using System.Collections.Generic;
using System.Security.Policy;
using System.Windows.Documents;

namespace Thetacat.ServiceClient;

public class ServiceMetatagSchema
{
    public List<ServiceMetatag>? Metatags { get; set; } = new();
    public int? SchemaVersion { get; set; }
}
