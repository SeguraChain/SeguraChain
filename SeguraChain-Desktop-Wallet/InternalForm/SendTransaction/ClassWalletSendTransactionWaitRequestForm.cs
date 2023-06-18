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
using SeguraChain_Lib.Blockchain.Transaction.Object;

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

        private void ClassWalletSendTransactionWaitRequestForm_Load(object sender, EventArgs e)
        {
            _walletSendTransactionWaitRequestFormLanguage = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletSendTransactionWaitRequestFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_SEND_TRANSACTION_WAIT_REQUEST_FORM);
            labelSendTransactionWaitRequestText.Text = _walletSendTransactionWaitRequestFormLanguage.LABEL_SEND_TRANSACTION_WAIT_REQUEST_TEXT;
            SendAndWaitTransactionResponse();
        }


        private void SendAndWaitTransactionResponse()
        {
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

                Task.Factory.StartNew(async () =>
                {
                    while(!_taskComplete)
                    {
                        if (_cancellation.IsCancellationRequested)
                            break;

                        await Task.Delay(10, _cancellation.Token);
                    }

                    _formClosed = true;

                    MethodInvoker closeForm = Close;

                    BeginInvoke(closeForm);

                }, _cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                if (!_formClosed)
                    Close();
            }
        }

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
    }
}
