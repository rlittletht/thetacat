using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Types;

namespace Thetacat.Migration;

/// <summary>
/// Interaction logic for Migration.xaml
/// </summary>
public partial class Migration : Window, INotifyPropertyChanged
{
    readonly IAppState m_appState;

    private string m_elementsDb = string.Empty;

    public string ElementsDb
    {
        get => m_elementsDb;
        set => SetField(ref m_elementsDb, value);
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public Migration(IAppState appState)
    {
        m_appState = appState;
        InitializeComponent();
        DataContext = this;
        ElementsDb = m_appState.Settings.Settings.SValue("LastElementsDb");

        m_appState.RegisterWindowPlace(this, "Migration");
    }

    private void LaunchElementsMigration(object sender, RoutedEventArgs e)
    {
        SaveSettingsIfNeeded();
        MigrationManager elements = new MigrationManager(ElementsDb, m_appState);
        elements.ShowDialog();
    }

    private void SaveSettingsIfNeeded()
    {
        if (m_appState.Settings.Settings.SValue("LastElementsDb") != ElementsDb)
        {
            m_appState.Settings.Settings.SetSValue("LastElementsDb", ElementsDb);
            m_appState.Settings.Settings.Save();
        }
    }
}
