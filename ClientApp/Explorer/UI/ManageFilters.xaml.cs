using Emgu.CV.Dnn;
using MetadataExtractor.Formats.Xmp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Accessibility;
using Thetacat.Explorer.UI;
using Thetacat.Filtering;
using Thetacat.Filtering.UI;
using Thetacat.Metatags;
using Thetacat.Metatags.Model;
using Thetacat.Standards;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Explorer;

/// <summary>
/// Interaction logic for Filter.xaml
/// </summary>
public partial class ManageFilters : Window
{
    private ChooseFilterModel m_model = new();
    private Dictionary<Guid, string> m_metatagLineageMap;
    private MetatagSchema m_filterSchema;

    /*----------------------------------------------------------------------------
        %%Function: FillAvailableFilters
        %%Qualified: Thetacat.Explorer.ManageFilters.FillAvailableFilters
    ----------------------------------------------------------------------------*/
    void FillAvailableFilters()
    {
        m_model.AvailableFilters.Clear();
        foreach (Filter filter in App.State.Filters)
        {
            m_model.AvailableFilters.Add(filter);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: BuildFilterSchema
        %%Qualified: Thetacat.Explorer.ManageFilters.BuildFilterSchema
    ----------------------------------------------------------------------------*/
    MetatagSchema BuildFilterSchema()
    {
        MetatagSchema filterSchema = new MetatagSchema(App.State.MetatagSchema);

        filterSchema.GetOrBuildDirectoryTag(null, MetatagStandards.Standard.User, "user root", BuiltinTags.s_UserRootID);
        filterSchema.GetOrBuildDirectoryTag(null, MetatagStandards.Standard.Cat, "cat root", BuiltinTags.s_CatRootID);

        foreach (Metatag tag in BuiltinTags.s_NonSchemaBuiltinTags)
        {
            filterSchema.AddMetatag(tag);
        }

        return filterSchema;
    }

    /*----------------------------------------------------------------------------
        %%Function: ManageFilters
        %%Qualified: Thetacat.Explorer.ManageFilters.ManageFilters
    ----------------------------------------------------------------------------*/
    public ManageFilters(Filter? currentFilter)
    {
        InitializeComponent();
        DataContext = m_model;

        if (currentFilter != null)
        {
            m_model.Name = currentFilter.Definition.FilterName;
        }

        FillAvailableFilters();
        App.State.RegisterWindowPlace(this, "FilterCatalogWindow");

        BuildFilterSchema();
        m_filterSchema = BuildFilterSchema();
        m_metatagLineageMap = m_filterSchema.BuildLineageMap();
        m_model.PropertyChanged += OnModelChanged;
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateQueryClauses
        %%Qualified: Thetacat.Explorer.ManageFilters.UpdateQueryClauses
    ----------------------------------------------------------------------------*/
    void UpdateQueryClauses()
    {
        m_model.QueryText.Clear();
        if (m_model.SelectedFilter == null)
            return;

        m_model.QueryText.AddRange(
            m_model.SelectedFilter.Definition.Expression.ToStrings(
                (field) =>
                {
                    if (Guid.TryParse(field, out Guid metatagId))
                    {
                        if (m_metatagLineageMap.TryGetValue(metatagId, out string? lineage))
                            return $"[{lineage}]";
                    }

                    return $"[{field}]";
                }));
    }

    /*----------------------------------------------------------------------------
        %%Function: OnModelChanged
        %%Qualified: Thetacat.Explorer.ManageFilters.OnModelChanged
    ----------------------------------------------------------------------------*/
    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "SelectedFilter")
            UpdateQueryClauses();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoApply
        %%Qualified: Thetacat.Explorer.ManageFilters.DoApply
    ----------------------------------------------------------------------------*/
    private void DoApply(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        this.Close();
    }

    private void SaveFilter(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(m_model.Name))
        {
            MessageBox.Show("Must specify a name for the filter");
            return;
        }

        Filter filter = m_model.SelectedFilter ?? throw new CatExceptionInternalFailure("no selected filter definition on save?");

        if (filter.FilterType == FilterType.Local)
        {
            if (App.State.ActiveProfile.Filters.TryGetValue(filter.Definition.FilterName, out FilterDefinition? filterDef))
            {
                filterDef.Description = filter.Definition.Description;
                filterDef.Expression = filter.Definition.Expression;
            }
            else
            {
                App.State.ActiveProfile.Filters.Add(filter.Definition.FilterName, filter.Definition);
                App.State.Filters.ResetLocalFilters();
            }

            App.State.Settings.WriteSettings();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: GetFilter
        %%Qualified: Thetacat.Explorer.ManageFilters.GetFilter
    ----------------------------------------------------------------------------*/
    public Filter? GetFilter()
    {
        return m_model.SelectedFilter;
    }

    /*----------------------------------------------------------------------------
        %%Function: DoEditFilter
        %%Qualified: Thetacat.Explorer.ManageFilters.DoEditFilter
    ----------------------------------------------------------------------------*/
    private void DoEditFilter(object sender, RoutedEventArgs e)
    {
        EditFilter editFilter = new EditFilter(m_filterSchema, m_metatagLineageMap, m_model.SelectedFilter);

        editFilter.Owner = this;
        editFilter.ShowDialog();
        if (editFilter.DialogResult is true)
        {
            FilterDefinition def = editFilter.GetDefinition();

            if (editFilter.GetFilterType() == FilterType.Local)
                App.State.Filters.UpdateLocalFilter(def);
            else
                App.State.Filters.UpdateWorkgroupFilter(editFilter.GetId(), def);

            FillAvailableFilters();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DoNewFilter
        %%Qualified: Thetacat.Explorer.ManageFilters.DoNewFilter
    ----------------------------------------------------------------------------*/
    private void DoNewFilter(object sender, RoutedEventArgs e)
    {
        EditFilter editFilter = new EditFilter(m_filterSchema, m_metatagLineageMap);

        editFilter.Owner = this;
        editFilter.ShowDialog();
        if (editFilter.DialogResult is true)
        {
            FilterDefinition def = editFilter.GetDefinition();

            if (editFilter.GetFilterType() == FilterType.Local)
                App.State.Filters.CreateLocalFilter(def);
            else
                App.State.Filters.CreateWorkgroupFilter(editFilter.GetId(), def);

            FillAvailableFilters();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DoSetAsDefault
        %%Qualified: Thetacat.Explorer.ManageFilters.DoSetAsDefault
    ----------------------------------------------------------------------------*/
    private void DoSetAsDefault(object sender, RoutedEventArgs e)
    {
        if (m_model.SelectedFilter == null)
        {
            MessageBox.Show("Choose a filter to make the default");
            return;
        }

        if (m_model.SelectedFilter.FilterType == FilterType.Local)
            App.State.ActiveProfile.DefaultFilterName = m_model.SelectedFilter.Definition.FilterName;
        else
            App.State.ActiveProfile.DefaultFilterName = m_model.SelectedFilter.Id.ToString();

        App.State.Settings.WriteSettings();
    }
}
