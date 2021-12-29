using System;
using System.Windows.Forms;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.Wallet.Function;
using SeguraChain_Desktop_Wallet.Wallet.Function.Enum;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Desktop_Wallet.InternalForm.SendTransaction
{
    public partial class ClassWalletSendTransactionPassphraseForm : Form
    {
        private ClassWalletSendTransactionPassphraseFormLanguage _walletSendTransactionPassphraseFormLanguage;
        private string _currentWalletFilename;
        public bool WalletDecryptPrivateKeyResultStatus;
        public string WalletDecryptedPrivateKey;

        public ClassWalletSendTransactionPassphraseForm(string currentWalletFilename)
        {
            _currentWalletFilename = currentWalletFilename;
            InitializeComponent();
        }

        private void ClassSendTransactionPassphraseForm_Load(object sender, EventArgs e)
        {
            _walletSendTransactionPassphraseFormLanguage = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletSendTransactionPassphraseFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_SEND_TRANSACTION_PASSPHRASE_FORM);
            Text = BlockchainSetting.CoinName + _walletSendTransactionPassphraseFormLanguage.FORM_SEND_TRANSACTION_PASSPHRASE_TITLE_TEXT;
            labelSendTransactionInputPassphrase.Text = _walletSendTransactionPassphraseFormLanguage.LABEL_SEND_TRANSACTION_INPUT_PASSPHRASE_TEXT;
            checkBoxSendTransactionShowHidePassphrase.Text = _walletSendTransactionPassphraseFormLanguage.CHECKBOX_SEND_TRANSACTION_SHOW_PASSPHRASE_TEXT;
            buttonSendTransactionUnlockWallet.Text = _walletSendTransactionPassphraseFormLanguage.BUTTON_SEND_TRANSACTION_UNLOCK_WALLET_TEXT;
            buttonSendTransactionUnlockWallet = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonSendTransactionUnlockWallet);
            buttonSendTransactionUnlockWallet = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonSendTransactionUnlockWallet, this, 50, false);
        }

        private void checkBoxSendTransactionShowHidePassphrase_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSendTransactionShowHidePassphrase.Checked)
            {
                checkBoxSendTransactionShowHidePassphrase.Text = _walletSendTransactionPassphraseFormLanguage.CHECKBOX_SEND_TRANSACTION_HIDE_PASSPHRASE_TEXT;
                textBoxSendTransactionPassphrase.PasswordChar = '\0';
            }
            else
            {
                checkBoxSendTransactionShowHidePassphrase.Text = _walletSendTransactionPassphraseFormLanguage.CHECKBOX_SEND_TRANSACTION_SHOW_PASSPHRASE_TEXT;
                textBoxSendTransactionPassphrase.PasswordChar = '*';
            }
        }

        private void buttonSendTransactionUnlockWallet_Click(object sender, EventArgs e)
        {
            bool walletError = true;

            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.ContainsKey(_currentWalletFilename))
            {
                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletEncrypted)
                {
                    walletError = false;

                    string walletPrivateKeyEncryptedHex = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletPrivateKey;
                    string walletPassphraseHash = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletPassphraseHash;
                    string walletEncryptionIvHex = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletEncryptionIv;
                    int walletEncryptionRounds = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[_currentWalletFilename].WalletEncryptionRounds;

                    ClassWalletDecryptWalletPrivateKeyEnumResult decryptWalletPrivateKeyEnumResult = ClassWalletDataFunction.DecryptWalletPrivateKey(walletPrivateKeyEncryptedHex, walletPassphraseHash, walletEncryptionIvHex, textBoxSendTransactionPassphrase.Text, walletEncryptionRounds, out string walletPrivateKeyDecrypted);

                    bool failed = false;

                    if (decryptWalletPrivateKeyEnumResult != ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_SUCCESS)
                    {
                        failed = true;
                        MessageBox.Show(_walletSendTransactionPassphraseFormLanguage.MESSAGEBOX_SEND_TRANSACTION_UNLOCK_WALLET_FAILED_CONTENT_TEXT, _walletSendTransactionPassphraseFormLanguage.MESSAGEBOX_SEND_TRANSACTION_UNLOCK_WALLET_FAILED_TITLE_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        WalletDecryptedPrivateKey = walletPrivateKeyDecrypted;
                    }

                    textBoxSendTransactionPassphrase.Text.Clear();

                    if (!failed)
                    {
                        WalletDecryptPrivateKeyResultStatus = true;
                        Close();
                    }

                }
            }
            if (walletError)
            {
                MessageBox.Show(_walletSendTransactionPassphraseFormLanguage.MESSAGEBOX_SEND_TRANSACTION_UNLOCK_WALLET_ERROR_CONTENT_TEXT, _walletSendTransactionPassphraseFormLanguage.MESSAGEBOX_SEND_TRANSACTION_UNLOCK_WALLET_ERROR_TITLE_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
