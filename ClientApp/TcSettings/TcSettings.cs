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
//    public string? ElementsDatabase;
//    public string? CacheLocation;
//    public string? CacheType;
//    public string? WorkgroupId;
//    public string? WorkgroupCacheServer;
//    public string? WorkgroupCacheRoot;
//    public string? WorkgroupName;
//
//    public string? AzureStorageAccount;
//    public string? StorageContainer;
//    public string? SqlConnection;
//
//    public bool? ShowAsyncLogOnStart;
//    public bool? ShowAppLogOnStart;
//    public string? ExplorerItemSize;
//
//    public string? TimelineType;
//    public string? TimelineOrder;
//
//    public string? DerivativeCache;
//
//    public List<string> MetatagMru = new();
//    public List<MapPair> ElementsSubstitutions = new();
    public Dictionary<string, Rectangle> Placements { get; private set; } = new();
    private IEnumerator<KeyValuePair<string, Rectangle>>? PlacementsEnumerator { get; set; }

//    public string? DefaultFilterName;

//    public Dictionary<string, FilterDefinition> Filters = new();
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
                                .SetRepeating(CreateMetatagMruRepeatContext, AreRemainingMetatagMru, CommitMetatagMruRepeatItem)
                               .Pop()
                            .Pop()
                        .Pop()
                      .Pop()
                      .AddChildElement("Account")
                         .AddChildElement("AzureStorageAccount", (_, context) => context!.GetDictionaryValue<string, Profile>().AzureStorageAccount, (_, value, context) => context!.GetDictionaryValue<string, Profile>().AzureStorageAccount = value)
                         .AddElement("StorageContainer", (_, context) => context!.GetDictionaryValue<string, Profile>().StorageContainer, (_, value, context) => context!.GetDictionaryValue<string, Profile>().StorageContainer = value)
                         .AddElement("SqlConnection", (_, context) => context!.GetDictionaryValue<string, Profile>().SqlConnection, (_, value, context) => context!.GetDictionaryValue<string, Profile>().SqlConnection = value)
                      .Pop()
                   .Pop()
                      .AddChildElement("Filters")
                         .AddAttribute("DefaultFilter", (_, context) => context!.GetDictionaryValue<string, Profile>().DefaultFilterName, (_, value, context) => context!.GetDictionaryValue<string, Profile>().DefaultFilterName = value)
                         .AddChildElement("Filter")
                           .SetRepeating(CreateFiltersRepeatContext, AreRemainingFilters, CommitFiltersRepeatItem)
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
                             .SetRepeating(CreateElementsSubstitutionRepeatContext, AreRemainingElementsSubstitutions, CommitElementsSubstitutionRepeatItem)
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
                 .Pop() //////// LEFT OFF HERE...
               .AddElement("WindowPlacements")
                  .AddChildElement("Placement")
                   .SetRepeating(
                        TcSettings.CreatePlacementRepeatItem,
                        TcSettings.AreRemainingPlacementItems,
                        TcSettings.CommitPlacementsItem)
                   .AddAttribute("Name", TcSettings.GetPlacementName, TcSettings.SetPlacementName)
                   .AddAttribute("X", TcSettings.GetPlacementX, TcSettings.SetPlacementX)
                   .AddAttribute("Y", TcSettings.GetPlacementY, TcSettings.SetPlacementY)
                   .AddAttribute("Width", TcSettings.GetPlacementWidth, TcSettings.SetPlacementWidth)
                   .AddAttribute("Height", TcSettings.GetPlacementHeight, TcSettings.SetPlacementHeight)
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

//    private static void SetShowAsyncLogOnStart(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ShowAsyncLogOnStart = bool.Parse(value ?? bool.FalseString);
//    private static string? GetShowAsyncLogOnStart(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ShowAsyncLogOnStart.ToString();
//
//    private static void SetStorageAccountNameValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.AzureStorageAccount = value;
//    private static string? GetStorageAccountNameValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.AzureStorageAccount;
//
//    private static void SetStorageContainerValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.StorageContainer = value;
//    private static string? GetStorageContainerValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.StorageContainer;

