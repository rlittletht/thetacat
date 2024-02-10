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
using Thetacat.Export;
using Thetacat.Util;

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
            DataContext = m_model;
            App.State.RegisterWindowPlace(this, "import-data");
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

        private void DoImport(object sender, RoutedEventArgs e)
        {
        }

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
        }

        private void LoadExportedData(object sender, RoutedEventArgs e)
        {
            RestoreDatabase restore = new RestoreDatabase(m_model.RestorePath);

            App.State.AddBackgroundWork(
                "Restoring database", 
                (progress) => restore.DoRestore(progress));
            // ,(worker) => OnExportDataLoaded(worker, restore)
        }
    }
}
