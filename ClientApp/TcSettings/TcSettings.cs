using Emgu.CV.Ocl;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TCore.PostfixText;
using TCore.XmlSettings;
using Thetacat.Filtering;
using Thetacat.Types;

namespace Thetacat.TcSettings;

public class TcSettings
{
    public class MapPair
    {
        public string From = string.Empty;
        public string To = string.Empty;
    }

    // Public Settings. Use LoadSettings

    public static string s_uri = "http://schemas.thetasoft.com/Thetacat/settings/2023";
    public Dictionary<string, Rectangle> Placements { get; private set; } = new();
    public string LastExportPath = string.Empty;
    public Dictionary<string, Profile> Profiles = new();

    public TcSettings()
    {
#pragma warning disable format // @formatter:off
        #region XML Descriptor
        XmlSettingsDescription =
        XmlDescriptionBuilder<TcSettings>
            .Build(s_uri, "Settings")
                .AddChildElement("Profile")
                .AddAttribute("Name", (_, context) => context!.GetDictionaryValue<string, Profile>().Name, (_, value, context) => context!.SetDictionaryKey<string, Profile>(context!.GetDictionaryValue<string, Profile>().Name = value))
                .AddAttribute("Default", (_, context) => context!.GetDictionaryValue<string, Profile>().Default.ToString(), (_, value, context) => context!.GetDictionaryValue<string, Profile>().Default = bool.Parse(value))
                .SetRepeating(CreateProfilesRepeatContext, AreRemainingProfiles, CommitProfilesRepeatItem)
                    .AddChildElement("Options")
                        .AddChildElement("ShowAsyncLogOnStart")
                        .AddAttribute("value", (_, context) => context!.GetDictionaryValue<string, Profile>().ShowAsyncLogOnStart.ToString(), (_, value, context) => context!.GetDictionaryValue<string, Profile>().ShowAsyncLogOnStart = bool.Parse(value))
                        .AddElement("ShowAppLogOnStart")
                        .AddAttribute("value", (_, context) => context!.GetDictionaryValue<string, Profile>().ShowAppLogOnStart.ToString(), (_, value, context) => context!.GetDictionaryValue<string, Profile>().ShowAppLogOnStart = bool.Parse(value))
                        .AddElement("MediaExplorer")
                        .AddAttribute("ItemSize", (_, context) => context!.GetDictionaryValue<string, Profile>().ExplorerItemSize, (_, value, context) => context!.GetDictionaryValue<string, Profile>().ExplorerItemSize = value)
                        .Pop()
                    .Pop()
                    .AddChildElement("View")
                        .AddChildElement("Explorer")
                            .AddChildElement("Timeline")
                            .AddAttribute("Type", (_, context) => context!.GetDictionaryValue<string, Profile>().TimelineType, (_, value, context) => context!.GetDictionaryValue<string, Profile>().TimelineType = value)
                            .AddAttribute("Order", (_, context) => context!.GetDictionaryValue<string, Profile>().TimelineOrder, (_, value, context) => context!.GetDictionaryValue<string, Profile>().TimelineOrder = value)
                            .AddElement("MetatagMru")
                                .AddChildElement("Tag", (_, context) => (string?)context?.RepeatKey, (_, value, context) => context!.RepeatKey = value ?? "")
                                .SetRepeating(CreateMetatagMruRepeatContext, AreRemainingMetatagMru, CommitMetatagMruRepeatItem, (settings) => settings.MetatagMruEnumerator = null)
                                .Pop()
                            .Pop()
                        .Pop()
                    .Pop()
                    .AddChildElement("Account")
                        .AddChildElement("AzureStorageAccount", (_, context) => context!.GetDictionaryValue<string, Profile>().AzureStorageAccount, (_, value, context) => context!.GetDictionaryValue<string, Profile>().AzureStorageAccount = value)
                        .AddElement("StorageContainer", (_, context) => context!.GetDictionaryValue<string, Profile>().StorageContainer, (_, value, context) => context!.GetDictionaryValue<string, Profile>().StorageContainer = value)
                        .AddElement("SqlConnection", (_, context) => context!.GetDictionaryValue<string, Profile>().SqlConnection, (_, value, context) => context!.GetDictionaryValue<string, Profile>().SqlConnection = value)
                        .AddElement("CatalogID", (_, context) => context!.GetDictionaryValue<string, Profile>().CatalogID.ToString(), (_, value, context) => context!.GetDictionaryValue<string, Profile>().CatalogID = Guid.Parse(value ?? Guid.Empty.ToString()))
                        .Pop()
                    .Pop()
                    .AddChildElement("Filters")
                    .AddAttribute("DefaultFilter", (_, context) => context!.GetDictionaryValue<string, Profile>().DefaultFilterName, (_, value, context) => context!.GetDictionaryValue<string, Profile>().DefaultFilterName = value)
                        .AddChildElement("Filter")
                        .SetRepeating(CreateFiltersRepeatContext, AreRemainingFilters, CommitFiltersRepeatItem, (settings) => settings.FiltersEnumerator = null)
                        .AddAttribute("Name", (_, context) => context!.GetDictionaryValue<string, FilterDefinition>().FilterName, (_, value, context) => context!.GetDictionaryValue<string, FilterDefinition>().FilterName = value)
                            .AddChildElement("Description", (_, context) => context!.GetDictionaryValue<string, FilterDefinition>().Description, (_, value, context) => context!.GetDictionaryValue<string, FilterDefinition>().Description = value ?? "")
                            .AddElement("Expression", (_, context) => context!.GetDictionaryValue<string, FilterDefinition>().ExpressionText, (_, value, context) => context!.GetDictionaryValue<string, FilterDefinition>().ExpressionText = value ?? "")
                            .Pop()
                        .Pop()
                    .Pop()
                    .AddChildElement("Migration")
                        .AddChildElement("ElementsDatabase", (_, context) => context!.GetDictionaryValue<string, Profile>().ElementsDatabase, (_, value, context) => context!.GetDictionaryValue<string, Profile>().ElementsDatabase = value)
                        .AddElement("Substitutions")
                            .AddChildElement("Substitution")
                            .SetRepeating(CreateElementsSubstitutionRepeatContext, AreRemainingElementsSubstitutions, CommitElementsSubstitutionRepeatItem, (settings) => settings.SubstitutionsEnumerator = null)
                            .AddAttribute("From", (_, context) => ((MapPair)context!.RepeatKey).From, (_, value, context) => ((MapPair)context!.RepeatKey).From = value)
                            .AddAttribute("To", (_, context) => ((MapPair)context!.RepeatKey).To, (_, value, context) => ((MapPair)context!.RepeatKey).To = value)
                            .Pop()
                        .Pop()
                    .AddElement("CacheOptions")
                        .AddChildElement("Client")
                            .AddChildElement("DerivativeCache", (_, context) => context!.GetDictionaryValue<string, Profile>().DerivativeCache, (_, value, context) => context!.GetDictionaryValue<string, Profile>().DerivativeCache = value)
                            .AddElement("ClientDatabase", (_, context) => context!.GetDictionaryValue<string, Profile>().ClientDatabaseName, (_, value, context) => context!.GetDictionaryValue<string, Profile>().ClientDatabaseName = value ?? "")
                            .Pop()
                        .Pop()
                        .AddChildElement("CacheType")
                        .AddAttribute("Type", (_, context) => context!.GetDictionaryValue<string, Profile>().CacheType, (_, value, context) => context!.GetDictionaryValue<string, Profile>().CacheType = value)
                        .AddElement("PrivateCache")
                            .AddChildElement("CacheLocation", (_, context) => context!.GetDictionaryValue<string, Profile>().CacheLocation, (_, value, context) => context!.GetDictionaryValue<string, Profile>().CacheLocation = value)
                            .Pop()
                        .AddElement("WorkgroupCache")
                            .AddChildElement("Workgroup")
                            .AddAttribute("ID", (_, context) => context!.GetDictionaryValue<string, Profile>().WorkgroupId, (_, value, context) => context!.GetDictionaryValue<string, Profile>().WorkgroupId = value)
                            .AddElement("CachedValues")
                            .AddAttribute("Server", (_, context) => context!.GetDictionaryValue<string, Profile>().WorkgroupCacheServer, (_, value, context) => context!.GetDictionaryValue<string, Profile>().WorkgroupCacheServer = value)
                            .AddAttribute("CacheRoot", (_, context) => context!.GetDictionaryValue<string, Profile>().WorkgroupCacheRoot, (_, value, context) => context!.GetDictionaryValue<string, Profile>().WorkgroupCacheRoot = value)
                            .AddAttribute("Name", (_, context) => context!.GetDictionaryValue<string, Profile>().WorkgroupName, (_, value, context) => context!.GetDictionaryValue<string, Profile>().WorkgroupName = value)
                            .Pop()
                        .Pop()
                    .Pop()
            .AddElement("LastExportPath", (settings, _) => settings.LastExportPath, (settings, value, _) => settings.LastExportPath = value ?? "")
            .AddElement("WindowPlacements")
            .AddChildElement("Placement")
            .SetRepeating(CreatePlacementRepeatItem,AreRemainingPlacementItems,CommitPlacementsItem)
            .AddAttribute("Name", GetPlacementName, SetPlacementName)
            .AddAttribute("X", GetPlacementX, SetPlacementX)
            .AddAttribute("Y", GetPlacementY, SetPlacementY)
            .AddAttribute("Width", GetPlacementWidth, SetPlacementWidth)
            .AddAttribute("Height", GetPlacementHeight, SetPlacementHeight)
            .Pop();
        #endregion
#pragma warning restore format // @formatter: on
        try
        {
            using ReadFile<TcSettings> file = ReadFile<TcSettings>.CreateSettingsFile(App.SettingsPath);
            file.DeSerialize(XmlSettingsDescription, this);
        }
        catch (Exception ex) when
            (ex is DirectoryNotFoundException
             || ex is FileNotFoundException)
        {
            // this is fine, we just don't have any options to load
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Caught exception reading settings: {ex.Message}");
        }
    }

