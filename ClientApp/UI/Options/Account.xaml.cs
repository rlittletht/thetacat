using System.Windows.Controls;

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
            _Model.StorageAccount = App.State.ActiveProfile.AzureStorageAccount  ?? string.Empty;
            _Model.Container = App.State.ActiveProfile.StorageContainer ?? string.Empty;
            _Model.SqlConnection = App.State.ActiveProfile.SqlConnection ?? string.Empty;
        }

        public bool FSaveSettings()
        {
            App.State.ActiveProfile.AzureStorageAccount = _Model.StorageAccount;
            App.State.ActiveProfile.StorageContainer = _Model.Container;
            App.State.ActiveProfile.SqlConnection = _Model.SqlConnection;

            return true;
        }
    }
    }
