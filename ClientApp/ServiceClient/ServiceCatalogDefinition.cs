using System;

namespace Thetacat.ServiceClient;

public class ServiceCatalogDefinition
{
    public Guid ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public ServiceCatalogDefinition(Guid id, string name, string description)
    {
        ID = id;
        Name = name;
        Description = description;
    }
}
