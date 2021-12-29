namespace SeguraChain_Desktop_Wallet.InternalForm.TransactionHistory
{
    partial class ClassWalletTransactionHistoryInformationInternalForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClassWalletTransactionHistoryInformationInternalForm));
            this.buttonTransactionHistoryInformationClose = new System.Windows.Forms.Button();
            this.richTextBoxTransactionInformations = new System.Windows.Forms.RichTextBox();
            this.richTextBoxTransactionInformationsNotes = new System.Windows.Forms.RichTextBox();
            this.buttonTransactionHistoryInformationCopy = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonTransactionHistoryInformationClose
            // 
            this.buttonTransactionHistoryInformationClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonTransactionHistoryInformationClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonTransactionHistoryInformationClose.Location = new System.Drawing.Point(412, 355);
            this.buttonTransactionHistoryInformationClose.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonTransactionHistoryInformationClose.Name = "buttonTransactionHistoryInformationClose";
            this.buttonTransactionHistoryInformationClose.Size = new System.Drawing.Size(331, 40);
            this.buttonTransactionHistoryInformationClose.TabIndex = 2;
            this.buttonTransactionHistoryInformationClose.Text = "BUTTON_TRANSACTION_HISTORY_CLOSE_TEXT";
            this.buttonTransactionHistoryInformationClose.UseVisualStyleBackColor = false;
            this.buttonTransactionHistoryInformationClose.Click += new System.EventHandler(this.buttonTransactionHistoryInformationClose_Click);
            // 
            // richTextBoxTransactionInformations
            // 
            this.richTextBoxTransactionInformations.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.richTextBoxTransactionInformations.Location = new System.Drawing.Point(15, 24);
            this.richTextBoxTransactionInformations.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.richTextBoxTransactionInformations.Name = "richTextBoxTransactionInformations";
            this.richTextBoxTransactionInformations.ReadOnly = true;
            this.richTextBoxTransactionInformations.Size = new System.Drawing.Size(1095, 231);
            this.richTextBoxTransactionInformations.TabIndex = 3;
            this.richTextBoxTransactionInformations.Text = "";
            // 
            // richTextBoxTransactionInformationsNotes
            // 
            this.richTextBoxTransactionInformationsNotes.Location = new System.Drawing.Point(15, 263);
            this.richTextBoxTransactionInformationsNotes.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.richTextBoxTransactionInformationsNotes.Name = "richTextBoxTransactionInformationsNotes";
            this.richTextBoxTransactionInformationsNotes.ReadOnly = true;
            this.richTextBoxTransactionInformationsNotes.Size = new System.Drawing.Size(1095, 80);
            this.richTextBoxTransactionInformationsNotes.TabIndex = 4;
            this.richTextBoxTransactionInformationsNotes.Text = "";
            // 
            // buttonTransactionHistoryInformationCopy
            // 
            this.buttonTransactionHistoryInformationCopy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonTransactionHistoryInformationCopy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonTransactionHistoryInformationCopy.Location = new System.Drawing.Point(14, 355);
            this.buttonTransactionHistoryInformationCopy.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonTransactionHistoryInformationCopy.Name = "buttonTransactionHistoryInformationCopy";
            this.buttonTransactionHistoryInformationCopy.Size = new System.Drawing.Size(331, 40);
            this.buttonTransactionHistoryInformationCopy.TabIndex = 5;
            this.buttonTransactionHistoryInformationCopy.Text = "BUTTON_TRANSACTION_HISTORY_COPY_TEXT";
            this.buttonTransactionHistoryInformationCopy.UseVisualStyleBackColor = false;
            this.buttonTransactionHistoryInformationCopy.Click += new System.EventHandler(this.buttonTransactionHistoryInformationCopy_Click);
            // 
            // ClassWalletTransactionHistoryInformationInternalForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this.ClientSize = new System.Drawing.Size(1125, 425);
            this.Controls.Add(this.buttonTransactionHistoryInformationCopy);
            this.Controls.Add(this.richTextBoxTransactionInformationsNotes);
            this.Controls.Add(this.richTextBoxTransactionInformations);
            this.Controls.Add(this.buttonTransactionHistoryInformationClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ClassWalletTransactionHistoryInformationInternalForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FORM_TITLE_TRANSACTION_HISTORY_TEXT";
            this.Load += new System.EventHandler(this.ClassWalletTransactionHistoryInformationInternalForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonTransactionHistoryInformationClose;
        private System.Windows.Forms.RichTextBox richTextBoxTransactionInformations;
        private System.Windows.Forms.RichTextBox richTextBoxTransactionInformationsNotes;
        private System.Windows.Forms.Button buttonTransactionHistoryInformationCopy;
    }
}