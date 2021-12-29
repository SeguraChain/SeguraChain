using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.InternalForm.Create.Enum;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Properties;
using SeguraChain_Desktop_Wallet.Wallet.Function;
using SeguraChain_Desktop_Wallet.Wallet.Function.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Desktop_Wallet.Wallet.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Desktop_Wallet.InternalForm.Create
{
    public partial class ClassWalletCreateInternalForm : Form
    {
        private ClassWalletCreateEnumMenu _currentStep;
        private readonly ClassWalletCreateFormLanguage _walletCreateFormLanguageObject;
        private ClassWalletCreateEnumCreateMethod _currentWalletCreateMethod;
        private ClassWalletDataObject _currentWalletDataObject;
        private string _baseWordSelected;
        private string _passwordSelected;
        private int _currentWalletEncryptionRounds;
        private bool _usePassword;
        private bool _saved;
        private bool _import;

        #region Enum steps of the wallet create menu.


        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassWalletCreateInternalForm(bool import, string importPrivateKey)
        {
            if (import)
            {
                _import = import;
                _currentStep = ClassWalletCreateEnumMenu.WALLET_CREATE_WRITE_PASSWORD;
                _currentWalletDataObject = ClassWalletDataFunction.GenerateWalletFromPrivateKey(importPrivateKey);
            }
            else
            {
                _currentStep = ClassWalletCreateEnumMenu.WALLET_CREATE_SELECT_GENERATE_MODE; // Default step.
                _usePassword = true;
                _currentWalletCreateMethod = ClassWalletCreateEnumCreateMethod.FAST_RANDOM_WAY;
            }
            _walletCreateFormLanguageObject = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletCreateFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_CREATE_WALLET_FORM);
            _currentWalletEncryptionRounds = ClassWalletDefaultSetting.DefaultWalletIterationCount;
            InitializeComponent();
           
        }

        /// <summary>
        /// Event executed once the form is loaded, show texts from the current language selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletCreateInternalForm_Load(object sender, EventArgs e)
        {
            tabControlCreateWallet.ItemSize = new Size(tabControlCreateWallet.ItemSize.Width, 1);
            Height -= 10;
            InitializeLanguageContentText();
            ChangeTabPage();
            if(_import)
            {
                buttonCreateWalletBackToStepOne.Enabled = false;
                buttonCreateWalletBackToStepOne.Hide();
            }
        }

        /// <summary>
        /// Event executed on the form is on closing, show a warning message if the user was on the last step of generating a new wallet without to save it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletCreateInternalForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_currentStep == ClassWalletCreateEnumMenu.WALLET_CREATE_FINAL_STEP && !_saved)
            {
                if (MessageBox.Show(_walletCreateFormLanguageObject.MESSAGEBOX_WALLET_CREATE_ON_CLOSING_LAST_STEP_TEXT, _walletCreateFormLanguageObject.MESSAGEBOX_WALLET_CREATE_ON_CLOSING_LAST_STEP_TITLE_TEXT, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    e.Cancel = true;
                else
                    e.Cancel = false;
            }
        }

        #region Initialize text content depending of the language selected.

        /// <summary>
        /// Initialize language content text.
        /// </summary>
        private void InitializeLanguageContentText()
        {

            #region Common content text.

            Text = BlockchainSetting.CoinName + _walletCreateFormLanguageObject.FORM_TITLE_CREATE_WALLET_TEXT;
            tabPageStep1.Text = _walletCreateFormLanguageObject.TABPAGE_CREATE_WALLET_STEP_ONE_TEXT;
            tabPageStep2.Text = _walletCreateFormLanguageObject.TABPAGE_CREATE_WALLET_STEP_TWO_TEXT;
            tabPageStep3.Text = _walletCreateFormLanguageObject.TABPAGE_CREATE_WALLET_STEP_THREE_TEXT;

            #endregion

            #region First step content texts.

            labelCreateWalletTitleStepOneText.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_TITLE_STEP_ONE_TEXT;
            labelCreateWalletTitleStepOneText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelCreateWalletTitleStepOneText, tabControlCreateWallet, 50d, false);

            checkBoxCreateWalletFastRandomWay.Text = _walletCreateFormLanguageObject.CHECKBOX_CREATE_WALLET_FAST_RANDOM_WAY_METHOD_TEXT;

            checkBoxCreateWalletSlowRandomWay.Text = _walletCreateFormLanguageObject.CHECKBOX_CREATE_WALLET_SLOW_RANDOM_WAY_METHOD_TEXT;

            checkBoxCreateWalletBaseWordWay.Text = _walletCreateFormLanguageObject.CHECKBOX_CREATE_WALLET_BASE_WORD_WAY_METHOD_TEXT;

            labelWalletCreateDescriptionType.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_WAY_DESCRIPTION_TEXT;
            labelWalletCreateDescriptionType = UpdateLabelLocation(labelWalletCreateDescriptionType);

            // Set default description way.
            richTextBoxWalletCreateTypeDescription.Text = _walletCreateFormLanguageObject.RICHTEXTBOX_CREATE_WALLET_FAST_RANDOM_WAY_METHOD_DESCRIPTION_TEXT;

            buttonWalletCreateNextStepTwoText.Text = _walletCreateFormLanguageObject.BUTTON_CREATE_WALLET_NEXT_TEXT;
            buttonWalletCreateNextStepTwoText = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonWalletCreateNextStepTwoText);
            buttonWalletCreateNextStepTwoText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonWalletCreateNextStepTwoText, tabControlCreateWallet, 98d, false);

            #endregion

            #region Second step content texts.

            buttonCreateWalletBackToStepOne.Text = _walletCreateFormLanguageObject.BUTTON_CREATE_WALLET_BACK_TEXT;
            buttonCreateWalletBackToStepOne = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonCreateWalletBackToStepOne);

            textBoxCreateWalletTotalEncryptionRounds.Text = _currentWalletEncryptionRounds.ToString();

            labelCreateWalletTitleStepTwoText.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_TITLE_STEP_TWO_TEXT;
            labelCreateWalletTitleStepTwoText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelCreateWalletTitleStepTwoText, tabControlCreateWallet, 50d, false);

            labelCreateWalletPasswordText.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_PASSWORD_TEXT;
            labelCreateWalletPasswordText = UpdateLabelLocation(labelCreateWalletPasswordText);

            checkBoxCreateWalletNoPassword.Text = _walletCreateFormLanguageObject.CHECKBOX_CREATE_WALLET_NO_PASSWORD_TEXT;

            labelCreateWalletEncryptionRounds.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_ENCRYPTION_ROUNDS_TEXT;
            labelCreateWalletEncryptionRounds = UpdateLabelLocation(labelCreateWalletEncryptionRounds);

            buttonWalletCreateNextStepThreeText.Text = _walletCreateFormLanguageObject.BUTTON_CREATE_WALLET_NEXT_TEXT;
            buttonWalletCreateNextStepThreeText = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonWalletCreateNextStepThreeText);
            buttonWalletCreateNextStepThreeText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonWalletCreateNextStepThreeText, tabControlCreateWallet, 98d, false);

            #endregion

            #region Third step content texts.

            labelCreateWalletTitleStepThreeText.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_TITLE_STEP_THREE_TEXT;
            labelCreateWalletTitleStepThreeText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelCreateWalletTitleStepThreeText, tabControlCreateWallet, 50d, false);

            labelCreateWalletQrCodePrivateKeyText.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_QR_CODE_PRIVATE_KEY_TEXT;
            labelCreateWalletQrCodePrivateKeyText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelCreateWalletQrCodePrivateKeyText, panelQrCodePrivateKey, 50d, false);

            labelCreateWalletQrCodeWalletAddress.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_QR_CODE_WALLET_ADDRESS_TEXT;
            labelCreateWalletQrCodeWalletAddress = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelCreateWalletQrCodeWalletAddress, panelQrCodeWalletAddress, 50d, false);

            labelCreateWalletWalletFileName.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_WALLET_FILENAME_TEXT;
            labelCreateWalletWalletFileName = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelCreateWalletWalletFileName, panelSaveWallet, 50d, false);

            buttonCreateWalletSaveWallet.Text = _walletCreateFormLanguageObject.BUTTON_CREATE_WALLET_SAVE_TEXT;
            buttonCreateWalletSaveWallet = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonCreateWalletSaveWallet, panelSaveWallet, 50d, true);

            buttonCreateWalletPrintWallet.Text = _walletCreateFormLanguageObject.BUTTON_CREATE_WALLET_PRINT_TEXT;
            buttonCreateWalletPrintWallet = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonCreateWalletPrintWallet, panelSaveWallet, 50d, true);

            labelCreateWalletPrivateKeyDescription.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_PRIVATE_KEY_DESCRIPTION_TEXT;
            labelCreateWalletWalletAddressDescription.Text = _walletCreateFormLanguageObject.LABEL_CREATE_WALLET_WALLET_ADDRESS_DESCRIPTION_TEXT;

            #endregion

        }

        #endregion

        #region First step event functions.

        private void checkBoxCreateWalletFastRandomWayText_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCreateWalletFastRandomWay.Checked)
            {
                _currentWalletCreateMethod = ClassWalletCreateEnumCreateMethod.FAST_RANDOM_WAY;
                checkBoxCreateWalletSlowRandomWay.Checked = false;
                checkBoxCreateWalletBaseWordWay.Checked = false;
                richTextBoxWalletCreateTypeDescription.Clear();
                richTextBoxWalletCreateTypeDescription.Text = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletCreateFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_CREATE_WALLET_FORM).RICHTEXTBOX_CREATE_WALLET_FAST_RANDOM_WAY_METHOD_DESCRIPTION_TEXT;
            }
        }

        private void checkBoxCreateWalletSlowRandomWay_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCreateWalletSlowRandomWay.Checked)
            {
                _currentWalletCreateMethod = ClassWalletCreateEnumCreateMethod.SLOW_RANDOM_WAY;
                checkBoxCreateWalletFastRandomWay.Checked = false;
                checkBoxCreateWalletBaseWordWay.Checked = false;
                richTextBoxWalletCreateTypeDescription.Clear();
                richTextBoxWalletCreateTypeDescription.Text = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletCreateFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_CREATE_WALLET_FORM).RICHTEXTBOX_CREATE_WALLET_SLOW_RANDOM_WAY_METHOD_DESCRIPTION_TEST;
            }
        }

        private void checkBoxCreateWalletBaseWordWay_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCreateWalletBaseWordWay.Checked)
            {
                _currentWalletCreateMethod = ClassWalletCreateEnumCreateMethod.BASE_WORD_WAY;
                checkBoxCreateWalletFastRandomWay.Checked = false;
                checkBoxCreateWalletSlowRandomWay.Checked = false;
                richTextBoxWalletCreateTypeDescription.Clear();
                richTextBoxWalletCreateTypeDescription.Text = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletCreateFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_CREATE_WALLET_FORM).RICHTEXTBOX_CREATE_WALLET_BASE_WORD_WAY_METHOD_DESCRIPTION_TEST;
            }
        }

        private void richTextBoxCreateWalletBaseWordContent_TextChanged(object sender, EventArgs e)
        {
            _baseWordSelected = richTextBoxCreateWalletBaseWordContent.Text;
        }

        /// <summary>
        /// Reach the second step.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonWalletCreateNextStepTwoText_Click(object sender, EventArgs e)
        {
            bool goNextStep = true;
            switch (_currentWalletCreateMethod)
            {
                case ClassWalletCreateEnumCreateMethod.BASE_WORD_WAY:
                    {
                        if (_baseWordSelected.IsNullOrEmpty(out _))
                        {
                            goNextStep = false;
                            MessageBox.Show(_walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_BASE_WORD_WAY_METHOD_CONTENT_ERROR_TEXT, _walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_BASE_WORD_WAY_METHOD_TITLE_ERROR_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                    }
                    break;
            }

            if (goNextStep)
            {
                _currentStep = ClassWalletCreateEnumMenu.WALLET_CREATE_WRITE_PASSWORD;
                ChangeTabPage();
            }
        }

        #endregion

        #region Second step event functions.

        /// <summary>
        /// Executed once the password written change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxCreateWalletPassword_TextChanged(object sender, EventArgs e)
        {
            if (_currentStep == ClassWalletCreateEnumMenu.WALLET_CREATE_WRITE_PASSWORD)
                _passwordSelected = textBoxCreateWalletPassword.Text;
        }

        /// <summary>
        /// Event executed once the checkbox is checked or unchecked, use or not a password depending the state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxCreateWalletNoPassword_CheckedChanged(object sender, EventArgs e)
        {
            _usePassword = !checkBoxCreateWalletNoPassword.Checked;
        }

        /// <summary>
        /// Event executed once the multiplicator of encryption rounds change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBarCreateWalletEncryptionRounds_ValueChanged(object sender, EventArgs e)
        {
            _currentWalletEncryptionRounds = ClassWalletDefaultSetting.DefaultWalletIterationCount * trackBarCreateWalletEncryptionRounds.Value;
            textBoxCreateWalletTotalEncryptionRounds.Text = _currentWalletEncryptionRounds.ToString();
        }

        /// <summary>
        /// Return back to the step one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCreateWalletBackToStepOne_Click(object sender, EventArgs e)
        {
            if (!_import)
            {
                _currentStep = ClassWalletCreateEnumMenu.WALLET_CREATE_SELECT_GENERATE_MODE;
                ChangeTabPage();
            }
        }

        /// <summary>
        /// Reach the to the step three.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonWalletCreateNextStepThreeText_Click(object sender, EventArgs e)
        {
            bool goNextStep = true;
            if (_usePassword)
            {
                if (_passwordSelected.IsNullOrEmpty(out _))
                {
                    goNextStep = false;
                    MessageBox.Show(_walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_SELECT_PASSWORD_CONTENT_ERROR_TEXT, _walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_SELECT_PASSWORD_TITLE_ERROR_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (goNextStep)
            {
                _currentStep = ClassWalletCreateEnumMenu.WALLET_CREATE_FINAL_STEP;
                if (!_import)
                {
                    switch (_currentWalletCreateMethod)
                    {
                        case ClassWalletCreateEnumCreateMethod.FAST_RANDOM_WAY:
                            _currentWalletDataObject = ClassWalletDataFunction.GenerateNewWalletDataObject(null, true);
                            break;
                        case ClassWalletCreateEnumCreateMethod.SLOW_RANDOM_WAY:
                            _currentWalletDataObject = ClassWalletDataFunction.GenerateNewWalletDataObject(null, false);
                            break;
                        case ClassWalletCreateEnumCreateMethod.BASE_WORD_WAY:
                            _currentWalletDataObject = ClassWalletDataFunction.GenerateNewWalletDataObject(_baseWordSelected, false);
                            break;
                    }
                }
                pictureBoxQrCodePrivateKey.BackgroundImage = ClassWalletDataFunction.GenerateBitmapWalletQrCode(_currentWalletDataObject.WalletPrivateKey);
                pictureBoxQrCodeWalletAddress.BackgroundImage = ClassWalletDataFunction.GenerateBitmapWalletQrCode(_currentWalletDataObject.WalletAddress);
                labelCreateWalletPrivateKey.Text = _currentWalletDataObject.WalletPrivateKey;
                labelCreateWalletWalletAddress.Text = _currentWalletDataObject.WalletAddress;

                if (_usePassword)
                {
#if DEBUG
                    Debug.WriteLine("[TEST] Decrypted: " + _currentWalletDataObject.WalletPrivateKey);
#endif
                    var result = ClassWalletDataFunction.EncryptWalletPrivateKey(_currentWalletDataObject.WalletPrivateKey, _passwordSelected, _currentWalletEncryptionRounds, out string privateKeyEncryptedHex, out string encryptionIvHex, out string passphraseHashHex);

                    if (result == ClassWalletEncryptWalletPrivateKeyEnumResult.WALLET_ENCRYPT_PRIVATE_KEY_SUCCESS)
                    {
#if DEBUG
                        Debug.WriteLine("[TEST] Encrypted hex:" + privateKeyEncryptedHex);
#endif
                        _currentWalletDataObject.WalletEncrypted = true;
                        _currentWalletDataObject.WalletPrivateKey = privateKeyEncryptedHex;
                        _currentWalletDataObject.WalletPassphraseHash = passphraseHashHex;
                        _currentWalletDataObject.WalletEncryptionIv = encryptionIvHex;
                        _currentWalletDataObject.WalletEncryptionRounds = _currentWalletEncryptionRounds;
                        ClassWalletDataFunction.DecryptWalletPrivateKey(privateKeyEncryptedHex, passphraseHashHex, encryptionIvHex, _passwordSelected, _currentWalletEncryptionRounds, out string privateKeyDecrypted);

#if DEBUG
                        Debug.WriteLine("[TEST] Decrypted from encrypted hex:" + privateKeyDecrypted);
#endif
                    }
                }

                ChangeTabPage();
            }
        }

        /// <summary>
        /// Execute the save of the wallet created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCreateWalletSaveWallet_Click(object sender, EventArgs e)
        {
            if (!textBoxCreateWalletSaveWalletFile.Text.IsNullOrEmpty(out _))
            {
                string walletFilename = textBoxCreateWalletSaveWalletFile.Text + ClassWalletDefaultSetting.WalletFileFormat.Replace("*", "");

                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.ContainsKey(walletFilename))
                    MessageBox.Show(_walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_ALREADY_EXIST_CONTENT_ERROR_TEXT, _walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_ALREADY_EXIST_TITLE_ERROR_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    _currentWalletDataObject.WalletFileName = walletFilename;
                    if (!ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.TryAdd(walletFilename, _currentWalletDataObject))
                        MessageBox.Show(_walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_ERROR_CONTENT_ERROR_TEXT, _walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_ERROR_TITLE_ERROR_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                    {
                        if (ClassDesktopWalletCommonData.WalletDatabase.SaveWalletFileAsync(walletFilename).Result)
                        {
                            _saved = true;
                            MessageBox.Show(_walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_SUCCESS_CONTENT_TEXT, _walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_SUCCESS_TITLE_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Close();
                        }
                        else
                            MessageBox.Show(_walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_ERROR_CONTENT_ERROR_TEXT, _walletCreateFormLanguageObject.MESSAGEBOX_CREATE_WALLET_SAVE_WALLET_ERROR_TITLE_ERROR_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        #endregion

        #region Common functions.

        /// <summary>
        /// Change tab page depending of the step.
        /// </summary>
        private void ChangeTabPage()
        {
            #region Disable all tabpages on start. Enable the first step only.

            foreach (TabPage tab in tabControlCreateWallet.TabPages)
            {
                tab.Enabled = false;
                tab.Hide();
            }
            tabControlCreateWallet.TabPages[(int)_currentStep].Enabled = true;
            tabControlCreateWallet.TabPages[(int)_currentStep].Show();
            tabControlCreateWallet.SelectedTab = tabControlCreateWallet.TabPages[(int)_currentStep];

            #endregion
        }

        /// <summary>
        /// Event executed once the tab selected changed, check if the selected tab is the right one.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControlCreateWallet_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControlCreateWallet.SelectedIndex != (int)_currentStep)
                tabControlCreateWallet.SelectedTab = tabControlCreateWallet.TabPages[(int)_currentStep];
        }

        /// <summary>
        /// Update the label location.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        private Label UpdateLabelLocation(Label label)
        {
            double originalPositionFactor = ((double)label.Location.X / tabControlCreateWallet.Width) * 100d;
            return ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(label, tabControlCreateWallet, originalPositionFactor, false);
        }

        #endregion

        #region Print functions.

        /// <summary>
        /// Preview the wallet qr codes created before to print them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCreateWalletPrintWallet_Click(object sender, EventArgs e)
        {
            using (ClassPrintWalletObject printWalletObject = new ClassPrintWalletObject(
                new Bitmap(pictureBoxQrCodePrivateKey.BackgroundImage),
                new Bitmap(pictureBoxQrCodeWalletAddress.BackgroundImage)))
            {
                printWalletObject.DoPrintWallet(this);
            }
        }


        #endregion


    }
}