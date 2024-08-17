using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Desktop_Wallet.InternalForm.SendTransaction
{
    public partial class ClassWalletSendTransactionWaitRequestForm : Form
    {
        private string _currentWalletFileName;
        private string _walletAddressTarget;
        private decimal _amountToSpend;
        private decimal _feeToPay;
        private long _paymentId;
        private int _totalConfirmationsTarget;
        private string _walletPrivateKey;
        private Dictionary<string, ClassTransactionHashSourceObject> _transactionAmountSourceList;
        private ClassWalletSendTransactionWaitRequestFormLanguage _walletSendTransactionWaitRequestFormLanguage;
        private CancellationTokenSource _cancellation;
        public bool SendTransactionStatus;
        private bool _taskComplete;
        private bool _formClosed;
        private long _timestampStart;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="currentWalletFileName"></param>
        /// <param name="walletAddressTarget"></param>
        /// <param name="amountToSpend"></param>
        /// <param name="feeToPay"></param>
        /// <param name="paymentId"></param>
        /// <param name="totalConfirmationsTarget"></param>
        /// <param name="walletPrivateKey"></param>
        /// <param name="transactionAmountSourceList"></param>
        /// <param name="cancellation"></param>
        public ClassWalletSendTransactionWaitRequestForm(string currentWalletFileName, string walletAddressTarget, decimal amountToSpend, decimal feeToPay, long paymentId, int totalConfirmationsTarget, string walletPrivateKey, Dictionary<string, ClassTransactionHashSourceObject> transactionAmountSourceList, CancellationTokenSource cancellation)
        {
            _currentWalletFileName = currentWalletFileName;
            _walletAddressTarget = walletAddressTarget;
            _amountToSpend = amountToSpend;
            _feeToPay = feeToPay;
            _paymentId = paymentId;
            _totalConfirmationsTarget = totalConfirmationsTarget;
            _walletPrivateKey = walletPrivateKey;
            _transactionAmountSourceList = transactionAmountSourceList;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
            InitializeComponent();
        }

        /// <summary>
        /// Executed once the form is loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletSendTransactionWaitRequestForm_Load(object sender, EventArgs e)
        {
            _walletSendTransactionWaitRequestFormLanguage = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletSendTransactionWaitRequestFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_SEND_TRANSACTION_WAIT_REQUEST_FORM);
            labelSendTransactionWaitRequestText.Text = _walletSendTransactionWaitRequestFormLanguage.LABEL_SEND_TRANSACTION_WAIT_REQUEST_TEXT;
            buttonExit.Text = _walletSendTransactionWaitRequestFormLanguage.BUTTON_SEND_TRANSACTION_WAIT_REQUEST_EXIT_TEXT;

            buttonExit = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonExit);
            buttonExit = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonExit, this, 50d, false);
            buttonExit.Visible = false;

            SendAndWaitTransactionResponse();
        }


        private void SendAndWaitTransactionResponse()
        {
            _timestampStart = ClassUtility.GetCurrentTimestampInMillisecond();

            try
            {
                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        SendTransactionStatus = await ClassDesktopWalletCommonData.WalletSyncSystem.BuildAndSendTransaction(_currentWalletFileName, _walletAddressTarget, _amountToSpend, _feeToPay, _paymentId, _totalConfirmationsTarget, _walletPrivateKey, _transactionAmountSourceList, _cancellation);
                    }
                    catch
                    {
                        // Ignored.
                    }
                    _taskComplete = true;

                }, _cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }

            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (!_taskComplete)
                    {
                        if (_cancellation.IsCancellationRequested)
                            break;

                        if (!buttonExit.Visible && _timestampStart + ClassWalletDefaultSetting.DefaultAwaitInvokeDesktopWalletFormDelay <= ClassUtility.GetCurrentTimestampInMillisecond())
                        {
                            System.Windows.Forms.MethodInvoker showExit = () =>  buttonExit.Visible = true;
                            BeginInvoke(showExit);
                        }

                        await Task.Delay(10);
                    }

                    _formClosed = true;

                    System.Windows.Forms.MethodInvoker closeForm = Close;

                    BeginInvoke(closeForm);

                }, _cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Cancel the the task of sending a transaction
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletSendTransactionWaitRequestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (!_cancellation.IsCancellationRequested)
                    _cancellation.Cancel();
            }
            catch
            {
                // Ignored, catch in case of double cancellation.
            }
        }

        private void ClassWalletSendTransactionWaitRequestForm_Paint(object sender, PaintEventArgs e)
        {
            ClassGraphicsUtility.DrawBorderOnControl(e.Graphics, Color.Ivory, Width, Height, 2.5f);
        }

        /// <summary>
        /// Force to close the send transaction form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonExit_Click(object sender, EventArgs e)
        {
            _taskComplete = true;

            if (!_formClosed)
                Close();

            try
            {
                if (!_cancellation.IsCancellationRequested)
                    _cancellation.Cancel();
            }
            catch
            {
                // Ignored, catch in case of double cancellation.
            }
        }
    }
}
