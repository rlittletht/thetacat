using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
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

    private void OnProfileChanged(object? sender, ProfileChangedEventArgs e)
    {
        ResetLocalFilters();
    }

    private void ResetLocalFilters(IAppState? appState = null)
    {
        m_localFilters.Clear();
        foreach (FilterDefinition filterDef in (appState ?? App.State).ActiveProfile.Filters.Values)
        {
            m_localFilters.Add(new Filter(filterDef, FilterType.Local, null));
        }
    }

    public void ResetWorkgroupFilters()
    {
        m_workgroupFilters.Clear();
    }

    public Filters(IAppState appState)
    {
        appState.ProfileChanged += OnProfileChanged;
        ResetLocalFilters(appState);
    }

}
