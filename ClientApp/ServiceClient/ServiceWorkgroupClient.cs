using System;

namespace Thetacat.ServiceClient;

public class ServiceWorkgroupClient
{
    public Guid? ClientId { get; set; }
    public string? ClientName { get; set; }
    public Guid? AuthId { get; set; }
}
