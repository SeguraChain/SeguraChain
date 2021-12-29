
namespace SeguraChain_Desktop_Wallet.InternalForm.Setup
{
    partial class ClassWalletSetupForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClassWalletSetupForm));
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.buttonWalletSetupSave = new System.Windows.Forms.Button();
            this.panelWalletSetupParent = new System.Windows.Forms.Panel();
            this.buttonWalletSetupPrevStep = new System.Windows.Forms.Button();
            this.buttonWalletSetupNextStep = new System.Windows.Forms.Button();
            this.labelWalletSetupDescription = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxLogo
            // 
            this.pictureBoxLogo.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxLogo.BackgroundImage = global::SeguraChain_Desktop_Wallet.Properties.Resources.logo_web_profil;
            this.pictureBoxLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBoxLogo.Location = new System.Drawing.Point(472, 9);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(80, 80);
            this.pictureBoxLogo.TabIndex = 2;
            this.pictureBoxLogo.TabStop = false;
            // 
            // buttonWalletSetupSave
            // 
            this.buttonWalletSetupSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonWalletSetupSave.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonWalletSetupSave.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.buttonWalletSetupSave.Location = new System.Drawing.Point(415, 504);
            this.buttonWalletSetupSave.Name = "buttonWalletSetupSave";
            this.buttonWalletSetupSave.Size = new System.Drawing.Size(232, 33);
            this.buttonWalletSetupSave.TabIndex = 6;
            this.buttonWalletSetupSave.Text = "BUTTON_WALLET_SETUP_SAVE_TEXT";
            this.buttonWalletSetupSave.UseVisualStyleBackColor = false;
            this.buttonWalletSetupSave.Click += new System.EventHandler(this.buttonWalletSetupSave_Click);
            // 
            // panelWalletSetupParent
            // 
            this.panelWalletSetupParent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(83)))), ((int)(((byte)(105)))));
            this.panelWalletSetupParent.Location = new System.Drawing.Point(12, 112);
            this.panelWalletSetupParent.Name = "panelWalletSetupParent";
            this.panelWalletSetupParent.Size = new System.Drawing.Size(1014, 386);
            this.panelWalletSetupParent.TabIndex = 7;
            // 
            // buttonWalletSetupPrevStep
            // 
            this.buttonWalletSetupPrevStep.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonWalletSetupPrevStep.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonWalletSetupPrevStep.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.buttonWalletSetupPrevStep.Location = new System.Drawing.Point(12, 504);
            this.buttonWalletSetupPrevStep.Name = "buttonWalletSetupPrevStep";
            this.buttonWalletSetupPrevStep.Size = new System.Drawing.Size(241, 33);
            this.buttonWalletSetupPrevStep.TabIndex = 8;
            this.buttonWalletSetupPrevStep.Text = "BUTTON_WALLET_SETUP_PREV_TEXT";
            this.buttonWalletSetupPrevStep.UseVisualStyleBackColor = false;
            this.buttonWalletSetupPrevStep.Visible = false;
            this.buttonWalletSetupPrevStep.Click += new System.EventHandler(this.buttonWalletSetupPrevStep_Click);
            // 
            // buttonWalletSetupNextStep
            // 
            this.buttonWalletSetupNextStep.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonWalletSetupNextStep.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonWalletSetupNextStep.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.buttonWalletSetupNextStep.Location = new System.Drawing.Point(786, 504);
            this.buttonWalletSetupNextStep.Name = "buttonWalletSetupNextStep";
            this.buttonWalletSetupNextStep.Size = new System.Drawing.Size(241, 33);
            this.buttonWalletSetupNextStep.TabIndex = 9;
            this.buttonWalletSetupNextStep.Text = "BUTTON_WALLET_SETUP_NEXT_TEXT\r\n";
            this.buttonWalletSetupNextStep.UseVisualStyleBackColor = false;
            this.buttonWalletSetupNextStep.Click += new System.EventHandler(this.buttonWalletSetupNextStep_Click);
            // 
            // labelWalletSetupDescription
            // 
            this.labelWalletSetupDescription.AutoSize = true;
            this.labelWalletSetupDescription.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelWalletSetupDescription.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.labelWalletSetupDescription.Location = new System.Drawing.Point(388, 92);
            this.labelWalletSetupDescription.Name = "labelWalletSetupDescription";
            this.labelWalletSetupDescription.Size = new System.Drawing.Size(272, 17);
            this.labelWalletSetupDescription.TabIndex = 10;
            this.labelWalletSetupDescription.Text = "LABEL_WALLET_SETUP_DESCRIPTION_TEXT";
            // 
            // ClassWalletSetupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(49)))), ((int)(((byte)(55)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(1039, 549);
            this.Controls.Add(this.labelWalletSetupDescription);
            this.Controls.Add(this.buttonWalletSetupNextStep);
            this.Controls.Add(this.buttonWalletSetupPrevStep);
            this.Controls.Add(this.panelWalletSetupParent);
            this.Controls.Add(this.buttonWalletSetupSave);
            this.Controls.Add(this.pictureBoxLogo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ClassWalletSetupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FORM_WALLET_SETUP_TITLE_TEXT";
            this.Load += new System.EventHandler(this.ClassWalletSetupForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Button buttonWalletSetupSave;
        private System.Windows.Forms.Panel panelWalletSetupParent;
        private System.Windows.Forms.Button buttonWalletSetupPrevStep;
        private System.Windows.Forms.Button buttonWalletSetupNextStep;
        private System.Windows.Forms.Label labelWalletSetupDescription;
    }
}