    public void WriteSettings()
    {
        SubstitutionsEnumerator = null;
        PlacementsEnumerator = null;
        MetatagMruEnumerator = null;
        using WriteFile<TcSettings> file = WriteFile<TcSettings>.CreateSettingsFile(XmlSettingsDescription, App.SettingsPath, this);

        file.SerializeSettings(XmlSettingsDescription, this);
    }

    #region Xml Option Reading/Writing Support

    private readonly XmlDescription<TcSettings> XmlSettingsDescription;

    #region MetatagMru
    private IEnumerator<string>? MetatagMruEnumerator;

    private static RepeatContext<TcSettings>.RepeatItemContext CreateMetatagMruRepeatContext(
        TcSettings settings,
        Element<TcSettings> element,
        RepeatContext<TcSettings>.RepeatItemContext? parent)
    {
        if (settings.MetatagMruEnumerator != null)
        {
            return new RepeatContext<TcSettings>.RepeatItemContext(
                element,
                parent,
                settings.MetatagMruEnumerator.Current);
        }

        return new RepeatContext<TcSettings>.RepeatItemContext(element, parent, "");
    }

    private static bool AreRemainingMetatagMru(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (itemContext == null)
            throw new CatExceptionInternalFailure("no contexts in metatag remaining");

        if (itemContext.GetDictionaryValue<string, Profile>().MetatagMru.Count == 0)
            return false;

        settings.MetatagMruEnumerator ??= itemContext.GetDictionaryValue<string, Profile>().MetatagMru.GetEnumerator();

        return settings.MetatagMruEnumerator.MoveNext();
    }

