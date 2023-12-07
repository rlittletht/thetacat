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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Metadata.UI;

/// <summary>
/// Interaction logic for MetadataMigrateSummary.xaml
/// </summary>
public partial class MetadataMigrateSummary : UserControl
{
    private IAppState? m_appState;
    private MetatagMigrate? m_migrate;

    public MetadataMigrateSummary()
    {
        InitializeComponent();
    }

    public void Initialize(IAppState appState, MetatagMigrate migrate)
    {
        m_migrate = migrate;
        m_appState = appState;
    }

    public void RebuildSchemaDiff()
    {
        // build the schema differences for all the metadata and metatag migration tabs

    }
}