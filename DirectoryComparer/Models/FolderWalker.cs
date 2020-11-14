using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DirectoryComparer.Models
{
    public static class FolderWalker
    {
        private static List<string> FolderBlacklist = new List<string> { ".git", ".vs", "node_modules" };

        private static List<string> ExtensionBlacklist = new List<string> { ".log", ".tlog", ".gitignore" };

        private static Dictionary<string, List<FileDetails>> DetailsDictionary;

        private static string LastFolder;

        // TODO - Return events for each file added, including details read to handle progress

        // TODO - Specify directory names to ignore, such as node_modules - these will waste time parsing, just option to delete?

        // TODO - Specify cores for Parallel

        public static async Task WalkAsync(List<string> folders)
            => await Task.Run(() => { Walk(folders); });
        
        public static void Walk(List<string> folders)
        {
            try
            {
                DetailsDictionary = new Dictionary<string, List<FileDetails>>();

                foreach (var folder in folders)
                {
                    LastFolder = folder;
                    Walk(folder);
                }
                //Parallel.ForEach(folders, folder => {
                //    LastFolder = folder;
                //    Walk(folder); 
                //});
            }
            catch (Exception ex)
            {
                // TODO - Report global error
            }
        }

        // Breadth first
        public static void Walk(string folder)
        {
            string folderName = Path.GetFileName(folder);

            if (FolderBlacklist.Contains(folderName)) return;

            if (!Directory.Exists(folder)) return;

            var files = Directory.EnumerateFiles(folder);

            foreach (var file in files)
            {
                try
                {
                    var details = new FileDetails(file, true, false);

                    if (ExtensionBlacklist.Contains(details.Extension)) continue;

                    details.RelativePath = details.AbsolutePath.Substring(LastFolder.Length + 1);
                    details.GetHash();

                    if (!DetailsDictionary.ContainsKey(details.Hash))
                    {
                        DetailsDictionary[details.Hash] = new List<FileDetails>();
                    }
                    DetailsDictionary[details.Hash].Add(details);
                }
                catch (Exception ex)
                {
                    // TODO - Report error file
                }
            }

            var directories = Directory.EnumerateDirectories(folder);

            foreach (var directory in directories)
            {
                Walk(directory);
            }
        }

        public static async Task<List<FileDetails>> GetFileDetailsAsync()
            => await Task.Run(() => { return GetFileDetails(); });

        // TODO - Special sorting algorithm for weighting multiple files against file size

        public static List<FileDetails> GetFileDetails()
        {
            var detailsList = DetailsDictionary.Select(kvp => kvp.Value)
                .Where(list => list.Count > 1)
                .OrderByDescending(list => list.Sum(file => file.Size));
            //.OrderByDescending(list => list.Count);

            return detailsList.SelectMany(list => list).ToList();

            //return DetailsDictionary.SelectMany(kvp => kvp.Value, (pair, details) => pair.Key, tuple.).ToList();
        }

        public static async Task<DataTable> GetTableAsync(List<FileDetails> files)
            => await Task.Run(() => { return GetTable(files); });
        
        public static DataTable GetTable(List<FileDetails> files)
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("RelativePath", typeof(string));
            table.Columns.Add("ShortSize", typeof(string));
            table.Columns.Add("Hash", typeof(string));
            table.Columns.Add("Extension", typeof(string));
            table.Columns.Add("Directory", typeof(string));
            table.Columns.Add("AbsolutePath", typeof(string));
            table.Columns.Add("Size", typeof(long));
            table.Columns.Add("Created", typeof(System.DateTime));
            table.Columns.Add("Modified", typeof(System.DateTime));
            table.Columns.Add("Accessed", typeof(System.DateTime));

            foreach (var file in files)
            {
                DataRow row = table.NewRow();
                row["Name"] = file.Name;
                row["Hash"] = file.Hash;
                row["Extension"] = file.Extension;
                row["Directory"] = file.Directory;
                row["AbsolutePath"] = file.AbsolutePath;
                row["RelativePath"] = file.RelativePath;
                row["Size"] = file.Size;
                row["ShortSize"] = file.GetSize();
                row["Created"] = file.Created;
                row["Modified"] = file.Modified;
                row["Accessed"] = file.Accessed;
                table.Rows.Add(row);
            }
            return table;
        }
    }
}
