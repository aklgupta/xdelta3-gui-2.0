using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace xdelta3_GUI
{
    public partial class Form1 : Form
    {
        ListBox openFileDialogueCaller;
        List<string> oldFiles, oldFileNames, newFiles, newFileNames;

        public Form1()
        {
            InitializeComponent();
            oldFiles = new List<string>();
            oldFileNames = new List<string>();
            newFiles = new List<string>();
            newFileNames = new List<string>();
        }

        #region Button_Click_Events

        private void create_Click(object sender, EventArgs e)
        {
            string dest = destinationTextBox.Text;
            string subDir = patchSubDirTextBox.Text;
            string zipName = zipNameTextBox.Text;

            if (oldFiles.Count != newFiles.Count)
            {
                MessageBox.Show("Number of files don't match. Please ensure there are the same number of files on both sides.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (oldFiles.Count == 0)
            {
                MessageBox.Show("No files to patch!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (destinationTextBox.Text == "")
            {
                MessageBox.Show("Please enter a destination.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (!Path.IsPathRooted(dest) || dest.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                MessageBox.Show("Invalid destination path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (subDir.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                MessageBox.Show("Invalid subdirectory name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (zipName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                MessageBox.Show("Invalid .zip file name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            dest = (dest.EndsWith("\\") || dest.EndsWith("/")) ? dest : dest + "\\";

            if (subDir != "")
                subDir += "\\";

            string tempDir = "";
            if (zipCheckBox.Checked)
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

            StreamWriter patchWriter = new StreamWriter(dest + tempDir + "patch.bat");
            for (int i = 0; i < oldFiles.Count; i++)
            {
                patchWriter.WriteLine("xdelta3.exe -d -s \"{0}\" \"{1}.diff\" \"{2}\"", oldFileNames[i], subDir + (i + 1).ToString(), newFileNames[i]);
                if (!batchOnlyCheckBox.Checked)
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "xdelta3.exe";
                    p.StartInfo.Arguments = "-e -s \"" + oldFiles[i] + "\" \"" + newFiles[i] + "\" \"" + dest + tempDir + subDir + (i + 1).ToString() + ".diff\"";
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    p.WaitForExit();
                }
            }
            patchWriter.Close();

            if (batchOnlyCheckBox.Checked)
            {
                StreamWriter makePatchWriter = new StreamWriter(dest + "makepatch.bat");
                for (int i = 0; i < oldFiles.Count; i++)
                    makePatchWriter.WriteLine("xdelta3.exe -e -s \"{0}\" \"{1}\" \"{2}.diff\"", oldFiles[i], newFiles[i], dest + subDir + (i + 1).ToString());
                makePatchWriter.Close();
            }

            if (copyxdeltaCheckBox.Checked)
                File.Copy("xdelta3.exe", dest + tempDir + "xdelta3.exe");

            if (zipCheckBox.Checked)
            {
                StreamWriter listWriter = new StreamWriter(dest + tempDir + "filelist.txt");
                if (copyxdeltaCheckBox.Checked)
                    listWriter.WriteLine("xdelta3.exe");
                listWriter.WriteLine("patch.bat");
                for (int i = 0; i < oldFiles.Count; i++)
                    listWriter.WriteLine(subDir + (i + 1).ToString() + ".diff");
                listWriter.Close();

                Process p = new Process();
                p.StartInfo.FileName = Directory.GetCurrentDirectory() + "\\7za.exe";
                p.StartInfo.WorkingDirectory = dest + tempDir;
                p.StartInfo.Arguments = "a -tzip " + dest + zipName + ".zip -mx9 @\"" + dest + tempDir + "filelist.txt\"";
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
                Directory.Delete(dest + tempDir, true);
            }
        }

        private void clear_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Clear all fields?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                oldFiles.Clear();
                oldFileNames.Clear();
                newFiles.Clear();
                newFileNames.Clear();
                destinationTextBox.Clear();
                oldListBox.DataSource = null;
                newListBox.DataSource = null;
                patchSubDirTextBox.Clear();
                zipNameTextBox.Clear();
                zipCheckBox.Checked = false;
                copyxdeltaCheckBox.Checked = false;
                batchOnlyCheckBox.Checked = false;
                fullPathCheckBox.Checked = false;
            }
        }

        private void destinationButton_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
                destinationTextBox.Text = folderBrowserDialog.SelectedPath;
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
            box.DataSource = fullPathCheckBox.Checked ? files : fileNames;
            box.ClearSelected();
            foreach (int i in indices)
                box.SelectedIndex = i;
        }

        private void upOld_Click(object sender, EventArgs e)
        {
            move(oldListBox, oldFiles, oldFileNames, true);
        }

        private void upNew_Click(object sender, EventArgs e)
        {
            move(newListBox, newFiles, newFileNames, true);
        }

        private void downOld_Click(object sender, EventArgs e)
        {
            move(oldListBox, oldFiles, oldFileNames, false);
        }

        private void downNew_Click(object sender, EventArgs e)
        {
            move(newListBox, newFiles, newFileNames, false);
        }

        private void addNewButton_Click(object sender, EventArgs e)
        {
            openFileDialogueCaller = newListBox;
            openFileDialog.ShowDialog();
        }

        private void addOldButton_Click(object sender, EventArgs e)
        {
            openFileDialogueCaller = oldListBox;
            openFileDialog.ShowDialog();
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
            box.DataSource = fullPathCheckBox.Checked ? files : fileNames;
        }

        private void removeOld_Click(object sender, EventArgs e)
        {
            remove(oldListBox, oldFiles, oldFileNames);
        }

        private void removeNew_Click(object sender, EventArgs e)
        {
            remove(newListBox, newFiles, newFileNames);
        }

        #endregion

        #region Checkbox_Change_Events

        private void fullPathCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ListBox.SelectedIndexCollection oldSelection = oldListBox.SelectedIndices;
            ListBox.SelectedIndexCollection newSelection = newListBox.SelectedIndices;

            List<int> oldIndices = new List<int>();
            List<int> newIndices = new List<int>();

            foreach (int s in oldSelection)
                oldIndices.Add(s);
            foreach (int s in newSelection)
                newIndices.Add(s);

            oldListBox.DataSource = fullPathCheckBox.Checked ? oldFiles : oldFileNames;
            newListBox.DataSource = fullPathCheckBox.Checked ? newFiles : newFileNames;

            oldListBox.ClearSelected();
            newListBox.ClearSelected();

            foreach (int s in oldIndices)
                oldListBox.SelectedIndex = s;
            foreach (int s in newIndices)
                newListBox.SelectedIndex = s;
        }

        private void zipCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            zipNameTextBox.Enabled = !zipNameTextBox.Enabled;
            zipNameLabel.Enabled = !zipNameLabel.Enabled;
            batchOnlyCheckBox.Enabled = !zipCheckBox.Checked;
        }

        private void batchOnlyCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            zipCheckBox.Enabled = !batchOnlyCheckBox.Checked;
        }

        #endregion

        #region Dialog_Events

        private void openFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            string[] files = new string[0];

            try
            {
                files = openFileDialog.FileNames;
            }
            catch (NotSupportedException)
            {
                MessageBox.Show("Unsupported file path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (openFileDialogueCaller == newListBox)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    if (newFiles.Contains(files[i]))
                        continue;
                    newFiles.Add(files[i]);

                    string[] parts = files[i].Split('\\');
                    newFileNames.Add(parts[parts.Length - 1]);
                }

                newListBox.DataSource = null;
                newListBox.DataSource = fullPathCheckBox.Checked ? newFiles : newFileNames;
            }
            else if (openFileDialogueCaller == oldListBox)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    if (oldFiles.Contains(files[i]))
                        continue;
                    oldFiles.Add(files[i]);

                    string[] parts = files[i].Split('\\');
                    oldFileNames.Add(parts[parts.Length - 1]);
                }
                oldListBox.DataSource = null;
                oldListBox.DataSource = fullPathCheckBox.Checked ? oldFiles : oldFileNames;
            }
        }

        #endregion

        /*
        private void oldListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (oldListBox.SelectedItem == null)
                return;
            oldListBox.DoDragDrop(oldListBox.SelectedItem, DragDropEffects.Move);
        }

        private void oldListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void oldListBox_DragDrop(object sender, DragEventArgs e)
        {
            Point point = oldListBox.PointToClient(new Point(e.X, e.Y));
            int index = oldListBox.IndexFromPoint(point);
            if (index < 0)
                index = oldListBox.Items.Count - 1;
            object data = e.Data.GetData(typeof(string));
            oldListBox.Items.Remove(data);
            oldListBox.Items.Insert(index, data);
        }
        */
    }
}