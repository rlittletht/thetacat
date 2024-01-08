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
            _Model.StorageAccount = App.State.Settings.AzureStorageAccount  ?? string.Empty;
            _Model.Container = App.State.Settings.StorageContainer ?? string.Empty;
            _Model.SqlConnection = App.State.Settings.SqlConnection ?? string.Empty;
        }

        public bool FSaveSettings()
        {
            App.State.Settings.AzureStorageAccount = _Model.StorageAccount;
            App.State.Settings.StorageContainer = _Model.Container;
            App.State.Settings.SqlConnection = _Model.SqlConnection;

            return true;
        }
    }
    }
