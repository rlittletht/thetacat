using System.Windows;

namespace Thetacat.UI.Options;

/// <summary>
/// Interaction logic for Options.xaml
/// </summary>
public partial class CatOptions : Window
{
    public CatOptions()
    {
        InitializeComponent();
        CacheConfigTab.LoadFromSettings();
        AccountTab.LoadFromSettings();
        App.State.RegisterWindowPlace(this, "CatOptions");
    }

    public void SaveToSettings()
    {
        if (!CacheConfigTab.FSaveSettings())
            MessageBox.Show("Failed to save Cache options");
        if (!AccountTab.FSaveSettings())
            MessageBox.Show("Failed to save account options");

        App.State.Settings.WriteSettings();
    }

    private void DoSave(object sender, RoutedEventArgs e)
    {
        // launcher will interrogate us and save the settings if we return true.
        DialogResult = true;
    }
}