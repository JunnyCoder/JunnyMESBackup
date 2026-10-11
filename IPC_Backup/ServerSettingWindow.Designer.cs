namespace IPC_Backup
{
    partial class ServerSettingWindow
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.TextBox_ServerIP = new System.Windows.Forms.TextBox();
            this.TextBox_ServerPort = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.BtnSaveServerSet = new System.Windows.Forms.Button();
            this.TextBox_ServerPW = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(35, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server IP";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(35, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Server Port";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // TextBox_ServerIP
            // 
            this.TextBox_ServerIP.Location = new System.Drawing.Point(107, 18);
            this.TextBox_ServerIP.Name = "TextBox_ServerIP";
            this.TextBox_ServerIP.Size = new System.Drawing.Size(208, 20);
            this.TextBox_ServerIP.TabIndex = 2;
            // 
            // TextBox_ServerPort
            // 
            this.TextBox_ServerPort.Location = new System.Drawing.Point(107, 58);
            this.TextBox_ServerPort.Name = "TextBox_ServerPort";
            this.TextBox_ServerPort.Size = new System.Drawing.Size(208, 20);
            this.TextBox_ServerPort.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(35, 100);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Password";
            this.label3.Click += new System.EventHandler(this.label1_Click);
            // 
            // BtnSaveServerSet
            // 
            this.BtnSaveServerSet.Location = new System.Drawing.Point(135, 131);
            this.BtnSaveServerSet.Name = "BtnSaveServerSet";
            this.BtnSaveServerSet.Size = new System.Drawing.Size(75, 23);
            this.BtnSaveServerSet.TabIndex = 5;
            this.BtnSaveServerSet.Text = "SAVE";
            this.BtnSaveServerSet.UseVisualStyleBackColor = true;
            this.BtnSaveServerSet.Click += new System.EventHandler(this.BtnSaveServerSet_Click);
            // 
            // TextBox_ServerPW
            // 
            this.TextBox_ServerPW.Location = new System.Drawing.Point(107, 98);
            this.TextBox_ServerPW.Name = "TextBox_ServerPW";
            this.TextBox_ServerPW.Size = new System.Drawing.Size(208, 20);
            this.TextBox_ServerPW.TabIndex = 6;
            // 
            // ServerSetWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(355, 166);
            this.Controls.Add(this.TextBox_ServerPW);
            this.Controls.Add(this.BtnSaveServerSet);
            this.Controls.Add(this.TextBox_ServerPort);
            this.Controls.Add(this.TextBox_ServerIP);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Name = "ServerSetWindow";
            this.Text = "ServerSet";
            this.Load += new System.EventHandler(this.ServerSetWindow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TextBox_ServerIP;
        private System.Windows.Forms.TextBox TextBox_ServerPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button BtnSaveServerSet;
        private System.Windows.Forms.TextBox TextBox_ServerPW;
    }
}