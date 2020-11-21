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
        
        private void ReportEstimation(int total, int counter, object source = null, string message = null)
        {
            if (Timer == null) StartTimer();
            long elapsedMs = Timer.ElapsedMilliseconds;

            if (elapsedMs - LastElapsedMs > ReportPeriodMs) {
                OnEstimate?.Invoke(null, new EstimateEventArgs(total, counter, elapsedMs, source, message));
                LastElapsedMs = elapsedMs;
            }
        }

        // TODO - Report every file if desired, along with its FileDetails

        // TODO - Return events for each file added, including details read to handle progress

        // Breadth first
        private void Walk(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return;

            if (!Directory.Exists(folder)) return;

            string folderName = Path.GetFileName(folder);

            if (FolderBlacklist.Contains(folderName))
            {
                FileCounter += GetFileCount(folder);
                return;
            }
            //var options = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            //Parallel.ForEach(files, options, file => 

            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)) 
            {
                ++FileCounter;

                var details = new FileDetails(file, true, false);

                try {
                    if (ExtensionBlacklist.Contains(details.Extension)) continue;

                    details.RelativePath = details.AbsolutePath.Substring(LastFolder.Length + 1);
                    details.GetHash();

                    if (!DetailsDictionary.ContainsKey(details.Hash))
                        DetailsDictionary[details.Hash] = new List<FileDetails>();
                    
                    DetailsDictionary[details.Hash].Add(details);
                }
                catch (Exception ex)
                {
                    ReportError(ex, $"File failed", details);
                }
                ReportEstimation(TotalFiles, FileCounter, details);
            }

            foreach (var directory in Directory.EnumerateDirectories(folder, "*", SearchOption.TopDirectoryOnly))
                Walk(directory);
        }


        public void Walk(List<string> folders)
        {
            StartTimer();
            DetailsDictionary = new Dictionary<string, List<FileDetails>>();
            
            TotalFiles = GetFileCount(folders);
            FileCounter = 0;

            foreach (var folder in folders)
            {
                try
                {
                    LastFolder = folder;
                    Walk(folder);
                }
                catch (Exception ex) { ReportError(ex, $"Folder failed: {folder}"); }
            }
            StopTimer();
            LastFolder = null;
        }

        public Task WalkAsync(List<string> folders)
            => Task.Run(() => Walk(folders));

        // TODO - Special sorting algorithm for weighting multiple files against file size

        public List<FileDetails> GetFileDetails()
        {
            var detailsList = DetailsDictionary.Select(kvp => kvp.Value)
                .Where(list => list.Count > 1)
                .OrderByDescending(list => list.Sum(file => file.Size));
            //.OrderByDescending(list => list.Count);

            return detailsList.SelectMany(list => list).ToList();
            //return DetailsDictionary.SelectMany(kvp => kvp.Value, (pair, details) => pair.Key, tuple.).ToList();
        }

        public Task<List<FileDetails>> GetFileDetailsAsync()
            => Task.Run(() => GetFileDetails());

        public DataTable GetTable(List<FileDetails> files)
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

        public Task<DataTable> GetTableAsync(List<FileDetails> files)
            => Task.Run(() => GetTable(files));

        public int GetFileCount(string folder)
        {
            try {
                if (string.IsNullOrWhiteSpace(folder)) return 0;
                if (!Directory.Exists(folder)) return 0;
                // TODO - Fails on 'My Music' folder
                return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Count();
            }
            catch (Exception ex)
            { ReportError(ex, $"File count failed: {folder}"); }

            return 0;
        }

        public int GetFileCount(List<string> folders)
        {
            int fileCount = 0;
            foreach (var folder in folders)
                fileCount += GetFileCount(folder);
            return fileCount;
        }

        public Task<int> GetFileCountAsync(List<string> folders)
            => Task.Run(() => GetFileCount(folders));

        public int GetFileCountSafe(string folder)
        {
            int fileCount = 0;
            try {
                if (string.IsNullOrWhiteSpace(folder)) return 0;
                if (!Directory.Exists(folder)) return 0;

                fileCount += Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly).Count();

                foreach (var dir in Directory.EnumerateDirectories(folder, "*", SearchOption.TopDirectoryOnly))
                    try { fileCount += GetFileCount(folder); }
                    catch { fileCount += GetFileCountSafe(folder); }
            }
            catch (Exception ex)
            { ReportError(ex, $"Safe file count failed: {folder}"); }

            return fileCount;
        }

        public int GetFileCountSafe(List<string> folders)
        {
            int fileCount = 0;
            foreach (var folder in folders)
                fileCount += GetFileCountSafe(folder);
            return fileCount;
        }

        public Task<int> GetFileCountSafeAsync(List<string> folders)
            => Task.Run(() => GetFileCountSafe(folders));
    }
}
