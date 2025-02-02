using System;

namespace Thetacat.Model.Workgroups;

public class ServiceWorkgroupFilter
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Expression { get; set; }
    public int? FilterClock { get; set; }
}
