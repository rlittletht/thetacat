using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Thetacat.Migration.Elements.Media;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Metadata.UI;


/// <summary>
/// Interaction logic for MigrationManager.xaml
/// </summary>
public partial class MigrationManager : Window
{
    private readonly IAppState m_appState;
    private ElementsMigrate m_migrate;

    void SwitchToSummaryTab()
    {
        Dispatcher.BeginInvoke((Action)((() => MigrationTabs.SelectedIndex = 3)));
    }

    void ReloadSchemas()
    {
        MetatagMigrationTab.RefreshForSchemaChange();
        MetadataMigrationTab.RefreshForSchemaChange();
    }

    void BuildMetadataReportFromDatabase(string database)
    {
        ElementsDb db = ElementsDb.Create(database);

        MetatagMigrationTab.Initialize(m_appState, db, m_migrate);
        MetadataMigrationTab.Initialize(m_appState, db, m_migrate);
        MetadataMigrateSummaryTab.Initialize(m_appState, m_migrate);
        MediaMigrationTab.Initialize(m_appState, db, m_migrate);

        m_migrate.MediaMigrate.SetMediaStacks(db.ReadMediaStacks());
        
        db.Close();
    }

    public MigrationManager(string database, IAppState appState)
    {
        m_appState = appState;
        InitializeComponent();

        m_migrate = new ElementsMigrate(
            new MetatagMigrate(SwitchToSummaryTab, ReloadSchemas),
            new MediaMigrate());

        BuildMetadataReportFromDatabase(database);
        m_appState.RegisterWindowPlace(this, "ElementsMigrationManager");
    }

    private void OnMetatagMigrateSummaryTabSelected(object sender, RoutedEventArgs e)
    {
        MetadataMigrateSummaryTab.RebuildSchemaDiff();
    }
}
