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
        FolderWalker Walker;

        public enum MessageState
        {
            OK,
            Info,
            Success,
            Warning,
            Error
        }

        // TODO - Option to delete all matching files under a path or all other files

        public MainForm()
        {
            InitializeComponent();

            Walker = new FolderWalker();
            Walker.ReportPeriodMs = 200;
            Walker.OnError += this.OnError;
            Walker.OnEstimate += this.OnEstimation;

            MainProgressBar.Maximum = 1000;
            MainProgressBar.Step = 1;
            MainProgressBar.Value = 0;
            MainProgressBar.Text = "Waiting";
        }

        public static Color GetColor(MessageState state)
        {
            switch (state)
            {
                case MessageState.OK:       return Color.Black;
                case MessageState.Info:     return Color.Blue;
                case MessageState.Success:  return Color.Green;
                case MessageState.Warning:  return Color.Orange;
                case MessageState.Error:    return Color.Red;
            }
            return Color.Black;
        }

        // Running on the UI thread
        private void SetStatus(string text, MessageState state = MessageState.OK)
        {
            Invoke((MethodInvoker)delegate {
                ToolStripStatusLabel.Text = text;
                ToolStripStatusLabel.ForeColor = GetColor(state);
            });
        }

        private void OnError(object sender, Models.ErrorEventArgs args) 
            => SetStatus($"{args.Message} - {args.Error.Message}", MessageState.Error);

        // args.Elapsed.ToString("c") args.End.ToShortTimeString()
        private void OnEstimation(object sender, EstimateEventArgs args)
        {
            if (args.Source != null) {
                if (args.Source is FileDetails) {
                    var details = (FileDetails)args.Source;

                    SetStatus(details.RelativePath, MessageState.OK);
                }
            }

            Invoke((MethodInvoker)delegate {
                MainProgressBar.Maximum = args.Total;
                MainProgressBar.Value = args.Counter > MainProgressBar.Maximum ? MainProgressBar.Maximum : args.Counter;
                //MainProgressBar.Value = (int)(args.Percentage * 100);
                MainProgressBar.Refresh();
            });
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
            try {
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
                SetStatus($"Folder dialog error: {ex.Message}", MessageState.Error);
            }
        }

        // TODO - if modified < created - then the file is very old and likely a copy so can be deleted

        private async void StartButton_Click(object sender, EventArgs e)
        {
            StartButton.Enabled = false;

            MainProgressBar.Text = null;
            MainProgressBar.Maximum = 1000;
            MainProgressBar.Step = 1;
            MainProgressBar.Value = 0;
            MainProgressBar.Refresh();

            try {
                var directories = DirectoriesTextBox.Lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

                var total = Walker.GetFileCountSafe(directories);

                await Walker.WalkAsync(directories);

                var files = await Walker.GetFileDetailsAsync();

                var table = await Walker.GetTableAsync(files);

                MainDataGridView.DataSource = table;
                MainDataGridView.Refresh();

                MainProgressBar.Value = MainProgressBar.Maximum;
            }
            catch (Exception ex)
            {
                SetStatus($"Processing error: {ex.Message}", MessageState.Error);
            }
            StartButton.Enabled = true;
        }

        // TODO - Option to move common files to a single location and create a csv for restoring

        // TODO - fix progress bar

        // TODO - display table as it loads

        // TODO - option to delete Debug or Release folders

        // TODO - Pop-up for selecting folder paths from toolbar rather than main form
        
        // TODO - Colour following matching rows in red

        // TODO - report files in estimation
    }
}
