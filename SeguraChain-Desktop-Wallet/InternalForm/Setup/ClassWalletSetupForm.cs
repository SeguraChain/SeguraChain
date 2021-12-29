using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.InternalForm.Setup.Enum;
using SeguraChain_Desktop_Wallet.InternalForm.Setup.StepForm;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Lib.Blockchain.Setting;
using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.InternalForm.Setup
{
    public partial class ClassWalletSetupForm : Form
    {
        private ClassSetupStepEnum _currentSetupStep;
        private ClassWalletSetupStepOneForm _walletSetupStepOneForm;
        private ClassWalletSetupStepFinalForm _walletSetupStepFinalForm;

        public ClassWalletSetupForm()
        {
            _currentSetupStep = ClassSetupStepEnum.STEP_BASIC;
            InitializeComponent();
        }

        #region Events forms.

        private void ClassWalletSetupForm_Load(object sender, EventArgs e)
        {
            SwitchLanguage();
            SelectStepForm();
        }

        private void buttonWalletSetupNextStep_Click(object sender, EventArgs e)
        {
            bool switchStepFailed = false;

            switch (_currentSetupStep)
            {
                case ClassSetupStepEnum.STEP_BASIC:
                    {
                        if (_walletSetupStepOneForm.checkBoxSyncExternalMode.Checked)
                        {
                            bool failedPort = !int.TryParse(_walletSetupStepOneForm.textBoxSyncExternalModePort.Text, out int port);

                            if (!failedPort)
                                failedPort = port < BlockchainSetting.PeerMinPort || port > BlockchainSetting.PeerMaxPort;

                            bool failedHost = !IPAddress.TryParse(_walletSetupStepOneForm.textBoxSyncExternalModeHost.Text, out _);

                            _walletSetupStepOneForm.textBoxSyncExternalModePort.ForeColor = failedPort ? Color.Red : Color.Black;
                            _walletSetupStepOneForm.textBoxSyncExternalModeHost.ForeColor = failedHost ? Color.Red : Color.Black;

                            switchStepFailed = failedPort || failedHost ? true : false;
                        }
                    }
                    break;

            }

            if (!switchStepFailed)
                SwitchStep(true);
        }

        private void buttonWalletSetupPrevStep_Click(object sender, EventArgs e)
        {
            SwitchStep(false);
        }

        #endregion

        public void SwitchLanguage()
        {
            ClassWalletSetupFormLanguage walletSetupFormLanguageObject = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletSetupFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_WALLET_SETUP_FORM);

            Text = BlockchainSetting.CoinName + walletSetupFormLanguageObject.FORM_WALLET_SETUP_TITLE_TEXT;
            labelWalletSetupDescription.Text = walletSetupFormLanguageObject.LABEL_WALLET_SETUP_DESCRIPTION_TEXT;
            buttonWalletSetupPrevStep.Text = walletSetupFormLanguageObject.BUTTON_WALLET_SETUP_PREV_TEXT;
            buttonWalletSetupNextStep.Text = walletSetupFormLanguageObject.BUTTON_WALLET_SETUP_NEXT_TEXT;
            buttonWalletSetupSave.Text = walletSetupFormLanguageObject.BUTTON_WALLET_SETUP_SAVE_TEXT;

            labelWalletSetupDescription = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelWalletSetupDescription, this, 50, false);
            buttonWalletSetupPrevStep = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonWalletSetupPrevStep);
            buttonWalletSetupNextStep = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonWalletSetupNextStep);
            buttonWalletSetupSave = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonWalletSetupSave);

            buttonWalletSetupPrevStep.Location = new Point(panelWalletSetupParent.Location.X, buttonWalletSetupPrevStep.Location.Y);
            buttonWalletSetupNextStep.Location = new Point((panelWalletSetupParent.Location.X + panelWalletSetupParent.Width) - buttonWalletSetupNextStep.Width, buttonWalletSetupNextStep.Location.Y);
            buttonWalletSetupSave = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonWalletSetupSave, this, 50, false);
            pictureBoxLogo = ClassGraphicsUtility.AutoSetLocationAndResizeControl<PictureBox>(pictureBoxLogo, this, 50, false);
        }

        /// <summary>
        /// Select the step.
        /// </summary>
        private void SelectStepForm()
        {
            if (_walletSetupStepOneForm != null)
                _walletSetupStepOneForm.Hide();

            switch (_currentSetupStep)
            {
                case ClassSetupStepEnum.STEP_BASIC:
                    {
                        buttonWalletSetupPrevStep.Visible = false;
                        buttonWalletSetupNextStep.Visible = true;
                        buttonWalletSetupSave.Visible = false;

                        if (_walletSetupStepOneForm == null)
                            _walletSetupStepOneForm = new ClassWalletSetupStepOneForm(this)
                            {
                                TopLevel = false,
                                TopMost = true
                            };
                        panelWalletSetupParent.Controls.Clear();
                        panelWalletSetupParent.Controls.Add(_walletSetupStepOneForm);
                        _walletSetupStepOneForm.Show();
                    }
                    break;
                case ClassSetupStepEnum.STEP_ADVANCED:
                    {
                        buttonWalletSetupNextStep.Visible = false;
                        buttonWalletSetupPrevStep.Visible = true;
                        buttonWalletSetupSave.Visible = true;
                        _currentSetupStep = ClassSetupStepEnum.STEP_FINAL;

                        // Not advanced setting available yet.
                        if (_walletSetupStepFinalForm == null)
                            _walletSetupStepFinalForm = new ClassWalletSetupStepFinalForm()
                            {
                                TopLevel = false,
                                TopMost = true
                            };
                        panelWalletSetupParent.Controls.Clear();
                        panelWalletSetupParent.Controls.Add(_walletSetupStepFinalForm);
                        _walletSetupStepFinalForm.Show();
                    }
                    break;
                case ClassSetupStepEnum.STEP_FINAL:
                    {
                        buttonWalletSetupPrevStep.Visible = true;
                        buttonWalletSetupNextStep.Visible = false;
                        buttonWalletSetupSave.Visible = true;

                        if (_walletSetupStepFinalForm == null)
                            _walletSetupStepFinalForm = new ClassWalletSetupStepFinalForm()
                            {
                                TopLevel = false,
                                TopMost = true
                            };
                        panelWalletSetupParent.Controls.Clear();
                        panelWalletSetupParent.Controls.Add(_walletSetupStepFinalForm);
                        _walletSetupStepFinalForm.Show();
                    }
                    break;
            }
        }

        /// <summary>
        /// Switch step.
        /// </summary>
        /// <param name="next">Switch to next or previous step.</param>
        private void SwitchStep(bool next)
        {
            switch (_currentSetupStep)
            {
                case ClassSetupStepEnum.STEP_BASIC:
                case ClassSetupStepEnum.STEP_FINAL:
                    {
                        if (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode == ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE)
                            _currentSetupStep = next ? ClassSetupStepEnum.STEP_ADVANCED : ClassSetupStepEnum.STEP_BASIC;
                        else
                            _currentSetupStep = next ? ClassSetupStepEnum.STEP_FINAL : ClassSetupStepEnum.STEP_BASIC;
                    }
                    break;
                case ClassSetupStepEnum.STEP_ADVANCED:
                    {
                        _currentSetupStep = next ? ClassSetupStepEnum.STEP_FINAL : ClassSetupStepEnum.STEP_BASIC;
                    }
                    break;
            }

            SelectStepForm();
        }

        /// <summary>
        /// Close the wallet setup window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonWalletSetupSave_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
