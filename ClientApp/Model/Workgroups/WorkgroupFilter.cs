using System;
using Thetacat.Types;

namespace Thetacat.Model.Workgroups;

public class WorkgroupFilter
{
    private WorkgroupFilterData m_currentData;
    private WorkgroupFilterData? m_baseData;

    public WorkgroupFilterState State { get; set; }

    public Guid Id
    {
        get => m_currentData.Id;
        set => m_currentData.Id = EnsureBaseAndReturn(value);
    }

    public string Name
    {
        get => m_currentData.Name;
        set => m_currentData.Name = EnsureBaseAndReturn(value);
    }

    public string Description
    {
        get => m_currentData.Description;
        set => m_currentData.Description= EnsureBaseAndReturn(value);
    }

    public string Expression
    {
        get => m_currentData.Expression;
        set => m_currentData.Expression = EnsureBaseAndReturn(value);
    }

    public int FilterClock
    {
        get => m_currentData.FilterClock;
        set => m_currentData.FilterClock = EnsureBaseAndReturn(value);
    }

    void EnsureBase()
    {
        m_baseData ??= new WorkgroupFilterData(m_currentData);
    }

    T EnsureBaseAndReturn<T>(T value)
    {
        EnsureBase();
        return value;
    }

    public WorkgroupFilter(ServiceWorkgroupFilter filter)
    {
        m_currentData = new WorkgroupFilterData(filter.Id!.Value, filter.Name!, filter.Description!, filter.Expression!, filter.FilterClock!.Value);
    }
}
