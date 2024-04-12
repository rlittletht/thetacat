using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using TCore.XmlSettings;
using Thetacat.Filtering;

namespace Thetacat.Import.UI;

public class MapStore
{
    private readonly XmlDescription<MapStore> XmlMapStoreDescription;
    private static string s_uri = "http://schemas.thetasoft.com/Thetacat/mappingStore/2024";

    public class MapPair
    {
        public string From = string.Empty;
        public string To = string.Empty;

        public MapPair(){}
        public MapPair(string from, string to)
        {
            From = from;
            To = to;
        }
    }

    public Dictionary<string, List<MapPair>> Mappings { get; } = new();

    public MapStore()
    {
#pragma warning disable format // @formatter:off
        #region XML Description
        XmlMapStoreDescription =
            XmlDescriptionBuilder<MapStore>
                .Build(s_uri, "MappingsCollection")
                    .AddChildElement("Mappings")
                    .AddAttribute("Name", (_, context) => context!.GetDictionaryKey<string, List<MapPair>>(), (_, value, context) => context!.SetDictionaryKey<string, List<MapPair>> (value))
                    .SetRepeating(CreateMappingsRepeatContext, AreRemainingMappings, CommitMappingsRepeatItem)
                        .AddChildElement("Mapping")
                        .SetRepeating(CreateMapRepeatContext, AreRemainingMaps, CommitMapRepeatItem)
                            .AddChildElement("From", (_, context) => ((MapPair)context!.RepeatKey).From, (_, value, context) => ((MapPair)context!.RepeatKey).From = value ?? string.Empty)
                            .AddElement("To", (_, context) => ((MapPair)context!.RepeatKey).To, (_, value, context) => ((MapPair)context!.RepeatKey).To= value ?? string.Empty)
                            .Pop()
                        .Pop()
                    .Pop();
        #endregion
#pragma warning restore format // @formatter: on

        try
        {
            using ReadFile<MapStore> file = ReadFile<MapStore>.CreateSettingsFile(App.MapStorePath);
            file.DeSerialize(XmlMapStoreDescription, this);
        }
        catch (Exception ex) when
            (ex is DirectoryNotFoundException
             || ex is FileNotFoundException)
        {
            // this is fine, we just don't have any maps to load
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Caught exception reading mapstore: {ex.Message}");
        }
    }

    #region Repeating Mappings

    // this is the top level repating mappings
    // <MappingsCollection><Mappings Name="Maps1">...</Mappings><Mappings Name="Maps2">...</Mappings</MappingsCollection>

    private IEnumerator<KeyValuePair<string, List<MapPair>>>? MappingsEnumerator;

    private static RepeatContext<MapStore>.RepeatItemContext CreateMappingsRepeatContext(
        MapStore store,
        Element<MapStore> element,
        RepeatContext<MapStore>.RepeatItemContext? parent)
    {
        if (store.MappingsEnumerator != null)
        {
            return new RepeatContext<MapStore>.RepeatItemContext(
                element,
                parent,
                store.MappingsEnumerator.Current);
        }

        return new RepeatContext<MapStore>.RepeatItemContext(element, parent, new KeyValuePair<string, List<MapPair>>("", new List<MapPair>()));
    }

    private static bool AreRemainingMappings(MapStore store, RepeatContext<MapStore>.RepeatItemContext? itemContext)
    {
        if (store.Mappings.Count == 0)
            return false;

        store.MappingsEnumerator ??= store.Mappings.GetEnumerator();

        bool remaining = store.MappingsEnumerator.MoveNext();
        if (!remaining)
            store.MappingsEnumerator = null;

        return remaining;
    }

    private static void CommitMappingsRepeatItem(MapStore store, RepeatContext<MapStore>.RepeatItemContext? itemContext)
    {
        if (itemContext == null)
            throw new ArgumentNullException(nameof(itemContext));

        KeyValuePair<string, List<MapPair>> pair = (KeyValuePair<string, List<MapPair>>)itemContext.RepeatKey;

        store.Mappings.Add(pair.Key, pair.Value);
    }
    #endregion

    #region Repeating Map
    // these are the repeating "Map" elements
    // <Mappings...><Map><From/><To/></Map><Map><From/><To/></Map></Mappings>
    private IEnumerator<MapPair>? MapEnumerator;

    private static RepeatContext<MapStore>.RepeatItemContext CreateMapRepeatContext(
        MapStore store,
        Element<MapStore> element,
        RepeatContext<MapStore>.RepeatItemContext? parent)
    {
        if (store.Mappings.Count > 0 && store.MapEnumerator != null)
        {
            // we are enumerating the map list, return the map pair
            return new RepeatContext<MapStore>.RepeatItemContext(
                element,
                parent,
                store.MapEnumerator.Current);
        }

        return new RepeatContext<MapStore>.RepeatItemContext(element, parent, new MapPair());
    }

    private static bool AreRemainingMaps(MapStore store, RepeatContext<MapStore>.RepeatItemContext? itemContext)
    {
        if (store.MappingsEnumerator == null)
            return false;

        store.MapEnumerator ??= store.MappingsEnumerator.Current.Value.GetEnumerator();

        bool remaining = store.MapEnumerator.MoveNext();
        if (!remaining)
            store.MapEnumerator = null;

        return remaining;
    }

    private static void CommitMapRepeatItem(MapStore store, RepeatContext<MapStore>.RepeatItemContext? itemContext)
    {
        if (itemContext == null)
            throw new ArgumentNullException(nameof(itemContext));

        MapPair pair = (MapPair)itemContext.RepeatKey;

        ((KeyValuePair<string, List<MapPair>>)itemContext.Parent!.RepeatKey).Value.Add(pair);
    }

    #endregion

    public void WriteStore()
    {
        MapEnumerator = null;
        MappingsEnumerator = null;

        using WriteFile<MapStore> file = WriteFile<MapStore>.CreateSettingsFile(XmlMapStoreDescription, App.MapStorePath, this);

        file.SerializeSettings(XmlMapStoreDescription, this);
    }
}
