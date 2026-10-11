namespace IPC_Backup_Server
{
    partial class ServerMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerMain));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.BtnServerStart = new System.Windows.Forms.ToolStripButton();
            this.LogView = new System.Windows.Forms.ListView();
            this.Time = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Log = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.BtnServerStart});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(784, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // BtnServerStart
            // 
            this.BtnServerStart.Image = ((System.Drawing.Image)(resources.GetObject("BtnServerStart.Image")));
            this.BtnServerStart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.BtnServerStart.Name = "BtnServerStart";
            this.BtnServerStart.Size = new System.Drawing.Size(86, 22);
            this.BtnServerStart.Text = "Start Server";
            this.BtnServerStart.Click += new System.EventHandler(this.BtnServerStart_Click);
            // 
            // LogView
            // 
            this.LogView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Time,
            this.Log});
            this.LogView.GridLines = true;
            this.LogView.HideSelection = false;
            this.LogView.Location = new System.Drawing.Point(5, 470);
            this.LogView.Margin = new System.Windows.Forms.Padding(0);
            this.LogView.Name = "LogView";
            this.LogView.Size = new System.Drawing.Size(775, 285);
            this.LogView.TabIndex = 1;
            this.LogView.UseCompatibleStateImageBehavior = false;
            this.LogView.View = System.Windows.Forms.View.Details;
            // 
            // Time
            // 
            this.Time.Text = "Time";
            this.Time.Width = 150;
            // 
            // Log
            // 
            this.Log.Text = "Log";
            this.Log.Width = 500;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 761);
            this.Controls.Add(this.LogView);
            this.Controls.Add(this.toolStrip1);
            this.MaximumSize = new System.Drawing.Size(800, 800);
            this.MinimumSize = new System.Drawing.Size(800, 800);
            this.Name = "Form1";
            this.Text = "Form1";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ListView LogView;
        private System.Windows.Forms.ToolStripButton BtnServerStart;
        private System.Windows.Forms.ColumnHeader Time;
        private System.Windows.Forms.ColumnHeader Log;
    }
}

