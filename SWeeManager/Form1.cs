using SWeeManager.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SWeeManager
{
    public partial class Form1 : Form
    {
        private List<String> lists = new List<String>();
        private string placeholder = "Choose folder or paste the directory path";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btnGrantAll.Enabled = false;
            btnRevokeAll.Enabled = false;
        }

        private void btnChooseFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                txtFolder.Text = folderBrowser.SelectedPath;
            }
        }

        private void txtFolder_MouseDown(object sender, MouseEventArgs e)
        {
            txtFolder.Text = "";
            txtFolder.ForeColor = Color.Black;
        }

        private void txtFolder_TextChanged(object sender, EventArgs e)
        {
            var dir = txtFolder.Text;

            if (String.IsNullOrEmpty(dir) || dir.Equals(placeholder))
            {
                return;
            }

            try
            {
                var code = dir.Substring(dir.LastIndexOf('\\') + 1);

                lists = Directory.GetDirectories(dir, "*").ToList();

                var readMe = lists.Where(x => x.ToLower().Contains("readme")).First();
                if (readMe == null)
                {
                    throw new Exception("Invalid");
                }

                btnGrantAll.Enabled = true;
                btnRevokeAll.Enabled = true;
                jobBindingSource.Clear();
                foreach (var item in lists)
                {
                    if (item.ToLower().Contains("readme"))
                    {
                        continue;
                    }

                    jobBindingSource.Add(new Job()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Code = code.ToUpper(),
                        Initial = item.Substring(item.LastIndexOf('\\') + 1).ToUpper(),
                    });
                }
            }
            catch (Exception)
            {
                btnGrantAll.Enabled = false;
                btnRevokeAll.Enabled = false;
                MessageBox.Show("Invalid directory path");
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var initial = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();
            var dir = txtFolder.Text;
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Grant")
            {
                GrantAccess(dir, initial);
                MessageBox.Show("Successfully grant access for initial " + initial);
            }
            else if (dataGridView1.Columns[e.ColumnIndex].Name == "Revoke")
            {
                RevokeAccess(dir, initial);
                MessageBox.Show("Successfully revoke access for initial " + initial);
            }
        }

        private void Exec(string root, string access, string initial, string permission = "")
        {
            var command = String.Format("icacls \"{0}\" /{1} {2}{3}", root, access, initial, permission).Trim();
            try
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = command;
                process.StartInfo = startInfo;
                process.Start();
            }
            catch (Exception)
            {
                MessageBox.Show("Failed execute " + command);
            }
        }

        private void GrantAccess(string root, string initial)
        {
            Exec(root, "grant:r", initial, ":(R)");
            root = root + "\\";
            Exec(root + "readme", "grant:r", initial, ":(OI)(CI)(R)");
            Exec(root + initial, "grant:r", initial, ":(OI)(CI)(RX,WD,WEA,WA)");
        }

        private void RevokeAccess(string root, string initial)
        {
            Exec(root, "remove:g", initial);
            root = root + "\\";
            Exec(root + "readme", "remove:g", initial);
            Exec(root + initial, "remove:g", initial);
        }

        private void btnGrantAll_Click(object sender, EventArgs e)
        {
            var dir = txtFolder.Text;

            foreach (var item in lists)
            {
                if (item.ToLower().Contains("readme"))
                {
                    continue;
                }

                var initial = item.Substring(item.LastIndexOf('\\') + 1).ToUpper();

                GrantAccess(dir, initial);
            }

            MessageBox.Show("Successfully grant all access");
        }

        private void btnRevokeAll_Click(object sender, EventArgs e)
        {
            var dir = txtFolder.Text;

            foreach (var item in lists)
            {
                if (item.ToLower().Contains("readme"))
                {
                    continue;
                }

                var initial = item.Substring(item.LastIndexOf('\\') + 1).ToUpper();

                RevokeAccess(dir, initial);
            }

            MessageBox.Show("Successfully revoke all access");
        }
    }
}
