using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Desktop_Wallet.InternalForm.TransactionHistory
{
    public partial class ClassWalletTransactionHistoryInformationInternalForm : Form
    {
        /// <summary>
        /// Transaction to show.
        /// </summary>
        private List<Tuple<bool, ClassBlockTransaction>> _blockTransactionInformationList;
        private ClassWalletTransactionHistoryInformationFormLanguage _walletTransactionHistoryInformationFormLanguage;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="blockTransactionList"></param>
        /// <param name="isMemPool"></param>
        public ClassWalletTransactionHistoryInformationInternalForm(List<Tuple<bool, ClassBlockTransaction>> blockTransactionList)
        {
            _blockTransactionInformationList = blockTransactionList;
            InitializeComponent();
        }

        /// <summary>
        /// Executed on loading the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClassWalletTransactionHistoryInformationInternalForm_Load(object sender, EventArgs e)
        {
            _walletTransactionHistoryInformationFormLanguage = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletTransactionHistoryInformationFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_TRANSACTION_HISTORY_INFORMATION_FORM);

            Text = _blockTransactionInformationList?.Count > 1 ? 
                _walletTransactionHistoryInformationFormLanguage.FORM_TITLE_TRANSACTION_HISTORY_MULTI_INFORMATION_TEXT.Replace("%d", _blockTransactionInformationList.Count.ToString()) :
                _walletTransactionHistoryInformationFormLanguage.FORM_TITLE_TRANSACTION_HISTORY_INFORMATION_TEXT;

            buttonTransactionHistoryInformationClose.Text = _walletTransactionHistoryInformationFormLanguage.BUTTON_TRANSACTION_INFORMATION_CLOSE_TEXT;
            buttonTransactionHistoryInformationClose = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonTransactionHistoryInformationClose);
            buttonTransactionHistoryInformationClose = ClassGraphicsUtility.AutoSetLocationAndResizeControl<Button>(buttonTransactionHistoryInformationClose, this, 50, false);

            buttonTransactionHistoryInformationCopy.Text = _walletTransactionHistoryInformationFormLanguage.BUTTON_TRANSACTION_INFORMATION_COPY_TEXT;
            buttonTransactionHistoryInformationCopy = ClassGraphicsUtility.AutoResizeControlFromText<Button>(buttonTransactionHistoryInformationCopy);

            bool showTransactionNotes = _blockTransactionInformationList?.Count == 1;

            if (!showTransactionNotes)
            {
                richTextBoxTransactionInformationsNotes.Hide();
                richTextBoxTransactionInformations.Height += richTextBoxTransactionInformationsNotes.Height;
                LoadBlockTransactionInformations();
            }
            else
            {
                // A single block transaction content listed.
                if (_blockTransactionInformationList?.Count > 0)
                {
                    foreach (Tuple<bool, ClassBlockTransaction> blockTransactionTuple in _blockTransactionInformationList)
                    {

                        richTextBoxTransactionInformations.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_BLOCK_HEIGHT_TEXT + blockTransactionTuple.Item2.TransactionObject.BlockHeightTransaction + Environment.NewLine);
                        richTextBoxTransactionInformations.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_BLOCK_HEIGHT_TARGET_TEXT + blockTransactionTuple.Item2.TransactionObject.BlockHeightTransactionConfirmationTarget + Environment.NewLine);
                        richTextBoxTransactionInformations.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_CONFIRMATIONS_COUNT_TEXT + blockTransactionTuple.Item2.TransactionTotalConfirmation + @"/" + (blockTransactionTuple.Item2.TransactionObject.BlockHeightTransactionConfirmationTarget - blockTransactionTuple.Item2.TransactionObject.BlockHeightTransaction) + Environment.NewLine);
                        richTextBoxTransactionInformations.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_DATE_TEXT + ClassUtility.GetDatetimeFromTimestamp(blockTransactionTuple.Item2.TransactionObject.TimestampSend).ToString(CultureInfo.CurrentUICulture) + Environment.NewLine);
                        richTextBoxTransactionInformations.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_SRC_WALLET_TEXT + blockTransactionTuple.Item2.TransactionObject.WalletAddressSender + Environment.NewLine);
                        richTextBoxTransactionInformations.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_DST_WALLET_TEXT + blockTransactionTuple.Item2.TransactionObject.WalletAddressReceiver + Environment.NewLine);
                        richTextBoxTransactionInformations.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_AMOUNT_TEXT + ClassTransactionUtility.GetFormattedAmountFromBigInteger(blockTransactionTuple.Item2.TransactionObject.Amount) + @" " + BlockchainSetting.CoinTickerName + Environment.NewLine);
                        richTextBoxTransactionInformations.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_FEE_TEXT + ClassTransactionUtility.GetFormattedAmountFromBigInteger(blockTransactionTuple.Item2.TransactionObject.Fee) + @" " + BlockchainSetting.CoinTickerName + Environment.NewLine);
                        richTextBoxTransactionInformations.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_HASH_TEXT + blockTransactionTuple.Item2.TransactionObject.TransactionHash + Environment.NewLine);
                        richTextBoxTransactionInformations.AppendText(string.Format(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_SIZE_TEXT, blockTransactionTuple.Item2.TransactionSize) + Environment.NewLine);

                        if (blockTransactionTuple.Item1)
                        {
                            richTextBoxTransactionInformationsNotes.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_IS_MEMPOOL_TEXT);
                            if (!blockTransactionTuple.Item2.TransactionStatus)
                            {
                                richTextBoxTransactionInformationsNotes.AppendText(Environment.NewLine);
                                richTextBoxTransactionInformationsNotes.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_IS_INVALID_FROM_MEMPOOL_TEXT);
                            }
                        }
                        else
                        {
                            if (!blockTransactionTuple.Item2.TransactionStatus)
                            {
                                richTextBoxTransactionInformationsNotes.AppendText(Environment.NewLine);
                                richTextBoxTransactionInformationsNotes.AppendText(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_IS_INVALID_FROM_BLOCKCHAIN_TEXT);
                            }
                            else
                            {
                                richTextBoxTransactionInformationsNotes.Hide();
                                buttonTransactionHistoryInformationCopy.Location = new Point(buttonTransactionHistoryInformationCopy.Location.X, richTextBoxTransactionInformationsNotes.Location.Y);
                                buttonTransactionHistoryInformationClose.Location = new Point(buttonTransactionHistoryInformationClose.Location.X, richTextBoxTransactionInformationsNotes.Location.Y);
                                Height = richTextBoxTransactionInformationsNotes.Location.Y + richTextBoxTransactionInformationsNotes.Height + buttonTransactionHistoryInformationClose.Height;
                            }
                        }

                    }
                }
            }

        }

        /// <summary>
        /// Load block transactions informations in parallel task.
        /// </summary>
        private void LoadBlockTransactionInformations()
        {
            ClassWalletTransactionHistoryInformationsLoadingForm walletTransactionHistoryInformationsLoadingForm = new ClassWalletTransactionHistoryInformationsLoadingForm(_blockTransactionInformationList.Count);

            // Show the loading form.
            Task.Factory.StartNew(() =>
            {
                System.Windows.Forms.MethodInvoker invoke = () =>
                {
                    walletTransactionHistoryInformationsLoadingForm.ShowDialog(this);
                };

                BeginInvoke(invoke);

            }).ConfigureAwait(false);

            // Loading block transactions informations.
            Task.Factory.StartNew(() =>
            {
                string contentBlockInformations = string.Empty;
                int countShowed = 0;

                foreach (Tuple<bool, ClassBlockTransaction> blockTransactionTuple in _blockTransactionInformationList)
                {

                    contentBlockInformations += _walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_BLOCK_HEIGHT_TEXT + blockTransactionTuple.Item2.TransactionObject.BlockHeightTransaction + Environment.NewLine;
                    contentBlockInformations += _walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_BLOCK_HEIGHT_TARGET_TEXT + blockTransactionTuple.Item2.TransactionObject.BlockHeightTransactionConfirmationTarget + Environment.NewLine;
                    contentBlockInformations += _walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_CONFIRMATIONS_COUNT_TEXT + blockTransactionTuple.Item2.TransactionTotalConfirmation + @"/" + (blockTransactionTuple.Item2.TransactionObject.BlockHeightTransactionConfirmationTarget - blockTransactionTuple.Item2.TransactionObject.BlockHeightTransaction) + Environment.NewLine;
                    contentBlockInformations += _walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_DATE_TEXT + ClassUtility.GetDatetimeFromTimestamp(blockTransactionTuple.Item2.TransactionObject.TimestampSend).ToString(CultureInfo.CurrentUICulture) + Environment.NewLine;
                    contentBlockInformations += _walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_SRC_WALLET_TEXT + blockTransactionTuple.Item2.TransactionObject.WalletAddressSender + Environment.NewLine;
                    contentBlockInformations += _walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_DST_WALLET_TEXT + blockTransactionTuple.Item2.TransactionObject.WalletAddressReceiver + Environment.NewLine;
                    contentBlockInformations += _walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_AMOUNT_TEXT + ClassTransactionUtility.GetFormattedAmountFromBigInteger(blockTransactionTuple.Item2.TransactionObject.Amount) + @" " + BlockchainSetting.CoinTickerName + Environment.NewLine;
                    contentBlockInformations += _walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_FEE_TEXT + ClassTransactionUtility.GetFormattedAmountFromBigInteger(blockTransactionTuple.Item2.TransactionObject.Fee) + @" " + BlockchainSetting.CoinTickerName + Environment.NewLine;
                    contentBlockInformations += _walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_HASH_TEXT + blockTransactionTuple.Item2.TransactionObject.TransactionHash + Environment.NewLine;
                    contentBlockInformations += string.Format(_walletTransactionHistoryInformationFormLanguage.LINE_TRANSACTION_INFORMATION_SIZE_TEXT, blockTransactionTuple.Item2.TransactionSize) + Environment.NewLine;

                    countShowed++;

                    if (countShowed < _blockTransactionInformationList.Count)
                    {
                        contentBlockInformations += "____________________________________________________________________________________";
                        contentBlockInformations += Environment.NewLine;
                        contentBlockInformations += Environment.NewLine;
                    }

                    walletTransactionHistoryInformationsLoadingForm.TotalTransactionLoaded = countShowed;
                }


                System.Windows.Forms.MethodInvoker invoke = () =>
                {
                    richTextBoxTransactionInformations.Text = contentBlockInformations;
                    walletTransactionHistoryInformationsLoadingForm.Close();
                };

                BeginInvoke(invoke);

            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Close the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonTransactionHistoryInformationClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Copy the content of the block transaction informations showed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonTransactionHistoryInformationCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(richTextBoxTransactionInformations.Text);
            MessageBox.Show(_walletTransactionHistoryInformationFormLanguage.MESSAGEBOX_TRANSACTION_INFORMATION_COPY_CONTENT_TEXT, _walletTransactionHistoryInformationFormLanguage.MESSAGEBOX_TRANSACTION_INFORMATION_COPY_TITLE_TEXT, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
