using DirectoryComparer.Components;
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

        // Option to delete all matching files within a path or all other files

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

        private void StartButton_Click(object sender, EventArgs e)
        {

        }
    }
}
