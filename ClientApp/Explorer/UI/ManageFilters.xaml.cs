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

    void FillAvailableFilters()
    {
        m_model.AvailableFilters.Clear();
        foreach (KeyValuePair<string, FilterDefinition> def in App.State.ActiveProfile.Filters)
        {
            m_model.AvailableFilters.Add(def.Value);
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
    public ManageFilters(FilterDefinition? currentFilter)
    {
        InitializeComponent();
        DataContext = m_model;

        if (currentFilter != null)
        {
            m_model.Name = currentFilter.FilterName;
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
        if (m_model.SelectedFilterDefinition == null)
            return;

        m_model.QueryText.AddRange(
            m_model.SelectedFilterDefinition.Expression.ToStrings(
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

    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "SelectedFilterDefinition")
            UpdateQueryClauses();
    }

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

        FilterDefinition def = m_model.SelectedFilterDefinition ?? throw new CatExceptionInternalFailure("no selected filter definition on save?");

        if (App.State.ActiveProfile.Filters.TryGetValue(def.FilterName, out FilterDefinition? filter))
        {
            filter.Description = def.Description;
            filter.Expression = def.Expression;
        }
        else
        {
            App.State.ActiveProfile.Filters.Add(def.FilterName, def);
        }

        App.State.Settings.WriteSettings();
    }

    public string? GetFilterName()
    {
        return m_model.SelectedFilterDefinition?.FilterName;
    }

    private void DoEditFilter(object sender, RoutedEventArgs e)
    {
        EditFilter editFilter = new EditFilter(m_filterSchema, m_metatagLineageMap, m_model.SelectedFilterDefinition);

        editFilter.Owner = this;
        editFilter.ShowDialog();
        if (editFilter.DialogResult is true)
        {
            FilterDefinition def = editFilter.GetDefinition();

            App.State.ActiveProfile.Filters[def.FilterName] = def;
            App.State.Settings.WriteSettings();
            FillAvailableFilters();
        }
    }

    private void DoNewFilter(object sender, RoutedEventArgs e)
    {
        EditFilter editFilter = new EditFilter(m_filterSchema, m_metatagLineageMap);

        editFilter.Owner = this;
        editFilter.ShowDialog();
        if (editFilter.DialogResult is true)
        {
            FilterDefinition def = editFilter.GetDefinition();

            App.State.ActiveProfile.Filters[def.FilterName] = def;
            App.State.Settings.WriteSettings();
            FillAvailableFilters();
        }
    }

    private void DoSetAsDefault(object sender, RoutedEventArgs e)
    {
        if (m_model.SelectedFilterDefinition == null)
        {
            MessageBox.Show("Choose a filter to make the default");
            return;
        }

        App.State.ActiveProfile.DefaultFilterName = m_model.SelectedFilterDefinition.FilterName;
        App.State.Settings.WriteSettings();
    }
}