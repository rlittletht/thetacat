using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Thetacat;

// this is a singleton
class BlobSync
{
    private static TokenCredential m_credential = null;

    private static string m_storageAccountName = null;

    public static async Task Create(string appTenant, string appId, string storageAccountName)
    {
        TokenCredentialOptions options = new TokenCredentialOptions();

        BlobSync.m_credential = new InteractiveBrowserCredential(appTenant, appId, options);
        BlobSync.m_storageAccountName = storageAccountName;
    }

    public static async Task<TcBlobContainer> OpenContainer(string containerName)
    {
        Uri uri = new Uri($"https://{m_storageAccountName}.blob.core.windows.net/{containerName}");

        BlobContainerClient container = new BlobContainerClient(uri, m_credential);

        // make sure this exists
        Azure.Response<bool> exists = await container.ExistsAsync();

        if (!exists.HasValue)
            throw new Exception("no response from azure");

        if (!exists.Value)
            throw new Exception($"container {containerName} doesn't exist");

        return new TcBlobContainer(container);
    }

//    private void foo()
//    {
//
//        Uri uri = new Uri($"https://{storageAccountName}.blob.core.windows.net/");
//
//        Azure.Pageable<BlobContainerItem> foo = client.GetBlobContainers();
//
//        foreach (BlobContainerItem item in foo)
//        {
//            MessageBox.Show($"item: ${item.Name}");
//        }
//
//        Uri uri2 = new Uri($"https://thetacattest.blob.core.windows.net/imagetest");
//
//        BlobContainerClient container = new BlobContainerClient(uri2, cred);
//
//        Azure.Response<bool> exists = container.Exists();
//
//        MessageBox.Show($"exists: ${exists.Value}");
//
//        await using (FileStream fs = File.Open(
//                   "c:\\temp\\snoozecropped2.jpg",
//                   FileMode.Open,
//                   FileAccess.Read,
//                   FileShare.Read))
//        {
//            using (MD5 md5 = MD5.Create())
//            {
//                byte[] hash = await md5.ComputeHashAsync(fs);
//
//                string md5string = Convert.ToBase64String(hash);
//                fs.Seek(0, SeekOrigin.Begin);
//
//                Azure.Response<BlobContentInfo> info = await container.UploadBlobAsync("imgtest3.jpg", fs);
//                MessageBox.Show($"info = ${info.HasValue}, ${info.Value.ContentHash}, ${info.Value.ETag}");
//                BlobClient blob = container.GetBlobClient("imgtest3.jpg");
//                Dictionary<string, string> d = new Dictionary<string, string> { { "full_md5", md5string } };
//                await blob.SetMetadataAsync(d);
//            }
//        }
//
//    }
}
