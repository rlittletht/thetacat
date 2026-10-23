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
using Thetacat.Triage;

namespace Thetacat.Explorer.UI
{
    /// <summary>
    /// Interaction logic for DownloadMedia.xaml
    /// </summary>
    public partial class DownloadMedia : Window
    {
        private DownloadMediaModel _Model;
        
        public DownloadMedia()
        {
            InitializeComponent();
            _Model = new DownloadMediaModel();
            DataContext = _Model;
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

        public static bool FPrompt(string lastLocation, Window? parent,  out string? directory, out bool triage, out bool replaceExisting)
        {
            DownloadMedia download = new DownloadMedia();

            download._Model.DownloadTo = lastLocation;
            download.Owner = parent;
            if (download.ShowDialog() ?? false)
            {
                directory = download._Model.DownloadTo;
                triage = download._Model.Triage;
                replaceExisting = download._Model.ReplaceExisting;
                return true;
            }
            else
            {
                directory = null;
                triage = false;
                replaceExisting = false;
                return false;
            }
        }
    }
}
