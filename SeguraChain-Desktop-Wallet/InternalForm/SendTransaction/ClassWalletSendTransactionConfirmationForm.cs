using System;
using System.Numerics;
using System.Windows.Forms;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Transaction.Utility;

namespace SeguraChain_Desktop_Wallet.InternalForm.SendTransaction
{
    public partial class ClassWalletSendTransactionConfirmationForm : Form
    {
        private ClassWalletSendTransactionConfirmationFormLanguage _walletSendTransactionConfirmationFormLanguage;
        private readonly BigInteger _sendTransactionTotalSend;
        private readonly BigInteger _sendTransactionTotalFeeToPay;
        private readonly string _sendTransactionWalletAddressTarget;
        private int _sendTransactionTimeAutoCancel;
        public bool SendTransactionConfirmationStatus;

        public ClassWalletSendTransactionConfirmationForm(BigInteger sendTransactionTotalSend, BigInteger sendTransactionTotalFeeToPay, string sendTransactionWalletAddressTarget)
        {
            _sendTransactionTimeAutoCancel = ClassWalletDefaultSetting.DefaultWalletSendTransactionConfirmationDelayAutoCancel;
            _sendTransactionTotalSend = sendTransactionTotalSend;
            _sendTransactionTotalFeeToPay = sendTransactionTotalFeeToPay;
            _sendTransactionWalletAddressTarget = sendTransactionWalletAddressTarget;
            InitializeComponent();
        }

        /// <summary>
        /// Automatically translate the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletSendTransactionConfirmationForm_Load(object sender, EventArgs e)
        {
            _walletSendTransactionConfirmationFormLanguage = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletSendTransactionConfirmationFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_SEND_TRANSACTION_CONFIRMATION_FORM);
            Text = BlockchainSetting.CoinName + _walletSendTransactionConfirmationFormLanguage.FORM_SEND_TRANSACTION_CONFIRMATION_TITLE_TEXT;
            labelSendTransactionAmountTotalToSend.Text = _walletSendTransactionConfirmationFormLanguage.LABEL_SEND_TRANSACTION_CONFIRMATION_AMOUNT_TO_SEND_TEXT + ClassTransactionUtility.GetFormattedAmountFromBigInteger(_sendTransactionTotalSend) + @" " + BlockchainSetting.CoinMinName;
            labelSendTransactionConfirmationWalletAddressTarget.Text = _walletSendTransactionConfirmationFormLanguage.LABEL_SEND_TRANSACTION_CONFIRMATION_WALLET_ADDRESS_TARGET_TEXT + _sendTransactionWalletAddressTarget;
            labelSendTransactionConfirmationFeeToPay.Text = _walletSendTransactionConfirmationFormLanguage.LABEL_SEND_TRANSACTION_CONFIRMATION_FEE_TO_PAY_TEXT + ClassTransactionUtility.GetFormattedAmountFromBigInteger(_sendTransactionTotalFeeToPay) + @" " + BlockchainSetting.CoinMinName;
            labelSendTransactionConfirmationTotalToSpend.Text = _walletSendTransactionConfirmationFormLanguage.LABEL_SEND_TRANSACTION_CONFIRMATION_TOTAL_TO_SPEND_TEXT + ClassTransactionUtility.GetFormattedAmountFromBigInteger(_sendTransactionTotalSend + _sendTransactionTotalFeeToPay) + @" " + BlockchainSetting.CoinMinName;

            buttonSendTransactionConfirmationAccept.Text = string.Format(_walletSendTransactionConfirmationFormLanguage.BUTTON_SEND_TRANSACTION_CONFIRMATION_ACCEPT_TEXT, _sendTransactionTimeAutoCancel);
            buttonSendTransactionConfirmationAccept = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonSendTransactionConfirmationAccept);
            buttonSendTransactionConfirmationAccept = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonSendTransactionConfirmationAccept, this, 50, false);

            buttonSendTransactionConfirmationCancel.Text = _walletSendTransactionConfirmationFormLanguage.BUTTON_SEND_TRANSACTION_CONFIRMATION_CANCEL_TEXT;
            buttonSendTransactionConfirmationCancel = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonSendTransactionConfirmationCancel);
            buttonSendTransactionConfirmationCancel = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonSendTransactionConfirmationCancel, this, 50, false);
        }

        /// <summary>
        /// Automatically close the form of confirmation and cancel the sending transaction task if the delay is reach.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerSendTransactionConfirmationAutoCancel_Tick(object sender, EventArgs e)
        {
            _sendTransactionTimeAutoCancel--;
            buttonSendTransactionConfirmationAccept.Text = string.Format(_walletSendTransactionConfirmationFormLanguage.BUTTON_SEND_TRANSACTION_CONFIRMATION_ACCEPT_TEXT, _sendTransactionTimeAutoCancel);
            buttonSendTransactionConfirmationAccept = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonSendTransactionConfirmationAccept);
            buttonSendTransactionConfirmationAccept = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonSendTransactionConfirmationAccept, this, 50, false);

            if (_sendTransactionTimeAutoCancel <= 0)
            {
                Close();
            }
        }

        /// <summary>
        /// Confirm the sending transaction task.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSendTransactionConfirmationAccept_Click(object sender, EventArgs e)
        {
            SendTransactionConfirmationStatus = true;
            timerSendTransactionConfirmationAutoCancel.Stop();
            Close();
        }

        /// <summary>
        /// Cancel the sending transaction task.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSendTransactionConfirmationCancel_Click(object sender, EventArgs e)
        {
            timerSendTransactionConfirmationAutoCancel.Stop();
            Close();
        }
    }
}
