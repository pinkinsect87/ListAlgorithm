namespace GPTW.ListAutomation.TestUI
{
    partial class FrmMain
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
            label1 = new Label();
            txtFileName = new TextBox();
            btnSelect = new Button();
            btnImport = new Button();
            lblStatus = new Label();
            btnExport = new Button();
            btnImportCultureBrief = new Button();
            label2 = new Label();
            txtListSourceFileId = new TextBox();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            groupBox3 = new GroupBox();
            btnExportListAlgorithm = new Button();
            txtListRequestId = new TextBox();
            label6 = new Label();
            lblAlgStatus = new Label();
            txtCountryRegionId = new TextBox();
            label5 = new Label();
            txtListCompanyId = new TextBox();
            label4 = new Label();
            btnProcess = new Button();
            label3 = new Label();
            cbAlgorithmTemplate = new ComboBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(40, 54);
            label1.Name = "label1";
            label1.Size = new Size(138, 15);
            label1.TabIndex = 0;
            label1.Text = "Please select an excel file";
            // 
            // txtFileName
            // 
            txtFileName.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            txtFileName.Location = new Point(37, 79);
            txtFileName.Name = "txtFileName";
            txtFileName.ReadOnly = true;
            txtFileName.Size = new Size(489, 25);
            txtFileName.TabIndex = 1;
            // 
            // btnSelect
            // 
            btnSelect.Location = new Point(547, 79);
            btnSelect.Name = "btnSelect";
            btnSelect.Size = new Size(75, 23);
            btnSelect.TabIndex = 2;
            btnSelect.Text = "Select...";
            btnSelect.UseVisualStyleBackColor = true;
            btnSelect.Click += btnSelect_Click;
            // 
            // btnImport
            // 
            btnImport.Location = new Point(275, 190);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(75, 35);
            btnImport.TabIndex = 3;
            btnImport.Text = "Import";
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += btnImport_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            lblStatus.ForeColor = Color.Red;
            lblStatus.Location = new Point(139, 283);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 20);
            lblStatus.TabIndex = 4;
            // 
            // btnExport
            // 
            btnExport.Location = new Point(40, 190);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(175, 37);
            btnExport.TabIndex = 5;
            btnExport.Text = "Export ORG Sample Data";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            // 
            // btnImportCultureBrief
            // 
            btnImportCultureBrief.Location = new Point(378, 190);
            btnImportCultureBrief.Name = "btnImportCultureBrief";
            btnImportCultureBrief.Size = new Size(148, 35);
            btnImportCultureBrief.TabIndex = 6;
            btnImportCultureBrief.Text = "Import Culture Brief";
            btnImportCultureBrief.UseVisualStyleBackColor = true;
            btnImportCultureBrief.Click += btnImportCultureBrief_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(40, 137);
            label2.Name = "label2";
            label2.Size = new Size(104, 15);
            label2.TabIndex = 8;
            label2.Text = "List Source File Id: ";
            // 
            // txtListSourceFileId
            // 
            txtListSourceFileId.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            txtListSourceFileId.Location = new Point(155, 134);
            txtListSourceFileId.MaxLength = 5;
            txtListSourceFileId.Name = "txtListSourceFileId";
            txtListSourceFileId.Size = new Size(100, 25);
            txtListSourceFileId.TabIndex = 9;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(txtListSourceFileId);
            groupBox1.Controls.Add(txtFileName);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(btnSelect);
            groupBox1.Controls.Add(btnImportCultureBrief);
            groupBox1.Controls.Add(btnImport);
            groupBox1.Controls.Add(btnExport);
            groupBox1.Controls.Add(lblStatus);
            groupBox1.Location = new Point(23, 27);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(671, 504);
            groupBox1.TabIndex = 10;
            groupBox1.TabStop = false;
            groupBox1.Text = "Import and export tool";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(groupBox3);
            groupBox2.Controls.Add(lblAlgStatus);
            groupBox2.Controls.Add(txtCountryRegionId);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(txtListCompanyId);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(btnProcess);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(cbAlgorithmTemplate);
            groupBox2.Location = new Point(738, 27);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(460, 504);
            groupBox2.TabIndex = 11;
            groupBox2.TabStop = false;
            groupBox2.Text = "List algorithm tool";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(btnExportListAlgorithm);
            groupBox3.Controls.Add(txtListRequestId);
            groupBox3.Controls.Add(label6);
            groupBox3.Location = new Point(37, 284);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(364, 154);
            groupBox3.TabIndex = 15;
            groupBox3.TabStop = false;
            groupBox3.Text = "Export List Algorithm";
            // 
            // btnExportListAlgorithm
            // 
            btnExportListAlgorithm.Location = new Point(171, 69);
            btnExportListAlgorithm.Name = "btnExportListAlgorithm";
            btnExportListAlgorithm.Size = new Size(101, 33);
            btnExportListAlgorithm.TabIndex = 14;
            btnExportListAlgorithm.Text = "Export";
            btnExportListAlgorithm.UseVisualStyleBackColor = true;
            btnExportListAlgorithm.Click += btnExportListAlgorithm_Click;
            // 
            // txtListRequestId
            // 
            txtListRequestId.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            txtListRequestId.Location = new Point(27, 75);
            txtListRequestId.MaxLength = 5;
            txtListRequestId.Name = "txtListRequestId";
            txtListRequestId.Size = new Size(100, 25);
            txtListRequestId.TabIndex = 13;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(27, 49);
            label6.Name = "label6";
            label6.Size = new Size(89, 15);
            label6.TabIndex = 12;
            label6.Text = "List Request Id: ";
            // 
            // lblAlgStatus
            // 
            lblAlgStatus.AutoSize = true;
            lblAlgStatus.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            lblAlgStatus.ForeColor = Color.Red;
            lblAlgStatus.Location = new Point(44, 218);
            lblAlgStatus.Name = "lblAlgStatus";
            lblAlgStatus.Size = new Size(0, 20);
            lblAlgStatus.TabIndex = 14;
            // 
            // txtCountryRegionId
            // 
            txtCountryRegionId.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            txtCountryRegionId.Location = new Point(168, 116);
            txtCountryRegionId.MaxLength = 5;
            txtCountryRegionId.Name = "txtCountryRegionId";
            txtCountryRegionId.Size = new Size(100, 25);
            txtCountryRegionId.TabIndex = 13;
            txtCountryRegionId.Text = "US";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(164, 90);
            label5.Name = "label5";
            label5.Size = new Size(103, 15);
            label5.TabIndex = 12;
            label5.Text = "countryregion_id: ";
            // 
            // txtListCompanyId
            // 
            txtListCompanyId.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            txtListCompanyId.Location = new Point(37, 116);
            txtListCompanyId.MaxLength = 5;
            txtListCompanyId.Name = "txtListCompanyId";
            txtListCompanyId.Size = new Size(100, 25);
            txtListCompanyId.TabIndex = 11;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(34, 90);
            label4.Name = "label4";
            label4.Size = new Size(99, 15);
            label4.TabIndex = 10;
            label4.Text = "List Company Id: ";
            // 
            // btnProcess
            // 
            btnProcess.Location = new Point(289, 170);
            btnProcess.Name = "btnProcess";
            btnProcess.Size = new Size(101, 33);
            btnProcess.TabIndex = 2;
            btnProcess.Text = "Process";
            btnProcess.UseVisualStyleBackColor = true;
            btnProcess.Click += btnProcess_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(37, 24);
            label3.Name = "label3";
            label3.Size = new Size(194, 15);
            label3.TabIndex = 1;
            label3.Text = "Please select an algorithm template";
            // 
            // cbAlgorithmTemplate
            // 
            cbAlgorithmTemplate.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            cbAlgorithmTemplate.FormattingEnabled = true;
            cbAlgorithmTemplate.Location = new Point(37, 47);
            cbAlgorithmTemplate.Name = "cbAlgorithmTemplate";
            cbAlgorithmTemplate.Size = new Size(279, 25);
            cbAlgorithmTemplate.TabIndex = 0;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1242, 584);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Name = "FrmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Culture Survey Desktop";
            FormClosed += FrmMain_FormClosed;
            Load += FrmMain_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label label1;
        private TextBox txtFileName;
        private Button btnSelect;
        private Button btnImport;
        private Label lblStatus;
        private Button btnExport;
        private Button btnImportCultureBrief;
        private Label label2;
        private TextBox txtListSourceFileId;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private Label label3;
        private ComboBox cbAlgorithmTemplate;
        private Button btnProcess;
        private TextBox txtListCompanyId;
        private Label label4;
        private TextBox txtCountryRegionId;
        private Label label5;
        private Label lblAlgStatus;
        private GroupBox groupBox3;
        private TextBox txtListRequestId;
        private Label label6;
        private Button btnExportListAlgorithm;
    }
}