    private static void CommitMetatagMruRepeatItem(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (itemContext == null || itemContext.Parent == null)
            throw new ArgumentNullException(nameof(itemContext));

        string nested = (string)itemContext.RepeatKey;

        itemContext.Parent.GetDictionaryValue<string, Profile>().MetatagMru.Add(nested);
    }

    #endregion

    #region Profiles
    private IEnumerator<KeyValuePair<string, Profile>>? ProfilesEnumerator;

    private static RepeatContext<TcSettings>.RepeatItemContext CreateProfilesRepeatContext(
        TcSettings settings,
        Element<TcSettings> element,
        RepeatContext<TcSettings>.RepeatItemContext? parent)
    {
        if (settings.ProfilesEnumerator != null)
        {
            return new RepeatContext<TcSettings>.RepeatItemContext(
                element,
                parent,
                settings.ProfilesEnumerator.Current);
        }

        return new RepeatContext<TcSettings>.RepeatItemContext(element, parent, new KeyValuePair<string, Profile>("", new Profile()));
    }

    private static bool AreRemainingProfiles(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (settings.Profiles.Count == 0)
            return false;

        settings.ProfilesEnumerator ??= settings.Profiles.GetEnumerator();

        bool remaining = settings.ProfilesEnumerator.MoveNext();
        if (!remaining)
            settings.ProfilesEnumerator = null;

        return remaining;
    }
    
