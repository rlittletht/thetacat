using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Security.Principal;
using System.Windows.Controls;
using Thetacat.ServiceClient;
using Thetacat.Util;

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

        public void LoadCatalogDefinitions()
        {
            _Model.CatalogDefinitions.Clear();
            App.State.PushTemporarySqlConnection(_Model.SqlConnection);
            Guid? catalogID = _Model.CatalogDefinition?.ID;

            _Model.CatalogDefinitions.AddRange(ServiceInterop.GetCatalogDefinitions());
            MatchCatalogDefinition(catalogID);

            App.State.PopTemporarySqlConnection();
        }

        private void MatchCatalogDefinition(Guid? catalogID)
        {
            _Model.CatalogDefinition = null;
            foreach (ServiceCatalogDefinition item in _Model.CatalogDefinitions)
            {
                if (item.ID == catalogID)
                {
                    _Model.CatalogDefinition = item;
                    return;
                }
            }
        }

        public Guid CatalogID => _Model.CatalogDefinition?.ID ?? Guid.Empty;

        public void LoadFromSettings(CatOptionsModel optionsModel)
        {
            _Model.CurrentProfile = optionsModel.CurrentProfile;
            _Model.StorageAccount = _Model.CurrentProfile?.Profile.AzureStorageAccount  ?? string.Empty;
            _Model.Container = _Model.CurrentProfile?.Profile.StorageContainer ?? string.Empty;
            _Model.SqlConnection = _Model.CurrentProfile?.Profile.SqlConnection ?? string.Empty;
            LoadCatalogDefinitions();
            MatchCatalogDefinition(_Model.CurrentProfile?.Profile.CatalogID);
        }

        public bool FSaveSettings()
        {
            if (_Model.CurrentProfile != null)
            {
                _Model.CurrentProfile.Profile.AzureStorageAccount = _Model.StorageAccount;
                _Model.CurrentProfile.Profile.StorageContainer = _Model.Container;
                _Model.CurrentProfile.Profile.SqlConnection = _Model.SqlConnection;
                _Model.CurrentProfile.Profile.CatalogID = _Model.CatalogDefinition?.ID ?? Guid.Empty;
            }

            return true;
        }
    }
    }
