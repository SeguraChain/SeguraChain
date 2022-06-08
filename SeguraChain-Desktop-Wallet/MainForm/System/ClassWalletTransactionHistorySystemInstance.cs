using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Components;
using SeguraChain_Desktop_Wallet.Enum;
using SeguraChain_Desktop_Wallet.InternalForm.Custom.Object;
using SeguraChain_Desktop_Wallet.Language.Enum;
using SeguraChain_Desktop_Wallet.Language.Object;
using SeguraChain_Desktop_Wallet.MainForm.Object;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Desktop_Wallet.Wallet.Object;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Desktop_Wallet.MainForm.System
{
    public class ClassWalletTransactionHistorySystemInstance
    {
        /// <summary>
        /// Databases of the transaction history.
        /// </summary>
        private readonly ConcurrentDictionary<string, ClassTransactionHistoryObject> _dictionaryTransactionHistory;
        private readonly ConcurrentDictionary<ClassEnumTransactionHistoryColumnType, ClassTransactionHistoryOrderObject> _dictionaryTransactionHistoryOrderType;

        /// <summary>
        /// Handle multithreading access.
        /// </summary>
        private readonly SemaphoreSlim _semaphoreTransactionHistoryAccess;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassWalletTransactionHistorySystemInstance()
        {
            _dictionaryTransactionHistory = new ConcurrentDictionary<string, ClassTransactionHistoryObject>();
            _dictionaryTransactionHistoryOrderType = new ConcurrentDictionary<ClassEnumTransactionHistoryColumnType, ClassTransactionHistoryOrderObject>();
            _semaphoreTransactionHistoryAccess = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Get the drawed bitmap content of the transaction history target by wallet filename.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <returns></returns>
        public Bitmap this[string walletFileOpened, CancellationTokenSource cancellation]
        {
            get
            {
                bool semaphoreUsed = false;

                try
                {
                    try
                    {
                        _semaphoreTransactionHistoryAccess.Wait(cancellation.Token);
                        semaphoreUsed = true;

                        return _dictionaryTransactionHistory[walletFileOpened].BitmapTransactionHistory;
                    }
                    catch
                    {
                        // Ignored, catch the exception once the cancellation token is cancelled.
                    }
                }
                finally
                {
                    if (semaphoreUsed)
                        _semaphoreTransactionHistoryAccess.Release();
                }


                return null;
            }
        }

        /// <summary>
        /// Update the transaction history of a wallet file opened.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="cancellation"></param>
        public async Task<bool> UpdateTransactionHistoryOfWalletFileOpened(string walletFileOpened, CancellationTokenSource cancellation)
        {
            bool doClean = false;
            bool updated = false;
            bool changed = false;
            bool exception = false;

            bool semaphoreUsed = false;

            try
            {
                try
                {
                    if (await _semaphoreTransactionHistoryAccess.TryWaitAsync(1000, cancellation))
                    {
                        semaphoreUsed = true;

                        if (!_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                            RemoveTransactionHistoryFromWalletFileOpenedTarget(walletFileOpened);
                        else
                        {

                            if (_dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory == null || _dictionaryTransactionHistory[walletFileOpened].BitmapTransactionHistory == null)
                            {
                                _dictionaryTransactionHistory[walletFileOpened].InitializeOrClearPanelTransactionHistoryGraphicsContent();
                                _dictionaryTransactionHistory[walletFileOpened].ClearTransactionHistoryContent();
                            }

                            ClassWalletDataObject walletDataObject = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileOpened];

                            if (walletDataObject != null)
                            {
                                if (!walletDataObject.WalletEnableRescan)
                                {
                                    if (walletDataObject.WalletLastBlockHeightSynced >= BlockchainSetting.GenesisBlockHeight)
                                    {
                                        long lastBlockHeightProgress = _dictionaryTransactionHistory[walletFileOpened].LastBlockHeight;
                                        long walletLastBlockHeightSync = walletDataObject.WalletLastBlockHeightSynced;

                                        bool requireUpdate = false;
                                        string walletAddress = walletDataObject.WalletAddress;

                                        #region List all MemPool tx synced.

                                        if (walletDataObject.WalletMemPoolTransactionList.Count > 0)
                                        {
                                            foreach (string memPoolTransactionHash in walletDataObject.WalletMemPoolTransactionList.ToArray())
                                            {
                                                if (cancellation.IsCancellationRequested)
                                                    break;

                                                ClassTransactionObject transactionObject = await ClassDesktopWalletCommonData.WalletSyncSystem.GetMemPoolTransactionObjectFromSync(walletAddress, memPoolTransactionHash, false, cancellation);

                                                if (transactionObject != null)
                                                {
                                                    if (!_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.ContainsKey(memPoolTransactionHash))
                                                    {
                                                        if (!_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.ContainsKey(memPoolTransactionHash))
                                                        {
                                                            _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Add(memPoolTransactionHash, BuildTransactionInformationObject(walletAddress, transactionObject, true));
                                                            requireUpdate = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        #endregion

                                        #region List all tx synced from blocks.

                                        if (!requireUpdate)
                                        {
                                            if (lastBlockHeightProgress <= walletLastBlockHeightSync)
                                            {
                                                long previousTransactionCount = _dictionaryTransactionHistory[walletFileOpened].LastTransactionCount;
                                                long walletTotalTransactionCount = walletDataObject.WalletTotalMemPoolTransaction;
                                                bool changeDone = false;

                                                walletTotalTransactionCount += walletDataObject.WalletTotalTransaction;
                                                _dictionaryTransactionHistory[walletFileOpened].LastTransactionCountOnRead = walletTotalTransactionCount;

                                                // Travel transactions synced push on blocks unlocked.
                                                if (ClassDesktopWalletCommonData.WalletSyncSystem.DatabaseSyncCache.ContainsKey(walletAddress))
                                                {
                                                    using (var listBlockHeight = ClassDesktopWalletCommonData.WalletSyncSystem.DatabaseSyncCache[walletAddress].BlockHeightKeys)
                                                    {
                                                        foreach (long blockHeight in listBlockHeight.GetList.OrderBy(x => x))
                                                        {
                                                            if (cancellation.IsCancellationRequested)
                                                                break;

                                                            if (blockHeight < _dictionaryTransactionHistory[walletFileOpened].LastBlockHeight)
                                                                continue;

                                                            // Travel every block transaction hash synced and listed on the wallet file opened.
                                                            using (var listBlockTransactionCached = await ClassDesktopWalletCommonData.WalletSyncSystem.DatabaseSyncCache[walletAddress].GetBlockTransactionFromBlockHeight(blockHeight, cancellation))
                                                            {
                                                                foreach (var blockTransactionCached in listBlockTransactionCached.GetList)
                                                                {
                                                                    if (cancellation.IsCancellationRequested)
                                                                        break;

                                                                    if (blockTransactionCached.Value != null)
                                                                    {
                                                                        if (!_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.ContainsKey(blockTransactionCached.Key))
                                                                        {
                                                                            _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Add(blockTransactionCached.Key, BuildTransactionInformationObject(walletAddress, blockTransactionCached.Value.BlockTransaction.TransactionObject, blockTransactionCached.Value.IsMemPool));
                                                                            changeDone = true;
                                                                        }
                                                                        else
                                                                        {
                                                                            if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed[blockTransactionCached.Key].IsMemPool && !blockTransactionCached.Value.IsMemPool)
                                                                            {
                                                                                _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed[blockTransactionCached.Key] = BuildTransactionInformationObject(walletAddress, blockTransactionCached.Value.BlockTransaction.TransactionObject, blockTransactionCached.Value.IsMemPool);
                                                                                changeDone = true;
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            _dictionaryTransactionHistory[walletFileOpened].LastBlockHeight = blockHeight;

                                                        }
                                                    }
                                                }

                                                if (changeDone)
                                                {
                                                    _dictionaryTransactionHistory[walletFileOpened].LastTransactionCount = walletTotalTransactionCount;
                                                    requireUpdate = true;
                                                }
                                            }
                                        }

                                        #endregion

                                        #region Check transactions showed if no update of the transaction history is required. Then update the history if necessary.

                                        if (!requireUpdate)
                                        {
                                            foreach (var transactionShowed in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.Keys)
                                            {
                                                if (cancellation.IsCancellationRequested)
                                                    break;

                                                try
                                                {

                                                    long blockHeight = _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed[transactionShowed].BlockTransaction.TransactionObject.BlockHeightTransaction;

                                                    var tupleBlockTransaction = await ClassDesktopWalletCommonData.WalletSyncSystem.GetTransactionObjectFromSync(walletAddress, transactionShowed, blockHeight, false, cancellation);

                                                    if (tupleBlockTransaction?.Item2 != null)
                                                    {
                                                        if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed[transactionShowed].IsMemPool && !tupleBlockTransaction.Item1)
                                                            requireUpdate = true;
                                                        else
                                                        {
                                                            if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed[transactionShowed].BlockTransaction.TransactionStatus != tupleBlockTransaction.Item2.TransactionStatus)
                                                                requireUpdate = true;
                                                        }

                                                        if (requireUpdate)
                                                        {
                                                            _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed[transactionShowed].IsMemPool = tupleBlockTransaction.Item1;
                                                            _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed[transactionShowed].BlockTransaction = tupleBlockTransaction.Item2;
                                                        }
                                                    }
                                                }
                                                catch (Exception error)
                                                {
#if DEBUG
                                                    Debug.WriteLine("Error on checking transactions showed. | Exception: " + error.Message);
#endif
                                                    requireUpdate = true;
                                                    break;
                                                }
                                            }
                                        }

                                        #endregion

                                        #region Then, show all tx per pages if an update is required or asked.

                                        if (_dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage || requireUpdate || _dictionaryTransactionHistory[walletFileOpened].OnLoad)
                                        {
                                            try
                                            {
                                                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.Clear(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryBackgroundColorOnClear);
                                                _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.Clear();
                                                _dictionaryTransactionHistory[walletFileOpened].TotalTransactionShowed = 0;

                                                int totalTransactionToShow = _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage * ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage;


                                                if (totalTransactionToShow > 0)
                                                {

                                                    int totalToSkip = ((_dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage - 1) * ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage);

                                                    int totalToTake = (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Count - totalToSkip);



                                                    if (totalToTake > 0)
                                                    {
                                                        if (totalToTake > ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage)
                                                            totalToTake = ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage;

                                                        if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Count >= totalToSkip + totalToTake)
                                                        {

                                                            _dictionaryTransactionHistory[walletFileOpened].InitializeOrClearPanelTransactionHistoryGraphicsContent();


                                                            foreach (TransactionHistoryInformationObject transactionHistoryInformationObject in GetTransactionHistoryInformationObjectOrdered(walletFileOpened, totalToSkip, totalToTake))
                                                            {
                                                                if (cancellation.IsCancellationRequested)
                                                                    break;

                                                                ClassBlockTransaction blockTransaction = null;

                                                                if (transactionHistoryInformationObject.IsMemPool)
                                                                {
                                                                    var memPoolTransactionObject = await ClassDesktopWalletCommonData.WalletSyncSystem.GetMemPoolTransactionObjectFromSync(walletAddress, transactionHistoryInformationObject.TransactionHash, false, cancellation);

                                                                    if (memPoolTransactionObject != null)
                                                                        blockTransaction = new ClassBlockTransaction(0, memPoolTransactionObject)
                                                                        {
                                                                            TransactionStatus = true,
                                                                        };
                                                                }

                                                                if (blockTransaction == null)
                                                                {
                                                                    var tupleBlockTransaction = await ClassDesktopWalletCommonData.WalletSyncSystem.GetTransactionObjectFromSync(walletAddress, transactionHistoryInformationObject.TransactionHash, ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHistoryInformationObject.TransactionHash), false, cancellation);
                                                                    blockTransaction = tupleBlockTransaction.Item2;
                                                                }


                                                                if (blockTransaction == null)
                                                                {
                                                                    if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Remove(transactionHistoryInformationObject.TransactionHash))
                                                                    {
#if DEBUG
                                                                        Debug.WriteLine("empty transaction for hash: " + transactionHistoryInformationObject.TransactionHash);
#endif
                                                                        doClean = true;
                                                                        break;
                                                                    }
                                                                }

                                                                if (PaintTransactionObjectToTransactionHistory(walletFileOpened, transactionHistoryInformationObject, _dictionaryTransactionHistory[walletFileOpened].TotalTransactionShowed, blockTransaction, true))
                                                                {
                                                                    updated = true;
                                                                    _dictionaryTransactionHistory[walletFileOpened].TotalTransactionShowed++;
                                                                }
                                                                else
                                                                {
                                                                    doClean = true;
                                                                    break;
                                                                }

                                                                if (_dictionaryTransactionHistory[walletFileOpened].TotalTransactionShowed >= ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage ||
                                                                   _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.Count >= ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage)
                                                                    break;
                                                            }

                                                            // Show empty column transaction.
                                                            if (totalToTake < ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage)
                                                            {
                                                                int left = ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage - totalToTake;

                                                                for (int i = 0; i < left; i++)
                                                                {
                                                                    if (PaintTransactionObjectToTransactionHistory(walletFileOpened, null, _dictionaryTransactionHistory[walletFileOpened].TotalTransactionShowed, null, false))
                                                                    {
                                                                        updated = true;
                                                                        _dictionaryTransactionHistory[walletFileOpened].TotalTransactionShowed++;
                                                                    }
                                                                    else
                                                                    {
                                                                        doClean = true;
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                            }
                                            catch (Exception error)
                                            {
#if DEBUG
                                                Debug.WriteLine("1. Error on drawing transaction history. Exception: " + error.Message);
#endif
                                                exception = true;
                                            }


                                        }

                                        #endregion


                                        if (doClean || exception)
                                        {
                                            _dictionaryTransactionHistory[walletFileOpened].ClearTransactionHistoryContent();
                                            _dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage = true;
                                            _dictionaryTransactionHistory[walletFileOpened].OnLoad = true;
                                        }
                                        else
                                        {
                                            _dictionaryTransactionHistory[walletFileOpened].OnLoad = false;
                                            _dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage = false;
                                        }


                                        if (walletDataObject.WalletLastBlockHeightSynced == 0 || (walletDataObject.WalletTotalMemPoolTransaction == 0 && walletDataObject.WalletTotalTransaction == 0))
                                        {
                                            _dictionaryTransactionHistory[walletFileOpened].LastBlockHeight = 0;
                                            _dictionaryTransactionHistory[walletFileOpened].OnLoad = false;
                                            _dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage = false;
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
                catch (Exception error)
                {
#if DEBUG
                    Debug.WriteLine("2. Error on drawing transaction history. Exception: " + error.Message);
#endif
                    try
                    {
                        if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                            _dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage = true;
                    }
                    catch
                    {
                        changed = true;
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreTransactionHistoryAccess.Release();
            }


            if (doClean || updated || exception)
                changed = true;

            return changed;
        }

        /// <summary>
        /// Return the amount of the transaction history listed about the wallet file opened.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public long GetTransactionHistoryCountOfWalletFileOpened(string walletFileOpened, CancellationTokenSource cancellation)
        {
            bool useSemaphore = false;

            try
            {
                useSemaphore = _semaphoreTransactionHistoryAccess.TryWait(cancellation);

                if (useSemaphore)
                {
                    try
                    {
                        if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                            return _dictionaryTransactionHistory[walletFileOpened].LastTransactionCount;
                        
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            }
            finally
            {
                if (useSemaphore)
                    _semaphoreTransactionHistoryAccess.Release();
            }
            return 0;
        }


        #region Transaction history management functions.

        /// <summary>
        /// Stop the transaction history update task.
        /// </summary>
        public void ClearTransactionHistory()
        {
            // Clean up.
            _dictionaryTransactionHistory.Clear();
        }

        /// <summary>
        /// Check if the transaction history system contain a wallet file opened.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <returns></returns>
        public bool ContainsTransactionHistoryToWalletFileOpened(string walletFileOpened)
        {
            bool exist = false;

            if (!walletFileOpened.IsNullOrEmpty(false, out _))
            {
                if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                    exist = true;
                else
                    RemoveTransactionHistoryFromWalletFileOpenedTarget(walletFileOpened);
            }

            return exist;
        }

        /// <summary>
        /// Clear the transaction history dedicated to a wallet file opened.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <returns></returns>
        public void ClearTransactionHistoryOfWalletFileOpened(string walletFileOpened)
        {
            if (!walletFileOpened.IsNullOrEmpty(false, out _))
                RemoveTransactionHistoryFromWalletFileOpenedTarget(walletFileOpened);
        }

        /// <summary>
        /// Insert a wallet file opened indexed on the transaction history.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="height"></param>
        /// <param name="cancellation"></param>
        /// <param name="width"></param>
        public void InsertTransactionHistoryToWalletFileOpened(string walletFileOpened, int width, int height)
        {
            if (!_dictionaryTransactionHistory.TryAdd(walletFileOpened, new ClassTransactionHistoryObject(width, height)))
                RemoveTransactionHistoryFromWalletFileOpenedTarget(walletFileOpened);
            else
            {
                _dictionaryTransactionHistory[walletFileOpened].OnLoad = true;
                _dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage = true;
            }
        }

        /// <summary>
        /// Remove a wallet file opened indexed on the transaction history.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        public void RemoveTransactionHistoryFromWalletFileOpenedTarget(string walletFileOpened)
        {
            if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
            {
                try
                {
                    _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Clear();
                    _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.Clear();
                    if (_dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory != null && _dictionaryTransactionHistory[walletFileOpened].BitmapTransactionHistory != null)
                    {
                        try
                        {
                            _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.Dispose();
                            _dictionaryTransactionHistory[walletFileOpened].BitmapTransactionHistory.Dispose();
                        }
                        catch
                        {
                            // Ignored.
                        }
                    }
                    _dictionaryTransactionHistory.TryRemove(walletFileOpened, out _);
                }
                catch
                {
                    // Ignored.
                }
            }
        }

        /// <summary>
        /// Get the load status of the transaction history.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="progressPercent"></param>
        /// <returns></returns>
        public bool GetLoadStatus(string walletFileOpened, out double progressPercent)
        {
            bool loadStatus = false; // Default.
            progressPercent = 0; // Default.

            if (!walletFileOpened.IsNullOrEmpty(false, out _))
            {

                if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                {
                    loadStatus = _dictionaryTransactionHistory[walletFileOpened].OnLoad;

                    if (loadStatus)
                    {
                        if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Count > 0 && _dictionaryTransactionHistory[walletFileOpened].LastTransactionCountOnRead > 0)
                        {
                            progressPercent = ((double)_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Count / _dictionaryTransactionHistory[walletFileOpened].LastTransactionCountOnRead) * 100d;
                            progressPercent = Math.Round(progressPercent, 2);
                        }
                    }
                }
            }

            return loadStatus;
        }

        #endregion

        #region Transaction history page event/functions.

        /// <summary>
        /// Reach the next page of the transaction history of a wallet file opened.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="page"></param>
        /// <param name="cancellation"></param>
        public void SetPageTransactionHistory(string walletFileOpened, int page, CancellationTokenSource cancellation)
        {
            if (!walletFileOpened.IsNullOrEmpty(false, out _))
            {
                bool useSemaphore = false;

                try
                {
                    _semaphoreTransactionHistoryAccess.Wait(cancellation.Token);

                    useSemaphore = true;

                    if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                    {
                        if (!_dictionaryTransactionHistory[walletFileOpened].OnLoad && !_dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage)
                        {
                            if (_dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage != page)
                            {
                                double calculation = (double)_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Count / ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage;

                                int maxPage = (int)Math.Ceiling(Math.Round(calculation, 2));

                                if (page <= maxPage)
                                {
                                    _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage = page;
                                    _dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage = true;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (useSemaphore)
                        _semaphoreTransactionHistoryAccess.Release();
                }
            }
        }

        /// <summary>
        /// Reach the next page of the transaction history of a wallet file opened.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="cancellation"></param>
        /// <param name="currentPage"></param>
        public void NextPageTransactionHistory(string walletFileOpened, CancellationTokenSource cancellation, out int currentPage)
        {
            currentPage = 1; // Default.

            if (!walletFileOpened.IsNullOrEmpty(false, out _))
            {
                bool useSemaphore = false;

                try
                {
                    _semaphoreTransactionHistoryAccess.Wait(cancellation.Token);

                    useSemaphore = true;

                    if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                    {
                        if (!_dictionaryTransactionHistory[walletFileOpened].OnLoad && !_dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage)
                        {
                            if (_dictionaryTransactionHistory[walletFileOpened].TotalTransactionShowed < _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Count)
                            {

                                double calculation = (double)_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Count / ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage;

                                int maxPage = (int)Math.Ceiling(Math.Round(calculation, 2));

                                if (maxPage >= _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage + 1)
                                {
                                    _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage++;
                                    currentPage = _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage;
                                    _dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage = true;
                                }
                                else
                                    currentPage = maxPage;
                            }
                        }
                        else
                            currentPage = _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage;
                    }
                }
                finally
                {
                    if (useSemaphore)
                        _semaphoreTransactionHistoryAccess.Release();
                }
            }
        }

        /// <summary>
        /// Reach the next page of the transaction history of a wallet file opened.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="cancellation"></param>
        /// <param name="currentPage"></param>
        public void BackPageTransactionHistory(string walletFileOpened, CancellationTokenSource cancellation, out int currentPage)
        {
            currentPage = 1; // Default.
            if (!walletFileOpened.IsNullOrEmpty(false, out _))
            {
                bool useSemaphore = false;

                try
                {
                    _semaphoreTransactionHistoryAccess.Wait(cancellation.Token);

                    useSemaphore = true;

                    if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                    {
                        if (!_dictionaryTransactionHistory[walletFileOpened].OnLoad && !_dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage)
                        {
                            if (_dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage > 1)
                            {
                                _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage--;
                                _dictionaryTransactionHistory[walletFileOpened].TotalTransactionShowed -= ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage;

                                if (_dictionaryTransactionHistory[walletFileOpened].TotalTransactionShowed < 0)
                                    _dictionaryTransactionHistory[walletFileOpened].TotalTransactionShowed = 0;

                                currentPage = _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage;
                                _dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage = true;
                            }
                            else
                                currentPage = _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage;
                        }
                        else
                            currentPage = _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage;
                    }
                }
                finally
                {
                    if (useSemaphore)
                        _semaphoreTransactionHistoryAccess.Release();
                }
            }
        }

        /// <summary>
        /// Return the current page of the transaction history.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="cancellation"></param>
        public int CurrentPageTransactionHistory(string walletFileOpened, CancellationTokenSource cancellation)
        {
            int currentPage = 1;
            if (!walletFileOpened.IsNullOrEmpty(false, out _))
            {
                bool useSemaphore = false;

                try
                {
                    _semaphoreTransactionHistoryAccess.Wait(cancellation.Token);

                    useSemaphore = true;

                    if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                        currentPage = _dictionaryTransactionHistory[walletFileOpened].CurrentTransactionHistoryPage;
                }
                finally
                {
                    if (useSemaphore)
                        _semaphoreTransactionHistoryAccess.Release();
                }
            }
            return currentPage;
        }

        /// <summary>
        /// Return the max page of the transaction history.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="cancellation"></param>
        public int MaxPageTransactionHistory(string walletFileOpened, CancellationTokenSource cancellation)
        {
            int maxPage = 1;
            if (!walletFileOpened.IsNullOrEmpty(false, out _))
            {
                bool useSemaphore = false;

                try
                {
                    _semaphoreTransactionHistoryAccess.Wait(cancellation.Token);
                    useSemaphore = true;

                    if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                    {
                        double calculation = (double)_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Count / ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage;

                        maxPage = (int)Math.Ceiling(Math.Round(calculation, 2));
                    }
                }
                finally
                {
                    if (useSemaphore)
                        _semaphoreTransactionHistoryAccess.Release();
                }
            }
            return maxPage;
        }

        /// <summary>
        /// Enable hover on a transaction selected by click.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="mouseClickPoint"></param>
        /// <param name="transactionHash"></param>
        /// <param name="rectangleTransactionPosition"></param>
        public bool EnableTransactionHoverByClick(string walletFileOpened, Point mouseClickPoint, CancellationTokenSource cancellation, out string transactionHash, out Rectangle rectangleTransactionPosition)
        {
            bool useSemaphore = false;
            bool containsPosition = false;
            transactionHash = null; // Default.
            rectangleTransactionPosition = new Rectangle(); // Default.

            try
            {
                _semaphoreTransactionHistoryAccess.Wait(cancellation.Token);

                useSemaphore = true;

                try
                {
                    if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.Count > 0)
                    {
                        foreach (var rectangleTransaction in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.ToArray())
                        {
                            if (rectangleTransaction.Value.RectangleTransaction.Contains(mouseClickPoint))
                            {
                                _dictionaryTransactionHistory[walletFileOpened].TransactionInformationSelectedByClick = rectangleTransaction.Key;
                                transactionHash = rectangleTransaction.Key;
                                containsPosition = true;
                                rectangleTransactionPosition = rectangleTransaction.Value.RectangleTransaction;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignored.
                }
            }
            finally
            {
                if (useSemaphore)
                    _semaphoreTransactionHistoryAccess.Release();
                
            }
            return containsPosition;
        }

        /// <summary>
        /// Enable hover on a transaction selected by position.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="mouseClickPoint"></param>
        /// <param name="transactionHash"></param>
        /// <param name="rectangleTransactionPosition"></param>
        public bool EnableTransactionHoverByPosition(string walletFileOpened, Point mouseClickPoint, CancellationTokenSource cancellation, out string transactionHash, out Rectangle rectangleTransactionPosition)
        {
            bool useSemaphore = false;
            bool containsPosition = false;
            transactionHash = null; // Default.
            rectangleTransactionPosition = new Rectangle(); // Default.
            try
            {

                try
                {
                    _semaphoreTransactionHistoryAccess.Wait(cancellation.Token);
                    useSemaphore = true;

                    if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.Count > 0)
                    {
                        foreach (var rectangleTransaction in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.ToArray())
                        {
                            if (rectangleTransaction.Value.RectangleTransaction.Contains(mouseClickPoint))
                            {
                                _dictionaryTransactionHistory[walletFileOpened].TransactionInformationSelectedByPosition = rectangleTransaction.Key;
                                transactionHash = rectangleTransaction.Key;
                                containsPosition = true;
                                rectangleTransactionPosition = rectangleTransaction.Value.RectangleTransaction;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignored.
                }
            }
            finally
            {
                if (useSemaphore)
                    _semaphoreTransactionHistoryAccess.Release();
            }

            return containsPosition;
        }

        /// <summary>
        /// Disable transaction hover by position.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        public void DisableTransactionHoverByPosition(string walletFileOpened)
        {
            try
            {
                if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.Count > 0)
                    _dictionaryTransactionHistory[walletFileOpened].TransactionInformationSelectedByPosition = string.Empty;
            }
            catch
            {
                // Ignored.
            }
        }

        /// <summary>
        /// Retrieve back a block transaction showed on the transaction history from an event click.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="cancellation"></param>
        /// <param name="mouseClickPoint"></param>
        /// <param name="found"></param>
        /// <param name="isMemPool"></param>
        /// 
        /// <returns></returns>
        public ClassBlockTransaction GetBlockTransactionShowedFromClick(string walletFileOpened, CancellationTokenSource cancellation, Point mouseClickPoint, out bool found, out bool isMemPool)
        {
            bool useSemaphore = false;
            found = false; // Default.
            isMemPool = false; // Default.
            ClassBlockTransaction blockTransaction = null; // Default.

            if (!walletFileOpened.IsNullOrEmpty(false, out _))
            {
                try
                {
                    _semaphoreTransactionHistoryAccess.Wait(cancellation.Token);
                    useSemaphore = true;

                    if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                    {
                        if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.Count > 0)
                        {
                            try
                            {
                                foreach (var rectangleTransaction in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.ToArray())
                                {
                                    if (cancellation.IsCancellationRequested)
                                        break;

                                    if (rectangleTransaction.Value.RectangleTransaction.Contains(mouseClickPoint))
                                    {
                                        _dictionaryTransactionHistory[walletFileOpened].TransactionInformationSelectedByClick = rectangleTransaction.Key;
                                        blockTransaction = rectangleTransaction.Value.BlockTransaction;
                                        isMemPool = rectangleTransaction.Value.IsMemPool;
                                        if (blockTransaction != null)
                                            found = true;
                                        
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                found = false;
                            }
                        }
                    }
                }
                finally
                {
                    if (useSemaphore)
                        _semaphoreTransactionHistoryAccess.Release();
                }
            }

            return blockTransaction;
        }

        #endregion

        #region Transaction history manage order functions.

        /// <summary>
        /// Set the order type of the transaction history.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="mouseClickPosition"></param>
        /// <param name="walletMainFormLanguage"></param>
        /// <param name="cancellation"></param>
        /// <param name="panelTransactionHistoryColumns"></param>
        public void SetOrderTypeTransactionHistory(string walletFileOpened, Point mouseClickPosition, Panel panelTransactionHistoryColumns, ClassWalletMainFormLanguage walletMainFormLanguage, CancellationTokenSource cancellation)
        {
            if (!walletFileOpened.IsNullOrEmpty(false, out _))
            {
                bool useSemaphore = false;

                try
                {
                    _semaphoreTransactionHistoryAccess.Wait(cancellation.Token);

                    useSemaphore = true;

                    if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
                    {
                        if (!_dictionaryTransactionHistory[walletFileOpened].OnLoad && !_dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage)
                        {
                            ClassEnumTransactionHistoryColumnType typeSelected = ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE;
                            bool typeFound = false;

                            foreach (var typePair in _dictionaryTransactionHistoryOrderType.ToArray())
                            {
                                if (typePair.Value.RectangleOrderTypeLocation.Contains(mouseClickPosition.X, mouseClickPosition.Y))
                                {
                                    typeSelected = typePair.Key;
                                    typeFound = true;
                                    break;
                                }
                            }

                            if (typeFound)
                            {
                                if (_dictionaryTransactionHistoryOrderType[typeSelected].OrderByDescending)
                                {
                                    _dictionaryTransactionHistoryOrderType[typeSelected].OrderByDescending = false;
                                }
                                else
                                {
                                    _dictionaryTransactionHistoryOrderType[typeSelected].OrderByDescending = true;
                                }
                                _dictionaryTransactionHistory[walletFileOpened].TransactionHistoryColumnOrdering = typeSelected;

                                DrawTransactionHistoryColumns(panelTransactionHistoryColumns, walletMainFormLanguage, typeSelected);
                                _dictionaryTransactionHistory[walletFileOpened].EnableEventDrawPage = true;
                            }
                        }
                    }
                }
                finally
                {
                    if (useSemaphore)
                    {
                        _semaphoreTransactionHistoryAccess.Release();
                    }
                }
            }
        }

        #endregion

        #region Transaction history data functions.

        /// <summary>
        /// Insert a transaction information into the transaction history list.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="transactionObject"></param>
        /// <param name="isMemPool"></param>
        private TransactionHistoryInformationObject BuildTransactionInformationObject(string walletAddress, ClassTransactionObject transactionObject, bool isMemPool)
        {
            string walletAddressToShow = string.Empty;
            string textTypeToShow = string.Empty;
            BigInteger amountToShow = 0;
            BigInteger feeToShow = 0;

            switch (transactionObject.TransactionType)
            {
                case ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION:
                    {
                        walletAddressToShow = transactionObject.WalletAddressReceiver;
                        textTypeToShow = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletMainFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_MAIN_FORM).ROW_TRANSACTION_TYPE_BLOCK_REWARD_TEXT;
                        amountToShow = transactionObject.Amount;
                    }
                    break;
                case ClassTransactionEnumType.DEV_FEE_TRANSACTION:
                    {
                        walletAddressToShow = transactionObject.WalletAddressReceiver;
                        textTypeToShow = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletMainFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_MAIN_FORM).ROW_TRANSACTION_TYPE_DEV_FEE_TEXT;
                        amountToShow = transactionObject.Amount;
                    }
                    break;
                case ClassTransactionEnumType.TRANSFER_TRANSACTION:
                    {

                        if (walletAddress == transactionObject.WalletAddressSender)
                        {
                            walletAddressToShow = transactionObject.WalletAddressReceiver;

                            textTypeToShow = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletMainFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_MAIN_FORM).ROW_TRANSACTION_TYPE_TRANSFER_TRANSACTION_SENT_TEXT;

                            amountToShow = transactionObject.Amount + transactionObject.Fee;
                        }
                        else
                        {
                            walletAddressToShow = transactionObject.WalletAddressSender;

                            textTypeToShow = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletMainFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_MAIN_FORM).ROW_TRANSACTION_TYPE_TRANSFER_TRANSACTION_RECEIVED_TEXT;
                            amountToShow = transactionObject.Amount;
                        }

                        feeToShow = transactionObject.Fee;
                    }
                    break;
                case ClassTransactionEnumType.NORMAL_TRANSACTION:
                    {

                        if (walletAddress == transactionObject.WalletAddressSender)
                        {
                            walletAddressToShow = transactionObject.WalletAddressReceiver;

                            textTypeToShow = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletMainFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_MAIN_FORM).ROW_TRANSACTION_TYPE_NORMAL_TRANSACTION_SENT_TEXT;

                            amountToShow = transactionObject.Amount + transactionObject.Fee;
                        }
                        else
                        {
                            walletAddressToShow = transactionObject.WalletAddressSender;

                            textTypeToShow = ClassDesktopWalletCommonData.LanguageDatabase.GetLanguageContentObject<ClassWalletMainFormLanguage>(ClassLanguageEnumType.LANGUAGE_TYPE_MAIN_FORM).ROW_TRANSACTION_TYPE_NORMAL_TRANSACTION_RECEIVED_TEXT;
                            amountToShow = transactionObject.Amount;
                        }

                        feeToShow = transactionObject.Fee;
                    }
                    break;

            }

            return new TransactionHistoryInformationObject()
            {
                IsMemPool = isMemPool,
                DateSent = ClassUtility.GetDatetimeFromTimestamp(transactionObject.TimestampSend),
                TransactionType = textTypeToShow,
                Amount = amountToShow,
                Fee = feeToShow,
                WalletAddress = walletAddressToShow,
                TransactionHash = transactionObject.TransactionHash
            };
        }

        /// <summary>
        /// Retrieve back transactions ordered in a column selected, in ordering by ascending/decending.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="totalToSkip"></param>
        /// <param name="totalToTake"></param>
        /// <returns></returns>
        private IEnumerable<TransactionHistoryInformationObject> GetTransactionHistoryInformationObjectOrdered(string walletFileOpened, int totalToSkip, int totalToTake)
        {
            switch (_dictionaryTransactionHistory[walletFileOpened].TransactionHistoryColumnOrdering)
            {
                case ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE:
                    {
                        switch (_dictionaryTransactionHistoryOrderType[_dictionaryTransactionHistory[walletFileOpened].TransactionHistoryColumnOrdering].OrderByDescending)
                        {
                            case true:
                                foreach (var transactionPair in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.OrderByDescending(x => x.Value.DateSent).Skip(totalToSkip).Take(totalToTake))
                                    yield return transactionPair.Value;
                                break;
                            case false:
                                foreach (var transactionPair in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.OrderBy(x => x.Value.DateSent).Skip(totalToSkip).Take(totalToTake))
                                    yield return transactionPair.Value;
                                break;
                        }
                    }
                    break;
                case ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_TYPE:
                    {
                        switch (_dictionaryTransactionHistoryOrderType[_dictionaryTransactionHistory[walletFileOpened].TransactionHistoryColumnOrdering].OrderByDescending)
                        {
                            case true:
                                foreach (var transactionPair in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.OrderByDescending(x => x.Value.TransactionType).Skip(totalToSkip).Take(totalToTake))
                                    yield return transactionPair.Value;
                                break;
                            case false:
                                foreach (var transactionPair in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.OrderBy(x => x.Value.TransactionType).Skip(totalToSkip).Take(totalToTake))
                                    yield return transactionPair.Value;
                                break;
                        }
                    }
                    break;
                case ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_WALLET_ADDRESS:
                    {
                        switch (_dictionaryTransactionHistoryOrderType[_dictionaryTransactionHistory[walletFileOpened].TransactionHistoryColumnOrdering].OrderByDescending)
                        {
                            case true:
                                foreach (var transactionPair in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.OrderByDescending(x => x.Value.WalletAddress).Skip(totalToSkip).Take(totalToTake))
                                    yield return transactionPair.Value;
                                break;
                            case false:
                                foreach (var transactionPair in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.OrderBy(x => x.Value.WalletAddress).Skip(totalToSkip).Take(totalToTake))
                                    yield return transactionPair.Value;
                                break;
                        }
                    }
                    break;
                case ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_HASH:
                    {
                        switch (_dictionaryTransactionHistoryOrderType[_dictionaryTransactionHistory[walletFileOpened].TransactionHistoryColumnOrdering].OrderByDescending)
                        {
                            case true:
                                foreach (var transactionPair in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.OrderByDescending(x => x.Value.TransactionHash).Skip(totalToSkip).Take(totalToTake))
                                    yield return transactionPair.Value;
                                break;
                            case false:
                                foreach (var transactionPair in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.OrderBy(x => x.Value.TransactionHash).Skip(totalToSkip).Take(totalToTake))
                                    yield return transactionPair.Value;
                                break;
                        }
                    }
                    break;
                case ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_AMOUNT:
                    {
                        switch (_dictionaryTransactionHistoryOrderType[_dictionaryTransactionHistory[walletFileOpened].TransactionHistoryColumnOrdering].OrderByDescending)
                        {
                            case true:
                                foreach (var transactionPair in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.OrderByDescending(x => x.Value.Amount).Skip(totalToSkip).Take(totalToTake))
                                    yield return transactionPair.Value;
                                break;
                            case false:
                                foreach (var transactionPair in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.OrderBy(x => x.Value.Amount).Skip(totalToSkip).Take(totalToTake))
                                    yield return transactionPair.Value;
                                break;
                        }
                    }
                    break;
            }
        }

        #endregion

        #region Transaction history drawing functions.

        /// <summary>
        /// Paint a transaction object to the transaction history.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="transactionHistoryInformationObject"></param>
        /// <param name="totalShowedVirtually"></param>
        /// <param name="blockTransaction"></param>
        /// <param name="containTransactionData"></param>
        /// <returns></returns>
        private bool PaintTransactionObjectToTransactionHistory(string walletFileOpened, TransactionHistoryInformationObject transactionHistoryInformationObject, int totalShowedVirtually, ClassBlockTransaction blockTransaction, bool containTransactionData)
        {

            int height = _dictionaryTransactionHistory[walletFileOpened].Height / ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage;

            int positionTextY = height * (totalShowedVirtually + 1);

            if (_dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory == null || _dictionaryTransactionHistory[walletFileOpened].BitmapTransactionHistory == null)
            {
#if DEBUG
                Debug.WriteLine("Graphics panel content is empty.");
#endif
                return false;
            }

            if (transactionHistoryInformationObject == null && containTransactionData)
            {
#if DEBUG
                Debug.WriteLine("Transaction object is empty.");
#endif
                return false;
            }


            Rectangle rectangleTransaction = new Rectangle(0, height * totalShowedVirtually, _dictionaryTransactionHistory[walletFileOpened].Width, height);

            if (containTransactionData)
            {
                // Different background color on mem pool transaction to draw.
                if (transactionHistoryInformationObject.IsMemPool)
                {
                    if (!blockTransaction.TransactionStatus)
                        _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.FillRectangle(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryBackgroundColorInvalidMemPoolTransactionSolidBrush, rectangleTransaction);
                    else
                        _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.FillRectangle(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryBackgroundColorMemPoolTransactionSolidBrush, rectangleTransaction);

                    // Redraw column lines.
                    _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth, 0);
                    _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth, 0);
                    _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth, 0);
                    _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth, 0);
                    _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnAmountMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnAmountMaxWidth, 0);
                }
                else
                {
                    if (!blockTransaction.TransactionStatus)
                    {
                        _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.FillRectangle(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryBackgroundColorInvalidTransactionSolidBrush, rectangleTransaction);

                        // Redraw column lines.
                        _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth, 0);
                        _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth, 0);
                        _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth, 0);
                        _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth, 0);
                        _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnAmountMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnAmountMaxWidth, 0);
                    }
                }


                float middlePositionX = (float)_dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth / 2;
                float positionBaseX = 0;

                float positionBaseY = (positionTextY - (height / 2f)) - (_dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.MeasureString(@"test", ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont).Height / 2);

                // Draw transaction date sent.
                string dateToShow = ClassGraphicsUtility.GetMeasureStringToDraw(transactionHistoryInformationObject.DateSent.ToString(CultureInfo.CurrentUICulture), positionBaseX, _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth, _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out float widthText);

                positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawString(dateToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, ClassWalletDefaultSetting.DefaultPanelTransactionHistorySolidBrush, positionBaseX, positionBaseY);

                // Draw transaction type.
                positionBaseX = _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth;

                string typeToShow = ClassGraphicsUtility.GetMeasureStringToDraw(transactionHistoryInformationObject.TransactionType, positionBaseX, _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth, _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);

                positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawString(typeToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, ClassWalletDefaultSetting.DefaultPanelTransactionHistorySolidBrush, positionBaseX, positionBaseY);

                // Draw transaction wallet address.
                positionBaseX = _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth;

                string addressToShow = ClassGraphicsUtility.GetMeasureStringToDraw(transactionHistoryInformationObject.WalletAddress, positionBaseX, _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth, _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);


                positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawString(addressToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, ClassWalletDefaultSetting.DefaultPanelTransactionHistorySolidBrush, positionBaseX, positionBaseY);

                // Draw transaction hash.
                positionBaseX = _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth;

                string hashToShow = ClassGraphicsUtility.GetMeasureStringToDraw(transactionHistoryInformationObject.TransactionHash, positionBaseX, _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth, _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);


                positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawString(hashToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, ClassWalletDefaultSetting.DefaultPanelTransactionHistorySolidBrush, positionBaseX, positionBaseY);

                // Draw transaction amount.
                positionBaseX = _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth;

                string amountToShow = ClassGraphicsUtility.GetMeasureStringToDraw((ClassTransactionUtility.GetFormattedAmountFromBigInteger(transactionHistoryInformationObject.Amount) + @" " + BlockchainSetting.CoinMinName), positionBaseX, _dictionaryTransactionHistory[walletFileOpened].ColumnAmountMaxWidth, _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);

                positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawString(amountToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, ClassWalletDefaultSetting.DefaultPanelTransactionHistorySolidBrush, positionBaseX, positionBaseY);

                if (!_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.TryAdd(transactionHistoryInformationObject.TransactionHash, new TransactionHistoryInformationShowedObject()
                {
                    TransactionHistoryInformationObject = transactionHistoryInformationObject,
                    BlockTransaction = blockTransaction,
                    RectangleTransaction = rectangleTransaction,
                    IsMemPool = transactionHistoryInformationObject.IsMemPool
                }))
                {
                    return false;
                }
            }
            else
            {
                // Redraw column lines.
                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth, 0);
                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth, 0);
                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth, 0);
                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth, 0);
                _dictionaryTransactionHistory[walletFileOpened].GraphicsTransactionHistory.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen, _dictionaryTransactionHistory[walletFileOpened].ColumnAmountMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnAmountMaxWidth, 0);
            }


            return true;
        }

        /// <summary>
        /// Draw columns of the transaction history depending of the language selected.
        /// </summary>
        /// <param name="panelTransactionHistoryColumns"></param>
        /// <param name="walletMainFormLanguage"></param>
        /// <param name="orderType"></param>
        /// 
        public void DrawTransactionHistoryColumns(Panel panelTransactionHistoryColumns, ClassWalletMainFormLanguage walletMainFormLanguage, ClassEnumTransactionHistoryColumnType orderType)
        {
            Bitmap bitmapColumns = new Bitmap(panelTransactionHistoryColumns.Width, panelTransactionHistoryColumns.Height);

            using (Graphics graphicsColumns = Graphics.FromImage(bitmapColumns))
            {
                graphicsColumns.SmoothingMode = SmoothingMode.HighQuality;
                graphicsColumns.CompositingQuality = CompositingQuality.HighQuality;

                bool initializeOrderType = _dictionaryTransactionHistoryOrderType.Count == 0;

                graphicsColumns.FillRectangle(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnsBackgroundColorOnClearSolidBrush, new Rectangle(0, 0, bitmapColumns.Width, bitmapColumns.Height));

                int columnDateMaxWidth = bitmapColumns.Width / 5;
                int columnTypeMaxWidth = columnDateMaxWidth * 2;
                int columnWalletAddressMaxWidth = columnDateMaxWidth * 3;
                int columnHashMaxWidth = columnDateMaxWidth * 4;
                int columnAmountMaxWidth = columnDateMaxWidth * 5;

                float middlePositionX = (float)columnDateMaxWidth / 2;
                float positionBaseY = (bitmapColumns.Height - (bitmapColumns.Height / 2f)) - (graphicsColumns.MeasureString(@"test", ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont).Height / 2);

                float positionBaseX = 0;

                #region Column date.

                graphicsColumns.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColummBorderPen, columnDateMaxWidth, bitmapColumns.Height, columnDateMaxWidth, 0);

                string dateToShow = ClassGraphicsUtility.GetMeasureStringToDraw(walletMainFormLanguage.COLUMN_TRANSACTION_DATE, positionBaseX, columnDateMaxWidth, graphicsColumns, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out float widthText);

                positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                graphicsColumns.DrawString(dateToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnForeColorSolidBrush, positionBaseX, positionBaseY);

                bool orderSelected = orderType == ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE;

                if (initializeOrderType)
                {

                    _dictionaryTransactionHistoryOrderType.TryAdd(ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE, new ClassTransactionHistoryOrderObject()
                    {
                        OrderByDescending = true,
                        RectangleOrderTypeLocation = new Rectangle(0, 0, columnDateMaxWidth, bitmapColumns.Height),
                    });
                }

                DrawOrderColumnButton(graphicsColumns, positionBaseX, positionBaseY, widthText, columnDateMaxWidth, bitmapColumns.Height, _dictionaryTransactionHistoryOrderType[ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE].OrderByDescending, orderSelected);

                #endregion

                #region Column type.

                positionBaseX = columnDateMaxWidth;

                graphicsColumns.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColummBorderPen, columnTypeMaxWidth, bitmapColumns.Height, columnTypeMaxWidth, 0);

                string typeToShow = ClassGraphicsUtility.GetMeasureStringToDraw(walletMainFormLanguage.COLUMN_TRANSACTION_TYPE, positionBaseX, columnTypeMaxWidth, graphicsColumns, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);

                positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                graphicsColumns.DrawString(typeToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnForeColorSolidBrush, positionBaseX, positionBaseY);

                orderSelected = orderType == ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_TYPE;

                if (initializeOrderType)
                {
                    _dictionaryTransactionHistoryOrderType.TryAdd(ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_TYPE, new ClassTransactionHistoryOrderObject()
                    {
                        OrderByDescending = true,
                        RectangleOrderTypeLocation = new Rectangle(columnDateMaxWidth, 0, columnDateMaxWidth, bitmapColumns.Height),
                    });
                }

                DrawOrderColumnButton(graphicsColumns, positionBaseX, positionBaseY, widthText, columnDateMaxWidth, bitmapColumns.Height, _dictionaryTransactionHistoryOrderType[ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_TYPE].OrderByDescending, orderSelected);

                #endregion

                #region Column wallet address.

                positionBaseX = columnTypeMaxWidth;

                graphicsColumns.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColummBorderPen, columnWalletAddressMaxWidth, bitmapColumns.Height, columnWalletAddressMaxWidth, 0);

                string walletAddressToShow = ClassGraphicsUtility.GetMeasureStringToDraw(walletMainFormLanguage.COLUMN_TRANSACTION_WALLET_ADDRESS, positionBaseX, columnWalletAddressMaxWidth, graphicsColumns, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);

                positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                graphicsColumns.DrawString(walletAddressToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnForeColorSolidBrush, positionBaseX, positionBaseY);

                orderSelected = orderType == ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_WALLET_ADDRESS;

                if (initializeOrderType)
                {
                    _dictionaryTransactionHistoryOrderType.TryAdd(ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_WALLET_ADDRESS, new ClassTransactionHistoryOrderObject()
                    {
                        OrderByDescending = true,
                        RectangleOrderTypeLocation = new Rectangle(columnTypeMaxWidth, 0, columnDateMaxWidth, bitmapColumns.Height)
                    });
                }

                DrawOrderColumnButton(graphicsColumns, positionBaseX, positionBaseY, widthText, columnDateMaxWidth, bitmapColumns.Height, _dictionaryTransactionHistoryOrderType[ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_WALLET_ADDRESS].OrderByDescending, orderSelected);

                #endregion

                #region Column hash.

                positionBaseX = columnWalletAddressMaxWidth;

                graphicsColumns.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColummBorderPen, columnHashMaxWidth, bitmapColumns.Height, columnHashMaxWidth, 0);

                string hashToShow = ClassGraphicsUtility.GetMeasureStringToDraw(walletMainFormLanguage.COLUMN_TRANSACTION_HASH, positionBaseX, columnHashMaxWidth, graphicsColumns, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);

                positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                graphicsColumns.DrawString(hashToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnForeColorSolidBrush, positionBaseX, positionBaseY);

                orderSelected = orderType == ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_HASH;

                if (initializeOrderType)
                {
                    _dictionaryTransactionHistoryOrderType.TryAdd(ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_HASH, new ClassTransactionHistoryOrderObject()
                    {
                        OrderByDescending = true,
                        RectangleOrderTypeLocation = new Rectangle(columnWalletAddressMaxWidth, 0, columnDateMaxWidth, bitmapColumns.Height)
                    });
                }

                DrawOrderColumnButton(graphicsColumns, positionBaseX, positionBaseY, widthText, columnDateMaxWidth, bitmapColumns.Height, _dictionaryTransactionHistoryOrderType[ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_HASH].OrderByDescending, orderSelected);

                #endregion

                #region Column amount.

                positionBaseX = columnHashMaxWidth;

                graphicsColumns.DrawLine(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColummBorderPen, columnAmountMaxWidth, bitmapColumns.Height, columnAmountMaxWidth, 0);

                string amountToShow = ClassGraphicsUtility.GetMeasureStringToDraw(walletMainFormLanguage.COLUMN_TRANSACTION_AMOUNT, positionBaseX, columnAmountMaxWidth, graphicsColumns, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);

                positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                graphicsColumns.DrawString(amountToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnForeColorSolidBrush, positionBaseX, positionBaseY);

                orderSelected = orderType == ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_AMOUNT;

                if (initializeOrderType)
                {
                    _dictionaryTransactionHistoryOrderType.TryAdd(ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_AMOUNT, new ClassTransactionHistoryOrderObject()
                    {
                        OrderByDescending = true,
                        RectangleOrderTypeLocation = new Rectangle(columnHashMaxWidth, 0, columnDateMaxWidth, bitmapColumns.Height)
                    });
                }

                DrawOrderColumnButton(graphicsColumns, positionBaseX, positionBaseY, widthText, columnDateMaxWidth, bitmapColumns.Height, _dictionaryTransactionHistoryOrderType[ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_AMOUNT].OrderByDescending, orderSelected);

                #endregion

                // Complete borders
                Rectangle rectangleBorderTransaction = new Rectangle(0, bitmapColumns.Height, bitmapColumns.Width - 1, bitmapColumns.Height);

                graphicsColumns.DrawRectangle(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColummBorderPen, rectangleBorderTransaction);

                panelTransactionHistoryColumns.BackgroundImage = bitmapColumns;
                panelTransactionHistoryColumns.Refresh();
            }

        }

        /// <summary>
        /// Draw order column button of the transaction history.
        /// </summary>
        /// <param name="graphicsColumns"></param>
        /// <param name="positionTextX"></param>
        /// <param name="heightColumn"></param>
        /// <param name="widthColumn"></param>
        /// <param name="orderDescending"></param>
        /// <param name="positionTextY"></param>
        /// <param name="widthText"></param>
        /// <param name="selected"></param>
        private void DrawOrderColumnButton(Graphics graphicsColumns, float positionTextX, float positionTextY, float widthText, float widthColumn, float heightColumn, bool orderDescending, bool selected)
        {
            float buttonOrderColumnHeight = heightColumn / 3f;
            float buttonOrderColumnPositionY = positionTextY;
            float buttonOrderColumnWidth = widthColumn / 15f;

            Rectangle rectangleOrderColumnButton = new Rectangle((int)(positionTextX + widthText), (int)buttonOrderColumnPositionY, (int)buttonOrderColumnWidth, (int)buttonOrderColumnHeight);

            int halfWidth = rectangleOrderColumnButton.Width / 2;
            Point p0 = Point.Empty;
            Point p1 = Point.Empty;
            Point p2 = Point.Empty;

            switch (orderDescending)
            {
                case true:
                    p0 = new Point(rectangleOrderColumnButton.Left + halfWidth, rectangleOrderColumnButton.Top);
                    p1 = new Point(rectangleOrderColumnButton.Left, rectangleOrderColumnButton.Bottom);
                    p2 = new Point(rectangleOrderColumnButton.Right, rectangleOrderColumnButton.Bottom);
                    break;
                case false:
                    p0 = new Point(rectangleOrderColumnButton.Left + halfWidth, rectangleOrderColumnButton.Bottom);
                    p1 = new Point(rectangleOrderColumnButton.Left, rectangleOrderColumnButton.Top);
                    p2 = new Point(rectangleOrderColumnButton.Right, rectangleOrderColumnButton.Top);
                    break;
            }

            if (!selected)
                graphicsColumns.FillPolygon(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnButtonOrderColumnsSolidBrushUnselected, new[] { p0, p1, p2 });
            else
                graphicsColumns.FillPolygon(ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnButtonOrderColumnsSolidBrushSelected, new[] { p0, p1, p2 });
        }

        /// <summary>
        /// Paint the transaction hover by click to the transaction history.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="graphicsTarget"></param>
        /// <param name="byClick"></param>
        public void PaintTransactionHoverToTransactionHistory(string walletFileOpened, Graphics graphicsTarget, bool byClick)
        {
            try
            {
                graphicsTarget.SmoothingMode = SmoothingMode.HighQuality;
                graphicsTarget.CompositingQuality = CompositingQuality.HighQuality;

                string transactionHashSelected;
                SolidBrush solidBrushTextSelected;
                SolidBrush solidBrushBackgroundSelected;
                Pen penBorderSelected = ClassWalletDefaultSetting.DefaultPanelTransactionHistoryColumnLinesPen;

                if (_dictionaryTransactionHistory[walletFileOpened].TransactionInformationSelectedByClick == _dictionaryTransactionHistory[walletFileOpened].TransactionInformationSelectedByPosition)
                    byClick = true;

                if (byClick)
                {
                    transactionHashSelected = _dictionaryTransactionHistory[walletFileOpened].TransactionInformationSelectedByClick;
                    solidBrushTextSelected = ClassWalletDefaultSetting.DefaultPanelTransactionHistorySolidBrush;
                    solidBrushBackgroundSelected = ClassWalletDefaultSetting.DefaultPanelTransactionHistoryBackgroundColorSelectedTransactionSolidBrushByClick;
                }
                else
                {
                    transactionHashSelected = _dictionaryTransactionHistory[walletFileOpened].TransactionInformationSelectedByPosition;
                    solidBrushTextSelected = ClassWalletDefaultSetting.DefaultPanelTransactionHistorySolidBrushPosition;
                    solidBrushBackgroundSelected = ClassWalletDefaultSetting.DefaultPanelTransactionHistoryBackgroundColorSelectedTransactionSolidBrushByPosition;
                }

                if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.ContainsKey(transactionHashSelected))
                {
                    Rectangle rectangleTransaction = _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed[transactionHashSelected].RectangleTransaction;
                    TransactionHistoryInformationObject transactionHistoryInformationObject = _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed[transactionHashSelected].TransactionHistoryInformationObject;

                    // Draw the background color of the selected transaction.
                    graphicsTarget.FillRectangle(solidBrushBackgroundSelected, rectangleTransaction);

                    // Redraw column lines.
                    graphicsTarget.DrawLine(penBorderSelected, _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth, 0);
                    graphicsTarget.DrawLine(penBorderSelected, _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth, 0);
                    graphicsTarget.DrawLine(penBorderSelected, _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth, 0);
                    graphicsTarget.DrawLine(penBorderSelected, _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth, 0);
                    graphicsTarget.DrawLine(penBorderSelected, _dictionaryTransactionHistory[walletFileOpened].ColumnAmountMaxWidth, _dictionaryTransactionHistory[walletFileOpened].Height, _dictionaryTransactionHistory[walletFileOpened].ColumnAmountMaxWidth, 0);

                    // Draw borders of the selected transaction.
                    graphicsTarget.DrawRectangle(ClassWalletDefaultSetting.DefaultPanelTransactionHistorySelectClickBorderPen, rectangleTransaction.X, rectangleTransaction.Y, rectangleTransaction.Width, rectangleTransaction.Height - 0.5f);

                    int height = _dictionaryTransactionHistory[walletFileOpened].Height / ClassWalletDefaultSetting.DefaultWalletMaxTransactionInHistoryPerPage;

                    int positionTextY = rectangleTransaction.Y + (height);

                    float middlePositionX = (float)_dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth / 2;
                    float positionBaseX = 0;

                    float positionBaseY = (positionTextY - (height / 2f)) - (graphicsTarget.MeasureString(@"test", ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont).Height / 2);

                    // Draw transaction date sent.
                    string dateToShow = ClassGraphicsUtility.GetMeasureStringToDraw(transactionHistoryInformationObject.DateSent.ToString(CultureInfo.CurrentUICulture), positionBaseX, _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth, graphicsTarget, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out float widthText);

                    positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                    graphicsTarget.DrawString(dateToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, solidBrushTextSelected, positionBaseX, positionBaseY);

                    // Draw transaction type.
                    positionBaseX = _dictionaryTransactionHistory[walletFileOpened].ColumnDateMaxWidth;

                    string typeToShow = ClassGraphicsUtility.GetMeasureStringToDraw(transactionHistoryInformationObject.TransactionType, positionBaseX, _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth, graphicsTarget, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);

                    positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                    graphicsTarget.DrawString(typeToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, solidBrushTextSelected, positionBaseX, positionBaseY);

                    // Draw transaction wallet address.
                    positionBaseX = _dictionaryTransactionHistory[walletFileOpened].ColumnTypeMaxWidth;

                    string addressToShow = ClassGraphicsUtility.GetMeasureStringToDraw(transactionHistoryInformationObject.WalletAddress, positionBaseX, _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth, graphicsTarget, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);

                    positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                    graphicsTarget.DrawString(addressToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, solidBrushTextSelected, positionBaseX, positionBaseY);

                    // Draw transaction hash.
                    positionBaseX = _dictionaryTransactionHistory[walletFileOpened].ColumnWalletAddressMaxWidth;

                    string hashToShow = ClassGraphicsUtility.GetMeasureStringToDraw(transactionHistoryInformationObject.TransactionHash, positionBaseX, _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth, graphicsTarget, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);

                    positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                    graphicsTarget.DrawString(hashToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, solidBrushTextSelected, positionBaseX, positionBaseY);

                    // Draw transaction amount.
                    positionBaseX = _dictionaryTransactionHistory[walletFileOpened].ColumnHashMaxWidth;

                    string amountToShow = ClassGraphicsUtility.GetMeasureStringToDraw((ClassTransactionUtility.GetFormattedAmountFromBigInteger(transactionHistoryInformationObject.Amount) + @" " + BlockchainSetting.CoinMinName), positionBaseX, _dictionaryTransactionHistory[walletFileOpened].ColumnAmountMaxWidth, graphicsTarget, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, middlePositionX, out widthText);

                    positionBaseX = (positionBaseX + middlePositionX) - (widthText / 2);

                    graphicsTarget.DrawString(amountToShow, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryFont, solidBrushTextSelected, positionBaseX, positionBaseY);
                }
            }
            catch
            {
                // Ignored.
            }
        }

        /// <summary>
        /// Paint the loading animation to the transaction history panel.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="loadText"></param>
        /// <param name="percentProgress"></param>
        /// <param name="graphicsTarget"></param>
        /// <param name="panelTransactionHistory"></param>
        /// <param name="cancellation"></param>
        public void PaintTransactionLoadingAnimationToTransactionHistory(string walletFileOpened, string loadText, double percentProgress, Graphics graphicsTarget, ClassCustomPanel panelTransactionHistory, bool noTransaction)
        {
            graphicsTarget.SmoothingMode = SmoothingMode.HighQuality;

            string text = loadText + (!noTransaction ? percentProgress.ToString("N2") + @"%" : string.Empty);
            if (!noTransaction)
            {
                float positionY = (float)panelTransactionHistory.Height / 2;
                SizeF sizeText = graphicsTarget.MeasureString(text, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryOnLoadFont);
                float positionBaseY = (positionY - (sizeText.Height / 2));
                float middlePosition = (float)panelTransactionHistory.Width / 2;
                float positionX = 0;

                positionX = (positionX + middlePosition) - (sizeText.Width / 2);

                string textCountTx = _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Count + @"/" + _dictionaryTransactionHistory[walletFileOpened].LastTransactionCountOnRead;

                SizeF sizeTextCountTx = graphicsTarget.MeasureString(textCountTx, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryOnLoadFont);
                float positionCountTxY = positionBaseY + sizeTextCountTx.Height;
                float positionCountTxX = middlePosition - (sizeTextCountTx.Width / 2);

                graphicsTarget.DrawString(text, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryOnLoadFont, new SolidBrush(Color.Ivory), new RectangleF(positionX, positionBaseY, panelTransactionHistory.Width, panelTransactionHistory.Height));
                graphicsTarget.DrawString(textCountTx, ClassWalletDefaultSetting.DefaultPanelTransactionHistoryOnLoadFont, new SolidBrush(Color.Ivory), new RectangleF(positionCountTxX, positionCountTxY, panelTransactionHistory.Width, panelTransactionHistory.Height));
            }
        }

        #endregion

        #region Transaction history export functions.

        /// <summary>
        /// Try to export transaction history as CSV file.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="transactionExportFilePath"></param>
        /// <param name="allTransactions"></param>
        /// <param name="walletMainFormLanguage"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public bool TryExportTransactionHistory(string walletFileOpened, string transactionExportFilePath, bool allTransactions, ClassWalletMainFormLanguage walletMainFormLanguage, CancellationTokenSource cancellation)
        {
            if (_dictionaryTransactionHistory.ContainsKey(walletFileOpened))
            {
                using (StreamWriter writer = new StreamWriter(transactionExportFilePath))
                {
                    // Date, hash, type, address, amount, fee columns.
                    writer.WriteLine("\"{0}\", \"{1}\", \"{2}\", \"{3}\", \"{4}\", \"{5}\"",
                        walletMainFormLanguage.COLUMN_TRANSACTION_DATE,
                        walletMainFormLanguage.COLUMN_TRANSACTION_HASH,
                        walletMainFormLanguage.COLUMN_TRANSACTION_TYPE,
                        walletMainFormLanguage.COLUMN_TRANSACTION_WALLET_ADDRESS,
                        walletMainFormLanguage.COLUMN_TRANSACTION_AMOUNT,
                        walletMainFormLanguage.COLUMN_TRANSACTION_FEE);

                    lock (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed)
                    {
                        lock (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed)
                        {
                            // Write the current page.
                            if (!allTransactions)
                            {
                                if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.Count > 0)
                                {
                                    foreach (string transactionHash in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListedShowed.Keys.ToArray())
                                    {
                                        if (cancellation != null)
                                        {
                                            if (cancellation.IsCancellationRequested)
                                                break;
                                        }

                                        TransactionHistoryInformationObject transactionHistoryInformationObject = _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed[transactionHash];

                                        if (transactionHistoryInformationObject != null)
                                        {
                                            writer.WriteLine("\"{0}\", \"{1}\", \"{2}\", \"{3}\", \"{4}\", \"{5}\"",
                                                            transactionHistoryInformationObject.DateSent.ToString(CultureInfo.CurrentUICulture),
                                                            transactionHistoryInformationObject.TransactionHash,
                                                            transactionHistoryInformationObject.TransactionType,
                                                            transactionHistoryInformationObject.WalletAddress,
                                                            string.Format("N" + BlockchainSetting.CoinDecimalNumber, ((decimal)transactionHistoryInformationObject.Amount / BlockchainSetting.CoinDecimal)),
                                                            string.Format("N" + BlockchainSetting.CoinDecimalNumber, ((decimal)transactionHistoryInformationObject.Fee / BlockchainSetting.CoinDecimal)));
                                        }
                                        else
                                            return false;

                                        if (cancellation != null)
                                        {
                                            if (cancellation.IsCancellationRequested)
                                                return false;
                                        }
                                    }

                                    return true;
                                }

                            }
                            // Write all transactions.
                            else
                            {
                                if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Count > 0)
                                {
                                    foreach (var transactionHistoryInformationObject in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed)
                                    {
                                        if (cancellation != null)
                                        {
                                            if (cancellation.IsCancellationRequested)
                                                break;
                                        }

                                        if (transactionHistoryInformationObject.Value != null)
                                        {
                                            writer.WriteLine("\"{0}\", \"{1}\", \"{2}\", \"{3}\", \"{4}\", \"{5}\"",
                                                            transactionHistoryInformationObject.Value.DateSent.ToString(CultureInfo.CurrentUICulture),
                                                            transactionHistoryInformationObject.Value.TransactionHash,
                                                            transactionHistoryInformationObject.Value.TransactionType,
                                                            transactionHistoryInformationObject.Value.WalletAddress,
                                                            string.Format("N" + BlockchainSetting.CoinDecimalNumber, ((decimal)transactionHistoryInformationObject.Value.Amount / BlockchainSetting.CoinDecimal)),
                                                            string.Format("N" + BlockchainSetting.CoinDecimalNumber, ((decimal)transactionHistoryInformationObject.Value.Fee / BlockchainSetting.CoinDecimal)));
                                        }
                                        else
                                            return false;
                                    }

                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region Transaction history search functions.

        /// <summary>
        /// Try to research transactions from the argument selected.
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="cancellation"></param>
        /// <param name="foundElement"></param>
        /// <returns>Return a list of transaction hash found.</returns>
        public DisposableList<string> TrySearchTransactionHistory(string walletFileOpened, string argument, CancellationTokenSource cancellation, out bool foundElement)
        {
            DisposableList<string> listTransactionHash = new DisposableList<string>();

            bool isDate = DateTime.TryParse(argument, out DateTime dateTimeSearch);

            bool isWallet = ClassWalletUtility.CheckWalletAddress(argument);

            bool isTransactionHash = ClassUtility.CheckHexStringFormat(argument) && argument.Length == BlockchainSetting.TransactionHashSize;

            bool isAmount = decimal.TryParse(argument.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount);

            if (isDate || isWallet || isAmount || isTransactionHash)
            {
                // Search in content of the transaction history loaded.
                foreach (string transactionHash in _dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed.Keys.ToArray())
                {
                    if (cancellation.IsCancellationRequested)
                        break;

                    if (isDate)
                    {
                        if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed[transactionHash].DateSent >= dateTimeSearch)
                            listTransactionHash.Add(transactionHash);
                    }

                    if (isWallet)
                    {
                        if (_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed[transactionHash].WalletAddress == argument)
                            listTransactionHash.Add(transactionHash);
                    }

                    if (isTransactionHash && transactionHash == argument)
                    {
                        listTransactionHash.Add(transactionHash);
                        break;
                    }

                    if (isAmount)
                    {
                        decimal amountHistory = (decimal)_dictionaryTransactionHistory[walletFileOpened].DictionaryTransactionHistoryHashListed[transactionHash].Amount / BlockchainSetting.CoinDecimal;

                        if (amountHistory == amount)
                            listTransactionHash.Add(transactionHash);
                    }
                }
            }

#if DEBUG
            Debug.WriteLine("Found " + listTransactionHash.Count + " transaction(s).");
#endif
            foundElement = listTransactionHash.Count > 0;

            return listTransactionHash;
        }

        #endregion
    }
}
