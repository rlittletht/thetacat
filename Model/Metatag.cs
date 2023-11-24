using System;
using Thetacat.ServiceClient;

namespace Thetacat.Model;

public class Metatag
{
    public Guid ID { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? Parent { get; set; }

    public static Metatag CreateFromService(ServiceMetatag serviceMetatag)
    {
        return new Metatag()
               {
                   ID = serviceMetatag.ID, 
                   Parent = serviceMetatag.Parent, 
                   Name = serviceMetatag.Name ?? string.Empty,
                   Description = serviceMetatag.Description ?? string.Empty
               };
    }
}


