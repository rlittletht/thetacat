using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Thetacat.Migration.Elements.Metadata.UI;
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
        if (App.State.MetatagSchema.SchemaVersionWorking == 0)
            App.State.RefreshMetatagSchema(App.State.ActiveProfile.CatalogID);

        InitializeComponent();
        DataContext = this;
        ElementsDb = App.State.ActiveProfile.ElementsDatabase ?? string.Empty;

        App.State.RegisterWindowPlace(this, "Migration");
    }

    private void LaunchElementsMigration(object sender, RoutedEventArgs e)
    {
        SaveSettingsIfNeeded();
        MigrationManager elements = new(ElementsDb);
        elements.Owner = this.Owner;
        elements.Show();
        this.Close();
    }

    private void SaveSettingsIfNeeded()
    {
        PathSegment path = new PathSegment(ElementsDb);

        if (App.State.ActiveProfile.ElementsDatabase != path)
        {
            App.State.ActiveProfile.ElementsDatabase = path;
            App.State.Settings.WriteSettings();
        }
    }
}
