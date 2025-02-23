using Emgu.CV.Dnn;
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
using Thetacat.Secrets;
using Thetacat.ServiceClient;
using Thetacat.TcSettings;
using Thetacat.Types;
using Thetacat.Util;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Thetacat.BackupRestore.Restore;

/// <summary>
/// Interaction logic for ConfirmRestoreTargets.xaml
/// </summary>
public partial class ConfirmRestoreTargets : Window
{
    private ConfirmRestoreTargetsModel Model = new();
    Dictionary<Guid, List<ServiceWorkgroup>> Workgroups = new();

    /*----------------------------------------------------------------------------
        %%Function: ConfirmRestoreTargets
        %%Qualified: Thetacat.BackupRestore.Restore.ConfirmRestoreTargets.ConfirmRestoreTargets
    ----------------------------------------------------------------------------*/
    public ConfirmRestoreTargets()
    {
        DataContext = Model;
        InitializeComponent();
        App.State.RegisterWindowPlace(this, "ConfirmTargets");

        Model.PropertyChanged += Model_PropertyChanged;
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateForTargetProfile
        %%Qualified: Thetacat.BackupRestore.Restore.ConfirmRestoreTargets.UpdateForTargetProfile
    ----------------------------------------------------------------------------*/
    void UpdateForTargetProfile(string profile)
    {
        Profile p = App.State.Settings.Profiles[profile];

        Model.ReferenceCatalogID = p.CatalogID.ToString();
        Model.ReferenceSqlConnection = p.SqlConnection!;
        Model.ReferenceWorkgroupName = p.WorkgroupName!;
        Model.ReferenceWorkgroupId = p.WorkgroupId!;

        EnsureWorkgroupsForCatalog(Model.ReferenceSqlConnection, p.CatalogID);
        Model.ReferenceWorkgroupName = GetWorkgroup(p.CatalogID, Guid.Parse(Model.ReferenceWorkgroupId)).Name
            ?? "<Workgroup Not Found>";
    }

    /*----------------------------------------------------------------------------
        %%Function: EnsureWorkgroupsForCatalog
        %%Qualified: Thetacat.BackupRestore.Restore.ConfirmRestoreTargets.EnsureWorkgroupsForCatalog
    ----------------------------------------------------------------------------*/
    void EnsureWorkgroupsForCatalog(string? sqlConnection, Guid catalogID)
    {
        if (!Workgroups.ContainsKey(catalogID))
        {
            if (sqlConnection != null)
                App.State.PushTemporarySqlConnection(sqlConnection);
            Workgroups.Add(catalogID, ServiceInterop.GetAvailableWorkgroups(catalogID));
            if (sqlConnection != null)
                App.State.PopTemporarySqlConnection();
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: GetWorkgroup
        %%Qualified: Thetacat.BackupRestore.Restore.ConfirmRestoreTargets.GetWorkgroup
    ----------------------------------------------------------------------------*/
    ServiceWorkgroup GetWorkgroup(Guid catalogID, Guid workgroupId)
    {
        return Workgroups[catalogID].First(wg => wg.ID == workgroupId);
    }

    /*----------------------------------------------------------------------------
        %%Function: Model_PropertyChanged
        %%Qualified: Thetacat.BackupRestore.Restore.ConfirmRestoreTargets.Model_PropertyChanged
    ----------------------------------------------------------------------------*/
    private void Model_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ReferenceProfile")
        {
            UpdateForTargetProfile(Model.ReferenceProfile);
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: DoCancel
        %%Qualified: Thetacat.BackupRestore.Restore.ConfirmRestoreTargets.DoCancel
    ----------------------------------------------------------------------------*/
    private void DoCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /*----------------------------------------------------------------------------
        %%Function: DoRestore
        %%Qualified: Thetacat.BackupRestore.Restore.ConfirmRestoreTargets.DoRestore
    ----------------------------------------------------------------------------*/
    private void DoRestore(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    /*----------------------------------------------------------------------------
        %%Function: ConfirmAndGetReference
        %%Qualified: Thetacat.BackupRestore.Restore.ConfirmRestoreTargets.ConfirmAndGetReference
    ----------------------------------------------------------------------------*/
    public static bool ConfirmAndGetReference(Window? owner, out Profile? referenceProfile, out string? exportGuidMapPath)
    {
        ConfirmRestoreTargets dialog = new ConfirmRestoreTargets();
        dialog.Owner = owner;
        dialog.Model.TargetSqlConnection = AppSecrets.MasterSqlConnectionString;
        dialog.Model.TargetWorkgroupId = App.State.ActiveProfile.WorkgroupId!;
        dialog.Model.TargetCatalogID = App.State.ActiveProfile.CatalogID.ToString();
        dialog.Model.TargetProfile = App.State.ActiveProfile.Name!;
        dialog.Model.Profiles.AddRange(App.State.Settings.Profiles.Keys);

        dialog.EnsureWorkgroupsForCatalog(null, App.State.ActiveProfile.CatalogID);
        dialog.Model.TargetWorkgroupName = dialog.GetWorkgroup(App.State.ActiveProfile.CatalogID, Guid.Parse(App.State.ActiveProfile.WorkgroupId!)).Name
            ?? "<Workgroup Not Found>";

        if (dialog.ShowDialog() == true)
        {
            referenceProfile = App.State.Settings.Profiles[dialog.Model.ReferenceProfile];
            if (!string.IsNullOrWhiteSpace(dialog.Model.GuidMapExportPath))
                exportGuidMapPath = dialog.Model.GuidMapExportPath;
            else
                exportGuidMapPath = null;
            return true;
        }

        referenceProfile = null;
        exportGuidMapPath = null;

        return false;
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
}
