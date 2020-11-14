using DirectoryComparer.Components;
using DirectoryComparer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DirectoryComparer
{
    public partial class MainForm : Form
    {

        // Option to delete all matching files under a path or all other files

        // 1.   Enumerate files in the given directories
        // 2.   Determine the required properties about each file

        public MainForm()
        {
            InitializeComponent();
        }


        private string[] RemoveInvalidFolders(string[] paths)
        {
            var validPaths = new List<string>();

            foreach (var path in paths)
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    validPaths.Add(path);

            return validPaths.ToArray();
        }


        private void BrowseButton_Click(object sender, EventArgs e)
        {
            try
            {
                DirectoriesTextBox.Lines = RemoveInvalidFolders(DirectoriesTextBox.Lines);

                var lines = DirectoriesTextBox.Lines;
                int lineCount = lines != null ? lines.Length : 0;

                string lastLine = lineCount > 0 ? lines.Last() : null;
                string initialFolder = lastLine;

                if (!Directory.Exists(initialFolder))
                    initialFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // TODO - Just use folder as set and get rather than extra initial folder
                var dialog = new FolderDialog() {
                    InitialFolder = initialFolder,
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var path in lines)
                        if (dialog.Folder.StartsWith(path))
                            throw new Exception($"That folder is within '{path}'");

                    string append = (lastLine != null ? Environment.NewLine : null)
                        + dialog.Folder + Environment.NewLine;
                    DirectoriesTextBox.AppendText(append);
                }
            }
            catch (Exception ex)
            {
                ToolStripStatusLabel.Text = $"Folder dialog error: {ex.Message}";
            }
        }

        // if modified < created - then the file is very old and likely a copy so can be deleted

        private async void StartButton_Click(object sender, EventArgs e)
        {
            StartButton.Enabled = false;

            try {
                var directories = DirectoriesTextBox.Lines.ToList();

                await FolderWalker.WalkAsync(directories);

                var files = await FolderWalker.GetFileDetailsAsync();

                var table = await FolderWalker.GetTableAsync(files);

                MainDataGridView.DataSource = table;
                MainDataGridView.Refresh();
            }
            catch (Exception ex)
            {
                ToolStripStatusLabel.Text = $"Processing error: {ex.Message}";
            }
            StartButton.Enabled = true;
        }

        // TODO - Pop-up for selecting folder paths from toolbar rather than main form
        
        // TODO - Display file size as actual
        
        // TODO - Colour following matching rows in red
    }
}
