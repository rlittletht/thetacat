using System;
using System.Collections.Generic;

namespace Thetacat.ServiceClient;

public class ServiceStack
{
    public Guid? Id { get; set; }
    public string? StackType { get; set; }
    public string? Description { get; set; }

    public List<ServiceStackItem>? StackItems { get; set; }
}
