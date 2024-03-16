using System;

namespace Thetacat.ServiceClient.LocalDatabase;

public class Md5CacheDbItem
{
    public string Path { get; set; }
    public string MD5 { get; set; }
    public DateTime LastModified { get; init; }
    public long Size { get; set; }

    public Md5CacheDbItem(string path, string md5, DateTime lastModified, long size)
    {
        Path = path;
        MD5 = md5;
        LastModified = lastModified;
        Size = size;
    }
}
