using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Lib.Blockchain.Setting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.InternalForm.Setup.StepForm
{
    public partial class ClassWalletSetupStepOneForm : Form
    {
        private ClassWalletSetupForm _walletSetupForm;

        public ClassWalletSetupStepOneForm(ClassWalletSetupForm walletSetupForm)
        {
            _walletSetupForm = walletSetupForm;
            InitializeComponent();
        }

        #region WinForm events.

        private void ClassWalletSetupStepOneForm_Load(object sender, EventArgs e)
        {
            SwitchLanguage();

            int selectedIndex = 0;

            foreach (KeyValuePair<string, string> languageNamesPair in ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageList)
            {
                comboBoxSetupSelectLanguage.Items.Add(languageNamesPair.Value);
                if (ClassDesktopWalletCommonData.WalletSettingObject.WalletLanguageNameSelected == languageNamesPair.Key)
                {
                    selectedIndex = comboBoxSetupSelectLanguage.Items.Count - 1;
                    _walletSetupForm.SwitchLanguage();
                }
            }

            if (comboBoxSetupSelectLanguage.Items.Count > 0)
                comboBoxSetupSelectLanguage.SelectedIndex = selectedIndex;

            switch(ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        checkBoxSyncInternalMode.Checked = true;
                        SwitchSyncInternalMode();
                    }
                    break;
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        checkBoxSyncExternalMode.Checked = true;
                        SwitchSyncExternalMode();
                        textBoxSyncExternalModeHost.Text = ClassDesktopWalletCommonData.WalletSettingObject.ApiHost;
                        textBoxSyncExternalModePort.Text = (ClassDesktopWalletCommonData.WalletSettingObject.ApiPort > 0 ? ClassDesktopWalletCommonData.WalletSettingObject.ApiPort : BlockchainSetting.PeerDefaultApiPort).ToString();
                    }
                    break;
            }
        }


        private void checkBoxSyncExternalMode_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSyncExternalMode.Checked)
                SwitchSyncExternalMode();
            else
                SwitchSyncInternalMode();
        }

        private void checkBoxSyncInternalMode_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSyncInternalMode.Checked)
                SwitchSyncInternalMode();
            else
                SwitchSyncExternalMode();
        }

        private void textBoxSyncExternalModeHost_TextChanged(object sender, EventArgs e)
        {
            if (IPAddress.TryParse(textBoxSyncExternalModeHost.Text, out _))
            {
                ClassDesktopWalletCommonData.WalletSettingObject.ApiHost = textBoxSyncExternalModeHost.Text;
                textBoxSyncExternalModeHost.ForeColor = Color.Black;
            }
            else
                textBoxSyncExternalModeHost.ForeColor = Color.Red;
        }

        private void textBoxSyncExternalModePort_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textBoxSyncExternalModePort.Text, out int port))
            {
                ClassDesktopWalletCommonData.WalletSettingObject.ApiPort = port;
                textBoxSyncExternalModePort.ForeColor = Color.Black;
            }
            else textBoxSyncExternalModePort.ForeColor = Color.Red;
        }

        private void comboBoxSetupSelectLanguage_SelectedValueChanged(object sender, EventArgs e)
        {
            foreach (KeyValuePair<string, string> languageNamesPair in ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageList)
            {
                if (languageNamesPair.Value == comboBoxSetupSelectLanguage.SelectedItem.ToString())
                {
                    ClassDesktopWalletCommonData.WalletSettingObject.WalletLanguageNameSelected = languageNamesPair.Key;
                    _walletSetupForm.SwitchLanguage();
                    SwitchLanguage();
                    break;
                }
            }
        }

        #endregion

        private void SwitchLanguage()
        {
            var walletSetupStepOneForm = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletSetupStepOneFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_WALLET_SETUP_STEP_ONE_FORM);

            labelSelectLanguage.Text = walletSetupStepOneForm.LABEL_SELECT_LANGUAGE;
            labelSyncInternalModeDescription.Text = walletSetupStepOneForm.LABEL_SYNC_INTERNAL_MODE_DESCRIPTION;
            labelSyncExternalModeDescription.Text = walletSetupStepOneForm.LABEL_SYNC_EXTERNAL_MODE_DESCRIPTION;
            labelSyncExternalModeHost.Text = walletSetupStepOneForm.LABEL_SYNC_EXTERNAL_MODE_HOST;
            labelSyncExternalModePort.Text = walletSetupStepOneForm.LABEL_SYNC_EXTERNAL_MODE_PORT;
            checkBoxSyncInternalMode.Text = walletSetupStepOneForm.CHECKBOX_SYNC_INTERNAL_MODE_TEXT;
            checkBoxSyncExternalMode.Text = walletSetupStepOneForm.CHECKBOX_SYNC_EXTERNAL_MODE_TEXT;

            labelSelectLanguage = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelSelectLanguage, this, 50, false);
            comboBoxSetupSelectLanguage = ClassGraphicsUtility.AutoSetLocationAndResizeControl<ComboBox>(comboBoxSetupSelectLanguage, this, 50, false);
            checkBoxSyncInternalMode = ClassGraphicsUtility.AutoSetLocationAndResizeControl<CheckBox>(checkBoxSyncInternalMode, this, 50, false);
            labelSyncInternalModeDescription = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelSyncInternalModeDescription, this, 50, false);
            checkBoxSyncExternalMode = ClassGraphicsUtility.AutoSetLocationAndResizeControl<CheckBox>(checkBoxSyncExternalMode, this, 50, false);
            labelSyncExternalModeDescription = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelSyncExternalModeDescription, this, 50, false);
            labelSyncExternalModeHost = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelSyncExternalModeHost, panelSetupSyncExternalMode, 50, false);
            labelSyncExternalModePort = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelSyncExternalModePort, panelSetupSyncExternalMode, 50, false);

        }

        private void SwitchSyncInternalMode()
        {
            checkBoxSyncInternalMode.Checked = true;
            checkBoxSyncExternalMode.Checked = false;
            panelSetupSyncExternalMode.Visible = false;
            ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode = ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE;
        }

        private void SwitchSyncExternalMode()
        {
            checkBoxSyncExternalMode.Checked = true;
            checkBoxSyncInternalMode.Checked = false;
            panelSetupSyncExternalMode.Visible = true;
            ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode = ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE;
        }
    }
}
