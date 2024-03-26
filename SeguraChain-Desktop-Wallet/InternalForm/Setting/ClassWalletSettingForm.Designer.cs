﻿namespace SeguraChain_Desktop_Wallet.InternalForm.Setting
{
    partial class ClassWalletSettingForm
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
            panel1 = new System.Windows.Forms.Panel();
            labelNodePort = new System.Windows.Forms.Label();
            labelNodeHost = new System.Windows.Forms.Label();
            textBoxNodePort = new System.Windows.Forms.TextBox();
            textBoxNodeHost = new System.Windows.Forms.TextBox();
            buttonCloseSettingForm = new System.Windows.Forms.Button();
            comboBoxSyncMode = new System.Windows.Forms.ComboBox();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = System.Drawing.Color.FromArgb(77, 104, 145);
            panel1.Controls.Add(comboBoxSyncMode);
            panel1.Controls.Add(labelNodePort);
            panel1.Controls.Add(labelNodeHost);
            panel1.Controls.Add(textBoxNodePort);
            panel1.Controls.Add(textBoxNodeHost);
            panel1.Location = new System.Drawing.Point(23, 26);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(331, 197);
            panel1.TabIndex = 0;
            // 
            // labelNodePort
            // 
            labelNodePort.AutoSize = true;
            labelNodePort.ForeColor = System.Drawing.SystemColors.ButtonFace;
            labelNodePort.Location = new System.Drawing.Point(51, 158);
            labelNodePort.Name = "labelNodePort";
            labelNodePort.Size = new System.Drawing.Size(38, 15);
            labelNodePort.TabIndex = 5;
            labelNodePort.Text = "PORT:";
            // 
            // labelNodeHost
            // 
            labelNodeHost.AutoSize = true;
            labelNodeHost.ForeColor = System.Drawing.SystemColors.ButtonFace;
            labelNodeHost.Location = new System.Drawing.Point(51, 118);
            labelNodeHost.Name = "labelNodeHost";
            labelNodeHost.Size = new System.Drawing.Size(40, 15);
            labelNodeHost.TabIndex = 4;
            labelNodeHost.Text = "HOST:";
            // 
            // textBoxNodePort
            // 
            textBoxNodePort.Location = new System.Drawing.Point(95, 155);
            textBoxNodePort.Name = "textBoxNodePort";
            textBoxNodePort.Size = new System.Drawing.Size(168, 23);
            textBoxNodePort.TabIndex = 3;
            // 
            // textBoxNodeHost
            // 
            textBoxNodeHost.Location = new System.Drawing.Point(95, 115);
            textBoxNodeHost.Name = "textBoxNodeHost";
            textBoxNodeHost.Size = new System.Drawing.Size(168, 23);
            textBoxNodeHost.TabIndex = 2;
            // 
            // buttonCloseSettingForm
            // 
            buttonCloseSettingForm.Location = new System.Drawing.Point(142, 232);
            buttonCloseSettingForm.Name = "buttonCloseSettingForm";
            buttonCloseSettingForm.Size = new System.Drawing.Size(75, 38);
            buttonCloseSettingForm.TabIndex = 1;
            buttonCloseSettingForm.Text = "BUTTON_CLOSE_SETTING_FORM";
            buttonCloseSettingForm.UseVisualStyleBackColor = true;
            buttonCloseSettingForm.Click += buttonCloseSettingForm_Click;
            // 
            // comboBoxSyncMode
            // 
            comboBoxSyncMode.FormattingEnabled = true;
            comboBoxSyncMode.Items.AddRange(new object[] { "INTERNAL", "EXTERNAL" });
            comboBoxSyncMode.Location = new System.Drawing.Point(95, 62);
            comboBoxSyncMode.Name = "comboBoxSyncMode";
            comboBoxSyncMode.Size = new System.Drawing.Size(168, 23);
            comboBoxSyncMode.TabIndex = 6;
            comboBoxSyncMode.SelectedIndexChanged += comboBoxSyncMode_SelectedIndexChanged;
            // 
            // ClassWalletSettingForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(49, 55, 64);
            ClientSize = new System.Drawing.Size(382, 280);
            ControlBox = false;
            Controls.Add(buttonCloseSettingForm);
            Controls.Add(panel1);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ClassWalletSettingForm";
            RightToLeftLayout = true;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "FORM_SETTING";
            Load += TITLE_FORM_SETTING_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonCloseSettingForm;
        private System.Windows.Forms.Label labelNodePort;
        private System.Windows.Forms.Label labelNodeHost;
        private System.Windows.Forms.TextBox textBoxNodePort;
        private System.Windows.Forms.TextBox textBoxNodeHost;
        private System.Windows.Forms.ComboBox comboBoxSyncMode;
    }
}