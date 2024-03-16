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
            Guid catalogID = _Model.CurrentCatalogID;

            _Model.CatalogDefinitions.AddRange(ServiceInterop.GetCatalogDefinitions());
            _Model.CurrentCatalogID = catalogID;

            App.State.PopTemporarySqlConnection();
        }

        public Guid CatalogID => _Model.CurrentCatalogID;

        public void LoadFromSettings(CatOptionsModel optionsModel)
        {
            _Model.CurrentProfile = optionsModel.CurrentProfile;
            _Model.StorageAccount = _Model.CurrentProfile?.Profile.AzureStorageAccount  ?? string.Empty;
            _Model.Container = _Model.CurrentProfile?.Profile.StorageContainer ?? string.Empty;
            _Model.CurrentCatalogID = _Model.CurrentProfile?.Profile.CatalogID ?? Guid.Empty;
            _Model.SqlConnection = _Model.CurrentProfile?.Profile.SqlConnection ?? string.Empty;
        }

        public bool FSaveSettings()
        {
            if (_Model.CurrentProfile != null)
            {
                _Model.CurrentProfile.Profile.AzureStorageAccount = _Model.StorageAccount;
                _Model.CurrentProfile.Profile.StorageContainer = _Model.Container;
                _Model.CurrentProfile.Profile.SqlConnection = _Model.SqlConnection;

                if (_Model.CreateNewCatalog)
                {
                    ServiceCatalogDefinition newCatalog = new ServiceCatalogDefinition(_Model.CurrentCatalogID, _Model.CatalogName, _Model.CatalogDescription);

                    App.State.PushTemporarySqlConnection(_Model.SqlConnection);
                    ServiceInterop.AddCatalogDefinition(newCatalog);
                    App.State.PopTemporarySqlConnection();
                    _Model.CatalogDefinition = newCatalog;
                    _Model.CatalogDefinitions.Add(newCatalog);
                }

                _Model.CurrentProfile.Profile.CatalogID = _Model.CatalogDefinition?.ID ?? Guid.Empty;
            }

            return true;
        }
    }
    }
