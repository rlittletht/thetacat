
using System;
using Azure.Storage.Blobs.Models;

namespace Thetacat;

class TcBlob
{
    public static string META_FULL_CONTENT_MD5 = "fullContentMD5";

    public string ContentMd5 { get; }
    public string BlobName { get; }
    public Azure.ETag? Etag { get; }

    public TcBlob(string blobName, string contentMd5, Azure.ETag? etag)
    {
        Etag = etag;
        ContentMd5 = contentMd5;
        BlobName = blobName;
    }

    public override string ToString()
    {
        return $"{BlobName}({ContentMd5}:{Etag}";
    }
}