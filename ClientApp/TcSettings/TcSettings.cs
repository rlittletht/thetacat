using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using TCore.XmlSettings;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

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
    public string? ElementsDatabase;
    public string? CacheLocation;
    public string? CacheType;
    public string? WorkgroupId;
    public string? WorkgroupCacheServer;
    public string? WorkgroupCacheRoot;
    public string? WorkgroupName;

    public string? AzureStorageAccount;
    public string? StorageContainer;
    public string? SqlConnection;

    public bool? ShowAsyncLogOnStart;
    public bool? ShowAppLogOnStart;
    public string? ExplorerItemSize;

    public string? TimelineType;

    public List<MapPair> ElementsSubstitutions = new();
    public Dictionary<string, Rectangle> Placements { get; private set; } = new();
    private IEnumerator<KeyValuePair<string, Rectangle>>? PlacementsEnumerator { get; set; }

    public TcSettings()
    {
#pragma warning disable format // @formatter:off
        XmlSettingsDescription =
            XmlDescriptionBuilder<TcSettings>
               .Build(s_uri, "Settings")
                  .AddChildElement("Options")
                     .AddChildElement("ShowAsyncLogOnStart")
                      .AddAttribute("value", GetShowAsyncLogOnStart, SetShowAsyncLogOnStart)
                     .AddElement("ShowAppLogOnStart")
                      .AddAttribute("value", GetShowAppLogOnStart, SetShowAppLogOnStart)
                     .AddElement("MediaExplorer")
                      .AddAttribute("ItemSize", GetExplorerItemSize, SetExplorerItemSize)
                  .Pop()
               .Pop()
                  .AddChildElement("View")
                    .AddChildElement("Explorer")
                        .AddChildElement("Timeline")
                            .AddAttribute("Type", (settings, _) => settings.TimelineType, (settings, value, _) => settings.TimelineType = value)
                        .Pop()
                    .Pop()
                  .Pop()
                  .AddChildElement("Account")
                     .AddChildElement("AzureStorageAccount", (settings, _) => settings.AzureStorageAccount, (settings, value, _) => settings.AzureStorageAccount = value)
                     .AddElement("StorageContainer", (settings, _) => settings.StorageContainer, (settings, value, _) => settings.StorageContainer = value)
                     .AddElement("SqlConnection", GetSqlConnection, SetSqlConnection)
                  .Pop()
               .Pop()
                  .AddChildElement("Migration")
                     .AddChildElement("ElementsDatabase", GetElementsDatabaseValue, SetElementsDatabaseValue)
                     .AddElement("Substitutions")
                        .AddChildElement("Substitution")
                         .SetRepeating(
                              TcSettings.CreateElementsSubstitutionRepeatContext,
                              TcSettings.AreRemainingElementsSubstitutions,
                              TcSettings.CommitElementsSubstitutionRepeatItem)
                         .AddAttribute("From", GetSubstitutionFrom, SetSubstitutionFrom)
                         .AddAttribute("To", GetSubstitutionTo, SetSubstitutionTo)
                     .Pop()
                  .Pop()
               .AddElement("CacheOptions")
                  .AddChildElement("CacheType")
                   .AddAttribute("Type", GetCacheTypeValue, SetCacheTypeValue)
                  .AddElement("PrivateCache")
                     .AddChildElement("CacheLocation", GetCacheLocationValue, SetCacheLocationValue)
                  .Pop()
                  .AddElement("WorkgroupCache")
                     .AddChildElement("Workgroup")
                      .AddAttribute("ID", GetWorkgroupIDValue, SetWorkgroupIDValue)
                     .AddElement("CachedValues")
                      .AddAttribute("Server", GetWorkgroupCacheServerValue, SetWorkgroupCacheServerValue)
                      .AddAttribute("CacheRoot", GetWorkgroupCacheRootValue, SetWorkgroupCacheRootValue)
                      .AddAttribute("Name", GetWorkgroupNameValue, SetWorkgroupNameValue)
                  .Pop()
               .Pop()
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

        using WriteFile<TcSettings> file = WriteFile<TcSettings>.CreateSettingsFile(XmlSettingsDescription, App.SettingsPath, this);

        file.SerializeSettings(XmlSettingsDescription, this);
    }

    #region Xml Option Reading/Writing Support

    private readonly XmlDescription<TcSettings> XmlSettingsDescription;

    private static void SetShowAsyncLogOnStart(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ShowAsyncLogOnStart = bool.Parse(value ?? bool.FalseString);
    private static string? GetShowAsyncLogOnStart(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ShowAsyncLogOnStart.ToString();

    private static void SetStorageAccountNameValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.AzureStorageAccount = value;
    private static string? GetStorageAccountNameValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.AzureStorageAccount;

    private static void SetStorageContainerValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.StorageContainer = value;
    private static string? GetStorageContainerValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.StorageContainer;

    private static void SetSqlConnection(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.SqlConnection = value;
    private static string? GetSqlConnection(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.SqlConnection;

    private static void SetShowAppLogOnStart(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ShowAppLogOnStart = bool.Parse(value ?? bool.FalseString);
    private static string? GetShowAppLogOnStart(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ShowAppLogOnStart.ToString();

    private static void SetExplorerItemSize(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ExplorerItemSize = value;
    private static string? GetExplorerItemSize(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ExplorerItemSize;

    private static void SetElementsDatabaseValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ElementsDatabase = value;
    private static string? GetElementsDatabaseValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.ElementsDatabase;

    private static void SetCacheLocationValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.CacheLocation = value;
    private static string? GetCacheLocationValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.CacheLocation;

    private static void SetCacheTypeValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.CacheType = value;
    private static string? GetCacheTypeValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.CacheType;

    private static void   SetWorkgroupIDValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupId = value;
    private static string? GetWorkgroupIDValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupId;

    private static void   SetWorkgroupCacheServerValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupCacheServer = value;
    private static string? GetWorkgroupCacheServerValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupCacheServer;

    private static void   SetWorkgroupCacheRootValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupCacheRoot = value;
    private static string? GetWorkgroupCacheRootValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupCacheRoot;

    private static void   SetWorkgroupNameValue(TcSettings settings, string? value, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupName = value;
    private static string? GetWorkgroupNameValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? repeatItemContext) => settings.WorkgroupName;

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
        if (settings.ElementsSubstitutions.Count == 0)
            return false;

        settings.SubstitutionsEnumerator ??= settings.ElementsSubstitutions.GetEnumerator();

        return settings.SubstitutionsEnumerator.MoveNext();
    }

    private static void CommitElementsSubstitutionRepeatItem(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (itemContext == null)
            throw new ArgumentNullException(nameof(itemContext));

        MapPair nested = (MapPair)itemContext.RepeatKey;

        settings.ElementsSubstitutions.Add(nested);
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
        if (settings.PlacementsEnumerator == null)
            settings.PlacementsEnumerator = settings.Placements.GetEnumerator();

        return settings.PlacementsEnumerator.MoveNext();
    }

    public static void CommitPlacementsItem(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (itemContext == null)
            throw new ArgumentNullException(nameof(itemContext));

        KeyValuePair<string, Rectangle> pair = ((KeyValuePair<string, Rectangle>)itemContext.RepeatKey);
        if (settings.Placements == null)
            settings.Placements = new Dictionary<string, Rectangle>();

        // where can we store the name away? its not part of the rectangle...have to squirrel it away somewhere...
        settings.Placements.Add(pair.Key, pair.Value);
    }

    #endregion

    #endregion

    #endregion
}
