using System;
using System.IO;
using System.Security.Cryptography;

namespace Utils
{
    public static class FileUtils
    {
        public static string GetMD5ChecksumForFile(this FileInfo fileInfo)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = fileInfo.OpenRead())
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}