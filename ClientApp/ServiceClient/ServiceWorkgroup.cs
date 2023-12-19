using System;
using System.Security.Permissions;

namespace Thetacat.ServiceClient;

public class ServiceWorkgroup
{
    public Guid? ID { get; set; }
    public string? Name { get; set; }
    public string? ServerPath { get; set; }
    public string? CacheRoot { get; set; }
}
