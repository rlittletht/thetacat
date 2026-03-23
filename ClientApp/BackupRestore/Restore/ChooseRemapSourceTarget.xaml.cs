using Microsoft.WindowsAPICodePack.Dialogs;
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
using System.Windows.Shapes;
using Thetacat.TcSettings;
using Thetacat.Util;

namespace Thetacat.BackupRestore.Restore
{
    /// <summary>
    /// Interaction logic for ChooseRemapSourceTarget.xaml
    /// </summary>
    public partial class ChooseRemapSourceTarget : Window
    {
        ChooseRemapSourceTargetModel Model = new();

        public ChooseRemapSourceTarget()
        {
            DataContext = Model;
            UpdateForTargetProfile(Model.TargetProfile);

            InitializeComponent();
            App.State.RegisterWindowPlace(this, "ConfirmTargets");

            Model.PropertyChanged += Model_PropertyChanged;
        }

        void UpdateForSourceProfile(string profile)
        {
            Profile p = App.State.Settings.Profiles[profile];

            Model.SourceCatalogID = p.CatalogID.ToString();
            Model.SourceSqlConnection = p.SqlConnection!;
            Model.SourceWorkgroupName = p.WorkgroupName!;
            Model.SourceWorkgroupId = p.WorkgroupId!;
            Model.SourceAzureStorage = p.AzureStorageAccount!;
            Model.SourceAzureContainer = p.StorageContainer!;
        }

        void UpdateForTargetProfile(string profile)
        {
            try
            {
                Profile p = App.State.Settings.Profiles[profile];

                Model.TargetCatalogID = p.CatalogID.ToString();
                Model.TargetSqlConnection = p.SqlConnection!;
                Model.TargetWorkgroupName = p.WorkgroupName!;
                Model.TargetWorkgroupId = p.WorkgroupId!;
                Model.TargetAzureStorage = p.AzureStorageAccount!;
                Model.TargetAzureContainer = p.StorageContainer!;
            }
            catch
            {
                Model.TargetCatalogID = string.Empty;
                Model.TargetSqlConnection = string.Empty;
                Model.TargetWorkgroupName = string.Empty;
                Model.TargetWorkgroupId = string.Empty;
                Model.TargetAzureStorage = string.Empty;
                Model.TargetAzureContainer = string.Empty;
            }
        }

        private void Model_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SourceProfile")
            {
                UpdateForSourceProfile(Model.SourceProfile);
            }
        }

        private void BrowseForPath(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.Title = "Choose export file";
            dlg.IsFolderPicker = false;
            dlg.InitialDirectory = Model.GuidMapExportPath;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = "";
            dlg.EnsureFileExists = false;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Model.GuidMapExportPath = dlg.FileName;
            }
        }

        private void DoOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void DoCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public static bool GetRemapRestoreTargetInfo(
            Window? owner, out string? sourceProfile, out string? guidMapFile, out bool migrateAzureBlobs, out bool migrateWorkgroup)
        {
            sourceProfile = null;
            guidMapFile = null;
            migrateAzureBlobs = false;
            migrateWorkgroup = false;

            ChooseRemapSourceTarget dialog = new();

            dialog.Model.Profiles.AddRange(App.State.Settings.Profiles.Keys);
            dialog.Model.TargetProfile = App.State.ActiveProfile.Name!;
            dialog.UpdateForTargetProfile(App.State.ActiveProfile.Name!);
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                sourceProfile = dialog.Model.SourceProfile;
                guidMapFile = dialog.Model.GuidMapExportPath;
                migrateAzureBlobs = dialog.Model.MigrateAzureBlobs;
                migrateWorkgroup = dialog.Model.MigrateWorkgroup;

                return true;
            }

            return false;
        }
    }
}
