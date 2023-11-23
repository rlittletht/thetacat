using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCore.Settings;

namespace Thetacat.TcSettings;

public class TcSettings
{
    readonly string s_registryRoot = "Software\\Thetasoft\\Thetacat";
    private Settings? m_settings;

    private readonly Settings.SettingsElt[] s_appSettings =
    {
        new("LastElementsDb", Settings.Type.Str, "", ""),
        new("LastElementsSubstitutions", Settings.Type.StrArray, Array.Empty<string>(), Array.Empty<string>())
    };

    public static TcSettings LoadSettings()
    {
        TcSettings settings = new();

        settings.m_settings = new Settings(settings.s_appSettings, settings.s_registryRoot, "app");
        settings.m_settings.Load();

        return settings;
    }

    public Settings Settings
    {
        get
        {
            if (m_settings == null) 
                throw new Exception("improper creation of TcSettings");
            return m_settings;
        }
    }
}