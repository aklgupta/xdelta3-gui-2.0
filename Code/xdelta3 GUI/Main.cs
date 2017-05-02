using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace xdelta3_GUI
{
    public partial class Main : Form
    {
        private ListBox openFileDialogueCaller;
        private List<string> oldFiles, oldFileNames, newFiles, newFileNames;

        public Main()
        {
            this.InitializeComponent();
            this.oldFiles = new List<string>();
            this.oldFileNames = new List<string>();
            this.newFiles = new List<string>();
            this.newFileNames = new List<string>();
                                 
        }

        #region Button_Click_Events

     

        private void create_Click(object sender, EventArgs e)
        {
            string dest = this.destinationTextBox.Text;
            string subDir = this.patchSubDirTextBox.Text;
            string xdeltaargs = this.xdeltaargs.Text;
            string zipName = this.zipNameTextBox.Text;
            string patchExt = this.patchExtTextBox.Text.Trim();

            if (this.oldFiles.Count != this.newFiles.Count)
            {
                MessageBox.Show("Number of files don't match. Please ensure there are the same number of files on both sides.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (this.oldFiles.Count == 0)
            {
                MessageBox.Show("No files to patch!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (this.destinationTextBox.Text == "")
            {
                MessageBox.Show("Please enter a destination.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (this.xdeltaargs.Text == "")
            { 
                MessageBox.Show("Please enter xdelta compression arguments.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Path.IsPathRooted(dest) || dest.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                MessageBox.Show("Invalid destination path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (subDir.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                MessageBox.Show("Invalid subdirectory name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (zipName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                MessageBox.Show("Invalid .zip file name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if(string.IsNullOrEmpty(patchExt)) {
                MessageBox.Show("Please enter a patch file extension.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else {
                //Should cover all invalid characters for a file extension, except spaces and dot(.)
                foreach(char ch in Path.GetInvalidFileNameChars()) {
                    if(patchExt.Contains(ch.ToString())) {
                        MessageBox.Show("The patch file extension is invalid. Please check it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                //Need to check for spaces(optional) and dot(.) separately
                if(patchExt.Contains(" ") || patchExt.Contains(".")) {
                    MessageBox.Show("The patch file extension is invalid. Please check it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            }

            dest = (dest.EndsWith("\\") || dest.EndsWith("/")) ? dest : dest + "\\";

            if (subDir != "")
                subDir += "\\";

            string tempDir = "";
            if (this.zipCheckBox.Checked)
            {
                char[] chars = new char[17];
                Random random = new Random();
                for (int i = 0; i < 16; i++)
                {
                    int c = random.Next(48, 64);
                    if (c > 57)
                        c += 39;
                    chars[i] = (char)c;
                }
                chars[16] = '\\';
                tempDir = new string(chars);
            }

            if (!Directory.Exists(dest + tempDir + subDir))
                Directory.CreateDirectory(dest + tempDir + subDir);



            //Patch creater//
                //Readme.txt creation//
            StreamWriter readmeWriter = new StreamWriter(dest + tempDir + "1.Readme.txt");
            readmeWriter.WriteLine("1. Copy your original files into this folder. DO NOT RENAME!!"); 
            readmeWriter.WriteLine("2. Run the 3.Apply Patch file, a CMD window will open and will start patching automatically.");
            readmeWriter.WriteLine("3. Once patching is complete you will find your new files in the main folder and the orginals in a folder called 'Old'.");
            readmeWriter.WriteLine("4. Enjoy.");
            readmeWriter.Close();

                //Changelog.txt creation//
            StreamWriter changelogWriter = new StreamWriter(dest + tempDir + "2.Changelog.txt");
            changelogWriter.Close();

                //Batch creation//
            StreamWriter patchWriter = new StreamWriter(dest + tempDir + "3.Apply Patch.bat");
            patchWriter.WriteLine("@echo off");
            patchWriter.WriteLine("mkdir old");
            for (int i = 0; i < this.oldFiles.Count; i++)
            {
                patchWriter.WriteLine(".\\" + subDir + "xdelta-3.1.0-x86_64.exe -v -d -s \"{0}\" " + "\".\\" + subDir + "{0}." + patchExt + "\" \"{2}\"", this.oldFileNames[i], subDir + (i + 1).ToString(), this.newFileNames[i]);
                patchWriter.WriteLine("move \"{0}\" old", this.oldFileNames[i]);
                if (!batchOnlyCheckBox.Checked)
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "xdelta-3.1.0-x86_64.exe";
                    p.StartInfo.Arguments = xdeltaargs + " " + "\"" + this.oldFiles[i] + "\" \"" + this.newFiles[i] + "\" \"" + dest + tempDir + subDir + this.oldFileNames[i] + "." + patchExt + "\"";
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    p.WaitForExit();
                }
                
            }
            patchWriter.Close();
            MessageBox.Show("Patch(s) created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (this.batchOnlyCheckBox.Checked)
            {
                StreamWriter makePatchWriter = new StreamWriter(dest + "Make Patch.bat");
                for (int i = 0; i < this.oldFiles.Count; i++)
                    makePatchWriter.WriteLine(".\\" + subDir + "xdelta-3.1.0-x86_64.exe " + xdeltaargs + " " + "\"{0}\" \"{1}\" \"{0}." + patchExt + "\"", this.oldFiles[i], this.newFiles[i], dest + subDir + (i + 1).ToString());
                File.Copy("xdelta-3.1.0-x86_64.exe", dest + tempDir + subDir + "xdelta-3.1.0-x86_64.exe", true);    
                makePatchWriter.Close();

            }

            if (this.copyxdeltaCheckBox.Checked)
                File.Copy("xdelta-3.1.0-x86_64.exe", dest + tempDir + subDir + "xdelta-3.1.0-x86_64.exe", true);

            if (this.zipCheckBox.Checked)
            {
                StreamWriter listWriter = new StreamWriter(dest + tempDir + "File List.txt");
                if (this.copyxdeltaCheckBox.Checked)
                    listWriter.WriteLine("xdelta-3.1.0-x86_64.exe");
                listWriter.WriteLine("Apply Patch.bat");
                for (int i = 0; i < this.oldFiles.Count; i++)
                    listWriter.WriteLine(subDir + (i + 1).ToString() + "." + patchExt);
                listWriter.Close();

                Process p = new Process();
                p.StartInfo.FileName = Directory.GetCurrentDirectory() + "\\7za.exe";
                p.StartInfo.WorkingDirectory = dest + tempDir;
                p.StartInfo.Arguments = "a -tzip \"" + dest + zipName + ".zip\" -mx9 @\"" + dest + tempDir + "File List.txt\"";
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
                Directory.Delete(dest + tempDir, true);
            }
        }
    
        private void clear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Clear all fields?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                
                this.oldFiles.Clear();
                this.oldFileNames.Clear();
                this.newFiles.Clear();
                this.newFileNames.Clear();
                this.destinationTextBox.Clear();
                this.oldListBox.DataSource = null;
                this.newListBox.DataSource = null;
                this.patchSubDirTextBox.Clear();
                this.patchExtTextBox.Clear();
                this.zipNameTextBox.Clear();
                this.zipCheckBox.Checked = false;
                this.copyxdeltaCheckBox.Checked = false;
                this.batchOnlyCheckBox.Checked = false;
                this.fullPathCheckBox.Checked = false;
            }
        }

        private void destinationButton_Click(object sender, EventArgs e)
        {
           // if (this.folderBrowserDialog.ShowDialog() == DialogResult.OK)
           // this.destinationTextBox.Text = this.folderBrowserDialog.SelectedPath;

            //Open updated folder browser window
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = destinationTextBox.Text;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                destinationTextBox.Text = dialog.FileName;
            }
        }

        private void move(ListBox box, List<string> files, List<string> fileNames, bool up)
        {
            ListBox.SelectedIndexCollection selection = box.SelectedIndices;
            List<int> indices = new List<int>();
            foreach (int s in selection)
                indices.Add(s);
            indices.Sort();

            int top = up ? 0 : files.Count - 1;
            for (int i = up ? 0 : indices.Count - 1; up ? i < indices.Count : i >= 0; i += up ? 1 : -1)
            {
                if (indices[i] == top)
                {
                    top += up ? 1 : -1;
                    continue;
                }

                int offset = up ? -1 : 1;
                string temp = files[indices[i] + offset];
                files[indices[i] + offset] = files[indices[i]];
                files[indices[i]] = temp;

                temp = fileNames[indices[i] + offset];
                fileNames[indices[i] + offset] = fileNames[indices[i]];
                fileNames[indices[i]] = temp;

                indices[i] += offset;
            }

            box.DataSource = null;
            box.DataSource = this.fullPathCheckBox.Checked ? files : fileNames;
            box.ClearSelected();
            foreach (int i in indices)
                box.SelectedIndex = i;
        }

        //Enable drag and drop to OLD List//
        private void oldListBox_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private string OldGetFileName(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        private void oldListBox_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            //Take dropped items and store in array
            string[] OldDroppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            //Loop through all dropped items and display them
            foreach (string file in OldDroppedFiles)
                //   oldListBox.Items.Add(file);
            {
                string filename = OldGetFileName(file);
                oldListBox.Items.Add(filename);
            }
                  
        }
        
        //Enable drag and drop to NEW List//
        private void newListBox_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }
        private string NewGetFileName(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        private void newListBox_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            //Take dropped items and store in array
            string[] NewDroppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            //Loop through all dropped items and display them
            foreach (string file in NewDroppedFiles)
            //   newListBox.Items.Add(file);
            {
                string filename = NewGetFileName(file);
                newListBox.Items.Add(filename);
            }
        }
        
        private void upOld_Click(object sender, EventArgs e) { this.move(oldListBox, oldFiles, oldFileNames, true); }

        private void upNew_Click(object sender, EventArgs e) { this.move(newListBox, newFiles, newFileNames, true); }

        private void downOld_Click(object sender, EventArgs e) { this.move(oldListBox, oldFiles, oldFileNames, false); }

        private void downNew_Click(object sender, EventArgs e) { this.move(newListBox, newFiles, newFileNames, false); }

        private void addNewButton_Click(object sender, EventArgs e)
        {
            this.openFileDialogueCaller = this.newListBox;
            this.openFileDialog.ShowDialog();
        }

        private void addOldButton_Click(object sender, EventArgs e)
        {
            this.openFileDialogueCaller = this.oldListBox;
            this.openFileDialog.ShowDialog();
        }

        private void remove(ListBox box, List<string> files, List<string> fileNames)
        {
            ListBox.SelectedIndexCollection selection = box.SelectedIndices;
            List<int> indices = new List<int>();
            foreach (int s in selection)
                indices.Add(s);
            indices.Sort();

            for (int i = indices.Count - 1; i >= 0; i--)
            {
                files.RemoveAt(indices[i]);
                fileNames.RemoveAt(indices[i]);
            }

            box.DataSource = null;
            box.DataSource = this.fullPathCheckBox.Checked ? files : fileNames;
        }

        private void removeOld_Click(object sender, EventArgs e) { this.remove(oldListBox, oldFiles, oldFileNames); }

        private void removeNew_Click(object sender, EventArgs e) { this.remove(newListBox, newFiles, newFileNames); }

        #endregion

        #region Checkbox_Change_Events

        private void fullPathCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection oldSelection = this.oldListBox.SelectedIndices;
            ListBox.SelectedIndexCollection newSelection = this.newListBox.SelectedIndices;

            List<int> oldIndices = new List<int>();
            List<int> newIndices = new List<int>();

            foreach (int s in oldSelection)
                oldIndices.Add(s);
            foreach (int s in newSelection)
                newIndices.Add(s);

            this.oldListBox.DataSource = this.fullPathCheckBox.Checked ? this.oldFiles : this.oldFileNames;
            this.newListBox.DataSource = this.fullPathCheckBox.Checked ? this.newFiles : this.newFileNames;

            this.oldListBox.ClearSelected();
            this.newListBox.ClearSelected();

            foreach (int s in oldIndices)
                this.oldListBox.SelectedIndex = s;
            foreach (int s in newIndices)
                this.newListBox.SelectedIndex = s;
        }

        private void zipCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.zipNameTextBox.Enabled = !this.zipNameTextBox.Enabled;
            this.zipNameLabel.Enabled = !this.zipNameLabel.Enabled;
            this.batchOnlyCheckBox.Enabled = !this.zipCheckBox.Checked;
        }

        private void batchOnlyCheckBox_CheckedChanged(object sender, EventArgs e) { this.zipCheckBox.Enabled = !this.batchOnlyCheckBox.Checked; }

        #endregion

        #region Dialog_Events

        private void openFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            string[] files = new string[0];

            try { files = this.openFileDialog.FileNames; }
            catch (NotSupportedException)
            {
                MessageBox.Show("Unsupported file path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (this.openFileDialogueCaller == this.newListBox)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    if (this.newFiles.Contains(files[i]))
                        continue;
                    this.newFiles.Add(files[i]);

                    string[] parts = files[i].Split('\\');
                    this.newFileNames.Add(parts[parts.Length - 1]);
                }

                this.newListBox.DataSource = null;
                this.newListBox.DataSource = this.fullPathCheckBox.Checked ? this.newFiles : this.newFileNames;
            }
            else if (this.openFileDialogueCaller == this.oldListBox)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    if (this.oldFiles.Contains(files[i]))
                        continue;
                    this.oldFiles.Add(files[i]);

                    string[] parts = files[i].Split('\\');
                    this.oldFileNames.Add(parts[parts.Length - 1]);
                }

                this.oldListBox.DataSource = null;
                this.oldListBox.DataSource = this.fullPathCheckBox.Checked ? this.oldFiles : this.oldFileNames;
            }
        }

        #endregion

        

                   
    }
}