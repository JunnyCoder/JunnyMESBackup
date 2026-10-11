using System.Windows.Forms;

namespace IPC_Backup
{
    partial class ClientMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClientMain));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.SysName = new System.Windows.Forms.ToolStripLabel();
            this.BtnPreset = new System.Windows.Forms.ToolStripButton();
            this.ServerName = new System.Windows.Forms.ToolStripLabel();
            this.BtnReconnect = new System.Windows.Forms.ToolStripButton();
            this.AutoModePanel = new System.Windows.Forms.Panel();
            this.RBtnAutoUpdate = new System.Windows.Forms.RadioButton();
            this.RBtnCheckOnly = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.BtnStop = new System.Windows.Forms.Button();
            this.BtnRun = new System.Windows.Forms.Button();
            this.panel4 = new System.Windows.Forms.Panel();
            this.LogView = new System.Windows.Forms.ListView();
            this.ManualPanel = new System.Windows.Forms.Panel();
            this.Modify = new System.Windows.Forms.Button();
            this.ServerSendingProgressBar = new System.Windows.Forms.ProgressBar();
            this.ManualModePanelLabel = new System.Windows.Forms.Label();
            this.ManualUpdate = new System.Windows.Forms.Button();
            this.ManualCheck = new System.Windows.Forms.Button();
            this.listView1 = new System.Windows.Forms.ListView();
            this.CheckBox = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SettingPanel = new System.Windows.Forms.Panel();
            this.LastCheckTime = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.AlertBox = new System.Windows.Forms.RichTextBox();
            this.toolStrip1.SuspendLayout();
            this.AutoModePanel.SuspendLayout();
            this.panel4.SuspendLayout();
            this.ManualPanel.SuspendLayout();
            this.SettingPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SysName,
            this.BtnPreset,
            this.ServerName,
            this.BtnReconnect});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(800, 26);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            this.toolStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.toolStrip1_ItemClicked);
            // 
            // SysName
            // 
            this.SysName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.SysName.Name = "SysName";
            this.SysName.Size = new System.Drawing.Size(103, 23);
            this.SysName.Text = "System Name -";
            this.SysName.Click += new System.EventHandler(this.SysName_Click);
            // 
            // BtnPreset
            // 
            this.BtnPreset.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.BtnPreset.Image = ((System.Drawing.Image)(resources.GetObject("BtnPreset.Image")));
            this.BtnPreset.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.BtnPreset.Name = "BtnPreset";
            this.BtnPreset.Size = new System.Drawing.Size(121, 23);
            this.BtnPreset.Text = "Manage Preset";
            this.BtnPreset.Click += new System.EventHandler(this.BtnPreset_Click);
            // 
            // ServerName
            // 
            this.ServerName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.ServerName.Name = "ServerName";
            this.ServerName.Size = new System.Drawing.Size(95, 23);
            this.ServerName.Text = "Target Server :";
            this.ServerName.Click += new System.EventHandler(this.ServerName_Click);
            // 
            // BtnReconnect
            // 
            this.BtnReconnect.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.BtnReconnect.Image = ((System.Drawing.Image)(resources.GetObject("BtnReconnect.Image")));
            this.BtnReconnect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.BtnReconnect.Name = "BtnReconnect";
            this.BtnReconnect.Size = new System.Drawing.Size(93, 23);
            this.BtnReconnect.Text = "Get Preset";
            this.BtnReconnect.Click += new System.EventHandler(this.BtnReconnect_Click);
            // 
            // AutoModePanel
            // 
            this.AutoModePanel.AutoSize = true;
            this.AutoModePanel.BackColor = System.Drawing.SystemColors.Control;
            this.AutoModePanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.AutoModePanel.Controls.Add(this.RBtnAutoUpdate);
            this.AutoModePanel.Controls.Add(this.RBtnCheckOnly);
            this.AutoModePanel.Controls.Add(this.label1);
            this.AutoModePanel.Controls.Add(this.BtnStop);
            this.AutoModePanel.Controls.Add(this.BtnRun);
            this.AutoModePanel.Location = new System.Drawing.Point(496, 28);
            this.AutoModePanel.Name = "AutoModePanel";
            this.AutoModePanel.Size = new System.Drawing.Size(294, 79);
            this.AutoModePanel.TabIndex = 2;
            this.AutoModePanel.Paint += new System.Windows.Forms.PaintEventHandler(this.panel3_Paint);
            // 
            // RBtnAutoUpdate
            // 
            this.RBtnAutoUpdate.AutoSize = true;
            this.RBtnAutoUpdate.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.RBtnAutoUpdate.Location = new System.Drawing.Point(184, 51);
            this.RBtnAutoUpdate.Name = "RBtnAutoUpdate";
            this.RBtnAutoUpdate.Size = new System.Drawing.Size(105, 21);
            this.RBtnAutoUpdate.TabIndex = 2;
            this.RBtnAutoUpdate.Text = "Auto Update";
            this.RBtnAutoUpdate.UseVisualStyleBackColor = true;
            this.RBtnAutoUpdate.CheckedChanged += new System.EventHandler(this.RBtnAutoUpdate_CheckedChanged);
            // 
            // RBtnCheckOnly
            // 
            this.RBtnCheckOnly.AutoSize = true;
            this.RBtnCheckOnly.Checked = true;
            this.RBtnCheckOnly.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.RBtnCheckOnly.Location = new System.Drawing.Point(184, 24);
            this.RBtnCheckOnly.Name = "RBtnCheckOnly";
            this.RBtnCheckOnly.Size = new System.Drawing.Size(98, 21);
            this.RBtnCheckOnly.TabIndex = 2;
            this.RBtnCheckOnly.TabStop = true;
            this.RBtnCheckOnly.Text = "Check Only";
            this.RBtnCheckOnly.UseVisualStyleBackColor = true;
            this.RBtnCheckOnly.CheckedChanged += new System.EventHandler(this.RBtnCheckOnly_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.label1.Location = new System.Drawing.Point(8, 4);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 18);
            this.label1.TabIndex = 1;
            this.label1.Text = "Auto Mode";
            // 
            // BtnStop
            // 
            this.BtnStop.AutoSize = true;
            this.BtnStop.Enabled = false;
            this.BtnStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnStop.Location = new System.Drawing.Point(95, 24);
            this.BtnStop.Name = "BtnStop";
            this.BtnStop.Size = new System.Drawing.Size(83, 48);
            this.BtnStop.TabIndex = 0;
            this.BtnStop.Text = "STOP";
            this.BtnStop.UseVisualStyleBackColor = true;
            this.BtnStop.Click += new System.EventHandler(this.BtnStop_Click);
            // 
            // BtnRun
            // 
            this.BtnRun.AutoSize = true;
            this.BtnRun.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnRun.Location = new System.Drawing.Point(6, 24);
            this.BtnRun.Name = "BtnRun";
            this.BtnRun.Size = new System.Drawing.Size(83, 48);
            this.BtnRun.TabIndex = 0;
            this.BtnRun.Text = "RUN";
            this.BtnRun.UseVisualStyleBackColor = true;
            this.BtnRun.Click += new System.EventHandler(this.BtnRun_Click);
            // 
            // panel4
            // 
            this.panel4.AutoSize = true;
            this.panel4.Controls.Add(this.LogView);
            this.panel4.Location = new System.Drawing.Point(12, 231);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(481, 268);
            this.panel4.TabIndex = 3;
            this.panel4.Paint += new System.Windows.Forms.PaintEventHandler(this.panel4_Paint);
            // 
            // LogView
            // 
            this.LogView.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.LogView.GridLines = true;
            this.LogView.HideSelection = false;
            this.LogView.Location = new System.Drawing.Point(0, 0);
            this.LogView.MultiSelect = false;
            this.LogView.Name = "LogView";
            this.LogView.Size = new System.Drawing.Size(478, 229);
            this.LogView.TabIndex = 0;
            this.LogView.UseCompatibleStateImageBehavior = false;
            this.LogView.View = System.Windows.Forms.View.Details;
            // 
            // ManualPanel
            // 
            this.ManualPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ManualPanel.Controls.Add(this.Modify);
            this.ManualPanel.Controls.Add(this.ServerSendingProgressBar);
            this.ManualPanel.Controls.Add(this.ManualModePanelLabel);
            this.ManualPanel.Controls.Add(this.ManualUpdate);
            this.ManualPanel.Controls.Add(this.ManualCheck);
            this.ManualPanel.Controls.Add(this.listView1);
            this.ManualPanel.Location = new System.Drawing.Point(496, 113);
            this.ManualPanel.Name = "ManualPanel";
            this.ManualPanel.Size = new System.Drawing.Size(294, 386);
            this.ManualPanel.TabIndex = 4;
            this.ManualPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // Modify
            // 
            this.Modify.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.Modify.Location = new System.Drawing.Point(213, 8);
            this.Modify.Name = "Modify";
            this.Modify.Size = new System.Drawing.Size(75, 23);
            this.Modify.TabIndex = 5;
            this.Modify.Text = "Modify";
            this.Modify.UseVisualStyleBackColor = true;
            this.Modify.Click += new System.EventHandler(this.Modify_Click);
            // 
            // ServerSendingProgressBar
            // 
            this.ServerSendingProgressBar.Enabled = false;
            this.ServerSendingProgressBar.Location = new System.Drawing.Point(6, 359);
            this.ServerSendingProgressBar.Name = "ServerSendingProgressBar";
            this.ServerSendingProgressBar.Size = new System.Drawing.Size(120, 16);
            this.ServerSendingProgressBar.TabIndex = 4;
            this.ServerSendingProgressBar.Value = 1;
            // 
            // ManualModePanelLabel
            // 
            this.ManualModePanelLabel.AutoSize = true;
            this.ManualModePanelLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.ManualModePanelLabel.Location = new System.Drawing.Point(8, 9);
            this.ManualModePanelLabel.Name = "ManualModePanelLabel";
            this.ManualModePanelLabel.Size = new System.Drawing.Size(98, 18);
            this.ManualModePanelLabel.TabIndex = 3;
            this.ManualModePanelLabel.Text = "Manual Mode";
            // 
            // ManualUpdate
            // 
            this.ManualUpdate.Enabled = false;
            this.ManualUpdate.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.ManualUpdate.Location = new System.Drawing.Point(213, 352);
            this.ManualUpdate.Name = "ManualUpdate";
            this.ManualUpdate.Size = new System.Drawing.Size(75, 29);
            this.ManualUpdate.TabIndex = 2;
            this.ManualUpdate.Text = "Update";
            this.ManualUpdate.UseVisualStyleBackColor = true;
            this.ManualUpdate.Click += new System.EventHandler(this.ManualUpdate_Click);
            // 
            // ManualCheck
            // 
            this.ManualCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.ManualCheck.Location = new System.Drawing.Point(132, 352);
            this.ManualCheck.Name = "ManualCheck";
            this.ManualCheck.Size = new System.Drawing.Size(75, 29);
            this.ManualCheck.TabIndex = 1;
            this.ManualCheck.Text = "Check";
            this.ManualCheck.UseVisualStyleBackColor = true;
            this.ManualCheck.Click += new System.EventHandler(this.ManualCheck_Click);
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.CheckBox});
            this.listView1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.listView1.GridLines = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(6, 39);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(282, 307);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // CheckBox
            // 
            this.CheckBox.Text = "";
            this.CheckBox.Width = 20;
            // 
            // SettingPanel
            // 
            this.SettingPanel.AutoSize = true;
            this.SettingPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SettingPanel.Controls.Add(this.LastCheckTime);
            this.SettingPanel.Controls.Add(this.label2);
            this.SettingPanel.Location = new System.Drawing.Point(12, 113);
            this.SettingPanel.Name = "SettingPanel";
            this.SettingPanel.Size = new System.Drawing.Size(478, 112);
            this.SettingPanel.TabIndex = 2;
            this.SettingPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.panel2_Paint);
            // 
            // LastCheckTime
            // 
            this.LastCheckTime.AutoSize = true;
            this.LastCheckTime.Location = new System.Drawing.Point(95, 13);
            this.LastCheckTime.Name = "LastCheckTime";
            this.LastCheckTime.Size = new System.Drawing.Size(35, 13);
            this.LastCheckTime.TabIndex = 1;
            this.LastCheckTime.Text = "label3";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "LatCheckTime : ";
            // 
            // AlertBox
            // 
            this.AlertBox.BackColor = System.Drawing.Color.LawnGreen;
            this.AlertBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.AlertBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 35F);
            this.AlertBox.Location = new System.Drawing.Point(12, 28);
            this.AlertBox.Multiline = false;
            this.AlertBox.Name = "AlertBox";
            this.AlertBox.ReadOnly = true;
            this.AlertBox.Size = new System.Drawing.Size(478, 79);
            this.AlertBox.TabIndex = 0;
            this.AlertBox.Text = "No Issue";
            this.AlertBox.TextChanged += new System.EventHandler(this.richTextBox1_TextChanged);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(800, 506);
            this.Controls.Add(this.AlertBox);
            this.Controls.Add(this.ManualPanel);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.AutoModePanel);
            this.Controls.Add(this.SettingPanel);
            this.Controls.Add(this.toolStrip1);
            this.Name = "MainWindow";
            this.Text = "Form1";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.AutoModePanel.ResumeLayout(false);
            this.AutoModePanel.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.ManualPanel.ResumeLayout(false);
            this.ManualPanel.PerformLayout();
            this.SettingPanel.ResumeLayout(false);
            this.SettingPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel SysName;
        private System.Windows.Forms.ToolStripLabel ServerName;
        private System.Windows.Forms.ToolStripButton BtnPreset;
        private System.Windows.Forms.ToolStripButton BtnReconnect;
        private System.Windows.Forms.Panel AutoModePanel;
        private System.Windows.Forms.Button BtnStop;
        private System.Windows.Forms.Button BtnRun;
        private System.Windows.Forms.RadioButton RBtnAutoUpdate;
        private System.Windows.Forms.RadioButton RBtnCheckOnly;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel ManualPanel;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Button ManualUpdate;
        private System.Windows.Forms.Button ManualCheck;
        private System.Windows.Forms.ColumnHeader CheckBox;
        private System.Windows.Forms.ListView LogView;
        private System.Windows.Forms.Label ManualModePanelLabel;
        private System.Windows.Forms.ProgressBar ServerSendingProgressBar;
        private System.Windows.Forms.Button Modify;
        private System.Windows.Forms.Panel SettingPanel;
        private System.Windows.Forms.RichTextBox AlertBox;
        private Label LastCheckTime;
        private Label label2;
    }
}

