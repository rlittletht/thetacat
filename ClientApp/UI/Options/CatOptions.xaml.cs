using System;
using System.Collections.Generic;
using System.Linq;
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
using Thetacat.Types;

namespace Thetacat.UI.Options;

/// <summary>
/// Interaction logic for Options.xaml
/// </summary>
public partial class CatOptions : Window
{
    private readonly IAppState m_appState;

    public CatOptions(IAppState appState)
    {
        InitializeComponent();
        m_appState = appState;
        CacheConfigTab.Initialize(appState);
        CacheConfigTab.LoadFromSettings();

        m_appState.RegisterWindowPlace(this, "CatOptions");
    }

    public void SaveToSettings()
    {
        if (CacheConfigTab.FSaveSettings())
            m_appState.Settings.WriteSettings();
    }

    private void DoSave(object sender, RoutedEventArgs e)
    {
        // launcher will interrogate us and save the settings if we return true.
        DialogResult = true;
    }
}