    private static void CommitProfilesRepeatItem(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (itemContext == null)
            throw new ArgumentNullException(nameof(itemContext));

        KeyValuePair<string, Profile> pair = (KeyValuePair<string, Profile>)itemContext.RepeatKey;

        settings.Profiles.Add(pair.Key, pair.Value);
    }

    #endregion

    #region Filters
    private IEnumerator<KeyValuePair<string, FilterDefinition>>? FiltersEnumerator;

    private static RepeatContext<TcSettings>.RepeatItemContext CreateFiltersRepeatContext(
        TcSettings settings,
        Element<TcSettings> element,
        RepeatContext<TcSettings>.RepeatItemContext? parent)
    {
        if (settings.FiltersEnumerator != null)
        {
            return new RepeatContext<TcSettings>.RepeatItemContext(
                element,
                parent,
                settings.FiltersEnumerator.Current);
        }

        return new RepeatContext<TcSettings>.RepeatItemContext(element, parent, new KeyValuePair<string, FilterDefinition>("", new FilterDefinition()));
    }

    private static bool AreRemainingFilters(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (itemContext== null)
            throw new CatExceptionInternalFailure("no contexts for remaining filters check");

        if (itemContext.GetDictionaryValue<string, Profile>().ElementsSubstitutions.Count == 0)
            return false;

        settings.FiltersEnumerator ??= itemContext.GetDictionaryValue<string, Profile>().Filters.GetEnumerator();

        bool remaining = settings.FiltersEnumerator.MoveNext();
        if (!remaining)
            settings.FiltersEnumerator = null;

        return remaining;
    }

    private static void CommitFiltersRepeatItem(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (itemContext?.Parent == null)
            throw new ArgumentNullException(nameof(itemContext));

        KeyValuePair<string, FilterDefinition> pair = (KeyValuePair<string, FilterDefinition>)itemContext.RepeatKey;

        itemContext.Parent.GetDictionaryValue<string, Profile>().Filters.Add(pair.Value.FilterName, pair.Value);
    }

    #endregion
    
    #region Substitutions
    private IEnumerator<MapPair>? SubstitutionsEnumerator;

    private static RepeatContext<TcSettings>.RepeatItemContext CreateElementsSubstitutionRepeatContext(
        TcSettings settings,
        Element<TcSettings> element,
        RepeatContext<TcSettings>.RepeatItemContext? parent)
    {
        if (settings.SubstitutionsEnumerator != null)
        {
            return new RepeatContext<TcSettings>.RepeatItemContext(
                element,
                parent,
                settings.SubstitutionsEnumerator.Current);
        }

        return new RepeatContext<TcSettings>.RepeatItemContext(element, parent, new MapPair());
    }

    private static bool AreRemainingElementsSubstitutions(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (itemContext == null)
            throw new CatExceptionInternalFailure("no contexts in remaining elements substitutions check");

        if (itemContext.GetDictionaryValue<string, Profile>().ElementsSubstitutions.Count == 0)
            return false;
        
        settings.SubstitutionsEnumerator ??= itemContext.GetDictionaryValue<string, Profile>().ElementsSubstitutions.GetEnumerator();

        return settings.SubstitutionsEnumerator.MoveNext();
    }

    private static void CommitElementsSubstitutionRepeatItem(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (itemContext!.Parent == null)
            throw new ArgumentNullException(nameof(itemContext));

        MapPair nested = (MapPair)itemContext.RepeatKey;

        itemContext.Parent.GetDictionaryValue<string, Profile>().ElementsSubstitutions.Add(nested);
    }

    #endregion

    #region Placements
    private IEnumerator<KeyValuePair<string, Rectangle>>? PlacementsEnumerator { get; set; }

    public static string? GetPlacementX(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) =>
        ((KeyValuePair<string, Rectangle>?)repeatItemContext?.RepeatKey)?.Value.X.ToString();

    public static string? GetPlacementY(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) =>
        ((KeyValuePair<string, Rectangle>?)repeatItemContext?.RepeatKey)?.Value.Y.ToString();

