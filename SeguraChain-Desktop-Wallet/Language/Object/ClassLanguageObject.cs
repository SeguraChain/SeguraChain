// ReSharper disable InconsistentNaming
namespace SeguraChain_Desktop_Wallet.Language.Object
{
    /// <summary>
    /// All language parts.
    /// </summary>
    public class ClassLanguageObject
    {
        public string LanguageName = "English"; // Default.
        public string LanguageMinName = "EN"; // Default.
        public ClassWalletStartupFormLanguage WalletStartupFormLanguage;
        public ClassWalletMainFormLanguage WalletMainFormLanguage;
        public ClassWalletCreateFormLanguage WalletCreateFormLanguage;
        public ClassWalletRescanFormLanguage WalletRescanFormLanguage;
        public ClassWalletTransactionHistoryInformationFormLanguage WalletTransactionHistoryInformationFormLanguage;
        public ClassWalletSendTransactionPassphraseFormLanguage WalletSendTransactionPassphraseFormLanguage;
        public ClassWalletSendTransactionConfirmationFormLanguage WalletSendTransactionConfirmationFormLanguage;
        public ClassWalletSendTransactionWaitRequestFormLanguage WalletSendTransactionWaitRequestFormLanguage;
        public ClassWalletImportPrivateKeyFormLanguage WalletImportPrivateKeyFormLanguage;
        public ClassWalletSetupFormLanguage WalletSetupFormLanguage;
        public ClassWalletSetupStepOneFormLanguage WalletSetupStepOneFormLanguage;
        public ClassWalletSetupStepFinalFormLanguage WalletSetupStepFinalFormLanguage;
        public ClassWalletTransactionHistoryInformationLoadingFormLanguage WalletTransactionHistoryInformationLoadingFormLanguage;

        /// <summary>
        /// Default initialization.
        /// </summary>
        public ClassLanguageObject()
        {
            WalletStartupFormLanguage = new ClassWalletStartupFormLanguage();
            WalletMainFormLanguage = new ClassWalletMainFormLanguage();
            WalletCreateFormLanguage = new ClassWalletCreateFormLanguage();
            WalletRescanFormLanguage = new ClassWalletRescanFormLanguage();
            WalletTransactionHistoryInformationFormLanguage = new ClassWalletTransactionHistoryInformationFormLanguage();
            WalletSendTransactionPassphraseFormLanguage = new ClassWalletSendTransactionPassphraseFormLanguage();
            WalletSendTransactionConfirmationFormLanguage = new ClassWalletSendTransactionConfirmationFormLanguage();
            WalletSendTransactionWaitRequestFormLanguage = new ClassWalletSendTransactionWaitRequestFormLanguage();
            WalletImportPrivateKeyFormLanguage = new ClassWalletImportPrivateKeyFormLanguage();
            WalletSetupFormLanguage = new ClassWalletSetupFormLanguage();
            WalletSetupStepOneFormLanguage = new ClassWalletSetupStepOneFormLanguage();
            WalletSetupStepFinalFormLanguage = new ClassWalletSetupStepFinalFormLanguage();
            WalletTransactionHistoryInformationLoadingFormLanguage = new ClassWalletTransactionHistoryInformationLoadingFormLanguage();
        }
    }

    /// <summary>
    /// Start form.
    /// </summary>
    public class ClassWalletStartupFormLanguage
    {
        public string FORM_TITLE_LOADING_TEXT = " - Loading";
        public string FORM_TITLE_CLOSING_TEXT = " - Closing";
        public string LABEL_STARTUP_DESKTOP_WALLET_LOADING_TEXT = " - Loading, please wait a moment.";
        public string LABEL_STARTUP_DESKTOP_WALLET_LOADING_SUCCESS_TEXT = "Every systems of the desktop wallet\n are loaded successfully.";
        public string LABEL_ON_CLOSE_DESKTOP_WALLET_PENDING_TEXT = "Closing please wait a moment,\nyour wallet files update is going to be saved too.";
        public string LABEL_ON_CLOSE_DESKTOP_WALLET_SUCCESS_TEXT = "Every systems are closed successfully,\nyour wallet file(s) is/are saved successfully.";
        public string LABEL_ON_CLOSE_DESKTOP_WALLET_FAILED_TEXT = "Warning, an error occured somewhere, forcing to close..";
    }

