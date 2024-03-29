﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Thetacat.Standards;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Metadata.UI;

/// <summary>
/// Interaction logic for DefineMetadataMap.xaml
/// </summary>
public partial class DefineMetadataMap : Window
{
    readonly PseMetadataMapItem m_item;

    void InitializeStandards()
    {
        HashSet<string> mappings = new();

        foreach (StandardDefinitions mapping in MetatagStandards.KnownStandards.Values)
        {
            mappings.Add(mapping.StandardTag);
        }

        mappings.Add("USER");

        foreach (string tag in mappings)
        {
            Standard.Items.Add(tag);
        }
    }

    public DefineMetadataMap(IAppState appState, string pseIdentifier)
    {
        InitializeComponent();
        appState.RegisterWindowPlace(this, "DefineMetadataMap");

        m_item = new PseMetadataMapItem(pseIdentifier);
        DataContext = m_item;
        InitializeStandards();
    }

    /*----------------------------------------------------------------------------
        %%Function: StandardSelected
        %%Qualified: Thetacat.Migration.Elements.Metadata.DefineMetadataMap.StandardSelected

        When the standard is selected, repopulated the list of tagnames
    ----------------------------------------------------------------------------*/
    private void StandardSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is string)
        {
            string? standard = (string?)e.AddedItems[0];

            // get the matching standard
            Debug.Assert(standard != null, nameof(standard) + " != null");
            IEnumerable<StandardDefinitions> mappings = MetatagStandards.GetStandardMappingsFromStandardName(standard);

            TagName.Items.Clear();
            List<string> tagNames = new();

            foreach (StandardDefinitions mapping in mappings)
            {
                foreach (StandardDefinition mappingItem in mapping.Properties.Values)
                {
                    tagNames.Add(mappingItem.PropertyTagName);
                }
            }

            tagNames.Sort();
            foreach (string standardName in tagNames)
            {
                TagName.Items.Add(standardName);
            }
        }
    }

    private void DoSave(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}