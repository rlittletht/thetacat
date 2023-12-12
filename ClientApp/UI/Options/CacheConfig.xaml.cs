using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
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

namespace Thetacat.UI.Options;

/// <summary>
/// Interaction logic for CacheConfig.xaml
/// </summary>
public partial class CacheConfig : UserControl
{
    readonly CacheConfigModel _Model = new CacheConfigModel();
    private IAppState? m_appState;

    private IAppState _AppState
    {
        get
        {
            if (m_appState == null)
                throw new Exception($"initialize never called on {this.GetType().Name}");
            return m_appState;
        }
    }

    public CacheConfig()
    {
        InitializeComponent();
        DataContext = _Model;
    }

    public void Initialize(IAppState appState)
    {
        m_appState = appState;
    }


    public void LoadFromSettings()
    {
        _Model.CacheLocation = _AppState.Settings.CacheLocation;
        _Model.CacheType = _AppState.Settings.CacheType;
    }

    public void SaveToSettings()
    {
        _AppState.Settings.CacheLocation = _Model.CacheLocation;
        _AppState.Settings.CacheType = _Model.CacheType;
    }

}
