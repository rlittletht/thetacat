using System;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Thetacat.Model;
using Thetacat.Types;
using Thetacat.UI;
using Thetacat.UI.ProgressReporting;
using Thetacat.Util;
using Directory = System.IO.Directory;
using Path = System.IO.Path;
using PathSegment = Thetacat.Util.PathSegment;
using System.Collections.Immutable;
using Thetacat.Filtering.UI;
using Thetacat.Standards;
using Thetacat.Metatags;
using Thetacat.Import.UI.Commands;
using Thetacat.Metatags.Model;
using Thetacat.Repair;
using Thetacat.Model.Mediatags;

namespace Thetacat.Import.UI
{
    /// <summary>
    /// Interaction logic for MediaImport.xaml
    /// </summary>
    public partial class MediaImport : Window
    {
        private readonly MediaImportModel m_model = new();
        private readonly MediaImporter m_importer;
        private readonly Dictionary<Guid, string> m_metatagLineageMap;

        public MediaImportModel Model => m_model;

        public MediaImport(MediaImporter importer)
        {
            m_model.RemoveInitialTagCommand = new RemoveInitialTagCommand(_RemoveInitialTagCommand);
            InitializeComponent();
            DataContext = m_model;
            m_importer = importer;
            Sources.ItemsSource = m_model.Nodes;
            m_importBackgroundWorkers = new BackgroundWorkers(BackgroundActivity.Start, BackgroundActivity.Stop);
            App.State.RegisterWindowPlace(this, "media-import");
            InitializeVirtualRoots();
            m_metatagLineageMap = App.State.MetatagSchema.BuildLineageMap();
            InitializeAvailableParents();
        }

        static KeyValuePair<string?, VirtualRootNameItem>[] VirtualRootSplitter(VirtualRootNameItem data)
        {
            // simple string splitter
            string[] split = data.FullName.Split("/");
            List<KeyValuePair<string?, VirtualRootNameItem>> newData = new();

            int i = 0;
            PathSegment segment = PathSegment.Empty;
            for (; i < split.Length - 1; i++)
            {
                string s = split[i];
                segment = PathSegment.Join(segment, s);
                newData.Add(new KeyValuePair<string?, VirtualRootNameItem>(s, new VirtualRootNameItem(s, segment)));
            }

            if (i == split.Length - 1)
                newData.Add(new KeyValuePair<string?, VirtualRootNameItem>(null, new VirtualRootNameItem(split[i], data.FullName)));

            return newData.ToArray();
        }

        void InitializeVirtualRoots()
        {
            VirtualRootTree.Clear();

            HashSet<string> roots = new();

            foreach (MediaItem item in App.State.Catalog.GetMediaCollection())
            {
                roots.Add(item.VirtualPath.GetPathDirectory());
            }

            List<VirtualRootNameItem> items = new();

            foreach (string root in roots)
            {
                items.Add(new VirtualRootNameItem(root));
            }

            items.Sort(VirtualRootNameItemComparer);
            BackingTree tree = BackingTree.CreateFromList<VirtualRootNameItem, string>(items, VirtualRootSplitter, new VirtualRootNameItem(""));
            VirtualRootTree.Initialize(tree.Children);
            m_model.VirtualPathRoots.AddRange(items);
        }

        private int VirtualRootNameItemComparer(VirtualRootNameItem x, VirtualRootNameItem y)
        {
            return string.Compare(x.FullName, y.FullName, StringComparison.CurrentCultureIgnoreCase);
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

        private async void SetSourcePath(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(m_model.SourcePath))
            {
                MessageBox.Show($"Must specify a directory to add. {m_model.SourcePath} is not a directory");
                return;
            }

            string? parent = Path.GetDirectoryName(m_model.SourcePath) ?? m_model.SourcePath;
            string directoryLeaf = Path.GetRelativePath(parent, m_model.SourcePath);

            if (PathSegment.CreateFromString(m_model.SourcePath).Subsumes(App.State.Cache.LocalPathToCacheRoot))
            {
                m_model.ImportInPlace = true;
                MessageBox.Show("You have selected a directory inside the cache. This will search for and import in-place");
            }

            List<ImportNode> nodesToAdd =
                await m_importBackgroundWorkers.DoWorkAsync(
                    "Adding items to source",
                    (progress) =>
                    {
                        List<ImportNode> _nodesToAdd = new();

                        ImportNode node = new ImportNode(true, directoryLeaf, "", parent, true);

                        _nodesToAdd.Add(node);
                        ProcessNode(node);
                        return _nodesToAdd;
                    });
            m_model.Nodes.Clear();
            m_model.ImportItems.Clear();
            m_model.Nodes.AddRange(nodesToAdd);
        }

