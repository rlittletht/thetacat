using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Thetacat.Model.Workgroups;
using Thetacat.ServiceClient.LocalDatabase;
using Thetacat.Types;

namespace Thetacat.Filtering;

/*----------------------------------------------------------------------------
    %%Class: Filters
    %%Qualified: Thetacat.Filtering.Filters

    This is the collection of filters, both workgroup and local

    The local filters are just populated from the profile (which is the
    source of truth for local filters).

    The workgroup filters are maintained here
----------------------------------------------------------------------------*/
public class Filters : IEnumerable<Filter>
{
    private readonly List<Filter> m_localFilters = new();
    private readonly List<Filter> m_workgroupFiltersList = new();

    private readonly Dictionary<Guid, WorkgroupFilter> m_workgroupFilters = new();

    public IEnumerator<Filter> GetEnumerator() => new AggregatedEnumerator<Filter>(m_localFilters, m_workgroupFiltersList);

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
    public void ResetLocalFilters(IAppState? appState = null)
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
        m_workgroupFiltersList.Clear();
        m_workgroupFilters.Clear();

        if (workgroup == null)
            return;

        List<ServiceWorkgroupFilter>? filters = App.State.Workgroup?.GetLatestWorkgroupFilters();

        if (filters != null)
        {
            foreach (ServiceWorkgroupFilter filter in filters)
            {
                WorkgroupFilter wgFilter = new(filter);

                m_workgroupFilters.Add(wgFilter.Id, wgFilter);

                m_workgroupFiltersList.Add(new Filter(wgFilter));
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

    /*----------------------------------------------------------------------------
        %%Function: UpdateLocalFilter
        %%Qualified: Thetacat.Filtering.Filters.UpdateLocalFilter
    ----------------------------------------------------------------------------*/
    public void UpdateLocalFilter(FilterDefinition definition)
    {
        App.State.ActiveProfile.Filters[definition.FilterName] = definition;
        App.State.Settings.WriteSettings();

        App.State.Filters.ResetLocalFilters();
    }

    /*----------------------------------------------------------------------------
        %%Function: CreateLocalFilter
        %%Qualified: Thetacat.Filtering.Filters.CreateLocalFilter
    ----------------------------------------------------------------------------*/
    public void CreateLocalFilter(FilterDefinition definition)
    {
        App.State.ActiveProfile.Filters[definition.FilterName] = definition;
        App.State.Settings.WriteSettings();

        App.State.Filters.ResetLocalFilters();
    }

    /*----------------------------------------------------------------------------
        %%Function: GetMatchingWorkgroupFilter
        %%Qualified: Thetacat.Filtering.Filters.GetMatchingWorkgroupFilter
    ----------------------------------------------------------------------------*/
    Filter GetMatchingWorkgroupFilter(Guid id)
    {
        foreach (Filter filter in m_workgroupFiltersList)
        {
            if (filter.Id == id)
                return filter;
        }

        throw new CatExceptionInternalFailure("filter not found");
    }

    /*----------------------------------------------------------------------------
        %%Function: DeleteFilter
        %%Qualified: Thetacat.Filtering.Filters.DeleteFilter
    ----------------------------------------------------------------------------*/
    public void DeleteFilter(Filter filter)
    {
        if (filter.FilterType == FilterType.Local)
        {
            if (!App.State.ActiveProfile.Filters.ContainsKey(filter.Definition.FilterName))
                return;

            App.State.ActiveProfile.Filters.Remove(filter.Definition.FilterName);
            App.State.Settings.WriteSettings();

            ResetLocalFilters();
        }
        else
        {
            if (!m_workgroupFilters.TryGetValue(filter.Id, out WorkgroupFilter? workgroupFilter))
                return;

            workgroupFilter.MarkDeleted();

            Filter filterInList = GetMatchingWorkgroupFilter(workgroupFilter.Id);
            m_workgroupFiltersList.Remove(filterInList);

            CommitFilters();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: GetMatchingLocalFilter
        %%Qualified: Thetacat.Filtering.Filters.GetMatchingLocalFilter
    ----------------------------------------------------------------------------*/
    Filter GetMatchingLocalFilter(string name)
    {
        foreach (Filter filter in m_localFilters)
        {
            if (string.Compare(name, filter.Definition.FilterName, StringComparison.CurrentCultureIgnoreCase) == 0)
                return filter;
        }

        throw new CatExceptionInternalFailure("filter not found");
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateWorkgroupFilter
        %%Qualified: Thetacat.Filtering.Filters.UpdateWorkgroupFilter
    ----------------------------------------------------------------------------*/
    public void UpdateWorkgroupFilter(Guid id, FilterDefinition definition)
    {
        // get the matching workgroup filter
        WorkgroupFilter filter = m_workgroupFilters[id];

        if (filter.Id != id)
            throw new CatExceptionInternalFailure("filter id mismatch");

        filter.Description = definition.Description;
        filter.Expression = definition.Expression.ToString();
        filter.Name = definition.FilterName;

        Filter cachedItem = GetMatchingWorkgroupFilter(id);

        // since we aren't keeping track of base/working, we can just reset the definition
        cachedItem.Definition = definition;
    }

    /*----------------------------------------------------------------------------
        %%Function: CreateWorkgroupFilter
        %%Qualified: Thetacat.Filtering.Filters.CreateWorkgroupFilter
    ----------------------------------------------------------------------------*/
    public void CreateWorkgroupFilter(Guid id, FilterDefinition definition)
    {
        // get the matching workgroup filter
        WorkgroupFilter filter = new WorkgroupFilter(id, definition.FilterName, definition.Description, definition.ExpressionText);

        m_workgroupFilters.Add(id, filter);
        m_workgroupFiltersList.Add(new Filter(filter));
    }

    /*----------------------------------------------------------------------------
        %%Function: GetDefaultFilter
        %%Qualified: Thetacat.Filtering.Filters.GetDefaultFilter
    ----------------------------------------------------------------------------*/
    public bool TryGetDefaultFilter(string defName, [NotNullWhen(true)] out Filter? filter)
    {
        try
        {
            if (Guid.TryParse(defName, out Guid id))
            {
                // if its a guid, its a workgroup...
                filter = GetMatchingWorkgroupFilter(id);
                return true;
            }

            // otherwise get it by the local name
            filter = GetMatchingLocalFilter(defName);
            return true;
        }
        catch
        {
            filter = null;
            return false;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: GetWorkgroupFilter
        %%Qualified: Thetacat.Filtering.Filters.GetWorkgroupFilter
    ----------------------------------------------------------------------------*/
    public Filter GetWorkgroupFilter(Guid id)
    {
        return GetMatchingWorkgroupFilter(id);
    }

    /*----------------------------------------------------------------------------
        %%Function: GetLocalFilter
        %%Qualified: Thetacat.Filtering.Filters.GetLocalFilter
    ----------------------------------------------------------------------------*/
    public Filter GetLocalFilter(string name)
    {
        return GetMatchingLocalFilter(name);
    }

    /*----------------------------------------------------------------------------
        %%Function: CommitFilters
        %%Qualified: Thetacat.Filtering.Filters.CommitFilters
    ----------------------------------------------------------------------------*/
    public void CommitFilters()
    {
        List<WorkgroupFilter> inserts = new();
        List<WorkgroupFilter> deletes = new();
        List<WorkgroupFilter> updates = new();

        foreach (WorkgroupFilter filter in m_workgroupFilters.Values)
        {
            if (filter.State == WorkgroupFilterState.Create)
                inserts.Add(filter);
            else if (filter.State == WorkgroupFilterState.Delete)
                deletes.Add(filter);
            else if (filter.State == WorkgroupFilterState.MaybeUpdate)
                updates.Add(filter);
        }

        App.State.Workgroup!.ExecuteFilterAddsAndDeletes(deletes, inserts);

        // now we have to process each filter one by one
        foreach (WorkgroupFilter filter in updates)
        {
            filter.CommitUpdateToDatabase(App.State.Workgroup!);
        }
    }
}
