using Newtonsoft.Json;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Desktop_Wallet.Settings.Object;
using SeguraChain_Lib.Utility;
using System;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.InternalForm.Setting
{
    public partial class ClassWalletSettingForm : Form
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassWalletSettingForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Close the setting form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCloseSettingForm_Click(object sender, EventArgs e)
        {
            if (IPAddress.TryParse(textBoxNodeHost.Text, out IPAddress _))
                ClassDesktopWalletCommonData.WalletSettingObject.ApiHost = textBoxNodeHost.Text;

            if (int.TryParse(textBoxNodePort.Text, out int port))
                ClassDesktopWalletCommonData.WalletSettingObject.ApiPort = port;

            using (StreamWriter writer = new StreamWriter(ClassUtility.ConvertPath(AppContext.BaseDirectory + ClassWalletDefaultSetting.WalletSettingFile)) { AutoFlush = true })
                writer.Write(ClassUtility.SerializeData(ClassDesktopWalletCommonData.WalletSettingObject, Formatting.Indented));

            Close();
        }


        /// <summary>
        /// Load every settins like the language setting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TITLE_FORM_SETTING_Load(object sender, EventArgs e)
        {
            ClassWalletSettingFormLanguage walletStartupFormLanguageObject = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletSettingFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_WALLET_SETTING_FORM);

            Text = walletStartupFormLanguageObject.TITLE_FORM;
            labelNodeHost.Text = walletStartupFormLanguageObject.LABEL_NODE_HOST;
            buttonCloseSettingForm.Text = walletStartupFormLanguageObject.BUTTON_CLOSE_SETTING;
            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        comboBoxSyncMode.SelectedIndex = 0;
                    }
                    break;
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        comboBoxSyncMode.SelectedIndex = 1;
                    }
                    break;
            }
        }

        private void comboBoxSyncMode_SelectedIndexChanged(object sender, EventArgs e)
        {
             switch(comboBoxSyncMode.SelectedIndex)
            {
                case 0:
                    {
                        ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode = ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE;
                    }
                    break;
                case 1:
                    {
                        ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode = ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE;
                    }
                    break;
            }
        }
    }
}
