
namespace SeguraChain_Desktop_Wallet.InternalForm.Import
{
    partial class ClassImportWalletPrivateKeyInternalForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClassImportWalletPrivateKeyInternalForm));
            this.textBoxImportWalletPrivateKey = new System.Windows.Forms.TextBox();
            this.buttonImportWalletPrivateKey = new System.Windows.Forms.Button();
            this.labelImportWalletPrivateKey = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBoxImportWalletPrivateKey
            // 
            this.textBoxImportWalletPrivateKey.Location = new System.Drawing.Point(34, 48);
            this.textBoxImportWalletPrivateKey.Name = "textBoxImportWalletPrivateKey";
            this.textBoxImportWalletPrivateKey.Size = new System.Drawing.Size(622, 23);
            this.textBoxImportWalletPrivateKey.TabIndex = 0;
            // 
            // buttonImportWalletPrivateKey
            // 
            this.buttonImportWalletPrivateKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonImportWalletPrivateKey.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonImportWalletPrivateKey.Location = new System.Drawing.Point(132, 89);
            this.buttonImportWalletPrivateKey.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonImportWalletPrivateKey.Name = "buttonImportWalletPrivateKey";
            this.buttonImportWalletPrivateKey.Size = new System.Drawing.Size(424, 28);
            this.buttonImportWalletPrivateKey.TabIndex = 4;
            this.buttonImportWalletPrivateKey.Text = "BUTTON_IMPORT_WALLET_PRIVATE_KEY_TEXT";
            this.buttonImportWalletPrivateKey.UseVisualStyleBackColor = false;
            this.buttonImportWalletPrivateKey.Click += new System.EventHandler(this.buttonImportWalletPrivateKey_Click);
            // 
            // labelImportWalletPrivateKey
            // 
            this.labelImportWalletPrivateKey.AutoSize = true;
            this.labelImportWalletPrivateKey.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelImportWalletPrivateKey.ForeColor = System.Drawing.Color.Ivory;
            this.labelImportWalletPrivateKey.Location = new System.Drawing.Point(34, 9);
            this.labelImportWalletPrivateKey.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelImportWalletPrivateKey.Name = "labelImportWalletPrivateKey";
            this.labelImportWalletPrivateKey.Size = new System.Drawing.Size(275, 15);
            this.labelImportWalletPrivateKey.TabIndex = 5;
            this.labelImportWalletPrivateKey.Text = "LABEL_IMPORT_WALLET_PRIVATE_KEY_TEXT";
            this.labelImportWalletPrivateKey.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ClassImportWalletPrivateKeyInternalForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this.ClientSize = new System.Drawing.Size(691, 129);
            this.Controls.Add(this.labelImportWalletPrivateKey);
            this.Controls.Add(this.buttonImportWalletPrivateKey);
            this.Controls.Add(this.textBoxImportWalletPrivateKey);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ClassImportWalletPrivateKeyInternalForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FORM_IMPORT_WALLET_PRIVATE_KEY_TITLE_TEXT";
            this.Load += new System.EventHandler(this.ClassImportWalletPrivateKeyInternalForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxImportWalletPrivateKey;
        private System.Windows.Forms.Button buttonImportWalletPrivateKey;
        private System.Windows.Forms.Label labelImportWalletPrivateKey;
    }
}