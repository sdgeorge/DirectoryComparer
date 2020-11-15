using System;
using System.IO;

namespace DirectoryComparer.Models
{
    public class FileDetails
    {
        public string Name { get; set; }
        public string Hash { get; set; }
        public string Extension { get; set; }
        public string Directory { get; set; }
        public string AbsolutePath { get; set; }
        public string RelativePath { get; set; }
        public long Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public DateTime Accessed { get; set; }

        public FileDetails(string path, bool read = true, bool hash = true)
        {
            AbsolutePath = path;

            if (read) GetDetails();
            else {
                Name = Path.GetFileNameWithoutExtension(path);
                Extension = Path.GetExtension(path);
                Directory = Path.GetDirectoryName(path);
            }
            if (hash) GetHashCode();
        }

        public void GetDetails()
        {
            var info = new FileInfo(AbsolutePath);

            if (info != null && info.Exists)
            {
                Name = info.Name;
                Extension = info.Extension;
                Directory = info.DirectoryName;
                AbsolutePath = info.FullName;
                Size = info.Length;
                Created = info.CreationTime;
                Modified = info.LastWriteTime;
                Accessed = info.LastAccessTime;
            }
        }

        public void GetHash()
        {
            Hash = FileComparer.GetHashCode(AbsolutePath);
        }

        public bool Compare(FileDetails other) => FileComparer.Compare(this, other);

        public override string ToString() => Name;

        // TODO - Tidy this
        public string GetSize()
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB

            long bytes = Math.Abs(Size);

            if (bytes == 0) return "0 " + suf[0];

            int order = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, order), 2);
            return (Math.Sign(bytes) * num).ToString() + " " + suf[order];

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            //string result = String.Format("{0:0.##} {1}", len, sizes[order]);
        }
    }
}
