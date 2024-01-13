using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualBasic;
using Thetacat.Types;
using Thetacat.Util;

namespace Thetacat;

public class TcBlobContainer
{
    readonly BlobContainerClient m_client;

    public TcBlobContainer(BlobContainerClient client)
    {
        m_client = client;
    }

    public async Task<ETag> DoUploadWithMetadataCheckOnRetry(string localPath, string blobName, FileStream fs, string expectedMd5)
    {
        try
        {
            Response<BlobContentInfo> info = await m_client.UploadBlobAsync(blobName, fs);
            if (!info.HasValue)
                throw new Exception($"upload {localPath}->{blobName} failed!");

            BlobClient blob = m_client.GetBlobClient(blobName);

            Dictionary<string, string> metadata = new() { { TcBlob.META_FULL_CONTENT_MD5, expectedMd5 } };
            Response<BlobInfo> blobInfo = await blob.SetMetadataAsync(metadata);

            if (!blobInfo.HasValue)
                throw new Exception($"could not get properties for {blobName}");

            return info.Value.ETag;
        }
        catch (RequestFailedException exc)
        {
            // probably because it already exists...check for that
            BlobClient blob = m_client.GetBlobClient(blobName);
            string? md5Blob = null;

            BlobProperties properties = await blob.GetPropertiesAsync();

            // find the MD5 prop
            foreach (KeyValuePair<string, string> metadata in properties.Metadata)
            {
                if (metadata.Key == TcBlob.META_FULL_CONTENT_MD5)
                    md5Blob = metadata.Value;
            }

            if (md5Blob == null || md5Blob != expectedMd5)
                throw new CatExceptionAzureFailure($"upload {localPath}->{blobName} failed: {exc.Message}", exc);

            // otherwise, the blob that's already there has the same md5, so we must have uploaded
            // it before and didn't get to update our DB
            return properties.ETag;
        }
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

        string fullContentMd5 = await Checksum.GetMD5ForStream(fs);

        fs.Seek(0, SeekOrigin.Begin);
        ETag etag = await DoUploadWithMetadataCheckOnRetry(localPath, blobName, fs, fullContentMd5);


        return new TcBlob(blobName, fullContentMd5, etag);
    }

    public async Task<TcBlob> Download(string blobName, string localPath, string? md5Expected)
    {
        if (Path.Exists(localPath))
        {
            // local path exists. Check to see if the MD5 hash matches
            if (md5Expected != null)
            {
                string md5 = await App.State.Md5Cache.GetMd5ForPathAsync(localPath);

                if (md5 == md5Expected)
                    return new TcBlob(blobName, md5, null);
            }

            throw new CatExceptionAzureFailure($"local file {localPath} already existed with different md5");
        }

        BlobClient blob = m_client.GetBlobClient(blobName);
        string? md5Blob = null;

        BlobProperties properties = await blob.GetPropertiesAsync();

        // find the MD5 prop
        foreach (KeyValuePair<string, string> metadata in properties.Metadata)
        {
            if (metadata.Key == TcBlob.META_FULL_CONTENT_MD5)
                md5Blob = metadata.Value;
        }

        // before we can download, we have to make sure the directory exists
        string? directory = Path.GetDirectoryName(localPath);

        if (directory != null && (!Directory.Exists(directory)))
            Directory.CreateDirectory(directory);

        // consider passing an IProgress to BlobDownloadToOptions for progress reporting...
        using Response response = await blob.DownloadToAsync(localPath);

        if (response.IsError)
            throw new CatExceptionAzureFailure($"could not download for {localPath}: {response.ReasonPhrase}");

        md5Blob ??= await App.State.Md5Cache.GetMd5ForPathAsync(localPath);

        return new TcBlob(blobName, md5Blob, properties.ETag);
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
