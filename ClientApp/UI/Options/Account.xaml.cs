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
using Thetacat.ServiceClient;

namespace Thetacat.UI.Options
{
    /// <summary>
    /// Interaction logic for Account.xaml
    /// </summary>
    public partial class Account : UserControl
    {
        readonly AccountModel _Model = new AccountModel();

        public Account()
        {
            InitializeComponent();
            DataContext = _Model;
        }

        public void LoadFromSettings()
        {
            _Model.StorageAccount = MainWindow._AppState.Settings.AzureStorageAccount  ?? string.Empty;
            _Model.Container = MainWindow._AppState.Settings.StorageContainer ?? string.Empty;
            _Model.SqlConnection = MainWindow._AppState.Settings.SqlConnection ?? string.Empty;
        }

        public bool FSaveSettings()
        {
            MainWindow._AppState.Settings.AzureStorageAccount = _Model.StorageAccount;
            MainWindow._AppState.Settings.StorageContainer = _Model.Container;
            MainWindow._AppState.Settings.SqlConnection = _Model.SqlConnection;

            return true;
        }
    }
    }
