using System;

namespace Thetacat.ServiceClient;

public class ServiceMetatag
{
    public Guid ID { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? Parent { get; set; }
    public string? Standard { get; set; }
}
