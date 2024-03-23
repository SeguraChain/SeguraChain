using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.Enum;
using SeguraChain_Desktop_Wallet.InternalForm.Custom.Object;
using SeguraChain_Desktop_Wallet.InternalForm.Rescan;
using SeguraChain_Desktop_Wallet.InternalForm.Startup;
using SeguraChain_Desktop_Wallet.InternalForm.TransactionHistory;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Wallet.Function;
using SeguraChain_Desktop_Wallet.InternalForm.Create;
using SeguraChain_Desktop_Wallet.InternalForm.SendTransaction;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.MainForm.Object;
using SeguraChain_Desktop_Wallet.MainForm.System;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Utility;
using SeguraChain_Desktop_Wallet.InternalForm.Import;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.TaskManager;
using SeguraChain_Desktop_Wallet.InternalForm.Setting;
using EO.WebBrowser;
using EO.WinForm;
using Org.BouncyCastle.Asn1.Crmf;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Enum;
using Org.BouncyCastle.Crmf;

namespace SeguraChain_Desktop_Wallet
{
    public partial class ClassWalletMainInterfaceForm : Form
    {

        private bool _noWalletFile;
        private ClassWalletStartupInternalForm _startupInternalForm;
        private HashSet<string> _listWalletOpened;
        private CancellationTokenSource _cancellationTokenTaskUpdateWalletListOpened;
        private CancellationTokenSource _cancellationTokenTaskUpdateWalletListFilesFound;
        private CancellationTokenSource _cancellationTokenTaskUpdateWalletContentInformations;
        private ClassWalletMainFormLanguage _walletMainFormLanguageObject;
        private ClassWalletTransactionHistorySystemInstance _walletTransactionHistorySystemInstance;
        private ClassWalletRecentTransactionHistorySystemInstance _walletRecentTransactionHistorySystemInstance;
        private WebView _webBrowserStoreNetwork;
        private WebControl _webBrowserControlStoreNetwork;
        private Task _storeNetworkUpdateTask;

        /// <summary>
        /// Current wallet informations of the wallet opened.
        /// </summary>
        private string _currentWalletFilename;

        /// <summary>
        /// Semaphore for graphic events access.
        /// </summary>
        private readonly SemaphoreSlim _semaphoreCopyWalletAddressClickEvent;

        /// <summary>
        /// Graphics content to draw.
        /// </summary>
        private List<Control> _listMainInterfaceControlShadow;
        private Bitmap _mainInterfaceShadowBitmap;
        private List<Control> _listOverviewPanelControlShadow;
        private Bitmap _overviewPanelShadowBitmap;
        private List<Control> _listSendTransactionPanelControlShadow;
        private Bitmap _sendTransactionPanelShadowBitmap;
        private List<Control> _listTransactionHistoryPanelControlShadow;
        private Bitmap _transactionHistoryPanelShadowBitmap;
        private List<Control> _listSendTransactionDetailsPanelControlShadow;
        private Bitmap _sendTransactionDetailsPanelShadowBitmap;
        private List<Control> _listRecentTransactionHistoryPanelControlShadow;
        private Bitmap _recentTransactionHistoryPanelShadowBitmap;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="noWalletFile"></param>
        /// <param name="startupInternalForm"></param>
        public ClassWalletMainInterfaceForm(bool noWalletFile, ClassWalletStartupInternalForm startupInternalForm)
        {

            _listWalletOpened = new HashSet<string>();

            _cancellationTokenTaskUpdateWalletListOpened = new CancellationTokenSource();
            _noWalletFile = noWalletFile;
            _startupInternalForm = startupInternalForm;
            _semaphoreCopyWalletAddressClickEvent = new SemaphoreSlim(1, 1);
            InitializeComponent();
            EnableDoubleBuffer();

            #region Insert control panels target to draw shadows.

            _listMainInterfaceControlShadow = new List<Control>();
            _listOverviewPanelControlShadow = new List<Control>();
            _listSendTransactionPanelControlShadow = new List<Control>();
            _listTransactionHistoryPanelControlShadow = new List<Control>();
            _listSendTransactionDetailsPanelControlShadow = new List<Control>();
            _listRecentTransactionHistoryPanelControlShadow = new List<Control>();
            _listMainInterfaceControlShadow.Add(menuStripGeneralWallet);
            _listMainInterfaceControlShadow.Add(progressBarMainInterfaceSyncProgress);
            _listMainInterfaceControlShadow.Add(comboBoxListWalletFile);
            _listOverviewPanelControlShadow.Add(panelInternalNetworkStats);
            _listOverviewPanelControlShadow.Add(panelRecentTransactions);
            _listSendTransactionPanelControlShadow.Add(panelSendTransaction);
            _listTransactionHistoryPanelControlShadow.Add(panelTransactionHistory);
            _listTransactionHistoryPanelControlShadow.Add(panelTransactionHistoryColumns);
            _listSendTransactionDetailsPanelControlShadow.Add(textBoxSendTransactionAmountToSpend);
            _listSendTransactionDetailsPanelControlShadow.Add(textBoxSendTransactionTotalAmountSource);
            _listSendTransactionDetailsPanelControlShadow.Add(textBoxSendTransactionFeeSizeCost);
            _listSendTransactionDetailsPanelControlShadow.Add(textBoxSendTransactionFeeConfirmationCost);
            _listSendTransactionDetailsPanelControlShadow.Add(textBoxSendTransactionFeeCalculated);
            _listRecentTransactionHistoryPanelControlShadow.Add(panelInternalRecentTransactions);

            #endregion
        }


        #region Graphic WinForm optimizations.

        /// <summary>
        /// Enable doubler buffer on the form, to erase at maximum flickering.
        /// </summary>
        private void EnableDoubleBuffer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        #endregion

        #region Main events.

        /// <summary>Event started after loading the form.</summary>
        private void ClassWalletMainInterfaceForm_Load(object sender, EventArgs e)
        {

#if NET5_0_OR_GREATER
            AutoScaleDimensions = new SizeF(7F, 16F);
            AutoScaleMode = AutoScaleMode.Inherit;
#else
            AutoScaleDimensions = new SizeF(1F, 1F);
#endif

            // Insert language list items.
            InsertLanguageToolstripList();

            // Initialize default transaction confirmations count target.
            textBoxSendTransactionConfirmationsCountTarget.Text = BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations.ToString();

            // Initialize the transaction history.
            _walletTransactionHistorySystemInstance = new ClassWalletTransactionHistorySystemInstance();

            // Initialize the recent transaction history.
            _walletRecentTransactionHistorySystemInstance = new ClassWalletRecentTransactionHistorySystemInstance(panelInternalRecentTransactions.Width, panelInternalRecentTransactions.Height);

            // Update language texts.
            UpdateWalletMainInterfaceLanguageText(ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE);

            // Hide startup internal form.
            _startupInternalForm.Hide();

            UpdateListWalletFileToolstripList();


            // Create or load wallet.
            if (_noWalletFile)
                StartCreateDefaultWallet();
            else
                StartLoadFirstWallet();

            // Enable update tasks.
            EnableTaskUpdateWalletFileListOpened();
            EnableTaskUpdateMenuStripWalletList();
            EnableTaskUpdateBlockchainNetworkStats();
            UpdateRecentTransactionDraw();

            #region Initialize Store Network browser.

            _webBrowserStoreNetwork = new WebView();
            _webBrowserControlStoreNetwork = new WebControl
            {
                Height = panelStoreNetwork.Height,
                Width = panelStoreNetwork.Width,
                WebView = _webBrowserStoreNetwork,
                Dock = DockStyle.Fill
            };
            _webBrowserStoreNetwork.Engine.Options.DisableSpellChecker = true;
            panelStoreNetwork.Controls.Add(_webBrowserControlStoreNetwork);
            _webBrowserStoreNetwork.LoadUrl(BlockchainSetting.CoinUrl);
            UpdateStoreNetworkList();

            #endregion
            Refresh();

            try
            {
                ClassDataContextForm DCF = new ClassDataContextForm();
                DCF.InitDataResponsiveFormControls(this);
                this.Tag = DCF;
                this.Location = new System.Drawing.Point(Convert.ToInt32((Screen.PrimaryScreen.Bounds.Width / 2) - this.Width / 2), Convert.ToInt32((Screen.PrimaryScreen.Bounds.Height / 2) - this.Height / 2));
                //adaptResponsiveFormControlsToFormSize(this, ClassViewStrategiesEnum.TypeWebSite);
                //Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// Update the store network list.
        /// </summary>
        private async void UpdateStoreNetworkList()
        {
            if (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode == ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE)
            {
                while (true)
                {

                    listViewWebNode.Items.Clear();

                    foreach (string peerIp in ClassDesktopWalletCommonData.WalletSyncSystem.NodeInstance.PeerDatabase.Keys)
                    {
                        foreach (var peer in ClassDesktopWalletCommonData.WalletSyncSystem.NodeInstance.PeerDatabase[peerIp, _cancellationTokenTaskUpdateWalletListOpened].Values)
                        {
                            if (peer == null)
                                continue;

                            if (!peer.PeerUniqueId.IsNullOrEmpty(false, out _))
                                listViewWebNode.Items.Add(peerIp + ":" + peer.PeerApiPort);
                        }
                    }

                    await Task.Delay(1000);
                }
            }
        }

        /// <summary>
        /// Event executed once the desktop wallet is on closing state, stop every tasks who update the wallet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletMainInterfaceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_cancellationTokenTaskUpdateWalletListFilesFound.IsCancellationRequested)
                _cancellationTokenTaskUpdateWalletListFilesFound.Cancel();

            _walletRecentTransactionHistorySystemInstance.ClearRecentTransactionHistory();
            StopTaskUpdateWallet();
        }

        /// <summary>
        /// Event executed on closing the desktop wallet main interface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletMainInterfaceForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _startupInternalForm.OnCloseDesktopWallet(this);
        }

        /// <summary>
        /// Function who update every languages.
        /// </summary>
        private void UpdateWalletMainInterfaceLanguageText(ClassEnumTransactionHistoryColumnType columnOrderType)
        {
            _walletMainFormLanguageObject = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletMainFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_MAIN_FORM);

            Text = BlockchainSetting.CoinName + _walletMainFormLanguageObject.FORM_TITLE_MAIN_INTERFACE_TEXT + Assembly.GetExecutingAssembly().GetName().Version;
            labelWalletOpened.Text = _walletMainFormLanguageObject.LABEL_WALLET_OPENED_LIST_TEXT;
            labelWalletOpened = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelWalletOpened, this, 98, false);
            // Testing Views
            //progressBarMainInterfaceSyncProgress = ClassGraphicsUtility.AutoSetLocationAndResizeControl<ClassCustomProgressBar>(progressBarMainInterfaceSyncProgress, this, 50, false);
            //progressBarMainInterfaceCheckSyncProgress = ClassGraphicsUtility.AutoSetLocationAndResizeControl<ClassCustomProgressBar>(progressBarMainInterfaceCheckSyncProgress, this, 50, false);

            labelMainInterfaceSyncProgress.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_SYNC_PROGRESS;
            //labelMainInterfaceSyncProgress = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelMainInterfaceSyncProgress, this, 50, false);

            #region MenuStrip

            fileToolStripMenuItem.Text = _walletMainFormLanguageObject.MENUSTRIP_FILE_TEXT;
            settingsToolStripMenuItem.Text = _walletMainFormLanguageObject.MENUSTRIP_SETTING_TEXT;
            rescanToolStripMenuItem.Text = _walletMainFormLanguageObject.MENUSTRIP_RESCAN_TEXT;
            languageToolStripMenuItem.Text = _walletMainFormLanguageObject.MENUSTRIP_LANGUAGE_TEXT;

            openWalletToolStripMenuItem.Text = _walletMainFormLanguageObject.MENUSTRIP_FILE_OPEN_WALLET_TEXT;
            closeWalletToolStripMenuItem.Text = _walletMainFormLanguageObject.MENUSTRIP_FILE_CLOSE_WALLET_TEXT;
            createWalletToolStripMenuItem.Text = _walletMainFormLanguageObject.MENUSTRIP_FILE_CREATE_WALLET_TEXT;
            importWalletPrivateKeytoolStripMenuItem.Text = _walletMainFormLanguageObject.MENUSTRIP_FILE_IMPORT_PRIVATE_KEY_TEXT;
            exitToolStripMenuItem.Text = _walletMainFormLanguageObject.MENUSTRIP_FILE_EXIT_TEXT;

            #endregion

            #region Tabpages.

            tabPageOverview.Text = _walletMainFormLanguageObject.TABPAGE_OVERVIEW_TEXT;
            tabPageSendTransaction.Text = _walletMainFormLanguageObject.TABPAGE_SEND_TRANSACTION_TEXT;
            tabPageReceiveTransaction.Text = _walletMainFormLanguageObject.TABPAGE_RECEIVE_TRANSACTION_TEXT;
            tabPageTransactionHistory.Text = _walletMainFormLanguageObject.TABPAGE_TRANSACTION_HISTORY_TEXT;
            tabPageStoreNetwork.Text = _walletMainFormLanguageObject.TABPAGE_STORE_NETWORK_TEXT;

            #endregion

            #region Overview.

            // Balance.
            labelMainInterfaceCurrentBalanceText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_CURRENT_BALANCE_TEXT;
            labelMainInterfaceCurrentBalanceText.Location = new Point(panelSeperatorBalanceLine.Location.X, labelMainInterfaceCurrentBalanceText.Location.Y);

