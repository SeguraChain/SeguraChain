
namespace SeguraChain_Desktop_Wallet.InternalForm.TransactionHistory
{
    partial class ClassWalletTransactionHistoryInformationsLoadingForm
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
            this.labelLoadingBlockTransactionInformationsText = new System.Windows.Forms.Label();
            this.timerLoadingBlockTransactionInformations = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // labelLoadingBlockTransactionInformationsText
            // 
            this.labelLoadingBlockTransactionInformationsText.AutoSize = true;
            this.labelLoadingBlockTransactionInformationsText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelLoadingBlockTransactionInformationsText.ForeColor = System.Drawing.Color.Ivory;
            this.labelLoadingBlockTransactionInformationsText.Location = new System.Drawing.Point(28, 51);
            this.labelLoadingBlockTransactionInformationsText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelLoadingBlockTransactionInformationsText.Name = "labelLoadingBlockTransactionInformationsText";
            this.labelLoadingBlockTransactionInformationsText.Size = new System.Drawing.Size(389, 13);
            this.labelLoadingBlockTransactionInformationsText.TabIndex = 1;
            this.labelLoadingBlockTransactionInformationsText.Text = "LABEL_LOADING_BLOCK_TRANSACTION_INFORMATIONS_TEXT";
            // 
            // timerLoadingBlockTransactionInformations
            // 
            this.timerLoadingBlockTransactionInformations.Enabled = true;
            this.timerLoadingBlockTransactionInformations.Tick += new System.EventHandler(this.timerLoadingBlockTransactionInformations_Tick);
            // 
            // ClassWalletTransactionHistoryInformationsLoadingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this.ClientSize = new System.Drawing.Size(600, 117);
            this.ControlBox = false;
            this.Controls.Add(this.labelLoadingBlockTransactionInformationsText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ClassWalletTransactionHistoryInformationsLoadingForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FORM_TITLE_TRANSACTION_HISTORY_INFORMATION_LOADING_TEXT";
            this.Load += new System.EventHandler(this.ClassWalletTransactionHistoryInformationsLoadingForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelLoadingBlockTransactionInformationsText;
        private System.Windows.Forms.Timer timerLoadingBlockTransactionInformations;
    }
}