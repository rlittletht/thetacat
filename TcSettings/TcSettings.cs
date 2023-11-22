using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCore.Settings;

namespace Thetacat.TcSettings;

public class TcSettings
{
    string s_registryRoot = "Software\\Thetasoft\\Thetacat";
    private Settings? m_settings;

    private Settings.SettingsElt[] s_appSettings =
    {
        new Settings.SettingsElt("LastElementsDb", Settings.Type.Str, "", ""),
        new Settings.SettingsElt("LastElementsSubstitutions", Settings.Type.StrArray, Array.Empty<string>(), Array.Empty<string>())
    };

    public static TcSettings LoadSettings()
    {
        TcSettings settings = new TcSettings();

        settings.m_settings = new Settings(settings.s_appSettings, settings.s_registryRoot, "app");
        settings.m_settings.Load();

        return settings;
    }

    public Settings Settings => m_settings;
}