            // Recent transactions.
            labelMainInterfaceRecentTransaction.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_RECENT_TRANSACTION_TEXT;
            labelMainInterfaceRecentTransaction = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelMainInterfaceRecentTransaction, panelRecentTransactions, 50, false);

            // Sync.
            labelMainInterfaceNetworkStatsTitleText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TITLE_TEXT;
            labelMainInterfaceNetworkStatsTitleText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelMainInterfaceNetworkStatsTitleText, panelInternalNetworkStats, 50, false);
            labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_BLOCK_HEIGHT_SYNC_TEXT;
            labelMainInterfaceNetworkStatsCurrentDifficultyText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_DIFFICULTY_TEXT;
            labelMainInterfaceNetworkStatsCurrentHashrateText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_HASHRATE_TEXT;
            labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_MINING_LUCK_STATUS_TEXT;
            labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_MINING_LUCK_PERCENT_TEXT;

            labelMainInterfaceNetworkStatsInfoSyncText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_INFO_SYNC_TEXT;
            labelMainInterfaceNetworkStatsInfoSyncText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelMainInterfaceNetworkStatsInfoSyncText, panelInternalNetworkStats, 50, false);

            labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_MEMPOOL_TEXT;
            labelMainInterfaceNetworkStatsTotalTransactionText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_TEXT;
            labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_CONFIRMED_TEXT;
            labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_BLOCK_UNLOCKED_CHECKED_TEXT;
            labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_CIRCULATING_TEXT;
            labelMainInterfaceNetworkStatsTotalCoinPendingText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_PENDING_TEXT;
            labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_FEE_CIRCULATING_TEXT;
            labelMainInterfaceNetworkStatsTotalCoinSpreadText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_SPREAD_TEXT;

            #endregion

            #region Send transaction.

            labelSendTransactionAvailableBalanceText.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_AVAILABLE_BALANCE_TEXT;
            labelSendTransactionWalletAddressTarget.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_WALLET_ADDRESS_TARGET_TEXT;
            labelSendTransactionAmountSelected.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_AMOUNT_SELECTED_TEXT;
            labelSendTransactionFeeCalculated.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_FEE_CALCULATED_TEXT;
            labelSendTransactionFeeConfirmationCost.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_FEE_CONFIRMATION_COST_TEXT;
            labelSendTransactionFeeSizeCost.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_FEE_SIZE_COST_TEXT;
            labelSendTransactionPaymentId.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_PAYMENT_ID_TEXT;
            labelSendTransactionConfirmationCountTarget.Text = string.Format(_walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_COUNT_TARGET_TEXT, BlockchainSetting.TransactionMandatoryMaxBlockTransactionConfirmations);
            labelSendTransactionAmountToSpend.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_AMOUNT_TO_SPEND_TEXT;
            labelSendTransactionTotalAmountSource.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_TOTAL_AMOUNT_SOURCE_TEXT;
            buttonSendTransactionOpenContactList.Text = _walletMainFormLanguageObject.BUTTON_SEND_TRANSACTION_OPEN_CONTACT_LIST_TEXT;
            buttonSendTransactionOpenContactList = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonSendTransactionOpenContactList);
            buttonSendTransactionOpenContactList.Location = new Point(textBoxSendTransactionWalletAddressTarget.Location.X, buttonSendTransactionOpenContactList.Location.Y);
            buttonSendTransactionDoProcess.Text = _walletMainFormLanguageObject.BUTTON_SEND_TRANSACTION_DO_PROCESS_TEXT;
            buttonSendTransactionDoProcess = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonSendTransactionDoProcess);
            buttonSendTransactionDoProcess = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonSendTransactionDoProcess, tabPageSendTransaction, 50, false);
            UpdateTransactionConfirmationTimeEstimated();

            #endregion

            #region Receive transaction.

            labelWalletAddressReceiveTransactionTitle.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_WALLET_ADDRESS_RECEIVE_TITLE_TEXT;
            labelWalletAddressReceiveTransactionTitle = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelWalletAddressReceiveTransactionTitle, this, 50, false);

            labelWalletReceiveTransactionQrCodeText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_QR_CODE_RECEIVE_TITLE_TEXT;
            labelWalletReceiveTransactionQrCodeText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelWalletReceiveTransactionQrCodeText, tabPageReceiveTransaction, 50, false);

            buttonSaveQrCodeReceiveTransactionWalletAddress.Text = _walletMainFormLanguageObject.BUTTON_MAIN_INTERFACE_SAVE_QR_CODE_TEXT;
            buttonSaveQrCodeReceiveTransactionWalletAddress = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonSaveQrCodeReceiveTransactionWalletAddress, tabPageReceiveTransaction, 50, false);

            buttonPrintQrCodeReceiveTransactionWalletAddress.Text = _walletMainFormLanguageObject.BUTTON_MAIN_INTERFACE_PRINT_QR_CODE_TEXT;
            buttonPrintQrCodeReceiveTransactionWalletAddress = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonPrintQrCodeReceiveTransactionWalletAddress, tabPageReceiveTransaction, 50, false);

            panelQrCodeWalletAddress = ClassGraphicsUtility.AutoSetLocationAndResizeControl<ClassCustomPanel>(panelQrCodeWalletAddress, tabPageReceiveTransaction, 50, false);

            #endregion

            #region Transaction history.

            // Draw columns of the transaction history.
            _walletTransactionHistorySystemInstance.DrawTransactionHistoryColumns(panelTransactionHistoryColumns, _walletMainFormLanguageObject, columnOrderType);

            buttonMainInterfaceExportTransactionHistory.Text = _walletMainFormLanguageObject.BUTTON_MAIN_INTERFACE_EXPORT_TRANSACTION_HISTORY_TEXT;
            buttonMainInterfaceExportTransactionHistory = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonMainInterfaceExportTransactionHistory);
            buttonMainInterfaceExportTransactionHistory.Location = new Point(panelTransactionHistory.Location.X + panelTransactionHistory.Width - buttonMainInterfaceExportTransactionHistory.Width, buttonMainInterfaceExportTransactionHistory.Location.Y);

            buttonMainInterfaceBackPageTransactionHistory.Text = _walletMainFormLanguageObject.BUTTON_MAIN_INTERFACE_BACK_PAGE_TRANSACTION_HISTORY_TEXT;
            buttonMainInterfaceBackPageTransactionHistory = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonMainInterfaceBackPageTransactionHistory);

            buttonMainInterfaceNextPageTransactionHistory.Text = _walletMainFormLanguageObject.BUTTON_MAIN_INTERFACE_NEXT_PAGE_TRANSACTION_HISTORY_TEXT;
            buttonMainInterfaceNextPageTransactionHistory = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonMainInterfaceNextPageTransactionHistory);
            buttonMainInterfaceNextPageTransactionHistory.Location = new Point(buttonMainInterfaceBackPageTransactionHistory.Location.X + buttonMainInterfaceBackPageTransactionHistory.Width, buttonMainInterfaceNextPageTransactionHistory.Location.Y);

            buttonMainInterfaceSearchTransactionHistory.Text = _walletMainFormLanguageObject.BUTTON_MAIN_INTERFACE_SEARCH_TRANSACTION_HISTORY_TEXT;
            buttonMainInterfaceSearchTransactionHistory = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonMainInterfaceSearchTransactionHistory);
            buttonMainInterfaceSearchTransactionHistory.Location = new Point(textBoxTransactionHistorySearch.Location.X + textBoxTransactionHistorySearch.Width, buttonMainInterfaceSearchTransactionHistory.Location.Y);


            textBoxMainInterfaceCurrentPageTransactionHistory.Location = new Point(buttonMainInterfaceNextPageTransactionHistory.Location.X + buttonMainInterfaceNextPageTransactionHistory.Width, textBoxMainInterfaceCurrentPageTransactionHistory.Location.Y);
            textBoxMainInterfaceMaxPageTransactionHistory.Location = new Point(textBoxMainInterfaceCurrentPageTransactionHistory.Location.X + textBoxMainInterfaceCurrentPageTransactionHistory.Width, textBoxMainInterfaceMaxPageTransactionHistory.Location.Y);

            #endregion
        }

        #endregion

        #region Manage combobox wallet list opened.

        /// <summary>
        /// This event change the current wallet opened to show
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxListWalletFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            SwitchWalletFile(comboBoxListWalletFile.GetItemText(comboBoxListWalletFile.SelectedItem), false, _cancellationTokenTaskUpdateWalletListOpened);
        }

        #endregion

        #region Manage wallet files.

        /// <summary>
        /// Start to create the default wallet.
        /// </summary>
        private void StartCreateDefaultWallet()
        {
            if (ClassWalletDataFunction.GenerateNewWalletDataToSave(ClassWalletDefaultSetting.WalletDefaultFilename).Result)
            {
                SwitchWalletFile(ClassWalletDefaultSetting.WalletDefaultFilename, true, _cancellationTokenTaskUpdateWalletListOpened);
                if (MessageBox.Show(@"Congratulations, your first wallet has been generated, do you want to print QR Codes of your wallet? [Y/N]", @"Wallet created.", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    using (ClassPrintWalletObject printWalletObject = new ClassPrintWalletObject(
                    ClassWalletDataFunction.GenerateBitmapWalletQrCode(ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[ClassWalletDefaultSetting.WalletDefaultFilename].WalletPrivateKey),
                    ClassWalletDataFunction.GenerateBitmapWalletQrCode(ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[ClassWalletDefaultSetting.WalletDefaultFilename].WalletAddress)))
                        printWalletObject.DoPrintWallet(this);
                }
            }
            else
            {
                MessageBox.Show(@"Failed to generate your first wallet, ensure to have propertly install the desktop wallet", @"Failed to create wallet.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _startupInternalForm.OnCloseDesktopWallet(this);
            }
        }

        /// <summary>
        /// Load the first wallet indexed.
        /// </summary>
        /// <returns></returns>
        private void StartLoadFirstWallet()
        {
            Task.Factory.StartNew(async () =>
            {
                bool successfullyLoaded = false;

                if (ClassDesktopWalletCommonData.WalletDatabase.ListWalletFile.Contains(ClassWalletDefaultSetting.WalletDefaultFilename))
                {
                    if (await ClassDesktopWalletCommonData.WalletDatabase.LoadWalletFileAsync(ClassWalletDefaultSetting.WalletDefaultFilename, new CancellationTokenSource()) == ClassWalletLoadFileEnumResult.WALLET_LOAD_SUCCESS)
                    {
#if DEBUG
                        Debug.WriteLine("Wallet file: " + ClassWalletDefaultSetting.WalletDefaultFilename + " loaded successfully.");
#endif

                        SwitchWalletFile(ClassWalletDefaultSetting.WalletDefaultFilename, true, _cancellationTokenTaskUpdateWalletListOpened);
                        successfullyLoaded = true;
                    }
                }
                else
                {
                    foreach (string walletFilename in ClassDesktopWalletCommonData.WalletDatabase.ListWalletFile)
                    {
                        if (await ClassDesktopWalletCommonData.WalletDatabase.LoadWalletFileAsync(walletFilename, new CancellationTokenSource()) == ClassWalletLoadFileEnumResult.WALLET_LOAD_SUCCESS)
                        {
#if DEBUG
                            Debug.WriteLine("Wallet file: " + walletFilename + " loaded successfully.");
#endif

                            SwitchWalletFile(walletFilename, true, _cancellationTokenTaskUpdateWalletListOpened);
                            successfullyLoaded = true;
                            break;
                        }
                    }
                }

                if (!successfullyLoaded)
                    MessageBox.Show(@"Any wallet(s) has been loaded successfully, check if your wallet files are correct, dump/import your private key(s) into a new wallet if necessary.", @"Load wallet(s) failed.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Switch the wallet file used.
        /// </summary>
        /// <param name="walletFilename"></param>
        /// <param name="cancellation"></param>
        private void SwitchWalletFile(string walletFilename, bool firstLoad, CancellationTokenSource cancellation)
        {
            if (!walletFilename.IsNullOrEmpty(false, out _))
            {
                // Cancel the update task who update wallet informations showed from the previous wallet file.
                StopTaskUpdateWalletInformations();

                if (!firstLoad)
                {
                    if (_walletRecentTransactionHistorySystemInstance != null)
                        _walletRecentTransactionHistorySystemInstance.ClearRecentTransactionHistory();

                    if (_walletTransactionHistorySystemInstance != null)
                    {
                        if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                            _walletTransactionHistorySystemInstance.ClearTransactionHistoryOfWalletFileOpened(_currentWalletFilename);
                    }
                }

                if (_currentWalletFilename != walletFilename)
                {
#if DEBUG
                    Debug.WriteLine("Switch from wallet: " + _currentWalletFilename + " to " + walletFilename);
#endif
                    _currentWalletFilename = walletFilename;

                    MethodInvoker invokeSwitch = () =>
                    {
                        if (!_listWalletOpened.Contains(walletFilename))
                        {
                            _listWalletOpened.Add(walletFilename);
                            comboBoxListWalletFile.Items.Add(walletFilename);
                        }
                    };
                    BeginInvoke(invokeSwitch);


                    if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.ContainsKey(walletFilename))
                    {
                        TaskManager.InsertTask(async () =>
                        {
                            long lastBlockHeightUnlocked = await ClassDesktopWalletCommonData.WalletSyncSystem.GetLastBlockHeightUnlockedSynced(cancellation, true);
                            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletLastBlockHeightSynced < lastBlockHeightUnlocked || ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletLastBlockHeightSynced > lastBlockHeightUnlocked || ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletEnableRescan)
                            {
                                MethodInvoker invoke = () =>
                                {
                                    using (ClassWalletRescanInternalForm walletRescanInternalForm = new ClassWalletRescanInternalForm(walletFilename, false))
                                        walletRescanInternalForm.ShowDialog(this);
                                };

                                BeginInvoke(invoke);
                            }
                        }, 0, null).Wait();
                    }

                    invokeSwitch = () =>
                    {
                        comboBoxListWalletFile.SelectedItem = walletFilename;
                        UpdateOpenWalletFileToolStripMenuItemForeColor(true, walletFilename);
                    };
                    BeginInvoke(invokeSwitch);
                }

                EnableTaskUpdateWalletContentInformations();
                UpdateWalletAddressQrCodeShowed();
            }
        }

        /// <summary>
        /// Update the wallet address qr code to show.
        /// </summary>
        private void UpdateWalletAddressQrCodeShowed()
        {
            MethodInvoker invoke = () =>
            {
                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.ContainsKey(_currentWalletFilename))
                {
                    panelQrCodeWalletAddress.BackgroundImage = ClassWalletDataFunction.GenerateBitmapWalletQrCode(ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletAddress);
                    labelWalletAddressReceiveTransaction.Text = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletAddress;
                    labelWalletAddressReceiveTransaction = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelWalletAddressReceiveTransaction, this, 50d, false);
                }

            };
            BeginInvoke(invoke);
        }

        #endregion

        #region Desktop wallet parallel tasks functions.

        /// <summary>
        /// Enable a task who show wallet files available on the directory of wallets.
        /// </summary>
        private void EnableTaskUpdateMenuStripWalletList()
        {
            _cancellationTokenTaskUpdateWalletListFilesFound = new CancellationTokenSource();

            try
            {
                Task.Factory.StartNew(async () =>
                {
                    HashSet<string> listWalletFileListed = new HashSet<string>();

                    while (ClassDesktopWalletCommonData.DesktopWalletStarted)
                    {
                        try
                        {
                            foreach (var walletFile in Directory.GetFiles(ClassDesktopWalletCommonData.WalletSettingObject.WalletDirectoryPath, ClassWalletDefaultSetting.WalletFileFormat))
                            {
                                string walletFilename = Path.GetFileName(walletFile);

                                bool containWalletFile = listWalletFileListed.Contains(walletFilename);


                                if (!containWalletFile)
                                {
                                    foreach (ToolStripMenuItem walletFileMenuStrip in openWalletToolStripMenuItem.DropDownItems)
                                    {
                                        if (walletFileMenuStrip.Text == walletFilename)
                                            containWalletFile = true;
                                    }


                                    if (!containWalletFile)
                                    {
                                        ToolStripMenuItem walletFileItem = new ToolStripMenuItem { Name = walletFilename, Text = walletFilename };
                                        walletFileItem.Click += openWalletFileToolStripMenuItem_Click;

                                        bool complete = false;

                                        MethodInvoker invoke = () =>
                                        {
                                            openWalletToolStripMenuItem.DropDownItems.Add(walletFileItem);
                                            complete = true;
                                        };
                                        menuStripGeneralWallet.BeginInvoke(invoke);

                                        while (!complete)
                                        {
                                            try
                                            {
                                                await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay, _cancellationTokenTaskUpdateWalletListFilesFound.Token);
                                            }
                                            catch
                                            {
                                                break;
                                            }
                                        }
                                    }

                                    listWalletFileListed.Add(walletFilename);
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            Debug.WriteLine("Exception on update the list of wallet file names: " + error.Message);
                        }
                        await Task.Delay(ClassWalletDefaultSetting.DefaultTaskUpdateWalletFileListOpenedInterval, _cancellationTokenTaskUpdateWalletListOpened.Token);
                    }

                }, _cancellationTokenTaskUpdateWalletListFilesFound.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Enable a task who update the list of wallet(s) opened.
        /// </summary>
        private void EnableTaskUpdateWalletFileListOpened()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (ClassDesktopWalletCommonData.DesktopWalletStarted)
                    {
                        try
                        {
                            #region Insert new wallet file opened.

                            foreach (var walletOpened in _listWalletOpened.ToArray())
                            {
                                bool containWallet = false;

                                if (comboBoxListWalletFile.Items.Count > 0)
                                {
                                    foreach (var walletFile in comboBoxListWalletFile.Items)
                                    {
                                        string walletComboBoxText = comboBoxListWalletFile.GetItemText(walletFile);
                                        if (walletComboBoxText == walletOpened)
                                        {
                                            containWallet = true;
                                            break;
                                        }
                                    }
                                }

                                if (!containWallet)
                                {
                                    _listWalletOpened.Add(walletOpened);

#if DEBUG
                                    Debug.WriteLine("Wallet file opened: " + walletOpened);
#endif
                                    bool complete = false;

                                    MethodInvoker invoke = () =>
                                    {
                                        comboBoxListWalletFile.Items.Add(walletOpened);
                                        complete = true;
                                    };
                                    BeginInvoke(invoke);

                                    while (!complete)
                                    {
                                        try
                                        {
                                            await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay, _cancellationTokenTaskUpdateWalletListOpened.Token);
                                        }
                                        catch
                                        {
                                            break;
                                        }
                                    }
                                }
                            }

                            #endregion

                            #region Research closed wallet file and remove them.

                            if (comboBoxListWalletFile.Items.Count > 0)
                            {
                                for (int i = 0; i < comboBoxListWalletFile.Items.Count; i++)
                                {
                                    if (i < comboBoxListWalletFile.Items.Count)
                                    {
                                        string walletComboBoxText = comboBoxListWalletFile.GetItemText(comboBoxListWalletFile.Items[i]);

                                        if (!_listWalletOpened.Contains(walletComboBoxText))
                                        {
                                            bool removed = false;
                                            MethodInvoker invoke = () =>
                                            {

                                                comboBoxListWalletFile.Items.Remove(
                                                   comboBoxListWalletFile.Items[i]);
                                                removed = true;
                                            };

                                            BeginInvoke(invoke);

                                            while (!removed)
                                            {
                                                try
                                                {
                                                    await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay, _cancellationTokenTaskUpdateWalletListOpened.Token);
                                                }
                                                catch
                                                {
                                                    break;
                                                }
                                            }

                                            break;
                                        }
                                    }
                                }
                            }

                            #endregion

                            #region Research duplicate listed opened wallet file.

                            for (int i = 0; i < comboBoxListWalletFile.Items.Count; i++)
                            {
                                if (i < comboBoxListWalletFile.Items.Count)
                                {
                                    string walletComboBoxText = string.Empty;

                                    bool complete = false;

                                    var i1 = i;
                                    MethodInvoker invokeText = () =>
                                    {
                                        walletComboBoxText = comboBoxListWalletFile.GetItemText(comboBoxListWalletFile.Items[i1]);
                                        complete = true;
                                    };

                                    comboBoxListWalletFile.BeginInvoke(invokeText);

                                    while (!complete)
                                    {
                                        try
                                        {
                                            await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay, _cancellationTokenTaskUpdateWalletListOpened.Token);
                                        }
                                        catch
                                        {
                                            break;
                                        }
                                    }

                                    int foundTime = 0;

                                    bool duplicatedFound = false;

                                    #region Read the whole listed opened file to see how many times this one is showed.

                                    for (int k = 0; k < comboBoxListWalletFile.Items.Count; k++)
                                    {
                                        if (k < comboBoxListWalletFile.Items.Count)
                                        {
                                            if (comboBoxListWalletFile.GetItemText(comboBoxListWalletFile.Items[k]) == walletComboBoxText)
                                            {
                                                foundTime++;
                                            }
                                            if (foundTime > 1)
                                            {
                                                bool removed = false;
                                                MethodInvoker invoke = () =>
                                                {

                                                    comboBoxListWalletFile.Items.Remove(
                                                        comboBoxListWalletFile.Items[k]);
                                                    removed = true;
                                                };

                                                BeginInvoke(invoke);

                                                while (!removed)
                                                {
                                                    try
                                                    {
                                                        await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay, _cancellationTokenTaskUpdateWalletListOpened.Token);
                                                    }
                                                    catch
                                                    {
                                                        break;
                                                    }
                                                }

                                                duplicatedFound = true;

                                                break;
                                            }
                                        }
                                    }

                                    #endregion

                                    if (duplicatedFound)
                                        break;

                                }
                            }

                            #endregion

                            #region Check the wallet file selected text listed.

                            string walletComboBoxSelected = string.Empty;
                            bool completeInvoke = false;
                            MethodInvoker invokeSelected = () =>
                            {
                                walletComboBoxSelected = comboBoxListWalletFile.SelectedText;
                                completeInvoke = true;
                            };

                            comboBoxListWalletFile.BeginInvoke(invokeSelected);

                            while (!completeInvoke)
                            {
                                try
                                {
                                    await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay, _cancellationTokenTaskUpdateWalletListOpened.Token);
                                }
                                catch
                                {
                                    break;
                                }
                            }

                            if (walletComboBoxSelected != _currentWalletFilename)
                            {
                                completeInvoke = false;
                                invokeSelected = () =>
                                {
                                    comboBoxListWalletFile.SelectedText = _currentWalletFilename;
                                    completeInvoke = true;
                                };

                                comboBoxListWalletFile.BeginInvoke(invokeSelected);

                                while (!completeInvoke)
                                {
                                    try
                                    {
                                        await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay, _cancellationTokenTaskUpdateWalletListOpened.Token);
                                    }
                                    catch
                                    {
                                        break;
                                    }
                                }
                            }

                            #endregion
                        }
                        catch
                        {
                            bool complete = false;

                            // Clean up on exception.
                            MethodInvoker invoke = () =>
                            {
                                comboBoxListWalletFile.Items.Clear();
                                complete = true;
                            };
                            BeginInvoke(invoke);

                            while (!complete)
                            {
                                try
                                {
                                    await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay, _cancellationTokenTaskUpdateWalletListOpened.Token);
                                }
                                catch
                                {
                                    break;
                                }
                            }
                        }

                        await Task.Delay(ClassWalletDefaultSetting.DefaultTaskUpdateWalletFileListOpenedInterval, _cancellationTokenTaskUpdateWalletListOpened.Token);

                    }
                }, _cancellationTokenTaskUpdateWalletListOpened.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Enable a task who update blockchain network stats showed.
        /// </summary>
        private void EnableTaskUpdateBlockchainNetworkStats()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (ClassDesktopWalletCommonData.DesktopWalletStarted)
                    {
                        ClassBlockchainNetworkStatsObject blockchainNetworkStatsObject = await ClassDesktopWalletCommonData.WalletSyncSystem.GetBlockchainNetworkStatsObject(_cancellationTokenTaskUpdateWalletContentInformations, true);

                        if (blockchainNetworkStatsObject != null)
                        {
                            long totalMemPoolTransaction = await ClassDesktopWalletCommonData.WalletSyncSystem.GetTotalMemPoolTransactionFromSyncAsync(_cancellationTokenTaskUpdateWalletContentInformations, true);

                            MethodInvoker invoke = () =>
                            {
                                try
                                {
                                    double percentProgress = 100;
                                    double percentCheckProgress = 100;

                                    if (blockchainNetworkStatsObject.LastNetworkBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                                    {
                                        if (blockchainNetworkStatsObject.LastNetworkBlockHeight >= blockchainNetworkStatsObject.LastBlockHeight)
                                            percentProgress = ((double)blockchainNetworkStatsObject.LastBlockHeight / blockchainNetworkStatsObject.LastNetworkBlockHeight) * 100d;

                                        int percentProgressInt = (int)(Math.Round(percentProgress, 2) * 100d);
                                        if (percentProgressInt >= progressBarMainInterfaceSyncProgress.Minimum && percentProgressInt <= progressBarMainInterfaceSyncProgress.Maximum)
                                            progressBarMainInterfaceSyncProgress.Value = percentProgressInt;

                                        if (blockchainNetworkStatsObject.LastBlockHeightUnlocked >= BlockchainSetting.GenesisBlockHeight)
                                            percentCheckProgress = ((double)blockchainNetworkStatsObject.LastBlockHeightTransactionConfirmationDone / blockchainNetworkStatsObject.LastBlockHeightUnlocked) * 100d;

                                        int percentCheckProgressInt = (int)(Math.Round(percentCheckProgress, 2) * 100d);
                                        if (percentCheckProgressInt >= progressBarMainInterfaceSyncProgress.Minimum && percentCheckProgressInt <= progressBarMainInterfaceSyncProgress.Maximum)
                                            progressBarMainInterfaceCheckSyncProgress.Value = percentCheckProgressInt;
                                    }
                                }
                                catch
                                {
                                    // Ignored.
                                }

                                // Sync progress of the wallet.
                                labelMainInterfaceCurrentBalanceText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_CURRENT_BALANCE_TEXT.Replace("%d1", ClassDesktopWalletCommonData.WalletDatabase.GetWalletFileOpenedData(_currentWalletFilename)?.WalletLastBlockHeightSynced.ToString()).Replace("%d2", blockchainNetworkStatsObject.LastBlockHeightUnlocked.ToString());

                                labelMainInterfaceNetworkStatsCurrentBlockHeightSyncText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_BLOCK_HEIGHT_SYNC_TEXT + @"Sync: " + blockchainNetworkStatsObject.LastBlockHeight + @" | Net: " + blockchainNetworkStatsObject.LastNetworkBlockHeight;
                                labelMainInterfaceNetworkStatsCurrentDifficultyText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_DIFFICULTY_TEXT + blockchainNetworkStatsObject.LastBlockDifficulty;
                                labelMainInterfaceNetworkStatsCurrentHashrateText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_HASHRATE_TEXT + blockchainNetworkStatsObject.NetworkHashrateEstimatedFormatted;
                                labelMainInterfaceNetworkStatsCurrentMiningLuckStatusText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_MINING_LUCK_STATUS_TEXT + blockchainNetworkStatsObject.BlockchainMiningStats;
                                labelMainInterfaceNetworkStatsCurrentMiningLuckPercentText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_MINING_LUCK_PERCENT_TEXT + blockchainNetworkStatsObject.BlockMiningLuckPercent + @"%";

                                // Synced part.
                                labelMainInterfaceNetworkStatsTotalTransactionMemPoolText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_MEMPOOL_TEXT + totalMemPoolTransaction;
                                labelMainInterfaceNetworkStatsTotalTransactionText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_TEXT + blockchainNetworkStatsObject.TotalTransactions;
                                labelMainInterfaceNetworkStatsTotalTransactionConfirmedText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_CONFIRMED_TEXT + blockchainNetworkStatsObject.TotalTransactionsConfirmed + @"/" + blockchainNetworkStatsObject.TotalTransactions;
                                labelMainInterfaceNetworkStatsTotalBlockUnlockedCheckedText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_BLOCK_UNLOCKED_CHECKED_TEXT + blockchainNetworkStatsObject.LastBlockHeightTransactionConfirmationDone + @"/" + blockchainNetworkStatsObject.LastBlockHeightUnlocked + _walletMainFormLanguageObject.TEXT_SPACE + @"(" + blockchainNetworkStatsObject.TotalTaskConfirmationsDoneProgress + @"%)";
                                labelMainInterfaceNetworkStatsTotalCoinCirculatingText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_CIRCULATING_TEXT + blockchainNetworkStatsObject.TotalCoinCirculatingFormatted + _walletMainFormLanguageObject.TEXT_SPACE + BlockchainSetting.CoinTickerName;
                                labelMainInterfaceNetworkStatsTotalCoinPendingText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_PENDING_TEXT + blockchainNetworkStatsObject.TotalCoinPendingFormatted + _walletMainFormLanguageObject.TEXT_SPACE + BlockchainSetting.CoinTickerName;
                                labelMainInterfaceNetworkStatsTotalFeeCirculatingText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_FEE_CIRCULATING_TEXT + blockchainNetworkStatsObject.TotalCoinFeeFormatted + _walletMainFormLanguageObject.TEXT_SPACE + BlockchainSetting.CoinTickerName;
                                labelMainInterfaceNetworkStatsTotalCoinSpreadText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_SPREAD_TEXT + blockchainNetworkStatsObject.TotalCoinsSpreadFormatted + _walletMainFormLanguageObject.TEXT_SPACE + BlockchainSetting.CoinTickerName;
                            };

                            BeginInvoke(invoke);
                        }


                        try
                        {
                            await Task.Delay(ClassWalletDefaultSetting.DefaultTaskUpdateWalletBlockchainNetworkStatsInterval, _cancellationTokenTaskUpdateWalletListOpened.Token);
                        }
                        catch
                        {
                            break;
                        }
                    }
                }, _cancellationTokenTaskUpdateWalletListOpened.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Enable a task who update wallet informations.
        /// </summary>
        private void EnableTaskUpdateWalletContentInformations()
        {
            _cancellationTokenTaskUpdateWalletContentInformations = new CancellationTokenSource();

            #region Update current balance showed.

            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (ClassDesktopWalletCommonData.DesktopWalletStarted)
                    {
                        if (_cancellationTokenTaskUpdateWalletContentInformations.IsCancellationRequested)
                            break;

                        try
                        {
                            var currentWalletBalanceObjectOpened = ClassDesktopWalletCommonData.WalletSyncSystem.GetWalletBalanceFromSyncedData(_currentWalletFilename);
                            BigInteger walletAvailableBalanceBigInteger = ClassDesktopWalletCommonData.WalletSyncSystem.GetWalletAvailableBalanceFromSyncedData(_currentWalletFilename);

                            bool complete = false;

                            MethodInvoker invoke = () =>
                            {
                                try
                                {


                                    // Available balance.
                                    labelMainInterfaceAvailableBalanceAmountText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_AVAILABLE_BALANCE_AMOUNT_TEXT + currentWalletBalanceObjectOpened.WalletAvailableBalance + _walletMainFormLanguageObject.TEXT_SPACE + BlockchainSetting.CoinTickerName;
                                    labelMainInterfaceAvailableBalanceAmountText.Location = new Point(panelSeperatorBalanceLine.Location.X, labelMainInterfaceAvailableBalanceAmountText.Location.Y);

                                    // Pending balance.
                                    labelMainInterfacePendingBalanceAmountText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_PENDING_BALANCE_AMOUNT_TEXT + currentWalletBalanceObjectOpened.WalletPendingBalance + _walletMainFormLanguageObject.TEXT_SPACE + BlockchainSetting.CoinTickerName;
                                    labelMainInterfacePendingBalanceAmountText.Location = new Point(panelSeperatorBalanceLine.Location.X, labelMainInterfacePendingBalanceAmountText.Location.Y);

                                    // Total balance.
                                    labelMainInterfaceTotalBalanceAmountText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_TOTAL_BALANCE_AMOUNT_TEXT + currentWalletBalanceObjectOpened.WalletTotalBalance + _walletMainFormLanguageObject.TEXT_SPACE + BlockchainSetting.CoinTickerName;
                                    labelMainInterfaceTotalBalanceAmountText.Location = new Point(panelSeperatorBalanceLine.Location.X, labelMainInterfaceTotalBalanceAmountText.Location.Y);

                                    labelSendTransactionAvailableBalanceText.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_AVAILABLE_BALANCE_TEXT + _walletMainFormLanguageObject.TEXT_SPACE + ClassTransactionUtility.GetFormattedAmountFromBigInteger(walletAvailableBalanceBigInteger) + _walletMainFormLanguageObject.TEXT_SPACE + BlockchainSetting.CoinTickerName;

                                }
                                catch (Exception error)
                                {
#if DEBUG
                                    Debug.WriteLine("Error the wallet balance for " + _currentWalletFilename + " cannot be updated. Exception: " + error.Message);

#endif
                                    // Ignored.
                                }

                                complete = true;
                            };

                            BeginInvoke(invoke);

                            long timespend = 0;

                            while (!complete)
                            {
                                if (_cancellationTokenTaskUpdateWalletContentInformations.IsCancellationRequested)
                                    break;

                                try
                                {
                                    await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay);
                                }
                                catch
                                {
                                    break;
                                }

                                timespend += ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay;


                                if (timespend >= ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelayCancel)
                                    break;
#if DEBUG
                                Debug.WriteLine("Wallet balance for " + _currentWalletFilename + " in pending to complete the update..");

#endif
                            }

                            if (_cancellationTokenTaskUpdateWalletContentInformations.IsCancellationRequested)
                                break;
                        }
                        // Ensure to break the while once the task is cancelled.
                        catch (OperationCanceledException)
                        {
                            break;
                        }
#if DEBUG
                        catch (Exception error)
                        {
                            Debug.WriteLine("Error on calculting the wallet balance of the wallet file: " + _currentWalletFilename + ". Exception: " + error.Message);
                        }
#endif

#if DEBUG
                        Debug.WriteLine("Balance updated for: " + _currentWalletFilename);
#endif

                        await Task.Delay(ClassWalletDefaultSetting.DefaultTaskUpdateWalletInformationsInterval);
                    }
                }, _cancellationTokenTaskUpdateWalletContentInformations.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }

            #endregion

            #region Update recent transactions showed.

            try
            {
                Task.Factory.StartNew(async () =>
                {

                    while (ClassDesktopWalletCommonData.DesktopWalletStarted)
                    {
                        if (_cancellationTokenTaskUpdateWalletContentInformations.IsCancellationRequested)
                            break;

                        try
                        {
                            if (!ClassDesktopWalletCommonData.WalletDatabase.GetWalletRescanStatus(_currentWalletFilename))
                                await _walletRecentTransactionHistorySystemInstance.UpdateRecentTransactionHistory(_currentWalletFilename, _cancellationTokenTaskUpdateWalletContentInformations);
                        }
                        catch (Exception error)
                        {
                            if (error is OperationCanceledException)
                                break;
                            else
                                _walletRecentTransactionHistorySystemInstance.ClearRecentTransactionHistory();
                        }

                        await Task.Delay(ClassWalletDefaultSetting.DefaultTaskUpdateWalletRecentTransactionsInterval);

                    }

                }, _cancellationTokenTaskUpdateWalletContentInformations.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }

            #endregion

            #region Update transaction history content of the current wallet file opened to show.

            try
            {
                // Insert/Update task of the transaction history.
                Task.Factory.StartNew(async () =>
                {

                    while (ClassDesktopWalletCommonData.DesktopWalletStarted)
                    {
                        try
                        {
                            if (_cancellationTokenTaskUpdateWalletContentInformations.IsCancellationRequested)
                                break;

                            if (!ClassDesktopWalletCommonData.WalletDatabase.GetWalletRescanStatus(_currentWalletFilename))
                            {
                                if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                                {
                                    bool complete = false;

                                    if (await _walletTransactionHistorySystemInstance.UpdateTransactionHistoryOfWalletFileOpened(_currentWalletFilename, _cancellationTokenTaskUpdateWalletContentInformations))
                                    {
                                        int maxPage = _walletTransactionHistorySystemInstance.MaxPageTransactionHistory(_currentWalletFilename, _cancellationTokenTaskUpdateWalletContentInformations);

                                        MethodInvoker invokePageUpdate = () =>
                                        {
                                            textBoxMainInterfaceMaxPageTransactionHistory.Text = maxPage.ToString();
                                            complete = true;
                                        };
                                        BeginInvoke(invokePageUpdate);

                                        while (!complete)
                                        {
                                            await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay);

                                            if (_cancellationTokenTaskUpdateWalletContentInformations.IsCancellationRequested)
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    bool complete = false;

                                    _walletTransactionHistorySystemInstance.InsertTransactionHistoryToWalletFileOpened(_currentWalletFilename, panelTransactionHistory.Width, panelTransactionHistory.Height);
                                    MethodInvoker invoke = () =>
                                    {
                                        textBoxMainInterfaceCurrentPageTransactionHistory.Text = @"1";
                                        textBoxMainInterfaceMaxPageTransactionHistory.Text = @"0";
                                        complete = true;
                                    };
                                    BeginInvoke(invoke);

                                    while (!complete)
                                    {
                                        await Task.Delay(ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay);

                                        if (_cancellationTokenTaskUpdateWalletContentInformations.IsCancellationRequested)
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            if (error is OperationCanceledException)
                                break;
                        }

                        await Task.Delay(ClassWalletDefaultSetting.DefaultTaskUpdateWalletTransactionHistoryInterval);
                    }
                }, _cancellationTokenTaskUpdateWalletContentInformations.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }

            #endregion

        }

        /// <summary>
        /// Stop every tasks who update the wallet.
        /// </summary>
        private void StopTaskUpdateWallet()
        {
            StopTaskUpdateWalletInformations();
            if (_cancellationTokenTaskUpdateWalletListOpened != null)
            {
                if (!_cancellationTokenTaskUpdateWalletListOpened.IsCancellationRequested)
                    _cancellationTokenTaskUpdateWalletListOpened.Cancel();
            }
        }

        /// <summary>
        /// Stop the task who update wallet informations.
        /// </summary>
        private void StopTaskUpdateWalletInformations()
        {
            if (_cancellationTokenTaskUpdateWalletContentInformations != null)
            {
                if (!_cancellationTokenTaskUpdateWalletContentInformations.IsCancellationRequested)
                    _cancellationTokenTaskUpdateWalletContentInformations.Cancel();
            }
        }

        #endregion

        #region Menu tooltip strip menu events & functions.

        /// <summary>
        /// This event close the desktop wallet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _startupInternalForm.OnCloseDesktopWallet(this);
        }

        /// <summary>
        /// Execute the wallet form who permit to create a wallet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void createWalletToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ClassWalletCreateInternalForm walletCreateInternalForm = new ClassWalletCreateInternalForm(false, null))
                walletCreateInternalForm.ShowDialog(this);
        }

        /// <summary>
        /// Open a wallet file listed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openWalletFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(async () =>
            {
                if (sender is ToolStripMenuItem menuItem)
                {
                    string walletFilename = menuItem.Text;

                    if (!_listWalletOpened.Contains(walletFilename))
                    {
                        bool failedToOpen = false;
                        if (!ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.ContainsKey(walletFilename))
                        {
                            if (await ClassDesktopWalletCommonData.WalletDatabase.LoadWalletFileAsync(walletFilename, _cancellationTokenTaskUpdateWalletListOpened) != ClassWalletLoadFileEnumResult.WALLET_LOAD_SUCCESS)
                            {
                                failedToOpen = true;
                                MessageBox.Show(@"Error, can't load the wallet file: " + walletFilename);
                            }

                        }
                        if (!failedToOpen)
                        {
                            EnableWalletTabs();
                            SwitchWalletFile(walletFilename, false, _cancellationTokenTaskUpdateWalletListOpened);
                        }
                    }
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Close a wallet file opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeWalletToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_listWalletOpened.Contains(_currentWalletFilename))
            {
                if (ClassDesktopWalletCommonData.WalletDatabase.CloseAndSaveWalletFileAsync(_currentWalletFilename).Result)
                {
                    if (_listWalletOpened.Remove(_currentWalletFilename))
                        UpdateOpenWalletFileToolStripMenuItemForeColor(false, _currentWalletFilename);

                    if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.Count > 0)
                    {
                        EnableWalletTabs();
                        SwitchWalletFile(ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.Keys.Last(), false, _cancellationTokenTaskUpdateWalletListOpened);
                    }
                    else
                    {
                        StopTaskUpdateWallet();
                        StopTaskUpdateWalletInformations();
                        CleanUpWalletBalanceInformations();
                        LockWalletTabs();
                    }
                }
                else
                {
                    MessageBox.Show(@"Failed to close the wallet file: " + _currentWalletFilename + @" please try again later.");
                }
            }
        }

        /// <summary>
        /// Rescan a wallet file opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rescanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopTaskUpdateWalletInformations();

            string walletAddress = ClassDesktopWalletCommonData.WalletDatabase.GetWalletAddressFromWalletFileName(_currentWalletFilename);

            _walletTransactionHistorySystemInstance.RemoveTransactionHistoryFromWalletFileOpenedTarget(_currentWalletFilename);

            ClassDesktopWalletCommonData.WalletSyncSystem.CleanSyncCacheOfWalletAddressTarget(walletAddress, new CancellationTokenSource());

            using (ClassWalletRescanInternalForm walletRescanInternalForm = new ClassWalletRescanInternalForm(_currentWalletFilename, true))
                walletRescanInternalForm.ShowDialog(this);

            SwitchWalletFile(_currentWalletFilename, false, _cancellationTokenTaskUpdateWalletListOpened);
        }

        private void importWalletPrivateKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ClassImportWalletPrivateKeyInternalForm importWalletPrivateKeyInternalForm = new ClassImportWalletPrivateKeyInternalForm())
                importWalletPrivateKeyInternalForm.ShowDialog(this);
        }

        /// <summary>
        /// Update forecolor of wallet file menu strip tool to open.
        /// </summary>
        /// <param name="opened"></param>
        private void UpdateOpenWalletFileToolStripMenuItemForeColor(bool opened, string walletFileName)
        {
            foreach (ToolStripMenuItem walletFileMenuStrip in openWalletToolStripMenuItem.DropDownItems)
            {
                if (walletFileMenuStrip.Text == walletFileName)
                {
                    if (opened)
                        walletFileMenuStrip.ForeColor = Color.LightGray;
                    else
                        walletFileMenuStrip.ForeColor = Color.Black;

                    break;
                }
            }
        }

        /// <summary>
        /// Update the list of wallet files on the toolstrip menu.
        /// </summary>
        /// <param name="fromThread"></param>
        private void UpdateListWalletFileToolstripList()
        {
            foreach (var walletFile in Directory.GetFiles(ClassDesktopWalletCommonData.WalletSettingObject.WalletDirectoryPath, ClassWalletDefaultSetting.WalletFileFormat))
            {
                string walletFilename = Path.GetFileName(walletFile);

                bool containWalletFile = false;

                foreach (ToolStripMenuItem walletFileMenuStrip in openWalletToolStripMenuItem.DropDownItems)
                {
                    if (walletFileMenuStrip.Text == walletFilename)
                        containWalletFile = true;
                }

                if (!containWalletFile)
                {
                    ToolStripMenuItem walletFileItem = new ToolStripMenuItem { Name = walletFilename, Text = walletFilename };
                    walletFileItem.Click += openWalletFileToolStripMenuItem_Click;
                    openWalletToolStripMenuItem.DropDownItems.Add(walletFileItem);
                }
            }
        }

        private void InsertLanguageToolstripList()
        {
            foreach (KeyValuePair<string, string> languageName in ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageList)
            {
                ToolStripMenuItem languageItem = new ToolStripMenuItem { Name = languageName.Key, Text = languageName.Value };
                languageItem.Click += switchLanguageToolStripItem;
                languageToolStripMenuItem.DropDownItems.Add(languageItem);
            }
        }

        private void switchLanguageToolStripItem(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                if (ClassDesktopWalletCommonData.LanguageDatabase.SetCurrentLanguageName(menuItem.Name))
                    UpdateWalletMainInterfaceLanguageText(ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE);
            }
        }

        #endregion

        #region Graphic functions.

        /// <summary>
        /// Clean up wallet balance informations.
        /// </summary>
        private void CleanUpWalletBalanceInformations()
        {
            // Available balance.
            labelMainInterfaceAvailableBalanceAmountText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_AVAILABLE_BALANCE_AMOUNT_TEXT;
            labelMainInterfaceAvailableBalanceAmountText.Location = new Point(panelSeperatorBalanceLine.Location.X, labelMainInterfaceAvailableBalanceAmountText.Location.Y);

            // Pending balance.
            labelMainInterfacePendingBalanceAmountText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_PENDING_BALANCE_AMOUNT_TEXT;
            labelMainInterfacePendingBalanceAmountText.Location = new Point(panelSeperatorBalanceLine.Location.X, labelMainInterfacePendingBalanceAmountText.Location.Y);

            // Total balance.
            labelMainInterfaceTotalBalanceAmountText.Text = _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_TOTAL_BALANCE_AMOUNT_TEXT;
            labelMainInterfaceTotalBalanceAmountText.Location = new Point(panelSeperatorBalanceLine.Location.X, labelMainInterfaceTotalBalanceAmountText.Location.Y);
        }

        /// <summary>
        /// Lock wallet tabs.
        /// </summary>
        private void LockWalletTabs()
        {
            MethodInvoker invoke = () =>
            {
                tabPageOverview.Enabled = false;
                tabPageReceiveTransaction.Enabled = false;
                tabPageSendTransaction.Enabled = false;
                tabPageStoreNetwork.Enabled = false;
                tabPageTransactionHistory.Enabled = false;
            };

            BeginInvoke(invoke);
        }

        /// <summary>
        /// Enable wallet tabs.
        /// </summary>
        private void EnableWalletTabs()
        {
            MethodInvoker invoke = () =>
            {
                tabPageOverview.Enabled = true;
                tabPageReceiveTransaction.Enabled = true;
                tabPageSendTransaction.Enabled = true;
                tabPageStoreNetwork.Enabled = true;
                tabPageTransactionHistory.Enabled = true;
            };

            BeginInvoke(invoke);

        }


        #endregion

        #region Transaction history panel events & functions.

        private bool _onDrawingTransactionHistory;
        private SemaphoreSlim _semaphoreSearchTransactionHistory = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Draw the bitmap of the transaction history virtually generated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelTransactionHistory_Paint(object sender, PaintEventArgs e)
        {
            _onDrawingTransactionHistory = true;

            if (tabControlWallet.SelectedTab == tabPageTransactionHistory)
            {
                if (_walletTransactionHistorySystemInstance != null)
                {

                    try
                    {
                        if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                        {
                            long countTransaction = _walletTransactionHistorySystemInstance.GetTransactionHistoryCountOfWalletFileOpened(_currentWalletFilename, _cancellationTokenTaskUpdateWalletContentInformations);

                            if (!_walletTransactionHistorySystemInstance.GetLoadStatus(_currentWalletFilename, out double percentProgress))
                            {
                                if (countTransaction == 0)
                                    _walletTransactionHistorySystemInstance.PaintTransactionLoadingAnimationToTransactionHistory(_currentWalletFilename,
                                         _walletMainFormLanguageObject.PANEL_TRANSACTION_HISTORY_NO_TRANSACTION_TEXT, 0, e.Graphics, panelTransactionHistory, true);
                                else
                                {
                                    Bitmap transactionHistoryBitmap = _walletTransactionHistorySystemInstance[_currentWalletFilename, _cancellationTokenTaskUpdateWalletContentInformations];

                                    if (transactionHistoryBitmap != null)
                                        e.Graphics.DrawImageUnscaled(transactionHistoryBitmap, 0, 0);

                                    _walletTransactionHistorySystemInstance.PaintTransactionHoverToTransactionHistory(_currentWalletFilename, e.Graphics, false);
                                    _walletTransactionHistorySystemInstance.PaintTransactionHoverToTransactionHistory(_currentWalletFilename, e.Graphics, true);
                                }

                            }
                            else
                                _walletTransactionHistorySystemInstance.PaintTransactionLoadingAnimationToTransactionHistory(_currentWalletFilename,
                                    countTransaction > 0 ? _walletMainFormLanguageObject.PANEL_TRANSACTION_HISTORY_ON_LOAD_TEXT : _walletMainFormLanguageObject.PANEL_TRANSACTION_HISTORY_NO_TRANSACTION_TEXT, percentProgress, e.Graphics, panelTransactionHistory, countTransaction == 0);
                        }
                        else
                            _walletTransactionHistorySystemInstance.PaintTransactionLoadingAnimationToTransactionHistory(_currentWalletFilename, _walletMainFormLanguageObject.PANEL_TRANSACTION_HISTORY_NO_TRANSACTION_TEXT, 0, e.Graphics, panelTransactionHistory, true);
                    }
                    catch
                    {
                        e.Graphics.Clear(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryBackgroundColorOnClear);
                    }
                }
            }

            _onDrawingTransactionHistory = false;
        }

        /// <summary>
        /// Draw borders on transaction history panel dedicated to columns.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelMainInterfaceTransactionHistoryColumns_Paint(object sender, PaintEventArgs e)
        {
            ClassGraphicsUtility.DrawBorderOnControl(e.Graphics, Color.DarkGray, panelTransactionHistoryColumns.Width, panelTransactionHistoryColumns.Height, 1f);
        }

        /// <summary>
        /// Try to draw the next page of the transaction history.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMainInterfaceNextPageTransactionHistory_Click(object sender, EventArgs e)
        {
            if (_walletTransactionHistorySystemInstance != null)
            {
                if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                {
                    if (!_walletTransactionHistorySystemInstance.GetLoadStatus(_currentWalletFilename, out _))
                    {
                        _walletTransactionHistorySystemInstance.NextPageTransactionHistory(_currentWalletFilename, _cancellationTokenTaskUpdateWalletContentInformations, out int currentPage);
                        textBoxMainInterfaceCurrentPageTransactionHistory.Text = currentPage.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Try to draw the previous page of the transaction history.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMainInterfaceBackPageTransactionHistory_Click(object sender, EventArgs e)
        {
            if (_walletTransactionHistorySystemInstance != null)
            {
                if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                {
                    if (!_walletTransactionHistorySystemInstance.GetLoadStatus(_currentWalletFilename, out _))
                    {
                        _walletTransactionHistorySystemInstance.BackPageTransactionHistory(_currentWalletFilename, _cancellationTokenTaskUpdateWalletContentInformations, out int currentPage);
                        textBoxMainInterfaceCurrentPageTransactionHistory.Text = currentPage.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Change the order by type of the transaction history.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelMainInterfaceTransactionHistoryColumns_Click(object sender, EventArgs e)
        {
            if (_walletTransactionHistorySystemInstance != null)
            {
                if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                {
                    if (!_walletTransactionHistorySystemInstance.GetLoadStatus(_currentWalletFilename, out _))
                    {
                        MouseEventArgs mouseEventArgs = (MouseEventArgs)e;

                        _walletTransactionHistorySystemInstance.SetOrderTypeTransactionHistory(_currentWalletFilename, mouseEventArgs.Location, panelTransactionHistoryColumns, _walletMainFormLanguageObject, _cancellationTokenTaskUpdateWalletContentInformations);
                    }
                }
            }
        }

        /// <summary>
        /// Enable an hover effect depending of the click location on a transaction showed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelTransactionHistory_Click(object sender, EventArgs e)
        {
            if (_walletTransactionHistorySystemInstance != null)
            {
                if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                {
                    Point currentCursorPosition = Cursor.Current != null ? Cursor.Position : new Point(0, 0);

                    if (!_walletTransactionHistorySystemInstance.GetLoadStatus(_currentWalletFilename, out _))
                        _walletTransactionHistorySystemInstance.EnableTransactionHoverByClick(_currentWalletFilename, panelTransactionHistory.PointToClient(currentCursorPosition), _cancellationTokenTaskUpdateWalletContentInformations, out string _, out Rectangle _);
                }
            }
        }

        /// <summary>
        /// Enable a transaction hover depending the mouse position inside of the transaction history.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelTransactionHistory_MouseMove(object sender, MouseEventArgs e)
        {
            if (_walletTransactionHistorySystemInstance != null)
            {
                if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                {
                    Point currentCursorPosition = Cursor.Current != null ? Cursor.Position : new Point(0, 0);

                    if (!_walletTransactionHistorySystemInstance.GetLoadStatus(_currentWalletFilename, out _))
                        _walletTransactionHistorySystemInstance.EnableTransactionHoverByPosition(_currentWalletFilename, panelTransactionHistory.PointToClient(currentCursorPosition), _cancellationTokenTaskUpdateWalletContentInformations, out _, out _);
                }
            }
        }

        /// <summary>
        /// Disable the transaction hover position inside of the transaction history once the mouse leave the transaction history.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelTransactionHistory_MouseLeave(object sender, EventArgs e)
        {
            if (_walletTransactionHistorySystemInstance != null)
            {
                if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                {
                    if (!_walletTransactionHistorySystemInstance.GetLoadStatus(_currentWalletFilename, out _))
                        _walletTransactionHistorySystemInstance.DisableTransactionHoverByPosition(_currentWalletFilename);
                }
            }
        }

        /// <summary>
        /// Try to show a transaction informations drawed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelTransactionHistory_DoubleClick(object sender, EventArgs e)
        {
            if (_walletTransactionHistorySystemInstance != null)
            {
                if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                {
                    if (!_walletTransactionHistorySystemInstance.GetLoadStatus(_currentWalletFilename, out _))
                    {
                        MouseEventArgs mouseEventArgs = (MouseEventArgs)e;

                        ClassBlockTransaction blockTransactionToShow = _walletTransactionHistorySystemInstance.GetBlockTransactionShowedFromClick(_currentWalletFilename, _cancellationTokenTaskUpdateWalletContentInformations, mouseEventArgs.Location, out bool found, out bool isMemPool);

                        if (found && blockTransactionToShow != null)
                        {
                            using (ClassWalletTransactionHistoryInformationInternalForm walletTransactionHistoryInformationInternalForm = new ClassWalletTransactionHistoryInformationInternalForm(new List<Tuple<bool, ClassBlockTransaction>>() { new Tuple<bool, ClassBlockTransaction>(isMemPool, blockTransactionToShow) }))
                                walletTransactionHistoryInformationInternalForm.ShowDialog(this);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set the current page of the transaction history to draw.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxMainInterfaceCurrentPageTransactionHistory_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (_walletTransactionHistorySystemInstance != null)
                {
                    if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
                    {
                        if (!_walletTransactionHistorySystemInstance.GetLoadStatus(_currentWalletFilename, out _))
                        {
                            bool error = !int.TryParse(textBoxMainInterfaceCurrentPageTransactionHistory.Text, out int inputPage);

                            if (!error)
                            {
                                int maxPage = _walletTransactionHistorySystemInstance.MaxPageTransactionHistory(_currentWalletFilename, _cancellationTokenTaskUpdateWalletContentInformations);

                                if (inputPage <= maxPage)
                                    _walletTransactionHistorySystemInstance.SetPageTransactionHistory(_currentWalletFilename, inputPage, _cancellationTokenTaskUpdateWalletContentInformations);
                                else
                                    error = true;
                            }
                            if (error)
                            {
                                int currentPage = _walletTransactionHistorySystemInstance.CurrentPageTransactionHistory(_currentWalletFilename, _cancellationTokenTaskUpdateWalletContentInformations);
                                textBoxMainInterfaceCurrentPageTransactionHistory.Text = currentPage.ToString();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Refresh the draw of the transaction history.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerRefreshTransactionHistory_Tick(object sender, EventArgs e)
        {
            if (tabControlWallet.SelectedTab == tabPageTransactionHistory && !_onDrawingTransactionHistory)
            {
                panelTransactionHistory.Invalidate(false);
                panelTransactionHistory.Update();
            }
        }


        /// <summary>
        /// Try to export the transaction history.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMainInterfaceExportTransactionHistory_Click(object sender, EventArgs e)
        {
            if (_walletTransactionHistorySystemInstance.ContainsTransactionHistoryToWalletFileOpened(_currentWalletFilename))
            {
                if (!_walletTransactionHistorySystemInstance.GetLoadStatus(_currentWalletFilename, out _))
                {
                    bool allTransaction = MessageBox.Show(_walletMainFormLanguageObject.MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_TEXT, _walletMainFormLanguageObject.MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_TITLE_TEXT, MessageBoxButtons.YesNo) == DialogResult.No;

                    bool continueExport = true;

                    if (allTransaction)
                        continueExport = MessageBox.Show(_walletMainFormLanguageObject.MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_ALL_NOTICE_TEXT, _walletMainFormLanguageObject.MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_TITLE_TEXT, MessageBoxButtons.YesNo) == DialogResult.Yes;

                    if (continueExport)
                    {
                        using (SaveFileDialog saveTransactionExportFileDialog = new SaveFileDialog()
                        {
                            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                            Filter = _walletMainFormLanguageObject.SAVEFILEDIALOG_TRANSACTION_HISTORY_EXPORT_TEXT,
                            FilterIndex = 1,
                            RestoreDirectory = true,
                            FileName = _currentWalletFilename + @"-transaction-history-export-" + ClassUtility.GetCurrentTimestampInSecond() + @".csv",
                        })
                        {
                            if (saveTransactionExportFileDialog.ShowDialog(this) == DialogResult.OK)
                            {
                                if (!saveTransactionExportFileDialog.FileName.IsNullOrEmpty(false, out _))
                                {
                                    if (_walletTransactionHistorySystemInstance.TryExportTransactionHistory(_currentWalletFilename, saveTransactionExportFileDialog.FileName, allTransaction, _walletMainFormLanguageObject, _cancellationTokenTaskUpdateWalletContentInformations))
                                        MessageBox.Show(_walletMainFormLanguageObject.MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_DONE_TEXT, _walletMainFormLanguageObject.MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_TITLE_TEXT);
                                    else
                                        MessageBox.Show(_walletMainFormLanguageObject.MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_FAILED_TEXT, _walletMainFormLanguageObject.MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_TITLE_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Execute the research inside of the transaction history once the user press the Enter key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxTransactionHistorySearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                DoTransactionHistoryResearch();
        }

        /// <summary>
        /// Execute the research inside of the transaction once the user click on the button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMainInterfaceSearchTransactionHistory_Click(object sender, EventArgs e)
        {
            DoTransactionHistoryResearch();
        }

        /// <summary>
        /// Try to research transaction inside of the transaction history.
        /// </summary>
        private void DoTransactionHistoryResearch()
        {
            bool semaphoreUsed = false;

            try
            {
                try
                {
                    _semaphoreSearchTransactionHistory.Wait(_cancellationTokenTaskUpdateWalletContentInformations.Token);
                    semaphoreUsed = true;

                    Task.Factory.StartNew(() =>
                    {
                        using (DisposableList<string> listTransactionHashFound = _walletTransactionHistorySystemInstance.TrySearchTransactionHistory(_currentWalletFilename, textBoxTransactionHistorySearch.Text, _cancellationTokenTaskUpdateWalletContentInformations, out bool foundElement))
                        {
                            if (foundElement)
                            {
                                DisposableList<Tuple<bool, ClassBlockTransaction>> listBlockTransaction = new DisposableList<Tuple<bool, ClassBlockTransaction>>();

                                string currentWalletAddress = ClassDesktopWalletCommonData.WalletDatabase.GetWalletAddressFromWalletFileName(_currentWalletFilename);

                                foreach (string transactionHash in listTransactionHashFound.GetList)
                                {
                                    Tuple<bool, ClassBlockTransaction> blockTransactionTuple = ClassDesktopWalletCommonData.WalletSyncSystem.GetTransactionObjectFromSync(currentWalletAddress, transactionHash, 0, false, _cancellationTokenTaskUpdateWalletContentInformations).Result;
                                    if (blockTransactionTuple?.Item2 != null)
                                        listBlockTransaction.Add(blockTransactionTuple);
                                }

                                if (listBlockTransaction.Count > 0)
                                {
                                    MethodInvoker invoke = () =>
                                    {
                                        using (ClassWalletTransactionHistoryInformationInternalForm walletTransactionHistoryInformationInternalForm = new ClassWalletTransactionHistoryInformationInternalForm(listBlockTransaction.GetList))
                                            walletTransactionHistoryInformationInternalForm.ShowDialog(this);

                                        listBlockTransaction.Clear();
                                    };
                                    BeginInvoke(invoke);
                                }

                            }
                            else
                            {
                                MethodInvoker invoke = () =>
                                {
                                    MessageBox.Show(this, _walletMainFormLanguageObject.MESSAGEBOX_TRANSACTION_HISTORY_SEARCH_NOTHING_FOUND_TEXT.Replace("%s", textBoxTransactionHistorySearch.Text));
                                };
                                BeginInvoke(invoke);
                            }
                        }
                    }, _cancellationTokenTaskUpdateWalletContentInformations.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                }
                catch
                {
                    // Catch the exception once the task is cancelled.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreSearchTransactionHistory.Release();
            }
        }

        #endregion

        #region Recent transaction history panel events & functions.

        /// <summary>
        /// Update recent transaction draw.
        /// </summary>
        private void UpdateRecentTransactionDraw()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    panelInternalRecentTransactions.Refresh();
                    await Task.Delay(1000);
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Draw recent transactions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelInternalRecentTransactions_Paint(object sender, PaintEventArgs e)
        {
            try
            {

                if (tabControlWallet.SelectedTab == tabPageOverview)
                {
                    Bitmap bitmap = _walletRecentTransactionHistorySystemInstance.GetRecentTransactionHistoryBitmap(_cancellationTokenTaskUpdateWalletContentInformations);

                    if (bitmap != null)
                    {
                        e.Graphics.DrawImageUnscaled(bitmap, 0, 0);

                        Point currentCursorPosition = Cursor.Current != null ? Cursor.Position : new Point(0, 0);

                        Point point = panelInternalRecentTransactions.PointToClient(currentCursorPosition);

                        using (DisposableList<ClassRecentTransactionHistoryObject> disposableRecentTransactionHistoryObjects = new DisposableList<ClassRecentTransactionHistoryObject>(false, 0, _walletRecentTransactionHistorySystemInstance.DictionaryRecentTransactionHistoryObjects.Values.ToList()))
                        {
                            foreach (var showedRecentTransactionHistoryObject in disposableRecentTransactionHistoryObjects.GetList)
                            {
                                if (_cancellationTokenTaskUpdateWalletContentInformations.IsCancellationRequested)
                                    break;

                                if (showedRecentTransactionHistoryObject == null ||
                                    !showedRecentTransactionHistoryObject.TransactionDrawRectangle.Contains(point))
                                    continue;

                                e.Graphics.DrawRectangle(new Pen(ClassWalletDefaultSetting.DefaultPictureBoxTransactionBorderColor, 1.0f), showedRecentTransactionHistoryObject.TransactionDrawRectangle.X, showedRecentTransactionHistoryObject.TransactionDrawRectangle.Y, showedRecentTransactionHistoryObject.TransactionDrawRectangle.Width - 2, showedRecentTransactionHistoryObject.TransactionDrawRectangle.Height - 2);
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // e.Graphics.Clear(ClassWalletDefaultSetting.DefaultRecentTransactionBackColor);
            }
        }

        /// <summary>
        /// Detect if the mouse enter inside of the recent transaction history, draw borders on recent transaction if one of them contain the mouse position 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelInternalRecentTransactions_MouseEnter(object sender, EventArgs e)
        {
            if (_walletRecentTransactionHistorySystemInstance != null)
            {
                Point currentCursorPosition = Cursor.Current != null ? Cursor.Position : new Point(0, 0);

                if (_walletRecentTransactionHistorySystemInstance.UpdateLastMousePosition(panelInternalRecentTransactions.PointToClient(currentCursorPosition)))
                    panelInternalRecentTransactions.Refresh();

            }
        }

        /// <summary>
        /// Detect if the mouse is inside of the recent transaction history, draw borders on recent transaction if one of them contain the mouse position 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelInternalRecentTransactions_MouseHover(object sender, EventArgs e)
        {
            if (_walletRecentTransactionHistorySystemInstance != null)
            {
                Point currentCursorPosition = Cursor.Current != null ? Cursor.Position : new Point(0, 0);

                if (_walletRecentTransactionHistorySystemInstance.UpdateLastMousePosition(panelInternalRecentTransactions.PointToClient(currentCursorPosition)))
                    panelInternalRecentTransactions.Refresh();

            }
        }

        private bool _enableRecentTransactionHover;

        /// <summary>
        /// Detect if the mouse moving inside of the recent transaction history, draw borders on recent transaction if one of them contain the mouse position 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelInternalRecentTransactions_MouseMove(object sender, MouseEventArgs e)
        {
            if (_walletRecentTransactionHistorySystemInstance != null)
            {
                Point currentCursorPosition = Cursor.Current != null ? Cursor.Position : new Point(0, 0);

                if (_walletRecentTransactionHistorySystemInstance.UpdateLastMousePosition(panelInternalRecentTransactions.PointToClient(currentCursorPosition)))
                {
                    _enableRecentTransactionHover = true;
                    panelInternalRecentTransactions.Refresh();
                }
                else
                {
                    if (_enableRecentTransactionHover)
                    {
                        _enableRecentTransactionHover = false;
                        panelInternalRecentTransactions.Refresh();
                    }
                }
            }
        }

        /// <summary>
        /// Detect if the mouse leave out of the recent transaction history, draw borders on recent transaction if one of them contain the mouse position 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelInternalRecentTransactions_MouseLeave(object sender, EventArgs e)
        {
            if (_walletRecentTransactionHistorySystemInstance != null)
            {
                Point currentCursorPosition = Cursor.Current != null ? Cursor.Position : new Point(0, 0);

                if (!_walletRecentTransactionHistorySystemInstance.UpdateLastMousePosition(panelInternalRecentTransactions.PointToClient(currentCursorPosition)))
                    panelInternalRecentTransactions.Refresh();

            }
        }

        /// <summary>
        /// Show a transaction informations by clicking on a recent transaction drawed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelInternalRecentTransactions_Click(object sender, EventArgs e)
        {
            if (_walletRecentTransactionHistorySystemInstance != null)
            {
                Point currentCursorPosition = Cursor.Current != null ? Cursor.Position : new Point(0, 0);

                string walletAddress = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletAddress;

                _walletRecentTransactionHistorySystemInstance.GetBlockTransactionByClick(panelInternalRecentTransactions.PointToClient(currentCursorPosition), walletAddress, _cancellationTokenTaskUpdateWalletContentInformations, out ClassBlockTransaction blockTransaction, out bool isMemPool);

                if (blockTransaction?.TransactionObject != null)
                {
                    using (ClassWalletTransactionHistoryInformationInternalForm walletTransactionHistoryInformationInternalForm = new ClassWalletTransactionHistoryInformationInternalForm(new List<Tuple<bool, ClassBlockTransaction>>() { new Tuple<bool, ClassBlockTransaction>(isMemPool, blockTransaction) }))
                        walletTransactionHistoryInformationInternalForm.ShowDialog(this);

                    panelInternalRecentTransactions.ResetCursor();
                    panelInternalRecentTransactions.Refresh();
                }
            }
        }

        #endregion

        #region Send transaction.

        /// <summary>
        /// Keep previous input amount selected. Handle firing event.
        /// </summary>
        private string _sendTransactionAmountSelectedText = string.Empty;
        private bool _sendTransactionOnCalculationFeeCost;

        /// <summary>
        /// Check the wallet address target validity by changing the forecolor of the text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxSendTransactionWalletAddressTarget_TextChanged(object sender, EventArgs e)
        {
            int textLength = textBoxSendTransactionWalletAddressTarget.Text.Length;
            string currentWalletAddress = ClassDesktopWalletCommonData.WalletDatabase.GetWalletAddressFromWalletFileName(_currentWalletFilename);
            if (textLength >= BlockchainSetting.WalletAddressWifLengthMin && textLength <= BlockchainSetting.WalletAddressWifLengthMax)
                textBoxSendTransactionWalletAddressTarget.ForeColor = ClassWalletUtility.CheckWalletAddress(textBoxSendTransactionWalletAddressTarget.Text) && textBoxSendTransactionWalletAddressTarget.Text != currentWalletAddress ? Color.Green : Color.Red;
            else
                textBoxSendTransactionWalletAddressTarget.ForeColor = Color.Black;
        }

        /// <summary>
        /// Check the amount to send selected before to proceed it, change the forecolor depending the amount selected compared with the available balance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxSendTransactionAmountSelected_KeyUp(object sender, KeyEventArgs e)
        {

            if (_cancellationTokenTaskUpdateWalletContentInformations != null)
            {
                if (!_sendTransactionOnCalculationFeeCost)
                {
                    if (_sendTransactionAmountSelectedText != textBoxSendTransactionAmountSelected.Text)
                    {

                        _sendTransactionAmountSelectedText = textBoxSendTransactionAmountSelected.Text;
                        _sendTransactionOnCalculationFeeCost = true;
                        UpdateTransactionConfirmationTimeEstimated();
                        try
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                await UpdateTransactionFeeCostEstimated();
                            }, _cancellationTokenTaskUpdateWalletContentInformations.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ignored cathc the exception once the task is cancelled.
                        }
                        _sendTransactionOnCalculationFeeCost = false;

                    }
                }
            }

        }

        private void textBoxSendTransactionWalletAddressTarget_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void textBoxSendTransactionAmountSelected_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void textBoxSendTransactionConfirmationsCountTarget_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void textBoxSendTransactionPaymentId_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }


        /// <summary>
        /// Update the fee cost calculated depending of the amount of confirmations target.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxSendTransactionConfirmationsCountTarget_TextChanged(object sender, EventArgs e)
        {
            if (!_sendTransactionOnCalculationFeeCost)
            {
                if (_cancellationTokenTaskUpdateWalletContentInformations != null)
                {
                    _sendTransactionAmountSelectedText = textBoxSendTransactionAmountSelected.Text;
                    _sendTransactionOnCalculationFeeCost = true;
                    UpdateTransactionConfirmationTimeEstimated();
                    try
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            await UpdateTransactionFeeCostEstimated();
                        }, _cancellationTokenTaskUpdateWalletContentInformations.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignored catch the exception once the task is cancelled.
                    }
                    _sendTransactionOnCalculationFeeCost = false;
                }
            }
        }

        /// <summary>
        /// Update the fee cost estimated.
        /// </summary>
        private async Task UpdateTransactionFeeCostEstimated()
        {

            if (!textBoxSendTransactionAmountSelected.Text.IsNullOrEmpty(false, out _))
            {
                if (decimal.TryParse(textBoxSendTransactionAmountSelected.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amountSelected))
                {

                    BigInteger walletAvailableBalanceBigInteger = ClassDesktopWalletCommonData.WalletSyncSystem.GetWalletAvailableBalanceFromSyncedData(_currentWalletFilename);

                    decimal walletAvailableBalance = (decimal)walletAvailableBalanceBigInteger / BlockchainSetting.CoinDecimal;
                    MethodInvoker invoke = () =>
                    {

                        if (amountSelected < walletAvailableBalance)
                            textBoxSendTransactionAmountSelected.ForeColor = Color.Green;

                        else if (amountSelected == walletAvailableBalance)
                            textBoxSendTransactionAmountSelected.ForeColor = Color.DarkOrange;

                        else if (amountSelected > walletAvailableBalance)
                            textBoxSendTransactionAmountSelected.ForeColor = Color.Red;
                    };
                    BeginInvoke(invoke);

                    if (int.TryParse(textBoxSendTransactionConfirmationsCountTarget.Text, out int totalConfirmationsTarget))
                    {
                        invoke = () => textBoxSendTransactionConfirmationsCountTarget.ForeColor = Color.Black;
                        BeginInvoke(invoke);

                        if (totalConfirmationsTarget >= BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations && totalConfirmationsTarget <= BlockchainSetting.TransactionMandatoryMaxBlockTransactionConfirmations)
                        {
                            ClassSendTransactionFeeCostCalculationResultObject sendTransactionFeeCostCalculationResult = await ClassDesktopWalletCommonData.WalletSyncSystem.GetTransactionFeeCostVirtuallyFromSync(_currentWalletFilename, amountSelected, totalConfirmationsTarget, _cancellationTokenTaskUpdateWalletContentInformations);

                            invoke = () =>
                            {
                                if (!sendTransactionFeeCostCalculationResult.Failed)
                                {
                                    BigInteger sumToSpend = sendTransactionFeeCostCalculationResult.TotalFeeCost + (BigInteger)(amountSelected * BlockchainSetting.CoinDecimal);

                                    if (sumToSpend > walletAvailableBalanceBigInteger)
                                    {
                                        textBoxSendTransactionAmountToSpend.Text = string.Empty;
                                        textBoxSendTransactionAmountSelected.ForeColor = Color.Red;
                                    }
                                    else
                                    {
                                        textBoxSendTransactionTotalAmountSource.Text = sendTransactionFeeCostCalculationResult.TransactionAmountSourceList.Count.ToString();
                                        textBoxSendTransactionFeeCalculated.Text = ClassTransactionUtility.GetFormattedAmountFromBigInteger(sendTransactionFeeCostCalculationResult.TotalFeeCost);
                                        textBoxSendTransactionFeeSizeCost.Text = ClassTransactionUtility.GetFormattedAmountFromBigInteger(sendTransactionFeeCostCalculationResult.FeeSizeCost);
                                        textBoxSendTransactionFeeConfirmationCost.Text = ClassTransactionUtility.GetFormattedAmountFromBigInteger(sendTransactionFeeCostCalculationResult.FeeConfirmationCost);
                                        textBoxSendTransactionAmountToSpend.Text = ClassTransactionUtility.GetFormattedAmountFromBigInteger(sumToSpend) + _walletMainFormLanguageObject.TEXT_SPACE + BlockchainSetting.CoinTickerName;
                                    }
                                }
                                else
                                    CleanAmountAndFeeEstimations(true);
                            };
                            BeginInvoke(invoke);
                        }
                        else
                        {
                            invoke = () =>
                            {
                                CleanAmountAndFeeEstimations(true);
                                textBoxSendTransactionConfirmationsCountTarget.ForeColor = Color.Red;
                            };
                            BeginInvoke(invoke);
                        }
                    }
                    else
                    {
                        invoke = () =>
                        {
                            CleanAmountAndFeeEstimations(false);
                            textBoxSendTransactionConfirmationsCountTarget.ForeColor = Color.Red;
                        };
                        BeginInvoke(invoke);
                    }
                }
                else
                {
                    MethodInvoker invoke = () =>
                    {
                        CleanAmountAndFeeEstimations(false);
                        textBoxSendTransactionConfirmationsCountTarget.ForeColor = Color.Red;
                    };
                    BeginInvoke(invoke);
                }
            }
            else
            {
                MethodInvoker invoke = () =>
                {
                    CleanAmountAndFeeEstimations(false);
                    textBoxSendTransactionConfirmationsCountTarget.ForeColor = Color.Red;
                };
                BeginInvoke(invoke);
            }
        }

        /// <summary>
        /// Clean amount(s) and fee(s) estimated.
        /// </summary>
        /// <param name="error"></param>
        private void CleanAmountAndFeeEstimations(bool error)
        {
            if (error)
                textBoxSendTransactionFeeCalculated.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_FAILED_TEXT;
            else
                textBoxSendTransactionFeeCalculated.Text = string.Empty;

            textBoxSendTransactionAmountToSpend.Text = string.Empty;
            textBoxSendTransactionTotalAmountSource.Text = string.Empty;
            textBoxSendTransactionFeeSizeCost.Text = string.Empty;
            textBoxSendTransactionFeeConfirmationCost.Text = string.Empty;
        }

        /// <summary>
        /// Calculate the time estimated on confirmations count selected.
        /// </summary>
        private void UpdateTransactionConfirmationTimeEstimated()
        {
            if (_walletMainFormLanguageObject != null)
            {
                if (!textBoxSendTransactionConfirmationsCountTarget.Text.IsNullOrEmpty(false, out _))
                {
                    if (textBoxSendTransactionFeeCalculated.Text != _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_FAILED_TEXT)
                    {
                        if (int.TryParse(textBoxSendTransactionConfirmationsCountTarget.Text, out int totalConfirmationsTarget))
                        {
                            textBoxSendTransactionConfirmationsCountTarget.ForeColor = Color.Black;

                            double n = BlockchainSetting.BlockTime * totalConfirmationsTarget;

                            if (n < 60)
                            {
                                n /= 60;
                                int totalSeconds = (int)n;
                                labelSendTransactionConfirmationTimeEstimated.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_TEXT + totalSeconds + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_SECONDS_TEXT;
                            }
                            else if (n >= 60 && n < 3600)
                            {
                                double totalMinutes = (n / 60);
                                double totalSeconds = (n - (totalMinutes * 60)) / 60;

                                labelSendTransactionConfirmationTimeEstimated.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_TEXT +
                                                                                     (int)totalMinutes + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_MINUTES_TEXT + _walletMainFormLanguageObject.TEXT_SPACE +
                                                                                     (int)totalSeconds + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_SECONDS_TEXT;
                            }
                            else if (n >= 3600 && n < 86400)
                            {

                                double totalHours = (n / 3600);
                                double totalMinutes = ((n - ((int)totalHours * 3600)) / 60);
                                double totalSeconds = ((((n - ((int)totalHours * 3600))) - ((int)totalMinutes * 60)) / 60);

                                labelSendTransactionConfirmationTimeEstimated.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_TEXT +
                                                                                     (int)totalHours + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_HOURS_TEXT + _walletMainFormLanguageObject.TEXT_SPACE +
                                                                                     (int)totalMinutes + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_MINUTES_TEXT + _walletMainFormLanguageObject.TEXT_SPACE +
                                                                                     (int)totalSeconds + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_SECONDS_TEXT;
                            }
                            else
                            {
                                double totalDays = (n / 86400d);
                                double totalHours = ((n - ((int)totalDays * 86400d)) / 3600d);
                                double totalMinutes = (((n - ((int)totalDays * 86400d)) - ((int)totalHours * 3600d)) / 60d);
                                double totalSeconds = ((((n - ((int)totalDays * 86400d)) - ((int)totalHours * 3600d)) - ((int)totalMinutes * 60d)) / 60d);

                                labelSendTransactionConfirmationTimeEstimated.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_TEXT +
                                                                                     (int)totalDays + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_DAYS_TEXT + _walletMainFormLanguageObject.TEXT_SPACE +
                                                                                     (int)totalHours + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_HOURS_TEXT + _walletMainFormLanguageObject.TEXT_SPACE +
                                                                                     (int)totalMinutes + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_MINUTES_TEXT + _walletMainFormLanguageObject.TEXT_SPACE +
                                                                                     (int)totalSeconds + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_SECONDS_TEXT;
                            }
                        }
                        else
                        {
                            textBoxSendTransactionFeeCalculated.Text = _walletMainFormLanguageObject.LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_FAILED_TEXT;
                            textBoxSendTransactionConfirmationsCountTarget.ForeColor = Color.Red;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Start a task to send a transaction.
        /// </summary>
        private ClassEnumSendTransactionResult StartSendTransactionTask()
        {

            if (textBoxSendTransactionWalletAddressTarget.Text.IsNullOrEmpty(false, out _))
                return ClassEnumSendTransactionResult.SEND_FAILED;

            if (!ClassWalletUtility.CheckWalletAddress(textBoxSendTransactionWalletAddressTarget.Text))
                return ClassEnumSendTransactionResult.SEND_FAILED;

            if (textBoxSendTransactionAmountSelected.Text.IsNullOrEmpty(false, out _))
                return ClassEnumSendTransactionResult.SEND_FAILED;

            if (textBoxSendTransactionFeeCalculated.Text.IsNullOrEmpty(false, out _))
                return ClassEnumSendTransactionResult.SEND_FAILED;

            string walletAddress = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletAddress;

            if (walletAddress == textBoxSendTransactionWalletAddressTarget.Text)
                return ClassEnumSendTransactionResult.SEND_FAILED;

            if (!int.TryParse(textBoxSendTransactionConfirmationsCountTarget.Text, out int totalConfirmationsTarget))
                return ClassEnumSendTransactionResult.SEND_FAILED;

            if (totalConfirmationsTarget < BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations || totalConfirmationsTarget > BlockchainSetting.TransactionMandatoryMaxBlockTransactionConfirmations)
                return ClassEnumSendTransactionResult.SEND_FAILED;

            if (!decimal.TryParse(textBoxSendTransactionAmountSelected.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amountToSpend))
                return ClassEnumSendTransactionResult.SEND_FAILED;

            Task<ClassSendTransactionFeeCostCalculationResultObject> taskSendTransactionFeeCostCalculationResult;

            try
            {
                taskSendTransactionFeeCostCalculationResult = ClassDesktopWalletCommonData.WalletSyncSystem.GetTransactionFeeCostVirtuallyFromSync(_currentWalletFilename, amountToSpend, totalConfirmationsTarget, CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenTaskUpdateWalletContentInformations.Token, new CancellationTokenSource(5000).Token));
                taskSendTransactionFeeCostCalculationResult.Wait(CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenTaskUpdateWalletContentInformations.Token, new CancellationTokenSource(5000).Token).Token);
            }
            catch
            {
                return ClassEnumSendTransactionResult.SEND_FAILED;
            }

            if (taskSendTransactionFeeCostCalculationResult.IsCanceled || taskSendTransactionFeeCostCalculationResult.IsFaulted || !taskSendTransactionFeeCostCalculationResult.IsCompleted)
                return ClassEnumSendTransactionResult.SEND_FAILED;

            ClassSendTransactionFeeCostCalculationResultObject sendTransactionFeeCostCalculationResult = taskSendTransactionFeeCostCalculationResult.Result;

            if (sendTransactionFeeCostCalculationResult.Failed)
                return ClassEnumSendTransactionResult.SEND_FAILED;

            decimal feeToPay = (decimal)sendTransactionFeeCostCalculationResult.TotalFeeCost / BlockchainSetting.CoinDecimal;

            BigInteger walletAvailableBalanceBigInteger = ClassDesktopWalletCommonData.WalletSyncSystem.GetWalletAvailableBalanceFromSyncedData(_currentWalletFilename);

            decimal walletAvailableBalance = (decimal)walletAvailableBalanceBigInteger / BlockchainSetting.CoinDecimal;

            if (amountToSpend + feeToPay > walletAvailableBalance)
                return ClassEnumSendTransactionResult.SEND_FAILED;

            BigInteger amountToSpendDecimals = (BigInteger)(amountToSpend * BlockchainSetting.CoinDecimal);

            BigInteger sumToSpend = amountToSpendDecimals + sendTransactionFeeCostCalculationResult.TotalFeeCost;

            if (sumToSpend > walletAvailableBalanceBigInteger)
                return ClassEnumSendTransactionResult.SEND_FAILED;

            string walletPrivateKey;

            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletEncrypted)
            {
                using (ClassWalletSendTransactionPassphraseForm sendTransactionPassphraseForm = new ClassWalletSendTransactionPassphraseForm(_currentWalletFilename))
                {
                    sendTransactionPassphraseForm.ShowDialog(this);

                    if (!sendTransactionPassphraseForm.WalletDecryptPrivateKeyResultStatus)
                        return ClassEnumSendTransactionResult.SEND_FAILED;

                    walletPrivateKey = sendTransactionPassphraseForm.WalletDecryptedPrivateKey;
                }
            }
            else
                walletPrivateKey = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletPrivateKey;

            if (walletPrivateKey.IsNullOrEmpty(false, out _))
                return ClassEnumSendTransactionResult.SEND_FAILED;

            bool doSendTransactionTask;

            using (ClassWalletSendTransactionConfirmationForm walletSendTransactionConfirmationForm = new ClassWalletSendTransactionConfirmationForm(amountToSpendDecimals, sendTransactionFeeCostCalculationResult.TotalFeeCost, textBoxSendTransactionWalletAddressTarget.Text))
            {
                walletSendTransactionConfirmationForm.ShowDialog(this);

                doSendTransactionTask = walletSendTransactionConfirmationForm.SendTransactionConfirmationStatus;
            }

            if (!doSendTransactionTask)
            {
                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletEncrypted)
                    walletPrivateKey.Clear();

                return ClassEnumSendTransactionResult.SEND_CANCELLED;
            }

            if (!long.TryParse(textBoxSendTransactionPaymentId.Text, out long paymendId))
                paymendId = 0;

            using (ClassWalletSendTransactionWaitRequestForm walletSendTransactionWaitRequestForm = new ClassWalletSendTransactionWaitRequestForm(_currentWalletFilename, textBoxSendTransactionWalletAddressTarget.Text, amountToSpend, feeToPay, paymendId, totalConfirmationsTarget, walletPrivateKey, sendTransactionFeeCostCalculationResult.TransactionAmountSourceList, CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenTaskUpdateWalletContentInformations.Token, new CancellationTokenSource(ClassWalletDefaultSetting.DefaultWalletSendTransactionMaxDelayRequest).Token)))
            {
                walletSendTransactionWaitRequestForm.ShowDialog(this);

                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletEncrypted)
                    walletPrivateKey.Clear();

                if (_cancellationTokenTaskUpdateWalletContentInformations.IsCancellationRequested)
                    return ClassEnumSendTransactionResult.SEND_FAILED;

                if (!walletSendTransactionWaitRequestForm.SendTransactionStatus)
                {
                    MessageBox.Show(_walletMainFormLanguageObject.MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_NETWORK_ERROR_CONTENT_TEXT, _walletMainFormLanguageObject.MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_NETWORK_ERROR_TITLE_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return ClassEnumSendTransactionResult.SEND_CANCELLED;
                }

            }
            MessageBox.Show(_walletMainFormLanguageObject.MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_NETWORK_SUCCESS_CONTENT_TEXT, _walletMainFormLanguageObject.MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_NETWORK_SUCCESS_TITLE_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Information);

            return ClassEnumSendTransactionResult.SEND_SUCCESS;
        }

        /// <summary>
        /// Start the process of sending the transaction.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSendTransactionDoProcess_Click(object sender, EventArgs e)
        {
            var sendTransactionResult = StartSendTransactionTask();

            if (sendTransactionResult == ClassEnumSendTransactionResult.SEND_FAILED)
                MessageBox.Show(_walletMainFormLanguageObject.MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_ERROR_CONTENT_TEXT, _walletMainFormLanguageObject.MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_ERROR_TITLE_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
            else if (sendTransactionResult == ClassEnumSendTransactionResult.SEND_SUCCESS)
            {
                try
                {
                    Task.Factory.StartNew(async () =>
                    {
                        await UpdateTransactionFeeCostEstimated();
                    }, _cancellationTokenTaskUpdateWalletContentInformations.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                }
                catch
                {
                    // Ignored, catch the exception once the task is cancelled.
                }
            }
        }

        #endregion

        #region Receive transaction.

        /// <summary>
        /// Copy the wallet address showed by click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelWalletAddressReceiveTransaction_Click(object sender, EventArgs e)
        {

            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.ContainsKey(_currentWalletFilename))
            {
                string walletAddress = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletAddress;
                _semaphoreCopyWalletAddressClickEvent.Wait();

                Task.Factory.StartNew(async () =>
                {

                    MethodInvoker invoke = async () =>
                    {
                        // This copy permit to ensure to not link text edited to the object wallet address string to show.
                        string walletAddressCopy = walletAddress.CopyBase58String(true);
                        string walletAddressCopyText = walletAddress.CopyBase58String(true);

                        try
                        {

                            Clipboard.SetText(walletAddressCopy);

                            labelWalletAddressReceiveTransaction.ForeColor = ClassWalletDefaultSetting.DefaultLabelWalletAddressEventCopyForeColor;

                            labelWalletAddressReceiveTransaction.Text = walletAddressCopyText + _walletMainFormLanguageObject.TEXT_SPACE + _walletMainFormLanguageObject.LABEL_MAIN_INTERFACE_WALLET_ADDRESS_EVENT_COPY_TEXT;

                            await Task.Delay(ClassWalletDefaultSetting.DefaultLabelWalletAddressEventCopyAwaitDelay);

                            labelWalletAddressReceiveTransaction.ForeColor = ClassWalletDefaultSetting.DefaultLabelWalletAddressEventCopyOriginalForeColor;
                            labelWalletAddressReceiveTransaction.Text = walletAddressCopyText;
                        }
                        catch
                        {
                            labelWalletAddressReceiveTransaction.ForeColor = ClassWalletDefaultSetting.DefaultLabelWalletAddressEventCopyOriginalForeColor;
                            labelWalletAddressReceiveTransaction.Text = walletAddressCopyText;
                        }

                    };


                    var result = BeginInvoke(invoke);

                    while (!result.IsCompleted)
                    {
                        if (!ClassDesktopWalletCommonData.DesktopWalletStarted)
                            break;

                        await Task.Delay(ClassWalletDefaultSetting.DefaultLabelWalletAddressEventCopyAwaitDelay, _cancellationTokenTaskUpdateWalletContentInformations.Token);
                    }


                    _semaphoreCopyWalletAddressClickEvent.Release();

                }, _cancellationTokenTaskUpdateWalletContentInformations.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }

        }

        /// <summary>
        /// Save the wallet address converted into a QR Code picture to a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSaveQrCodeReceiveTransactionWalletAddress_Click(object sender, EventArgs e)
        {
            if (panelQrCodeWalletAddress.BackgroundImage != null)
            {
                using (SaveFileDialog saveQrCodeFileDialog = new SaveFileDialog()
                {
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    Filter = _walletMainFormLanguageObject.SAVEFILEDIALOG_MAIN_INTERFACE_WALLET_ADDRESS_QR_CODE_TEXT,
                    FilterIndex = 1,
                    RestoreDirectory = true,
                    FileName = _currentWalletFilename + @"-address-qr-code.png",
                })
                {
                    if (saveQrCodeFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        if (!saveQrCodeFileDialog.FileName.IsNullOrEmpty(false, out _))
                            panelQrCodeWalletAddress.BackgroundImage.Save(saveQrCodeFileDialog.FileName, ImageFormat.Png);
                    }
                }
            }
        }

        /// <summary>
        /// Print the wallet address converted into a QR Code picture.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPrintQrCodeReceiveTransactionWalletAddress_Click(object sender, EventArgs e)
        {
            using (PrintPreviewDialog printPreviewQrCodeDialog = new PrintPreviewDialog())
            {
                using (PrintDocument printDocument = new PrintDocument())
                {
                    printDocument.PrintPage += PrintDocumentWalletAddressQrCode_Event;
                    printPreviewQrCodeDialog.Document = printDocument;
                    printPreviewQrCodeDialog.ShowDialog(this);
                }
            }
        }

        /// <summary>
        /// Print document wallet address qr code paint event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintDocumentWalletAddressQrCode_Event(object sender, PrintPageEventArgs e)
        {
            e.Graphics.DrawImageUnscaled(new Bitmap(panelQrCodeWalletAddress.BackgroundImage), 0, 0);
        }

        #endregion

        #region Graphics UI improvements.

        /// <summary>
        /// Draw borders.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelRecentTransactions_Paint(object sender, PaintEventArgs e)
        {
            ClassGraphicsUtility.DrawShadowOnListGraphicContentTarget(this, _listRecentTransactionHistoryPanelControlShadow, e.Graphics, 40, 30, _recentTransactionHistoryPanelShadowBitmap, out _recentTransactionHistoryPanelShadowBitmap);
        }

        /// <summary>
        /// Draw shadows on custom panel of the overview tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabPageOverview_Paint(object sender, PaintEventArgs e)
        {
            ClassGraphicsUtility.DrawShadowOnListGraphicContentTarget(tabPageOverview, _listOverviewPanelControlShadow, e.Graphics, 45, 30, _overviewPanelShadowBitmap, out _overviewPanelShadowBitmap);
        }

        /// <summary>
        /// Draw shadows on custom panel of the send transaction tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabPageSendTransaction_Paint(object sender, PaintEventArgs e)
        {
            ClassGraphicsUtility.DrawShadowOnListGraphicContentTarget(tabPageSendTransaction, _listSendTransactionPanelControlShadow, e.Graphics, 50, 50, _sendTransactionPanelShadowBitmap, out _sendTransactionPanelShadowBitmap);
        }

        /// <summary>
        /// Draw shadows on textbox inside of the panel transaction details.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelSendTransactionDetails_Paint(object sender, PaintEventArgs e)
        {
            ClassGraphicsUtility.DrawShadowOnListGraphicContentTarget(panelSendTransactionDetails, _listSendTransactionDetailsPanelControlShadow, e.Graphics, 30, 15, _sendTransactionDetailsPanelShadowBitmap, out _sendTransactionDetailsPanelShadowBitmap);
        }

        /// <summary>
        /// Draw shadows on custom panel of the transaction history tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabPageTransactionHistory_Paint(object sender, PaintEventArgs e)
        {
            ClassGraphicsUtility.DrawShadowOnListGraphicContentTarget(tabPageTransactionHistory, _listTransactionHistoryPanelControlShadow, e.Graphics, 55, 30, _transactionHistoryPanelShadowBitmap, out _transactionHistoryPanelShadowBitmap);
        }

        /// <summary>
        /// Draw shadows on controls selected inside of the main interface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletMainInterfaceForm_Paint(object sender, PaintEventArgs e)
        {
            ClassGraphicsUtility.DrawShadowOnListGraphicContentTarget(this, _listMainInterfaceControlShadow, e.Graphics, 50, 50, _mainInterfaceShadowBitmap, out _mainInterfaceShadowBitmap);
        }

        #endregion

        /// <summary>
        /// Open the setting form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ClassWalletSettingForm walletSettingForm = new ClassWalletSettingForm())
                walletSettingForm.ShowDialog();
        }


        /// <summary>
        /// Select a node from the list of public nodes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewWebNode_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;

            Debug.WriteLine("Test 1.");

            _webBrowserStoreNetwork.LoadUrl("http://" + item.Text + "/");
        }

        private void listViewWebNode_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            ListViewItem item = sender as ListViewItem;

            Debug.WriteLine("Test 2.");

            _webBrowserStoreNetwork.LoadUrl("http://" + item.Text + "/");
        }

        private void listViewWebNode_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                string target = listViewWebNode.SelectedItems[0].Text;

                Debug.WriteLine("Test 3. Target: " + target);

                _webBrowserStoreNetwork.LoadUrl("http://" + target + "/");
            }
            catch
            {
                // Ignored, catch the exception if any item is selected.
            }
        }

        #region INI Resize Responsive Form .-~^

        /// <summary>Ajusta y ordena los coponentesen base a los valores iniciales, que son los que darán sentido a la nueva composición</summary>
        /// <param name="f1">Formulario de aplicación</param>
        private void adaptResponsiveFormControlsToFormSize(Form f1, ClassViewStrategiesEnum strategy)
        {
            if (f1.Tag is ClassDataContextForm)
            {
                ClassDataContextForm context = (ClassDataContextForm)f1.Tag;

                if (context.FormContainerResponsiveData != null && context.FormContainerResponsiveData.ControlsCompData != null &&
                   context.FormContainerResponsiveData.ControlsCompData.Count > 0)
                {
                    ClassGraphicsUtility.RecursiveAdaptResponsiveFormControlsToParentSize(
                        context.FormContainerResponsiveData.ControlsCompData,
                        f1, strategy, false);
                }
            }
        }

        private async void ClassWalletMainInterfaceForm_ResizeBegin(object sender, EventArgs e)
        {

        }

        private async void ClassWalletMainInterfaceForm_ResizeEnd(object sender, EventArgs e)
        {
            adaptResponsiveFormControlsToFormSize(this, ClassViewStrategiesEnum.Normal);
        }

        private void typeWebSiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            adaptResponsiveFormControlsToFormSize(this, ClassViewStrategiesEnum.TypeWebSite);
        }

        private void leftCenterRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            adaptResponsiveFormControlsToFormSize(this, ClassViewStrategiesEnum.LeftCenterRight);
        }

        private void normalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            adaptResponsiveFormControlsToFormSize(this, ClassViewStrategiesEnum.Normal);
        }

        #endregion

        private void pictureBoxLogo_Click(object sender, EventArgs e)
        {
            // TODO: Send user to https://seguraChain.com
        }

        private void buttonSendTransactionOpenContactList_Click(object sender, EventArgs e)
        {

        }

        private void vIEWTEXTToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void ClassWalletMainInterfaceForm_Resize(object sender, EventArgs e)
        {

        }

        private void ClassWalletMainInterfaceForm_MaximizedBoundsChanged(object sender, EventArgs e)
        {
            adaptResponsiveFormControlsToFormSize(this, ClassViewStrategiesEnum.Normal);
        }

        private void ClassWalletMainInterfaceForm_SizeChanged(object sender, EventArgs e)
        {
            adaptResponsiveFormControlsToFormSize(this, ClassViewStrategiesEnum.Normal);
        }
    }
}

