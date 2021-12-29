using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.InternalForm.Create;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Utility;
using System;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.InternalForm.Import
{
    public partial class ClassImportWalletPrivateKeyInternalForm : Form
    {
        private ClassWalletImportPrivateKeyFormLanguage _walletImportPrivateKeyFormLanguage;

        public ClassImportWalletPrivateKeyInternalForm()
        {
            InitializeComponent();
        }

        private void ClassImportWalletPrivateKeyInternalForm_Load(object sender, EventArgs e)
        {
            _walletImportPrivateKeyFormLanguage = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletImportPrivateKeyFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_IMPORT_WALLET_PRIVATE_KEY_FORM);
            Text = BlockchainSetting.CoinName + _walletImportPrivateKeyFormLanguage.FORM_IMPORT_WALLET_PRIVATE_KEY_TITLE_TEXT;
            labelImportWalletPrivateKey.Text = _walletImportPrivateKeyFormLanguage.LABEL_IMPORT_WALLET_PRIVATE_KEY_TEXT;
            buttonImportWalletPrivateKey.Text = _walletImportPrivateKeyFormLanguage.BUTTON_IMPORT_WALLET_PRIVATE_KEY_TEXT;

            buttonImportWalletPrivateKey = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonImportWalletPrivateKey);
            buttonImportWalletPrivateKey = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonImportWalletPrivateKey, this, 50, false);
            labelImportWalletPrivateKey = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelImportWalletPrivateKey, this, 50, false);
        }

        private void buttonImportWalletPrivateKey_Click(object sender, EventArgs e)
        {
            if (ClassBase58.DecodeWithCheckSum(textBoxImportWalletPrivateKey.Text, true) != null)
            {
                using(ClassWalletCreateInternalForm createWalletInternalForm = new ClassWalletCreateInternalForm(true, textBoxImportWalletPrivateKey.Text))
                {
                    createWalletInternalForm.ShowDialog(this);
                }
                textBoxImportWalletPrivateKey.Text.Clear();
                Close();
            }
            else
            {
                MessageBox.Show(this, _walletImportPrivateKeyFormLanguage.MESSAGEBOX_IMPORT_WALLET_PRIVATE_KEY_ERROR_TEXT, _walletImportPrivateKeyFormLanguage.MESSAGEBOX_IMPORT_WALLET_PRIVATE_KEY_ERROR_TITLE_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
