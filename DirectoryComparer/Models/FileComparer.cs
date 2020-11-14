using System;
using System.IO;
using System.Security.Cryptography;

namespace DirectoryComparer.Models
{
    public static class FileComparer
    {
        //public static float Threshold;

        public static string GetHashCode(string filepath)
        {
            string hash = null;
            if (File.Exists(filepath))
                using (var stream = File.OpenRead(filepath))
                    try { hash = GetSha256Hash(stream); } catch { }
            return hash;
        }

        public static string GetMd5Hash(FileStream file)
        {
            var md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(file);
            return Convert.ToBase64String(hash);
        }

        public static string GetSha256Hash(FileStream file)
        {
            var sha256 = new SHA256Managed();
            byte[] hash = sha256.ComputeHash(file);
            return Convert.ToBase64String(hash);
            //string hashString = string.Empty;
            //foreach (byte x in hash) hashString += String.Format("{0:x2}", x);
            //return hashString;
            //return BitConverter.ToString(hash).Replace("-", String.Empty);
        }

        static bool CompareAllBytes(FileDetails first, FileDetails second)
        {
            using (var fs1 = File.OpenRead(first.AbsolutePath))
            using (var fs2 = File.OpenRead(second.AbsolutePath))
                for (int i = 0; i < first.Size; i++)
                    if (fs1.ReadByte() != fs2.ReadByte())
                        return false;
            return true;
        }

        static bool CompareBytes(FileDetails first, FileDetails second)
        {
            const int BYTES_TO_READ = sizeof(Int64);

            int iterations = (int)Math.Ceiling((double)first.Size / BYTES_TO_READ);

            using (var fs1 = File.OpenRead(first.AbsolutePath))
            using (var fs2 = File.OpenRead(second.AbsolutePath))
            {
                var one = new byte[BYTES_TO_READ];
                var two = new byte[BYTES_TO_READ];

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }
            return true;
        }

        public static bool Compare(FileDetails first, FileDetails second)
        {
            if (first.AbsolutePath == second.AbsolutePath)
                return true;
            if (!string.IsNullOrWhiteSpace(first.Hash) && first.Hash == second.Hash)
                return true;
            return first.Name.Equals(second.Name, StringComparison.OrdinalIgnoreCase) && first.Size == second.Size;
        }

        /*
        const int MAX_MATCHES = 4;

        public static float Compare(FileDetails first, FileDetails second)
        {
            int matches = 0;

            if (first.AbsolutePath == second.AbsolutePath)
                return 1.0f;

            if (first.Size == second.Size)
                ++matches;
            if (first.Name.Equals(second.Name, StringComparison.OrdinalIgnoreCase))
                ++matches;
            if (first.Modified == second.Modified)
                ++matches;
            if (first.Hash == second.Hash)
                ++matches;

            return matches / MAX_MATCHES;
        }
        */

        //public static bool IsMatch(string filepath1, string filepath2)
        //    => Compare(filepath1, filepath2) > Threshold;
    }
}
