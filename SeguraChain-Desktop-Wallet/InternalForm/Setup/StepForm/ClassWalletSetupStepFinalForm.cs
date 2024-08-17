using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Lib.Utility;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SeguraChain_Desktop_Wallet.InternalForm.Setup.StepForm
{
    public partial class ClassWalletSetupStepFinalForm : Form
    {
        private SemaphoreSlim _semaphoreClickCopyDonationAdress;
        private ClassWalletSetupStepFinalFormLanguage _walletSetupStepFinalFormLanguage;
        private bool _onCopyWalletAddress;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassWalletSetupStepFinalForm()
        {
            _semaphoreClickCopyDonationAdress = new SemaphoreSlim(1, 1);
            InitializeComponent();
        }

        /// <summary>
        /// On loading the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletSetupStepFinalForm_Load(object sender, EventArgs e)
        {
            _walletSetupStepFinalFormLanguage = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletSetupStepFinalFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_WALLET_SETUP_STEP_FINAL_FORM);

            labelDonation.Text = _walletSetupStepFinalFormLanguage.LABEL_DONATION;
            labelDonation = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Label>(labelDonation, this, 50, false);

            // Curecoin.
            labelCurecoin.Text = ClassWalletDefaultSetting.CurecoinAddress;
            labelCurecoin.Click += CopyWalletAddressByClick;

            // Bitcoin.
            labelBitcoin.Text = ClassWalletDefaultSetting.BitcoinAddress;
            labelBitcoin.Click += CopyWalletAddressByClick;

            // Monero.
            labelMonero.Text = ClassWalletDefaultSetting.MoneroAddress;
            labelMonero.Click += CopyWalletAddressByClick;

            // Litecoin.
            labelLitecoin.Text = ClassWalletDefaultSetting.LitecoinAddress;
            labelLitecoin.Click += CopyWalletAddressByClick;
        }

        /// <summary>
        /// Copy the wallet address by click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyWalletAddressByClick(object sender, EventArgs e)
        {
            if (!_onCopyWalletAddress)
            {
                _semaphoreClickCopyDonationAdress.Wait();
                _onCopyWalletAddress = true;

                Label labelWalletTarget = sender as Label;

                Task.Factory.StartNew(async () =>
                {
                    bool complete = false;

                    System.Windows.Forms.MethodInvoker invoke = async () =>
                    {

                        // This copy permit to ensure to not link text edited to the object wallet address string to show.
                        string walletAddressCopy = labelWalletTarget.Text.DeepCopy();
                        string walletAddressCopyText = labelWalletTarget.Text.DeepCopy();

                        try
                        {

                            Clipboard.SetText(walletAddressCopy);

                            labelWalletTarget.ForeColor = ClassWalletDefaultSetting.DefaultLabelWalletAddressEventCopyForeColor;

                            labelWalletTarget.Text = walletAddressCopyText + _walletSetupStepFinalFormLanguage.TEXT_SPACE + _walletSetupStepFinalFormLanguage.LABEL_COPY_ADDRESS_EVENT;

                            await Task.Delay(ClassWalletDefaultSetting.DefaultLabelWalletAddressEventCopyAwaitDelay);

                            labelWalletTarget.ForeColor = ClassWalletDefaultSetting.DefaultLabelWalletAddressEventCopyOriginalForeColor;
                            labelWalletTarget.Text = walletAddressCopyText;
                        }
                        catch
                        {
                            labelWalletTarget.ForeColor = ClassWalletDefaultSetting.DefaultLabelWalletAddressEventCopyOriginalForeColor;
                            labelWalletTarget.Text = walletAddressCopyText;
                        }
                        _onCopyWalletAddress = false;
                    };

                    var result = BeginInvoke(invoke);

                    while (!complete)
                    {
                        if (!ClassDesktopWalletCommonData.DesktopWalletStarted)
                            break;

                        await Task.Delay(ClassWalletDefaultSetting.DefaultLabelWalletAddressEventCopyAwaitDelay);
                    }


                    _semaphoreClickCopyDonationAdress.Release();

                }).ConfigureAwait(false);
            }
        }
    }
}
