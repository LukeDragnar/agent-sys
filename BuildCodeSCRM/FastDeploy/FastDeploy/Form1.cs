using FastDeploy.Common;
using FastDeploy.Services;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace FastDeploy
{
    public partial class Form1 : Form
    {
        private readonly ProjectScannerService _scanner = new();
        private CheckBox _headerCheckBox;
        private bool _isInitializing = true;

        public Form1()
        {
            this.WindowState = FormWindowState.Maximized;
            InitializeComponent();
            AddHeaderCheckbox();
            this.Load += FormMain_Load;
            lstSelectedProject.CellMouseDown += lstSelectedProject_CellMouseDown;
            dgvProjects.CellMouseDown += dgvProjects_CellMouseDown;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            typeEnv.Items.Add(EnumEnviroment.STAGING);
            typeEnv.Items.Add(EnumEnviroment.PRODUCTION);
            typeEnv.SelectedItem = EnumEnviroment.STAGING;

            #region Config table all projects
            dgvProjects.CurrentCellDirtyStateChanged += dgvProjects_CurrentCellDirtyStateChanged;
            dgvProjects.CellValueChanged += dgvProjects_CellValueChanged;

            // Checkbox
            dgvProjects.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "colSelect",
                HeaderText = "",
                Width = 40,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False
            });

            // Project Name
            dgvProjects.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colProject",
                HeaderText = "Project",
                ReadOnly = true,
                Width = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False
            });

            // Full Path
            dgvProjects.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPath",
                HeaderText = "Full Path",
                ReadOnly = true
            });

            // Bat
            dgvProjects.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colBat",
                HeaderText = "Bat",
                ReadOnly = true
            });

            dgvProjects.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPortainer",
                HeaderText = "Portainer",
            });

            // Button Save Portainer
            var btnSavePortainer = new DataGridViewButtonColumn
            {
                Name = "colSavePortainer",
                HeaderText = "Thao tác",
                Text = "Lưu Portainer",
                UseColumnTextForButtonValue = true,
                Width = 100,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False
            };
            dgvProjects.Columns.Add(btnSavePortainer);
            #endregion

            #region Config table selected projects
            // Project Name
            lstSelectedProject.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colProject",
                HeaderText = "Project",
                ReadOnly = true,
                Width = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False
            });

            // Full Path
            lstSelectedProject.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPath",
                HeaderText = "Full Path",
                ReadOnly = true
            });

            // Old version
            lstSelectedProject.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colOldVersion",
                HeaderText = "Phiên bản cũ",
                ReadOnly = true,
                Width = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False
            });

            // Current version
            lstSelectedProject.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCurVersion",
                HeaderText = "Phiên bản hiện tại",
                ReadOnly = true,
                Width = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    //BackColor = Color.LightGray,
                    Font = new Font(lstSelectedProject.Font, FontStyle.Bold)
                }
            });

            // New version
            lstSelectedProject.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNewVersion",
                HeaderText = "Phiên bản mới",
                ReadOnly = false,
                Width = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False
            });

            lstSelectedProject.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPortainer",
                HeaderText = "Portainer",
                Visible = false
            });

            // Button Lưu file
            var btnSave = new DataGridViewButtonColumn
            {
                Name = "colSave",
                HeaderText = "Lưu phiên bản mới",
                Text = "Lưu",
                UseColumnTextForButtonValue = true,
                Width = 120,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False
            };
            lstSelectedProject.Columns.Add(btnSave);

            // Button Deploy
            var btnDeploy = new DataGridViewButtonColumn
            {
                Name = "colDeploy",
                HeaderText = "Deploy phiên bản hiện tại",
                Text = "Deploy",
                UseColumnTextForButtonValue = true,
                Width = 120,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.False
            };
            lstSelectedProject.Columns.Add(btnDeploy);
            #endregion

            #region path
            txtRootPath.PlaceholderText = "Nhập đường dẫn";
            txtRootPath.Text = _scanner.GetCachePath();
            #endregion

            _isInitializing = false;

        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            lstSelectedProject.Rows.Clear();
            _scanner.SetCachePath(txtRootPath.Text);
            SearchProject();
        }

        private void SearchProject()
        {
            dgvProjects.Rows.Clear();

            var path = txtRootPath.Text;

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                MessageBox.Show("Path không hợp lệ");
                return;
            }

            var projects = _scanner.ScanApiProjects(typeEnv.SelectedItem as string ?? string.Empty, txtRootPath.Text);

            foreach (var p in projects)
            {
                if (p.ProjectName != null)
                {
                    dgvProjects.Rows.Add(
                        false,
                        p.ProjectName,
                        p.ProjectPath,
                        p.BatPath,
                        _scanner.LoadPortainerFromXml(typeEnv.SelectedItem as string ?? string.Empty, p.ProjectName ?? string.Empty)
                    );
                }
            }
        }

        private void AddHeaderCheckbox()
        {
            _headerCheckBox = new CheckBox
            {
                Size = new Size(15, 15)
            };

            dgvProjects.Controls.Add(_headerCheckBox);

            _headerCheckBox.CheckedChanged += HeaderCheckBox_CheckedChanged;

            dgvProjects.CellPainting += DgvProjects_CellPainting;
        }

        private void DgvProjects_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex == -1 && e.ColumnIndex == dgvProjects.Columns["colSelect"].Index)
            {
                Rectangle rect = e.CellBounds;

                _headerCheckBox.Location = new Point(
                    rect.X + (rect.Width - _headerCheckBox.Width) / 2,
                    rect.Y + (rect.Height - _headerCheckBox.Height) / 2
                );
            }
        }

        private void HeaderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            dgvProjects.EndEdit();
            var check = _headerCheckBox.Checked;
            foreach (DataGridViewRow row in dgvProjects.Rows)
            {
                row.Cells["colSelect"].Value = check;
            }
        }

        private void dgvProjects_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvProjects.IsCurrentCellDirty)
            {
                dgvProjects.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgvProjects_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (dgvProjects.Columns[e.ColumnIndex].Name != "colSelect")
                return;

            var row = dgvProjects.Rows[e.RowIndex];
            bool isChecked = row.Cells["colSelect"].Value as bool? ?? false;

            var projectName = row.Cells["colProject"].Value?.ToString();
            var batPath = row.Cells["colBat"].Value?.ToString();
            var linkPortainer = row.Cells["colPortainer"].Value?.ToString();

            if (string.IsNullOrEmpty(projectName))
                return;

            if (isChecked)
            {
                // ===== ADD =====
                if (IsProjectAlreadyAdded(projectName))
                    return;

                var curVersion = GetVersionFromBat(batPath);

                lstSelectedProject.Rows.Add(
                    projectName,
                    batPath,
                    curVersion,
                    curVersion,
                    FunctionCommon.UpVersion(curVersion),
                    linkPortainer,
                    null,          // Save button
                    null          // Deploy button
                );
            }
            else
            {
                // ===== REMOVE =====
                RemoveSelectedProject(projectName);
            }
        }

        private bool IsProjectAlreadyAdded(string projectName)
        {
            return lstSelectedProject.Rows
                .Cast<DataGridViewRow>()
                .Any(r => r.Cells["colProject"].Value?.ToString() == projectName);
        }

        private void RemoveSelectedProject(string projectName)
        {
            foreach (DataGridViewRow row in lstSelectedProject.Rows)
            {
                if (row.Cells["colProject"].Value?.ToString() == projectName)
                {
                    lstSelectedProject.Rows.Remove(row);
                    break;
                }
            }
        }

        private void lstSelectedProject_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            lstSelectedProject.EndEdit();
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            //if (e.RowIndex >= 0 && e.Button == MouseButtons.Right)
            //{
            //}

            var columnName = lstSelectedProject.Columns[e.ColumnIndex].Name;

            switch (columnName)
            {
                case "colSave":
                    var rowSave = lstSelectedProject.Rows[e.RowIndex];
                    _scanner.HandleSaveBat(rowSave);
                    MessageBox.Show($"Đã lưu file bat thành công");
                    break;
                case "colDeploy":
                    var rowDeploy = lstSelectedProject.Rows[e.RowIndex];
                    _scanner.HandleDeploy(rowDeploy);
                    break;
                case "colSelect":
                    lstSelectedProject.ClearSelection();
                    lstSelectedProject.Rows[e.RowIndex].Selected = true;
                    break;
                default:
                    break;
            }
        }

        private void dgvProjects_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            dgvProjects.EndEdit();
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            //if (e.RowIndex >= 0 && e.Button == MouseButtons.Right)
            //{
            //}

            var columnName = dgvProjects.Columns[e.ColumnIndex].Name;

            switch (columnName)
            {
                case "colSavePortainer":
                    var rowSave = dgvProjects.Rows[e.RowIndex];
                    if (rowSave == null) return;

                    var colProject = rowSave.Cells["colProject"].Value?.ToString();
                    var colPortainer = rowSave.Cells["colPortainer"].Value?.ToString();
                    _scanner.SavePoitanerFromXml(typeEnv.SelectedItem as string ?? string.Empty, colProject ?? string.Empty, colPortainer ?? string.Empty);
                    MessageBox.Show($"Đã lưu đường link portainer thành công");
                    break;
                default:
                    break;
            }
        }

        private void typeEnv_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isInitializing) return;

            var selectedEnv = typeEnv.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(selectedEnv))
            {
                return;
            }

            lstSelectedProject.Rows.Clear();
            _headerCheckBox.Checked = false;
            //foreach (DataGridViewRow row in dgvProjects.Rows)
            //{
            //    row.Cells["colSelect"].Value = false;
            //}
            SearchProject();

        }

        private string GetVersionFromBat(string batPath)
        {
            if (!File.Exists(batPath))
                return string.Empty;

            var regex = new Regex(@"-\s*(\d+(\.\d+)*)");

            foreach (var line in File.ReadAllLines(batPath))
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return string.Empty;
        }

        private void SaveAll_Click(object sender, EventArgs e)
        {
            if (lstSelectedProject.Rows.Count <= 0)
            {
                MessageBox.Show($"Hãy chọn dự án để lưu", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show("Bạn có  chắc chắn muốn lưu tất cả dự án đã chọn không?",
                "Lưu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            for (int i = 0; lstSelectedProject.Rows.Count > i; i++)
            {
                var row = lstSelectedProject.Rows[i];
                if (row == null) continue;

                _scanner.HandleSaveBat(row);
            }

            MessageBox.Show($"Đã lưu các files bat thành công");

        }

        private async void deployAll_Click(object sender, EventArgs e)
        {
            if (lstSelectedProject.Rows.Count <= 0)
            {
                MessageBox.Show($"Hãy chọn dự án để deploy", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show("Bạn có  chắc chắn muốn deploy tất cả dự án đã chọn không?",
                "Deploy", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            for (int i = 0; lstSelectedProject.Rows.Count > i; i++)
            {
                var row = lstSelectedProject.Rows[i];
                if (row == null) continue;

                _scanner.HandleDeploy(row);
                await Task.Delay(2000); // delay để có thể 
            }


            MessageBox.Show($"Đã deploy các files bat thành công");
        }

        private void OpenPortainer_Click(object sender, EventArgs e)
        {
            if (lstSelectedProject.Rows.Count <= 0)
            {
                MessageBox.Show($"Hãy chọn dự án để mở portainer", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            for (int i = 0; lstSelectedProject.Rows.Count > i; i++)
            {
                var row = lstSelectedProject.Rows[i];
                if (row == null) continue;

                var linkPortainer = row.Cells["colPortainer"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(linkPortainer))
                {
                    continue;
                }

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "chrome",
                        Arguments = linkPortainer,
                        UseShellExecute = true
                    });
                }
                catch
                {
                }
            }
        }

        private void searchPrj_Changed(object sender, EventArgs e)
        {
            SearchProject();
        }

        private void layMessage_Click(object sender, EventArgs e)
        {
            if (lstSelectedProject.Rows.Count <= 0)
            {
                MessageBox.Show($"Hãy chọn dự án để tạo message", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("[DEPLOY]");

            foreach (DataGridViewRow row in lstSelectedProject.Rows)
            {
                if (row.IsNewRow) continue;

                var projectName = row.Cells["colProject"].Value?.ToString();
                var oldVersion = row.Cells["colOldVersion"].Value?.ToString();
                var curVersion = row.Cells["colCurVersion"].Value?.ToString();

                if (string.IsNullOrWhiteSpace(projectName)
                    || string.IsNullOrWhiteSpace(oldVersion)
                    || string.IsNullOrWhiteSpace(curVersion))
                    continue;

                if (oldVersion == curVersion)
                    continue; // không đổi version thì bỏ qua

                sb.AppendLine(
                    $"- {projectName} {oldVersion} → {curVersion}"
                );
            }

            var message = sb.ToString();

            Clipboard.SetText(message);
            MessageBox.Show("Đã copy message vào clipboard", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
