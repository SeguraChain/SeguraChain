using SeguraChain_Desktop_Wallet.InternalForm.Custom.Object;

namespace SeguraChain_Desktop_Wallet
{
    partial class ClassWalletMainInterfaceForm
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClassWalletMainInterfaceForm));
            this.menuStripGeneralWallet = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openWalletToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeWalletToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.createWalletToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importWalletPrivateKeytoolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rescanToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.languageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.comboBoxListWalletFile = new System.Windows.Forms.ComboBox();
            this.labelWalletOpened = new System.Windows.Forms.Label();
            this.labelMainInterfaceSyncProgress = new System.Windows.Forms.Label();
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.timerRefreshTransactionHistory = new System.Windows.Forms.Timer(this.components);
            this.tabControlWallet = new System.Windows.Forms.TabControl();
            this.tabPageOverview = new System.Windows.Forms.TabPage();
            this.panelInternalNetworkStats = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomPanel();
            this.labelMainInterfaceNetworkStatsTotalCoinPendingText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsTotalCoinSpreadText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsTotalTransactionText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText = new System.Windows.Forms.Label();
            this.panelSyncInformationsSeperator = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomPanel();
            this.labelMainInterfaceNetworkStatsInfoSyncText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsCurrentHashrateText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsCurrentDifficultyText = new System.Windows.Forms.Label();
            this.labelMainInterfaceNetworkStatsTitleText = new System.Windows.Forms.Label();
            this.panelRecentTransactions = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomPanel();
            this.panelInternalRecentTransactions = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomPanel();
            this.labelMainInterfaceRecentTransaction = new System.Windows.Forms.Label();
            this.labelMainInterfaceTotalBalanceAmountText = new System.Windows.Forms.Label();
            this.labelMainInterfacePendingBalanceAmountText = new System.Windows.Forms.Label();
            this.labelMainInterfaceAvailableBalanceAmountText = new System.Windows.Forms.Label();
            this.panelSeperatorBalanceLine = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomPanel();
            this.labelMainInterfaceCurrentBalanceText = new System.Windows.Forms.Label();
            this.tabPageSendTransaction = new System.Windows.Forms.TabPage();
            this.buttonSendTransactionDoProcess = new System.Windows.Forms.Button();
            this.panelSendTransaction = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomPanel();
            this.panelSendTransactionDetails = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomPanel();
            this.labelSendTransactionAmountToSpend = new System.Windows.Forms.Label();
            this.labelSendTransactionFeeSizeCost = new System.Windows.Forms.Label();
            this.textBoxSendTransactionFeeCalculated = new System.Windows.Forms.TextBox();
            this.textBoxSendTransactionFeeSizeCost = new System.Windows.Forms.TextBox();
            this.labelSendTransactionFeeCalculated = new System.Windows.Forms.Label();
            this.labelSendTransactionFeeConfirmationCost = new System.Windows.Forms.Label();
            this.textBoxSendTransactionAmountToSpend = new System.Windows.Forms.TextBox();
            this.textBoxSendTransactionFeeConfirmationCost = new System.Windows.Forms.TextBox();
            this.textBoxSendTransactionTotalAmountSource = new System.Windows.Forms.TextBox();
            this.labelSendTransactionTotalAmountSource = new System.Windows.Forms.Label();
            this.textBoxSendTransactionConfirmationsCountTarget = new System.Windows.Forms.TextBox();
            this.labelSendTransactionConfirmationTimeEstimated = new System.Windows.Forms.Label();
            this.labelSendTransactionPaymentId = new System.Windows.Forms.Label();
            this.textBoxSendTransactionPaymentId = new System.Windows.Forms.TextBox();
            this.labelSendTransactionAvailableBalanceText = new System.Windows.Forms.Label();
            this.buttonSendTransactionOpenContactList = new System.Windows.Forms.Button();
            this.labelSendTransactionConfirmationCountTarget = new System.Windows.Forms.Label();
            this.labelSendTransactionAmountSelected = new System.Windows.Forms.Label();
            this.textBoxSendTransactionAmountSelected = new System.Windows.Forms.TextBox();
            this.labelSendTransactionWalletAddressTarget = new System.Windows.Forms.Label();
            this.textBoxSendTransactionWalletAddressTarget = new System.Windows.Forms.TextBox();
            this.tabPageReceiveTransaction = new System.Windows.Forms.TabPage();
            this.buttonSaveQrCodeReceiveTransactionWalletAddress = new System.Windows.Forms.Button();
            this.buttonPrintQrCodeReceiveTransactionWalletAddress = new System.Windows.Forms.Button();
            this.labelWalletReceiveTransactionQrCodeText = new System.Windows.Forms.Label();
            this.panelQrCodeWalletAddress = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomPanel();
            this.tabPageTransactionHistory = new System.Windows.Forms.TabPage();
            this.buttonMainInterfaceSearchTransactionHistory = new System.Windows.Forms.Button();
            this.textBoxTransactionHistorySearch = new System.Windows.Forms.TextBox();
            this.panelTransactionHistoryColumns = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomPanel();
            this.textBoxMainInterfaceMaxPageTransactionHistory = new System.Windows.Forms.TextBox();
            this.textBoxMainInterfaceCurrentPageTransactionHistory = new System.Windows.Forms.TextBox();
            this.buttonMainInterfaceNextPageTransactionHistory = new System.Windows.Forms.Button();
            this.buttonMainInterfaceBackPageTransactionHistory = new System.Windows.Forms.Button();
            this.buttonMainInterfaceExportTransactionHistory = new System.Windows.Forms.Button();
            this.panelTransactionHistory = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomPanel();
            this.tabPageStoreNetwork = new System.Windows.Forms.TabPage();
            this.labelWalletAddressReceiveTransactionTitle = new System.Windows.Forms.Label();
            this.labelWalletAddressReceiveTransaction = new System.Windows.Forms.Label();
            this.progressBarMainInterfaceSyncProgress = new SeguraChain_Desktop_Wallet.InternalForm.Custom.Object.ClassCustomProgressBar();
            this.menuStripGeneralWallet.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.tabControlWallet.SuspendLayout();
            this.tabPageOverview.SuspendLayout();
            this.panelInternalNetworkStats.SuspendLayout();
            this.panelRecentTransactions.SuspendLayout();
            this.tabPageSendTransaction.SuspendLayout();
            this.panelSendTransaction.SuspendLayout();
            this.panelSendTransactionDetails.SuspendLayout();
            this.tabPageReceiveTransaction.SuspendLayout();
            this.tabPageTransactionHistory.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStripGeneralWallet
            // 
            this.menuStripGeneralWallet.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(83)))), ((int)(((byte)(105)))));
            this.menuStripGeneralWallet.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.menuStripGeneralWallet.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.rescanToolStripMenuItem,
            this.languageToolStripMenuItem});
            this.menuStripGeneralWallet.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.menuStripGeneralWallet.Location = new System.Drawing.Point(0, 0);
            this.menuStripGeneralWallet.MinimumSize = new System.Drawing.Size(1376, 0);
            this.menuStripGeneralWallet.Name = "menuStripGeneralWallet";
            this.menuStripGeneralWallet.Padding = new System.Windows.Forms.Padding(7, 2, 7, 2);
            this.menuStripGeneralWallet.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.menuStripGeneralWallet.Size = new System.Drawing.Size(1376, 24);
            this.menuStripGeneralWallet.Stretch = false;
            this.menuStripGeneralWallet.TabIndex = 0;
            this.menuStripGeneralWallet.Text = "menuStripControl";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openWalletToolStripMenuItem,
            this.closeWalletToolStripMenuItem,
            this.createWalletToolStripMenuItem,
            this.importWalletPrivateKeytoolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.fileToolStripMenuItem.ForeColor = System.Drawing.Color.Ivory;
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(74, 20);
            this.fileToolStripMenuItem.Text = "FILE_TEXT";
            // 
            // openWalletToolStripMenuItem
            // 
            this.openWalletToolStripMenuItem.Name = "openWalletToolStripMenuItem";
            this.openWalletToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            this.openWalletToolStripMenuItem.Text = "OPEN_WALLET_TEXT";
            // 
            // closeWalletToolStripMenuItem
            // 
            this.closeWalletToolStripMenuItem.Name = "closeWalletToolStripMenuItem";
            this.closeWalletToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            this.closeWalletToolStripMenuItem.Text = "CLOSE_WALLET_TEXT";
            this.closeWalletToolStripMenuItem.Click += new System.EventHandler(this.closeWalletToolStripMenuItem_Click);
            // 
            // createWalletToolStripMenuItem
            // 
            this.createWalletToolStripMenuItem.Name = "createWalletToolStripMenuItem";
            this.createWalletToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            this.createWalletToolStripMenuItem.Text = "CREATE_WALLET_TEXT";
            this.createWalletToolStripMenuItem.Click += new System.EventHandler(this.createWalletToolStripMenuItem_Click);
            // 
            // importWalletPrivateKeytoolStripMenuItem
            // 
            this.importWalletPrivateKeytoolStripMenuItem.Name = "importWalletPrivateKeytoolStripMenuItem";
            this.importWalletPrivateKeytoolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            this.importWalletPrivateKeytoolStripMenuItem.Text = "IMPORT_PRIVATE_KEY_TEXT";
            this.importWalletPrivateKeytoolStripMenuItem.Click += new System.EventHandler(this.importWalletPrivateKeyToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            this.exitToolStripMenuItem.Text = "EXIT_TEXT";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.settingsToolStripMenuItem.ForeColor = System.Drawing.Color.Ivory;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(101, 20);
            this.settingsToolStripMenuItem.Text = "SETTING_TEXT";
            // 
            // rescanToolStripMenuItem
            // 
            this.rescanToolStripMenuItem.ForeColor = System.Drawing.Color.Ivory;
            this.rescanToolStripMenuItem.Name = "rescanToolStripMenuItem";
            this.rescanToolStripMenuItem.Size = new System.Drawing.Size(97, 20);
            this.rescanToolStripMenuItem.Text = "RESCAN_TEXT";
            this.rescanToolStripMenuItem.Click += new System.EventHandler(this.rescanToolStripMenuItem_Click);
            // 
            // languageToolStripMenuItem
            // 
            this.languageToolStripMenuItem.ForeColor = System.Drawing.Color.GhostWhite;
            this.languageToolStripMenuItem.Name = "languageToolStripMenuItem";
            this.languageToolStripMenuItem.Size = new System.Drawing.Size(116, 20);
            this.languageToolStripMenuItem.Text = "LANGUAGE_TEXT";
            // 
            // comboBoxListWalletFile
            // 
            this.comboBoxListWalletFile.BackColor = System.Drawing.Color.White;
            this.comboBoxListWalletFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxListWalletFile.FormattingEnabled = true;
            this.comboBoxListWalletFile.Location = new System.Drawing.Point(1234, 68);
            this.comboBoxListWalletFile.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.comboBoxListWalletFile.Name = "comboBoxListWalletFile";
            this.comboBoxListWalletFile.Size = new System.Drawing.Size(140, 23);
            this.comboBoxListWalletFile.TabIndex = 2;
            this.comboBoxListWalletFile.SelectedIndexChanged += new System.EventHandler(this.comboBoxListWalletFile_SelectedIndexChanged);
            // 
            // labelWalletOpened
            // 
            this.labelWalletOpened.AutoSize = true;
            this.labelWalletOpened.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelWalletOpened.ForeColor = System.Drawing.Color.Ivory;
            this.labelWalletOpened.Location = new System.Drawing.Point(1119, 50);
            this.labelWalletOpened.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelWalletOpened.Name = "labelWalletOpened";
            this.labelWalletOpened.Size = new System.Drawing.Size(254, 15);
            this.labelWalletOpened.TabIndex = 3;
            this.labelWalletOpened.Text = "LABEL_WALLET_OPENED_LIST_TEXT";
            // 
            // labelMainInterfaceSyncProgress
            // 
            this.labelMainInterfaceSyncProgress.AutoSize = true;
            this.labelMainInterfaceSyncProgress.BackColor = System.Drawing.Color.Transparent;
            this.labelMainInterfaceSyncProgress.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceSyncProgress.ForeColor = System.Drawing.Color.Ivory;
            this.labelMainInterfaceSyncProgress.Location = new System.Drawing.Point(516, 732);
            this.labelMainInterfaceSyncProgress.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceSyncProgress.Name = "labelMainInterfaceSyncProgress";
            this.labelMainInterfaceSyncProgress.Size = new System.Drawing.Size(302, 15);
            this.labelMainInterfaceSyncProgress.TabIndex = 6;
            this.labelMainInterfaceSyncProgress.Text = "LABEL_MAIN_INTERFACE_SYNC_PROGRESS";
            // 
            // pictureBoxLogo
            // 
            this.pictureBoxLogo.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxLogo.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pictureBoxLogo.BackgroundImage")));
            this.pictureBoxLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBoxLogo.Location = new System.Drawing.Point(648, 29);
            this.pictureBoxLogo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(82, 81);
            this.pictureBoxLogo.TabIndex = 4;
            this.pictureBoxLogo.TabStop = false;
            // 
            // timerRefreshTransactionHistory
            // 
            this.timerRefreshTransactionHistory.Enabled = true;
            this.timerRefreshTransactionHistory.Interval = 10;
            this.timerRefreshTransactionHistory.Tick += new System.EventHandler(this.timerRefreshTransactionHistory_Tick);
            // 
            // tabControlWallet
            // 
            this.tabControlWallet.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tabControlWallet.Controls.Add(this.tabPageOverview);
            this.tabControlWallet.Controls.Add(this.tabPageSendTransaction);
            this.tabControlWallet.Controls.Add(this.tabPageReceiveTransaction);
            this.tabControlWallet.Controls.Add(this.tabPageTransactionHistory);
            this.tabControlWallet.Controls.Add(this.tabPageStoreNetwork);
            this.tabControlWallet.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.tabControlWallet.ItemSize = new System.Drawing.Size(128, 30);
            this.tabControlWallet.Location = new System.Drawing.Point(0, 145);
            this.tabControlWallet.Margin = new System.Windows.Forms.Padding(0);
            this.tabControlWallet.Name = "tabControlWallet";
            this.tabControlWallet.Padding = new System.Drawing.Point(0, 0);
            this.tabControlWallet.SelectedIndex = 0;
            this.tabControlWallet.Size = new System.Drawing.Size(1377, 584);
            this.tabControlWallet.TabIndex = 1;
            // 
            // tabPageOverview
            // 
            this.tabPageOverview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(77)))), ((int)(((byte)(104)))), ((int)(((byte)(145)))));
            this.tabPageOverview.Controls.Add(this.panelInternalNetworkStats);
            this.tabPageOverview.Controls.Add(this.panelRecentTransactions);
            this.tabPageOverview.Controls.Add(this.labelMainInterfaceTotalBalanceAmountText);
            this.tabPageOverview.Controls.Add(this.labelMainInterfacePendingBalanceAmountText);
            this.tabPageOverview.Controls.Add(this.labelMainInterfaceAvailableBalanceAmountText);
            this.tabPageOverview.Controls.Add(this.panelSeperatorBalanceLine);
            this.tabPageOverview.Controls.Add(this.labelMainInterfaceCurrentBalanceText);
            this.tabPageOverview.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.tabPageOverview.Location = new System.Drawing.Point(4, 34);
            this.tabPageOverview.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPageOverview.Name = "tabPageOverview";
            this.tabPageOverview.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPageOverview.Size = new System.Drawing.Size(1369, 546);
            this.tabPageOverview.TabIndex = 0;
            this.tabPageOverview.Text = "TABPAG_OVERVIEW_TEXT";
            this.tabPageOverview.Paint += new System.Windows.Forms.PaintEventHandler(this.tabPageOverview_Paint);
            // 
            // panelInternalNetworkStats
            // 
            this.panelInternalNetworkStats.BackColor = System.Drawing.Color.AliceBlue;
            this.panelInternalNetworkStats.BorderColor = System.Drawing.Color.DarkGray;
            this.panelInternalNetworkStats.BorderSize = 1F;
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsTotalCoinPendingText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsTotalCoinSpreadText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsTotalTransactionText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText);
            this.panelInternalNetworkStats.Controls.Add(this.panelSyncInformationsSeperator);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsInfoSyncText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsCurrentHashrateText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsCurrentDifficultyText);
            this.panelInternalNetworkStats.Controls.Add(this.labelMainInterfaceNetworkStatsTitleText);
            this.panelInternalNetworkStats.Location = new System.Drawing.Point(9, 128);
            this.panelInternalNetworkStats.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelInternalNetworkStats.Name = "panelInternalNetworkStats";
            this.panelInternalNetworkStats.Radius = 10;
            this.panelInternalNetworkStats.Size = new System.Drawing.Size(830, 402);
            this.panelInternalNetworkStats.TabIndex = 11;
            // 
            // labelMainInterfaceNetworkStatsTotalCoinPendingText
            // 
            this.labelMainInterfaceNetworkStatsTotalCoinPendingText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsTotalCoinPendingText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsTotalCoinPendingText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsTotalCoinPendingText.Location = new System.Drawing.Point(5, 317);
            this.labelMainInterfaceNetworkStatsTotalCoinPendingText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsTotalCoinPendingText.Name = "labelMainInterfaceNetworkStatsTotalCoinPendingText";
            this.labelMainInterfaceNetworkStatsTotalCoinPendingText.Size = new System.Drawing.Size(470, 16);
            this.labelMainInterfaceNetworkStatsTotalCoinPendingText.TabIndex = 25;
            this.labelMainInterfaceNetworkStatsTotalCoinPendingText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_PENDING_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalCoinSpreadText
            // 
            this.labelMainInterfaceNetworkStatsTotalCoinSpreadText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsTotalCoinSpreadText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsTotalCoinSpreadText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsTotalCoinSpreadText.Location = new System.Drawing.Point(5, 366);
            this.labelMainInterfaceNetworkStatsTotalCoinSpreadText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsTotalCoinSpreadText.Name = "labelMainInterfaceNetworkStatsTotalCoinSpreadText";
            this.labelMainInterfaceNetworkStatsTotalCoinSpreadText.Size = new System.Drawing.Size(466, 16);
            this.labelMainInterfaceNetworkStatsTotalCoinSpreadText.TabIndex = 24;
            this.labelMainInterfaceNetworkStatsTotalCoinSpreadText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_SPREAD_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalFeeCirculatingText
            // 
            this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Location = new System.Drawing.Point(5, 342);
            this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Name = "labelMainInterfaceNetworkStatsTotalFeeCirculatingText";
            this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Size = new System.Drawing.Size(490, 16);
            this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText.TabIndex = 23;
            this.labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_FEE_CIRCULATING_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalCoinCirculatingText
            // 
            this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Location = new System.Drawing.Point(5, 292);
            this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Name = "labelMainInterfaceNetworkStatsTotalCoinCirculatingText";
            this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Size = new System.Drawing.Size(495, 16);
            this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText.TabIndex = 22;
            this.labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_CIRCULATING_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText
            // 
            this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Location = new System.Drawing.Point(5, 269);
            this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Name = "labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText";
            this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Size = new System.Drawing.Size(566, 16);
            this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.TabIndex = 21;
            this.labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_BLOCK_UNLOCKED_CHECKED_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalTransactionConfirmedText
            // 
            this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Location = new System.Drawing.Point(5, 245);
            this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Name = "labelMainInterfaceNetworkStatsTotalTransactionConfirmedText";
            this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Size = new System.Drawing.Size(548, 16);
            this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.TabIndex = 20;
            this.labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_CONFIRMED_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalTransactionText
            // 
            this.labelMainInterfaceNetworkStatsTotalTransactionText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsTotalTransactionText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsTotalTransactionText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsTotalTransactionText.Location = new System.Drawing.Point(5, 220);
            this.labelMainInterfaceNetworkStatsTotalTransactionText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsTotalTransactionText.Name = "labelMainInterfaceNetworkStatsTotalTransactionText";
            this.labelMainInterfaceNetworkStatsTotalTransactionText.Size = new System.Drawing.Size(464, 16);
            this.labelMainInterfaceNetworkStatsTotalTransactionText.TabIndex = 19;
            this.labelMainInterfaceNetworkStatsTotalTransactionText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalTransactionMemPoolText
            // 
            this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Location = new System.Drawing.Point(5, 197);
            this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Name = "labelMainInterfaceNetworkStatsTotalTransactionMemPoolText";
            this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Size = new System.Drawing.Size(538, 16);
            this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.TabIndex = 18;
            this.labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_MEMPOOL_TEXT";
            // 
            // panelSyncInformationsSeperator
            // 
            this.panelSyncInformationsSeperator.BackColor = System.Drawing.Color.Black;
            this.panelSyncInformationsSeperator.BorderColor = System.Drawing.Color.White;
            this.panelSyncInformationsSeperator.BorderSize = 3F;
            this.panelSyncInformationsSeperator.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelSyncInformationsSeperator.Location = new System.Drawing.Point(114, 162);
            this.panelSyncInformationsSeperator.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelSyncInformationsSeperator.Name = "panelSyncInformationsSeperator";
            this.panelSyncInformationsSeperator.Radius = 2;
            this.panelSyncInformationsSeperator.Size = new System.Drawing.Size(583, 2);
            this.panelSyncInformationsSeperator.TabIndex = 17;
            // 
            // labelMainInterfaceNetworkStatsInfoSyncText
            // 
            this.labelMainInterfaceNetworkStatsInfoSyncText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsInfoSyncText.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsInfoSyncText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsInfoSyncText.Location = new System.Drawing.Point(124, 167);
            this.labelMainInterfaceNetworkStatsInfoSyncText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsInfoSyncText.Name = "labelMainInterfaceNetworkStatsInfoSyncText";
            this.labelMainInterfaceNetworkStatsInfoSyncText.Size = new System.Drawing.Size(476, 18);
            this.labelMainInterfaceNetworkStatsInfoSyncText.TabIndex = 16;
            this.labelMainInterfaceNetworkStatsInfoSyncText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_INFO_SYNC_TEXT";
            // 
            // labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText
            // 
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Location = new System.Drawing.Point(5, 130);
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Name = "labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText";
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Size = new System.Drawing.Size(550, 16);
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.TabIndex = 15;
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_MINING_LUCK_PERCENT_TEXT";
            // 
            // labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText
            // 
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Location = new System.Drawing.Point(5, 107);
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Name = "labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText";
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Size = new System.Drawing.Size(537, 16);
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.TabIndex = 14;
            this.labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_MINING_LUCK_STATUS_TEXT";
            // 
            // labelMainInterfaceNetworkStatsCurrentHashrateText
            // 
            this.labelMainInterfaceNetworkStatsCurrentHashrateText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsCurrentHashrateText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsCurrentHashrateText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsCurrentHashrateText.Location = new System.Drawing.Point(5, 84);
            this.labelMainInterfaceNetworkStatsCurrentHashrateText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsCurrentHashrateText.Name = "labelMainInterfaceNetworkStatsCurrentHashrateText";
            this.labelMainInterfaceNetworkStatsCurrentHashrateText.Size = new System.Drawing.Size(465, 16);
            this.labelMainInterfaceNetworkStatsCurrentHashrateText.TabIndex = 13;
            this.labelMainInterfaceNetworkStatsCurrentHashrateText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_HASHRATE_TEXT";
            // 
            // labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText
            // 
            this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Location = new System.Drawing.Point(5, 38);
            this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Name = "labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText";
            this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Size = new System.Drawing.Size(537, 16);
            this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.TabIndex = 12;
            this.labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_BLOCK_HEIGHT_SYNC_TEXT";
            // 
            // labelMainInterfaceNetworkStatsCurrentDifficultyText
            // 
            this.labelMainInterfaceNetworkStatsCurrentDifficultyText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsCurrentDifficultyText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsCurrentDifficultyText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsCurrentDifficultyText.Location = new System.Drawing.Point(5, 61);
            this.labelMainInterfaceNetworkStatsCurrentDifficultyText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsCurrentDifficultyText.Name = "labelMainInterfaceNetworkStatsCurrentDifficultyText";
            this.labelMainInterfaceNetworkStatsCurrentDifficultyText.Size = new System.Drawing.Size(467, 16);
            this.labelMainInterfaceNetworkStatsCurrentDifficultyText.TabIndex = 11;
            this.labelMainInterfaceNetworkStatsCurrentDifficultyText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_DIFFICULTY_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTitleText
            // 
            this.labelMainInterfaceNetworkStatsTitleText.AutoSize = true;
            this.labelMainInterfaceNetworkStatsTitleText.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceNetworkStatsTitleText.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceNetworkStatsTitleText.Location = new System.Drawing.Point(149, 9);
            this.labelMainInterfaceNetworkStatsTitleText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceNetworkStatsTitleText.Name = "labelMainInterfaceNetworkStatsTitleText";
            this.labelMainInterfaceNetworkStatsTitleText.Size = new System.Drawing.Size(433, 18);
            this.labelMainInterfaceNetworkStatsTitleText.TabIndex = 10;
            this.labelMainInterfaceNetworkStatsTitleText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TITLE_TEXT";
            // 
            // panelRecentTransactions
            // 
            this.panelRecentTransactions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(227)))), ((int)(((byte)(240)))));
            this.panelRecentTransactions.BorderColor = System.Drawing.Color.DarkGray;
            this.panelRecentTransactions.BorderSize = 1F;
            this.panelRecentTransactions.Controls.Add(this.panelInternalRecentTransactions);
            this.panelRecentTransactions.Controls.Add(this.labelMainInterfaceRecentTransaction);
            this.panelRecentTransactions.Location = new System.Drawing.Point(847, 23);
            this.panelRecentTransactions.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelRecentTransactions.Name = "panelRecentTransactions";
            this.panelRecentTransactions.Radius = 10;
            this.panelRecentTransactions.Size = new System.Drawing.Size(511, 507);
            this.panelRecentTransactions.TabIndex = 10;
            this.panelRecentTransactions.Paint += new System.Windows.Forms.PaintEventHandler(this.panelRecentTransactions_Paint);
            // 
            // panelInternalRecentTransactions
            // 
            this.panelInternalRecentTransactions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(249)))), ((int)(((byte)(252)))));
            this.panelInternalRecentTransactions.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(106)))), ((int)(((byte)(128)))));
            this.panelInternalRecentTransactions.BorderSize = 2F;
            this.panelInternalRecentTransactions.Location = new System.Drawing.Point(22, 85);
            this.panelInternalRecentTransactions.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelInternalRecentTransactions.Name = "panelInternalRecentTransactions";
            this.panelInternalRecentTransactions.Radius = 10;
            this.panelInternalRecentTransactions.Size = new System.Drawing.Size(467, 375);
            this.panelInternalRecentTransactions.TabIndex = 2;
            this.panelInternalRecentTransactions.Click += new System.EventHandler(this.panelInternalRecentTransactions_Click);
            this.panelInternalRecentTransactions.Paint += new System.Windows.Forms.PaintEventHandler(this.panelInternalRecentTransactions_Paint);
            this.panelInternalRecentTransactions.MouseEnter += new System.EventHandler(this.panelInternalRecentTransactions_MouseEnter);
            this.panelInternalRecentTransactions.MouseLeave += new System.EventHandler(this.panelInternalRecentTransactions_MouseLeave);
            this.panelInternalRecentTransactions.MouseHover += new System.EventHandler(this.panelInternalRecentTransactions_MouseHover);
            this.panelInternalRecentTransactions.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panelInternalRecentTransactions_MouseMove);
            // 
            // labelMainInterfaceRecentTransaction
            // 
            this.labelMainInterfaceRecentTransaction.AutoSize = true;
            this.labelMainInterfaceRecentTransaction.ForeColor = System.Drawing.Color.Black;
            this.labelMainInterfaceRecentTransaction.Location = new System.Drawing.Point(8, 10);
            this.labelMainInterfaceRecentTransaction.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceRecentTransaction.Name = "labelMainInterfaceRecentTransaction";
            this.labelMainInterfaceRecentTransaction.Size = new System.Drawing.Size(424, 16);
            this.labelMainInterfaceRecentTransaction.TabIndex = 1;
            this.labelMainInterfaceRecentTransaction.Text = "LABEL_MAIN_INTERFACE_RECENT_TRANSACTION_TEXT";
            this.labelMainInterfaceRecentTransaction.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // labelMainInterfaceTotalBalanceAmountText
            // 
            this.labelMainInterfaceTotalBalanceAmountText.AutoSize = true;
            this.labelMainInterfaceTotalBalanceAmountText.BackColor = System.Drawing.Color.Transparent;
            this.labelMainInterfaceTotalBalanceAmountText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceTotalBalanceAmountText.ForeColor = System.Drawing.Color.Ivory;
            this.labelMainInterfaceTotalBalanceAmountText.Location = new System.Drawing.Point(27, 100);
            this.labelMainInterfaceTotalBalanceAmountText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceTotalBalanceAmountText.Name = "labelMainInterfaceTotalBalanceAmountText";
            this.labelMainInterfaceTotalBalanceAmountText.Size = new System.Drawing.Size(401, 15);
            this.labelMainInterfaceTotalBalanceAmountText.TabIndex = 9;
            this.labelMainInterfaceTotalBalanceAmountText.Text = "LABEL_MAIN_INTERFACE_TOTAL_BALANCE_AMOUNT_TEXT";
            // 
            // labelMainInterfacePendingBalanceAmountText
            // 
            this.labelMainInterfacePendingBalanceAmountText.AutoSize = true;
            this.labelMainInterfacePendingBalanceAmountText.BackColor = System.Drawing.Color.Transparent;
            this.labelMainInterfacePendingBalanceAmountText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfacePendingBalanceAmountText.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.labelMainInterfacePendingBalanceAmountText.Location = new System.Drawing.Point(27, 67);
            this.labelMainInterfacePendingBalanceAmountText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfacePendingBalanceAmountText.Name = "labelMainInterfacePendingBalanceAmountText";
            this.labelMainInterfacePendingBalanceAmountText.Size = new System.Drawing.Size(421, 15);
            this.labelMainInterfacePendingBalanceAmountText.TabIndex = 8;
            this.labelMainInterfacePendingBalanceAmountText.Text = "LABEL_MAIN_INTERFACE_PENDING_BALANCE_AMOUNT_TEXT";
            // 
            // labelMainInterfaceAvailableBalanceAmountText
            // 
            this.labelMainInterfaceAvailableBalanceAmountText.AutoSize = true;
            this.labelMainInterfaceAvailableBalanceAmountText.BackColor = System.Drawing.Color.Transparent;
            this.labelMainInterfaceAvailableBalanceAmountText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceAvailableBalanceAmountText.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelMainInterfaceAvailableBalanceAmountText.Location = new System.Drawing.Point(27, 40);
            this.labelMainInterfaceAvailableBalanceAmountText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceAvailableBalanceAmountText.Name = "labelMainInterfaceAvailableBalanceAmountText";
            this.labelMainInterfaceAvailableBalanceAmountText.Size = new System.Drawing.Size(429, 15);
            this.labelMainInterfaceAvailableBalanceAmountText.TabIndex = 7;
            this.labelMainInterfaceAvailableBalanceAmountText.Text = "LABEL_MAIN_INTERFACE_AVAILABLE_BALANCE_AMOUNT_TEXT";
            // 
            // panelSeperatorBalanceLine
            // 
            this.panelSeperatorBalanceLine.BackColor = System.Drawing.Color.Black;
            this.panelSeperatorBalanceLine.BorderColor = System.Drawing.Color.White;
            this.panelSeperatorBalanceLine.BorderSize = 3F;
            this.panelSeperatorBalanceLine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelSeperatorBalanceLine.Location = new System.Drawing.Point(30, 90);
            this.panelSeperatorBalanceLine.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelSeperatorBalanceLine.Name = "panelSeperatorBalanceLine";
            this.panelSeperatorBalanceLine.Radius = 2;
            this.panelSeperatorBalanceLine.Size = new System.Drawing.Size(279, 2);
            this.panelSeperatorBalanceLine.TabIndex = 6;
            // 
            // labelMainInterfaceCurrentBalanceText
            // 
            this.labelMainInterfaceCurrentBalanceText.AutoSize = true;
            this.labelMainInterfaceCurrentBalanceText.BackColor = System.Drawing.Color.Transparent;
            this.labelMainInterfaceCurrentBalanceText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelMainInterfaceCurrentBalanceText.ForeColor = System.Drawing.Color.Ivory;
            this.labelMainInterfaceCurrentBalanceText.Location = new System.Drawing.Point(27, 16);
            this.labelMainInterfaceCurrentBalanceText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelMainInterfaceCurrentBalanceText.Name = "labelMainInterfaceCurrentBalanceText";
            this.labelMainInterfaceCurrentBalanceText.Size = new System.Drawing.Size(397, 16);
            this.labelMainInterfaceCurrentBalanceText.TabIndex = 4;
            this.labelMainInterfaceCurrentBalanceText.Text = "LABEL_MAIN_INTERFACE_CURRENT_BALANCE_TEXT";
            // 
            // tabPageSendTransaction
            // 
            this.tabPageSendTransaction.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(77)))), ((int)(((byte)(104)))), ((int)(((byte)(145)))));
            this.tabPageSendTransaction.Controls.Add(this.buttonSendTransactionDoProcess);
            this.tabPageSendTransaction.Controls.Add(this.panelSendTransaction);
            this.tabPageSendTransaction.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.tabPageSendTransaction.Location = new System.Drawing.Point(4, 34);
            this.tabPageSendTransaction.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPageSendTransaction.Name = "tabPageSendTransaction";
            this.tabPageSendTransaction.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPageSendTransaction.Size = new System.Drawing.Size(1369, 546);
            this.tabPageSendTransaction.TabIndex = 1;
            this.tabPageSendTransaction.Text = "TABPAGE_SEND_TRANSACTION_TEXT";
            this.tabPageSendTransaction.Paint += new System.Windows.Forms.PaintEventHandler(this.tabPageSendTransaction_Paint);
            // 
            // buttonSendTransactionDoProcess
            // 
            this.buttonSendTransactionDoProcess.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonSendTransactionDoProcess.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSendTransactionDoProcess.ForeColor = System.Drawing.Color.Black;
            this.buttonSendTransactionDoProcess.Location = new System.Drawing.Point(447, 497);
            this.buttonSendTransactionDoProcess.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonSendTransactionDoProcess.Name = "buttonSendTransactionDoProcess";
            this.buttonSendTransactionDoProcess.Size = new System.Drawing.Size(483, 36);
            this.buttonSendTransactionDoProcess.TabIndex = 5;
            this.buttonSendTransactionDoProcess.Text = "BUTTON_SEND_TRANSACTION_DO_PROCESS_TEXT";
            this.buttonSendTransactionDoProcess.UseVisualStyleBackColor = false;
            this.buttonSendTransactionDoProcess.Click += new System.EventHandler(this.buttonSendTransactionDoProcess_Click);
            // 
            // panelSendTransaction
            // 
            this.panelSendTransaction.BackColor = System.Drawing.Color.AliceBlue;
            this.panelSendTransaction.BorderColor = System.Drawing.Color.Ivory;
            this.panelSendTransaction.BorderSize = 2F;
            this.panelSendTransaction.Controls.Add(this.panelSendTransactionDetails);
            this.panelSendTransaction.Controls.Add(this.textBoxSendTransactionConfirmationsCountTarget);
            this.panelSendTransaction.Controls.Add(this.labelSendTransactionConfirmationTimeEstimated);
            this.panelSendTransaction.Controls.Add(this.labelSendTransactionPaymentId);
            this.panelSendTransaction.Controls.Add(this.textBoxSendTransactionPaymentId);
            this.panelSendTransaction.Controls.Add(this.labelSendTransactionAvailableBalanceText);
            this.panelSendTransaction.Controls.Add(this.buttonSendTransactionOpenContactList);
            this.panelSendTransaction.Controls.Add(this.labelSendTransactionConfirmationCountTarget);
            this.panelSendTransaction.Controls.Add(this.labelSendTransactionAmountSelected);
            this.panelSendTransaction.Controls.Add(this.textBoxSendTransactionAmountSelected);
            this.panelSendTransaction.Controls.Add(this.labelSendTransactionWalletAddressTarget);
            this.panelSendTransaction.Controls.Add(this.textBoxSendTransactionWalletAddressTarget);
            this.panelSendTransaction.Location = new System.Drawing.Point(9, 15);
            this.panelSendTransaction.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelSendTransaction.Name = "panelSendTransaction";
            this.panelSendTransaction.Radius = 10;
            this.panelSendTransaction.Size = new System.Drawing.Size(1349, 477);
            this.panelSendTransaction.TabIndex = 0;
            // 
            // panelSendTransactionDetails
            // 
            this.panelSendTransactionDetails.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this.panelSendTransactionDetails.BorderColor = System.Drawing.Color.Ivory;
            this.panelSendTransactionDetails.BorderSize = 1F;
            this.panelSendTransactionDetails.Controls.Add(this.labelSendTransactionAmountToSpend);
            this.panelSendTransactionDetails.Controls.Add(this.labelSendTransactionFeeSizeCost);
            this.panelSendTransactionDetails.Controls.Add(this.textBoxSendTransactionFeeCalculated);
            this.panelSendTransactionDetails.Controls.Add(this.textBoxSendTransactionFeeSizeCost);
            this.panelSendTransactionDetails.Controls.Add(this.labelSendTransactionFeeCalculated);
            this.panelSendTransactionDetails.Controls.Add(this.labelSendTransactionFeeConfirmationCost);
            this.panelSendTransactionDetails.Controls.Add(this.textBoxSendTransactionAmountToSpend);
            this.panelSendTransactionDetails.Controls.Add(this.textBoxSendTransactionFeeConfirmationCost);
            this.panelSendTransactionDetails.Controls.Add(this.textBoxSendTransactionTotalAmountSource);
            this.panelSendTransactionDetails.Controls.Add(this.labelSendTransactionTotalAmountSource);
            this.panelSendTransactionDetails.Location = new System.Drawing.Point(752, 102);
            this.panelSendTransactionDetails.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelSendTransactionDetails.Name = "panelSendTransactionDetails";
            this.panelSendTransactionDetails.Radius = 10;
            this.panelSendTransactionDetails.Size = new System.Drawing.Size(593, 372);
            this.panelSendTransactionDetails.TabIndex = 23;
            this.panelSendTransactionDetails.Paint += new System.Windows.Forms.PaintEventHandler(this.panelSendTransactionDetails_Paint);
            // 
            // labelSendTransactionAmountToSpend
            // 
            this.labelSendTransactionAmountToSpend.AutoSize = true;
            this.labelSendTransactionAmountToSpend.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionAmountToSpend.Location = new System.Drawing.Point(41, 27);
            this.labelSendTransactionAmountToSpend.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionAmountToSpend.Name = "labelSendTransactionAmountToSpend";
            this.labelSendTransactionAmountToSpend.Size = new System.Drawing.Size(428, 16);
            this.labelSendTransactionAmountToSpend.TabIndex = 16;
            this.labelSendTransactionAmountToSpend.Text = "LABEL_SEND_TRANSACTION_AMOUNT_TO_SPEND_TEXT";
            // 
            // labelSendTransactionFeeSizeCost
            // 
            this.labelSendTransactionFeeSizeCost.AutoSize = true;
            this.labelSendTransactionFeeSizeCost.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionFeeSizeCost.Location = new System.Drawing.Point(41, 163);
            this.labelSendTransactionFeeSizeCost.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionFeeSizeCost.Name = "labelSendTransactionFeeSizeCost";
            this.labelSendTransactionFeeSizeCost.Size = new System.Drawing.Size(393, 16);
            this.labelSendTransactionFeeSizeCost.TabIndex = 22;
            this.labelSendTransactionFeeSizeCost.Text = "LABEL_SEND_TRANSACTION_FEE_SIZE_COST_TEXT";
            // 
            // textBoxSendTransactionFeeCalculated
            // 
            this.textBoxSendTransactionFeeCalculated.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.textBoxSendTransactionFeeCalculated.Location = new System.Drawing.Point(44, 317);
            this.textBoxSendTransactionFeeCalculated.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxSendTransactionFeeCalculated.Multiline = true;
            this.textBoxSendTransactionFeeCalculated.Name = "textBoxSendTransactionFeeCalculated";
            this.textBoxSendTransactionFeeCalculated.ReadOnly = true;
            this.textBoxSendTransactionFeeCalculated.Size = new System.Drawing.Size(474, 25);
            this.textBoxSendTransactionFeeCalculated.TabIndex = 4;
            // 
            // textBoxSendTransactionFeeSizeCost
            // 
            this.textBoxSendTransactionFeeSizeCost.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.textBoxSendTransactionFeeSizeCost.Location = new System.Drawing.Point(44, 185);
            this.textBoxSendTransactionFeeSizeCost.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxSendTransactionFeeSizeCost.Multiline = true;
            this.textBoxSendTransactionFeeSizeCost.Name = "textBoxSendTransactionFeeSizeCost";
            this.textBoxSendTransactionFeeSizeCost.ReadOnly = true;
            this.textBoxSendTransactionFeeSizeCost.Size = new System.Drawing.Size(474, 25);
            this.textBoxSendTransactionFeeSizeCost.TabIndex = 21;
            // 
            // labelSendTransactionFeeCalculated
            // 
            this.labelSendTransactionFeeCalculated.AutoSize = true;
            this.labelSendTransactionFeeCalculated.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionFeeCalculated.Location = new System.Drawing.Point(41, 295);
            this.labelSendTransactionFeeCalculated.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionFeeCalculated.Name = "labelSendTransactionFeeCalculated";
            this.labelSendTransactionFeeCalculated.Size = new System.Drawing.Size(409, 16);
            this.labelSendTransactionFeeCalculated.TabIndex = 5;
            this.labelSendTransactionFeeCalculated.Text = "LABEL_SEND_TRANSACTION_FEE_CALCULATED_TEXT";
            // 
            // labelSendTransactionFeeConfirmationCost
            // 
            this.labelSendTransactionFeeConfirmationCost.AutoSize = true;
            this.labelSendTransactionFeeConfirmationCost.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionFeeConfirmationCost.Location = new System.Drawing.Point(41, 228);
            this.labelSendTransactionFeeConfirmationCost.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionFeeConfirmationCost.Name = "labelSendTransactionFeeConfirmationCost";
            this.labelSendTransactionFeeConfirmationCost.Size = new System.Drawing.Size(474, 16);
            this.labelSendTransactionFeeConfirmationCost.TabIndex = 20;
            this.labelSendTransactionFeeConfirmationCost.Text = "LABEL_SEND_TRANSACTION_FEE_CONFIRMATION_COST_TEXT";
            // 
            // textBoxSendTransactionAmountToSpend
            // 
            this.textBoxSendTransactionAmountToSpend.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.textBoxSendTransactionAmountToSpend.Location = new System.Drawing.Point(44, 48);
            this.textBoxSendTransactionAmountToSpend.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxSendTransactionAmountToSpend.Multiline = true;
            this.textBoxSendTransactionAmountToSpend.Name = "textBoxSendTransactionAmountToSpend";
            this.textBoxSendTransactionAmountToSpend.ReadOnly = true;
            this.textBoxSendTransactionAmountToSpend.Size = new System.Drawing.Size(474, 25);
            this.textBoxSendTransactionAmountToSpend.TabIndex = 15;
            // 
            // textBoxSendTransactionFeeConfirmationCost
            // 
            this.textBoxSendTransactionFeeConfirmationCost.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.textBoxSendTransactionFeeConfirmationCost.Location = new System.Drawing.Point(44, 250);
            this.textBoxSendTransactionFeeConfirmationCost.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxSendTransactionFeeConfirmationCost.Multiline = true;
            this.textBoxSendTransactionFeeConfirmationCost.Name = "textBoxSendTransactionFeeConfirmationCost";
            this.textBoxSendTransactionFeeConfirmationCost.ReadOnly = true;
            this.textBoxSendTransactionFeeConfirmationCost.Size = new System.Drawing.Size(474, 25);
            this.textBoxSendTransactionFeeConfirmationCost.TabIndex = 19;
            // 
            // textBoxSendTransactionTotalAmountSource
            // 
            this.textBoxSendTransactionTotalAmountSource.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.textBoxSendTransactionTotalAmountSource.Location = new System.Drawing.Point(44, 118);
            this.textBoxSendTransactionTotalAmountSource.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxSendTransactionTotalAmountSource.Multiline = true;
            this.textBoxSendTransactionTotalAmountSource.Name = "textBoxSendTransactionTotalAmountSource";
            this.textBoxSendTransactionTotalAmountSource.ReadOnly = true;
            this.textBoxSendTransactionTotalAmountSource.Size = new System.Drawing.Size(474, 25);
            this.textBoxSendTransactionTotalAmountSource.TabIndex = 17;
            // 
            // labelSendTransactionTotalAmountSource
            // 
            this.labelSendTransactionTotalAmountSource.AutoSize = true;
            this.labelSendTransactionTotalAmountSource.ForeColor = System.Drawing.Color.Ivory;
            this.labelSendTransactionTotalAmountSource.Location = new System.Drawing.Point(41, 96);
            this.labelSendTransactionTotalAmountSource.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionTotalAmountSource.Name = "labelSendTransactionTotalAmountSource";
            this.labelSendTransactionTotalAmountSource.Size = new System.Drawing.Size(467, 16);
            this.labelSendTransactionTotalAmountSource.TabIndex = 18;
            this.labelSendTransactionTotalAmountSource.Text = "LABEL_SEND_TRANSACTION_TOTAL_AMOUNT_SOURCE_TEXT";
            // 
            // textBoxSendTransactionConfirmationsCountTarget
            // 
            this.textBoxSendTransactionConfirmationsCountTarget.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.textBoxSendTransactionConfirmationsCountTarget.Location = new System.Drawing.Point(19, 267);
            this.textBoxSendTransactionConfirmationsCountTarget.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxSendTransactionConfirmationsCountTarget.Multiline = true;
            this.textBoxSendTransactionConfirmationsCountTarget.Name = "textBoxSendTransactionConfirmationsCountTarget";
            this.textBoxSendTransactionConfirmationsCountTarget.Size = new System.Drawing.Size(255, 25);
            this.textBoxSendTransactionConfirmationsCountTarget.TabIndex = 14;
            this.textBoxSendTransactionConfirmationsCountTarget.TextChanged += new System.EventHandler(this.textBoxSendTransactionConfirmationsCountTarget_TextChanged);
            this.textBoxSendTransactionConfirmationsCountTarget.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxSendTransactionConfirmationsCountTarget_KeyDown);
            // 
            // labelSendTransactionConfirmationTimeEstimated
            // 
            this.labelSendTransactionConfirmationTimeEstimated.AutoSize = true;
            this.labelSendTransactionConfirmationTimeEstimated.Location = new System.Drawing.Point(15, 301);
            this.labelSendTransactionConfirmationTimeEstimated.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionConfirmationTimeEstimated.Name = "labelSendTransactionConfirmationTimeEstimated";
            this.labelSendTransactionConfirmationTimeEstimated.Size = new System.Drawing.Size(527, 16);
            this.labelSendTransactionConfirmationTimeEstimated.TabIndex = 13;
            this.labelSendTransactionConfirmationTimeEstimated.Text = "LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_TEXT";
            // 
            // labelSendTransactionPaymentId
            // 
            this.labelSendTransactionPaymentId.AutoSize = true;
            this.labelSendTransactionPaymentId.Location = new System.Drawing.Point(15, 363);
            this.labelSendTransactionPaymentId.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionPaymentId.Name = "labelSendTransactionPaymentId";
            this.labelSendTransactionPaymentId.Size = new System.Drawing.Size(370, 16);
            this.labelSendTransactionPaymentId.TabIndex = 12;
            this.labelSendTransactionPaymentId.Text = "LABEL_SEND_TRANSACTION_PAYMENT_ID_TEXT";
            // 
            // textBoxSendTransactionPaymentId
            // 
            this.textBoxSendTransactionPaymentId.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.textBoxSendTransactionPaymentId.Location = new System.Drawing.Point(19, 385);
            this.textBoxSendTransactionPaymentId.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxSendTransactionPaymentId.Multiline = true;
            this.textBoxSendTransactionPaymentId.Name = "textBoxSendTransactionPaymentId";
            this.textBoxSendTransactionPaymentId.Size = new System.Drawing.Size(446, 25);
            this.textBoxSendTransactionPaymentId.TabIndex = 11;
            this.textBoxSendTransactionPaymentId.Text = "0";
            this.textBoxSendTransactionPaymentId.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxSendTransactionPaymentId_KeyDown);
            // 
            // labelSendTransactionAvailableBalanceText
            // 
            this.labelSendTransactionAvailableBalanceText.AutoSize = true;
            this.labelSendTransactionAvailableBalanceText.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelSendTransactionAvailableBalanceText.Location = new System.Drawing.Point(15, 13);
            this.labelSendTransactionAvailableBalanceText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionAvailableBalanceText.Name = "labelSendTransactionAvailableBalanceText";
            this.labelSendTransactionAvailableBalanceText.Size = new System.Drawing.Size(466, 18);
            this.labelSendTransactionAvailableBalanceText.TabIndex = 1;
            this.labelSendTransactionAvailableBalanceText.Text = "LABEL_SEND_TRANSACTION_AVAILABLE_BALANCE_TEXT";
            // 
            // buttonSendTransactionOpenContactList
            // 
            this.buttonSendTransactionOpenContactList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonSendTransactionOpenContactList.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSendTransactionOpenContactList.ForeColor = System.Drawing.Color.Black;
            this.buttonSendTransactionOpenContactList.Location = new System.Drawing.Point(16, 102);
            this.buttonSendTransactionOpenContactList.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonSendTransactionOpenContactList.Name = "buttonSendTransactionOpenContactList";
            this.buttonSendTransactionOpenContactList.Size = new System.Drawing.Size(558, 30);
            this.buttonSendTransactionOpenContactList.TabIndex = 10;
            this.buttonSendTransactionOpenContactList.Text = "BUTTON_SEND_TRANSACTION_OPEN_CONTACT_LIST_TEXT";
            this.buttonSendTransactionOpenContactList.UseVisualStyleBackColor = false;
            // 
            // labelSendTransactionConfirmationCountTarget
            // 
            this.labelSendTransactionConfirmationCountTarget.AutoSize = true;
            this.labelSendTransactionConfirmationCountTarget.Location = new System.Drawing.Point(15, 245);
            this.labelSendTransactionConfirmationCountTarget.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionConfirmationCountTarget.Name = "labelSendTransactionConfirmationCountTarget";
            this.labelSendTransactionConfirmationCountTarget.Size = new System.Drawing.Size(519, 16);
            this.labelSendTransactionConfirmationCountTarget.TabIndex = 7;
            this.labelSendTransactionConfirmationCountTarget.Text = "LABEL_SEND_TRANSACTION_CONFIRMATION_COUNT_TARGET_TEXT";
            // 
            // labelSendTransactionAmountSelected
            // 
            this.labelSendTransactionAmountSelected.AutoSize = true;
            this.labelSendTransactionAmountSelected.Location = new System.Drawing.Point(13, 168);
            this.labelSendTransactionAmountSelected.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionAmountSelected.Name = "labelSendTransactionAmountSelected";
            this.labelSendTransactionAmountSelected.Size = new System.Drawing.Size(426, 16);
            this.labelSendTransactionAmountSelected.TabIndex = 3;
            this.labelSendTransactionAmountSelected.Text = "LABEL_SEND_TRANSACTION_AMOUNT_SELECTED_TEXT";
            // 
            // textBoxSendTransactionAmountSelected
            // 
            this.textBoxSendTransactionAmountSelected.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.textBoxSendTransactionAmountSelected.Location = new System.Drawing.Point(16, 190);
            this.textBoxSendTransactionAmountSelected.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxSendTransactionAmountSelected.Multiline = true;
            this.textBoxSendTransactionAmountSelected.Name = "textBoxSendTransactionAmountSelected";
            this.textBoxSendTransactionAmountSelected.Size = new System.Drawing.Size(446, 25);
            this.textBoxSendTransactionAmountSelected.TabIndex = 2;
            this.textBoxSendTransactionAmountSelected.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxSendTransactionAmountSelected_KeyDown);
            this.textBoxSendTransactionAmountSelected.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBoxSendTransactionAmountSelected_KeyUp);
            // 
            // labelSendTransactionWalletAddressTarget
            // 
            this.labelSendTransactionWalletAddressTarget.AutoSize = true;
            this.labelSendTransactionWalletAddressTarget.Location = new System.Drawing.Point(15, 47);
            this.labelSendTransactionWalletAddressTarget.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSendTransactionWalletAddressTarget.Name = "labelSendTransactionWalletAddressTarget";
            this.labelSendTransactionWalletAddressTarget.Size = new System.Drawing.Size(485, 16);
            this.labelSendTransactionWalletAddressTarget.TabIndex = 1;
            this.labelSendTransactionWalletAddressTarget.Text = "LABEL_SEND_TRANSACTION_WALLET_ADDRESS_TARGET_TEXT";
            // 
            // textBoxSendTransactionWalletAddressTarget
            // 
            this.textBoxSendTransactionWalletAddressTarget.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.textBoxSendTransactionWalletAddressTarget.Location = new System.Drawing.Point(16, 69);
            this.textBoxSendTransactionWalletAddressTarget.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxSendTransactionWalletAddressTarget.Multiline = true;
            this.textBoxSendTransactionWalletAddressTarget.Name = "textBoxSendTransactionWalletAddressTarget";
            this.textBoxSendTransactionWalletAddressTarget.Size = new System.Drawing.Size(1328, 25);
            this.textBoxSendTransactionWalletAddressTarget.TabIndex = 0;
            this.textBoxSendTransactionWalletAddressTarget.TextChanged += new System.EventHandler(this.textBoxSendTransactionWalletAddressTarget_TextChanged);
            this.textBoxSendTransactionWalletAddressTarget.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxSendTransactionWalletAddressTarget_KeyDown);
            // 
            // tabPageReceiveTransaction
            // 
            this.tabPageReceiveTransaction.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(77)))), ((int)(((byte)(104)))), ((int)(((byte)(145)))));
            this.tabPageReceiveTransaction.Controls.Add(this.buttonSaveQrCodeReceiveTransactionWalletAddress);
            this.tabPageReceiveTransaction.Controls.Add(this.buttonPrintQrCodeReceiveTransactionWalletAddress);
            this.tabPageReceiveTransaction.Controls.Add(this.labelWalletReceiveTransactionQrCodeText);
            this.tabPageReceiveTransaction.Controls.Add(this.panelQrCodeWalletAddress);
            this.tabPageReceiveTransaction.Location = new System.Drawing.Point(4, 34);
            this.tabPageReceiveTransaction.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPageReceiveTransaction.Name = "tabPageReceiveTransaction";
            this.tabPageReceiveTransaction.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPageReceiveTransaction.Size = new System.Drawing.Size(1369, 546);
            this.tabPageReceiveTransaction.TabIndex = 4;
            this.tabPageReceiveTransaction.Text = "TABPAGE_RECEIVE_TRANSACTION_TEXT";
            // 
            // buttonSaveQrCodeReceiveTransactionWalletAddress
            // 
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.ForeColor = System.Drawing.Color.Black;
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.Location = new System.Drawing.Point(453, 427);
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.Name = "buttonSaveQrCodeReceiveTransactionWalletAddress";
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.Size = new System.Drawing.Size(475, 36);
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.TabIndex = 5;
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.Text = "BUTTON_MAIN_INTERFACE_SAVE_QR_CODE_TEXT";
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.UseVisualStyleBackColor = false;
            this.buttonSaveQrCodeReceiveTransactionWalletAddress.Click += new System.EventHandler(this.buttonSaveQrCodeReceiveTransactionWalletAddress_Click);
            // 
            // buttonPrintQrCodeReceiveTransactionWalletAddress
            // 
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.ForeColor = System.Drawing.Color.Black;
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.Location = new System.Drawing.Point(453, 473);
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.Name = "buttonPrintQrCodeReceiveTransactionWalletAddress";
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.Size = new System.Drawing.Size(475, 36);
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.TabIndex = 4;
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.Text = "BUTTON_MAIN_INTERFACE_PRINT_QR_CODE_TEXT";
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.UseVisualStyleBackColor = false;
            this.buttonPrintQrCodeReceiveTransactionWalletAddress.Click += new System.EventHandler(this.buttonPrintQrCodeReceiveTransactionWalletAddress_Click);
            // 
            // labelWalletReceiveTransactionQrCodeText
            // 
            this.labelWalletReceiveTransactionQrCodeText.AutoSize = true;
            this.labelWalletReceiveTransactionQrCodeText.ForeColor = System.Drawing.Color.Ivory;
            this.labelWalletReceiveTransactionQrCodeText.Location = new System.Drawing.Point(455, 36);
            this.labelWalletReceiveTransactionQrCodeText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelWalletReceiveTransactionQrCodeText.Name = "labelWalletReceiveTransactionQrCodeText";
            this.labelWalletReceiveTransactionQrCodeText.Size = new System.Drawing.Size(441, 16);
            this.labelWalletReceiveTransactionQrCodeText.TabIndex = 3;
            this.labelWalletReceiveTransactionQrCodeText.Text = "LABEL_MAIN_INTERFACE_QR_CODE_RECEIVE_TITLE_TEXT";
            // 
            // panelQrCodeWalletAddress
            // 
            this.panelQrCodeWalletAddress.BackColor = System.Drawing.Color.White;
            this.panelQrCodeWalletAddress.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panelQrCodeWalletAddress.BorderColor = System.Drawing.Color.Ivory;
            this.panelQrCodeWalletAddress.BorderSize = 1F;
            this.panelQrCodeWalletAddress.Location = new System.Drawing.Point(512, 74);
            this.panelQrCodeWalletAddress.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelQrCodeWalletAddress.Name = "panelQrCodeWalletAddress";
            this.panelQrCodeWalletAddress.Radius = 30;
            this.panelQrCodeWalletAddress.Size = new System.Drawing.Size(350, 346);
            this.panelQrCodeWalletAddress.TabIndex = 0;
            // 
            // tabPageTransactionHistory
            // 
            this.tabPageTransactionHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(77)))), ((int)(((byte)(104)))), ((int)(((byte)(145)))));
            this.tabPageTransactionHistory.Controls.Add(this.buttonMainInterfaceSearchTransactionHistory);
            this.tabPageTransactionHistory.Controls.Add(this.textBoxTransactionHistorySearch);
            this.tabPageTransactionHistory.Controls.Add(this.panelTransactionHistoryColumns);
            this.tabPageTransactionHistory.Controls.Add(this.textBoxMainInterfaceMaxPageTransactionHistory);
            this.tabPageTransactionHistory.Controls.Add(this.textBoxMainInterfaceCurrentPageTransactionHistory);
            this.tabPageTransactionHistory.Controls.Add(this.buttonMainInterfaceNextPageTransactionHistory);
            this.tabPageTransactionHistory.Controls.Add(this.buttonMainInterfaceBackPageTransactionHistory);
            this.tabPageTransactionHistory.Controls.Add(this.buttonMainInterfaceExportTransactionHistory);
            this.tabPageTransactionHistory.Controls.Add(this.panelTransactionHistory);
            this.tabPageTransactionHistory.Location = new System.Drawing.Point(4, 34);
            this.tabPageTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPageTransactionHistory.Name = "tabPageTransactionHistory";
            this.tabPageTransactionHistory.Size = new System.Drawing.Size(1369, 546);
            this.tabPageTransactionHistory.TabIndex = 2;
            this.tabPageTransactionHistory.Text = "TABPAGE_TRANSACTION_HISTORY_TEXT";
            this.tabPageTransactionHistory.Paint += new System.Windows.Forms.PaintEventHandler(this.tabPageTransactionHistory_Paint);
            // 
            // buttonMainInterfaceSearchTransactionHistory
            // 
            this.buttonMainInterfaceSearchTransactionHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonMainInterfaceSearchTransactionHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMainInterfaceSearchTransactionHistory.ForeColor = System.Drawing.Color.Black;
            this.buttonMainInterfaceSearchTransactionHistory.Location = new System.Drawing.Point(930, 508);
            this.buttonMainInterfaceSearchTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonMainInterfaceSearchTransactionHistory.Name = "buttonMainInterfaceSearchTransactionHistory";
            this.buttonMainInterfaceSearchTransactionHistory.Size = new System.Drawing.Size(177, 25);
            this.buttonMainInterfaceSearchTransactionHistory.TabIndex = 13;
            this.buttonMainInterfaceSearchTransactionHistory.Text = "BUTTON_MAIN_INTERFACE_SEARCH_TRANSACTION_HISTORY_TEXT";
            this.buttonMainInterfaceSearchTransactionHistory.UseVisualStyleBackColor = false;
            this.buttonMainInterfaceSearchTransactionHistory.Click += new System.EventHandler(this.buttonMainInterfaceSearchTransactionHistory_Click);
            // 
            // textBoxTransactionHistorySearch
            // 
            this.textBoxTransactionHistorySearch.Location = new System.Drawing.Point(600, 510);
            this.textBoxTransactionHistorySearch.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxTransactionHistorySearch.Name = "textBoxTransactionHistorySearch";
            this.textBoxTransactionHistorySearch.Size = new System.Drawing.Size(318, 22);
            this.textBoxTransactionHistorySearch.TabIndex = 12;
            this.textBoxTransactionHistorySearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxTransactionHistorySearch_KeyDown);
            // 
            // panelTransactionHistoryColumns
            // 
            this.panelTransactionHistoryColumns.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this.panelTransactionHistoryColumns.BorderColor = System.Drawing.Color.Transparent;
            this.panelTransactionHistoryColumns.BorderSize = 1F;
            this.panelTransactionHistoryColumns.Location = new System.Drawing.Point(13, 1);
            this.panelTransactionHistoryColumns.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelTransactionHistoryColumns.Name = "panelTransactionHistoryColumns";
            this.panelTransactionHistoryColumns.Radius = 10;
            this.panelTransactionHistoryColumns.Size = new System.Drawing.Size(1342, 52);
            this.panelTransactionHistoryColumns.TabIndex = 11;
            this.panelTransactionHistoryColumns.Click += new System.EventHandler(this.panelMainInterfaceTransactionHistoryColumns_Click);
            this.panelTransactionHistoryColumns.Paint += new System.Windows.Forms.PaintEventHandler(this.panelMainInterfaceTransactionHistoryColumns_Paint);
            // 
            // textBoxMainInterfaceMaxPageTransactionHistory
            // 
            this.textBoxMainInterfaceMaxPageTransactionHistory.Location = new System.Drawing.Point(482, 510);
            this.textBoxMainInterfaceMaxPageTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxMainInterfaceMaxPageTransactionHistory.Name = "textBoxMainInterfaceMaxPageTransactionHistory";
            this.textBoxMainInterfaceMaxPageTransactionHistory.ReadOnly = true;
            this.textBoxMainInterfaceMaxPageTransactionHistory.Size = new System.Drawing.Size(92, 22);
            this.textBoxMainInterfaceMaxPageTransactionHistory.TabIndex = 10;
            // 
            // textBoxMainInterfaceCurrentPageTransactionHistory
            // 
            this.textBoxMainInterfaceCurrentPageTransactionHistory.Location = new System.Drawing.Point(382, 510);
            this.textBoxMainInterfaceCurrentPageTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxMainInterfaceCurrentPageTransactionHistory.Name = "textBoxMainInterfaceCurrentPageTransactionHistory";
            this.textBoxMainInterfaceCurrentPageTransactionHistory.Size = new System.Drawing.Size(92, 22);
            this.textBoxMainInterfaceCurrentPageTransactionHistory.TabIndex = 9;
            this.textBoxMainInterfaceCurrentPageTransactionHistory.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxMainInterfaceCurrentPageTransactionHistory_KeyDown);
            // 
            // buttonMainInterfaceNextPageTransactionHistory
            // 
            this.buttonMainInterfaceNextPageTransactionHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonMainInterfaceNextPageTransactionHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMainInterfaceNextPageTransactionHistory.ForeColor = System.Drawing.Color.Black;
            this.buttonMainInterfaceNextPageTransactionHistory.Location = new System.Drawing.Point(197, 509);
            this.buttonMainInterfaceNextPageTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonMainInterfaceNextPageTransactionHistory.Name = "buttonMainInterfaceNextPageTransactionHistory";
            this.buttonMainInterfaceNextPageTransactionHistory.Size = new System.Drawing.Size(177, 28);
            this.buttonMainInterfaceNextPageTransactionHistory.TabIndex = 8;
            this.buttonMainInterfaceNextPageTransactionHistory.Text = "BUTTON_MAIN_INTERFACE_NEXT_PAGE_TRANSACTION_HISTORY_TEXT";
            this.buttonMainInterfaceNextPageTransactionHistory.UseVisualStyleBackColor = false;
            this.buttonMainInterfaceNextPageTransactionHistory.Click += new System.EventHandler(this.buttonMainInterfaceNextPageTransactionHistory_Click);
            // 
            // buttonMainInterfaceBackPageTransactionHistory
            // 
            this.buttonMainInterfaceBackPageTransactionHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonMainInterfaceBackPageTransactionHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMainInterfaceBackPageTransactionHistory.ForeColor = System.Drawing.Color.Black;
            this.buttonMainInterfaceBackPageTransactionHistory.Location = new System.Drawing.Point(13, 509);
            this.buttonMainInterfaceBackPageTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonMainInterfaceBackPageTransactionHistory.Name = "buttonMainInterfaceBackPageTransactionHistory";
            this.buttonMainInterfaceBackPageTransactionHistory.Size = new System.Drawing.Size(177, 28);
            this.buttonMainInterfaceBackPageTransactionHistory.TabIndex = 7;
            this.buttonMainInterfaceBackPageTransactionHistory.Text = "BUTTON_MAIN_INTERFACE_BACK_PAGE_TRANSACTION_HISTORY_TEXT";
            this.buttonMainInterfaceBackPageTransactionHistory.UseVisualStyleBackColor = false;
            this.buttonMainInterfaceBackPageTransactionHistory.Click += new System.EventHandler(this.buttonMainInterfaceBackPageTransactionHistory_Click);
            // 
            // buttonMainInterfaceExportTransactionHistory
            // 
            this.buttonMainInterfaceExportTransactionHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(229)))), ((int)(((byte)(72)))));
            this.buttonMainInterfaceExportTransactionHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMainInterfaceExportTransactionHistory.ForeColor = System.Drawing.Color.Black;
            this.buttonMainInterfaceExportTransactionHistory.Location = new System.Drawing.Point(1177, 509);
            this.buttonMainInterfaceExportTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonMainInterfaceExportTransactionHistory.Name = "buttonMainInterfaceExportTransactionHistory";
            this.buttonMainInterfaceExportTransactionHistory.Size = new System.Drawing.Size(177, 28);
            this.buttonMainInterfaceExportTransactionHistory.TabIndex = 5;
            this.buttonMainInterfaceExportTransactionHistory.Text = "BUTTON_MAIN_INTERFACE_EXPORT_TRANSACTION_HISTORY_TEXT";
            this.buttonMainInterfaceExportTransactionHistory.UseVisualStyleBackColor = false;
            this.buttonMainInterfaceExportTransactionHistory.Click += new System.EventHandler(this.buttonMainInterfaceExportTransactionHistory_Click);
            // 
            // panelTransactionHistory
            // 
            this.panelTransactionHistory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this.panelTransactionHistory.BorderColor = System.Drawing.Color.Transparent;
            this.panelTransactionHistory.BorderSize = 1F;
            this.panelTransactionHistory.Location = new System.Drawing.Point(13, 54);
            this.panelTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelTransactionHistory.Name = "panelTransactionHistory";
            this.panelTransactionHistory.Radius = 10;
            this.panelTransactionHistory.Size = new System.Drawing.Size(1342, 450);
            this.panelTransactionHistory.TabIndex = 6;
            this.panelTransactionHistory.Click += new System.EventHandler(this.panelTransactionHistory_Click);
            this.panelTransactionHistory.Paint += new System.Windows.Forms.PaintEventHandler(this.panelTransactionHistory_Paint);
            this.panelTransactionHistory.DoubleClick += new System.EventHandler(this.panelTransactionHistory_DoubleClick);
            this.panelTransactionHistory.MouseLeave += new System.EventHandler(this.panelTransactionHistory_MouseLeave);
            this.panelTransactionHistory.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panelTransactionHistory_MouseMove);
            // 
            // tabPageStoreNetwork
            // 
            this.tabPageStoreNetwork.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(77)))), ((int)(((byte)(104)))), ((int)(((byte)(145)))));
            this.tabPageStoreNetwork.Location = new System.Drawing.Point(4, 34);
            this.tabPageStoreNetwork.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPageStoreNetwork.Name = "tabPageStoreNetwork";
            this.tabPageStoreNetwork.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabPageStoreNetwork.Size = new System.Drawing.Size(1369, 546);
            this.tabPageStoreNetwork.TabIndex = 3;
            this.tabPageStoreNetwork.Text = "TABPAGE_STORE_NETWORK_TEXT";
            // 
            // labelWalletAddressReceiveTransactionTitle
            // 
            this.labelWalletAddressReceiveTransactionTitle.AutoSize = true;
            this.labelWalletAddressReceiveTransactionTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelWalletAddressReceiveTransactionTitle.ForeColor = System.Drawing.Color.Ivory;
            this.labelWalletAddressReceiveTransactionTitle.Location = new System.Drawing.Point(477, 113);
            this.labelWalletAddressReceiveTransactionTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelWalletAddressReceiveTransactionTitle.Name = "labelWalletAddressReceiveTransactionTitle";
            this.labelWalletAddressReceiveTransactionTitle.Size = new System.Drawing.Size(423, 13);
            this.labelWalletAddressReceiveTransactionTitle.TabIndex = 1;
            this.labelWalletAddressReceiveTransactionTitle.Text = "LABEL_MAIN_INTERFACE_WALLET_ADDRESS_RECEIVE_TITLE_TEXT";
            // 
            // labelWalletAddressReceiveTransaction
            // 
            this.labelWalletAddressReceiveTransaction.AutoSize = true;
            this.labelWalletAddressReceiveTransaction.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelWalletAddressReceiveTransaction.ForeColor = System.Drawing.Color.Ivory;
            this.labelWalletAddressReceiveTransaction.Location = new System.Drawing.Point(574, 126);
            this.labelWalletAddressReceiveTransaction.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelWalletAddressReceiveTransaction.Name = "labelWalletAddressReceiveTransaction";
            this.labelWalletAddressReceiveTransaction.Size = new System.Drawing.Size(199, 15);
            this.labelWalletAddressReceiveTransaction.TabIndex = 2;
            this.labelWalletAddressReceiveTransaction.Text = "WALLET_ADDRESS_RECEIVE";
            this.labelWalletAddressReceiveTransaction.Click += new System.EventHandler(this.labelWalletAddressReceiveTransaction_Click);
            // 
            // progressBarMainInterfaceSyncProgress
            // 
            this.progressBarMainInterfaceSyncProgress.BackColor = System.Drawing.Color.GhostWhite;
            this.progressBarMainInterfaceSyncProgress.ForeColor = System.Drawing.Color.Black;
            this.progressBarMainInterfaceSyncProgress.Location = new System.Drawing.Point(492, 751);
            this.progressBarMainInterfaceSyncProgress.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.progressBarMainInterfaceSyncProgress.Maximum = 10000;
            this.progressBarMainInterfaceSyncProgress.Name = "progressBarMainInterfaceSyncProgress";
            this.progressBarMainInterfaceSyncProgress.Size = new System.Drawing.Size(408, 23);
            this.progressBarMainInterfaceSyncProgress.Step = 1;
            this.progressBarMainInterfaceSyncProgress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBarMainInterfaceSyncProgress.TabIndex = 5;
            // 
            // ClassWalletMainInterfaceForm
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(49)))), ((int)(((byte)(55)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(1377, 781);
            this.Controls.Add(this.labelMainInterfaceSyncProgress);
            this.Controls.Add(this.progressBarMainInterfaceSyncProgress);
            this.Controls.Add(this.pictureBoxLogo);
            this.Controls.Add(this.labelWalletAddressReceiveTransaction);
            this.Controls.Add(this.labelWalletAddressReceiveTransactionTitle);
            this.Controls.Add(this.labelWalletOpened);
            this.Controls.Add(this.comboBoxListWalletFile);
            this.Controls.Add(this.tabControlWallet);
            this.Controls.Add(this.menuStripGeneralWallet);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStripGeneralWallet;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.Name = "ClassWalletMainInterfaceForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FORM_TITLE_MAIN_INTERFACE_TEXT";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ClassWalletMainInterfaceForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ClassWalletMainInterfaceForm_FormClosed);
            this.Load += new System.EventHandler(this.ClassWalletMainInterfaceForm_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ClassWalletMainInterfaceForm_Paint);
            this.menuStripGeneralWallet.ResumeLayout(false);
            this.menuStripGeneralWallet.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.tabControlWallet.ResumeLayout(false);
            this.tabPageOverview.ResumeLayout(false);
            this.tabPageOverview.PerformLayout();
            this.panelInternalNetworkStats.ResumeLayout(false);
            this.panelInternalNetworkStats.PerformLayout();
            this.panelRecentTransactions.ResumeLayout(false);
            this.panelRecentTransactions.PerformLayout();
            this.tabPageSendTransaction.ResumeLayout(false);
            this.panelSendTransaction.ResumeLayout(false);
            this.panelSendTransaction.PerformLayout();
            this.panelSendTransactionDetails.ResumeLayout(false);
            this.panelSendTransactionDetails.PerformLayout();
            this.tabPageReceiveTransaction.ResumeLayout(false);
            this.tabPageReceiveTransaction.PerformLayout();
            this.tabPageTransactionHistory.ResumeLayout(false);
            this.tabPageTransactionHistory.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

#endregion

        private System.Windows.Forms.MenuStrip menuStripGeneralWallet;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControlWallet;
        private System.Windows.Forms.TabPage tabPageOverview;
        private System.Windows.Forms.TabPage tabPageSendTransaction;
        private System.Windows.Forms.ComboBox comboBoxListWalletFile;
        private System.Windows.Forms.ToolStripMenuItem openWalletToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeWalletToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TabPage tabPageTransactionHistory;
        private System.Windows.Forms.Label labelWalletOpened;
        private System.Windows.Forms.Label labelMainInterfaceRecentTransaction;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.Label labelMainInterfaceCurrentBalanceText;
        private ClassCustomPanel panelSeperatorBalanceLine;
        private System.Windows.Forms.TabPage tabPageReceiveTransaction;
        private System.Windows.Forms.Button buttonPrintQrCodeReceiveTransactionWalletAddress;
        private System.Windows.Forms.Label labelWalletReceiveTransactionQrCodeText;
        private System.Windows.Forms.Label labelWalletAddressReceiveTransactionTitle;
        private ClassCustomPanel panelQrCodeWalletAddress;
        private System.Windows.Forms.TabPage tabPageStoreNetwork;
        private System.Windows.Forms.ToolStripMenuItem createWalletToolStripMenuItem;
        private System.Windows.Forms.Label labelMainInterfaceTotalBalanceAmountText;
        private System.Windows.Forms.Label labelMainInterfacePendingBalanceAmountText;
        private System.Windows.Forms.Label labelMainInterfaceAvailableBalanceAmountText;
        private System.Windows.Forms.ToolStripMenuItem rescanToolStripMenuItem;
        private ClassCustomPanel panelRecentTransactions;
        private ClassCustomPanel panelInternalRecentTransactions;
        private ClassCustomPanel panelInternalNetworkStats;
        private ClassCustomPanel panelSyncInformationsSeperator;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsInfoSyncText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsCurrentHashrateText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsCurrentDifficultyText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsTitleText;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Button buttonMainInterfaceExportTransactionHistory;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsTotalCoinSpreadText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsTotalFeeCirculatingText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsTotalCoinCirculatingText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsTotalTransactionConfirmedText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsTotalTransactionText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsTotalTransactionMemPoolText;
        private System.Windows.Forms.Label labelMainInterfaceNetworkStatsTotalCoinPendingText;
        private System.Windows.Forms.Button buttonSaveQrCodeReceiveTransactionWalletAddress;
        private ClassCustomPanel panelTransactionHistory;
        private System.Windows.Forms.Button buttonMainInterfaceNextPageTransactionHistory;
        private System.Windows.Forms.Button buttonMainInterfaceBackPageTransactionHistory;
        private System.Windows.Forms.TextBox textBoxMainInterfaceMaxPageTransactionHistory;
        private System.Windows.Forms.TextBox textBoxMainInterfaceCurrentPageTransactionHistory;
        private ClassCustomPanel panelTransactionHistoryColumns;
        private ClassCustomProgressBar progressBarMainInterfaceSyncProgress;
        private System.Windows.Forms.Label labelMainInterfaceSyncProgress;
        private System.Windows.Forms.Label labelSendTransactionAvailableBalanceText;
        private ClassCustomPanel panelSendTransaction;
        private System.Windows.Forms.Label labelSendTransactionWalletAddressTarget;
        private System.Windows.Forms.TextBox textBoxSendTransactionWalletAddressTarget;
        private System.Windows.Forms.Label labelSendTransactionAmountSelected;
        private System.Windows.Forms.TextBox textBoxSendTransactionAmountSelected;
        private System.Windows.Forms.Label labelSendTransactionFeeCalculated;
        private System.Windows.Forms.TextBox textBoxSendTransactionFeeCalculated;
        private System.Windows.Forms.Label labelSendTransactionConfirmationCountTarget;
        private System.Windows.Forms.Button buttonSendTransactionDoProcess;
        private System.Windows.Forms.Button buttonSendTransactionOpenContactList;
        private System.Windows.Forms.Label labelSendTransactionPaymentId;
        private System.Windows.Forms.TextBox textBoxSendTransactionPaymentId;
        private System.Windows.Forms.Label labelSendTransactionConfirmationTimeEstimated;
        private System.Windows.Forms.TextBox textBoxSendTransactionConfirmationsCountTarget;
        private System.Windows.Forms.Label labelSendTransactionAmountToSpend;
        private System.Windows.Forms.TextBox textBoxSendTransactionAmountToSpend;
        private System.Windows.Forms.Label labelSendTransactionTotalAmountSource;
        private System.Windows.Forms.TextBox textBoxSendTransactionTotalAmountSource;
        private System.Windows.Forms.Label labelSendTransactionFeeSizeCost;
        private System.Windows.Forms.TextBox textBoxSendTransactionFeeSizeCost;
        private System.Windows.Forms.Label labelSendTransactionFeeConfirmationCost;
        private System.Windows.Forms.TextBox textBoxSendTransactionFeeConfirmationCost;
        private ClassCustomPanel panelSendTransactionDetails;
        private System.Windows.Forms.Timer timerRefreshTransactionHistory;
        private System.Windows.Forms.Label labelWalletAddressReceiveTransaction;
        private System.Windows.Forms.ToolStripMenuItem importWalletPrivateKeytoolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem languageToolStripMenuItem;
        private System.Windows.Forms.Button buttonMainInterfaceSearchTransactionHistory;
        private System.Windows.Forms.TextBox textBoxTransactionHistorySearch;
    }
}

