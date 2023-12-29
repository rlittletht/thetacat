using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat.Migration;

/// <summary>
/// Interaction logic for Migration.xaml
/// </summary>
public partial class Migration : Window, INotifyPropertyChanged
{
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

    public Migration()
    {
        if (MainWindow._AppState.MetatagSchema.SchemaVersionWorking == 0)
            MainWindow._AppState.RefreshMetatagSchema();

        InitializeComponent();
        DataContext = this;
        ElementsDb = MainWindow._AppState.Settings.ElementsDatabase ?? string.Empty;

        MainWindow._AppState.RegisterWindowPlace(this, "Migration");
    }

    private void LaunchElementsMigration(object sender, RoutedEventArgs e)
    {
        SaveSettingsIfNeeded();
        MigrationManager elements = new MigrationManager(ElementsDb);
        elements.Show();
        this.Close();
    }

    private void SaveSettingsIfNeeded()
    {
        PathSegment path = new PathSegment(ElementsDb);

        if (MainWindow._AppState.Settings.ElementsDatabase != path)
        {
            MainWindow._AppState.Settings.ElementsDatabase = path;
            MainWindow._AppState.Settings.WriteSettings();
        }
    }
}