    /// <summary>
    /// Main form.
    /// </summary>
    public class ClassWalletMainFormLanguage
    {
        public string TEXT_SPACE = " "; // Much cleaner on the code.
        public string FORM_TITLE_MAIN_INTERFACE_TEXT = " - Desktop Wallet | ";
        public string LABEL_WALLET_OPENED_LIST_TEXT = "Wallet opened:";

        public string LABEL_MAIN_INTERFACE_SYNC_PROGRESS = "Your sync progress:";

        #region Menustrip

        public string MENUSTRIP_FILE_TEXT = "File";
        public string MENUSTRIP_SETTING_TEXT = "Settings";
        public string MENUSTRIP_RESCAN_TEXT = "Rescan";
        public string MENUSTRIP_LANGUAGE_TEXT = "Language";
        public string MENUSTRIP_FILE_OPEN_WALLET_TEXT = "Open a wallet";
        public string MENUSTRIP_FILE_CLOSE_WALLET_TEXT = "Close wallet";
        public string MENUSTRIP_FILE_CREATE_WALLET_TEXT = "Create a wallet";
        public string MENUSTRIP_FILE_IMPORT_PRIVATE_KEY_TEXT = "Import a private key";
        public string MENUSTRIP_FILE_EXIT_TEXT = "Exit";

        #endregion

        #region Tabpages.

        public string TABPAGE_OVERVIEW_TEXT = "Overview";
        public string TABPAGE_SEND_TRANSACTION_TEXT = "Send transaction";
        public string TABPAGE_RECEIVE_TRANSACTION_TEXT = "Receive transaction";
        public string TABPAGE_TRANSACTION_HISTORY_TEXT = "Transaction history";
        public string TABPAGE_STORE_NETWORK_TEXT = "Store network";

        #endregion

        #region Overview.

        public string LABEL_MAIN_INTERFACE_CURRENT_BALANCE_TEXT = "Current balance (scanned at %d1/%d2):";
        public string LABEL_MAIN_INTERFACE_AVAILABLE_BALANCE_AMOUNT_TEXT = "Available: ";
        public string LABEL_MAIN_INTERFACE_PENDING_BALANCE_AMOUNT_TEXT = "Pending: ";
        public string LABEL_MAIN_INTERFACE_TOTAL_BALANCE_AMOUNT_TEXT = "Total: ";
        public string LABEL_MAIN_INTERFACE_RECENT_TRANSACTION_TEXT = "Recent transaction(s): ";

        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_TITLE_TEXT = "Current network stats:";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_BLOCK_HEIGHT_SYNC_TEXT = "Current block height: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_DIFFICULTY_TEXT = "Current network difficulty: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_HASHRATE_TEXT = "Current network hashrate: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_MINING_LUCK_STATUS_TEXT = "Current mining luck status: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_CURRENT_MINING_LUCK_PERCENT_TEXT = "Current mining luck percent: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_INFO_SYNC_TEXT = "Current sync/internal confirmations progress stats:";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_MEMPOOL_TEXT = "Total transaction(s) in MemPool: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_TEXT = "Total transaction(s) synced: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_TRANSACTION_CONFIRMED_TEXT = "Total transaction(s) confirmed: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_BLOCK_UNLOCKED_CHECKED_TEXT = "Total block(s) unlocked checked: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_CIRCULATING_TEXT = "Total coin(s) circulating: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_PENDING_TEXT = "Total coin(s) in pending: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_FEE_CIRCULATING_TEXT = "Total fee(s) circulating: ";
        public string LABEL_MAIN_INTERFACE_NETWORK_STATS_TOTAL_COIN_SPREAD_TEXT = "Total coin(s) spread on the chain: ";

        #endregion

        #region Send transaction

