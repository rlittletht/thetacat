using Emgu.CV.Dnn;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Thetacat.Export;
using Thetacat.Metatags.Model;
using Thetacat.Model;
using Thetacat.ServiceClient;
using Thetacat.ServiceClient.LocalService;
using Thetacat.TcSettings;
using Thetacat.Types;
using Thetacat.Util;
using MessageBox = System.Windows.MessageBox;

namespace Thetacat.BackupRestore.Restore
{
    /// <summary>
    /// Interaction logic for RestoreData.xaml
    /// </summary>
    public partial class RestoreData : Window
    {
        private readonly RestoreDataModel m_model = new();

        public RestoreData()
        {
            InitializeComponent();
            m_model.CatalogDefinitions.AddRange(ServiceInterop.GetCatalogDefinitions());
            DataContext = m_model;
            m_model.PropertyChanged += ModelPropertyChanged;
            App.State.RegisterWindowPlace(this, "import-data");
        }

        private void ModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CreateNewCatalog")
            {
                if (m_model.CreateNewCatalog)
                    m_model.CatalogID = RT.Comb.Provider.Sql.Create().ToString();
                else
                    m_model.CatalogID = m_model.CatalogDefinition?.ID.ToString() ?? string.Empty;
            }
            else if (e.PropertyName == "CurrentRestoreBehavior")
            {
                m_model.CreateNewCatalog = m_model.CurrentRestoreBehavior == "Create New";
            }
            else if (e.PropertyName == "CatalogDefinition")
            {
                if (m_model.CatalogDefinition != null)
                {
                    m_model.CatalogName = m_model.CatalogDefinition.Name;
                    m_model.CatalogID = m_model.CatalogDefinition.ID.ToString();
                    m_model.CatalogDescription = m_model.CatalogDefinition.Description;
                    m_model.CurrentRestoreBehavior = "Replace";
                    m_model.CreateNewCatalog = false;
                }
            }

        }

