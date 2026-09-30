namespace FastDeploy
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            dgvProjects = new DataGridView();
            txtRootPath = new TextBox();
            btnScan = new Button();
            lstSelectedProject = new DataGridView();
            label1 = new Label();
            typeEnv = new ComboBox();
            deployAll = new Button();
            SaveAll = new Button();
            OpenPortainer = new Button();
            layMessage = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvProjects).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lstSelectedProject).BeginInit();
            SuspendLayout();
            // 
            // dgvProjects
            // 
            dgvProjects.AllowUserToAddRows = false;
            dgvProjects.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvProjects.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProjects.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvProjects.Location = new Point(0, 46);
            dgvProjects.Name = "dgvProjects";
            dgvProjects.RowHeadersVisible = false;
            dgvProjects.RowTemplate.Height = 25;
            dgvProjects.Size = new Size(1016, 246);
            dgvProjects.TabIndex = 0;
            // 
            // txtRootPath
            // 
            txtRootPath.Location = new Point(11, 12);
            txtRootPath.Name = "txtRootPath";
            txtRootPath.Size = new Size(383, 23);
            txtRootPath.TabIndex = 1;
            // 
            // btnScan
            // 
            btnScan.Location = new Point(410, 12);
            btnScan.Name = "btnScan";
            btnScan.Size = new Size(101, 23);
            btnScan.TabIndex = 2;
            btnScan.Text = "Lấy dự án";
            btnScan.UseVisualStyleBackColor = true;
            btnScan.Click += btnScan_Click;
            // 
            // lstSelectedProject
            // 
            lstSelectedProject.AllowUserToAddRows = false;
            lstSelectedProject.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstSelectedProject.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            lstSelectedProject.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            lstSelectedProject.Location = new Point(0, 403);
            lstSelectedProject.Name = "lstSelectedProject";
            lstSelectedProject.RowTemplate.Height = 25;
            lstSelectedProject.Size = new Size(1016, 150);
            lstSelectedProject.TabIndex = 3;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(410, 311);
            label1.Name = "label1";
            label1.Size = new Size(256, 21);
            label1.TabIndex = 4;
            label1.Text = "Danh sách dự án đã chọn để deploy";
            // 
            // typeEnv
            // 
            typeEnv.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            typeEnv.FormattingEnabled = true;
            typeEnv.Location = new Point(883, 13);
            typeEnv.Name = "typeEnv";
            typeEnv.Size = new Size(121, 23);
            typeEnv.TabIndex = 5;
            typeEnv.SelectedIndexChanged += typeEnv_SelectedIndexChanged;
            // 
            // deployAll
            // 
            deployAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            deployAll.Location = new Point(811, 361);
            deployAll.Name = "deployAll";
            deployAll.Size = new Size(193, 23);
            deployAll.TabIndex = 6;
            deployAll.Text = "Deploy tất cả phiên bản hiện tại";
            deployAll.UseVisualStyleBackColor = true;
            deployAll.Click += deployAll_Click;
            // 
            // SaveAll
            // 
            SaveAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            SaveAll.Location = new Point(652, 361);
            SaveAll.Name = "SaveAll";
            SaveAll.Size = new Size(153, 23);
            SaveAll.TabIndex = 7;
            SaveAll.Text = "Lưu tất cả phiên bản mới";
            SaveAll.UseVisualStyleBackColor = true;
            SaveAll.Click += SaveAll_Click;
            // 
            // OpenPortainer
            // 
            OpenPortainer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            OpenPortainer.Location = new Point(548, 361);
            OpenPortainer.Name = "OpenPortainer";
            OpenPortainer.Size = new Size(98, 23);
            OpenPortainer.TabIndex = 8;
            OpenPortainer.Text = "Mở Portainer";
            OpenPortainer.UseVisualStyleBackColor = true;
            OpenPortainer.Click += OpenPortainer_Click;
            // 
            // layMessage
            // 
            layMessage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            layMessage.Location = new Point(444, 361);
            layMessage.Name = "layMessage";
            layMessage.Size = new Size(98, 23);
            layMessage.TabIndex = 10;
            layMessage.Text = "Lấy messages";
            layMessage.UseVisualStyleBackColor = true;
            layMessage.Click += layMessage_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1016, 553);
            Controls.Add(layMessage);
            Controls.Add(OpenPortainer);
            Controls.Add(SaveAll);
            Controls.Add(deployAll);
            Controls.Add(typeEnv);
            Controls.Add(label1);
            Controls.Add(lstSelectedProject);
            Controls.Add(btnScan);
            Controls.Add(txtRootPath);
            Controls.Add(dgvProjects);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)dgvProjects).EndInit();
            ((System.ComponentModel.ISupportInitialize)lstSelectedProject).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView dgvProjects;
        private TextBox txtRootPath;
        private Button btnScan;
        private DataGridView lstSelectedProject;
        private Label label1;
        private ComboBox typeEnv;
        private Button deployAll;
        private Button SaveAll;
        private Button OpenPortainer;
        private Button layMessage;
    }
}
