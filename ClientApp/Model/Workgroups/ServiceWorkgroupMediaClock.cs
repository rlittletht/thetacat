using System.Collections.Generic;
using Thetacat.ServiceClient;

namespace Thetacat.Model.Workgroups;

public class ServiceWorkgroupMediaClock
{
    public int VectorClock { get; set; }
    public List<ServiceWorkgroupItem>? Media { get; set; }
}