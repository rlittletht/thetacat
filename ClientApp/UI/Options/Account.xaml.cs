using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace Thetacat.UI.Options
{
    /// <summary>
    /// Interaction logic for Account.xaml
    /// </summary>
    public partial class Account : UserControl
    {
        public readonly AccountModel _Model = new AccountModel();

        public Account()
        {
            InitializeComponent();
            DataContext = _Model;
        }

        public void LoadFromSettings(CatOptionsModel optionsModel)
        {
            _Model.CurrentProfile = optionsModel.CurrentProfile;
            _Model.StorageAccount = _Model.CurrentProfile?.Profile.AzureStorageAccount  ?? string.Empty;
            _Model.Container = _Model.CurrentProfile?.Profile.StorageContainer ?? string.Empty;
            _Model.SqlConnection = _Model.CurrentProfile?.Profile.SqlConnection ?? string.Empty;
        }

        public bool FSaveSettings()
        {
            if (_Model.CurrentProfile != null)
            {
                _Model.CurrentProfile.Profile.AzureStorageAccount = _Model.StorageAccount;
                _Model.CurrentProfile.Profile.StorageContainer = _Model.Container;
                _Model.CurrentProfile.Profile.SqlConnection = _Model.SqlConnection;
            }

            return true;
        }
    }
    }
