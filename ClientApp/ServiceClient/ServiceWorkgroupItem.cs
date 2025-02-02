using System;

namespace Thetacat.ServiceClient;

public class ServiceWorkgroupItem
{
    public Guid? MediaId { get; set; }
    public string? Path { get; set; }
    public Guid? CachedBy { get; set; }
    public DateTime? CachedDate { get; set; }
    public int? VectorClock { get; set; }
    public string? MD5 { get; set; }
}
