using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MultiDownload
{
    public partial class Form1 : Form
    {
        private static Mutex mutex = new Mutex();
        private int MULTI_TASK_COUNT = 4;
        SemaphoreSlim semaphore = new SemaphoreSlim(4);

        List<QueueFile> downloadFiles = new List<QueueFile>();
        public Form1(Form parentForm)
        {
            InitializeComponent();

            initData();

            comboBox1.SelectedIndex = 3;
        }

        private void DownloadFile_ProgressUpdate(object sender, int rowIndex, double progress)
        {

        }

        void initData()
        {
            dataGridView1.Rows.Clear();
            int i = 0;
            downloadFiles.ForEach(delegate(QueueFile file) {
                dataGridView1.Rows.Add(file.output, "%0.00", "", "", "", file.fileUrl);
                file.assignedRowID = i++;
            });
        }


        private void button1_Click(object sender, EventArgs e)
        {
            String url = textBox1.Text;
            if (!Utils.IsValidURL(url))
            {
                MessageBox.Show("Geçerli bir URL girin!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            textBox1.Text = "";

            mutex.WaitOne();

            QueueFile q = new QueueFile(url);
            int i = dataGridView1.Rows.Count;
            q.ProgressUpdate += DownloadFile_ProgressUpdate;
            q.assignedRowID = i;
            q.sm = semaphore;
            downloadFiles.Add(q);
            dataGridView1.Rows.Add(q.output, "%0.00", "", "", "", q.fileUrl);

            mutex.ReleaseMutex();
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            mutex.WaitOne();
            int selectedRow = e.Row.Index;
            if (downloadFiles[selectedRow] != null)
            {
                downloadFiles[selectedRow].cancel();
                downloadFiles.RemoveAt(selectedRow);
            }

            int rowId = 0;
            downloadFiles.ForEach(delegate (QueueFile file) {
                file.assignedRowID = rowId++;
            });
            mutex.ReleaseMutex();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                int i = row.Index;
                downloadFiles[i].cancel();
            }
            updateSelectionButtons();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                int i = row.Index;
                downloadFiles[i].status = iStatusList.STATUS_STARTING_DOWNLOAD;
            }
            updateSelectionButtons();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int removeFiles = 0;
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                int selectedRow = row.Index;
                if(downloadFiles[selectedRow].status == iStatusList.STATUS_FINISHED && removeFiles == 0)
                {
                    var result = MessageBox.Show("İndirilmiş yerel dosyaları da silmek istiyor musunuz? Hayır seçeneğini seçerseniz sadece listeden silinir.", "Silme Onayı", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.Cancel) return;
                    if (result == DialogResult.No) removeFiles = 1;
                    if (result == DialogResult.Yes) removeFiles = 2;
                }

                if (removeFiles == 2)
                {
                    var filename = Utils.GetDownloadDirectory() + downloadFiles[selectedRow].output;
                    if(File.Exists(filename))
                    {
                        File.Delete(filename);
                    }
                }

                mutex.WaitOne();

                downloadFiles[selectedRow].cancel();
                dataGridView1.Rows.RemoveAt(downloadFiles[selectedRow].assignedRowID);
                downloadFiles.RemoveAt(selectedRow);
            }

            int rowId = 0;
            downloadFiles.ForEach(delegate (QueueFile file) {
                file.assignedRowID = rowId++;
            });

            mutex.ReleaseMutex();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            mutex.WaitOne();

            String[] urls = new String[]
            {
                "http://link.testfile.org/30MB",
                "https://link.testfile.org/150MB",
                "https://link.testfile.org/300MB",
                "https://link.testfile.org/500MB",
                "https://link.testfile.org/1GB",
                "https://link.testfile.org/10GB",
                "https://proof.ovh.net/files/1Gb.dat",
                "https://proof.ovh.net/files/10Gb.dat",
                "https://speed.hetzner.de/10GB.bin",
                "https://speed.hetzner.de/1GB.bin"
            };
            for(int i = 0; i < urls.Length; i++)
            {
                QueueFile q = new QueueFile(urls[i]);
                int w = dataGridView1.Rows.Count;
                q.ProgressUpdate += DownloadFile_ProgressUpdate;
                q.assignedRowID = w;
                q.sm = semaphore;
                downloadFiles.Add(q);
                dataGridView1.Rows.Add(q.output, "%0.00", "", "", "", q.fileUrl);
            }

            mutex.ReleaseMutex();
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Get the row index at the clicked position
                int rowIndex = dataGridView1.HitTest(e.X, e.Y).RowIndex;

                // Check if a valid row is clicked
                if (rowIndex >= 0)
                {
                    if(dataGridView1.SelectedRows.Count > 1)
                    {
                        if(dosyayıGösterToolStripMenuItem.Visible == true) dosyayıGösterToolStripMenuItem.Visible = false;
                        if (dataGridView1.Rows[rowIndex].Selected) return;
                    }
                    // Clear the current selection
                    dataGridView1.ClearSelection();

                    // Select the clicked row
                    dataGridView1.Rows[rowIndex].Selected = true;

                    if(downloadFiles[rowIndex].status == iStatusList.STATUS_FINISHED)
                    {
                        dosyayıGösterToolStripMenuItem.Visible = true;
                    } 
                    else
                    {
                        dosyayıGösterToolStripMenuItem.Visible = false;
                    }

                    indirmeyiDurdurToolStripMenuItem.Enabled = button3.Enabled;
                    indirmeBaşlatToolStripMenuItem.Enabled = button4.Enabled;
                }
            }
        }

        private void indirmeBaşlatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button4_Click(sender, e);
        }

        private void indirmeyiDurdurToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button3_Click(sender, e);
        }

        private void silToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button2_Click(sender, e);
        }

        private void dosyayıGösterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count != 1) return;
            int i = dataGridView1.SelectedRows[0].Index;
            if (i >= downloadFiles.Count) return;
            string filePath = Path.GetFullPath(Utils.GetDownloadDirectory() + downloadFiles[i].output);
            if (!File.Exists(filePath))
            {
                return;
            }

            // combine the arguments together
            // it doesn't matter if there is a space after ','
            string argument = "/select,\"" + filePath + "\"";
            Process.Start("explorer.exe", argument);
        }

        private void updateSelectionButtons()
        {
            bool button4Enabled = false;
            bool button3Enabled = false;
            if (dataGridView1.SelectedRows.Count == 1)
            {
                int i = dataGridView1.SelectedRows[0].Index;
                if (i >= downloadFiles.Count)
                {
                    return;
                }
                if (downloadFiles[i].status != iStatusList.STATUS_FINISHED && downloadFiles[i].status != iStatusList.STATUS_DOWNLOADING) button4Enabled = true;
                if (downloadFiles[i].status == iStatusList.STATUS_DOWNLOADING) button3Enabled = true;
            } 
            else
            {
                if (dosyayıGösterToolStripMenuItem.Visible) dosyayıGösterToolStripMenuItem.Visible = false;
                button4Enabled = true;
                button3Enabled = true;
            }
            if (button4.Enabled != button4Enabled) button4.Enabled = button4Enabled;
            if (button3.Enabled != button3Enabled) button3.Enabled = button3Enabled;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            updateSelectionButtons();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            for(int i = 0; i < downloadFiles.Count; i++)
            {
                downloadFiles[i].status = iStatusList.STATUS_STARTING_DOWNLOAD;
            }
            updateSelectionButtons();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < downloadFiles.Count; i++)
            {
               downloadFiles[i].cancel();
            }
            updateSelectionButtons();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Bütün indirmeleri silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
            if (result == DialogResult.No) return;

            mutex.WaitOne();

            for (int i = 0; i < downloadFiles.Count; i++)
            {
                downloadFiles[i].cancel();
            }
            downloadFiles.Clear();
            dataGridView1.Rows.Clear();
            updateSelectionButtons();

            mutex.ReleaseMutex();
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            mutex.WaitOne();
            for (int d = 0; d < downloadFiles.Count; d++)
            {
                QueueFile file = downloadFiles[d];
                if(file.status == iStatusList.STATUS_RESTARTING)
                {
                    file.restart();
                }
                if (file.status == iStatusList.STATUS_STARTING_DOWNLOAD)
                {
                    file.download();        
                }
                int i = downloadFiles[d].assignedRowID;
                if (i >= dataGridView1.Rows.Count) continue;
                if (dataGridView1.Rows[i].Cells[0].Value.ToString() != file.output) dataGridView1.Rows[i].Cells[0].Value = file.output;
                if (file.status == iStatusList.STATUS_FAILED)
                    dataGridView1.Rows[i].Cells[1].Value = file.error;
                else
                    dataGridView1.Rows[i].Cells[1].Value = String.Format("%{0:N2}", file.progress.Progress);
                if (file.fileSize == 0)
                    dataGridView1.Rows[i].Cells[2].Value = "-";
                else if (file.status == iStatusList.STATUS_DOWNLOADING || file.status == iStatusList.STATUS_PAUSED || file.status == iStatusList.STATUS_FAILED)
                    dataGridView1.Rows[i].Cells[2].Value = String.Format("{0}/{1}", Utils.GetReadableFileSize(file.progress.TotalDownload), Utils.GetReadableFileSize(file.fileSize));
                else
                    dataGridView1.Rows[i].Cells[2].Value = String.Format("{0}", Utils.GetReadableFileSize(file.fileSize));
                if (file.progress.Speed > 0 && file.status == iStatusList.STATUS_DOWNLOADING)
                    dataGridView1.Rows[i].Cells[3].Value = Utils.GetReadableFileSize(Convert.ToInt64(file.progress.Speed)) + "/s";
                else
                    dataGridView1.Rows[i].Cells[3].Value = "";
                if (file.progress.Speed > 0 && file.status == iStatusList.STATUS_DOWNLOADING)
                    dataGridView1.Rows[i].Cells[4].Value = file.GetElapsedTime();
                else
                    dataGridView1.Rows[i].Cells[4].Value = "";
            }
            mutex.ReleaseMutex();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy) backgroundWorker1.RunWorkerAsync();
            updateSelectionButtons();
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            String w = comboBox1.SelectedItem.ToString();
            int r = Convert.ToInt32(w);
            this.MULTI_TASK_COUNT = r;
            if(!backgroundWorker2.IsBusy) backgroundWorker2.RunWorkerAsync();
            ActiveControl = null;
        }

        private void backgroundWorker2_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            List<int> downloadPausedContinue = new List<int>();

            for (int i = 0; i < downloadFiles.Count; i++)
            {
                if (downloadFiles[i].status == iStatusList.STATUS_DOWNLOADING)
                {
                    downloadPausedContinue.Add(i);
                    downloadFiles[i].cancel();
                }
            }

            Thread.Sleep(1000);

            semaphore = new SemaphoreSlim(MULTI_TASK_COUNT);

            for (int i = 0; i < downloadFiles.Count; i++)
            {
                downloadFiles[i].sm = semaphore;
            }
          
            downloadPausedContinue.ForEach(delegate (int value)
            { 
                downloadFiles[value].status = iStatusList.STATUS_STARTING_DOWNLOAD;
            });
        }

        private void backgroundWorker3_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            int total = 0;
            double totalProgress = 0;

            if (downloadFiles.Count == 0 && progressBar1.Visible) progressBar1.Visible = false;
            if (downloadFiles.Count > 0 && !progressBar1.Visible) progressBar1.Visible = true;

            for (int i = 0; i < downloadFiles.Count; i++)
            {
                var status = downloadFiles[i].status;
                if (status == iStatusList.STATUS_DOWNLOADING || status == iStatusList.STATUS_FINISHED || status == iStatusList.STATUS_PAUSED)
                {
                    totalProgress += downloadFiles[i].progress.Progress;
                    total++;
                }
            }

            if (total == 0)
            {
                if (progressBar1.Visible) progressBar1.Visible = false;
                return;
            }

            progressBar1.Value = (int)totalProgress / total;
        }

        private void yenidenBaşlatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                int i = row.Index;
                downloadFiles[i].status = iStatusList.STATUS_RESTARTING;
            }      
        }
    }
}
