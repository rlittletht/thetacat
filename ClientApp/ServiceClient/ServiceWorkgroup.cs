using System;

namespace Thetacat.ServiceClient;

public class ServiceWorkgroup
{
    public Guid? CatalogID { get; set; }
    public Guid? ID { get; set; }
    public string? Name { get; set; }
    public string? ServerPath { get; set; }
    public string? CacheRoot { get; set; }
}
