using System;

namespace Thetacat.ServiceClient;

public class ServiceWorkgroupItem
{
    public Guid? WorkgroupId { get; set; }
    public Guid? MediaId { get; set; }
    public string? Path { get; set; }
    public Guid? CachedBy { get; set; }
    public DateTime? CachedDate { get; set; }
}
