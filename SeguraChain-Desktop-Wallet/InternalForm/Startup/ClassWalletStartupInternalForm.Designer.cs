namespace SeguraChain_Desktop_Wallet.InternalForm.Startup
{
    partial class ClassWalletStartupInternalForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClassWalletStartupInternalForm));
            labelStartupDesktopWalletLoadingText = new System.Windows.Forms.Label();
            pictureBoxDesktopWalletLogo = new System.Windows.Forms.PictureBox();
            timerOpenMainInterface = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)pictureBoxDesktopWalletLogo).BeginInit();
            SuspendLayout();
            // 
            // labelStartupDesktopWalletLoadingText
            // 
            labelStartupDesktopWalletLoadingText.AutoSize = true;
            labelStartupDesktopWalletLoadingText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelStartupDesktopWalletLoadingText.ForeColor = System.Drawing.Color.GhostWhite;
            labelStartupDesktopWalletLoadingText.Location = new System.Drawing.Point(196, 342);
            labelStartupDesktopWalletLoadingText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelStartupDesktopWalletLoadingText.Name = "labelStartupDesktopWalletLoadingText";
            labelStartupDesktopWalletLoadingText.Size = new System.Drawing.Size(401, 16);
            labelStartupDesktopWalletLoadingText.TabIndex = 1;
            labelStartupDesktopWalletLoadingText.Text = "LABEL_STARTUP_DESKTOP_WALLET_LOADING_TEXT";
            labelStartupDesktopWalletLoadingText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pictureBoxDesktopWalletLogo
            // 
            pictureBoxDesktopWalletLogo.BackgroundImage = (System.Drawing.Image)resources.GetObject("pictureBoxDesktopWalletLogo.BackgroundImage");
            pictureBoxDesktopWalletLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            pictureBoxDesktopWalletLogo.Location = new System.Drawing.Point(239, 12);
            pictureBoxDesktopWalletLogo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            pictureBoxDesktopWalletLogo.Name = "pictureBoxDesktopWalletLogo";
            pictureBoxDesktopWalletLogo.Size = new System.Drawing.Size(314, 325);
            pictureBoxDesktopWalletLogo.TabIndex = 0;
            pictureBoxDesktopWalletLogo.TabStop = false;
            // 
            // timerOpenMainInterface
            // 
            timerOpenMainInterface.Enabled = true;
            timerOpenMainInterface.Interval = 1000;
            timerOpenMainInterface.Tick += timerOpenMainInterface_Tick;
            // 
            // ClassWalletStartupInternalForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(49, 55, 64);
            ClientSize = new System.Drawing.Size(793, 438);
            ControlBox = false;
            Controls.Add(labelStartupDesktopWalletLoadingText);
            Controls.Add(pictureBoxDesktopWalletLogo);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ClassWalletStartupInternalForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "FORM_TITLE_LOADING";
            Load += ClassWalletStartupInternalForm_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBoxDesktopWalletLogo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxDesktopWalletLogo;
        private System.Windows.Forms.Label labelStartupDesktopWalletLoadingText;
        private System.Windows.Forms.Timer timerOpenMainInterface;
    }
}