using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.MainForm.Object;
using SeguraChain_Desktop_Wallet.Properties;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Desktop_Wallet.Wallet.Object;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Desktop_Wallet.MainForm.System
{
    public class ClassWalletRecentTransactionHistorySystemInstance
    {
        /// <summary>
        /// Contains recent transactions.
        /// </summary>
        public Dictionary<string, ClassRecentTransactionHistoryObject> DictionaryRecentTransactionHistoryObjects;
        private readonly SemaphoreSlim _semaphoreRecentTransactionHistoryAccess;

        /// <summary>
        /// Sync scan progress.
        /// </summary>
        private long _lastWalletSyncBlockHeight;
        private long _lastWalletTransactionCount;
        private long _lastWalletMemPoolTransactionCount;

        /// <summary>
        /// Graphics settings.
        /// </summary>
        private bool _graphicsInitialized;
        private Bitmap _bitmapRecentTransactionHistory;
        private Graphics _graphicsRecentTransactionHistory;
        private readonly int _widthRecentTransactionHistory;
        private readonly int _heightRecentTransactionHistory;
        private int _mousePositionX;
        private int _mousePositionY;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="widthRecentTransactionHistory"></param>
        /// <param name="heightRecentTransactionHistory"></param>
        public ClassWalletRecentTransactionHistorySystemInstance(int widthRecentTransactionHistory, int heightRecentTransactionHistory)
        {
            _widthRecentTransactionHistory = widthRecentTransactionHistory;
            _heightRecentTransactionHistory = heightRecentTransactionHistory;
            DictionaryRecentTransactionHistoryObjects = new Dictionary<string, ClassRecentTransactionHistoryObject>();
            _semaphoreRecentTransactionHistoryAccess = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Get the recent transaction history bitmap drawed.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public Bitmap GetRecentTransactionHistoryBitmap(CancellationTokenSource cancellation)
        {

            bool semaphoreUsed = false;
            try
            {
                if (cancellation != null)
                {
                    try
                    {
                        _semaphoreRecentTransactionHistoryAccess.Wait(cancellation.Token);
                        semaphoreUsed = true;

                        return _bitmapRecentTransactionHistory;
                    }
                    catch
                    {
                        // The operation was canceled pending to wait the access of the semaphore.
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreRecentTransactionHistoryAccess.Release();
            }
            return null;
        }


        /// <summary>
        /// Clear the recent transaction history content.
        /// </summary>
        public void ClearRecentTransactionHistory()
        {
            _graphicsInitialized = false;
            _lastWalletTransactionCount = 0;
            _lastWalletMemPoolTransactionCount = 0;
            DictionaryRecentTransactionHistoryObjects?.Clear();
            if (!ResetOrClearGraphicsRecentTransactionHistory(true))
                InitializeGraphicsRecentTransactionHistory();
        }

        /// <summary>
        /// Initialize graphics content of the recent transaction history.
        /// </summary>
        private void InitializeGraphicsRecentTransactionHistory()
        {
            if (_graphicsInitialized)
                _graphicsRecentTransactionHistory.Clear(ClassWalletDefaultSetting.DefaultRecentTransactionBackColor);
            else
            {
                _bitmapRecentTransactionHistory = new Bitmap(_widthRecentTransactionHistory, _heightRecentTransactionHistory);
                _graphicsRecentTransactionHistory = Graphics.FromImage(_bitmapRecentTransactionHistory);
                _graphicsInitialized = true;
            }
        }

        /// <summary>
        /// Reset Graphics of the recent transaction history.
        /// </summary>
        private bool ResetOrClearGraphicsRecentTransactionHistory(bool clear)
        {
            try
            {
                if (_graphicsInitialized)
                {
                    _graphicsRecentTransactionHistory.Clear(ClassWalletDefaultSetting.DefaultRecentTransactionBackColor);
                    _graphicsRecentTransactionHistory.Dispose();
                    _bitmapRecentTransactionHistory.Dispose();
                }
                if (!clear)
                {
                    _bitmapRecentTransactionHistory = new Bitmap(_widthRecentTransactionHistory, _heightRecentTransactionHistory);
                    _graphicsRecentTransactionHistory = Graphics.FromImage(_bitmapRecentTransactionHistory);
                    _graphicsInitialized = true;
                }
                else _graphicsInitialized = false;
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Update last mouse position on the transaction history.
        /// </summary>
        /// <param name="mousePosition"></param>
        public bool UpdateLastMousePosition(Point mousePosition)
        {
            bool containsPosition = false;
            _mousePositionX = mousePosition.X;
            _mousePositionY = mousePosition.Y;

            try
            {
                foreach (var recentTransactionObject in DictionaryRecentTransactionHistoryObjects.Values.ToArray())
                {
                    if (recentTransactionObject == null)
                        break;

                    containsPosition = recentTransactionObject.TransactionDrawRectangle.Contains(_mousePositionX, _mousePositionY);

                    if (containsPosition)
                        break;
                }
            }
            catch
            {
                // Ignored.
            }

            return containsPosition;
        }

        /// <summary>
        /// Get a transaction hash by click.
        /// </summary>
        /// <param name="mousePosition"></param>
        /// <param name="walletAddress"></param>
        /// <param name="blockTransaction"></param>
        /// <param name="isMemPool"></param>
        public void GetBlockTransactionByClick(Point mousePosition, string walletAddress, CancellationTokenSource cancellation, out ClassBlockTransaction blockTransaction, out bool isMemPool)
        {
            blockTransaction = null; // Default.
            isMemPool = false; // Default.

            try
            {
                foreach (var recentTransactionObjectPair in DictionaryRecentTransactionHistoryObjects.ToArray())
                {
                    if (recentTransactionObjectPair.Value == null)
                        break;

                    if (recentTransactionObjectPair.Value.TransactionDrawRectangle.Contains(mousePosition.X, mousePosition.Y))
                    {
                        long blockHeightTransaction = ClassTransactionUtility.GetBlockHeightFromTransactionHash(recentTransactionObjectPair.Key);
                        blockTransaction = ClassDesktopWalletCommonData.WalletSyncSystem.GetBlockTransactionFromSyncCache(walletAddress, recentTransactionObjectPair.Key, blockHeightTransaction, out isMemPool);
                        break;
                    }
                }
            }
            catch
            {
                // Ignored.
            }
        }

        /// <summary>
        /// Update the recent transaction history.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> UpdateRecentTransactionHistory(string walletFileOpened, CancellationTokenSource cancellation)
        {
            if (!_graphicsInitialized)
                InitializeGraphicsRecentTransactionHistory();

            bool changed = false;
            bool useSemaphore = false;

            try
            {

                try
                {
                    ClassWalletDataObject walletDataObject = ClassDesktopWalletCommonData.WalletDatabase.GetWalletFileOpenedData(walletFileOpened);

                    if (walletDataObject != null)
                    {
                        if (!walletDataObject.WalletEnableRescan)
                        {
                            if (walletDataObject.WalletLastBlockHeightSynced >= await ClassDesktopWalletCommonData.WalletSyncSystem.GetLastBlockHeightUnlockedSynced(cancellation, true))
                            {
                                string walletAddress = walletDataObject.WalletAddress;
                                long walletLastBlockHeightSynced = walletDataObject.WalletLastBlockHeightSynced;
                                bool requireUpdate = _lastWalletSyncBlockHeight != walletLastBlockHeightSynced;

                                long countMemPoolTransactionIndexed = walletDataObject.WalletTotalMemPoolTransaction;
                                long countTransactionIndexed = walletDataObject.WalletTotalTransaction;

                                requireUpdate = countMemPoolTransactionIndexed != _lastWalletMemPoolTransactionCount || countTransactionIndexed != _lastWalletTransactionCount;


                                if (!requireUpdate)
                                {
                                    #region Check if the list of transaction drawed need to be updated.

                                    if (DictionaryRecentTransactionHistoryObjects.Count > 0)
                                    {
                                        using (DisposableDictionary<string, ClassRecentTransactionHistoryObject> copyRecentTransactionHistory = new DisposableDictionary<string, ClassRecentTransactionHistoryObject>(0, DictionaryRecentTransactionHistoryObjects.ToDictionary(x => x.Key, x => x.Value)))
                                        {
                                            if (copyRecentTransactionHistory.Count > 0)
                                            {
                                                foreach (var transaction in copyRecentTransactionHistory.GetList)
                                                {
                                                    if (cancellation != null)
                                                    {
                                                        if (cancellation.IsCancellationRequested)
                                                            break;
                                                    }

                                                    try
                                                    {
                                                        if (DictionaryRecentTransactionHistoryObjects.Count == 0)
                                                        {
                                                            requireUpdate = true;
                                                            break;
                                                        }
                                                        if (transaction.Value == null)
                                                        {
                                                            requireUpdate = true;
                                                            break;
                                                        }
                                                        long blockHeight = transaction.Value.BlockHeight;

                                                        switch (transaction.Value.IsMemPool)
                                                        {
                                                            case true:
                                                                {
                                                                    var tupleBlockTransaction = await ClassDesktopWalletCommonData.WalletSyncSystem.GetTransactionObjectFromSync(walletAddress, transaction.Key, blockHeight, false, cancellation);

                                                                    if (!tupleBlockTransaction.Item1)
                                                                        requireUpdate = true;
                                                                }
                                                                break;
                                                            case false:
                                                                {
                                                                    if (!transaction.Value.IsConfirmed && !transaction.Value.IsSender)
                                                                    {
                                                                        var tupleBlockTransaction = await ClassDesktopWalletCommonData.WalletSyncSystem.GetTransactionObjectFromSync(walletAddress, transaction.Key, blockHeight, false, cancellation);

                                                                        if (tupleBlockTransaction.Item2 != null)
                                                                        {
                                                                            if (tupleBlockTransaction.Item2.TransactionTotalConfirmation != transaction.Value.TransactionTotalConfirmations ||
                                                                                tupleBlockTransaction.Item2.TransactionStatus != transaction.Value.TransactionStatus)
                                                                                requireUpdate = true;
                                                                        }
                                                                    }
                                                                }
                                                                break;
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        requireUpdate = true;
                                                    }

                                                    if (requireUpdate)
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    else
                                        requireUpdate = countTransactionIndexed > 0 || countMemPoolTransactionIndexed > 0;

                                    #endregion
                                }

                                // Update recent transaction history and draw it.
                                if (requireUpdate)
                                {

                                    await _semaphoreRecentTransactionHistoryAccess.WaitAsync(cancellation.Token);
                                    useSemaphore = true;

                                    DictionaryRecentTransactionHistoryObjects.Clear();
                                    if (!ResetOrClearGraphicsRecentTransactionHistory(false))
                                        InitializeGraphicsRecentTransactionHistory();

                                    int totalTxDrawed = 0;
                                    bool completeDraw = false;

                                    if (countMemPoolTransactionIndexed > 0)
                                    {
                                        using (DisposableList<ClassTransactionObject> memPoolTransactionList = new DisposableList<ClassTransactionObject>())
                                        {
                                            foreach (string transactionHash in walletDataObject.WalletMemPoolTransactionList.ToArray())
                                            {
                                                if (cancellation != null)
                                                {
                                                    if (cancellation.IsCancellationRequested)
                                                        break;
                                                }

                                                // Ask sync cache.
                                                ClassTransactionObject transactionObject = await ClassDesktopWalletCommonData.WalletSyncSystem.GetMemPoolTransactionObjectFromSync(walletAddress, transactionHash, false, cancellation);

                                                if (transactionObject != null)
                                                    memPoolTransactionList.Add(transactionObject);
                                                // Else, directly the blockchain database.
                                                else
                                                {
                                                    transactionObject = await ClassDesktopWalletCommonData.WalletSyncSystem.GetMemPoolTransactionObjectFromSync(walletAddress, transactionHash, true, cancellation);
                                                    if (transactionObject != null)
                                                    {
                                                        ClassBlockTransaction blockTransaction = ClassDesktopWalletCommonData.WalletSyncSystem.GetBlockTransactionFromSyncCache(walletAddress, transactionHash, transactionObject.BlockHeightTransaction, out bool isMemPool);
                                                        if (blockTransaction == null || isMemPool)
                                                            memPoolTransactionList.Add(transactionObject);
                                                    }
                                                }
                                            }

                                            if (memPoolTransactionList.Count > 0)
                                            {
                                                foreach (var transactionObject in memPoolTransactionList.GetList.OrderByDescending(x => x.TimestampSend))
                                                {
                                                    if (cancellation != null)
                                                    {
                                                        if (cancellation.IsCancellationRequested)
                                                            break;
                                                    }

                                                    DrawTransactionToRecentHistory(new ClassBlockTransaction(0, transactionObject)
                                                    {
                                                        TransactionStatus = true,
                                                        TransactionObject = transactionObject
                                                    }, walletDataObject.WalletAddress, true, totalTxDrawed);

                                                    totalTxDrawed++;

                                                    if (totalTxDrawed >= ClassWalletDefaultSetting.DefaultWalletMaxRecentTransactionToShow)
                                                    {
                                                        completeDraw = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (!completeDraw)
                                    {
                                        if (countTransactionIndexed > 0)
                                        {
                                            long blockHeightStart = await ClassDesktopWalletCommonData.WalletSyncSystem.GetLastBlockHeightSynced(cancellation, true);
                                            while (blockHeightStart >= 0)
                                            {
                                                if (cancellation != null)
                                                {
                                                    if (cancellation.IsCancellationRequested)
                                                        break;
                                                }

                                                if (ClassDesktopWalletCommonData.WalletSyncSystem.DatabaseSyncCache[walletAddress].ContainsBlockHeight(blockHeightStart))
                                                {
                                                    using (var listBlockTransactionSynced = await ClassDesktopWalletCommonData.WalletSyncSystem.DatabaseSyncCache[walletAddress].GetBlockTransactionFromBlockHeight(blockHeightStart, cancellation))
                                                    {
                                                        foreach (var blockTransactionSynced in listBlockTransactionSynced.GetList.OrderByDescending(x => x.Value.BlockTransaction.TransactionObject.TimestampSend))
                                                        {
                                                            if (cancellation != null)
                                                            {
                                                                if (cancellation.IsCancellationRequested)
                                                                    break;
                                                            }

                                                            if (blockTransactionSynced.Value.BlockTransaction != null)
                                                            {
                                                                DrawTransactionToRecentHistory(blockTransactionSynced.Value.BlockTransaction, walletDataObject.WalletAddress, blockTransactionSynced.Value.IsMemPool, totalTxDrawed);
                                                                totalTxDrawed++;
                                                            }

                                                            if (totalTxDrawed >= ClassWalletDefaultSetting.DefaultWalletMaxRecentTransactionToShow)
                                                            {
                                                                completeDraw = true;
                                                                break;
                                                            }
                                                        }
                                                    }

                                                    if (completeDraw)
                                                        break;
                                                }

                                                blockHeightStart--;

                                                if (blockHeightStart < BlockchainSetting.GenesisBlockHeight)
                                                    break;
                                            }
                                        }
                                    }

                                    _lastWalletMemPoolTransactionCount = countMemPoolTransactionIndexed;
                                    _lastWalletTransactionCount = countTransactionIndexed;
                                    _lastWalletSyncBlockHeight = walletLastBlockHeightSynced;
                                    changed = true;

                                }
                            }
                        }
                    }
                }
                catch (Exception error)
                {
#if DEBUG
                    Debug.WriteLine("Error on updating the recent transaction history. Exception: " + error.Message);
#endif
                    changed = false;
                }
            }
            finally
            {
                if (useSemaphore)
                    _semaphoreRecentTransactionHistoryAccess.Release();
            }
            return changed;
        }

        /// <summary>
        /// Draw a transaction to recent transaction history.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <param name="walletAddress"></param>
        /// <param name="isMemPool"></param>
        /// <param name="totalTxDrawed"></param>
        private void DrawTransactionToRecentHistory(ClassBlockTransaction blockTransaction, string walletAddress, bool isMemPool, int totalTxDrawed)
        {

            ClassRecentTransactionHistoryObject recentTransactionHistoryObject = new ClassRecentTransactionHistoryObject
            {
                IsMemPool = isMemPool
            };

            long totalConfirmationsToReach = blockTransaction.TransactionObject.BlockHeightTransactionConfirmationTarget - blockTransaction.TransactionObject.BlockHeightTransaction;

            recentTransactionHistoryObject.IsConfirmed = blockTransaction.TransactionTotalConfirmation >= totalConfirmationsToReach;


            recentTransactionHistoryObject.IsSender = blockTransaction.TransactionObject.WalletAddressSender == walletAddress;


            recentTransactionHistoryObject.TransactionType = blockTransaction.TransactionObject.TransactionType;


            float positionX = ((_widthRecentTransactionHistory * 20f) / 100f) - ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize;
            float positionY = ((ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize + (ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize / 2f)) * totalTxDrawed) + (ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize / 4f);


            RectangleF rectangleBackgroundRecentTransaction = new RectangleF(0, positionY - (ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize / 3.75f) + 0.5f, _widthRecentTransactionHistory - 1, (ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize + (ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize / 2f)) - 1);

            float rectangleDesignSize = (rectangleBackgroundRecentTransaction.Height * 10) / 100f;

            RectangleF[] rectangleDesignCorner = new RectangleF[]
            {
                new RectangleF(rectangleBackgroundRecentTransaction.Location.X, rectangleBackgroundRecentTransaction.Location.Y, rectangleBackgroundRecentTransaction.Width, rectangleDesignSize)
            };


            _graphicsRecentTransactionHistory.FillRectangle(new SolidBrush(Color.FromArgb(245, 249, 252)), rectangleBackgroundRecentTransaction);

            _graphicsRecentTransactionHistory.FillRectangles(new SolidBrush(Color.FromArgb(228, 231, 235)), rectangleDesignCorner);

            _graphicsRecentTransactionHistory.DrawLine(new Pen(Color.FromArgb(91, 106, 128), 1.5f), new PointF(0, rectangleBackgroundRecentTransaction.Location.Y + rectangleBackgroundRecentTransaction.Height + 1), new PointF(rectangleBackgroundRecentTransaction.Width, rectangleBackgroundRecentTransaction.Location.Y + rectangleBackgroundRecentTransaction.Height + 1));

            RectangleF rectanglePictureTransactionType = new RectangleF(positionX, positionY + rectangleDesignSize, ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize, ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize);

            DateTime dateTransactionSent = ClassUtility.GetDatetimeFromTimestamp(blockTransaction.TransactionObject.TimestampSend);

            string transactionDateText = dateTransactionSent.ToString("dd/MM/yyyy") + " " + dateTransactionSent.ToString("HH:mm:ss");
            float positionDateY = positionY + (ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize / 4.5f);
            float positionDateX = ((_widthRecentTransactionHistory * 70f) / 100f) - _graphicsRecentTransactionHistory.MeasureString(transactionDateText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont).Width;

            if (!blockTransaction.TransactionStatus)
                _graphicsRecentTransactionHistory.DrawString(transactionDateText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, ClassWalletDefaultSetting.DefaultRecentTransactionInvalidSolidBrushColor, positionDateX, positionDateY);
            else
                _graphicsRecentTransactionHistory.DrawString(transactionDateText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, ClassWalletDefaultSetting.DefaultRecentTransactionSolidBrushColor, positionDateX, positionDateY);


            float positionAmountY = positionY + (ClassWalletDefaultSetting.DefaultWalletRecentTransactionLogoSize / 1.5f);

            switch (recentTransactionHistoryObject.TransactionType)
            {
                case ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION:
                    {
                        _graphicsRecentTransactionHistory.DrawImage(Resources.Wallet_Logo_mining_transaction, rectanglePictureTransactionType);
                        string transactionAmountText = @"+" + ClassTransactionUtility.GetFormattedAmountFromBigInteger(blockTransaction.TransactionObject.Amount) + " " + BlockchainSetting.CoinMinName;
                        float positionAmountX = ((_widthRecentTransactionHistory * 70f) / 100f) - _graphicsRecentTransactionHistory.MeasureString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont).Width;

                        if (recentTransactionHistoryObject.IsMemPool)
                            _graphicsRecentTransactionHistory.DrawString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, new SolidBrush(ClassWalletDefaultSetting.DefaultLabelTransactionInMemPoolForeColor), positionAmountX, positionAmountY);
                        else
                        {
                            if (recentTransactionHistoryObject.IsConfirmed)
                                _graphicsRecentTransactionHistory.DrawString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, new SolidBrush(ClassWalletDefaultSetting.DefaultLabelTransactionConfirmedForeColor), positionAmountX, positionAmountY);
                            else
                                _graphicsRecentTransactionHistory.DrawString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, new SolidBrush(ClassWalletDefaultSetting.DefaultLabelTransactionInPendingForeColor), positionAmountX, positionAmountY);
                        }
                    }
                    break;
                case ClassTransactionEnumType.TRANSFER_TRANSACTION:
                    {
                        _graphicsRecentTransactionHistory.DrawImage(Resources.Wallet_Logo_transfer_transaction, rectanglePictureTransactionType);
                        string transactionAmountText;

                        if (recentTransactionHistoryObject.IsSender)
                            transactionAmountText = @"-" + ClassTransactionUtility.GetFormattedAmountFromBigInteger(blockTransaction.TransactionObject.Amount) + " " + BlockchainSetting.CoinMinName;
                        else
                            transactionAmountText = @"+" + ClassTransactionUtility.GetFormattedAmountFromBigInteger(blockTransaction.TransactionObject.Amount) + " " + BlockchainSetting.CoinMinName;

                        float positionAmountX = ((_widthRecentTransactionHistory * 70f) / 100f) - _graphicsRecentTransactionHistory.MeasureString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont).Width;

                        if (recentTransactionHistoryObject.IsMemPool)
                            _graphicsRecentTransactionHistory.DrawString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, new SolidBrush(ClassWalletDefaultSetting.DefaultLabelTransactionInMemPoolForeColor), positionAmountX, positionAmountY);
                        else
                        {
                            if (recentTransactionHistoryObject.IsConfirmed)
                                _graphicsRecentTransactionHistory.DrawString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, new SolidBrush(ClassWalletDefaultSetting.DefaultLabelTransferTransactionConfirmedForeColor), positionAmountX, positionAmountY);
                            else
                                _graphicsRecentTransactionHistory.DrawString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, new SolidBrush(ClassWalletDefaultSetting.DefaultLabelTransactionInPendingForeColor), positionAmountX, positionAmountY);
                        }
                    }
                    break;
                case ClassTransactionEnumType.DEV_FEE_TRANSACTION:
                case ClassTransactionEnumType.NORMAL_TRANSACTION:
                    {
                        string transactionAmountText;

                        if (recentTransactionHistoryObject.IsSender)
                        {
                            _graphicsRecentTransactionHistory.DrawImage(Resources.Wallet_Logo_outgoing_normal_transaction, rectanglePictureTransactionType);
                            transactionAmountText = @"-" + ClassTransactionUtility.GetFormattedAmountFromBigInteger(blockTransaction.TransactionObject.Amount + blockTransaction.TransactionObject.Fee) + " " + BlockchainSetting.CoinMinName;
                            float positionAmountX = ((_widthRecentTransactionHistory * 70f) / 100f) - _graphicsRecentTransactionHistory.MeasureString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont).Width;
                            _graphicsRecentTransactionHistory.DrawString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, new SolidBrush(ClassWalletDefaultSetting.DefaultLabelTransactionOutgoingForeColor), positionAmountX, positionAmountY);
                        }
                        else
                        {
                            _graphicsRecentTransactionHistory.DrawImage(Resources.Wallet_Logo_incoming_normal_transaction, rectanglePictureTransactionType);
                            transactionAmountText = @"+" + ClassTransactionUtility.GetFormattedAmountFromBigInteger(blockTransaction.TransactionObject.Amount) + " " + BlockchainSetting.CoinMinName;

                            float positionAmountX = ((_widthRecentTransactionHistory * 70f) / 100f) - _graphicsRecentTransactionHistory.MeasureString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont).Width;

                            if (recentTransactionHistoryObject.IsMemPool)
                                _graphicsRecentTransactionHistory.DrawString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, new SolidBrush(ClassWalletDefaultSetting.DefaultLabelTransactionInMemPoolForeColor), positionAmountX, positionAmountY);
                            else
                            {
                                if (recentTransactionHistoryObject.IsConfirmed)
                                    _graphicsRecentTransactionHistory.DrawString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, new SolidBrush(ClassWalletDefaultSetting.DefaultLabelTransactionConfirmedForeColor), positionAmountX, positionAmountY);
                                else
                                    _graphicsRecentTransactionHistory.DrawString(transactionAmountText, ClassWalletDefaultSetting.DefaultPanelRecentTransactionHistoryFont, new SolidBrush(ClassWalletDefaultSetting.DefaultLabelTransactionInPendingForeColor), positionAmountX, positionAmountY);
                            }
                        }
                    }
                    break;
            }

            recentTransactionHistoryObject.TransactionDrawRectangle = rectanglePictureTransactionType;
            recentTransactionHistoryObject.TransactionStatus = blockTransaction.TransactionStatus;

            if (DictionaryRecentTransactionHistoryObjects.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                DictionaryRecentTransactionHistoryObjects[blockTransaction.TransactionObject.TransactionHash] = recentTransactionHistoryObject;
            else
                DictionaryRecentTransactionHistoryObjects.Add(blockTransaction.TransactionObject.TransactionHash, recentTransactionHistoryObject);

        }
    }
}
