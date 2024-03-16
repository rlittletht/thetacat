using Emgu.CV.CvEnum;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Thetacat.UI
{
    /// <summary>
    /// Interaction logic for Test.xaml
    /// </summary>
    public partial class Test : Window
    {
        //        public class ImageEntry
        //        {
        //            [PrimaryKey]
        //        }
        public Test()
        {
            InitializeComponent();

            App.State.RegisterWindowPlace(this, "MainWindow");
        }

        SQLiteConnection OpenDatabase()
        {
            return new SQLiteConnection("imagetest.db");
        }

        private string s_sAppID = "e1a078dd-755e-4b81-acb5-59d7770f96c8";
        //        private string s_sAppID = "bfbaffd7-2217-4deb-a85a-4f697e6bdf94";
        private string m_sAppTenant = "b90f9ef3-5e11-43e0-a75c-1f45e6b223fb";

        private async void DoCommand2(object sender, RoutedEventArgs e)
        {
            await BlobSync.Create(m_sAppTenant, s_sAppID, "thetacattest");
            TcBlobContainer blobContainer = await BlobSync.OpenContainer("imagetest");

            TcBlob uploaded = await blobContainer.Upload("c:\\temp\\snoozecropped2.jpg");

            MessageBox.Show($"blob uploaded: {uploaded.BlobName}, {uploaded.ContentMd5}");

            List<TcBlob> blobs = await blobContainer.EnumerateBlobs();

            List<string> names = new();

            foreach (TcBlob blob in blobs)
            {
                names.Add(blob.ToString());
            }

            MessageBox.Show($"Enumerated: {string.Join(",", names.ToArray())}");

            //            string accountName = "THETASOFT";
            //            TokenCredentialOptions options = new TokenCredentialOptions();
            //
            //            //            options.AuthorityHost = new Uri("https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize");
            //
            //            //            TokenCredential cred = new InteractiveBrowserCredential(
            //            //                authority: "https://login.microsoftonline.com/consumers",
            //            //                tenantId: m_sAppTenant, 
            //            //                clientId: s_sAppID);
            //
            //            TokenCredential cred = new InteractiveBrowserCredential(m_sAppTenant, s_sAppID, options);
            //
            //            Uri uri = new Uri($"https://thetacattest.blob.core.windows.net/");
            //
            //            //            Uri uri = new Uri($"https://{accountName}.blob.core.windows.net");
            //
            //            BlobServiceClient client = new BlobServiceClient(uri, cred);
            //            MessageBox.Show("Got a client!");
            //
            //            Azure.Pageable<BlobContainerItem> foo = client.GetBlobContainers();
            //
            //            foreach (BlobContainerItem item in foo)
            //            {
            //                MessageBox.Show($"item: ${item.Name}");
            //            }
            //
            //            Uri uri2 = new Uri($"https://thetacattest.blob.core.windows.net/imagetest");
            //
            //            BlobContainerClient container = new BlobContainerClient(uri2, cred);
            //
            //            Azure.Response<bool> exists = container.Exists();
            //
            //            MessageBox.Show($"exists: ${exists.Value}");
            //
            //            await using (FileStream fs = File.Open(
            //                       "c:\\temp\\snoozecropped2.jpg",
            //                       FileMode.Open,
            //                       FileAccess.Read,
            //                       FileShare.Read))
            //            {
            //                using (MD5 md5 = MD5.Create())
            //                {
            //                    byte []hash = await md5.ComputeHashAsync(fs);
            //
            //                    string md5string = Convert.ToBase64String(hash);
            //                    fs.Seek(0, SeekOrigin.Begin);
            //
            //                    Azure.Response<BlobContentInfo> info = await container.UploadBlobAsync("imgtest3.jpg", fs);
            //                    MessageBox.Show($"info = ${info.HasValue}, ${info.Value.ContentHash}, ${info.Value.ETag}");
            //                    BlobClient blob = container.GetBlobClient("imgtest3.jpg");
            //                    Dictionary<string, string> d = new Dictionary<string, string> { { "full_md5", md5string } };
            //                    await blob.SetMetadataAsync(d);
            //                }
            //            }
            //
        }

        private void DoCommand(object sender, RoutedEventArgs e)
        {
            // using (SQLiteConnection conn = OpenDatabase())
            {
                List<Image> images = new List<Image>();
                foreach (Image image in mainGrid.Children.OfType<Image>())
                {
                    images.Add(image);
                }

                int imgStart = 1; // 242;

                foreach (Image img in images)
                {
                    string baseName = $"Barbados_{imgStart}";
                    //                System.Uri uri = new Uri($"f:\\__nobackup\\scans\\img{imgStart}.jpg");
                    string filename = $"f:\\__nobackup\\scans\\processed\\{baseName}.jp2";
                    // System.Uri uri = new Uri();


                    using (Mat mat = Emgu.CV.CvInvoke.Imread(filename, ImreadModes.Color))
                    {
                        using (Mat matOut = new Mat(512, 512, mat.Depth, mat.NumberOfChannels))
                        {

                            CvInvoke.ResizeForFrame(mat, matOut, new System.Drawing.Size(512, 512));
                            mat.Save($"c:\\temp\\{baseName}-1.jpg");
                            string outFile = $"c:\\temp\\{baseName}-2.jpg";
                            matOut.Save(outFile);

                            img.Source = new BitmapImage(new Uri(outFile));
                            // PortableImage image = J2kImage.FromFile(filename);

                            //                BitmapSource bsrc = BitmapSource.Create(mat.Width, mat.Height, 300, 300, PixelFormats.Bgr24, ))
                            //                img.Source = new CachedBitmap()
                        }
                    }

                    imgStart++;
                }
            }
        }

        private void LaunchMigration(object sender, RoutedEventArgs e)
        {
            Migration.Migration migration = new();

            migration.ShowDialog();
        }

        private void ManageMetatags(object sender, RoutedEventArgs e)
        {
            Metatags.ManageMetadata manage = new();
            manage.ShowDialog();
        }

    }
}