        public string LABEL_SEND_TRANSACTION_AVAILABLE_BALANCE_TEXT = "Your available balance: ";
        public string LABEL_SEND_TRANSACTION_WALLET_ADDRESS_TARGET_TEXT = "Wallet address target:";
        public string LABEL_SEND_TRANSACTION_AMOUNT_SELECTED_TEXT = "Amount to send:";
        public string LABEL_SEND_TRANSACTION_FEE_CALCULATED_TEXT = "Total fees estimated to pay:";
        public string LABEL_SEND_TRANSACTION_FEE_SIZE_COST_TEXT = "Fees size cost estimated:";
        public string LABEL_SEND_TRANSACTION_FEE_CONFIRMATION_COST_TEXT = "Fees confirmations cost estimated:";
        public string LABEL_SEND_TRANSACTION_AMOUNT_TO_SPEND_TEXT = "Amount to spend:";
        public string LABEL_SEND_TRANSACTION_PAYMENT_ID_TEXT = "Payment ID (only if the receiver ask it):";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_COUNT_TARGET_TEXT = "Amount of confirmation(s) target. Maximum {0}:";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_TEXT = "Time estimated: ";
        public string LABEL_SEND_TRANSACTION_TOTAL_AMOUNT_SOURCE_TEXT = "Total transaction(s) amount source(s) to use:";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_FAILED_TEXT = "Error";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_SECONDS_TEXT = "Second(s)";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_MINUTES_TEXT = "Minute(s)";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_HOURS_TEXT = "Hours(s)";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_TIME_ESTIMATED_DAYS_TEXT = "Days(s)";

        public string BUTTON_SEND_TRANSACTION_OPEN_CONTACT_LIST_TEXT = "Contact(s) list";
        public string BUTTON_SEND_TRANSACTION_DO_PROCESS_TEXT = "Send your transaction";

        public string MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_ERROR_CONTENT_TEXT = "Failed to send your transaction, please check input informations.";
        public string MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_ERROR_TITLE_TEXT = "Can't send transaction";
        public string MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_NETWORK_ERROR_CONTENT_TEXT = "Failed to send your transaction.\nNot enough peers have accept your transactions to be broadcasted across the network.\nPlease try again later.";
        public string MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_NETWORK_ERROR_TITLE_TEXT = "Send transaction network error";
        public string MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_NETWORK_SUCCESS_CONTENT_TEXT = "Your transaction has been accepted from peers to be broadcasted.";
        public string MESSAGEBOX_SEND_TRANSACTION_DO_PROCESS_NETWORK_SUCCESS_TITLE_TEXT = "Transaction send successfully.";


        #endregion

        #region Receive transaction.

        public string LABEL_MAIN_INTERFACE_WALLET_ADDRESS_RECEIVE_TITLE_TEXT = "Wallet Address: (Click to copy the address)";
        public string LABEL_MAIN_INTERFACE_WALLET_ADDRESS_EVENT_COPY_TEXT = "copied.";
        public string LABEL_MAIN_INTERFACE_QR_CODE_RECEIVE_TITLE_TEXT = "Your wallet address as QR code:";
        public string BUTTON_MAIN_INTERFACE_SAVE_QR_CODE_TEXT = "Save QR Code";
        public string BUTTON_MAIN_INTERFACE_PRINT_QR_CODE_TEXT = "Print QR Code";
        public string SAVEFILEDIALOG_MAIN_INTERFACE_WALLET_ADDRESS_QR_CODE_TEXT = "PNG File (*.png) | *.png";

        #endregion

        #region Transaction history.

        public string COLUMN_TRANSACTION_DATE = "Date";
        public string COLUMN_TRANSACTION_TYPE = "Type";
        public string COLUMN_TRANSACTION_WALLET_ADDRESS = "Wallet Address";
        public string COLUMN_TRANSACTION_AMOUNT = "Amount";
        public string COLUMN_TRANSACTION_HASH = "Transaction Hash";
        public string COLUMN_TRANSACTION_FEE = "Fee";

        public string ROW_TRANSACTION_TYPE_BLOCK_REWARD_TEXT = "Mining reward received on";
        public string ROW_TRANSACTION_TYPE_DEV_FEE_TEXT = "DevFee received on";
        public string ROW_TRANSACTION_TYPE_NORMAL_TRANSACTION_SENT_TEXT = "Transaction sent to";
        public string ROW_TRANSACTION_TYPE_NORMAL_TRANSACTION_RECEIVED_TEXT = "Transaction received from";
        public string ROW_TRANSACTION_TYPE_TRANSFER_TRANSACTION_SENT_TEXT = "Transfer sent to";
        public string ROW_TRANSACTION_TYPE_TRANSFER_TRANSACTION_RECEIVED_TEXT = "Transfer received from";

