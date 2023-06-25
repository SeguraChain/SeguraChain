namespace SeguraChain_Desktop_Wallet.InternalForm.SendTransaction
{
    partial class ClassWalletSendTransactionWaitRequestForm
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
            this.labelSendTransactionWaitRequestText = new System.Windows.Forms.Label();
            this.buttonExit = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelSendTransactionWaitRequestText
            // 
            this.labelSendTransactionWaitRequestText.AutoSize = true;
            this.labelSendTransactionWaitRequestText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelSendTransactionWaitRequestText.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionWaitRequestText.Location = new System.Drawing.Point(14, 54);
            this.labelSendTransactionWaitRequestText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionWaitRequestText.Name = "labelSendTransactionWaitRequestText";
            this.labelSendTransactionWaitRequestText.Size = new System.Drawing.Size(325, 13);
            this.labelSendTransactionWaitRequestText.TabIndex = 0;
            this.labelSendTransactionWaitRequestText.Text = "LABEL_SEND_TRANSACTION_WAIT_REQUEST_TEXT";
            // 
            // buttonExit
            // 
            this.buttonExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonExit.Location = new System.Drawing.Point(148, 76);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(336, 33);
            this.buttonExit.TabIndex = 1;
            this.buttonExit.Text = "BUTTON_SEND_TRANSACTION_WAIT_REQUEST_EXIT_TEXT";
            this.buttonExit.UseVisualStyleBackColor = false;
            this.buttonExit.Visible = false;
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // ClassWalletSendTransactionWaitRequestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this.ClientSize = new System.Drawing.Size(600, 117);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.labelSendTransactionWaitRequestText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "ClassWalletSendTransactionWaitRequestForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ClassWalletSendTransactionWaitRequestForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ClassWalletSendTransactionWaitRequestForm_FormClosing);
            this.Load += new System.EventHandler(this.ClassWalletSendTransactionWaitRequestForm_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ClassWalletSendTransactionWaitRequestForm_Paint);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelSendTransactionWaitRequestText;
        private System.Windows.Forms.Button buttonExit;
    }
}