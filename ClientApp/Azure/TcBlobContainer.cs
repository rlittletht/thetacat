using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualBasic;

namespace Thetacat;

class TcBlobContainer
{
    readonly BlobContainerClient m_client;

    public TcBlobContainer(BlobContainerClient client)
    {
        m_client = client;
    }

    public async Task<TcBlob> Upload(string localPath, string? blobName = null, string? virtualRoot = null)
    {
        blobName ??= Guid.NewGuid().ToString();

        if (virtualRoot != null)
            blobName = $"{virtualRoot}/{blobName}";

        await using FileStream fs = File.Open(
            localPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        using MD5 md5 = MD5.Create();

        byte[] hash = await md5.ComputeHashAsync(fs);

        string fullContentMd5 = Convert.ToBase64String(hash);

        fs.Seek(0, SeekOrigin.Begin);

        Azure.Response<BlobContentInfo> info = await m_client.UploadBlobAsync(blobName, fs);

        if (!info.HasValue)
            throw new Exception($"upload {localPath}->{blobName} failed!");

        BlobClient blob = m_client.GetBlobClient(blobName);

        Dictionary<string, string> metadata = new() { { TcBlob.META_FULL_CONTENT_MD5, fullContentMd5 } };
        Azure.Response<BlobInfo> blobInfo = await blob.SetMetadataAsync(metadata);

        if (!blobInfo.HasValue)
            throw new Exception($"could not get properties for {blobName}");

        return new TcBlob(blobName, fullContentMd5, info.Value.ETag);
    }

    public async Task<List<TcBlob>> EnumerateBlobs()
    {
        List<TcBlob> blobs = new();

        await foreach (BlobItem blob in m_client.GetBlobsAsync(BlobTraits.Metadata | BlobTraits.Tags))
        {
            if (blob.Metadata.TryGetValue(TcBlob.META_FULL_CONTENT_MD5, out string? contentMd5))
            {
                blobs.Add(new TcBlob(blob.Name, contentMd5, blob.Properties.ETag));
            }
        }

        return blobs;
    }
}
