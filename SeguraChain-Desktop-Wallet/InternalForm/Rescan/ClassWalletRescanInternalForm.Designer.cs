namespace SeguraChain_Desktop_Wallet.InternalForm.Rescan
{
    partial class ClassWalletRescanInternalForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClassWalletRescanInternalForm));
            this.progressBarProgressRescan = new System.Windows.Forms.ProgressBar();
            this.labelWalletRescanPending = new System.Windows.Forms.Label();
            this.labelWalletRescanProgressText = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBarProgressRescan
            // 
            this.progressBarProgressRescan.Location = new System.Drawing.Point(14, 48);
            this.progressBarProgressRescan.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.progressBarProgressRescan.Name = "progressBarProgressRescan";
            this.progressBarProgressRescan.Size = new System.Drawing.Size(405, 27);
            this.progressBarProgressRescan.TabIndex = 0;
            // 
            // labelWalletRescanPending
            // 
            this.labelWalletRescanPending.AutoSize = true;
            this.labelWalletRescanPending.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelWalletRescanPending.Location = new System.Drawing.Point(63, 10);
            this.labelWalletRescanPending.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelWalletRescanPending.Name = "labelWalletRescanPending";
            this.labelWalletRescanPending.Size = new System.Drawing.Size(261, 13);
            this.labelWalletRescanPending.TabIndex = 1;
            this.labelWalletRescanPending.Text = "LABEL_WALLET_RESCAN_PENDING_TEXT";
            // 
            // labelWalletRescanProgressText
            // 
            this.labelWalletRescanProgressText.AutoSize = true;
            this.labelWalletRescanProgressText.Location = new System.Drawing.Point(83, 84);
            this.labelWalletRescanProgressText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelWalletRescanProgressText.Name = "labelWalletRescanProgressText";
            this.labelWalletRescanProgressText.Size = new System.Drawing.Size(228, 15);
            this.labelWalletRescanProgressText.TabIndex = 2;
            this.labelWalletRescanProgressText.Text = "LABEL_WALLET_RESCAN_PROGRESS_TEXT";
            // 
            // ClassWalletRescanInternalForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(433, 110);
            this.Controls.Add(this.labelWalletRescanProgressText);
            this.Controls.Add(this.labelWalletRescanPending);
            this.Controls.Add(this.progressBarProgressRescan);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ClassWalletRescanInternalForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FORM_TITLE_WALLET_RESCAN_FORM_TEXT";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ClassWalletRescanInternalForm_FormClosed);
            this.Load += new System.EventHandler(this.ClassWalletRescanInternalForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBarProgressRescan;
        private System.Windows.Forms.Label labelWalletRescanPending;
        private System.Windows.Forms.Label labelWalletRescanProgressText;
    }
}