//    private static void SetSqlConnection(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.SqlConnection = value;
//    private static string? GetSqlConnection(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.SqlConnection;
//
//    private static void SetShowAppLogOnStart(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ShowAppLogOnStart = bool.Parse(value ?? bool.FalseString);
//    private static string? GetShowAppLogOnStart(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ShowAppLogOnStart.ToString();
//
//    private static void SetExplorerItemSize(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ExplorerItemSize = value;
//    private static string? GetExplorerItemSize(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ExplorerItemSize;
//
//    private static void SetElementsDatabaseValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ElementsDatabase = value;
//    private static string? GetElementsDatabaseValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ElementsDatabase;
//
//    private static void SetCacheLocationValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.CacheLocation = value;
//    private static string? GetCacheLocationValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.CacheLocation;
//
//    private static void SetCacheTypeValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.CacheType = value;
//    private static string? GetCacheTypeValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.CacheType;
//
//    private static void   SetWorkgroupIDValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupId = value;
//    private static string? GetWorkgroupIDValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupId;
//
//    private static void   SetWorkgroupCacheServerValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupCacheServer = value;
//    private static string? GetWorkgroupCacheServerValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupCacheServer;
//
//    private static void   SetWorkgroupCacheRootValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupCacheRoot = value;
//    private static string? GetWorkgroupCacheRootValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupCacheRoot;
//
//    private static void   SetWorkgroupNameValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupName = value;
//    private static string? GetWorkgroupNameValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupName;

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

//    private static string GetMetatagMruItem(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? context) => ((string?)context?.RepeatKey) ?? throw new ArgumentNullException(nameof(context));
//    private static void SetMetatagMruItem(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? context)
//    {
//        if (context?.RepeatKey == null) 
//            throw new ArgumentNullException(nameof(context));
//
//        (context.RepeatKey) = value ?? "";
//    }

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

    private static string GetProfileName(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? context) =>
       ((KeyValuePair<string, Profile>?)context?.RepeatKey)?.Key ?? "";

    private static void SetProfileName(TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext? context)
    {
        if (context?.RepeatKey == null)
            throw new ArgumentNullException(nameof(context));

        // have to make a new KVP since we are going to reset the key
        KeyValuePair<string, Profile> pair = (KeyValuePair<string, Profile>)context.RepeatKey;

        context.RepeatKey = new KeyValuePair<string, Profile>(value, pair.Value);
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

    private static string GetFilterName(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? context) =>
       ((KeyValuePair<string, FilterDefinition>?)context?.RepeatKey)?.Value.FilterName ?? "";

    private static void SetFilterName(TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext? context)
    {
        if (context?.RepeatKey == null)
            throw new ArgumentNullException(nameof(context));

        KeyValuePair<string, FilterDefinition> pair = (KeyValuePair<string, FilterDefinition>)context.RepeatKey;

        pair.Value.FilterName = value;
    }

    private static string GetFilterDescription(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? context) =>
       ((KeyValuePair<string, FilterDefinition>?)context?.RepeatKey)?.Value.Description ?? "";

    private static void SetFilterDescription(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? context)
    {
        if (context?.RepeatKey == null)
            throw new ArgumentNullException(nameof(context));

        KeyValuePair<string, FilterDefinition> pair = (KeyValuePair<string, FilterDefinition>)context.RepeatKey;

        pair.Value.Description = value ?? "";
    }

    private static string GetFilterExpressionText(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? context) =>
       ((KeyValuePair<string, FilterDefinition>?)context?.RepeatKey)?.Value.ExpressionText ?? "";

    private static void SetFilterExpressionText(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? context)
    {
        if (context?.RepeatKey == null)
            throw new ArgumentNullException(nameof(context));

        KeyValuePair<string, FilterDefinition> pair = (KeyValuePair<string, FilterDefinition>)context.RepeatKey;

        pair.Value.ExpressionText = value ?? "";
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

    private static string GetSubstitutionFrom(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? context) => ((MapPair?)context?.RepeatKey)?.From ?? throw new ArgumentNullException(nameof(context));
    private static void SetSubstitutionFrom(TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext? context)
    {
        if (context?.RepeatKey == null) 
            throw new ArgumentNullException(nameof(context));

        ((MapPair)context.RepeatKey).From = value;
    }

    private static string GetSubstitutionTo(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? context) => ((MapPair?)context?.RepeatKey)?.To ?? throw new ArgumentNullException(nameof(context));

    private static void SetSubstitutionTo(TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext? context)
    {
        if (context == null) 
            throw new ArgumentNullException(nameof(context));

        ((MapPair)context.RepeatKey).To = value;
    }
    #endregion

    #region Placements

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
