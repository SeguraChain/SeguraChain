using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using System;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.InternalForm.TransactionHistory
{
    public partial class ClassWalletTransactionHistoryInformationsLoadingForm : Form
    {
        private ClassWalletTransactionHistoryInformationLoadingFormLanguage _walletTransactionHistoryInformationLoadingFormLanguage;
        private int _totalTransactionToLoad;
        public int TotalTransactionLoaded;

        public ClassWalletTransactionHistoryInformationsLoadingForm(int totalTransactionToLoad)
        {
            _totalTransactionToLoad = totalTransactionToLoad;

            InitializeComponent();
        }

        /// <summary>
        /// On loading.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletTransactionHistoryInformationsLoadingForm_Load(object sender, EventArgs e)
        {
            _walletTransactionHistoryInformationLoadingFormLanguage = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletTransactionHistoryInformationLoadingFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_TRANSACTION_HISTORY_INFORMATION_LOADING_FORM);

            Text = _walletTransactionHistoryInformationLoadingFormLanguage.FORM_TITLE_TRANSACTION_HISTORY_INFORMATION_LOADING_TEXT;

            labelLoadingBlockTransactionInformationsText.Text = _walletTransactionHistoryInformationLoadingFormLanguage.LABEL_LOADING_BLOCK_TRANSACTION_INFORMATIONS_TEXT.Replace("%s", TotalTransactionLoaded.ToString()).Replace("%e", _totalTransactionToLoad.ToString());        
        }

        /// <summary>
        /// Update the progress of the loading informations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerLoadingBlockTransactionInformations_Tick(object sender, EventArgs e)
        {
            labelLoadingBlockTransactionInformationsText.Text = _walletTransactionHistoryInformationLoadingFormLanguage.LABEL_LOADING_BLOCK_TRANSACTION_INFORMATIONS_TEXT.Replace("%s", TotalTransactionLoaded.ToString()).Replace("%e", _totalTransactionToLoad.ToString());

        }
    }
}
