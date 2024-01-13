using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;

namespace Thetacat.Util;

public class Checksum
{
    public static async Task<string> GetMD5ForStream(Stream stm)
    {
        using MD5 md5 = MD5.Create();

        byte[] hash = await md5.ComputeHashAsync(stm);

        string fullContentMd5 = Convert.ToBase64String(hash);

        return fullContentMd5;
    }

    public static async Task<string> GetMD5ForPath(string path)
    {
        await using FileStream fs = File.Open(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        string md5 = await GetMD5ForStream(fs);
        fs.Close();

        return md5;
    }

    public static string GetMD5ForStreamSync(Stream stm)
    {
        using MD5 md5 = MD5.Create();

        byte[] hash = md5.ComputeHash(stm);

        string fullContentMd5 = Convert.ToBase64String(hash);

        return fullContentMd5;
    }

    public static string GetMD5ForPathSync(string path)
    {
        using FileStream fs = File.Open(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        string md5 = GetMD5ForStreamSync(fs);
        fs.Close();

        return md5;
    }
}