        public string BUTTON_MAIN_INTERFACE_BACK_PAGE_TRANSACTION_HISTORY_TEXT = "Back";
        public string BUTTON_MAIN_INTERFACE_NEXT_PAGE_TRANSACTION_HISTORY_TEXT = "Next";
        public string BUTTON_MAIN_INTERFACE_EXPORT_TRANSACTION_HISTORY_TEXT = "Export";
        public string BUTTON_MAIN_INTERFACE_SEARCH_TRANSACTION_HISTORY_TEXT = "Search";

        public string PANEL_TRANSACTION_HISTORY_ON_LOAD_TEXT = "On loading data of the transaction history, please wait a moment: ";
        public string PANEL_TRANSACTION_HISTORY_NO_TRANSACTION_TEXT = "Your wallet currently don't have any transaction synced.";


        public string SAVEFILEDIALOG_TRANSACTION_HISTORY_EXPORT_TEXT = "CSV File (*.csv) | *.csv";
        public string MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_TEXT = "Do you want to export only the current page?";
        public string MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_TITLE_TEXT = "Export transaction history";
        public string MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_ALL_NOTICE_TEXT = "All transactions of the history are going to be exported, do you want to continue?";
        public string MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_DONE_TEXT = "Transaction export successfully done.";
        public string MESSAGEBOX_TRANSACTION_HISTORY_EXPORT_FAILED_TEXT = "Transaction export failed, please try again later.";
        public string MESSAGEBOX_TRANSACTION_HISTORY_SEARCH_NOTHING_FOUND_TEXT = "Nothing related of \"%s\" has been found inside of the transaction history.";

        #endregion
    }

    /// <summary>
    /// Create form.
    /// </summary>
    public class ClassWalletCreateFormLanguage
    {
        #region Common texts.

        public string FORM_TITLE_CREATE_WALLET_TEXT = " - Create a wallet";
        public string TABPAGE_CREATE_WALLET_STEP_ONE_TEXT = "Step 1";
        public string TABPAGE_CREATE_WALLET_STEP_TWO_TEXT = "Step 2";
        public string TABPAGE_CREATE_WALLET_STEP_THREE_TEXT = "Step 3";
        public string BUTTON_CREATE_WALLET_NEXT_TEXT = "Next";
        public string BUTTON_CREATE_WALLET_BACK_TEXT = "Back";

        #endregion

        #region First step texts.

        public string LABEL_CREATE_WALLET_TITLE_STEP_ONE_TEXT = "Select Wallet Creation Method";
        public string CHECKBOX_CREATE_WALLET_FAST_RANDOM_WAY_METHOD_TEXT = "Fast - Random";
        public string CHECKBOX_CREATE_WALLET_SLOW_RANDOM_WAY_METHOD_TEXT = "Slow - Random";
        public string CHECKBOX_CREATE_WALLET_BASE_WORD_WAY_METHOD_TEXT = "Base Words";
        public string LABEL_CREATE_WALLET_WAY_DESCRIPTION_TEXT = "Description:";
        public string RICHTEXTBOX_CREATE_WALLET_FAST_RANDOM_WAY_METHOD_DESCRIPTION_TEXT = "This is the most faster way to generate your private key, then to generate your wallet.\nIt's probably the most secure way for generate your wallet.";
        public string RICHTEXTBOX_CREATE_WALLET_SLOW_RANDOM_WAY_METHOD_DESCRIPTION_TEST = "This slower method use some random SHA512 rounds for generate your wallet. \nIt's probably not the most secure way.";
        public string RICHTEXTBOX_CREATE_WALLET_BASE_WORD_WAY_METHOD_DESCRIPTION_TEST = "This method is not the most secure way to use.\nYou can set a single word or a multiple set of words for generate your private key.\nBe sure to keep your words safe on a paper or somewhere else.";
        public string MESSAGEBOX_CREATE_WALLET_BASE_WORD_WAY_METHOD_TITLE_ERROR_TEXT = "No base word(s)";
        public string MESSAGEBOX_CREATE_WALLET_BASE_WORD_WAY_METHOD_CONTENT_ERROR_TEXT = "Their is no base word(s) selected, change the method or put some words on the specific input text showed.";

        #endregion

        #region Second step texts.

