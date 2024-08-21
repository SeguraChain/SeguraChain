using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Lib.Blockchain.Setting;
using static System.Net.Mime.MediaTypeNames;
using Timer = System.Windows.Forms.Timer;

namespace SeguraChain_Desktop_Wallet.InternalForm.Startup
{
    public partial class ClassWalletStartupInternalForm : Form
    {
        private bool _canOpenMainInterface;
        private bool _noWalletFile;
        private ClassWalletMainInterfaceForm _walletMainInterfaceForm;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassWalletStartupInternalForm()
        {
            InitializeComponent();

            if (!ClassDesktopWalletCommonData.InitializeLanguageDatabaseForStartupForm())
                Close();
        }

        /// <summary>
        /// Event executed after to finish to load the startup form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletStartupInternalForm_Load(object sender, EventArgs e)
        {
            OnStartupDesktopWallet();
        }

        #region On startup.

        /// <summary>
        /// Function executed on the startup of the desktop wallet, start desktop wallet systems.
        /// </summary>
        private void OnStartupDesktopWallet()
        {
            ClassWalletStartupFormLanguage walletStartupFormLanguageObject = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletStartupFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_STARTUP_FORM);

            Text = BlockchainSetting.CoinName + walletStartupFormLanguageObject.FORM_TITLE_LOADING_TEXT;
            UpdateLabelStartupText(BlockchainSetting.CoinName + walletStartupFormLanguageObject.LABEL_STARTUP_DESKTOP_WALLET_LOADING_TEXT);

            Task.Factory.StartNew(async () =>
            {
                if (ClassDesktopWalletCommonData.InitializeDesktopWalletCommonData())
                {
                    UpdateLabelStartupText(walletStartupFormLanguageObject.LABEL_STARTUP_DESKTOP_WALLET_LOADING_SUCCESS_TEXT);

                    await Task.Delay(1000);


                    _noWalletFile = ClassDesktopWalletCommonData.WalletDatabase.GetCountWalletFile == 0;

                    _canOpenMainInterface = true;

                }
            }).ConfigureAwait(false);
        }

        #endregion

        #region On close.

        /// <summary>
        /// Function executed on the close of the desktop wallet, close desktop wallet systems.
        /// </summary>
        public void OnCloseDesktopWallet()
        {
            ClassWalletStartupFormLanguage walletStartupFormLanguageObject = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletStartupFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_STARTUP_FORM);
            Show();

            Text = BlockchainSetting.CoinName + walletStartupFormLanguageObject.FORM_TITLE_CLOSING_TEXT;

            labelStartupDesktopWalletLoadingText.Text = walletStartupFormLanguageObject.LABEL_ON_CLOSE_DESKTOP_WALLET_PENDING_TEXT;
            labelStartupDesktopWalletLoadingText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelStartupDesktopWalletLoadingText, this, 50d, false);

            bool saveResult = ClassDesktopWalletCommonData.CloseDesktopWalletCommonData().Result;

            labelStartupDesktopWalletLoadingText.Text = saveResult ? walletStartupFormLanguageObject.LABEL_ON_CLOSE_DESKTOP_WALLET_SUCCESS_TEXT : walletStartupFormLanguageObject.LABEL_ON_CLOSE_DESKTOP_WALLET_FAILED_TEXT;
            labelStartupDesktopWalletLoadingText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelStartupDesktopWalletLoadingText, this, 50d, false);
            Process.GetCurrentProcess().Kill();
        }


        #endregion

        private void UpdateLabelStartupText(string text)
        {
            System.Windows.Forms.MethodInvoker invoke = () =>
            {
                labelStartupDesktopWalletLoadingText.Text = text;
                labelStartupDesktopWalletLoadingText = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelStartupDesktopWalletLoadingText, this, 50d, false);
            };
            BeginInvoke(invoke);
        }

        private void timerOpenMainInterface_Tick(object sender, EventArgs e)
        {
            if (_canOpenMainInterface)
            {
                timerOpenMainInterface.Stop();
                _canOpenMainInterface = false;
                _walletMainInterfaceForm = new ClassWalletMainInterfaceForm(_noWalletFile, this);
                _walletMainInterfaceForm.ShowDialog(this);
            }
        }
    }
}