        private void AddFilesInNodeToImportItems(List<ImportNode> nodesToAdd, HashSet<string> extensions, ImportNode node)
        {
            ImportNode parent = new ImportNode(true, node.FullPath, "", node.FullPath, true);

            foreach (string file in Directory.GetFiles(node.FullPath))
            {
                // don't try to import .db files
                if (file.EndsWith(".db"))
                    continue;

                if (m_model.ImportInPlace && IsFullLocalPathInCache(file))
                    continue;

                ImportNode fileNode = new ImportNode(true, Path.GetFileName(file), "", parent.Path, false);
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
                await m_importBackgroundWorkers.DoWorkAsync(
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

            if (nodesToAdd.Count == 0)
            {
                UpdateStatus("Done searching. No new media to import.");
            }
            else
            {
                UpdateStatus($"Done searching. Adding {nodesToAdd.Count} items.");
                m_model.ImportItems.AddRange(nodesToAdd);
                m_model.FileExtensions.Clear();
                List<string> sortedExtensions = new List<string>(extensions);
                sortedExtensions.Sort();
                m_model.FileExtensions.AddRange(sortedExtensions);
            }
        }

        private ProgressListDialog? m_backgroundProgressDialog;
        private readonly BackgroundWorkers m_importBackgroundWorkers;

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
            CheckableTreeViewSupport<ImportNode>.SetParentCheckStateForChildren(m_model.ImportItems);
            ExtensionList.SelectedItems.Clear();
        }

        private void CheckSelectedExtensions(object sender, RoutedEventArgs e)
        {
            HashSet<string> selected = GetSelectedExtensions();

            CheckableTreeViewSupport<ImportNode>.FilterAndToggleSetSubtree(
                m_model.ImportItems,
                (ImportNode node) => selected.Contains(Path.GetExtension(node.Name).ToLowerInvariant()),
                true);
            CheckableTreeViewSupport<ImportNode>.SetParentCheckStateForChildren(m_model.ImportItems);
            ExtensionList.SelectedItems.Clear();
        }

        private HashSet<string>? m_existingCacheItems;

        void BuildExistingCacheItemHash()
        {
            m_existingCacheItems = new HashSet<string>();

            foreach (ICacheEntry entry in App.State.Cache.Entries.Values)
            {
                string full = App.State.Cache.GetFullLocalPath(entry.Path).ToUpper();

                m_existingCacheItems.Add(full);
            }
        }

        bool IsFullLocalPathInCache(string fullPathLocal)
        {
            if (m_existingCacheItems == null)
                BuildExistingCacheItemHash();

            return m_existingCacheItems?.Contains(fullPathLocal.ToUpper()) ?? false;
        }

        MediaItem? LookupMediaIdForPath(string fullPathLocal, string sourcePath, string name)
        {
            string fullLocalWithName = Path.Join(fullPathLocal, name);

            PathSegment relativePathWithName = new(Path.GetRelativePath(sourcePath, fullLocalWithName));

            ICatalog catalog = App.State.Catalog;
            MediaItem? item = null;

            // first, treat the relativePath as the virtual path
            item = catalog.LookupItemFromVirtualPath(relativePathWithName, fullLocalWithName, true);

            if (item != null)
                return item;

            // no match, now working our way from full paths to the name, keep trying
            PathSegment fullWithName = new(fullLocalWithName);
            fullWithName.TraverseDirectories(
                (segment) =>
                {
                    item = catalog.LookupItemFromVirtualPath(segment, fullLocalWithName, true);
                    return item == null; // continue if we didn't find it
                });

            return item;
        }

        /*----------------------------------------------------------------------------
            %%Function: SearchForImportedItemsWork
            %%Qualified: Thetacat.Import.UI.MediaImport.SearchForImportedItemsWork

            This takes the checked items in the Import dialog and figures out which
            ones are already in the catalog (using the MD5 as the arbiter)
        ----------------------------------------------------------------------------*/
        private bool SearchForImportedItemsWork(IProgressReport progress)
        {
            try
            {
                List<ImportNode> checkedItems = CheckableTreeViewSupport<ImportNode>.GetCheckedItems(m_model.ImportItems);
                int count = checkedItems.Count;
                int i = 0;
                foreach (ImportNode item in checkedItems)
                {
                    progress.UpdateProgress((i++ * 100.0) / count);
                    // see if we have a match in the catalog
                    if (item.MediaId != null)
                        continue; // already matched

                    if (item.IsDirectory)
                        continue;

                    string fullLocalWithName = Path.Join(item.Path, item.Name);
                    string md5 = App.State.Md5Cache.GetMd5ForPathSync(fullLocalWithName);

                    // first, do we have a match on path?
                    MediaItem? mediaItem = LookupMediaIdForPath(item.Path, m_model.SourcePath, item.Name);

                    if (mediaItem == null || mediaItem.MD5 != md5)
                    {
                        mediaItem = App.State.Catalog.FindMatchingMediaByMD5(md5);
                    }

                    if (mediaItem != null)
                    {
                        item.MediaId = mediaItem.ID;
                        item.MD5 = mediaItem.MD5;
                        item.Checked = false;
                        item.MatchedItem = $"{mediaItem.VirtualPath}";

                        // check to see if this is in a broken workgroup state
                        if (WorkgroupRepair.IsMediaItemInBrokenWorkgroupState(mediaItem.ID))
                        {
                            item.NeedsRepair = true;
                            item.Checked = true;
                        }
                    }
                }

                // lastly, go through and uncheck any items where all the children are unchecked
                CheckableTreeViewSupport<ImportNode>.SetParentCheckStateForChildren(m_model.ImportItems);
            }
            finally
            {
                progress.WorkCompleted();
                m_model.IsMediaCheckedAgainstCatalog = true;
            }

            return true;
        }

        private void SearchForImportedItems(object sender, RoutedEventArgs e)
        {
            m_importBackgroundWorkers.AddWork("searching for already imported items", SearchForImportedItemsWork);
        }

        public static PathSegment BuildVirtualPath(
            string sourcePath,
            string itemPath,
            string itemName,
            bool includeParentDir,
            bool includeSubdirs,
            string? virtualRoot,
            string? virtualSuffix)
        {
            PathSegment sourceSegment = PathSegment.CreateFromString(sourcePath);
            PathSegment itemSegment = PathSegment.CreateFromString(itemPath);

            PathSegment relativeSegment = includeSubdirs ? itemSegment.GetRelativePath(sourceSegment) : PathSegment.Empty;

            PathSegment parentSegment =
                includeParentDir
                    ? sourceSegment.GetLeafItem() ?? PathSegment.Empty
                    : PathSegment.Empty;

            PathSegment virtualRootSegment = PathSegment.CreateFromString(virtualRoot);
            PathSegment virtualRootSuffixSegment = PathSegment.CreateFromString(virtualSuffix);

            PathSegment virtualPath = PathSegment.Join(virtualRootSegment, virtualRootSuffixSegment, parentSegment, relativeSegment, itemName);

//            PathSegment virtualPath = PathSegment.CreateFromString(virtualRoot);
//
//            virtualPath = PathSegment.Join(
//                virtualPath,
//                includeSubdirs
//                    ? PathSegment.Join(virtualSuffix ?? string.Empty, relativePath, itemName)
//                    : PathSegment.Join(virtualSuffix ?? string.Empty, itemName));

            return virtualPath.Unroot();
        }


        private PathSegment MakeVirtualPathForImportItem(ImportNode item)
        {
            if (item.IsDirectory)
                return PathSegment.Empty;

            string sourcePath =
                m_model.ImportInPlace
                    ? App.State.Cache.LocalPathToCacheRoot.Local
                    : m_model.SourcePath;

            return BuildVirtualPath(
                sourcePath,
                item.Path,
                item.Name,
                m_model.IncludeParentDirInVirtualPath,
                m_model.IncludeSubdirInVirtualPath,
                m_model.VirtualPathRoot?.FullName ?? null,
                m_model.VirtualPathSuffix ?? null);
        }

        private void DoPrePopulateWork(IProgressReport report, List<ImportNode> checkedItems)
        {
            int i = 0, iMax = checkedItems.Count;

            foreach (ImportNode item in checkedItems)
            {
                report.UpdateProgress((i++ * 100.0) / iMax);
                if (item.IsDirectory)
                    continue;

                // here we can pre-populate our cache.
                if (item.MediaId == null)
                    throw new CatExceptionInternalFailure($"media id not set after items added");
                MediaItem mediaItem = App.State.Catalog.GetMediaFromId(item.MediaId.Value);

                App.State.Cache.PrimeCacheFromImport(mediaItem, PathSegment.Join(item.Path, item.Name));
                mediaItem.NotifyCacheStatusChanged();
            }

            report.WorkCompleted();
        }

        private void DoImport(object sender, RoutedEventArgs e)
        {
            if (!m_model.IsMediaCheckedAgainstCatalog)
            {
                if (MessageBox.Show(
                        "Thetacat Import",
                        "You have not checked for items that already exist in the catalog. Do you want to continue and potentially import duplicate items?",
                        MessageBoxButton.YesNo)
                    == MessageBoxResult.No)
                {
                    return;
                }
            }

            List<ImportNode> checkedItems = CheckableTreeViewSupport<ImportNode>.GetCheckedItems(
                m_model.ImportItems,
                (node) => !node.IsDirectory);

            foreach (ImportNode item in checkedItems)
            {
                if (item.IsDirectory)
                    continue;

                item.VirtualPath = MakeVirtualPathForImportItem(item);
            }

            m_importer.ClearItems();
            m_importer.AddMediaItemFilesToImporter(
                checkedItems,
                MainApp.MainWindow.ClientName,
                (itemFile, catalogItem) =>
                {
                    ImportNode node = itemFile as ImportNode ?? throw new CatExceptionInternalFailure("file item isn't an ImportNode?");
                    node.MediaId = catalogItem.ID;
                    node.MatchedItem = catalogItem.VirtualPath;

                    if (m_model.InitialTags.Count > 0)
                    {
                        foreach (FilterModelMetatagItem metatag in m_model.InitialTags)
                        {
                            MediaTag tag = new MediaTag(metatag.Metatag, null);
                            catalogItem.FAddOrUpdateMediaTag(tag, true);
                        }
                    }
                });

            m_importer.CreateCatalogItemsAndUpdateImportTable(App.State.ActiveProfile.CatalogID, App.State.Catalog, App.State.MetatagSchema, App.State.Cache);
//            ProgressDialog.DoWorkWithProgress(report => DoPrePopulateWork(report, checkedItems), Window.GetWindow(this));

            // and lastly we have to add the items we just manually added to our cache
            // (we don't have any items we are tracking. these should all be adds)
            App.State.Cache.PushChangesToDatabase(null);
        }

        private void ImportItemSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ImportNode node)
                m_model.VirtualPathPreview = $"{MakeVirtualPathForImportItem(node)}";
        }

