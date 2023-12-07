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
using Thetacat.Migration.Elements.Metadata.UI;
using Thetacat.ServiceClient.LocalService;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Metadata.UI;


/// <summary>
/// Interaction logic for MigrationManager.xaml
/// </summary>
public partial class MigrationManager : Window
{
    private readonly IAppState m_appState;
    private MetatagMigrate m_migrate;

    void SwitchToSummaryTab()
    {
        Dispatcher.BeginInvoke((Action)((() => MigrationTabs.SelectedIndex = 3)));
    }

    void BuildMetadataReportFromDatabase(string database)
    {
        ElementsDb db = ElementsDb.Create(database);

        MediaMigrationTab.Initialize(m_appState, db, m_migrate);
        MetatagMigrationTab.Initialize(m_appState, db, m_migrate, new UserMetatagMigration.SwitchToSummaryDelegate(SwitchToSummaryTab));
        MetadataMigrationTab.Initialize(m_appState, db, m_migrate);
        MetadataMigrateSummaryTab.Initialize(m_appState, m_migrate);

        db.Close();
    }

    public MigrationManager(string database, IAppState appState)
    {
        m_appState = appState;
        InitializeComponent();

        m_migrate = new MetatagMigrate();
        BuildMetadataReportFromDatabase(database);
        m_appState.RegisterWindowPlace(this, "ElementsMigrationManager");
    }

    private void OnMetatagMigrateSummaryTabSelected(object sender, RoutedEventArgs e)
    {
        MetadataMigrateSummaryTab.RebuildSchemaDiff();
    }
}
