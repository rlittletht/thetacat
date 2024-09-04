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

    public int BaseClock => m_baseData!.FilterClock;

    void EnsureBase()
    {
        m_baseData ??= new WorkgroupFilterData(m_currentData);
    }

    T EnsureBaseAndReturn<T>(T value)
    {
        EnsureBase();
        if (State == WorkgroupFilterState.None)
            State = WorkgroupFilterState.MaybeUpdate;

        return value;
    }

    /*----------------------------------------------------------------------------
        %%Function: MarkDeleted
        %%Qualified: Thetacat.Model.Workgroups.WorkgroupFilter.MarkDeleted
    ----------------------------------------------------------------------------*/
    public void MarkDeleted()
    {
        State = WorkgroupFilterState.Delete;
    }

    /*----------------------------------------------------------------------------
        %%Function: WorkgroupFilter
        %%Qualified: Thetacat.Model.Workgroups.WorkgroupFilter.WorkgroupFilter
    ----------------------------------------------------------------------------*/
    public WorkgroupFilter(ServiceWorkgroupFilter filter)
    {
        m_currentData = new WorkgroupFilterData(filter.Id!.Value, filter.Name!, filter.Description!, filter.Expression!, filter.FilterClock!.Value);
    }

    /*----------------------------------------------------------------------------
        %%Function: WorkgroupFilter
        %%Qualified: Thetacat.Model.Workgroups.WorkgroupFilter.WorkgroupFilter
    ----------------------------------------------------------------------------*/
    public WorkgroupFilter(Guid id, string name, string description, string expression)
    {
        m_currentData = new WorkgroupFilterData(id, name, description, expression, 0);
        State = WorkgroupFilterState.Create;
    }

    /*----------------------------------------------------------------------------
        %%Function: DoThreeWayMerge
        %%Qualified: Thetacat.Model.Workgroups.WorkgroupFilter.DoThreeWayMerge
    ----------------------------------------------------------------------------*/
    void DoThreeWayMerge(WorkgroupFilterData serverData)
    {
        if (m_baseData == null)
        {
            // everything in conflict; server wins everything
            m_currentData = new WorkgroupFilterData(serverData);
            return;
        }

        if (m_baseData.Name != serverData.Name)
        {
            Name = serverData.Name;
            m_baseData.Name = Name;
        }

        if (m_baseData.Description != serverData.Description)
        {
            Description = serverData.Description;
            m_baseData.Description = Description;
        }

        if (m_baseData.Expression != serverData.Expression)
        {
            Expression = serverData.Expression;
            m_baseData.Expression = Expression;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: GetLatestFromServer
        %%Qualified: Thetacat.Model.Workgroups.WorkgroupFilter.GetLatestFromServer
    ----------------------------------------------------------------------------*/
    WorkgroupFilterData GetLatestFromServer(IWorkgroup workgroup)
    {
        return new WorkgroupFilterData(workgroup.GetWorkgroupFilter(Id));
    }

    /*----------------------------------------------------------------------------
        %%Function: CommitUpdateToDatabase
        %%Qualified: Thetacat.Model.Workgroups.WorkgroupFilter.CommitUpdateToDatabase
    ----------------------------------------------------------------------------*/
    public void CommitUpdateToDatabase(IWorkgroup workgroup)
    {
        if (State != WorkgroupFilterState.MaybeUpdate)
            throw new CatExceptionInternalFailure();

        bool fRetried = false;
        int baseClock = BaseClock;
        
        while (true)
        {
            try
            {
                workgroup.UpdateWorkgroupFilter(this, baseClock);
                return;
            }
            catch (CatExceptionDataCoherencyFailure)
            {
                if (fRetried)
                    throw;

                fRetried = true;
                WorkgroupFilterData server = GetLatestFromServer(workgroup);

                DoThreeWayMerge(server);
                baseClock = server.FilterClock;
            }
        }
    }
}
