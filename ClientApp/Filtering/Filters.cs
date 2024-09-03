using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Thetacat.Model.Workgroups;
using Thetacat.Types;

namespace Thetacat.Filtering;

/*----------------------------------------------------------------------------
    %%Class: Filters
    %%Qualified: Thetacat.Filtering.Filters

    This is the collection of filters, both workgroup and private
----------------------------------------------------------------------------*/
public class Filters: IEnumerable<Filter>
{
    private readonly List<Filter> m_localFilters = new();
    private readonly List<Filter> m_workgroupFilters = new();

    public IEnumerator<Filter> GetEnumerator() => new AggregatedEnumerator<Filter>(m_localFilters, m_workgroupFilters);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /*----------------------------------------------------------------------------
        %%Function: OnProfileChanged
        %%Qualified: Thetacat.Filtering.Filters.OnProfileChanged
    ----------------------------------------------------------------------------*/
    private void OnProfileChanged(object? sender, ProfileChangedEventArgs e)
    {
        ResetLocalFilters();
        ResetWorkgroupFilters(App.State.Workgroup);
    }

    /*----------------------------------------------------------------------------
        %%Function: ResetLocalFilters
        %%Qualified: Thetacat.Filtering.Filters.ResetLocalFilters
    ----------------------------------------------------------------------------*/
    private void ResetLocalFilters(IAppState? appState = null)
    {
        m_localFilters.Clear();
        foreach (FilterDefinition filterDef in (appState ?? App.State).ActiveProfile.Filters.Values)
        {
            m_localFilters.Add(new Filter(filterDef, FilterType.Local, null));
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: ResetWorkgroupFilters
        %%Qualified: Thetacat.Filtering.Filters.ResetWorkgroupFilters
    ----------------------------------------------------------------------------*/
    public void ResetWorkgroupFilters(IWorkgroup? workgroup)
    {
        m_workgroupFilters.Clear();

        if (workgroup == null)
            return;

        List<ServiceWorkgroupFilter>? filters = App.State.Workgroup?.GetLatestWorkgroupFilters();

        if (filters != null)
        {
            foreach (ServiceWorkgroupFilter filter in filters)
            {
                m_workgroupFilters.Add(new Filter(new FilterDefinition(filter.Name!, filter.Description!, filter.Expression!), FilterType.Workgroup, filter.Id!.Value));
            }
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: Filters
        %%Qualified: Thetacat.Filtering.Filters.Filters
    ----------------------------------------------------------------------------*/
    public Filters(IAppState appState)
    {
        appState.ProfileChanged += OnProfileChanged;
        ResetLocalFilters(appState);
        ResetWorkgroupFilters(appState.Workgroup);
    }

}
