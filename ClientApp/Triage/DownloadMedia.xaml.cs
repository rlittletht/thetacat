using Emgu.CV.Dnn;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Thetacat.Filtering.UI;
using Thetacat.Metatags;
using Thetacat.Metatags.Model;
using Thetacat.Triage;

namespace Thetacat.Explorer.UI
{
    /// <summary>
    /// Interaction logic for DownloadMedia.xaml
    /// </summary>
    public partial class DownloadMedia : Window
    {
        private DownloadMediaModel _Model;
        private readonly Dictionary<Guid, string> m_metatagLineageMap;

        public DownloadMedia()
        {
            InitializeComponent();
            _Model = new DownloadMediaModel();
            DataContext = _Model;
            m_metatagLineageMap = App.State.MetatagSchema.BuildLineageMap();
            InitializeAvailableParents();

        }

        void InitializeAvailableParents()
        {
            IComparer<KeyValuePair<Guid, string>> comparer =
                Comparer<KeyValuePair<Guid, string>>.Create((x, y) => String.Compare(x.Value, y.Value, StringComparison.Ordinal));
            ImmutableSortedSet<KeyValuePair<Guid, string>> sorted = m_metatagLineageMap.ToImmutableSortedSet(comparer);

            foreach (KeyValuePair<Guid, string> item in sorted)
            {
                _Model.AvailableTags.Add(new FilterModelMetatagItem(App.State.MetatagSchema.GetMetatagFromId(item.Key)!, item.Value));
            }

            AvailableMetatagsTree.Initialize(
                App.State.MetatagSchema.WorkingTree.Children,
                App.State.MetatagSchema.SchemaVersionWorking);
        }

        private void BrowseForPath(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.Title = "Select download folder";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = _Model.DownloadTo;

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
                _Model.DownloadTo = dlg.FileName;
            }
        }

        private void OkButton(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AddInitialTag(object sender, RoutedEventArgs e)
        {
            InitialTagPickerPopup.IsOpen = !InitialTagPickerPopup.IsOpen;
        }

        private FilterModelMetatagItem? GetTagFromId(Guid id)
        {
            foreach (FilterModelMetatagItem tag in _Model.AvailableTags)
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
                    _Model.InitialTags.Add(item);
            }

            InitialTagPickerPopup.IsOpen = false;
        }
        
        public static bool FPrompt(Window? parent, out string? directory, out bool triage, out bool replaceExisting, out bool appendMetadata, out DownloadFilenameType filenameType, out List<Metatag> metadataToExport)
        {
            DownloadMedia download = new DownloadMedia();

            string lastLocation = App.State.ActiveProfile.LastDownloadLocation
                ?? System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");

            download._Model.DownloadTo = lastLocation;
            download._Model.ReplaceExisting = App.State.ActiveProfile.ReplaceFilesOnDownload ?? false;
            download._Model.Triage = App.State.ActiveProfile.DownloadFilesForTriage ?? false;
            download._Model.AppendMetadata = App.State.ActiveProfile.AppendMetadataOnDownload ?? false;

            string filenameTypeString = App.State.ActiveProfile.DownloadFilenameType ?? "Leaf Only";

            download._Model.FilenameType = download._Model.GetFilenameTypeForDisplayString(filenameTypeString);

            download.Owner = parent;
            if (download.ShowDialog() ?? false)
            {
                directory = download._Model.DownloadTo;
                triage = download._Model.Triage;
                replaceExisting = download._Model.ReplaceExisting;
                appendMetadata = download._Model.AppendMetadata;
                filenameType = download._Model.FilenameType?.Value ?? DownloadFilenameType.LeafOnly;
                App.State.ActiveProfile.DownloadFilenameType = download._Model.FilenameType?.DisplayName;
                App.State.ActiveProfile.ReplaceFilesOnDownload = download._Model.ReplaceExisting;
                App.State.ActiveProfile.DownloadFilesForTriage = download._Model.Triage;
                App.State.ActiveProfile.LastDownloadLocation = download._Model.DownloadTo;
                App.State.ActiveProfile.AppendMetadataOnDownload = download._Model.AppendMetadata;
                
                App.State.Settings.WriteSettings();

                metadataToExport = new();
                
                foreach(FilterModelMetatagItem item in download._Model.InitialTags)
                {
                    metadataToExport.Add(item.Metatag);
                }
                
                return true;
            }
            else
            {
                directory = null;
                triage = false;
                replaceExisting = false;
                appendMetadata = false;
                filenameType = DownloadFilenameType.LeafOnly;
                metadataToExport = new();
                return false;
            }
        }
    }
}
