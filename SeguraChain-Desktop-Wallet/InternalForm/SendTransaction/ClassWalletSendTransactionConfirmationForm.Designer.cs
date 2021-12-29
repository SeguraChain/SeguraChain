namespace SeguraChain_Desktop_Wallet.InternalForm.SendTransaction
{
    partial class ClassWalletSendTransactionConfirmationForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClassWalletSendTransactionConfirmationForm));
            this.labelSendTransactionAmountTotalToSend = new System.Windows.Forms.Label();
            this.buttonSendTransactionConfirmationAccept = new System.Windows.Forms.Button();
            this.buttonSendTransactionConfirmationCancel = new System.Windows.Forms.Button();
            this.timerSendTransactionConfirmationAutoCancel = new System.Windows.Forms.Timer(this.components);
            this.labelSendTransactionConfirmationWalletAddressTarget = new System.Windows.Forms.Label();
            this.labelSendTransactionConfirmationFeeToPay = new System.Windows.Forms.Label();
            this.labelSendTransactionConfirmationTotalToSpend = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelSendTransactionAmountTotalToSend
            // 
            this.labelSendTransactionAmountTotalToSend.AutoSize = true;
            this.labelSendTransactionAmountTotalToSend.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelSendTransactionAmountTotalToSend.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionAmountTotalToSend.Location = new System.Drawing.Point(4, 10);
            this.labelSendTransactionAmountTotalToSend.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionAmountTotalToSend.Name = "labelSendTransactionAmountTotalToSend";
            this.labelSendTransactionAmountTotalToSend.Size = new System.Drawing.Size(433, 15);
            this.labelSendTransactionAmountTotalToSend.TabIndex = 0;
            this.labelSendTransactionAmountTotalToSend.Text = "LABEL_SEND_TRANSACTION_CONFIRMATION_AMOUNT_TO_SEND_TEXT";
            // 
            // buttonSendTransactionConfirmationAccept
            // 
            this.buttonSendTransactionConfirmationAccept.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonSendTransactionConfirmationAccept.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSendTransactionConfirmationAccept.Location = new System.Drawing.Point(257, 130);
            this.buttonSendTransactionConfirmationAccept.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonSendTransactionConfirmationAccept.Name = "buttonSendTransactionConfirmationAccept";
            this.buttonSendTransactionConfirmationAccept.Size = new System.Drawing.Size(424, 40);
            this.buttonSendTransactionConfirmationAccept.TabIndex = 3;
            this.buttonSendTransactionConfirmationAccept.Text = "BUTTON_SEND_TRANSACTION_CONFIRMATION_ACCEPT_TEXT";
            this.buttonSendTransactionConfirmationAccept.UseVisualStyleBackColor = false;
            this.buttonSendTransactionConfirmationAccept.Click += new System.EventHandler(this.buttonSendTransactionConfirmationAccept_Click);
            // 
            // buttonSendTransactionConfirmationCancel
            // 
            this.buttonSendTransactionConfirmationCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonSendTransactionConfirmationCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSendTransactionConfirmationCancel.Location = new System.Drawing.Point(257, 178);
            this.buttonSendTransactionConfirmationCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonSendTransactionConfirmationCancel.Name = "buttonSendTransactionConfirmationCancel";
            this.buttonSendTransactionConfirmationCancel.Size = new System.Drawing.Size(424, 40);
            this.buttonSendTransactionConfirmationCancel.TabIndex = 4;
            this.buttonSendTransactionConfirmationCancel.Text = "BUTTON_SEND_TRANSACTION_CONFIRMATION_CANCEL_TEXT";
            this.buttonSendTransactionConfirmationCancel.UseVisualStyleBackColor = false;
            this.buttonSendTransactionConfirmationCancel.Click += new System.EventHandler(this.buttonSendTransactionConfirmationCancel_Click);
            // 
            // timerSendTransactionConfirmationAutoCancel
            // 
            this.timerSendTransactionConfirmationAutoCancel.Enabled = true;
            this.timerSendTransactionConfirmationAutoCancel.Interval = 1000;
            this.timerSendTransactionConfirmationAutoCancel.Tick += new System.EventHandler(this.timerSendTransactionConfirmationAutoCancel_Tick);
            // 
            // labelSendTransactionConfirmationWalletAddressTarget
            // 
            this.labelSendTransactionConfirmationWalletAddressTarget.AutoSize = true;
            this.labelSendTransactionConfirmationWalletAddressTarget.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelSendTransactionConfirmationWalletAddressTarget.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionConfirmationWalletAddressTarget.Location = new System.Drawing.Point(4, 42);
            this.labelSendTransactionConfirmationWalletAddressTarget.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionConfirmationWalletAddressTarget.Name = "labelSendTransactionConfirmationWalletAddressTarget";
            this.labelSendTransactionConfirmationWalletAddressTarget.Size = new System.Drawing.Size(483, 15);
            this.labelSendTransactionConfirmationWalletAddressTarget.TabIndex = 5;
            this.labelSendTransactionConfirmationWalletAddressTarget.Text = "LABEL_SEND_TRANSACTION_CONFIRMATION_WALLET_ADDRESS_TARGET_TEXT";
            // 
            // labelSendTransactionConfirmationFeeToPay
            // 
            this.labelSendTransactionConfirmationFeeToPay.AutoSize = true;
            this.labelSendTransactionConfirmationFeeToPay.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelSendTransactionConfirmationFeeToPay.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionConfirmationFeeToPay.Location = new System.Drawing.Point(4, 72);
            this.labelSendTransactionConfirmationFeeToPay.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionConfirmationFeeToPay.Name = "labelSendTransactionConfirmationFeeToPay";
            this.labelSendTransactionConfirmationFeeToPay.Size = new System.Drawing.Size(392, 15);
            this.labelSendTransactionConfirmationFeeToPay.TabIndex = 6;
            this.labelSendTransactionConfirmationFeeToPay.Text = "LABEL_SEND_TRANSACTION_CONFIRMATION_FEE_TO_PAY_TEXT";
            // 
            // labelSendTransactionConfirmationTotalToSpend
            // 
            this.labelSendTransactionConfirmationTotalToSpend.AutoSize = true;
            this.labelSendTransactionConfirmationTotalToSpend.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelSendTransactionConfirmationTotalToSpend.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionConfirmationTotalToSpend.Location = new System.Drawing.Point(4, 102);
            this.labelSendTransactionConfirmationTotalToSpend.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionConfirmationTotalToSpend.Name = "labelSendTransactionConfirmationTotalToSpend";
            this.labelSendTransactionConfirmationTotalToSpend.Size = new System.Drawing.Size(426, 15);
            this.labelSendTransactionConfirmationTotalToSpend.TabIndex = 7;
            this.labelSendTransactionConfirmationTotalToSpend.Text = "LABEL_SEND_TRANSACTION_CONFIRMATION_TOTAL_TO_SPEND_TEXT";
            // 
            // ClassWalletSendTransactionConfirmationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this.ClientSize = new System.Drawing.Size(922, 224);
            this.Controls.Add(this.labelSendTransactionConfirmationTotalToSpend);
            this.Controls.Add(this.labelSendTransactionConfirmationFeeToPay);
            this.Controls.Add(this.labelSendTransactionConfirmationWalletAddressTarget);
            this.Controls.Add(this.buttonSendTransactionConfirmationCancel);
            this.Controls.Add(this.buttonSendTransactionConfirmationAccept);
            this.Controls.Add(this.labelSendTransactionAmountTotalToSend);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.Name = "ClassWalletSendTransactionConfirmationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FORM_SEND_TRANSACTION_CONFIRMATION_TITLE_TEXT";
            this.Load += new System.EventHandler(this.ClassWalletSendTransactionConfirmationForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelSendTransactionAmountTotalToSend;
        private System.Windows.Forms.Button buttonSendTransactionConfirmationAccept;
        private System.Windows.Forms.Button buttonSendTransactionConfirmationCancel;
        private System.Windows.Forms.Timer timerSendTransactionConfirmationAutoCancel;
        private System.Windows.Forms.Label labelSendTransactionConfirmationWalletAddressTarget;
        private System.Windows.Forms.Label labelSendTransactionConfirmationFeeToPay;
        private System.Windows.Forms.Label labelSendTransactionConfirmationTotalToSpend;
    }
}