        VirtualRootNameItem EnsurePathInVirtualRoots(string path)
        {
            VirtualRootNameItem item = new VirtualRootNameItem(path);

            foreach (VirtualRootNameItem root in m_model.VirtualPathRoots)
            {
                if (string.Compare(root.FullName, item.FullName, StringComparison.OrdinalIgnoreCase) == 0)
                    return root;
            }

            m_model.VirtualPathRoots.Add(item);
            return item;
        }

        private void DoSelectedVirtualRootChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is BackingTreeItem<VirtualRootNameItem> newItem)
            {
                VirtualPathRootsComboBox.SelectedItem = EnsurePathInVirtualRoots(newItem.Data.FullName);
            }

            VirtualRootPickerPopup.IsOpen = false;
        }

        private void SelectVirtualRoot(object sender, RoutedEventArgs e)
        {
            VirtualRootPickerPopup.IsOpen = !VirtualRootPickerPopup.IsOpen;
        }

        void UpdateStatus(string status)
        {
            m_model.ImportStatus = status;
        }

        void InitializeAvailableParents()
        {
            IComparer<KeyValuePair<Guid, string>> comparer =
                Comparer<KeyValuePair<Guid, string>>.Create((x, y) => String.Compare(x.Value, y.Value, StringComparison.Ordinal));
            ImmutableSortedSet<KeyValuePair<Guid, string>> sorted = m_metatagLineageMap.ToImmutableSortedSet(comparer);

            foreach (KeyValuePair<Guid, string> item in sorted)
            {
                m_model.AvailableTags.Add(new FilterModelMetatagItem(App.State.MetatagSchema.GetMetatagFromId(item.Key)!, item.Value));
            }

            AvailableMetatagsTree.Initialize(
                App.State.MetatagSchema.WorkingTree.Children,
                App.State.MetatagSchema.SchemaVersionWorking);
        }

        private FilterModelMetatagItem? GetTagFromId(Guid id)
        {
            foreach (FilterModelMetatagItem tag in m_model.AvailableTags)
            {
                if (tag.Metatag.ID == id)
                    return tag;
            }

            return null;
        }

        private void DoSelectedInitialTagChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is MetatagTreeItem newItem)
            {
                FilterModelMetatagItem? item = GetTagFromId(newItem.ItemId);

                if (item != null)
                    m_model.InitialTags.Add(item);
            }

            InitialTagPickerPopup.IsOpen = false;
        }

        private void AddInitialTag(object sender, RoutedEventArgs e)
        {
            InitialTagPickerPopup.IsOpen = !InitialTagPickerPopup.IsOpen;
        }

        void _RemoveInitialTagCommand(FilterModelMetatagItem? item)
        {
            if (item != null)
            {
                m_model.InitialTags.Remove(item);
            }
        }
    }
}
