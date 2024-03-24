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
            vIEWTEXTToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            normalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dimensionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            typeWebSiteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            leftCenterRightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            progressBarMainInterfaceSyncProgress = new ClassCustomProgressBar();
            progressBarMainInterfaceCheckSyncProgress = new ClassCustomProgressBar();
            panelRecentTransactions = new ClassCustomPanel();
            panelInternalRecentTransactions = new ClassCustomPanel();
            labelMainInterfaceRecentTransaction = new System.Windows.Forms.Label();
            labelMainInterfaceTotalBalanceAmountText = new System.Windows.Forms.Label();
            labelMainInterfacePendingBalanceAmountText = new System.Windows.Forms.Label();
            labelMainInterfaceAvailableBalanceAmountText = new System.Windows.Forms.Label();
            panelSeperatorBalanceLine = new ClassCustomPanel();
            labelMainInterfaceCurrentBalanceText = new System.Windows.Forms.Label();
            tabPageSendTransaction = new System.Windows.Forms.TabPage();
            panelSendTransaction = new ClassCustomPanel();
            buttonSendTransactionDoProcess = new System.Windows.Forms.Button();
            pictureBoxLogoTransaction = new System.Windows.Forms.PictureBox();
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
            menuStripGeneralWallet.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).BeginInit();
            tabControlWallet.SuspendLayout();
            tabPageOverview.SuspendLayout();
            panelInternalNetworkStats.SuspendLayout();
            panelRecentTransactions.SuspendLayout();
            tabPageSendTransaction.SuspendLayout();
            panelSendTransaction.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogoTransaction).BeginInit();
            panelSendTransactionDetails.SuspendLayout();
            tabPageReceiveTransaction.SuspendLayout();
            tabPageTransactionHistory.SuspendLayout();
            tabPageStoreNetwork.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStripGeneralWallet
            // 
            resources.ApplyResources(menuStripGeneralWallet, "menuStripGeneralWallet");
            menuStripGeneralWallet.BackColor = System.Drawing.Color.FromArgb(67, 83, 105);
            menuStripGeneralWallet.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem, settingsToolStripMenuItem, rescanToolStripMenuItem, languageToolStripMenuItem, vIEWTEXTToolStripMenuItem });
            menuStripGeneralWallet.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            menuStripGeneralWallet.Name = "menuStripGeneralWallet";
            menuStripGeneralWallet.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { openWalletToolStripMenuItem, closeWalletToolStripMenuItem, createWalletToolStripMenuItem, importWalletPrivateKeytoolStripMenuItem, exitToolStripMenuItem });
            resources.ApplyResources(fileToolStripMenuItem, "fileToolStripMenuItem");
            fileToolStripMenuItem.ForeColor = System.Drawing.Color.Ivory;
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            // 
            // openWalletToolStripMenuItem
            // 
            openWalletToolStripMenuItem.Name = "openWalletToolStripMenuItem";
            resources.ApplyResources(openWalletToolStripMenuItem, "openWalletToolStripMenuItem");
            // 
            // closeWalletToolStripMenuItem
            // 
            closeWalletToolStripMenuItem.Name = "closeWalletToolStripMenuItem";
            resources.ApplyResources(closeWalletToolStripMenuItem, "closeWalletToolStripMenuItem");
            closeWalletToolStripMenuItem.Click += closeWalletToolStripMenuItem_Click;
            // 
            // createWalletToolStripMenuItem
            // 
            createWalletToolStripMenuItem.Name = "createWalletToolStripMenuItem";
            resources.ApplyResources(createWalletToolStripMenuItem, "createWalletToolStripMenuItem");
            createWalletToolStripMenuItem.Click += createWalletToolStripMenuItem_Click;
            // 
            // importWalletPrivateKeytoolStripMenuItem
            // 
            importWalletPrivateKeytoolStripMenuItem.Name = "importWalletPrivateKeytoolStripMenuItem";
            resources.ApplyResources(importWalletPrivateKeytoolStripMenuItem, "importWalletPrivateKeytoolStripMenuItem");
            importWalletPrivateKeytoolStripMenuItem.Click += importWalletPrivateKeyToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            resources.ApplyResources(exitToolStripMenuItem, "exitToolStripMenuItem");
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // settingsToolStripMenuItem
            // 
            resources.ApplyResources(settingsToolStripMenuItem, "settingsToolStripMenuItem");
            settingsToolStripMenuItem.ForeColor = System.Drawing.Color.Ivory;
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
            // 
            // rescanToolStripMenuItem
            // 
            rescanToolStripMenuItem.ForeColor = System.Drawing.Color.Ivory;
            rescanToolStripMenuItem.Name = "rescanToolStripMenuItem";
            resources.ApplyResources(rescanToolStripMenuItem, "rescanToolStripMenuItem");
            rescanToolStripMenuItem.Click += rescanToolStripMenuItem_Click;
            // 
            // languageToolStripMenuItem
            // 
            languageToolStripMenuItem.ForeColor = System.Drawing.Color.GhostWhite;
            languageToolStripMenuItem.Name = "languageToolStripMenuItem";
            resources.ApplyResources(languageToolStripMenuItem, "languageToolStripMenuItem");
            // 
            // vIEWTEXTToolStripMenuItem
            // 
            vIEWTEXTToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { normalToolStripMenuItem, dimensionsToolStripMenuItem, typeWebSiteToolStripMenuItem, leftCenterRightToolStripMenuItem });
            resources.ApplyResources(vIEWTEXTToolStripMenuItem, "vIEWTEXTToolStripMenuItem");
            vIEWTEXTToolStripMenuItem.ForeColor = System.Drawing.Color.GhostWhite;
            vIEWTEXTToolStripMenuItem.Name = "vIEWTEXTToolStripMenuItem";
            vIEWTEXTToolStripMenuItem.Click += vIEWTEXTToolStripMenuItem_Click;
            // 
            // normalToolStripMenuItem
            // 
            normalToolStripMenuItem.Name = "normalToolStripMenuItem";
            resources.ApplyResources(normalToolStripMenuItem, "normalToolStripMenuItem");
            normalToolStripMenuItem.Click += normalToolStripMenuItem_Click;
            // 
            // dimensionsToolStripMenuItem
            // 
            dimensionsToolStripMenuItem.Name = "dimensionsToolStripMenuItem";
            resources.ApplyResources(dimensionsToolStripMenuItem, "dimensionsToolStripMenuItem");
            dimensionsToolStripMenuItem.Click += dimensionsToolStripMenuItem_Click;
            // 
            // typeWebSiteToolStripMenuItem
            // 
            typeWebSiteToolStripMenuItem.Name = "typeWebSiteToolStripMenuItem";
            resources.ApplyResources(typeWebSiteToolStripMenuItem, "typeWebSiteToolStripMenuItem");
            typeWebSiteToolStripMenuItem.Click += typeWebSiteToolStripMenuItem_Click;
            // 
            // leftCenterRightToolStripMenuItem
            // 
            leftCenterRightToolStripMenuItem.Name = "leftCenterRightToolStripMenuItem";
            resources.ApplyResources(leftCenterRightToolStripMenuItem, "leftCenterRightToolStripMenuItem");
            leftCenterRightToolStripMenuItem.Click += leftCenterRightToolStripMenuItem_Click;
            // 
            // comboBoxListWalletFile
            // 
            resources.ApplyResources(comboBoxListWalletFile, "comboBoxListWalletFile");
            comboBoxListWalletFile.BackColor = System.Drawing.Color.White;
            comboBoxListWalletFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBoxListWalletFile.FormattingEnabled = true;
            comboBoxListWalletFile.Name = "comboBoxListWalletFile";
            comboBoxListWalletFile.SelectedIndexChanged += comboBoxListWalletFile_SelectedIndexChanged;
            // 
            // labelWalletOpened
            // 
            resources.ApplyResources(labelWalletOpened, "labelWalletOpened");
            labelWalletOpened.ForeColor = System.Drawing.Color.Ivory;
            labelWalletOpened.Name = "labelWalletOpened";
            // 
            // labelMainInterfaceSyncProgress
            // 
            resources.ApplyResources(labelMainInterfaceSyncProgress, "labelMainInterfaceSyncProgress");
            labelMainInterfaceSyncProgress.BackColor = System.Drawing.Color.Transparent;
            labelMainInterfaceSyncProgress.ForeColor = System.Drawing.Color.Ivory;
            labelMainInterfaceSyncProgress.Name = "labelMainInterfaceSyncProgress";
            // 
            // pictureBoxLogo
            // 
            pictureBoxLogo.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(pictureBoxLogo, "pictureBoxLogo");
            pictureBoxLogo.Name = "pictureBoxLogo";
            pictureBoxLogo.TabStop = false;
            pictureBoxLogo.Click += pictureBoxLogo_Click;
            // 
            // timerRefreshTransactionHistory
            // 
            timerRefreshTransactionHistory.Enabled = true;
            timerRefreshTransactionHistory.Interval = 10;
            timerRefreshTransactionHistory.Tick += timerRefreshTransactionHistory_Tick;
            // 
            // tabControlWallet
            // 
            resources.ApplyResources(tabControlWallet, "tabControlWallet");
            tabControlWallet.Controls.Add(tabPageOverview);
            tabControlWallet.Controls.Add(tabPageSendTransaction);
            tabControlWallet.Controls.Add(tabPageReceiveTransaction);
            tabControlWallet.Controls.Add(tabPageTransactionHistory);
            tabControlWallet.Controls.Add(tabPageStoreNetwork);
            tabControlWallet.Name = "tabControlWallet";
            tabControlWallet.SelectedIndex = 0;
            tabControlWallet.SizeMode = System.Windows.Forms.TabSizeMode.FillToRight;
            // 
            // tabPageOverview
            // 
            resources.ApplyResources(tabPageOverview, "tabPageOverview");
            tabPageOverview.BackColor = System.Drawing.Color.FromArgb(77, 104, 145);
            tabPageOverview.Controls.Add(labelMainInterfaceSyncProgress);
            tabPageOverview.Controls.Add(panelInternalNetworkStats);
            tabPageOverview.Controls.Add(progressBarMainInterfaceSyncProgress);
            tabPageOverview.Controls.Add(progressBarMainInterfaceCheckSyncProgress);
            tabPageOverview.Controls.Add(panelRecentTransactions);
            tabPageOverview.Controls.Add(labelMainInterfaceTotalBalanceAmountText);
            tabPageOverview.Controls.Add(labelMainInterfacePendingBalanceAmountText);
            tabPageOverview.Controls.Add(labelMainInterfaceAvailableBalanceAmountText);
            tabPageOverview.Controls.Add(panelSeperatorBalanceLine);
            tabPageOverview.Controls.Add(labelMainInterfaceCurrentBalanceText);
            tabPageOverview.Name = "tabPageOverview";
            tabPageOverview.Paint += tabPageOverview_Paint;
            // 
            // panelInternalNetworkStats
            // 
            resources.ApplyResources(panelInternalNetworkStats, "panelInternalNetworkStats");
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
            panelInternalNetworkStats.Name = "panelInternalNetworkStats";
            panelInternalNetworkStats.Radius = 10;
            // 
            // labelMainInterfaceNetworkStatsTotalCoinPendingText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsTotalCoinPendingText, "labelMainInterfaceNetworkStatsTotalCoinPendingText");
            labelMainInterfaceNetworkStatsTotalCoinPendingText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalCoinPendingText.Name = "labelMainInterfaceNetworkStatsTotalCoinPendingText";
            // 
            // labelMainInterfaceNetworkStatsTotalCoinSpreadText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsTotalCoinSpreadText, "labelMainInterfaceNetworkStatsTotalCoinSpreadText");
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.Name = "labelMainInterfaceNetworkStatsTotalCoinSpreadText";
            // 
            // labelMainInterfaceNetworkStatsTotalFeeCirculatingText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsTotalFeeCirculatingText, "labelMainInterfaceNetworkStatsTotalFeeCirculatingText");
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Name = "labelMainInterfaceNetworkStatsTotalFeeCirculatingText";
            // 
            // labelMainInterfaceNetworkStatsTotalCoinCirculatingText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsTotalCoinCirculatingText, "labelMainInterfaceNetworkStatsTotalCoinCirculatingText");
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Name = "labelMainInterfaceNetworkStatsTotalCoinCirculatingText";
            // 
            // labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText, "labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText");
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Name = "labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText";
            // 
            // labelMainInterfaceNetworkStatsTotalTransactionConfirmedText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsTotalTransactionConfirmedText, "labelMainInterfaceNetworkStatsTotalTransactionConfirmedText");
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Name = "labelMainInterfaceNetworkStatsTotalTransactionConfirmedText";
            // 
            // labelMainInterfaceNetworkStatsTotalTransactionText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsTotalTransactionText, "labelMainInterfaceNetworkStatsTotalTransactionText");
            labelMainInterfaceNetworkStatsTotalTransactionText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalTransactionText.Name = "labelMainInterfaceNetworkStatsTotalTransactionText";
            // 
            // labelMainInterfaceNetworkStatsTotalTransactionMemPoolText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsTotalTransactionMemPoolText, "labelMainInterfaceNetworkStatsTotalTransactionMemPoolText");
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Name = "labelMainInterfaceNetworkStatsTotalTransactionMemPoolText";
            // 
            // panelSyncInformationsSeperator
            // 
            resources.ApplyResources(panelSyncInformationsSeperator, "panelSyncInformationsSeperator");
            panelSyncInformationsSeperator.BackColor = System.Drawing.Color.Black;
            panelSyncInformationsSeperator.BorderColor = System.Drawing.Color.White;
            panelSyncInformationsSeperator.BorderSize = 3F;
            panelSyncInformationsSeperator.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panelSyncInformationsSeperator.Name = "panelSyncInformationsSeperator";
            panelSyncInformationsSeperator.Radius = 2;
            // 
            // labelMainInterfaceNetworkStatsInfoSyncText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsInfoSyncText, "labelMainInterfaceNetworkStatsInfoSyncText");
            labelMainInterfaceNetworkStatsInfoSyncText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsInfoSyncText.Name = "labelMainInterfaceNetworkStatsInfoSyncText";
            // 
            // labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText, "labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText");
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Name = "labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText";
            // 
            // labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText, "labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText");
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Name = "labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText";
            // 
            // labelMainInterfaceNetworkStatsCurrentHashrateText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsCurrentHashrateText, "labelMainInterfaceNetworkStatsCurrentHashrateText");
            labelMainInterfaceNetworkStatsCurrentHashrateText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsCurrentHashrateText.Name = "labelMainInterfaceNetworkStatsCurrentHashrateText";
            // 
            // labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText, "labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText");
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Name = "labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText";
            // 
            // labelMainInterfaceNetworkStatsCurrentDifficultyText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsCurrentDifficultyText, "labelMainInterfaceNetworkStatsCurrentDifficultyText");
            labelMainInterfaceNetworkStatsCurrentDifficultyText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsCurrentDifficultyText.Name = "labelMainInterfaceNetworkStatsCurrentDifficultyText";
            // 
            // labelMainInterfaceNetworkStatsTitleText
            // 
            resources.ApplyResources(labelMainInterfaceNetworkStatsTitleText, "labelMainInterfaceNetworkStatsTitleText");
            labelMainInterfaceNetworkStatsTitleText.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceNetworkStatsTitleText.Name = "labelMainInterfaceNetworkStatsTitleText";
            // 
            // progressBarMainInterfaceSyncProgress
            // 
            resources.ApplyResources(progressBarMainInterfaceSyncProgress, "progressBarMainInterfaceSyncProgress");
            progressBarMainInterfaceSyncProgress.BackColor = System.Drawing.Color.GhostWhite;
            progressBarMainInterfaceSyncProgress.ForeColor = System.Drawing.Color.Black;
            progressBarMainInterfaceSyncProgress.Maximum = 10000;
            progressBarMainInterfaceSyncProgress.Name = "progressBarMainInterfaceSyncProgress";
            progressBarMainInterfaceSyncProgress.Step = 1;
            progressBarMainInterfaceSyncProgress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            // 
            // progressBarMainInterfaceCheckSyncProgress
            // 
            resources.ApplyResources(progressBarMainInterfaceCheckSyncProgress, "progressBarMainInterfaceCheckSyncProgress");
            progressBarMainInterfaceCheckSyncProgress.BackColor = System.Drawing.Color.GhostWhite;
            progressBarMainInterfaceCheckSyncProgress.ForeColor = System.Drawing.Color.BlueViolet;
            progressBarMainInterfaceCheckSyncProgress.Maximum = 10000;
            progressBarMainInterfaceCheckSyncProgress.Name = "progressBarMainInterfaceCheckSyncProgress";
            progressBarMainInterfaceCheckSyncProgress.Step = 1;
            progressBarMainInterfaceCheckSyncProgress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            // 
            // panelRecentTransactions
            // 
            resources.ApplyResources(panelRecentTransactions, "panelRecentTransactions");
            panelRecentTransactions.BackColor = System.Drawing.Color.FromArgb(216, 227, 240);
            panelRecentTransactions.BorderColor = System.Drawing.Color.DarkGray;
            panelRecentTransactions.BorderSize = 1F;
            panelRecentTransactions.Controls.Add(panelInternalRecentTransactions);
            panelRecentTransactions.Controls.Add(labelMainInterfaceRecentTransaction);
            panelRecentTransactions.Name = "panelRecentTransactions";
            panelRecentTransactions.Radius = 10;
            panelRecentTransactions.Paint += panelRecentTransactions_Paint;
            // 
            // panelInternalRecentTransactions
            // 
            resources.ApplyResources(panelInternalRecentTransactions, "panelInternalRecentTransactions");
            panelInternalRecentTransactions.BackColor = System.Drawing.Color.FromArgb(245, 249, 252);
            panelInternalRecentTransactions.BorderColor = System.Drawing.Color.FromArgb(91, 106, 128);
            panelInternalRecentTransactions.BorderSize = 2F;
            panelInternalRecentTransactions.Name = "panelInternalRecentTransactions";
            panelInternalRecentTransactions.Radius = 10;
            panelInternalRecentTransactions.Click += panelInternalRecentTransactions_Click;
            panelInternalRecentTransactions.Paint += panelInternalRecentTransactions_Paint;
            panelInternalRecentTransactions.MouseEnter += panelInternalRecentTransactions_MouseEnter;
            panelInternalRecentTransactions.MouseLeave += panelInternalRecentTransactions_MouseLeave;
            panelInternalRecentTransactions.MouseHover += panelInternalRecentTransactions_MouseHover;
            panelInternalRecentTransactions.MouseMove += panelInternalRecentTransactions_MouseMove;
            // 
            // labelMainInterfaceRecentTransaction
            // 
            resources.ApplyResources(labelMainInterfaceRecentTransaction, "labelMainInterfaceRecentTransaction");
            labelMainInterfaceRecentTransaction.ForeColor = System.Drawing.Color.Black;
            labelMainInterfaceRecentTransaction.Name = "labelMainInterfaceRecentTransaction";
            // 
            // labelMainInterfaceTotalBalanceAmountText
            // 
            resources.ApplyResources(labelMainInterfaceTotalBalanceAmountText, "labelMainInterfaceTotalBalanceAmountText");
            labelMainInterfaceTotalBalanceAmountText.BackColor = System.Drawing.Color.Transparent;
            labelMainInterfaceTotalBalanceAmountText.ForeColor = System.Drawing.Color.Ivory;
            labelMainInterfaceTotalBalanceAmountText.Name = "labelMainInterfaceTotalBalanceAmountText";
            // 
            // labelMainInterfacePendingBalanceAmountText
            // 
            resources.ApplyResources(labelMainInterfacePendingBalanceAmountText, "labelMainInterfacePendingBalanceAmountText");
            labelMainInterfacePendingBalanceAmountText.BackColor = System.Drawing.Color.Transparent;
            labelMainInterfacePendingBalanceAmountText.ForeColor = System.Drawing.Color.FromArgb(247, 229, 72);
            labelMainInterfacePendingBalanceAmountText.Name = "labelMainInterfacePendingBalanceAmountText";
            // 
            // labelMainInterfaceAvailableBalanceAmountText
            // 
            resources.ApplyResources(labelMainInterfaceAvailableBalanceAmountText, "labelMainInterfaceAvailableBalanceAmountText");
            labelMainInterfaceAvailableBalanceAmountText.BackColor = System.Drawing.Color.Transparent;
            labelMainInterfaceAvailableBalanceAmountText.ForeColor = System.Drawing.Color.LimeGreen;
            labelMainInterfaceAvailableBalanceAmountText.Name = "labelMainInterfaceAvailableBalanceAmountText";
            // 
            // panelSeperatorBalanceLine
            // 
            panelSeperatorBalanceLine.BackColor = System.Drawing.Color.Black;
            panelSeperatorBalanceLine.BorderColor = System.Drawing.Color.White;
            panelSeperatorBalanceLine.BorderSize = 3F;
            panelSeperatorBalanceLine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(panelSeperatorBalanceLine, "panelSeperatorBalanceLine");
            panelSeperatorBalanceLine.Name = "panelSeperatorBalanceLine";
            panelSeperatorBalanceLine.Radius = 2;
            // 
            // labelMainInterfaceCurrentBalanceText
            // 
            resources.ApplyResources(labelMainInterfaceCurrentBalanceText, "labelMainInterfaceCurrentBalanceText");
            labelMainInterfaceCurrentBalanceText.BackColor = System.Drawing.Color.Transparent;
            labelMainInterfaceCurrentBalanceText.ForeColor = System.Drawing.Color.Ivory;
            labelMainInterfaceCurrentBalanceText.Name = "labelMainInterfaceCurrentBalanceText";
            // 
            // tabPageSendTransaction
            // 
            tabPageSendTransaction.BackColor = System.Drawing.Color.FromArgb(77, 104, 145);
            tabPageSendTransaction.Controls.Add(panelSendTransaction);
            resources.ApplyResources(tabPageSendTransaction, "tabPageSendTransaction");
            tabPageSendTransaction.Name = "tabPageSendTransaction";
            tabPageSendTransaction.Paint += tabPageSendTransaction_Paint;
            // 
            // panelSendTransaction
            // 
            resources.ApplyResources(panelSendTransaction, "panelSendTransaction");
            panelSendTransaction.BackColor = System.Drawing.Color.AliceBlue;
            panelSendTransaction.BorderColor = System.Drawing.Color.Ivory;
            panelSendTransaction.BorderSize = 2F;
            panelSendTransaction.Controls.Add(buttonSendTransactionDoProcess);
            panelSendTransaction.Controls.Add(pictureBoxLogoTransaction);
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
            panelSendTransaction.Name = "panelSendTransaction";
            panelSendTransaction.Radius = 10;
            // 
            // buttonSendTransactionDoProcess
            // 
            resources.ApplyResources(buttonSendTransactionDoProcess, "buttonSendTransactionDoProcess");
            buttonSendTransactionDoProcess.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonSendTransactionDoProcess.ForeColor = System.Drawing.Color.Black;
            buttonSendTransactionDoProcess.Name = "buttonSendTransactionDoProcess";
            buttonSendTransactionDoProcess.UseVisualStyleBackColor = false;
            buttonSendTransactionDoProcess.Click += buttonSendTransactionDoProcess_Click;
            // 
            // pictureBoxLogoTransaction
            // 
            resources.ApplyResources(pictureBoxLogoTransaction, "pictureBoxLogoTransaction");
            pictureBoxLogoTransaction.BackColor = System.Drawing.Color.Transparent;
            pictureBoxLogoTransaction.Name = "pictureBoxLogoTransaction";
            pictureBoxLogoTransaction.TabStop = false;
            // 
            // panelSendTransactionDetails
            // 
            resources.ApplyResources(panelSendTransactionDetails, "panelSendTransactionDetails");
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
            panelSendTransactionDetails.Name = "panelSendTransactionDetails";
            panelSendTransactionDetails.Radius = 10;
            panelSendTransactionDetails.Paint += panelSendTransactionDetails_Paint;
            // 
            // labelSendTransactionAmountToSpend
            // 
            resources.ApplyResources(labelSendTransactionAmountToSpend, "labelSendTransactionAmountToSpend");
            labelSendTransactionAmountToSpend.ForeColor = System.Drawing.Color.Ivory;
            labelSendTransactionAmountToSpend.Name = "labelSendTransactionAmountToSpend";
            // 
            // labelSendTransactionFeeSizeCost
            // 
            resources.ApplyResources(labelSendTransactionFeeSizeCost, "labelSendTransactionFeeSizeCost");
            labelSendTransactionFeeSizeCost.ForeColor = System.Drawing.Color.Ivory;
            labelSendTransactionFeeSizeCost.Name = "labelSendTransactionFeeSizeCost";
            // 
            // textBoxSendTransactionFeeCalculated
            // 
            resources.ApplyResources(textBoxSendTransactionFeeCalculated, "textBoxSendTransactionFeeCalculated");
            textBoxSendTransactionFeeCalculated.Name = "textBoxSendTransactionFeeCalculated";
            textBoxSendTransactionFeeCalculated.ReadOnly = true;
            // 
            // textBoxSendTransactionFeeSizeCost
            // 
            resources.ApplyResources(textBoxSendTransactionFeeSizeCost, "textBoxSendTransactionFeeSizeCost");
            textBoxSendTransactionFeeSizeCost.Name = "textBoxSendTransactionFeeSizeCost";
            textBoxSendTransactionFeeSizeCost.ReadOnly = true;
            // 
            // labelSendTransactionFeeCalculated
            // 
            resources.ApplyResources(labelSendTransactionFeeCalculated, "labelSendTransactionFeeCalculated");
            labelSendTransactionFeeCalculated.ForeColor = System.Drawing.Color.Ivory;
            labelSendTransactionFeeCalculated.Name = "labelSendTransactionFeeCalculated";
            // 
            // labelSendTransactionFeeConfirmationCost
            // 
            resources.ApplyResources(labelSendTransactionFeeConfirmationCost, "labelSendTransactionFeeConfirmationCost");
            labelSendTransactionFeeConfirmationCost.ForeColor = System.Drawing.Color.Ivory;
            labelSendTransactionFeeConfirmationCost.Name = "labelSendTransactionFeeConfirmationCost";
            // 
            // textBoxSendTransactionAmountToSpend
            // 
            resources.ApplyResources(textBoxSendTransactionAmountToSpend, "textBoxSendTransactionAmountToSpend");
            textBoxSendTransactionAmountToSpend.Name = "textBoxSendTransactionAmountToSpend";
            textBoxSendTransactionAmountToSpend.ReadOnly = true;
            // 
            // textBoxSendTransactionFeeConfirmationCost
            // 
            resources.ApplyResources(textBoxSendTransactionFeeConfirmationCost, "textBoxSendTransactionFeeConfirmationCost");
            textBoxSendTransactionFeeConfirmationCost.Name = "textBoxSendTransactionFeeConfirmationCost";
            textBoxSendTransactionFeeConfirmationCost.ReadOnly = true;
            // 
            // textBoxSendTransactionTotalAmountSource
            // 
            resources.ApplyResources(textBoxSendTransactionTotalAmountSource, "textBoxSendTransactionTotalAmountSource");
            textBoxSendTransactionTotalAmountSource.Name = "textBoxSendTransactionTotalAmountSource";
            textBoxSendTransactionTotalAmountSource.ReadOnly = true;
            // 
            // labelSendTransactionTotalAmountSource
            // 
            resources.ApplyResources(labelSendTransactionTotalAmountSource, "labelSendTransactionTotalAmountSource");
            labelSendTransactionTotalAmountSource.ForeColor = System.Drawing.Color.Ivory;
            labelSendTransactionTotalAmountSource.Name = "labelSendTransactionTotalAmountSource";
            // 
            // textBoxSendTransactionConfirmationsCountTarget
            // 
            resources.ApplyResources(textBoxSendTransactionConfirmationsCountTarget, "textBoxSendTransactionConfirmationsCountTarget");
            textBoxSendTransactionConfirmationsCountTarget.Name = "textBoxSendTransactionConfirmationsCountTarget";
            textBoxSendTransactionConfirmationsCountTarget.TextChanged += textBoxSendTransactionConfirmationsCountTarget_TextChanged;
            textBoxSendTransactionConfirmationsCountTarget.KeyDown += textBoxSendTransactionConfirmationsCountTarget_KeyDown;
            // 
            // labelSendTransactionConfirmationTimeEstimated
            // 
            resources.ApplyResources(labelSendTransactionConfirmationTimeEstimated, "labelSendTransactionConfirmationTimeEstimated");
            labelSendTransactionConfirmationTimeEstimated.Name = "labelSendTransactionConfirmationTimeEstimated";
            // 
            // labelSendTransactionPaymentId
            // 
            resources.ApplyResources(labelSendTransactionPaymentId, "labelSendTransactionPaymentId");
            labelSendTransactionPaymentId.Name = "labelSendTransactionPaymentId";
            // 
            // textBoxSendTransactionPaymentId
            // 
            resources.ApplyResources(textBoxSendTransactionPaymentId, "textBoxSendTransactionPaymentId");
            textBoxSendTransactionPaymentId.Name = "textBoxSendTransactionPaymentId";
            textBoxSendTransactionPaymentId.KeyDown += textBoxSendTransactionPaymentId_KeyDown;
            // 
            // labelSendTransactionAvailableBalanceText
            // 
            resources.ApplyResources(labelSendTransactionAvailableBalanceText, "labelSendTransactionAvailableBalanceText");
            labelSendTransactionAvailableBalanceText.Name = "labelSendTransactionAvailableBalanceText";
            // 
            // buttonSendTransactionOpenContactList
            // 
            resources.ApplyResources(buttonSendTransactionOpenContactList, "buttonSendTransactionOpenContactList");
            buttonSendTransactionOpenContactList.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonSendTransactionOpenContactList.ForeColor = System.Drawing.Color.Black;
            buttonSendTransactionOpenContactList.Name = "buttonSendTransactionOpenContactList";
            buttonSendTransactionOpenContactList.UseVisualStyleBackColor = false;
            buttonSendTransactionOpenContactList.Click += buttonSendTransactionOpenContactList_Click;
            // 
            // labelSendTransactionConfirmationCountTarget
            // 
            resources.ApplyResources(labelSendTransactionConfirmationCountTarget, "labelSendTransactionConfirmationCountTarget");
            labelSendTransactionConfirmationCountTarget.Name = "labelSendTransactionConfirmationCountTarget";
            // 
            // labelSendTransactionAmountSelected
            // 
            resources.ApplyResources(labelSendTransactionAmountSelected, "labelSendTransactionAmountSelected");
            labelSendTransactionAmountSelected.Name = "labelSendTransactionAmountSelected";
            // 
            // textBoxSendTransactionAmountSelected
            // 
            resources.ApplyResources(textBoxSendTransactionAmountSelected, "textBoxSendTransactionAmountSelected");
            textBoxSendTransactionAmountSelected.Name = "textBoxSendTransactionAmountSelected";
            textBoxSendTransactionAmountSelected.KeyDown += textBoxSendTransactionAmountSelected_KeyDown;
            textBoxSendTransactionAmountSelected.KeyUp += textBoxSendTransactionAmountSelected_KeyUp;
            // 
            // labelSendTransactionWalletAddressTarget
            // 
            resources.ApplyResources(labelSendTransactionWalletAddressTarget, "labelSendTransactionWalletAddressTarget");
            labelSendTransactionWalletAddressTarget.Name = "labelSendTransactionWalletAddressTarget";
            // 
            // textBoxSendTransactionWalletAddressTarget
            // 
            resources.ApplyResources(textBoxSendTransactionWalletAddressTarget, "textBoxSendTransactionWalletAddressTarget");
            textBoxSendTransactionWalletAddressTarget.Name = "textBoxSendTransactionWalletAddressTarget";
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
            resources.ApplyResources(tabPageReceiveTransaction, "tabPageReceiveTransaction");
            tabPageReceiveTransaction.Name = "tabPageReceiveTransaction";
            // 
            // buttonSaveQrCodeReceiveTransactionWalletAddress
            // 
            resources.ApplyResources(buttonSaveQrCodeReceiveTransactionWalletAddress, "buttonSaveQrCodeReceiveTransactionWalletAddress");
            buttonSaveQrCodeReceiveTransactionWalletAddress.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonSaveQrCodeReceiveTransactionWalletAddress.ForeColor = System.Drawing.Color.Black;
            buttonSaveQrCodeReceiveTransactionWalletAddress.Name = "buttonSaveQrCodeReceiveTransactionWalletAddress";
            buttonSaveQrCodeReceiveTransactionWalletAddress.UseVisualStyleBackColor = false;
            buttonSaveQrCodeReceiveTransactionWalletAddress.Click += buttonSaveQrCodeReceiveTransactionWalletAddress_Click;
            // 
            // buttonPrintQrCodeReceiveTransactionWalletAddress
            // 
            resources.ApplyResources(buttonPrintQrCodeReceiveTransactionWalletAddress, "buttonPrintQrCodeReceiveTransactionWalletAddress");
            buttonPrintQrCodeReceiveTransactionWalletAddress.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonPrintQrCodeReceiveTransactionWalletAddress.ForeColor = System.Drawing.Color.Black;
            buttonPrintQrCodeReceiveTransactionWalletAddress.Name = "buttonPrintQrCodeReceiveTransactionWalletAddress";
            buttonPrintQrCodeReceiveTransactionWalletAddress.UseVisualStyleBackColor = false;
            buttonPrintQrCodeReceiveTransactionWalletAddress.Click += buttonPrintQrCodeReceiveTransactionWalletAddress_Click;
            // 
            // labelWalletReceiveTransactionQrCodeText
            // 
            resources.ApplyResources(labelWalletReceiveTransactionQrCodeText, "labelWalletReceiveTransactionQrCodeText");
            labelWalletReceiveTransactionQrCodeText.ForeColor = System.Drawing.Color.Ivory;
            labelWalletReceiveTransactionQrCodeText.Name = "labelWalletReceiveTransactionQrCodeText";
            // 
            // panelQrCodeWalletAddress
            // 
            resources.ApplyResources(panelQrCodeWalletAddress, "panelQrCodeWalletAddress");
            panelQrCodeWalletAddress.BackColor = System.Drawing.Color.White;
            panelQrCodeWalletAddress.BorderColor = System.Drawing.Color.Ivory;
            panelQrCodeWalletAddress.BorderSize = 1F;
            panelQrCodeWalletAddress.Name = "panelQrCodeWalletAddress";
            panelQrCodeWalletAddress.Radius = 30;
            panelQrCodeWalletAddress.Tag = "image";
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
            resources.ApplyResources(tabPageTransactionHistory, "tabPageTransactionHistory");
            tabPageTransactionHistory.Name = "tabPageTransactionHistory";
            tabPageTransactionHistory.Paint += tabPageTransactionHistory_Paint;
            // 
            // buttonMainInterfaceSearchTransactionHistory
            // 
            resources.ApplyResources(buttonMainInterfaceSearchTransactionHistory, "buttonMainInterfaceSearchTransactionHistory");
            buttonMainInterfaceSearchTransactionHistory.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonMainInterfaceSearchTransactionHistory.ForeColor = System.Drawing.Color.Black;
            buttonMainInterfaceSearchTransactionHistory.Name = "buttonMainInterfaceSearchTransactionHistory";
            buttonMainInterfaceSearchTransactionHistory.UseVisualStyleBackColor = false;
            buttonMainInterfaceSearchTransactionHistory.Click += buttonMainInterfaceSearchTransactionHistory_Click;
            // 
            // textBoxTransactionHistorySearch
            // 
            resources.ApplyResources(textBoxTransactionHistorySearch, "textBoxTransactionHistorySearch");
            textBoxTransactionHistorySearch.Name = "textBoxTransactionHistorySearch";
            textBoxTransactionHistorySearch.KeyDown += textBoxTransactionHistorySearch_KeyDown;
            // 
            // panelTransactionHistoryColumns
            // 
            resources.ApplyResources(panelTransactionHistoryColumns, "panelTransactionHistoryColumns");
            panelTransactionHistoryColumns.BackColor = System.Drawing.Color.FromArgb(70, 90, 120);
            panelTransactionHistoryColumns.BorderColor = System.Drawing.Color.Transparent;
            panelTransactionHistoryColumns.BorderSize = 1F;
            panelTransactionHistoryColumns.Name = "panelTransactionHistoryColumns";
            panelTransactionHistoryColumns.Radius = 10;
            panelTransactionHistoryColumns.Click += panelMainInterfaceTransactionHistoryColumns_Click;
            panelTransactionHistoryColumns.Paint += panelMainInterfaceTransactionHistoryColumns_Paint;
            // 
            // textBoxMainInterfaceMaxPageTransactionHistory
            // 
            resources.ApplyResources(textBoxMainInterfaceMaxPageTransactionHistory, "textBoxMainInterfaceMaxPageTransactionHistory");
            textBoxMainInterfaceMaxPageTransactionHistory.Name = "textBoxMainInterfaceMaxPageTransactionHistory";
            textBoxMainInterfaceMaxPageTransactionHistory.ReadOnly = true;
            // 
            // textBoxMainInterfaceCurrentPageTransactionHistory
            // 
            resources.ApplyResources(textBoxMainInterfaceCurrentPageTransactionHistory, "textBoxMainInterfaceCurrentPageTransactionHistory");
            textBoxMainInterfaceCurrentPageTransactionHistory.Name = "textBoxMainInterfaceCurrentPageTransactionHistory";
            textBoxMainInterfaceCurrentPageTransactionHistory.KeyDown += textBoxMainInterfaceCurrentPageTransactionHistory_KeyDown;
            // 
            // buttonMainInterfaceNextPageTransactionHistory
            // 
            resources.ApplyResources(buttonMainInterfaceNextPageTransactionHistory, "buttonMainInterfaceNextPageTransactionHistory");
            buttonMainInterfaceNextPageTransactionHistory.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonMainInterfaceNextPageTransactionHistory.ForeColor = System.Drawing.Color.Black;
            buttonMainInterfaceNextPageTransactionHistory.Name = "buttonMainInterfaceNextPageTransactionHistory";
            buttonMainInterfaceNextPageTransactionHistory.UseVisualStyleBackColor = false;
            buttonMainInterfaceNextPageTransactionHistory.Click += buttonMainInterfaceNextPageTransactionHistory_Click;
            // 
            // buttonMainInterfaceBackPageTransactionHistory
            // 
            resources.ApplyResources(buttonMainInterfaceBackPageTransactionHistory, "buttonMainInterfaceBackPageTransactionHistory");
            buttonMainInterfaceBackPageTransactionHistory.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonMainInterfaceBackPageTransactionHistory.ForeColor = System.Drawing.Color.Black;
            buttonMainInterfaceBackPageTransactionHistory.Name = "buttonMainInterfaceBackPageTransactionHistory";
            buttonMainInterfaceBackPageTransactionHistory.UseVisualStyleBackColor = false;
            buttonMainInterfaceBackPageTransactionHistory.Click += buttonMainInterfaceBackPageTransactionHistory_Click;
            // 
            // buttonMainInterfaceExportTransactionHistory
            // 
            resources.ApplyResources(buttonMainInterfaceExportTransactionHistory, "buttonMainInterfaceExportTransactionHistory");
            buttonMainInterfaceExportTransactionHistory.BackColor = System.Drawing.Color.FromArgb(247, 229, 72);
            buttonMainInterfaceExportTransactionHistory.ForeColor = System.Drawing.Color.Black;
            buttonMainInterfaceExportTransactionHistory.Name = "buttonMainInterfaceExportTransactionHistory";
            buttonMainInterfaceExportTransactionHistory.UseVisualStyleBackColor = false;
            buttonMainInterfaceExportTransactionHistory.Click += buttonMainInterfaceExportTransactionHistory_Click;
            // 
            // panelTransactionHistory
            // 
            resources.ApplyResources(panelTransactionHistory, "panelTransactionHistory");
            panelTransactionHistory.BackColor = System.Drawing.Color.FromArgb(70, 90, 120);
            panelTransactionHistory.BorderColor = System.Drawing.Color.Transparent;
            panelTransactionHistory.BorderSize = 1F;
            panelTransactionHistory.Name = "panelTransactionHistory";
            panelTransactionHistory.Radius = 10;
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
            resources.ApplyResources(tabPageStoreNetwork, "tabPageStoreNetwork");
            tabPageStoreNetwork.Name = "tabPageStoreNetwork";
            // 
            // panel1
            // 
            resources.ApplyResources(panel1, "panel1");
            panel1.BackColor = System.Drawing.Color.White;
            panel1.Controls.Add(listViewWebNode);
            panel1.Controls.Add(panelStoreNetwork);
            panel1.Name = "panel1";
            // 
            // listViewWebNode
            // 
            resources.ApplyResources(listViewWebNode, "listViewWebNode");
            listViewWebNode.Name = "listViewWebNode";
            listViewWebNode.UseCompatibleStateImageBehavior = false;
            listViewWebNode.View = System.Windows.Forms.View.List;
            listViewWebNode.ItemCheck += listViewWebNode_ItemCheck;
            listViewWebNode.ItemChecked += listViewWebNode_ItemChecked;
            listViewWebNode.MouseClick += listViewWebNode_MouseClick;
            // 
            // panelStoreNetwork
            // 
            resources.ApplyResources(panelStoreNetwork, "panelStoreNetwork");
            panelStoreNetwork.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            panelStoreNetwork.Name = "panelStoreNetwork";
            // 
            // labelWalletAddressReceiveTransactionTitle
            // 
            resources.ApplyResources(labelWalletAddressReceiveTransactionTitle, "labelWalletAddressReceiveTransactionTitle");
            labelWalletAddressReceiveTransactionTitle.ForeColor = System.Drawing.Color.Ivory;
            labelWalletAddressReceiveTransactionTitle.Name = "labelWalletAddressReceiveTransactionTitle";
            // 
            // labelWalletAddressReceiveTransaction
            // 
            resources.ApplyResources(labelWalletAddressReceiveTransaction, "labelWalletAddressReceiveTransaction");
            labelWalletAddressReceiveTransaction.ForeColor = System.Drawing.Color.Ivory;
            labelWalletAddressReceiveTransaction.Name = "labelWalletAddressReceiveTransaction";
            labelWalletAddressReceiveTransaction.Click += labelWalletAddressReceiveTransaction_Click;
            // 
            // ClassWalletMainInterfaceForm
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(49, 55, 64);
            Controls.Add(labelWalletAddressReceiveTransaction);
            Controls.Add(labelWalletAddressReceiveTransactionTitle);
            Controls.Add(labelWalletOpened);
            Controls.Add(comboBoxListWalletFile);
            Controls.Add(tabControlWallet);
            Controls.Add(menuStripGeneralWallet);
            Controls.Add(pictureBoxLogo);
            DoubleBuffered = true;
            MainMenuStrip = menuStripGeneralWallet;
            Name = "ClassWalletMainInterfaceForm";
            MaximizedBoundsChanged += ClassWalletMainInterfaceForm_MaximizedBoundsChanged;
            FormClosing += ClassWalletMainInterfaceForm_FormClosing;
            FormClosed += ClassWalletMainInterfaceForm_FormClosed;
            Load += ClassWalletMainInterfaceForm_Load;
            ResizeBegin += ClassWalletMainInterfaceForm_ResizeBegin;
            ResizeEnd += ClassWalletMainInterfaceForm_ResizeEnd;
            SizeChanged += ClassWalletMainInterfaceForm_SizeChanged;
            Paint += ClassWalletMainInterfaceForm_Paint;
            Resize += ClassWalletMainInterfaceForm_Resize;
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
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogoTransaction).EndInit();
            panelSendTransactionDetails.ResumeLayout(false);
            panelSendTransactionDetails.PerformLayout();
            tabPageReceiveTransaction.ResumeLayout(false);
            tabPageTransactionHistory.ResumeLayout(false);
            tabPageTransactionHistory.PerformLayout();
            tabPageStoreNetwork.ResumeLayout(false);
            tabPageStoreNetwork.PerformLayout();
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
        private ClassCustomProgressBar progressBarMainInterfaceCheckSyncProgress;
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
        private System.Windows.Forms.ToolStripMenuItem vIEWTEXTToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem typeWebSiteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem leftCenterRightToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem normalToolStripMenuItem;
        private System.Windows.Forms.PictureBox pictureBoxLogoTransaction;
        private System.Windows.Forms.ToolStripMenuItem dimensionsToolStripMenuItem;
    }
}

