using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public string ElementsDatabase = string.Empty;
    public string CacheLocation = string.Empty;
    public string CacheType = string.Empty;

    public List<MapPair> ElementsSubstitutions = new();

    public TcSettings()
    {
        XmlSettingsDescription =
            XmlDescriptionBuilder<TcSettings>
               .Build(s_uri, "settings")
               .AddChildElement("migration")
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
               .AddChildElement("CacheLocation", GetCacheLocationValue, SetCacheLocationValue)
               .AddAttribute("Type", GetCacheTypeValue, SetCacheTypeValue)
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
    }

    public void WriteSettings()
    {
        using WriteFile<TcSettings> file = WriteFile<TcSettings>.CreateSettingsFile(XmlSettingsDescription, m_settingsPath, this);

        file.SerializeSettings(XmlSettingsDescription, this);
    }

    #region Xml Option Reading/Writing Support

    private readonly string m_settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "thetacat\\options.xml");
    private readonly XmlDescription<TcSettings> XmlSettingsDescription;

    private static void SetElementsDatabaseValue(TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext repeatItemContext) => settings.ElementsDatabase = value;
    private static string GetElementsDatabaseValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext repeatItemContext) => settings.ElementsDatabase;

    private static void SetCacheLocationValue(TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext repeatItemContext) => settings.CacheLocation = value;
    private static string GetCacheLocationValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext repeatItemContext) => settings.CacheLocation;

    private static void SetCacheTypeValue(TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext repeatItemContext) => settings.CacheType = value;
    private static string GetCacheTypeValue(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext repeatItemContext) => settings.CacheType;

    private IEnumerator<MapPair>? SubEnum;


    private static RepeatContext<TcSettings>.RepeatItemContext CreateElementsSubstitutionRepeatContext(
        TcSettings settings,
        Element<TcSettings> element,
        RepeatContext<TcSettings>.RepeatItemContext parent)
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

    private static bool AreRemainingElementsSubstitutions(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext itemContext)
    {
        if (settings.ElementsSubstitutions.Count == 0)
            return false;

        settings.SubEnum ??= settings.ElementsSubstitutions.GetEnumerator();

        return settings.SubEnum.MoveNext();
    }

    private static void CommitElementsSubstitutionRepeatItem(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext itemContext)
    {
        MapPair nested = (MapPair)itemContext.RepeatKey;
        settings.ElementsSubstitutions ??= new List<MapPair>();

        settings.ElementsSubstitutions.Add(nested);
    }

    private static string GetSubstitutionFrom(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext context) => ((MapPair)context.RepeatKey).From;

    private static void SetSubstitutionFrom(TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext context) =>
        ((MapPair)context.RepeatKey).From = value;

    private static string GetSubstitutionTo(TcSettings settings, RepeatContext<TcSettings>.RepeatItemContext context) => ((MapPair)context.RepeatKey).To;

    private static void SetSubstitutionTo(TcSettings settings, string value, RepeatContext<TcSettings>.RepeatItemContext context) =>
        ((MapPair)context.RepeatKey).To = value;

#endregion
}