        public string LABEL_CREATE_WALLET_TITLE_STEP_TWO_TEXT = "Create private key password";
        public string LABEL_CREATE_WALLET_PASSWORD_TEXT = "Password:";
        public string CHECKBOX_CREATE_WALLET_NO_PASSWORD_TEXT = "I don't want to set a password";
        public string LABEL_CREATE_WALLET_ENCRYPTION_ROUNDS_TEXT = "Encryption rounds: (More the amount is big, more it's slow to encrypt/decrypt the private key)";
        public string MESSAGEBOX_CREATE_WALLET_SELECT_PASSWORD_TITLE_ERROR_TEXT = "No input password";
        public string MESSAGEBOX_CREATE_WALLET_SELECT_PASSWORD_CONTENT_ERROR_TEXT = "Their is no password written, input a password or tick the box who permit to not encrypt the private key of your wallet.";

        #endregion

        #region Third step texts.

        public string LABEL_CREATE_WALLET_TITLE_STEP_THREE_TEXT = "Save your wallet file:";
        public string LABEL_CREATE_WALLET_QR_CODE_PRIVATE_KEY_TEXT = "QR Code private key:";
        public string LABEL_CREATE_WALLET_QR_CODE_WALLET_ADDRESS_TEXT = "QR Code wallet address:";
        public string LABEL_CREATE_WALLET_WALLET_FILENAME_TEXT = "Wallet filename:";
        public string LABEL_CREATE_WALLET_PRIVATE_KEY_DESCRIPTION_TEXT = "Private key:";
        public string LABEL_CREATE_WALLET_WALLET_ADDRESS_DESCRIPTION_TEXT = "Wallet address:";
        public string BUTTON_CREATE_WALLET_SAVE_TEXT = "Save";
        public string BUTTON_CREATE_WALLET_PRINT_TEXT = "Print";
        public string MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_ALREADY_EXIST_TITLE_ERROR_TEXT = "Wallet filename already exist";
        public string MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_ALREADY_EXIST_CONTENT_ERROR_TEXT = "The wallet filename selected already exist, please select another one.";
        public string MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_ERROR_TITLE_ERROR_TEXT = "Wallet save error";
        public string MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_ERROR_CONTENT_ERROR_TEXT = "Can't save your new wallet file, please ensure the wallet folder exist but also if the wallet filename not exist and retry.";
        public string MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_SUCCESS_TITLE_TEXT = "Wallet saved successfully";
        public string MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_SUCCESS_CONTENT_TEXT = "Your new wallet has been saved successfully.";


        #endregion

        #region MessageBox texts.

        public string MESSAGEBOX_WALLET_CREATE_ON_CLOSING_LAST_STEP_TITLE_TEXT = "Are you sure ?";
        public string MESSAGEBOX_WALLET_CREATE_ON_CLOSING_LAST_STEP_TEXT = "Your wallet created has not been saved. Are you sure to not save it?";

        #endregion
    }

    /// <summary>
    /// Rescan form.
    /// </summary>
    public class ClassWalletRescanFormLanguage
    {
        public string FORM_TITLE_WALLET_RESCAN_FORM_TEXT = " - Rescan wallet";
        public string LABEL_WALLET_RESCAN_PENDING_TEXT = " rescan in pending, please wait a moment..";
        public string MESSAGEBOX_WALLET_RESCAN_ERROR_CONTENT_TEXT = "Error, the wallet file target not exist, please try again later.";
        public string MESSAGEBOX_WALLET_RESCAN_ERROR_TITLE_TEXT = "Error pending to rescan a wallet file.";
        public string LABEL_WALLET_RESCAN_PROGRESS_TEXT = "% rescan done";
    }

    /// <summary>
    /// Transaction history informations form.
    /// </summary>
    public class ClassWalletTransactionHistoryInformationFormLanguage
    {
        public string FORM_TITLE_TRANSACTION_HISTORY_INFORMATION_TEXT = "Details of the transaction";
        public string FORM_TITLE_TRANSACTION_HISTORY_MULTI_INFORMATION_TEXT = "Details of %d transactions";

