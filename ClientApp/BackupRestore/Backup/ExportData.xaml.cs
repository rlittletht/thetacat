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

namespace Thetacat.BackupRestore.Backup
{
    /// <summary>
    /// Interaction logic for ExportData.xaml
    /// </summary>
    public partial class ExportData : Window
    {
        private readonly ExportDataModel m_model = new();

        public ExportData()
        {
            InitializeComponent();
            DataContext = m_model;
            App.State.RegisterWindowPlace(this, "export-data");
        }

        private void BrowseForPath(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.Title = "Choose export file";
            dlg.IsFolderPicker = false;
            dlg.InitialDirectory = m_model.ExportPath;

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
                m_model.ExportPath = dlg.FileName;
            }
        }

        private void DoCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }   

        private void DoExport(object sender, RoutedEventArgs e)
        {
            BackupDatabase backup = new BackupDatabase(
                m_model.ExportPath, 
                m_model.ExportMediaItems, 
                m_model.ExportMediaStacks, 
                m_model.ExportVersionStacks, 
                m_model.ExportSchema, 
                m_model.ExportImports, 
                m_model.ExportWorkgroups);

            App.State.AddBackgroundWork("Backing up database", (progress) => backup.DoBackup(progress));
            Close();
        }
    }
}
