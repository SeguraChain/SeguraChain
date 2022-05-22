using System.Drawing;

namespace SeguraChain_Desktop_Wallet.Settings.Enum
{
    public class ClassWalletDefaultSetting
    {
        public const string WalletSettingFile = "wallet-setting.json";
        public const string WalletFileFormat = "*.dat";
        public const string WalletDefaultFilename = "wallet.dat";
        public const string DefaultWalletDirectoryFilePath = "\\Wallet\\";
        public const string WalletDefaultSyncCacheDirectoryPath = "\\SyncCache\\";
        public const string WalletDefaultSyncCacheFilename = "sync-cache.dat";
        public const string LanguageFileFormat = "*.json";
        public const string DefaultLanguageDirectoryFilePath = "\\Language\\";
        public const string DefaultLanguageName = "EN";
        public const int DefaultWalletIterationCount = 10000;
        public const int DefaultQrCodeLengthWidthSize = 250;
        public const int DefaultWalletSendTransactionConfirmationDelayAutoCancel = 60;
        public const int DefaultWalletSendTransactionMaxDelayRequest = 5 * 1000;

        /// <summary>
        /// Interval of update used of the sync of wallets files opened.
        /// </summary>
        public const int DefaultWalletUpdateSyncInterval = 1000;
        public const int DefaultWalletUpdateSyncCacheInterval = 1000;

        /// <summary>
        /// Interval of update to show latest blockchain network stats.
        /// </summary>
        public const int DefaultTaskUpdateWalletBlockchainNetworkStatsInterval = 1000;

        /// <summary>
        /// Interval of update to list wallet(s) file(s) opened.
        /// </summary>
        public const int DefaultTaskUpdateWalletFileListOpenedInterval = 1000;

        /// <summary>
        /// Interval of update informations of the wallet opened.
        /// </summary>
        public const int DefaultTaskUpdateWalletInformationsInterval = 10 * 1000;

        /// <summary>
        /// Await delay on invoke action on the form of the desktop wallet.
        /// </summary>
        public const int DefaultAwaitInvokeDesktopWalletFormDelay = 10;

        #region Receive transaction colors and settings.

        // Delay and settings of the event copy wallet address. 
        public const int DefaultLabelWalletAddressEventCopyAwaitDelay = 1000;
        public static readonly Color DefaultLabelWalletAddressEventCopyOriginalForeColor = Color.Ivory;
        public static readonly Color DefaultLabelWalletAddressEventCopyForeColor = Color.LimeGreen;

        #endregion

        #region Recent transaction history colors and settings.

        public static readonly Font DefaultPanelRecentTransactionHistoryFont = new Font("Arial", 10, FontStyle.Bold);
        public const int DefaultTaskUpdateWalletRecentTransactionsInterval = 100;
        public const int DefaultWalletMaxRecentTransactionToShow = 5;
        public const int DefaultWalletRecentTransactionLogoSize = 50;
        public static readonly Color DefaultRecentTransactionBackColor = Color.FromArgb(245, 249, 252);
        public static readonly Color DefaultLabelTransactionInMemPoolForeColor = Color.SlateGray;
        public static readonly Color DefaultLabelTransactionInPendingForeColor = Color.FromArgb(222, 206, 64);
        public static readonly Color DefaultLabelTransactionConfirmedForeColor = Color.Green;
        public static readonly Color DefaultLabelTransferTransactionConfirmedForeColor = Color.Purple;
        public static readonly Color DefaultLabelTransactionOutgoingForeColor = Color.Red;
        public static readonly Color DefaultPictureBoxTransactionBorderColor = Color.FromArgb(222, 206, 64);
        public static readonly SolidBrush DefaultRecentTransactionSolidBrushColor = new SolidBrush(Color.Black);
        public static readonly SolidBrush DefaultRecentTransactionInvalidSolidBrushColor = new SolidBrush(Color.Red);

        #endregion

        #region Transaction history default colors and settings.

        // Intervals and settings.
        public const int DefaultWalletMaxTransactionInHistoryPerPage = 10;
        public const int DefaultTaskUpdateWalletTransactionHistoryInterval = 100;

        // Content.
        public static readonly Color DefaultPanelTransactionHistoryBackgroundColorOnClear = Color.FromArgb(70, 90, 120);
        public static readonly Font DefaultPanelTransactionHistoryFont = new Font("Arial", 10);
        public static readonly Font DefaultPanelTransactionHistoryOnLoadFont = new Font("Arial", 16, FontStyle.Bold);

        public static readonly SolidBrush DefaultPanelTransactionHistorySolidBrush = new SolidBrush(Color.AliceBlue);
        public static readonly SolidBrush DefaultPanelTransactionHistorySolidBrushPosition = new SolidBrush(Color.Black);

        public static readonly Pen DefaultPanelTransactionHistorySelectClickBorderPen = new Pen(Color.FromArgb(222, 206, 64), 2.0f);
        public static readonly Pen DefaultPanelTransactionHistoryColumnLinesPen = new Pen(Color.FromArgb(222, 206, 64), 1.0f);

        public static readonly Pen DefaultPanelTransactionHistoryCellLinesPen = new Pen(Color.AliceBlue, 1.0f);
        public static readonly SolidBrush DefaultPanelTransactionHistoryBackgroundColorInvalidTransactionSolidBrush = new SolidBrush(Color.Red);
        public static readonly SolidBrush DefaultPanelTransactionHistoryBackgroundColorInvalidMemPoolTransactionSolidBrush = new SolidBrush(Color.Black);
        public static readonly SolidBrush DefaultPanelTransactionHistoryBackgroundColorMemPoolTransactionSolidBrush = new SolidBrush(Color.DarkSlateBlue);
        public static readonly SolidBrush DefaultPanelTransactionHistoryBackgroundColorSelectedTransactionSolidBrushByClick = new SolidBrush(Color.DodgerBlue);
        public static readonly SolidBrush DefaultPanelTransactionHistoryBackgroundColorSelectedTransactionSolidBrushByPosition = new SolidBrush(Color.AliceBlue);

        // Columns.
        public static readonly SolidBrush DefaultPanelTransactionHistoryColumnsBackgroundColorOnClearSolidBrush = new SolidBrush(Color.AliceBlue);
        public static readonly Pen DefaultPanelTransactionHistoryColummBorderPen = new Pen(Brushes.Black, 1);
        public static readonly SolidBrush DefaultPanelTransactionHistoryColumnForeColorSolidBrush = new SolidBrush(Color.Black);
        public static readonly SolidBrush DefaultPanelTransactionHistoryColumnButtonOrderColumnsSolidBrushUnselected = new SolidBrush(Color.Black);
        public static readonly SolidBrush DefaultPanelTransactionHistoryColumnButtonOrderColumnsSolidBrushSelected = new SolidBrush(Color.FromArgb(222, 206, 64));

        #endregion

        #region Donations addresses settings.

        public const string BitcoinAddress = "39mUsJFhjU6GDrchCkQ4iJsmdvD8S2jpzU";
        public const string CurecoinAddress = "B6V6mSNRDFzmDgiPcQKYhBer6GgbM5XJeR";
        public const string MoneroAddress = "44TSVkQ2k9TVns4AdKpP1bSPQ5ZgHU9sULZZwBrWye82fa6MpLgFFi66mjLMtQEZ7xPXGLz5LPktfH71tFqdX36HCbE3DvU";
        public const string LitecoinAddress = "MCBC7r7WWBULBhRyYj4XzVco3XQMzM4URt";

        #endregion
    }
}
