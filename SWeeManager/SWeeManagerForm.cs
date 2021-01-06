using SWeeManager.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SWeeManager
{
    public partial class SWeeManagerForm : Form
    {
        private List<string> lists = new List<string>();
        private const string placeholder = "Choose folder or paste the directory path";
        private string path = String.Empty;

        public SWeeManagerForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            SetButton(false);
            SetText(placeholder, Color.Gray);
        }

        private void SetButton(bool status)
        {
            btnGrantAll.Enabled = status;
            btnRevokeAll.Enabled = status;
        }

        private void SetText(string text, Color color)
        {
            txtFolder.Text = text;
            txtFolder.ForeColor = color;
        }

        private void btnChooseFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                SetText(folderBrowser.SelectedPath, Color.Black);
                path = txtFolder.Text;
            }
        }

        private void txtFolder_Enter(object sender, EventArgs e)
        {
            SetText(String.Empty, Color.Black);
        }

        private void txtFolder_Leave(object sender, EventArgs e)
        {
            var dir = txtFolder.Text;

            if (String.IsNullOrEmpty(dir))
            {
                if (String.IsNullOrEmpty(path))
                {
                    ResetForm();
                }
                else
                {
                    SetText(path, Color.Black);
                }
            }
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
                    throw new Exception("Invalid folder path");
                }

                SetButton(true);
                dataGridViewBinding.Clear();
                foreach (var item in lists)
                {
                    if (item.ToLower().Contains("readme"))
                    {
                        continue;
                    }

                    dataGridViewBinding.Add(new Job()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Code = code.ToUpper(),
                        Username = item.Substring(item.LastIndexOf('\\') + 1).ToUpper(),
                    });
                }
                path = dir;
                ActiveControl = null;
            }
            catch (Exception)
            {
                if (!String.IsNullOrEmpty(path))
                {
                    SetText(path, Color.Black);
                }
                else
                {
                    ResetForm();
                }
                MessageBox.Show("Invalid directory path");
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var username = dataGridView.Rows[e.RowIndex].Cells[2].Value.ToString();
            var dir = txtFolder.Text;
            if (dataGridView.Columns[e.ColumnIndex].Name.Equals("Grant"))
            {
                if (IsAllowedGrantAccess(dir))
                {
                    GrantAccess(dir, username);
                    MessageBox.Show("Successfully grant access for username " + username);
                }
            }
            else if (dataGridView.Columns[e.ColumnIndex].Name.Equals("Revoke"))
            {
                RevokeAccess(dir, username);
                MessageBox.Show("Successfully revoke access for username " + username);
            }
        }

        private void Exec(string root, string access, string username, string permission = "")
        {
            var command = String.Format("/C icacls \"{0}\" /{1} {2}{3}", root, access, username, permission).Trim();
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

        private void GrantAccess(string root, string username)
        {
            Exec(root, "grant:r", username, ":(R)");
            root = root + "\\";
            Exec(root + "readme", "grant:r", username, ":(OI)(CI)(R)");
            Exec(root + username, "grant:r", username, ":(OI)(CI)(RX,WD,WEA,WA)");
        }

        private void RevokeAccess(string root, string username)
        {
            Exec(root, "remove:g", username);
            root = root + "\\";
            Exec(root + "readme", "remove:g", username);
            Exec(root + username, "remove:g", username);
        }

        private void btnGrantAll_Click(object sender, EventArgs e)
        {
            var dir = txtFolder.Text;

            if (IsAllowedGrantAccess(dir))
            {
                foreach (var item in lists)
                {
                    if (item.ToLower().Contains("readme"))
                    {
                        continue;
                    }

                    var username = item.Substring(item.LastIndexOf('\\') + 1).ToUpper();

                    GrantAccess(dir, username);
                }

                MessageBox.Show("Successfully grant all access");
            }
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

                var username = item.Substring(item.LastIndexOf('\\') + 1).ToUpper();

                RevokeAccess(dir, username);
            }

            MessageBox.Show("Successfully revoke all access");
        }

        private bool IsAllowedGrantAccess(string root)
        {
            try
            {
                var dirs = root.Split(new string[] { "\\For Ast\\" }, StringSplitOptions.None);
                if (dirs.Length != 2)
                {
                    throw new Exception("Invalid folder path");
                }

                var path = dirs[0] + "\\For Ast\\";
                foreach (var item in dirs[1].Split('\\'))
                {
                    path += item;
                    Exec(path, "grant:r", "Employee", ":(R)");
                    path += "\\";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("The root folder must be SUBCO\\For Ast\\");
                return false;
            }
            return true;
        }
    }
}
