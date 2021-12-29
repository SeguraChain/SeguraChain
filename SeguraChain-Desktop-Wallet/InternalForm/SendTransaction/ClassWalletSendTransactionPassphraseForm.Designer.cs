namespace SeguraChain_Desktop_Wallet.InternalForm.SendTransaction
{
    partial class ClassWalletSendTransactionPassphraseForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClassWalletSendTransactionPassphraseForm));
            this.textBoxSendTransactionPassphrase = new System.Windows.Forms.TextBox();
            this.checkBoxSendTransactionShowHidePassphrase = new System.Windows.Forms.CheckBox();
            this.buttonSendTransactionUnlockWallet = new System.Windows.Forms.Button();
            this.labelSendTransactionInputPassphrase = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBoxSendTransactionPassphrase
            // 
            this.textBoxSendTransactionPassphrase.Location = new System.Drawing.Point(35, 47);
            this.textBoxSendTransactionPassphrase.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxSendTransactionPassphrase.Name = "textBoxSendTransactionPassphrase";
            this.textBoxSendTransactionPassphrase.PasswordChar = '*';
            this.textBoxSendTransactionPassphrase.Size = new System.Drawing.Size(460, 23);
            this.textBoxSendTransactionPassphrase.TabIndex = 0;
            // 
            // checkBoxSendTransactionShowHidePassphrase
            // 
            this.checkBoxSendTransactionShowHidePassphrase.AutoSize = true;
            this.checkBoxSendTransactionShowHidePassphrase.ForeColor = System.Drawing.Color.Ivory;
            this.checkBoxSendTransactionShowHidePassphrase.Location = new System.Drawing.Point(35, 77);
            this.checkBoxSendTransactionShowHidePassphrase.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.checkBoxSendTransactionShowHidePassphrase.Name = "checkBoxSendTransactionShowHidePassphrase";
            this.checkBoxSendTransactionShowHidePassphrase.Size = new System.Drawing.Size(380, 19);
            this.checkBoxSendTransactionShowHidePassphrase.TabIndex = 1;
            this.checkBoxSendTransactionShowHidePassphrase.Text = "CHECKBOX_SEND_TRANSACTION_SHOW_HIDE_PASSPHRASE_TEXT";
            this.checkBoxSendTransactionShowHidePassphrase.UseVisualStyleBackColor = true;
            this.checkBoxSendTransactionShowHidePassphrase.CheckedChanged += new System.EventHandler(this.checkBoxSendTransactionShowHidePassphrase_CheckedChanged);
            // 
            // buttonSendTransactionUnlockWallet
            // 
            this.buttonSendTransactionUnlockWallet.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonSendTransactionUnlockWallet.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSendTransactionUnlockWallet.Location = new System.Drawing.Point(49, 104);
            this.buttonSendTransactionUnlockWallet.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonSendTransactionUnlockWallet.Name = "buttonSendTransactionUnlockWallet";
            this.buttonSendTransactionUnlockWallet.Size = new System.Drawing.Size(425, 37);
            this.buttonSendTransactionUnlockWallet.TabIndex = 2;
            this.buttonSendTransactionUnlockWallet.Text = "BUTTON_SEND_TRANSACTION_UNLOCK_WALLET_TEXT";
            this.buttonSendTransactionUnlockWallet.UseVisualStyleBackColor = false;
            this.buttonSendTransactionUnlockWallet.Click += new System.EventHandler(this.buttonSendTransactionUnlockWallet_Click);
            // 
            // labelSendTransactionInputPassphrase
            // 
            this.labelSendTransactionInputPassphrase.AutoSize = true;
            this.labelSendTransactionInputPassphrase.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionInputPassphrase.Location = new System.Drawing.Point(35, 16);
            this.labelSendTransactionInputPassphrase.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionInputPassphrase.Name = "labelSendTransactionInputPassphrase";
            this.labelSendTransactionInputPassphrase.Size = new System.Drawing.Size(300, 15);
            this.labelSendTransactionInputPassphrase.TabIndex = 3;
            this.labelSendTransactionInputPassphrase.Text = "LABEL_SEND_TRANSACTION_INPUT_PASSPHRASE_TEXT";
            // 
            // ClassWalletSendTransactionPassphraseForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this.ClientSize = new System.Drawing.Size(537, 151);
            this.Controls.Add(this.labelSendTransactionInputPassphrase);
            this.Controls.Add(this.buttonSendTransactionUnlockWallet);
            this.Controls.Add(this.checkBoxSendTransactionShowHidePassphrase);
            this.Controls.Add(this.textBoxSendTransactionPassphrase);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.Name = "ClassWalletSendTransactionPassphraseForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FORM_SEND_TRANSACTION_PASSPHRASE_TITLE_TEXT";
            this.Load += new System.EventHandler(this.ClassSendTransactionPassphraseForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxSendTransactionPassphrase;
        private System.Windows.Forms.CheckBox checkBoxSendTransactionShowHidePassphrase;
        private System.Windows.Forms.Button buttonSendTransactionUnlockWallet;
        private System.Windows.Forms.Label labelSendTransactionInputPassphrase;
    }
}