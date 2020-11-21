using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DirectoryComparer.Models
{
    public class FolderWalker
    {
        // Specify directory names to ignore, such as node_modules - these will waste time parsing, just option to delete?
        private static List<string> FolderBlacklist = new List<string> { ".git", ".vs", "node_modules", "bower_components" };

        private static List<string> ExtensionBlacklist = new List<string> { ".log", ".tlog", ".gitignore" };

        private Dictionary<string, List<FileDetails>> DetailsDictionary;

        private string LastFolder;

        // Make these readable
        private int TotalFiles;

        private int FileCounter;

        // Merge all of these timer methods into a class which can be inherited by this
        private Stopwatch Timer = null;

        private long LastElapsedMs = 0;

        public long ReportPeriodMs = 5000;

        public EventHandler<ErrorEventArgs> OnError;
        public EventHandler<EstimateEventArgs> OnEstimate;

        private void StartTimer()
        {
            LastElapsedMs = 0;
            Timer = Stopwatch.StartNew();
        }

        private void StopTimer() => Timer?.Stop();

        private void ReportError(Exception error, string message, object source = null)
            => OnError?.Invoke(null, new ErrorEventArgs(error, message, source));
        
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
