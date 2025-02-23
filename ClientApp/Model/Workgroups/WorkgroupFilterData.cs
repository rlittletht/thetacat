using System;

namespace Thetacat.Model.Workgroups;

public class WorkgroupFilterData
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Expression { get; set; }

    public int FilterClock { get; set; }

    public WorkgroupFilterData()
    {
        FilterClock = 0;
        Id = Guid.Empty;
        Name = string.Empty;
        Description = string.Empty;
        Expression = string.Empty;
    }

    public WorkgroupFilterData(Guid id, string name, string description, string expression, int filterClock)
    {
        Id = id;
        Name = name;
        Description = description;
        Expression = expression;
        FilterClock = filterClock;
    }

    public WorkgroupFilterData(ServiceWorkgroupFilter filter)
    {
        Id = filter.Id!.Value;
        Name = filter.Name!;
        Description = filter.Description!;
        Expression = filter.Expression!;
        FilterClock = filter.FilterClock!.Value;
    }

    public WorkgroupFilterData(WorkgroupFilterData source)
    {
        Id = source.Id;
        Name = source.Name;
        Description = source.Description;
        Expression = source.Expression;
        FilterClock = source.FilterClock;
    }
}
