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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClassWalletMainInterfaceForm));
            menuStripGeneralWallet = new System.Windows.Forms.MenuStrip();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openWalletToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            closeWalletToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            createWalletToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            importWalletPrivateKeytoolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            rescanToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            languageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            comboBoxListWalletFile = new System.Windows.Forms.ComboBox();
            labelWalletOpened = new System.Windows.Forms.Label();
            labelMainInterfaceSyncProgress = new System.Windows.Forms.Label();
            pictureBoxLogo = new System.Windows.Forms.PictureBox();
            timerRefreshTransactionHistory = new System.Windows.Forms.Timer(components);
            tabControlWallet = new System.Windows.Forms.TabControl();
            tabPageOverview = new System.Windows.Forms.TabPage();
            panelInternalNetworkStats = new ClassCustomPanel();
            labelMainInterfaceNetworkStatsTotalCoinPendingText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsTotalCoinSpreadText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsTotalTransactionText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText = new System.Windows.Forms.Label();
            panelSyncInformationsSeperator = new ClassCustomPanel();
            labelMainInterfaceNetworkStatsInfoSyncText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsCurrentHashrateText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsCurrentDifficultyText = new System.Windows.Forms.Label();
            labelMainInterfaceNetworkStatsTitleText = new System.Windows.Forms.Label();
            panelRecentTransactions = new ClassCustomPanel();
            panelInternalRecentTransactions = new ClassCustomPanel();
            labelMainInterfaceRecentTransaction = new System.Windows.Forms.Label();
            labelMainInterfaceTotalBalanceAmountText = new System.Windows.Forms.Label();
            labelMainInterfacePendingBalanceAmountText = new System.Windows.Forms.Label();
            labelMainInterfaceAvailableBalanceAmountText = new System.Windows.Forms.Label();
            panelSeperatorBalanceLine = new ClassCustomPanel();
            labelMainInterfaceCurrentBalanceText = new System.Windows.Forms.Label();
            tabPageSendTransaction = new System.Windows.Forms.TabPage();
            buttonSendTransactionDoProcess = new System.Windows.Forms.Button();
            panelSendTransaction = new ClassCustomPanel();
            panelSendTransactionDetails = new ClassCustomPanel();
            labelSendTransactionAmountToSpend = new System.Windows.Forms.Label();
            labelSendTransactionFeeSizeCost = new System.Windows.Forms.Label();
            textBoxSendTransactionFeeCalculated = new System.Windows.Forms.TextBox();
            textBoxSendTransactionFeeSizeCost = new System.Windows.Forms.TextBox();
            labelSendTransactionFeeCalculated = new System.Windows.Forms.Label();
            labelSendTransactionFeeConfirmationCost = new System.Windows.Forms.Label();
            textBoxSendTransactionAmountToSpend = new System.Windows.Forms.TextBox();
            textBoxSendTransactionFeeConfirmationCost = new System.Windows.Forms.TextBox();
            textBoxSendTransactionTotalAmountSource = new System.Windows.Forms.TextBox();
            labelSendTransactionTotalAmountSource = new System.Windows.Forms.Label();
            textBoxSendTransactionConfirmationsCountTarget = new System.Windows.Forms.TextBox();
            labelSendTransactionConfirmationTimeEstimated = new System.Windows.Forms.Label();
            labelSendTransactionPaymentId = new System.Windows.Forms.Label();
            textBoxSendTransactionPaymentId = new System.Windows.Forms.TextBox();
            labelSendTransactionAvailableBalanceText = new System.Windows.Forms.Label();
            buttonSendTransactionOpenContactList = new System.Windows.Forms.Button();
            labelSendTransactionConfirmationCountTarget = new System.Windows.Forms.Label();
            labelSendTransactionAmountSelected = new System.Windows.Forms.Label();
            textBoxSendTransactionAmountSelected = new System.Windows.Forms.TextBox();
            labelSendTransactionWalletAddressTarget = new System.Windows.Forms.Label();
            textBoxSendTransactionWalletAddressTarget = new System.Windows.Forms.TextBox();
            tabPageReceiveTransaction = new System.Windows.Forms.TabPage();
            buttonSaveQrCodeReceiveTransactionWalletAddress = new System.Windows.Forms.Button();
            buttonPrintQrCodeReceiveTransactionWalletAddress = new System.Windows.Forms.Button();
            labelWalletReceiveTransactionQrCodeText = new System.Windows.Forms.Label();
            panelQrCodeWalletAddress = new ClassCustomPanel();
            tabPageTransactionHistory = new System.Windows.Forms.TabPage();
            buttonMainInterfaceSearchTransactionHistory = new System.Windows.Forms.Button();
            textBoxTransactionHistorySearch = new System.Windows.Forms.TextBox();
            panelTransactionHistoryColumns = new ClassCustomPanel();
            textBoxMainInterfaceMaxPageTransactionHistory = new System.Windows.Forms.TextBox();
            textBoxMainInterfaceCurrentPageTransactionHistory = new System.Windows.Forms.TextBox();
            buttonMainInterfaceNextPageTransactionHistory = new System.Windows.Forms.Button();
            buttonMainInterfaceBackPageTransactionHistory = new System.Windows.Forms.Button();
            buttonMainInterfaceExportTransactionHistory = new System.Windows.Forms.Button();
            panelTransactionHistory = new ClassCustomPanel();
            tabPageStoreNetwork = new System.Windows.Forms.TabPage();
            panel1 = new System.Windows.Forms.Panel();
            listViewWebNode = new System.Windows.Forms.ListView();
            panelStoreNetwork = new System.Windows.Forms.Panel();
            labelWalletAddressReceiveTransactionTitle = new System.Windows.Forms.Label();
            labelWalletAddressReceiveTransaction = new System.Windows.Forms.Label();
            progressBarMainInterfaceSyncProgress = new ClassCustomProgressBar();
            menuStripGeneralWallet.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).BeginInit();
            tabControlWallet.SuspendLayout();
            tabPageOverview.SuspendLayout();
            panelInternalNetworkStats.SuspendLayout();
            panelRecentTransactions.SuspendLayout();
            tabPageSendTransaction.SuspendLayout();
            panelSendTransaction.SuspendLayout();
            panelSendTransactionDetails.SuspendLayout();
            tabPageReceiveTransaction.SuspendLayout();
            tabPageTransactionHistory.SuspendLayout();
            tabPageStoreNetwork.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStripGeneralWallet
            // 
            menuStripGeneralWallet.BackColor = System.Drawing.Color.FromArgb(67, 83, 105);
            menuStripGeneralWallet.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            menuStripGeneralWallet.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem, settingsToolStripMenuItem, rescanToolStripMenuItem, languageToolStripMenuItem });
            menuStripGeneralWallet.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            menuStripGeneralWallet.Location = new System.Drawing.Point(0, 0);
            menuStripGeneralWallet.MinimumSize = new System.Drawing.Size(1376, 0);
            menuStripGeneralWallet.Name = "menuStripGeneralWallet";
            menuStripGeneralWallet.Padding = new System.Windows.Forms.Padding(7, 2, 7, 2);
            menuStripGeneralWallet.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            menuStripGeneralWallet.Size = new System.Drawing.Size(1376, 24);
            menuStripGeneralWallet.Stretch = false;
            menuStripGeneralWallet.TabIndex = 0;
            menuStripGeneralWallet.Text = "menuStripControl";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { openWalletToolStripMenuItem, closeWalletToolStripMenuItem, createWalletToolStripMenuItem, importWalletPrivateKeytoolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            fileToolStripMenuItem.ForeColor = System.Drawing.Color.Ivory;
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(74, 20);
            fileToolStripMenuItem.Text = "FILE_TEXT";
            // 
            // openWalletToolStripMenuItem
            // 
            openWalletToolStripMenuItem.Name = "openWalletToolStripMenuItem";
            openWalletToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            openWalletToolStripMenuItem.Text = "OPEN_WALLET_TEXT";
            // 
            // closeWalletToolStripMenuItem
            // 
            closeWalletToolStripMenuItem.Name = "closeWalletToolStripMenuItem";
            closeWalletToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            closeWalletToolStripMenuItem.Text = "CLOSE_WALLET_TEXT";
            closeWalletToolStripMenuItem.Click += closeWalletToolStripMenuItem_Click;
            // 
            // createWalletToolStripMenuItem
            // 
            createWalletToolStripMenuItem.Name = "createWalletToolStripMenuItem";
            createWalletToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            createWalletToolStripMenuItem.Text = "CREATE_WALLET_TEXT";
            createWalletToolStripMenuItem.Click += createWalletToolStripMenuItem_Click;
            // 
            // importWalletPrivateKeytoolStripMenuItem
            // 
            importWalletPrivateKeytoolStripMenuItem.Name = "importWalletPrivateKeytoolStripMenuItem";
            importWalletPrivateKeytoolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            importWalletPrivateKeytoolStripMenuItem.Text = "IMPORT_PRIVATE_KEY_TEXT";
            importWalletPrivateKeytoolStripMenuItem.Click += importWalletPrivateKeyToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new System.Drawing.Size(230, 22);
            exitToolStripMenuItem.Text = "EXIT_TEXT";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            settingsToolStripMenuItem.ForeColor = System.Drawing.Color.Ivory;
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new System.Drawing.Size(101, 20);
            settingsToolStripMenuItem.Text = "SETTING_TEXT";
            settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
            // 
            // rescanToolStripMenuItem
            // 
            rescanToolStripMenuItem.ForeColor = System.Drawing.Color.Ivory;
            rescanToolStripMenuItem.Name = "rescanToolStripMenuItem";
            rescanToolStripMenuItem.Size = new System.Drawing.Size(97, 20);
            rescanToolStripMenuItem.Text = "RESCAN_TEXT";
            rescanToolStripMenuItem.Click += rescanToolStripMenuItem_Click;
            // 
            // languageToolStripMenuItem
            // 
            languageToolStripMenuItem.ForeColor = System.Drawing.Color.GhostWhite;
            languageToolStripMenuItem.Name = "languageToolStripMenuItem";
            languageToolStripMenuItem.Size = new System.Drawing.Size(116, 20);
            languageToolStripMenuItem.Text = "LANGUAGE_TEXT";
            // 
            // comboBoxListWalletFile
            // 
            comboBoxListWalletFile.BackColor = System.Drawing.Color.White;
            comboBoxListWalletFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBoxListWalletFile.FormattingEnabled = true;
            comboBoxListWalletFile.Location = new System.Drawing.Point(1234, 68);
            comboBoxListWalletFile.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            comboBoxListWalletFile.Name = "comboBoxListWalletFile";
            comboBoxListWalletFile.Size = new System.Drawing.Size(140, 23);
            comboBoxListWalletFile.TabIndex = 2;
            comboBoxListWalletFile.SelectedIndexChanged += comboBoxListWalletFile_SelectedIndexChanged;
            // 
            // labelWalletOpened
            // 
            labelWalletOpened.AutoSize = true;
            labelWalletOpened.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelWalletOpened.ForeColor = System.Drawing.Color.Ivory;
            labelWalletOpened.Location = new System.Drawing.Point(1119, 50);
            labelWalletOpened.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelWalletOpened.Name = "labelWalletOpened";
            labelWalletOpened.Size = new System.Drawing.Size(254, 15);
            labelWalletOpened.TabIndex = 3;
            labelWalletOpened.Text = "LABEL_WALLET_OPENED_LIST_TEXT";
            // 
            // labelMainInterfaceSyncProgress
            // 
            labelMainInterfaceSyncProgress.AutoSize = true;
            labelMainInterfaceSyncProgress.BackColor = System.Drawing.Color.Transparent;
            labelMainInterfaceSyncProgress.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceSyncProgress.ForeColor = System.Drawing.Color.Ivory;
            labelMainInterfaceSyncProgress.Location = new System.Drawing.Point(516, 732);
            labelMainInterfaceSyncProgress.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceSyncProgress.Name = "labelMainInterfaceSyncProgress";
            labelMainInterfaceSyncProgress.Size = new System.Drawing.Size(302, 15);
            labelMainInterfaceSyncProgress.TabIndex = 6;
            labelMainInterfaceSyncProgress.Text = "LABEL_MAIN_INTERFACE_SYNC_PROGRESS";
            // 
            // pictureBoxLogo
            // 
            pictureBoxLogo.BackColor = System.Drawing.Color.Transparent;
            pictureBoxLogo.BackgroundImage = (System.Drawing.Image)resources.GetObject("pictureBoxLogo.BackgroundImage");
            pictureBoxLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            pictureBoxLogo.Location = new System.Drawing.Point(630, 27);
            pictureBoxLogo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            pictureBoxLogo.Name = "pictureBoxLogo";
            pictureBoxLogo.Size = new System.Drawing.Size(99, 83);
            pictureBoxLogo.TabIndex = 4;
            pictureBoxLogo.TabStop = false;
            // 
            // timerRefreshTransactionHistory
            // 
            timerRefreshTransactionHistory.Enabled = true;
            timerRefreshTransactionHistory.Interval = 10;
            timerRefreshTransactionHistory.Tick += timerRefreshTransactionHistory_Tick;
            // 
            // tabControlWallet
            // 
            tabControlWallet.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            tabControlWallet.Controls.Add(tabPageOverview);
            tabControlWallet.Controls.Add(tabPageSendTransaction);
            tabControlWallet.Controls.Add(tabPageReceiveTransaction);
            tabControlWallet.Controls.Add(tabPageTransactionHistory);
            tabControlWallet.Controls.Add(tabPageStoreNetwork);
            tabControlWallet.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            tabControlWallet.ItemSize = new System.Drawing.Size(128, 30);
            tabControlWallet.Location = new System.Drawing.Point(0, 145);
            tabControlWallet.Margin = new System.Windows.Forms.Padding(0);
            tabControlWallet.Name = "tabControlWallet";
            tabControlWallet.Padding = new System.Drawing.Point(0, 0);
            tabControlWallet.SelectedIndex = 0;
            tabControlWallet.Size = new System.Drawing.Size(1377, 584);
            tabControlWallet.TabIndex = 1;
            // 
            // tabPageOverview
            // 
            tabPageOverview.BackColor = System.Drawing.Color.FromArgb(77, 104, 145);
            tabPageOverview.Controls.Add(panelInternalNetworkStats);
            tabPageOverview.Controls.Add(panelRecentTransactions);
            tabPageOverview.Controls.Add(labelMainInterfaceTotalBalanceAmountText);
            tabPageOverview.Controls.Add(labelMainInterfacePendingBalanceAmountText);
            tabPageOverview.Controls.Add(labelMainInterfaceAvailableBalanceAmountText);
            tabPageOverview.Controls.Add(panelSeperatorBalanceLine);
            tabPageOverview.Controls.Add(labelMainInterfaceCurrentBalanceText);
            tabPageOverview.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            tabPageOverview.Location = new System.Drawing.Point(4, 34);
            tabPageOverview.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPageOverview.Name = "tabPageOverview";
            tabPageOverview.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPageOverview.Size = new System.Drawing.Size(1369, 546);
            tabPageOverview.TabIndex = 0;
            tabPageOverview.Text = "TABPAG_OVERVIEW_TEXT";
            tabPageOverview.Paint += tabPageOverview_Paint;
            // 
            // panelInternalNetworkStats
            // 
            panelInternalNetworkStats.BackColor = System.Drawing.Color.AliceBlue;
            panelInternalNetworkStats.BorderColor = System.Drawing.Color.DarkGray;
            panelInternalNetworkStats.BorderSize = 1F;
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsTotalCoinPendingText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsTotalCoinSpreadText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsTotalFeeCirculatingText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsTotalCoinCirculatingText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsTotalTransactionConfirmedText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsTotalTransactionText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsTotalTransactionMemPoolText);
            panelInternalNetworkStats.Controls.Add(panelSyncInformationsSeperator);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsInfoSyncText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsCurrentHashrateText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsCurrentDifficultyText);
            panelInternalNetworkStats.Controls.Add(labelMainInterfaceNetworkStatsTitleText);
            panelInternalNetworkStats.Location = new System.Drawing.Point(9, 128);
            panelInternalNetworkStats.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panelInternalNetworkStats.Name = "panelInternalNetworkStats";
            panelInternalNetworkStats.Radius = 10;
            panelInternalNetworkStats.Size = new System.Drawing.Size(830, 402);
            panelInternalNetworkStats.TabIndex = 11;
            // 
            // labelMainInterfaceNetworkStatsTotalCoinPendingText
            // 
            labelMainInterfaceNetworkStatsTotalCoinPendingText.AutoSize = true;
            labelMainInterfaceNetworkStatsTotalCoinPendingText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsTotalCoinPendingText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalCoinPendingText.Location = new System.Drawing.Point(5, 317);
            labelMainInterfaceNetworkStatsTotalCoinPendingText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsTotalCoinPendingText.Name = "labelMainInterfaceNetworkStatsTotalCoinPendingText";
            labelMainInterfaceNetworkStatsTotalCoinPendingText.Size = new System.Drawing.Size(470, 16);
            labelMainInterfaceNetworkStatsTotalCoinPendingText.TabIndex = 25;
            labelMainInterfaceNetworkStatsTotalCoinPendingText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_PENDING_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalCoinSpreadText
            // 
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.AutoSize = true;
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.Location = new System.Drawing.Point(5, 366);
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.Name = "labelMainInterfaceNetworkStatsTotalCoinSpreadText";
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.Size = new System.Drawing.Size(466, 16);
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.TabIndex = 24;
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_SPREAD_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalFeeCirculatingText
            // 
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.AutoSize = true;
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Location = new System.Drawing.Point(5, 342);
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Name = "labelMainInterfaceNetworkStatsTotalFeeCirculatingText";
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Size = new System.Drawing.Size(490, 16);
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.TabIndex = 23;
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_FEE_CIRCULATING_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalCoinCirculatingText
            // 
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.AutoSize = true;
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Location = new System.Drawing.Point(5, 292);
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Name = "labelMainInterfaceNetworkStatsTotalCoinCirculatingText";
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Size = new System.Drawing.Size(495, 16);
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.TabIndex = 22;
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_CIRCULATING_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText
            // 
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.AutoSize = true;
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Location = new System.Drawing.Point(5, 269);
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Name = "labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText";
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Size = new System.Drawing.Size(566, 16);
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.TabIndex = 21;
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_BLOCK_UNLOCKED_CHECKED_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalTransactionConfirmedText
            // 
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.AutoSize = true;
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Location = new System.Drawing.Point(5, 245);
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Name = "labelMainInterfaceNetworkStatsTotalTransactionConfirmedText";
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Size = new System.Drawing.Size(548, 16);
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.TabIndex = 20;
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_CONFIRMED_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalTransactionText
            // 
            labelMainInterfaceNetworkStatsTotalTransactionText.AutoSize = true;
            labelMainInterfaceNetworkStatsTotalTransactionText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsTotalTransactionText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalTransactionText.Location = new System.Drawing.Point(5, 220);
            labelMainInterfaceNetworkStatsTotalTransactionText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsTotalTransactionText.Name = "labelMainInterfaceNetworkStatsTotalTransactionText";
            labelMainInterfaceNetworkStatsTotalTransactionText.Size = new System.Drawing.Size(464, 16);
            labelMainInterfaceNetworkStatsTotalTransactionText.TabIndex = 19;
            labelMainInterfaceNetworkStatsTotalTransactionText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTotalTransactionMemPoolText
            // 
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.AutoSize = true;
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Location = new System.Drawing.Point(5, 197);
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Name = "labelMainInterfaceNetworkStatsTotalTransactionMemPoolText";
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Size = new System.Drawing.Size(538, 16);
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.TabIndex = 18;
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_MEMPOOL_TEXT";
            // 
            // panelSyncInformationsSeperator
            // 
            panelSyncInformationsSeperator.BackColor = System.Drawing.Color.Black;
            panelSyncInformationsSeperator.BorderColor = System.Drawing.Color.White;
            panelSyncInformationsSeperator.BorderSize = 3F;
            panelSyncInformationsSeperator.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panelSyncInformationsSeperator.Location = new System.Drawing.Point(114, 162);
            panelSyncInformationsSeperator.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panelSyncInformationsSeperator.Name = "panelSyncInformationsSeperator";
            panelSyncInformationsSeperator.Radius = 2;
            panelSyncInformationsSeperator.Size = new System.Drawing.Size(583, 2);
            panelSyncInformationsSeperator.TabIndex = 17;
            // 
            // labelMainInterfaceNetworkStatsInfoSyncText
            // 
            labelMainInterfaceNetworkStatsInfoSyncText.AutoSize = true;
            labelMainInterfaceNetworkStatsInfoSyncText.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsInfoSyncText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsInfoSyncText.Location = new System.Drawing.Point(124, 167);
            labelMainInterfaceNetworkStatsInfoSyncText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsInfoSyncText.Name = "labelMainInterfaceNetworkStatsInfoSyncText";
            labelMainInterfaceNetworkStatsInfoSyncText.Size = new System.Drawing.Size(476, 18);
            labelMainInterfaceNetworkStatsInfoSyncText.TabIndex = 16;
            labelMainInterfaceNetworkStatsInfoSyncText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_INFO_SYNC_TEXT";
            // 
            // labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText
            // 
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.AutoSize = true;
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Location = new System.Drawing.Point(5, 130);
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Name = "labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText";
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Size = new System.Drawing.Size(550, 16);
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.TabIndex = 15;
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_MINING_LUCK_PERCENT_TEXT";
            // 
            // labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText
            // 
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.AutoSize = true;
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Location = new System.Drawing.Point(5, 107);
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Name = "labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText";
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Size = new System.Drawing.Size(537, 16);
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.TabIndex = 14;
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_MINING_LUCK_STATUS_TEXT";
            // 
            // labelMainInterfaceNetworkStatsCurrentHashrateText
            // 
            labelMainInterfaceNetworkStatsCurrentHashrateText.AutoSize = true;
            labelMainInterfaceNetworkStatsCurrentHashrateText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsCurrentHashrateText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsCurrentHashrateText.Location = new System.Drawing.Point(5, 84);
            labelMainInterfaceNetworkStatsCurrentHashrateText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsCurrentHashrateText.Name = "labelMainInterfaceNetworkStatsCurrentHashrateText";
            labelMainInterfaceNetworkStatsCurrentHashrateText.Size = new System.Drawing.Size(465, 16);
            labelMainInterfaceNetworkStatsCurrentHashrateText.TabIndex = 13;
            labelMainInterfaceNetworkStatsCurrentHashrateText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_HASHRATE_TEXT";
            // 
            // labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText
            // 
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.AutoSize = true;
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Location = new System.Drawing.Point(5, 38);
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Name = "labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText";
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Size = new System.Drawing.Size(537, 16);
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.TabIndex = 12;
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_BLOCK_HEIGHT_SYNC_TEXT";
            // 
            // labelMainInterfaceNetworkStatsCurrentDifficultyText
            // 
            labelMainInterfaceNetworkStatsCurrentDifficultyText.AutoSize = true;
            labelMainInterfaceNetworkStatsCurrentDifficultyText.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsCurrentDifficultyText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsCurrentDifficultyText.Location = new System.Drawing.Point(5, 61);
            labelMainInterfaceNetworkStatsCurrentDifficultyText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsCurrentDifficultyText.Name = "labelMainInterfaceNetworkStatsCurrentDifficultyText";
            labelMainInterfaceNetworkStatsCurrentDifficultyText.Size = new System.Drawing.Size(467, 16);
            labelMainInterfaceNetworkStatsCurrentDifficultyText.TabIndex = 11;
            labelMainInterfaceNetworkStatsCurrentDifficultyText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_DIFFICULTY_TEXT";
            // 
            // labelMainInterfaceNetworkStatsTitleText
            // 
            labelMainInterfaceNetworkStatsTitleText.AutoSize = true;
            labelMainInterfaceNetworkStatsTitleText.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceNetworkStatsTitleText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTitleText.Location = new System.Drawing.Point(149, 9);
            labelMainInterfaceNetworkStatsTitleText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceNetworkStatsTitleText.Name = "labelMainInterfaceNetworkStatsTitleText";
            labelMainInterfaceNetworkStatsTitleText.Size = new System.Drawing.Size(433, 18);
            labelMainInterfaceNetworkStatsTitleText.TabIndex = 10;
            labelMainInterfaceNetworkStatsTitleText.Text = "LABEL_MAIN_INTERFACE_NETWORK_STATS_TITLE_TEXT";
            // 
            // panelRecentTransactions
            // 
            panelRecentTransactions.BackColor = System.Drawing.Color.FromArgb(216, 227, 240);
            panelRecentTransactions.BorderColor = System.Drawing.Color.DarkGray;
            panelRecentTransactions.BorderSize = 1F;
            panelRecentTransactions.Controls.Add(panelInternalRecentTransactions);
            panelRecentTransactions.Controls.Add(labelMainInterfaceRecentTransaction);
            panelRecentTransactions.Location = new System.Drawing.Point(847, 23);
            panelRecentTransactions.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panelRecentTransactions.Name = "panelRecentTransactions";
            panelRecentTransactions.Radius = 10;
            panelRecentTransactions.Size = new System.Drawing.Size(511, 507);
            panelRecentTransactions.TabIndex = 10;
            panelRecentTransactions.Paint += panelRecentTransactions_Paint;
            // 
            // panelInternalRecentTransactions
            // 
            panelInternalRecentTransactions.BackColor = System.Drawing.Color.FromArgb(245, 249, 252);
            panelInternalRecentTransactions.BorderColor = System.Drawing.Color.FromArgb(91, 106, 128);
            panelInternalRecentTransactions.BorderSize = 2F;
            panelInternalRecentTransactions.Location = new System.Drawing.Point(22, 85);
            panelInternalRecentTransactions.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panelInternalRecentTransactions.Name = "panelInternalRecentTransactions";
            panelInternalRecentTransactions.Radius = 10;
            panelInternalRecentTransactions.Size = new System.Drawing.Size(467, 375);
            panelInternalRecentTransactions.TabIndex = 2;
            panelInternalRecentTransactions.Click += panelInternalRecentTransactions_Click;
            panelInternalRecentTransactions.Paint += panelInternalRecentTransactions_Paint;
            panelInternalRecentTransactions.MouseEnter += panelInternalRecentTransactions_MouseEnter;
            panelInternalRecentTransactions.MouseLeave += panelInternalRecentTransactions_MouseLeave;
            panelInternalRecentTransactions.MouseHover += panelInternalRecentTransactions_MouseHover;
            panelInternalRecentTransactions.MouseMove += panelInternalRecentTransactions_MouseMove;
            // 
            // labelMainInterfaceRecentTransaction
            // 
            labelMainInterfaceRecentTransaction.AutoSize = true;
            labelMainInterfaceRecentTransaction.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceRecentTransaction.Location = new System.Drawing.Point(8, 10);
            labelMainInterfaceRecentTransaction.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceRecentTransaction.Name = "labelMainInterfaceRecentTransaction";
            labelMainInterfaceRecentTransaction.Size = new System.Drawing.Size(424, 16);
            labelMainInterfaceRecentTransaction.TabIndex = 1;
            labelMainInterfaceRecentTransaction.Text = "LABEL_MAIN_INTERFACE_RECENT_TRANSACTION_TEXT";
            labelMainInterfaceRecentTransaction.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // labelMainInterfaceTotalBalanceAmountText
            // 
            labelMainInterfaceTotalBalanceAmountText.AutoSize = true;
            labelMainInterfaceTotalBalanceAmountText.BackColor = System.Drawing.Color.Transparent;
            labelMainInterfaceTotalBalanceAmountText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceTotalBalanceAmountText.ForeColor = System.Drawing.Color.Ivory;
            labelMainInterfaceTotalBalanceAmountText.Location = new System.Drawing.Point(27, 100);
            labelMainInterfaceTotalBalanceAmountText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceTotalBalanceAmountText.Name = "labelMainInterfaceTotalBalanceAmountText";
            labelMainInterfaceTotalBalanceAmountText.Size = new System.Drawing.Size(401, 15);
            labelMainInterfaceTotalBalanceAmountText.TabIndex = 9;
            labelMainInterfaceTotalBalanceAmountText.Text = "LABEL_MAIN_INTERFACE_TOTAL_BALANCE_AMOUNT_TEXT";
            // 
            // labelMainInterfacePendingBalanceAmountText
            // 
            labelMainInterfacePendingBalanceAmountText.AutoSize = true;
            labelMainInterfacePendingBalanceAmountText.BackColor = System.Drawing.Color.Transparent;
            labelMainInterfacePendingBalanceAmountText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelMainInterfacePendingBalanceAmountText.ForeColor = System.Drawing.Color.FromArgb(247, 229, 72);
            labelMainInterfacePendingBalanceAmountText.Location = new System.Drawing.Point(27, 67);
            labelMainInterfacePendingBalanceAmountText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfacePendingBalanceAmountText.Name = "labelMainInterfacePendingBalanceAmountText";
            labelMainInterfacePendingBalanceAmountText.Size = new System.Drawing.Size(421, 15);
            labelMainInterfacePendingBalanceAmountText.TabIndex = 8;
            labelMainInterfacePendingBalanceAmountText.Text = "LABEL_MAIN_INTERFACE_PENDING_BALANCE_AMOUNT_TEXT";
            // 
            // labelMainInterfaceAvailableBalanceAmountText
            // 
            labelMainInterfaceAvailableBalanceAmountText.AutoSize = true;
            labelMainInterfaceAvailableBalanceAmountText.BackColor = System.Drawing.Color.Transparent;
            labelMainInterfaceAvailableBalanceAmountText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceAvailableBalanceAmountText.ForeColor = System.Drawing.Color.LimeGreen;
            labelMainInterfaceAvailableBalanceAmountText.Location = new System.Drawing.Point(27, 40);
            labelMainInterfaceAvailableBalanceAmountText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceAvailableBalanceAmountText.Name = "labelMainInterfaceAvailableBalanceAmountText";
            labelMainInterfaceAvailableBalanceAmountText.Size = new System.Drawing.Size(429, 15);
            labelMainInterfaceAvailableBalanceAmountText.TabIndex = 7;
            labelMainInterfaceAvailableBalanceAmountText.Text = "LABEL_MAIN_INTERFACE_AVAILABLE_BALANCE_AMOUNT_TEXT";
            // 
            // panelSeperatorBalanceLine
            // 
            panelSeperatorBalanceLine.BackColor = System.Drawing.Color.Black;
            panelSeperatorBalanceLine.BorderColor = System.Drawing.Color.White;
            panelSeperatorBalanceLine.BorderSize = 3F;
            panelSeperatorBalanceLine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panelSeperatorBalanceLine.Location = new System.Drawing.Point(30, 90);
            panelSeperatorBalanceLine.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panelSeperatorBalanceLine.Name = "panelSeperatorBalanceLine";
            panelSeperatorBalanceLine.Radius = 2;
            panelSeperatorBalanceLine.Size = new System.Drawing.Size(279, 2);
            panelSeperatorBalanceLine.TabIndex = 6;
            // 
            // labelMainInterfaceCurrentBalanceText
            // 
            labelMainInterfaceCurrentBalanceText.AutoSize = true;
            labelMainInterfaceCurrentBalanceText.BackColor = System.Drawing.Color.Transparent;
            labelMainInterfaceCurrentBalanceText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelMainInterfaceCurrentBalanceText.ForeColor = System.Drawing.Color.Ivory;
            labelMainInterfaceCurrentBalanceText.Location = new System.Drawing.Point(27, 16);
            labelMainInterfaceCurrentBalanceText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelMainInterfaceCurrentBalanceText.Name = "labelMainInterfaceCurrentBalanceText";
            labelMainInterfaceCurrentBalanceText.Size = new System.Drawing.Size(397, 16);
            labelMainInterfaceCurrentBalanceText.TabIndex = 4;
            labelMainInterfaceCurrentBalanceText.Text = "LABEL_MAIN_INTERFACE_CURRENT_BALANCE_TEXT";
            // 
            // tabPageSendTransaction
            // 
            tabPageSendTransaction.BackColor = System.Drawing.Color.FromArgb(77, 104, 145);
            tabPageSendTransaction.Controls.Add(buttonSendTransactionDoProcess);
            tabPageSendTransaction.Controls.Add(panelSendTransaction);
            tabPageSendTransaction.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            tabPageSendTransaction.Location = new System.Drawing.Point(4, 34);
            tabPageSendTransaction.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPageSendTransaction.Name = "tabPageSendTransaction";
            tabPageSendTransaction.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPageSendTransaction.Size = new System.Drawing.Size(1369, 546);
            tabPageSendTransaction.TabIndex = 1;
            tabPageSendTransaction.Text = "TABPAGE_SEND_TRANSACTION_TEXT";
            tabPageSendTransaction.Paint += tabPageSendTransaction_Paint;
            // 
            // buttonSendTransactionDoProcess
            // 
            buttonSendTransactionDoProcess.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonSendTransactionDoProcess.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            buttonSendTransactionDoProcess.ForeColor = System.Drawing.Color.Black;
            buttonSendTransactionDoProcess.Location = new System.Drawing.Point(447, 497);
            buttonSendTransactionDoProcess.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonSendTransactionDoProcess.Name = "buttonSendTransactionDoProcess";
            buttonSendTransactionDoProcess.Size = new System.Drawing.Size(483, 36);
            buttonSendTransactionDoProcess.TabIndex = 5;
            buttonSendTransactionDoProcess.Text = "BUTTON_SEND_TRANSACTION_DO_PROCESS_TEXT";
            buttonSendTransactionDoProcess.UseVisualStyleBackColor = false;
            buttonSendTransactionDoProcess.Click += buttonSendTransactionDoProcess_Click;
            // 
            // panelSendTransaction
            // 
            panelSendTransaction.BackColor = System.Drawing.Color.AliceBlue;
            panelSendTransaction.BorderColor = System.Drawing.Color.Ivory;
            panelSendTransaction.BorderSize = 2F;
            panelSendTransaction.Controls.Add(panelSendTransactionDetails);
            panelSendTransaction.Controls.Add(textBoxSendTransactionConfirmationsCountTarget);
            panelSendTransaction.Controls.Add(labelSendTransactionConfirmationTimeEstimated);
            panelSendTransaction.Controls.Add(labelSendTransactionPaymentId);
            panelSendTransaction.Controls.Add(textBoxSendTransactionPaymentId);
            panelSendTransaction.Controls.Add(labelSendTransactionAvailableBalanceText);
            panelSendTransaction.Controls.Add(buttonSendTransactionOpenContactList);
            panelSendTransaction.Controls.Add(labelSendTransactionConfirmationCountTarget);
            panelSendTransaction.Controls.Add(labelSendTransactionAmountSelected);
            panelSendTransaction.Controls.Add(textBoxSendTransactionAmountSelected);
            panelSendTransaction.Controls.Add(labelSendTransactionWalletAddressTarget);
            panelSendTransaction.Controls.Add(textBoxSendTransactionWalletAddressTarget);
            panelSendTransaction.Location = new System.Drawing.Point(9, 15);
            panelSendTransaction.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panelSendTransaction.Name = "panelSendTransaction";
            panelSendTransaction.Radius = 10;
            panelSendTransaction.Size = new System.Drawing.Size(1349, 477);
            panelSendTransaction.TabIndex = 0;
            // 
            // panelSendTransactionDetails
            // 
            panelSendTransactionDetails.BackColor = System.Drawing.Color.FromArgb(70, 90, 120);
            panelSendTransactionDetails.BorderColor = System.Drawing.Color.Ivory;
            panelSendTransactionDetails.BorderSize = 1F;
            panelSendTransactionDetails.Controls.Add(labelSendTransactionAmountToSpend);
            panelSendTransactionDetails.Controls.Add(labelSendTransactionFeeSizeCost);
            panelSendTransactionDetails.Controls.Add(textBoxSendTransactionFeeCalculated);
            panelSendTransactionDetails.Controls.Add(textBoxSendTransactionFeeSizeCost);
            panelSendTransactionDetails.Controls.Add(labelSendTransactionFeeCalculated);
            panelSendTransactionDetails.Controls.Add(labelSendTransactionFeeConfirmationCost);
            panelSendTransactionDetails.Controls.Add(textBoxSendTransactionAmountToSpend);
            panelSendTransactionDetails.Controls.Add(textBoxSendTransactionFeeConfirmationCost);
            panelSendTransactionDetails.Controls.Add(textBoxSendTransactionTotalAmountSource);
            panelSendTransactionDetails.Controls.Add(labelSendTransactionTotalAmountSource);
            panelSendTransactionDetails.Location = new System.Drawing.Point(752, 102);
            panelSendTransactionDetails.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panelSendTransactionDetails.Name = "panelSendTransactionDetails";
            panelSendTransactionDetails.Radius = 10;
            panelSendTransactionDetails.Size = new System.Drawing.Size(593, 372);
            panelSendTransactionDetails.TabIndex = 23;
            panelSendTransactionDetails.Paint += panelSendTransactionDetails_Paint;
            // 
            // labelSendTransactionAmountToSpend
            // 
            labelSendTransactionAmountToSpend.AutoSize = true;
            labelSendTransactionAmountToSpend.ForeColor = System.Drawing.Color.Ivory;
            labelSendTransactionAmountToSpend.Location = new System.Drawing.Point(41, 27);
            labelSendTransactionAmountToSpend.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionAmountToSpend.Name = "labelSendTransactionAmountToSpend";
            labelSendTransactionAmountToSpend.Size = new System.Drawing.Size(428, 16);
            labelSendTransactionAmountToSpend.TabIndex = 16;
            labelSendTransactionAmountToSpend.Text = "LABEL_SEND_TRANSACTION_AMOUNT_TO_SPEND_TEXT";
            // 
            // labelSendTransactionFeeSizeCost
            // 
            labelSendTransactionFeeSizeCost.AutoSize = true;
            labelSendTransactionFeeSizeCost.ForeColor = System.Drawing.Color.Ivory;
            labelSendTransactionFeeSizeCost.Location = new System.Drawing.Point(41, 163);
            labelSendTransactionFeeSizeCost.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionFeeSizeCost.Name = "labelSendTransactionFeeSizeCost";
            labelSendTransactionFeeSizeCost.Size = new System.Drawing.Size(393, 16);
            labelSendTransactionFeeSizeCost.TabIndex = 22;
            labelSendTransactionFeeSizeCost.Text = "LABEL_SEND_TRANSACTION_FEE_SIZE_COST_TEXT";
            // 
            // textBoxSendTransactionFeeCalculated
            // 
            textBoxSendTransactionFeeCalculated.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            textBoxSendTransactionFeeCalculated.Location = new System.Drawing.Point(44, 317);
            textBoxSendTransactionFeeCalculated.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxSendTransactionFeeCalculated.Multiline = true;
            textBoxSendTransactionFeeCalculated.Name = "textBoxSendTransactionFeeCalculated";
            textBoxSendTransactionFeeCalculated.ReadOnly = true;
            textBoxSendTransactionFeeCalculated.Size = new System.Drawing.Size(474, 25);
            textBoxSendTransactionFeeCalculated.TabIndex = 4;
            // 
            // textBoxSendTransactionFeeSizeCost
            // 
            textBoxSendTransactionFeeSizeCost.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            textBoxSendTransactionFeeSizeCost.Location = new System.Drawing.Point(44, 185);
            textBoxSendTransactionFeeSizeCost.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxSendTransactionFeeSizeCost.Multiline = true;
            textBoxSendTransactionFeeSizeCost.Name = "textBoxSendTransactionFeeSizeCost";
            textBoxSendTransactionFeeSizeCost.ReadOnly = true;
            textBoxSendTransactionFeeSizeCost.Size = new System.Drawing.Size(474, 25);
            textBoxSendTransactionFeeSizeCost.TabIndex = 21;
            // 
            // labelSendTransactionFeeCalculated
            // 
            labelSendTransactionFeeCalculated.AutoSize = true;
            labelSendTransactionFeeCalculated.ForeColor = System.Drawing.Color.Ivory;
            labelSendTransactionFeeCalculated.Location = new System.Drawing.Point(41, 295);
            labelSendTransactionFeeCalculated.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionFeeCalculated.Name = "labelSendTransactionFeeCalculated";
            labelSendTransactionFeeCalculated.Size = new System.Drawing.Size(409, 16);
            labelSendTransactionFeeCalculated.TabIndex = 5;
            labelSendTransactionFeeCalculated.Text = "LABEL_SEND_TRANSACTION_FEE_CALCULATED_TEXT";
            // 
            // labelSendTransactionFeeConfirmationCost
            // 
            labelSendTransactionFeeConfirmationCost.AutoSize = true;
            labelSendTransactionFeeConfirmationCost.ForeColor = System.Drawing.Color.Ivory;
            labelSendTransactionFeeConfirmationCost.Location = new System.Drawing.Point(41, 228);
            labelSendTransactionFeeConfirmationCost.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionFeeConfirmationCost.Name = "labelSendTransactionFeeConfirmationCost";
            labelSendTransactionFeeConfirmationCost.Size = new System.Drawing.Size(474, 16);
            labelSendTransactionFeeConfirmationCost.TabIndex = 20;
            labelSendTransactionFeeConfirmationCost.Text = "LABEL_SEND_TRANSACTION_FEE_CONFIRMATION_COST_TEXT";
            // 
            // textBoxSendTransactionAmountToSpend
            // 
            textBoxSendTransactionAmountToSpend.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            textBoxSendTransactionAmountToSpend.Location = new System.Drawing.Point(44, 48);
            textBoxSendTransactionAmountToSpend.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxSendTransactionAmountToSpend.Multiline = true;
            textBoxSendTransactionAmountToSpend.Name = "textBoxSendTransactionAmountToSpend";
            textBoxSendTransactionAmountToSpend.ReadOnly = true;
            textBoxSendTransactionAmountToSpend.Size = new System.Drawing.Size(474, 25);
            textBoxSendTransactionAmountToSpend.TabIndex = 15;
            // 
            // textBoxSendTransactionFeeConfirmationCost
            // 
            textBoxSendTransactionFeeConfirmationCost.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            textBoxSendTransactionFeeConfirmationCost.Location = new System.Drawing.Point(44, 250);
            textBoxSendTransactionFeeConfirmationCost.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxSendTransactionFeeConfirmationCost.Multiline = true;
            textBoxSendTransactionFeeConfirmationCost.Name = "textBoxSendTransactionFeeConfirmationCost";
            textBoxSendTransactionFeeConfirmationCost.ReadOnly = true;
            textBoxSendTransactionFeeConfirmationCost.Size = new System.Drawing.Size(474, 25);
            textBoxSendTransactionFeeConfirmationCost.TabIndex = 19;
            // 
            // textBoxSendTransactionTotalAmountSource
            // 
            textBoxSendTransactionTotalAmountSource.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            textBoxSendTransactionTotalAmountSource.Location = new System.Drawing.Point(44, 118);
            textBoxSendTransactionTotalAmountSource.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxSendTransactionTotalAmountSource.Multiline = true;
            textBoxSendTransactionTotalAmountSource.Name = "textBoxSendTransactionTotalAmountSource";
            textBoxSendTransactionTotalAmountSource.ReadOnly = true;
            textBoxSendTransactionTotalAmountSource.Size = new System.Drawing.Size(474, 25);
            textBoxSendTransactionTotalAmountSource.TabIndex = 17;
            // 
            // labelSendTransactionTotalAmountSource
            // 
            labelSendTransactionTotalAmountSource.AutoSize = true;
            labelSendTransactionTotalAmountSource.ForeColor = System.Drawing.Color.Ivory;
            labelSendTransactionTotalAmountSource.Location = new System.Drawing.Point(41, 96);
            labelSendTransactionTotalAmountSource.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionTotalAmountSource.Name = "labelSendTransactionTotalAmountSource";
            labelSendTransactionTotalAmountSource.Size = new System.Drawing.Size(467, 16);
            labelSendTransactionTotalAmountSource.TabIndex = 18;
            labelSendTransactionTotalAmountSource.Text = "LABEL_SEND_TRANSACTION_TOTAL_AMOUNT_SOURCE_TEXT";
            // 
            // textBoxSendTransactionConfirmationsCountTarget
            // 
            textBoxSendTransactionConfirmationsCountTarget.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            textBoxSendTransactionConfirmationsCountTarget.Location = new System.Drawing.Point(19, 267);
            textBoxSendTransactionConfirmationsCountTarget.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxSendTransactionConfirmationsCountTarget.Multiline = true;
            textBoxSendTransactionConfirmationsCountTarget.Name = "textBoxSendTransactionConfirmationsCountTarget";
            textBoxSendTransactionConfirmationsCountTarget.Size = new System.Drawing.Size(255, 25);
            textBoxSendTransactionConfirmationsCountTarget.TabIndex = 14;
            textBoxSendTransactionConfirmationsCountTarget.TextChanged += textBoxSendTransactionConfirmationsCountTarget_TextChanged;
            textBoxSendTransactionConfirmationsCountTarget.KeyDown += textBoxSendTransactionConfirmationsCountTarget_KeyDown;
            // 
            // labelSendTransactionConfirmationTimeEstimated
            // 
            labelSendTransactionConfirmationTimeEstimated.AutoSize = true;
            labelSendTransactionConfirmationTimeEstimated.Location = new System.Drawing.Point(15, 301);
            labelSendTransactionConfirmationTimeEstimated.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionConfirmationTimeEstimated.Name = "labelSendTransactionConfirmationTimeEstimated";
            labelSendTransactionConfirmationTimeEstimated.Size = new System.Drawing.Size(527, 16);
            labelSendTransactionConfirmationTimeEstimated.TabIndex = 13;
            labelSendTransactionConfirmationTimeEstimated.Text = "LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_TEXT";
            // 
            // labelSendTransactionPaymentId
            // 
            labelSendTransactionPaymentId.AutoSize = true;
            labelSendTransactionPaymentId.Location = new System.Drawing.Point(15, 363);
            labelSendTransactionPaymentId.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionPaymentId.Name = "labelSendTransactionPaymentId";
            labelSendTransactionPaymentId.Size = new System.Drawing.Size(370, 16);
            labelSendTransactionPaymentId.TabIndex = 12;
            labelSendTransactionPaymentId.Text = "LABEL_SEND_TRANSACTION_PAYMENT_ID_TEXT";
            // 
            // textBoxSendTransactionPaymentId
            // 
            textBoxSendTransactionPaymentId.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            textBoxSendTransactionPaymentId.Location = new System.Drawing.Point(19, 385);
            textBoxSendTransactionPaymentId.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxSendTransactionPaymentId.Multiline = true;
            textBoxSendTransactionPaymentId.Name = "textBoxSendTransactionPaymentId";
            textBoxSendTransactionPaymentId.Size = new System.Drawing.Size(446, 25);
            textBoxSendTransactionPaymentId.TabIndex = 11;
            textBoxSendTransactionPaymentId.Text = "0";
            textBoxSendTransactionPaymentId.KeyDown += textBoxSendTransactionPaymentId_KeyDown;
            // 
            // labelSendTransactionAvailableBalanceText
            // 
            labelSendTransactionAvailableBalanceText.AutoSize = true;
            labelSendTransactionAvailableBalanceText.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelSendTransactionAvailableBalanceText.Location = new System.Drawing.Point(15, 13);
            labelSendTransactionAvailableBalanceText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionAvailableBalanceText.Name = "labelSendTransactionAvailableBalanceText";
            labelSendTransactionAvailableBalanceText.Size = new System.Drawing.Size(466, 18);
            labelSendTransactionAvailableBalanceText.TabIndex = 1;
            labelSendTransactionAvailableBalanceText.Text = "LABEL_SEND_TRANSACTION_AVAILABLE_BALANCE_TEXT";
            // 
            // buttonSendTransactionOpenContactList
            // 
            buttonSendTransactionOpenContactList.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonSendTransactionOpenContactList.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            buttonSendTransactionOpenContactList.ForeColor = System.Drawing.Color.Black;
            buttonSendTransactionOpenContactList.Location = new System.Drawing.Point(16, 102);
            buttonSendTransactionOpenContactList.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonSendTransactionOpenContactList.Name = "buttonSendTransactionOpenContactList";
            buttonSendTransactionOpenContactList.Size = new System.Drawing.Size(558, 30);
            buttonSendTransactionOpenContactList.TabIndex = 10;
            buttonSendTransactionOpenContactList.Text = "BUTTON_SEND_TRANSACTION_OPEN_CONTACT_LIST_TEXT";
            buttonSendTransactionOpenContactList.UseVisualStyleBackColor = false;
            // 
            // labelSendTransactionConfirmationCountTarget
            // 
            labelSendTransactionConfirmationCountTarget.AutoSize = true;
            labelSendTransactionConfirmationCountTarget.Location = new System.Drawing.Point(15, 245);
            labelSendTransactionConfirmationCountTarget.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionConfirmationCountTarget.Name = "labelSendTransactionConfirmationCountTarget";
            labelSendTransactionConfirmationCountTarget.Size = new System.Drawing.Size(519, 16);
            labelSendTransactionConfirmationCountTarget.TabIndex = 7;
            labelSendTransactionConfirmationCountTarget.Text = "LABEL_SEND_TRANSACTION_CONFIRMATION_COUNT_TARGET_TEXT";
            // 
            // labelSendTransactionAmountSelected
            // 
            labelSendTransactionAmountSelected.AutoSize = true;
            labelSendTransactionAmountSelected.Location = new System.Drawing.Point(13, 168);
            labelSendTransactionAmountSelected.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionAmountSelected.Name = "labelSendTransactionAmountSelected";
            labelSendTransactionAmountSelected.Size = new System.Drawing.Size(426, 16);
            labelSendTransactionAmountSelected.TabIndex = 3;
            labelSendTransactionAmountSelected.Text = "LABEL_SEND_TRANSACTION_AMOUNT_SELECTED_TEXT";
            // 
            // textBoxSendTransactionAmountSelected
            // 
            textBoxSendTransactionAmountSelected.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            textBoxSendTransactionAmountSelected.Location = new System.Drawing.Point(16, 190);
            textBoxSendTransactionAmountSelected.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxSendTransactionAmountSelected.Multiline = true;
            textBoxSendTransactionAmountSelected.Name = "textBoxSendTransactionAmountSelected";
            textBoxSendTransactionAmountSelected.Size = new System.Drawing.Size(446, 25);
            textBoxSendTransactionAmountSelected.TabIndex = 2;
            textBoxSendTransactionAmountSelected.KeyDown += textBoxSendTransactionAmountSelected_KeyDown;
            textBoxSendTransactionAmountSelected.KeyUp += textBoxSendTransactionAmountSelected_KeyUp;
            // 
            // labelSendTransactionWalletAddressTarget
            // 
            labelSendTransactionWalletAddressTarget.AutoSize = true;
            labelSendTransactionWalletAddressTarget.Location = new System.Drawing.Point(15, 47);
            labelSendTransactionWalletAddressTarget.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelSendTransactionWalletAddressTarget.Name = "labelSendTransactionWalletAddressTarget";
            labelSendTransactionWalletAddressTarget.Size = new System.Drawing.Size(485, 16);
            labelSendTransactionWalletAddressTarget.TabIndex = 1;
            labelSendTransactionWalletAddressTarget.Text = "LABEL_SEND_TRANSACTION_WALLET_ADDRESS_TARGET_TEXT";
            // 
            // textBoxSendTransactionWalletAddressTarget
            // 
            textBoxSendTransactionWalletAddressTarget.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            textBoxSendTransactionWalletAddressTarget.Location = new System.Drawing.Point(16, 69);
            textBoxSendTransactionWalletAddressTarget.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxSendTransactionWalletAddressTarget.Multiline = true;
            textBoxSendTransactionWalletAddressTarget.Name = "textBoxSendTransactionWalletAddressTarget";
            textBoxSendTransactionWalletAddressTarget.Size = new System.Drawing.Size(1328, 25);
            textBoxSendTransactionWalletAddressTarget.TabIndex = 0;
            textBoxSendTransactionWalletAddressTarget.TextChanged += textBoxSendTransactionWalletAddressTarget_TextChanged;
            textBoxSendTransactionWalletAddressTarget.KeyDown += textBoxSendTransactionWalletAddressTarget_KeyDown;
            // 
            // tabPageReceiveTransaction
            // 
            tabPageReceiveTransaction.BackColor = System.Drawing.Color.FromArgb(77, 104, 145);
            tabPageReceiveTransaction.Controls.Add(buttonSaveQrCodeReceiveTransactionWalletAddress);
            tabPageReceiveTransaction.Controls.Add(buttonPrintQrCodeReceiveTransactionWalletAddress);
            tabPageReceiveTransaction.Controls.Add(labelWalletReceiveTransactionQrCodeText);
            tabPageReceiveTransaction.Controls.Add(panelQrCodeWalletAddress);
            tabPageReceiveTransaction.Location = new System.Drawing.Point(4, 34);
            tabPageReceiveTransaction.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPageReceiveTransaction.Name = "tabPageReceiveTransaction";
            tabPageReceiveTransaction.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPageReceiveTransaction.Size = new System.Drawing.Size(1369, 546);
            tabPageReceiveTransaction.TabIndex = 4;
            tabPageReceiveTransaction.Text = "TABPAGE_RECEIVE_TRANSACTION_TEXT";
            // 
            // buttonSaveQrCodeReceiveTransactionWalletAddress
            // 
            buttonSaveQrCodeReceiveTransactionWalletAddress.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonSaveQrCodeReceiveTransactionWalletAddress.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            buttonSaveQrCodeReceiveTransactionWalletAddress.ForeColor = System.Drawing.Color.Black;
            buttonSaveQrCodeReceiveTransactionWalletAddress.Location = new System.Drawing.Point(453, 427);
            buttonSaveQrCodeReceiveTransactionWalletAddress.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonSaveQrCodeReceiveTransactionWalletAddress.Name = "buttonSaveQrCodeReceiveTransactionWalletAddress";
            buttonSaveQrCodeReceiveTransactionWalletAddress.Size = new System.Drawing.Size(475, 36);
            buttonSaveQrCodeReceiveTransactionWalletAddress.TabIndex = 5;
            buttonSaveQrCodeReceiveTransactionWalletAddress.Text = "BUTTON_MAIN_INTERFACE_SAVE_QR_CODE_TEXT";
            buttonSaveQrCodeReceiveTransactionWalletAddress.UseVisualStyleBackColor = false;
            buttonSaveQrCodeReceiveTransactionWalletAddress.Click += buttonSaveQrCodeReceiveTransactionWalletAddress_Click;
            // 
            // buttonPrintQrCodeReceiveTransactionWalletAddress
            // 
            buttonPrintQrCodeReceiveTransactionWalletAddress.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonPrintQrCodeReceiveTransactionWalletAddress.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            buttonPrintQrCodeReceiveTransactionWalletAddress.ForeColor = System.Drawing.Color.Black;
            buttonPrintQrCodeReceiveTransactionWalletAddress.Location = new System.Drawing.Point(453, 473);
            buttonPrintQrCodeReceiveTransactionWalletAddress.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonPrintQrCodeReceiveTransactionWalletAddress.Name = "buttonPrintQrCodeReceiveTransactionWalletAddress";
            buttonPrintQrCodeReceiveTransactionWalletAddress.Size = new System.Drawing.Size(475, 36);
            buttonPrintQrCodeReceiveTransactionWalletAddress.TabIndex = 4;
            buttonPrintQrCodeReceiveTransactionWalletAddress.Text = "BUTTON_MAIN_INTERFACE_PRINT_QR_CODE_TEXT";
            buttonPrintQrCodeReceiveTransactionWalletAddress.UseVisualStyleBackColor = false;
            buttonPrintQrCodeReceiveTransactionWalletAddress.Click += buttonPrintQrCodeReceiveTransactionWalletAddress_Click;
            // 
            // labelWalletReceiveTransactionQrCodeText
            // 
            labelWalletReceiveTransactionQrCodeText.AutoSize = true;
            labelWalletReceiveTransactionQrCodeText.ForeColor = System.Drawing.Color.Ivory;
            labelWalletReceiveTransactionQrCodeText.Location = new System.Drawing.Point(455, 36);
            labelWalletReceiveTransactionQrCodeText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelWalletReceiveTransactionQrCodeText.Name = "labelWalletReceiveTransactionQrCodeText";
            labelWalletReceiveTransactionQrCodeText.Size = new System.Drawing.Size(441, 16);
            labelWalletReceiveTransactionQrCodeText.TabIndex = 3;
            labelWalletReceiveTransactionQrCodeText.Text = "LABEL_MAIN_INTERFACE_QR_CODE_RECEIVE_TITLE_TEXT";
            // 
            // panelQrCodeWalletAddress
            // 
            panelQrCodeWalletAddress.BackColor = System.Drawing.Color.White;
            panelQrCodeWalletAddress.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            panelQrCodeWalletAddress.BorderColor = System.Drawing.Color.Ivory;
            panelQrCodeWalletAddress.BorderSize = 1F;
            panelQrCodeWalletAddress.Location = new System.Drawing.Point(512, 74);
            panelQrCodeWalletAddress.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panelQrCodeWalletAddress.Name = "panelQrCodeWalletAddress";
            panelQrCodeWalletAddress.Radius = 30;
            panelQrCodeWalletAddress.Size = new System.Drawing.Size(350, 346);
            panelQrCodeWalletAddress.TabIndex = 0;
            // 
            // tabPageTransactionHistory
            // 
            tabPageTransactionHistory.BackColor = System.Drawing.Color.FromArgb(77, 104, 145);
            tabPageTransactionHistory.Controls.Add(buttonMainInterfaceSearchTransactionHistory);
            tabPageTransactionHistory.Controls.Add(textBoxTransactionHistorySearch);
            tabPageTransactionHistory.Controls.Add(panelTransactionHistoryColumns);
            tabPageTransactionHistory.Controls.Add(textBoxMainInterfaceMaxPageTransactionHistory);
            tabPageTransactionHistory.Controls.Add(textBoxMainInterfaceCurrentPageTransactionHistory);
            tabPageTransactionHistory.Controls.Add(buttonMainInterfaceNextPageTransactionHistory);
            tabPageTransactionHistory.Controls.Add(buttonMainInterfaceBackPageTransactionHistory);
            tabPageTransactionHistory.Controls.Add(buttonMainInterfaceExportTransactionHistory);
            tabPageTransactionHistory.Controls.Add(panelTransactionHistory);
            tabPageTransactionHistory.Location = new System.Drawing.Point(4, 34);
            tabPageTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPageTransactionHistory.Name = "tabPageTransactionHistory";
            tabPageTransactionHistory.Size = new System.Drawing.Size(1369, 546);
            tabPageTransactionHistory.TabIndex = 2;
            tabPageTransactionHistory.Text = "TABPAGE_TRANSACTION_HISTORY_TEXT";
            tabPageTransactionHistory.Paint += tabPageTransactionHistory_Paint;
            // 
            // buttonMainInterfaceSearchTransactionHistory
            // 
            buttonMainInterfaceSearchTransactionHistory.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonMainInterfaceSearchTransactionHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            buttonMainInterfaceSearchTransactionHistory.ForeColor = System.Drawing.Color.Black;
            buttonMainInterfaceSearchTransactionHistory.Location = new System.Drawing.Point(930, 508);
            buttonMainInterfaceSearchTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonMainInterfaceSearchTransactionHistory.Name = "buttonMainInterfaceSearchTransactionHistory";
            buttonMainInterfaceSearchTransactionHistory.Size = new System.Drawing.Size(177, 25);
            buttonMainInterfaceSearchTransactionHistory.TabIndex = 13;
            buttonMainInterfaceSearchTransactionHistory.Text = "BUTTON_MAIN_INTERFACE_SEARCH_TRANSACTION_HISTORY_TEXT";
            buttonMainInterfaceSearchTransactionHistory.UseVisualStyleBackColor = false;
            buttonMainInterfaceSearchTransactionHistory.Click += buttonMainInterfaceSearchTransactionHistory_Click;
            // 
            // textBoxTransactionHistorySearch
            // 
            textBoxTransactionHistorySearch.Location = new System.Drawing.Point(600, 510);
            textBoxTransactionHistorySearch.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxTransactionHistorySearch.Name = "textBoxTransactionHistorySearch";
            textBoxTransactionHistorySearch.Size = new System.Drawing.Size(318, 22);
            textBoxTransactionHistorySearch.TabIndex = 12;
            textBoxTransactionHistorySearch.KeyDown += textBoxTransactionHistorySearch_KeyDown;
            // 
            // panelTransactionHistoryColumns
            // 
            panelTransactionHistoryColumns.BackColor = System.Drawing.Color.FromArgb(70, 90, 120);
            panelTransactionHistoryColumns.BorderColor = System.Drawing.Color.Transparent;
            panelTransactionHistoryColumns.BorderSize = 1F;
            panelTransactionHistoryColumns.Location = new System.Drawing.Point(13, 1);
            panelTransactionHistoryColumns.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panelTransactionHistoryColumns.Name = "panelTransactionHistoryColumns";
            panelTransactionHistoryColumns.Radius = 10;
            panelTransactionHistoryColumns.Size = new System.Drawing.Size(1342, 52);
            panelTransactionHistoryColumns.TabIndex = 11;
            panelTransactionHistoryColumns.Click += panelMainInterfaceTransactionHistoryColumns_Click;
            panelTransactionHistoryColumns.Paint += panelMainInterfaceTransactionHistoryColumns_Paint;
            // 
            // textBoxMainInterfaceMaxPageTransactionHistory
            // 
            textBoxMainInterfaceMaxPageTransactionHistory.Location = new System.Drawing.Point(482, 510);
            textBoxMainInterfaceMaxPageTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxMainInterfaceMaxPageTransactionHistory.Name = "textBoxMainInterfaceMaxPageTransactionHistory";
            textBoxMainInterfaceMaxPageTransactionHistory.ReadOnly = true;
            textBoxMainInterfaceMaxPageTransactionHistory.Size = new System.Drawing.Size(92, 22);
            textBoxMainInterfaceMaxPageTransactionHistory.TabIndex = 10;
            // 
            // textBoxMainInterfaceCurrentPageTransactionHistory
            // 
            textBoxMainInterfaceCurrentPageTransactionHistory.Location = new System.Drawing.Point(382, 510);
            textBoxMainInterfaceCurrentPageTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            textBoxMainInterfaceCurrentPageTransactionHistory.Name = "textBoxMainInterfaceCurrentPageTransactionHistory";
            textBoxMainInterfaceCurrentPageTransactionHistory.Size = new System.Drawing.Size(92, 22);
            textBoxMainInterfaceCurrentPageTransactionHistory.TabIndex = 9;
            textBoxMainInterfaceCurrentPageTransactionHistory.KeyDown += textBoxMainInterfaceCurrentPageTransactionHistory_KeyDown;
            // 
            // buttonMainInterfaceNextPageTransactionHistory
            // 
            buttonMainInterfaceNextPageTransactionHistory.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonMainInterfaceNextPageTransactionHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            buttonMainInterfaceNextPageTransactionHistory.ForeColor = System.Drawing.Color.Black;
            buttonMainInterfaceNextPageTransactionHistory.Location = new System.Drawing.Point(197, 509);
            buttonMainInterfaceNextPageTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonMainInterfaceNextPageTransactionHistory.Name = "buttonMainInterfaceNextPageTransactionHistory";
            buttonMainInterfaceNextPageTransactionHistory.Size = new System.Drawing.Size(177, 28);
            buttonMainInterfaceNextPageTransactionHistory.TabIndex = 8;
            buttonMainInterfaceNextPageTransactionHistory.Text = "BUTTON_MAIN_INTERFACE_NEXT_PAGE_TRANSACTION_HISTORY_TEXT";
            buttonMainInterfaceNextPageTransactionHistory.UseVisualStyleBackColor = false;
            buttonMainInterfaceNextPageTransactionHistory.Click += buttonMainInterfaceNextPageTransactionHistory_Click;
            // 
            // buttonMainInterfaceBackPageTransactionHistory
            // 
            buttonMainInterfaceBackPageTransactionHistory.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonMainInterfaceBackPageTransactionHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            buttonMainInterfaceBackPageTransactionHistory.ForeColor = System.Drawing.Color.Black;
            buttonMainInterfaceBackPageTransactionHistory.Location = new System.Drawing.Point(13, 509);
            buttonMainInterfaceBackPageTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonMainInterfaceBackPageTransactionHistory.Name = "buttonMainInterfaceBackPageTransactionHistory";
            buttonMainInterfaceBackPageTransactionHistory.Size = new System.Drawing.Size(177, 28);
            buttonMainInterfaceBackPageTransactionHistory.TabIndex = 7;
            buttonMainInterfaceBackPageTransactionHistory.Text = "BUTTON_MAIN_INTERFACE_BACK_PAGE_TRANSACTION_HISTORY_TEXT";
            buttonMainInterfaceBackPageTransactionHistory.UseVisualStyleBackColor = false;
            buttonMainInterfaceBackPageTransactionHistory.Click += buttonMainInterfaceBackPageTransactionHistory_Click;
            // 
            // buttonMainInterfaceExportTransactionHistory
            // 
            buttonMainInterfaceExportTransactionHistory.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonMainInterfaceExportTransactionHistory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            buttonMainInterfaceExportTransactionHistory.ForeColor = System.Drawing.Color.Black;
            buttonMainInterfaceExportTransactionHistory.Location = new System.Drawing.Point(1177, 509);
            buttonMainInterfaceExportTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonMainInterfaceExportTransactionHistory.Name = "buttonMainInterfaceExportTransactionHistory";
            buttonMainInterfaceExportTransactionHistory.Size = new System.Drawing.Size(177, 28);
            buttonMainInterfaceExportTransactionHistory.TabIndex = 5;
            buttonMainInterfaceExportTransactionHistory.Text = "BUTTON_MAIN_INTERFACE_EXPORT_TRANSACTION_HISTORY_TEXT";
            buttonMainInterfaceExportTransactionHistory.UseVisualStyleBackColor = false;
            buttonMainInterfaceExportTransactionHistory.Click += buttonMainInterfaceExportTransactionHistory_Click;
            // 
            // panelTransactionHistory
            // 
            panelTransactionHistory.BackColor = System.Drawing.Color.FromArgb(70, 90, 120);
            panelTransactionHistory.BorderColor = System.Drawing.Color.Transparent;
            panelTransactionHistory.BorderSize = 1F;
            panelTransactionHistory.Location = new System.Drawing.Point(13, 54);
            panelTransactionHistory.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panelTransactionHistory.Name = "panelTransactionHistory";
            panelTransactionHistory.Radius = 10;
            panelTransactionHistory.Size = new System.Drawing.Size(1342, 450);
            panelTransactionHistory.TabIndex = 6;
            panelTransactionHistory.Click += panelTransactionHistory_Click;
            panelTransactionHistory.Paint += panelTransactionHistory_Paint;
            panelTransactionHistory.DoubleClick += panelTransactionHistory_DoubleClick;
            panelTransactionHistory.MouseLeave += panelTransactionHistory_MouseLeave;
            panelTransactionHistory.MouseMove += panelTransactionHistory_MouseMove;
            // 
            // tabPageStoreNetwork
            // 
            tabPageStoreNetwork.BackColor = System.Drawing.Color.FromArgb(77, 104, 145);
            tabPageStoreNetwork.Controls.Add(panel1);
            tabPageStoreNetwork.Location = new System.Drawing.Point(4, 34);
            tabPageStoreNetwork.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPageStoreNetwork.Name = "tabPageStoreNetwork";
            tabPageStoreNetwork.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPageStoreNetwork.Size = new System.Drawing.Size(1369, 546);
            tabPageStoreNetwork.TabIndex = 3;
            tabPageStoreNetwork.Text = "TABPAGE_STORE_NETWORK_TEXT";
            // 
            // panel1
            // 
            panel1.BackColor = System.Drawing.Color.White;
            panel1.Controls.Add(listViewWebNode);
            panel1.Controls.Add(panelStoreNetwork);
            panel1.Location = new System.Drawing.Point(7, 6);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1356, 534);
            panel1.TabIndex = 0;
            // 
            // listViewWebNode
            // 
            listViewWebNode.Location = new System.Drawing.Point(24, 18);
            listViewWebNode.Name = "listViewWebNode";
            listViewWebNode.Size = new System.Drawing.Size(333, 502);
            listViewWebNode.TabIndex = 2;
            listViewWebNode.UseCompatibleStateImageBehavior = false;
            listViewWebNode.View = System.Windows.Forms.View.List;
            listViewWebNode.ItemCheck += listViewWebNode_ItemCheck;
            listViewWebNode.ItemChecked += listViewWebNode_ItemChecked;
            listViewWebNode.MouseClick += listViewWebNode_MouseClick;
            // 
            // panelStoreNetwork
            // 
            panelStoreNetwork.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panelStoreNetwork.Location = new System.Drawing.Point(374, 18);
            panelStoreNetwork.Name = "panelStoreNetwork";
            panelStoreNetwork.Size = new System.Drawing.Size(963, 502);
            panelStoreNetwork.TabIndex = 1;
            // 
            // labelWalletAddressReceiveTransactionTitle
            // 
            labelWalletAddressReceiveTransactionTitle.AutoSize = true;
            labelWalletAddressReceiveTransactionTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelWalletAddressReceiveTransactionTitle.ForeColor = System.Drawing.Color.Ivory;
            labelWalletAddressReceiveTransactionTitle.Location = new System.Drawing.Point(477, 113);
            labelWalletAddressReceiveTransactionTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelWalletAddressReceiveTransactionTitle.Name = "labelWalletAddressReceiveTransactionTitle";
            labelWalletAddressReceiveTransactionTitle.Size = new System.Drawing.Size(423, 13);
            labelWalletAddressReceiveTransactionTitle.TabIndex = 1;
            labelWalletAddressReceiveTransactionTitle.Text = "LABEL_MAIN_INTERFACE_WALLET_ADDRESS_RECEIVE_TITLE_TEXT";
            // 
            // labelWalletAddressReceiveTransaction
            // 
            labelWalletAddressReceiveTransaction.AutoSize = true;
            labelWalletAddressReceiveTransaction.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            labelWalletAddressReceiveTransaction.ForeColor = System.Drawing.Color.Ivory;
            labelWalletAddressReceiveTransaction.Location = new System.Drawing.Point(574, 126);
            labelWalletAddressReceiveTransaction.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelWalletAddressReceiveTransaction.Name = "labelWalletAddressReceiveTransaction";
            labelWalletAddressReceiveTransaction.Size = new System.Drawing.Size(199, 15);
            labelWalletAddressReceiveTransaction.TabIndex = 2;
            labelWalletAddressReceiveTransaction.Text = "WALLET_ADDRESS_RECEIVE";
            labelWalletAddressReceiveTransaction.Click += labelWalletAddressReceiveTransaction_Click;
            // 
            // progressBarMainInterfaceSyncProgress
            // 
            progressBarMainInterfaceSyncProgress.BackColor = System.Drawing.Color.GhostWhite;
            progressBarMainInterfaceSyncProgress.ForeColor = System.Drawing.Color.Black;
            progressBarMainInterfaceSyncProgress.Location = new System.Drawing.Point(492, 751);
            progressBarMainInterfaceSyncProgress.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            progressBarMainInterfaceSyncProgress.Maximum = 10000;
            progressBarMainInterfaceSyncProgress.Name = "progressBarMainInterfaceSyncProgress";
            progressBarMainInterfaceSyncProgress.Size = new System.Drawing.Size(408, 23);
            progressBarMainInterfaceSyncProgress.Step = 1;
            progressBarMainInterfaceSyncProgress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            progressBarMainInterfaceSyncProgress.TabIndex = 5;
            // 
            // ClassWalletMainInterfaceForm
            // 
            BackColor = System.Drawing.Color.FromArgb(49, 55, 64);
            ClientSize = new System.Drawing.Size(1377, 781);
            Controls.Add(labelMainInterfaceSyncProgress);
            Controls.Add(progressBarMainInterfaceSyncProgress);
            Controls.Add(pictureBoxLogo);
            Controls.Add(labelWalletAddressReceiveTransaction);
            Controls.Add(labelWalletAddressReceiveTransactionTitle);
            Controls.Add(labelWalletOpened);
            Controls.Add(comboBoxListWalletFile);
            Controls.Add(tabControlWallet);
            Controls.Add(menuStripGeneralWallet);
            DoubleBuffered = true;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStripGeneralWallet;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            Name = "ClassWalletMainInterfaceForm";
            Padding = new System.Windows.Forms.Padding(0, 0, 6, 0);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "FORM_TITLE_MAIN_INTERFACE_TEXT";
            FormClosing += ClassWalletMainInterfaceForm_FormClosing;
            FormClosed += ClassWalletMainInterfaceForm_FormClosed;
            Load += ClassWalletMainInterfaceForm_Load;
            Paint += ClassWalletMainInterfaceForm_Paint;
            menuStripGeneralWallet.ResumeLayout(false);
            menuStripGeneralWallet.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).EndInit();
            tabControlWallet.ResumeLayout(false);
            tabPageOverview.ResumeLayout(false);
            tabPageOverview.PerformLayout();
            panelInternalNetworkStats.ResumeLayout(false);
            panelInternalNetworkStats.PerformLayout();
            panelRecentTransactions.ResumeLayout(false);
            panelRecentTransactions.PerformLayout();
            tabPageSendTransaction.ResumeLayout(false);
            panelSendTransaction.ResumeLayout(false);
            panelSendTransaction.PerformLayout();
            panelSendTransactionDetails.ResumeLayout(false);
            panelSendTransactionDetails.PerformLayout();
            tabPageReceiveTransaction.ResumeLayout(false);
            tabPageReceiveTransaction.PerformLayout();
            tabPageTransactionHistory.ResumeLayout(false);
            tabPageTransactionHistory.PerformLayout();
            tabPageStoreNetwork.ResumeLayout(false);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panelStoreNetwork;
        private System.Windows.Forms.ListView listViewWebNode;
    }
}