        public string LINE_TRANSACTION_INFORMATION_BLOCK_HEIGHT_TEXT = "Block height: ";
        public string LINE_TRANSACTION_INFORMATION_BLOCK_HEIGHT_TARGET_TEXT = "Block height confirmation target: ";
        public string LINE_TRANSACTION_INFORMATION_CONFIRMATIONS_COUNT_TEXT = "Confirmations: ";
        public string LINE_TRANSACTION_INFORMATION_DATE_TEXT = "Date sent: ";
        public string LINE_TRANSACTION_INFORMATION_SRC_WALLET_TEXT = "From: ";
        public string LINE_TRANSACTION_INFORMATION_DST_WALLET_TEXT = "To: ";
        public string LINE_TRANSACTION_INFORMATION_AMOUNT_TEXT = "Amount: ";
        public string LINE_TRANSACTION_INFORMATION_FEE_TEXT = "Fees: ";
        public string LINE_TRANSACTION_INFORMATION_HASH_TEXT = "Hash:\n";
        public string LINE_TRANSACTION_INFORMATION_SIZE_TEXT = "Size: {0} Bytes";
        public string LINE_TRANSACTION_INFORMATION_IS_MEMPOOL_TEXT = "Note: This transaction is currently in MemPool.\n" +
                                                                 "The transaction will start the confirmation process once this one is proceed on a block mined.\n" +
                                                                 "If this is one is not propertly broadcasted across the majority of peers or if this one is invalid, this transaction will be ignored and removed.";
        public string LINE_TRANSACTION_INFORMATION_IS_INVALID_FROM_MEMPOOL_TEXT = "This transaction who is in MemPool is invalid, this one has not been broadcast propertly across the majority of peers.";
        public string LINE_TRANSACTION_INFORMATION_IS_INVALID_FROM_BLOCKCHAIN_TEXT = "This transaction who is in Blockchain is invalid, this one has been checked by your internal nodes has invalid. Other nodes should have done the same.";

        public string MESSAGEBOX_TRANSACTION_INFORMATION_COPY_CONTENT_TEXT = "Informations copied.";
        public string MESSAGEBOX_TRANSACTION_INFORMATION_COPY_TITLE_TEXT = "Copied.";

        public string BUTTON_TRANSACTION_INFORMATION_COPY_TEXT = "Copy";
        public string BUTTON_TRANSACTION_INFORMATION_CLOSE_TEXT = "Close";
    }

    /// <summary>
    /// Transaction history informations loading form.
    /// </summary>
    public class ClassWalletTransactionHistoryInformationLoadingFormLanguage
    {
        public string FORM_TITLE_TRANSACTION_HISTORY_INFORMATION_LOADING_TEXT = "Loading transaction informations";
        public string LABEL_LOADING_BLOCK_TRANSACTION_INFORMATIONS_TEXT = "Loading transaction informations, please wait a moment.  %s/%e Loaded.";
    }

    /// <summary>
    /// Send transaction passphrase form.
    /// </summary>
    public class ClassWalletSendTransactionPassphraseFormLanguage
    {
        public string FORM_SEND_TRANSACTION_PASSPHRASE_TITLE_TEXT = " - Unlock your wallet.";
        public string CHECKBOX_SEND_TRANSACTION_SHOW_PASSPHRASE_TEXT = "Show passphrase.";
        public string CHECKBOX_SEND_TRANSACTION_HIDE_PASSPHRASE_TEXT = "Hide passphrase.";
        public string LABEL_SEND_TRANSACTION_INPUT_PASSPHRASE_TEXT = "Input your passphrase to unlock your wallet momently:";
        public string BUTTON_SEND_TRANSACTION_UNLOCK_WALLET_TEXT = "Unlock";
        public string MESSAGEBOX_SEND_TRANSACTION_UNLOCK_WALLET_FAILED_CONTENT_TEXT = "Failed to unlock your wallet, the passphrase is invalid.";
        public string MESSAGEBOX_SEND_TRANSACTION_UNLOCK_WALLET_FAILED_TITLE_TEXT = "Invalid passphrase.";
        public string MESSAGEBOX_SEND_TRANSACTION_UNLOCK_WALLET_ERROR_CONTENT_TEXT = "Can't unlock your wallet, check your wallet file content.";
        public string MESSAGEBOX_SEND_TRANSACTION_UNLOCK_WALLET_ERROR_TITLE_TEXT = "Invalid wallet file.";
    }

