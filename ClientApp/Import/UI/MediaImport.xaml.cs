using MetadataExtractor;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
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
using TCore.Pipeline;
using Thetacat.Types;
using Thetacat.UI.ProgressReporting;
using Thetacat.Util;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Directory = System.IO.Directory;
using Path = System.IO.Path;

namespace Thetacat.Import.UI
{
    /// <summary>
    /// Interaction logic for MediaImport.xaml
    /// </summary>
    public partial class MediaImport : Window
    {
        private readonly MediaImportModel m_model = new ();
        private MediaImporter m_importer;

        public MediaImport(MediaImporter importer)
        {
            InitializeComponent();
            DataContext = m_model;
            m_importer = importer;
            Sources.ItemsSource = m_model.Nodes;
            m_importBackgroundWorkers = new BackgroundWorkers(BackgroundActivity.Start, BackgroundActivity.Stop);
            App.State.RegisterWindowPlace(this, "media-import");
        }

        private void BrowseForPath(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.Title = "My Title";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = "";

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = "";
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                m_model.SourcePath = dlg.FileName;
            }
        }

        void ProcessNode(ImportNode node)
        {
            string fullPath = Path.Join(node.Path, node.Name);

            string[] entries = Directory.GetDirectories(fullPath);

            foreach (string entry in entries)
            {
                string? parent = Path.GetDirectoryName(entry);
                if (parent == null)
                {
                    MessageBox.Show($"could not process directory {entry}: no leaf name?");
                    continue;
                }

                string leaf = Path.GetRelativePath(parent, entry);

                ImportNode nodeNew = new ImportNode(true, leaf, "", fullPath, true);
                node.Children.Add(nodeNew);
                ProcessNode(nodeNew);
            }
        }

        private async void AddToSources(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(m_model.SourcePath))
            {
                MessageBox.Show($"Must specify a directory to add. {m_model.SourcePath} is not a directory");
                return;
            }

            string? parent = Path.GetDirectoryName(m_model.SourcePath) ?? m_model.SourcePath;

            string directoryLeaf = Path.GetRelativePath(parent, m_model.SourcePath);

            List<ImportNode> nodesToAdd =
                await DoBackgroundWorkAsync<List<ImportNode>>(
                    "Adding items to source",
                    (progress) =>
                    {
                        List<ImportNode> _nodesToAdd = new();

                        ImportNode node = new ImportNode(true, directoryLeaf, "", parent, true);

                        _nodesToAdd.Add(node);
                        ProcessNode(node);
                        return _nodesToAdd;
                    });

            m_model.Nodes.AddRange(nodesToAdd);
        }

        private void AddFilesInNodeToImportItems(List<ImportNode> nodesToAdd, HashSet<string> extensions, ImportNode node)
        {
            ImportNode parent = new ImportNode(true, node.FullPath, "", node.FullPath, true);

            foreach (string file in Directory.GetFiles(node.FullPath))
            {
                ImportNode fileNode = new ImportNode(true, file, "", parent.Path, false);
                string extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension.Length > 1)
                    extensions.Add(extension);

                parent.Children.Add(fileNode);
            }

            if (parent.Children.Count > 0)
                nodesToAdd.Add(parent);
        }

        private async void AddToImport(object sender, RoutedEventArgs e)
        {
            List<ImportNode> checkedItems = CheckableTreeViewSupport<ImportNode>.GetCheckedItems(Sources);
            HashSet<string> extensions = new HashSet<string>(m_model.FileExtensions);

            List<ImportNode> nodesToAdd =
                await DoBackgroundWorkAsync<List<ImportNode>>(
                    "Adding import items",
                    (progress) =>
                    {
                        List<ImportNode> _nodesToAdd = new();

                        foreach (ImportNode node in checkedItems)
                        {
                            AddFilesInNodeToImportItems(_nodesToAdd, extensions, node);
                        }

                        return _nodesToAdd;
                    });

            m_model.ImportItems.AddRange(nodesToAdd);
            m_model.FileExtensions.Clear();
            List<string> sortedExtensions = new List<string>(extensions);
            sortedExtensions.Sort();
            m_model.FileExtensions.AddRange(sortedExtensions);
        }

        private ProgressListDialog? m_backgroundProgressDialog;
        private readonly BackgroundWorkers m_importBackgroundWorkers;

        public void AddBackgroundWork<T>(string description, BackgroundWorkerWork<T> work)
        {
            m_importBackgroundWorkers.AddWork(description, work);
        }

        public async Task<T> DoBackgroundWorkAsync<T>(string description, BackgroundWorkerWork<T> work)
        {
            return await m_importBackgroundWorkers.DoWorkAsync<T>(description, work);
        }

        private void HandleSpinnerDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (m_backgroundProgressDialog == null)
            {
                m_backgroundProgressDialog = new ProgressListDialog();
                m_backgroundProgressDialog.ProgressReports.ItemsSource = m_importBackgroundWorkers.Workers;
                m_backgroundProgressDialog.Owner = this;
                m_backgroundProgressDialog.Show();
                m_backgroundProgressDialog.Closing +=
                    (_, _) => { m_backgroundProgressDialog = null; };
                m_backgroundProgressDialog.Show();
            }
        }

        private int m_lastSpinnerClick = 0;

        private void HandleSpinnerMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Timestamp - m_lastSpinnerClick < 200)
                HandleSpinnerDoubleClick(sender, e);

            m_lastSpinnerClick = e.Timestamp;
        }

        private void OnCheckboxClick(object sender, RoutedEventArgs e)
        {
            CheckableTreeViewSupport<ImportNode>.DoCheckboxClickSetUnsetChildren(sender, e);
        }

        private void ToggleSelectedExtensions(object sender, RoutedEventArgs e)
        {
            
        }

        HashSet<string> GetSelectedExtensions()
        {
            HashSet<string> selected = new();

            foreach (object? item in ExtensionList.SelectedItems)
            {
                if (item is string extensions)
                    selected.Add(extensions);
            }

            return selected;
        }

        private void UncheckSelectedExtensions(object sender, RoutedEventArgs e)
        {
            HashSet<string> selected = GetSelectedExtensions();

            CheckableTreeViewSupport<ImportNode>.FilterAndToggleSetSubtree(
                m_model.ImportItems,
                (ImportNode node) => selected.Contains(Path.GetExtension(node.Name).ToLowerInvariant()),
                false);
        }

        private void CheckSelectedExtensions(object sender, RoutedEventArgs e)
        {
            HashSet<string> selected = GetSelectedExtensions();

            CheckableTreeViewSupport<ImportNode>.FilterAndToggleSetSubtree(
                m_model.ImportItems,
                (ImportNode node) => selected.Contains(Path.GetExtension(node.Name).ToLowerInvariant()),
                true);
        }
    }
}