        private void BrowseForPath(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.Title = "Choose export file";
            dlg.IsFolderPicker = false;
            dlg.InitialDirectory = m_model.RestorePath;

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
                m_model.RestorePath = dlg.FileName;
            }
        }

        private void DoCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void DoImport(object sender, RoutedEventArgs e)
        {
            if (m_fullRestoreData == null)
            {
                MessageBox.Show("Cannot restore empty dataset");
                return;
            }

            string destination = $"{Secrets.AppSecrets.MasterSqlConnectionString}";
            Guid catalogID;

            bool fClearBeforeRestore = false;

            FullExportRestore restore = m_fullRestoreData;
            GuidMaps idMaps = new();

            if (m_model.RegenerateIds)
            {
                if (m_model.ImportWorkgroups)
                {
                    MessageBox.Show("Cannot regenerate IDs when importing workgroups");
                    return;
                }

                if (m_model.CurrentRestoreBehavior != "Replace")
                {
                    MessageBox.Show("Regenerate IDs requires an existing Workgroup + Catalog to restore into (with behavior = REPLACE)");
                    return;
                }

                if (restore.CatalogRestore == null)
                {
                    MessageBox.Show("Cannot regenerate IDs without a catalog");
                    return;
                }

                if (restore.CatalogRestore.Schema.MetatagCount == 0)
                {
                    MessageBox.Show("Cannot regenerate IDs without a schema");
                    return;
                }

                // TODO: Change this. We require a target profile and a reference profile. The target profile is the current profile, and the
                // reference profile should be listed and made available to select. This means we don't have to have any special selection code, etc.

                if (MessageBox.Show(
                        $"Regenerating ids and migrating requires that you have already created the target profile with a new SQL database and workgroup to restore into. "
                        + $"(NOTE: You will want a new database since the whole point of regenerating IDs is to optimize the indexes, and restoring into an existing "
                        + $"database will just perpetuate the fragmented indexes.\n\nYou will be prompted for the reference profile in order to "
                        + $"properly migrate Non-Cloud media to the new workgroup."
                        + $"\n\nDo you want to continue?",
                        "Restore data",
                        MessageBoxButton.OKCancel)
                    == MessageBoxResult.Cancel)
                {
                    return;
                }

                if (!ConfirmRestoreTargets.ConfirmAndGetReference(this, out Profile? referenceProfile, out string? exportGuidMapPath))
                    return;

                if (exportGuidMapPath == null && m_fullRestoreData.WorkgroupDataRestore == null)
                {
                    if (MessageBox.Show(
                            "ID mapping will not be saved and there is no Workgroup data in this backup\n\nYou will not be able to restore a workgroup at a later time without saving the ID mapping.\n\nDo you want to continue?",
                            "Restore data",
                            MessageBoxButton.OKCancel)
                        == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                restore = idMaps.RemapFullRestore(restore);

            }

            // first figure out what we're restoring to
            if (m_model.CurrentRestoreBehavior == "Create New")
            {
                // make sure we have all the info we need
                if (!Guid.TryParse(m_model.CatalogID, out catalogID))
                {
                    MessageBox.Show("Must provide a valid GUID for the catalog ID");
                    return;
                }

                if (string.IsNullOrWhiteSpace(m_model.CatalogName))
                {
                    MessageBox.Show($"Catalog name '{m_model.CatalogName}' is not valid.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(m_model.CatalogDescription))
                {
                    MessageBox.Show($"Catalog description '{m_model.CatalogDescription}' is not valid.");
                    return;
                }

                ServiceCatalogDefinition def = new(catalogID, m_model.CatalogName, m_model.CatalogDescription);
                ServiceInterop.AddCatalogDefinition(def);
            }
            else
            {
                if (m_model.CatalogDefinition == null)
                {
                    MessageBox.Show("Must specify a catalog to append or replace to");
                    return;
                }

                catalogID = m_model.CatalogDefinition.ID;
            }

            if (m_model.CurrentRestoreBehavior == "Append")
            {
                if (MessageBox.Show(
                        $"This will APPEND the restored data to {destination}\n\nContinue with restore?",
                        "Restore data",
                        MessageBoxButton.OKCancel)
                    == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            else
            {
                if (MessageBox.Show(
                        $"This will DELETE ALL DATA in the database '{destination}', catalog '{catalogID}' before restoring the data. THIS CANNOT BE UNDONE.\n\nContinue with restore?",
                        "Restore data",
                        MessageBoxButton.OKCancel)
                    == MessageBoxResult.Cancel)
                {
                    return;
                }

                fClearBeforeRestore = true;
            }

            // now (maybe clear) and restore all the items they selected
            if (m_model.ImportSchema)
            {
                // clear if request
                if (restore.CatalogRestore == null || restore.CatalogRestore.Schema.MetatagCount == 0)
                {
                    MessageBox.Show("Can't restore from empty catalog or schema");
                }
                else
                {
                    if (fClearBeforeRestore)
                        ServiceInterop.ResetMetatagSchema(catalogID);

                    // load the base schema
                    ServiceMetatagSchema serviceSchema = ServiceInterop.GetMetatagSchema(catalogID);
                    restore.CatalogRestore.Schema.ReadNewBaseFromService(serviceSchema);

                    restore.CatalogRestore.Schema.UpdateServer(
                        catalogID,
                        (count) => MessageBox.Show(
                                $"Updating {count} schema items. Proceed?",
                                "Restore Data",
                                MessageBoxButton.OKCancel)
                            == MessageBoxResult.OK);
                }
            }

            if (m_model.ImportWorkgroups)
            {
                if ((restore.WorkgroupsRestore?.Workgroups.Count ?? 0) == 0)
                {
                    MessageBox.Show("Can't restore an empty set of workgroups");
                }
                else if (MessageBox.Show(
                             $"Updating {restore.WorkgroupsRestore!.Workgroups.Count} workgroups. Proceed?",
                             "Restore Data",
                             MessageBoxButton.OKCancel)
                         == MessageBoxResult.OK)
                {
//                    if (fClearBeforeRestore)
//                        ServiceInterop.DeleteAllWorkgroups();

                    foreach (ServiceWorkgroup workgroup in restore.WorkgroupsRestore!.Workgroups)
                    {
                        try
                        {
                            ServiceInterop.CreateWorkgroup(catalogID, workgroup);
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"caught exception trying to add workgroup: {exc}");
                        }
                    }
                }
            }

            if (!m_model.ImportMediaItems)
            {
                if (m_model.ImportMediaStacks || m_model.ImportVersionStacks)
                {
                    MessageBox.Show("It makes no sense to restore version or media stacks when not restoring media");
                }
            }
            else
            {
                if ((restore.CatalogRestore?.Catalog.GetMediaCount ?? 0) == 0)
                {
                    MessageBox.Show("Can't restore an empty catalog");
                }
                else
                {
                    if (!m_model.ImportMediaStacks)
                        restore.CatalogRestore!.Catalog.MediaStacks.Clear();

                    if (!m_model.ImportVersionStacks)
                        restore.CatalogRestore!.Catalog.VersionStacks.Clear();

                    if (fClearBeforeRestore)
                        ServiceInterop.DeleteAllMediaAndMediaTagsAndStacks(catalogID);

                    Catalog catalogCurrent = new Catalog();
                    // we don't support restoring deprecated databases. you must remap them.
                    MetatagSchema schema = new MetatagSchema(false);
                    ServiceMetatagSchema serviceSchema = ServiceInterop.GetMetatagSchema(catalogID);

                    schema.ReplaceFromService(serviceSchema);
                    await catalogCurrent.ReadFullCatalogFromServer(catalogID, schema);
                        
                    // and now we have to diff
                    restore.CatalogRestore!.Catalog.SetBaseFromBaseCatalog(catalogCurrent);
                    restore.CatalogRestore!.Catalog.PushPendingChanges(
                        catalogID,
                        (count, itemType) => MessageBox.Show(
                                $"Updating {count} {itemType} items. Proceed?",
                                "Restore Data",
                                MessageBoxButton.OKCancel)
                            == MessageBoxResult.OK);
                }
            }

            if (m_model.ImportImports)
            {
                if ((restore.ImportsRestore?.ImportItems.Count ?? 0) == 0)
                {
                    MessageBox.Show("Can't restore an empty imports collection");
                }
                else if (MessageBox.Show(
                             $"Inserting {restore.ImportsRestore!.ImportItems.Count} import records. Proceed?",
                             "Restore Data",
                             MessageBoxButton.OKCancel)
                         == MessageBoxResult.OK)
                {
                    ServiceInterop.InsertAllServiceImportItems(catalogID, restore.ImportsRestore!.ImportItems);
                }
            }
        }

        private FullExportRestore? m_fullRestoreData;

        void OnExportDataLoaded(IBackgroundWorker worker, RestoreDatabase restore)
        {
            m_model.ImportImports = false;
            m_model.ImportMediaItems = false;
            m_model.ImportMediaStacks = false;
            m_model.ImportVersionStacks = false;
            m_model.ImportSchema = false;
            m_model.ImportWorkgroups = false;

            if (restore.FullExportRestore == null)
                return;

            m_fullRestoreData = restore.FullExportRestore;

            if (restore.FullExportRestore.CatalogRestore?.Catalog.GetMediaCount > 0)
                m_model.ImportMediaItems = true;

            if (restore.FullExportRestore.CatalogRestore?.Schema.MetatagCount > 0)
                m_model.ImportSchema = true;

            if (restore.FullExportRestore.CatalogRestore?.Catalog.MediaStacks.Items.Count > 0)
                m_model.ImportMediaStacks = true;

            if (restore.FullExportRestore.CatalogRestore?.Catalog.VersionStacks.Items.Count > 0)
                m_model.ImportVersionStacks = true;

            if (restore.FullExportRestore.ImportsRestore?.ImportItems.Count > 0)
                m_model.ImportImports = true;

            if (restore.FullExportRestore.WorkgroupsRestore?.Workgroups.Count > 0)
                m_model.ImportWorkgroups = true;

            UpdateCurrentCatalogFromID(m_fullRestoreData.CatalogID);
        }

        void UpdateCurrentCatalogFromID(Guid? id)
        {
            if (id == null || m_model.CurrentRestoreBehavior == "Create New")
            {
                // no id, so we have to create a new one
                m_model.CatalogDefinition = null;
                m_model.CurrentRestoreBehavior = "Create New";
            }
            else 
            {
                foreach (ServiceCatalogDefinition def in m_model.CatalogDefinitions)
                {
                    if (def.ID == id)
                    {
                        m_model.CatalogDefinition = def;
                        return;
                    }
                }

                // otherwise couldn't find it, so its a create new...
                UpdateCurrentCatalogFromID(null);
            }
        }

        private void LoadExportedData(object sender, RoutedEventArgs e)
        {
            RestoreDatabase restore = new RestoreDatabase(m_model.RestorePath);

            App.State.AddBackgroundWork(
                "Restoring database",
                (progress) => restore.DoRestore(progress),
                (worker) => OnExportDataLoaded(worker, restore));
        }
    }
}
