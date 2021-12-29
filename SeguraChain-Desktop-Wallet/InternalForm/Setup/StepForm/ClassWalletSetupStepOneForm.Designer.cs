
namespace SeguraChain_Desktop_Wallet.InternalForm.Setup.StepForm
{
    partial class ClassWalletSetupStepOneForm
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
            this.checkBoxSyncInternalMode = new System.Windows.Forms.CheckBox();
            this.checkBoxSyncExternalMode = new System.Windows.Forms.CheckBox();
            this.textBoxSyncExternalModeHost = new System.Windows.Forms.TextBox();
            this.textBoxSyncExternalModePort = new System.Windows.Forms.TextBox();
            this.panelSetupSyncExternalMode = new System.Windows.Forms.Panel();
            this.labelSyncExternalModePort = new System.Windows.Forms.Label();
            this.labelSyncExternalModeHost = new System.Windows.Forms.Label();
            this.comboBoxSetupSelectLanguage = new System.Windows.Forms.ComboBox();
            this.labelSelectLanguage = new System.Windows.Forms.Label();
            this.labelSyncInternalModeDescription = new System.Windows.Forms.Label();
            this.labelSyncExternalModeDescription = new System.Windows.Forms.Label();
            this.panelSetupSyncExternalMode.SuspendLayout();
            this.SuspendLayout();
            // 
            // checkBoxSyncInternalMode
            // 
            this.checkBoxSyncInternalMode.AutoSize = true;
            this.checkBoxSyncInternalMode.Checked = true;
            this.checkBoxSyncInternalMode.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSyncInternalMode.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.checkBoxSyncInternalMode.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.checkBoxSyncInternalMode.Location = new System.Drawing.Point(345, 86);
            this.checkBoxSyncInternalMode.Name = "checkBoxSyncInternalMode";
            this.checkBoxSyncInternalMode.Size = new System.Drawing.Size(282, 21);
            this.checkBoxSyncInternalMode.TabIndex = 0;
            this.checkBoxSyncInternalMode.Text = "CHECKBOX_SYNC_INTERNAL_MODE_TEXT";
            this.checkBoxSyncInternalMode.UseVisualStyleBackColor = true;
            this.checkBoxSyncInternalMode.CheckedChanged += new System.EventHandler(this.checkBoxSyncInternalMode_CheckedChanged);
            // 
            // checkBoxSyncExternalMode
            // 
            this.checkBoxSyncExternalMode.AutoSize = true;
            this.checkBoxSyncExternalMode.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.checkBoxSyncExternalMode.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.checkBoxSyncExternalMode.Location = new System.Drawing.Point(345, 139);
            this.checkBoxSyncExternalMode.Name = "checkBoxSyncExternalMode";
            this.checkBoxSyncExternalMode.Size = new System.Drawing.Size(284, 21);
            this.checkBoxSyncExternalMode.TabIndex = 1;
            this.checkBoxSyncExternalMode.Text = "CHECKBOX_SYNC_EXTERNAL_MODE_TEXT";
            this.checkBoxSyncExternalMode.UseVisualStyleBackColor = true;
            this.checkBoxSyncExternalMode.CheckedChanged += new System.EventHandler(this.checkBoxSyncExternalMode_CheckedChanged);
            // 
            // textBoxSyncExternalModeHost
            // 
            this.textBoxSyncExternalModeHost.Location = new System.Drawing.Point(31, 49);
            this.textBoxSyncExternalModeHost.Name = "textBoxSyncExternalModeHost";
            this.textBoxSyncExternalModeHost.Size = new System.Drawing.Size(259, 23);
            this.textBoxSyncExternalModeHost.TabIndex = 2;
            this.textBoxSyncExternalModeHost.TextChanged += new System.EventHandler(this.textBoxSyncExternalModeHost_TextChanged);
            // 
            // textBoxSyncExternalModePort
            // 
            this.textBoxSyncExternalModePort.Location = new System.Drawing.Point(31, 106);
            this.textBoxSyncExternalModePort.Name = "textBoxSyncExternalModePort";
            this.textBoxSyncExternalModePort.Size = new System.Drawing.Size(258, 23);
            this.textBoxSyncExternalModePort.TabIndex = 3;
            this.textBoxSyncExternalModePort.TextChanged += new System.EventHandler(this.textBoxSyncExternalModePort_TextChanged);
            // 
            // panelSetupSyncExternalMode
            // 
            this.panelSetupSyncExternalMode.Controls.Add(this.labelSyncExternalModePort);
            this.panelSetupSyncExternalMode.Controls.Add(this.labelSyncExternalModeHost);
            this.panelSetupSyncExternalMode.Controls.Add(this.textBoxSyncExternalModePort);
            this.panelSetupSyncExternalMode.Controls.Add(this.textBoxSyncExternalModeHost);
            this.panelSetupSyncExternalMode.Location = new System.Drawing.Point(328, 188);
            this.panelSetupSyncExternalMode.Name = "panelSetupSyncExternalMode";
            this.panelSetupSyncExternalMode.Size = new System.Drawing.Size(324, 154);
            this.panelSetupSyncExternalMode.TabIndex = 4;
            this.panelSetupSyncExternalMode.Visible = false;
            // 
            // labelSyncExternalModePort
            // 
            this.labelSyncExternalModePort.AutoSize = true;
            this.labelSyncExternalModePort.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelSyncExternalModePort.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.labelSyncExternalModePort.Location = new System.Drawing.Point(28, 83);
            this.labelSyncExternalModePort.Name = "labelSyncExternalModePort";
            this.labelSyncExternalModePort.Size = new System.Drawing.Size(258, 20);
            this.labelSyncExternalModePort.TabIndex = 5;
            this.labelSyncExternalModePort.Text = "LABEL_SYNC_EXTERNAL_MODE_PORT";
            // 
            // labelSyncExternalModeHost
            // 
            this.labelSyncExternalModeHost.AutoSize = true;
            this.labelSyncExternalModeHost.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelSyncExternalModeHost.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.labelSyncExternalModeHost.Location = new System.Drawing.Point(28, 26);
            this.labelSyncExternalModeHost.Name = "labelSyncExternalModeHost";
            this.labelSyncExternalModeHost.Size = new System.Drawing.Size(261, 20);
            this.labelSyncExternalModeHost.TabIndex = 4;
            this.labelSyncExternalModeHost.Text = "LABEL_SYNC_EXTERNAL_MODE_HOST";
            // 
            // comboBoxSetupSelectLanguage
            // 
            this.comboBoxSetupSelectLanguage.FormattingEnabled = true;
            this.comboBoxSetupSelectLanguage.Location = new System.Drawing.Point(345, 41);
            this.comboBoxSetupSelectLanguage.Name = "comboBoxSetupSelectLanguage";
            this.comboBoxSetupSelectLanguage.Size = new System.Drawing.Size(279, 23);
            this.comboBoxSetupSelectLanguage.TabIndex = 5;
            this.comboBoxSetupSelectLanguage.SelectedValueChanged += new System.EventHandler(this.comboBoxSetupSelectLanguage_SelectedValueChanged);
            // 
            // labelSelectLanguage
            // 
            this.labelSelectLanguage.AutoSize = true;
            this.labelSelectLanguage.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelSelectLanguage.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.labelSelectLanguage.Location = new System.Drawing.Point(388, 18);
            this.labelSelectLanguage.Name = "labelSelectLanguage";
            this.labelSelectLanguage.Size = new System.Drawing.Size(186, 20);
            this.labelSelectLanguage.TabIndex = 6;
            this.labelSelectLanguage.Text = "LABEL_SELECT_LANGUAGE";
            // 
            // labelSyncInternalModeDescription
            // 
            this.labelSyncInternalModeDescription.AutoSize = true;
            this.labelSyncInternalModeDescription.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelSyncInternalModeDescription.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.labelSyncInternalModeDescription.Location = new System.Drawing.Point(352, 110);
            this.labelSyncInternalModeDescription.Name = "labelSyncInternalModeDescription";
            this.labelSyncInternalModeDescription.Size = new System.Drawing.Size(270, 17);
            this.labelSyncInternalModeDescription.TabIndex = 8;
            this.labelSyncInternalModeDescription.Text = "LABEL_SYNC_INTERNAL_MODE_DESCRIPTION";
            // 
            // labelSyncExternalModeDescription
            // 
            this.labelSyncExternalModeDescription.AutoSize = true;
            this.labelSyncExternalModeDescription.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelSyncExternalModeDescription.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.labelSyncExternalModeDescription.Location = new System.Drawing.Point(352, 163);
            this.labelSyncExternalModeDescription.Name = "labelSyncExternalModeDescription";
            this.labelSyncExternalModeDescription.Size = new System.Drawing.Size(272, 17);
            this.labelSyncExternalModeDescription.TabIndex = 9;
            this.labelSyncExternalModeDescription.Text = "LABEL_SYNC_EXTERNAL_MODE_DESCRIPTION";
            // 
            // ClassWalletSetupStepOneForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(83)))), ((int)(((byte)(105)))));
            this.ClientSize = new System.Drawing.Size(998, 347);
            this.ControlBox = false;
            this.Controls.Add(this.labelSyncExternalModeDescription);
            this.Controls.Add(this.labelSyncInternalModeDescription);
            this.Controls.Add(this.labelSelectLanguage);
            this.Controls.Add(this.comboBoxSetupSelectLanguage);
            this.Controls.Add(this.checkBoxSyncInternalMode);
            this.Controls.Add(this.panelSetupSyncExternalMode);
            this.Controls.Add(this.checkBoxSyncExternalMode);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.Name = "ClassWalletSetupStepOneForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "ClassWalletSetupStepOneForm";
            this.Load += new System.EventHandler(this.ClassWalletSetupStepOneForm_Load);
            this.panelSetupSyncExternalMode.ResumeLayout(false);
            this.panelSetupSyncExternalMode.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Panel panelSetupSyncExternalMode;
        private System.Windows.Forms.Label labelSyncExternalModeHost;
        private System.Windows.Forms.ComboBox comboBoxSetupSelectLanguage;
        private System.Windows.Forms.Label labelSyncExternalModePort;
        private System.Windows.Forms.Label labelSelectLanguage;
        private System.Windows.Forms.Label labelSyncInternalModeDescription;
        private System.Windows.Forms.Label labelSyncExternalModeDescription;
        public System.Windows.Forms.TextBox textBoxSyncExternalModeHost;
        public System.Windows.Forms.TextBox textBoxSyncExternalModePort;
        public System.Windows.Forms.CheckBox checkBoxSyncInternalMode;
        public System.Windows.Forms.CheckBox checkBoxSyncExternalMode;
    }
}