    public static string? GetPlacementWidth(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) =>
        ((KeyValuePair<string, Rectangle>?)repeatItemContext?.RepeatKey)?.Value.Width.ToString();

    public static string? GetPlacementHeight(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) =>
        ((KeyValuePair<string, Rectangle>?)repeatItemContext?.RepeatKey)?.Value.Height.ToString();

    public static void SetPlacementX(
        TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext)
    {
        if (repeatItemContext == null)
            throw new ArgumentNullException(nameof(repeatItemContext));

        KeyValuePair<string, Rectangle> pair = (KeyValuePair<string, Rectangle>)repeatItemContext.RepeatKey;

        repeatItemContext.RepeatKey =
            new KeyValuePair<string, Rectangle>(
                pair.Key,
                pair.Value with { X = int.Parse(value) });
    }

    public static void SetPlacementY(
        TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext)
    {
        if (repeatItemContext == null)
            throw new ArgumentNullException(nameof(repeatItemContext));

        KeyValuePair<string, Rectangle> pair = (KeyValuePair<string, Rectangle>)repeatItemContext.RepeatKey;

        repeatItemContext.RepeatKey =
            new KeyValuePair<string, Rectangle>(
                pair.Key,
                pair.Value with { Y = int.Parse(value) });
    }

    public static void SetPlacementWidth(
        TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext)
    {
        if (repeatItemContext == null)
            throw new ArgumentNullException(nameof(repeatItemContext));

        KeyValuePair<string, Rectangle> pair = (KeyValuePair<string, Rectangle>)repeatItemContext.RepeatKey;

        repeatItemContext.RepeatKey =
            new KeyValuePair<string, Rectangle>(
                pair.Key,
                pair.Value with { Width = int.Parse(value) });
    }

    public static void SetPlacementHeight(
        TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext)
    {
        if (repeatItemContext == null)
            throw new ArgumentNullException(nameof(repeatItemContext));

        KeyValuePair<string, Rectangle> pair = (KeyValuePair<string, Rectangle>)repeatItemContext.RepeatKey;

        repeatItemContext.RepeatKey =
            new KeyValuePair<string, Rectangle>(
                pair.Key,
                pair.Value with { Height = int.Parse(value) });
    }

    public static void SetPlacementName(TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext)
    {
        if (repeatItemContext == null)
            throw new ArgumentNullException(nameof(repeatItemContext));

        // we can't assign to Key directly, so create a new key/value pair with the new key and the existing value
        repeatItemContext.RepeatKey =
            new KeyValuePair<string, Rectangle>(
                value,
                ((KeyValuePair<string, Rectangle>)repeatItemContext.RepeatKey).Value);
    }

    public static string? GetPlacementName(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) =>
        ((KeyValuePair<string, Rectangle>?)repeatItemContext?.RepeatKey)?.Key;

    #region Placement Repeaters

    public static RepeatContext<TcSettings>.RepeatItemContext CreatePlacementRepeatItem(
        TcSettings settings,
        Element<TcSettings> element,
        RepeatContext<TcSettings>.RepeatItemContext? parent)
    {
        if (settings.PlacementsEnumerator != null)
        {
            // we are enumerating the placements. return a key/value pair for the current item
            return new RepeatContext<TcSettings>.RepeatItemContext(
                element,
                parent,
                new KeyValuePair<string, Rectangle>(settings.PlacementsEnumerator.Current.Key, settings.Placements[settings.PlacementsEnumerator.Current.Key]));
        }

        return new RepeatContext<TcSettings>.RepeatItemContext(element, parent, new KeyValuePair<string, Rectangle>());
    }

    public static bool AreRemainingPlacementItems(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        settings.PlacementsEnumerator ??= settings.Placements.GetEnumerator();

        return settings.PlacementsEnumerator.MoveNext();
    }

    public static void CommitPlacementsItem(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (itemContext == null)
            throw new CatExceptionInternalFailure("no context for commit placements");

        KeyValuePair<string, Rectangle> pair = ((KeyValuePair<string, Rectangle>)itemContext.RepeatKey);

        // where can we store the name away? its not part of the rectangle...have to squirrel it away somewhere...
        settings.Placements.Add(pair.Key, pair.Value);
    }

    #endregion

    #endregion

    #endregion
}