    /// <summary>
    /// Send transaction confirmation form.
    /// </summary>
    public class ClassWalletSendTransactionConfirmationFormLanguage
    {
        public string FORM_SEND_TRANSACTION_CONFIRMATION_TITLE_TEXT = " - Confirm your request";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_AMOUNT_TO_SEND_TEXT = "Your are going to send: ";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_WALLET_ADDRESS_TARGET_TEXT = "To: ";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_FEE_TO_PAY_TEXT = "Fee to pay: ";
        public string LABEL_SEND_TRANSACTION_CONFIRMATION_TOTAL_TO_SPEND_TEXT = "Total to spend: ";
        public string BUTTON_SEND_TRANSACTION_CONFIRMATION_ACCEPT_TEXT = "Accept ({0})";
        public string BUTTON_SEND_TRANSACTION_CONFIRMATION_CANCEL_TEXT = "Cancel";
    }

    /// <summary>
    /// Send transaction waiting request from.
    /// </summary>
    public class ClassWalletSendTransactionWaitRequestFormLanguage
    {
        public string LABEL_SEND_TRANSACTION_WAIT_REQUEST_TEXT = "Broadcasting your transaction to peers, please wait a moment..";
        public string BUTTON_SEND_TRANSACTION_WAIT_REQUEST_EXIT_TEXT = "Too long ? Click to close the form.";
    }

    /// <summary>
    /// Import private key form.
    /// </summary>
    public class ClassWalletImportPrivateKeyFormLanguage
    {
        public string FORM_IMPORT_WALLET_PRIVATE_KEY_TITLE_TEXT = " - Import wallet private key";
        public string LABEL_IMPORT_WALLET_PRIVATE_KEY_TEXT = "Input your private key, you will be redirect to the wallet create menu\n to backup QR codes and save your wallet file.";
        public string BUTTON_IMPORT_WALLET_PRIVATE_KEY_TEXT = "Import private key";
        public string MESSAGEBOX_IMPORT_WALLET_PRIVATE_KEY_ERROR_TITLE_TEXT = "Invalid private key format";
        public string MESSAGEBOX_IMPORT_WALLET_PRIVATE_KEY_ERROR_TEXT = "The input private key format is invalid, please input a valid private key";
    }

    /// <summary>
    /// Wallet setup form.
    /// </summary>
    public class ClassWalletSetupFormLanguage
    {
        public string FORM_WALLET_SETUP_TITLE_TEXT = " - Setup your desktop wallet";
        public string LABEL_WALLET_SETUP_DESCRIPTION_TEXT = "Welcome, this menu help you to setup your desktop wallet, steps should be different depending of the sync mode selected.";
        public string BUTTON_WALLET_SETUP_PREV_TEXT = "Back";
        public string BUTTON_WALLET_SETUP_NEXT_TEXT = "Next";
        public string BUTTON_WALLET_SETUP_SAVE_TEXT = "Save and continue";
    }

    /// <summary>
    /// Wallet setup step one form.
    /// </summary>
    public class ClassWalletSetupStepOneFormLanguage
    {
        public string LABEL_SELECT_LANGUAGE = "Select your language";
        public string CHECKBOX_SYNC_INTERNAL_MODE_TEXT = "Use the sync internal mode:";
        public string LABEL_SYNC_INTERNAL_MODE_DESCRIPTION = "This mode is the most secure one, but also the most slower one, this mode run internally a node and provide you synced data directly to your desktop wallet";
        public string CHECKBOX_SYNC_EXTERNAL_MODE_TEXT = "Use the sync external mode:";
        public string LABEL_SYNC_EXTERNAL_MODE_DESCRIPTION = "This mode is the faster one, secure if you use your own one hosted somewhere, this mode reach your node API for provide synced data to your desktop wallet";
        public string LABEL_SYNC_EXTERNAL_MODE_HOST = "Node IP/Hostname:";
        public string LABEL_SYNC_EXTERNAL_MODE_PORT = "Node API Port:";
    }

    /// <summary>
    /// Wallet setup step final form.
    /// </summary>
    public class ClassWalletSetupStepFinalFormLanguage
    {
        public string LABEL_DONATION = "Your wallet has been setup successfully.\nPlease do not hesitate to donate";
        public string LABEL_COPY_ADDRESS_EVENT = "copied.";
        public string TEXT_SPACE = " ";
    }
}
