using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;
using TCore.XmlSettings;

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

    public List<MapPair> ElementsSubstitutions = new();

    public TcSettings()
    {
        XmlSettingsDescription =
            XmlDescriptionBuilder<TcSettings>
               .Build(s_uri, "Settings")
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
               .Pop();

        try
        {
            using ReadFile<TcSettings> file = ReadFile<TcSettings>.CreateSettingsFile(m_settingsPath);
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
        using WriteFile<TcSettings> file = WriteFile<TcSettings>.CreateSettingsFile(XmlSettingsDescription, m_settingsPath, this);

        file.SerializeSettings(XmlSettingsDescription, this);
    }

    #region Xml Option Reading/Writing Support

    private readonly string m_settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "thetacat\\options.xml");
    private readonly XmlDescription<TcSettings> XmlSettingsDescription;

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

    private IEnumerator<MapPair>? SubEnum;


    private static RepeatContext<TcSettings>.RepeatItemContext CreateElementsSubstitutionRepeatContext(
        TcSettings settings,
        Element<TcSettings> element,
        RepeatContext<TcSettings>.RepeatItemContext? parent)
    {
        if (settings.SubEnum != null)
        {
            return new RepeatContext<TcSettings>.RepeatItemContext(
                element,
                parent,
                settings.SubEnum.Current);
        }

        return new RepeatContext<TcSettings>.RepeatItemContext(element, parent, new MapPair());
    }

    private static bool AreRemainingElementsSubstitutions(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext? itemContext)
    {
        if (settings.ElementsSubstitutions.Count == 0)
            return false;

        settings.SubEnum ??= settings.ElementsSubstitutions.GetEnumerator();

        return settings.SubEnum.MoveNext();
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
}
