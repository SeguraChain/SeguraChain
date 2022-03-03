using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.MainForm.Object;
using SeguraChain_Desktop_Wallet.Settings.Object;
using SeguraChain_Desktop_Wallet.Sync.Object;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Desktop_Wallet.Wallet.Object;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.MemPool.Database;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Blockchain.Stats.Object;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Instance.Node;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;
using SeguraChain_Lib.Other.Object.List;
using System.Net;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Utility;

namespace SeguraChain_Desktop_Wallet.Sync
{
    public class ClassWalletSyncSystem
    {
        /// <summary>
        /// Internal sync mode node instance.
        /// </summary>
        private ClassNodeInstance _nodeInstance;
        private CancellationTokenSource _cancellationSyncCache;


        /// <summary>
        /// For external sync mode update task.
        /// </summary>
        private CancellationTokenSource _cancellationExternalSyncModeUpdateTask;
        private bool _apiIsAlive;
        private long _lastBlockHeight;
        private long _lastBlockHeightUnlocked;
        private long _lastBlockHeightTransactionConfirmationDone;
        private long _lastTotalMemPoolTransaction;
        private long _lastBlockHeightTimestampCreate;
        private long _lastBlockHeightConfirmationTarget;
        private ClassBlockchainNetworkStatsObject _lastBlockchainNetworkStatsObject;

        #region Sync cache system.

        /// <summary>
        /// Store wallets sync caches.
        /// </summary>
        public ConcurrentDictionary<string, ClassSyncCacheObject> DatabaseSyncCache { get; private set; }

        /// <summary>
        /// Load the sync database cache.
        /// </summary>
        /// <param name="walletSettingObject"></param>
        /// <returns></returns>
        public async Task<bool> LoadSyncDatabaseCache(ClassWalletSettingObject walletSettingObject)
        {
            bool result = true;
            DatabaseSyncCache = new ConcurrentDictionary<string, ClassSyncCacheObject>();
            _cancellationSyncCache = new CancellationTokenSource();

            try
            {
                if (!Directory.Exists(walletSettingObject.WalletSyncCacheDirectoryPath))
                    Directory.CreateDirectory(walletSettingObject.WalletSyncCacheDirectoryPath);

                if (!File.Exists(walletSettingObject.WalletSyncCacheFilePath))
                    File.Create(walletSettingObject.WalletSyncCacheFilePath).Close();
                else
                {
                    using (StreamReader reader = new StreamReader(walletSettingObject.WalletSyncCacheFilePath))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (ClassUtility.TryDeserialize(line, out ClassSyncCacheBlockTransactionObject blockTransactionSyncCacheObject))
                            {
                                if (blockTransactionSyncCacheObject != null)
                                    await InsertOrUpdateBlockTransactionToSyncCache(blockTransactionSyncCacheObject.IsSender ? blockTransactionSyncCacheObject.BlockTransaction.TransactionObject.WalletAddressSender : blockTransactionSyncCacheObject.BlockTransaction.TransactionObject.WalletAddressReceiver, blockTransactionSyncCacheObject.BlockTransaction, blockTransactionSyncCacheObject.IsMemPool, true, _cancellationSyncCache);
                            }
                        }
                    }

                    if (DatabaseSyncCache.Count > 0)
                    {
                        foreach (string walletAddress in DatabaseSyncCache.Keys)
                            await DatabaseSyncCache[walletAddress].UpdateWalletBalance(_cancellationSyncCache);
                    }
                }
            }
            catch
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Save the sync database cache.
        /// </summary>
        /// <param name="walletSettingObject"></param>
        public async Task SaveSyncDatabaseCache(ClassWalletSettingObject walletSettingObject)
        {
            if (!Directory.Exists(walletSettingObject.WalletSyncCacheDirectoryPath))
                Directory.CreateDirectory(walletSettingObject.WalletSyncCacheDirectoryPath);

            if (!File.Exists(walletSettingObject.WalletSyncCacheFilePath))
                File.Create(walletSettingObject.WalletSyncCacheFilePath).Close();

            using (StreamWriter writer = new StreamWriter(walletSettingObject.WalletSyncCacheFilePath))
            {
                using (CancellationTokenSource cancellation = new CancellationTokenSource())
                {
                    foreach (string walletAddress in DatabaseSyncCache.Keys.ToArray())
                    {
                        if (DatabaseSyncCache[walletAddress].CountBlockHeight > 0)
                        {
                            using (var blockHeightList = DatabaseSyncCache[walletAddress].BlockHeightKeys)
                            {
                                foreach (long blockHeight in blockHeightList.GetList)
                                {
                                    using (DisposableList<string> listTransactionHash = await DatabaseSyncCache[walletAddress].GetListBlockTransactionHashFromBlockHeight(blockHeight, false, cancellation))
                                    {
                                        foreach (string transactionHash in listTransactionHash.GetList)
                                            writer.WriteLine(JsonConvert.SerializeObject(DatabaseSyncCache[walletAddress].GetSyncBlockTransactionCached(blockHeight, transactionHash)));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enable the task who update the sync cache.
        /// </summary>
        public void EnableTaskUpdateSyncCache()
        {

            StopTaskUpdateSyncCache(); // Ensure to stop the task.

            _cancellationSyncCache = new CancellationTokenSource();

            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (ClassDesktopWalletCommonData.DesktopWalletStarted)
                    {
                        long lastBlockHeightTransactionConfirmation = await GetLastBlockHeightSynced(_cancellationSyncCache, true);

                        if (DatabaseSyncCache.Count > 0)
                        {
                            string[] walletAddresses = DatabaseSyncCache.Keys.ToArray();
                            using (CancellationTokenSource cancellationUpdateWalletSync = CancellationTokenSource.CreateLinkedTokenSource(_cancellationSyncCache.Token))
                            {
                                int totalTask = walletAddresses.Length;

                                using (DisposableDictionary<string, bool> walletAddressUpdateSyncCacheState = new DisposableDictionary<string, bool>())
                                {

                                    foreach (string walletAddress in walletAddresses)
                                    {
                                        walletAddressUpdateSyncCacheState.Add(walletAddress, false);

                                        string walletFileName = ClassDesktopWalletCommonData.WalletDatabase.GetWalletFileNameFromWalletAddress(walletAddress);

                                        if (!ClassDesktopWalletCommonData.WalletDatabase.WalletOnResync(walletFileName))
                                        {
                                            try
                                            {
                                                await Task.Factory.StartNew(async () =>
                                                {
                                                    if (walletAddressUpdateSyncCacheState.ContainsKey(walletAddress))
                                                    {
                                                        try
                                                        {
                                                            await UpdateWalletSyncTransactionCache(walletAddress, walletFileName, lastBlockHeightTransactionConfirmation, cancellationUpdateWalletSync);

                                                            walletAddressUpdateSyncCacheState[walletAddress] = true;
                                                        }
#if DEBUG
                                                        catch (Exception error)
                                                        {
                                                            Debug.WriteLine("Error update sync transaction cache: "+error.Message);
#else
                                                        {
#endif

                                                        }
                                                    }

                                                }, cancellationUpdateWalletSync.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                                            }
                                            catch
                                            {
                                                // Ignored, catch the exception once canceled.
                                                walletAddressUpdateSyncCacheState[walletAddress] = true;
                                            }
                                        }
                                        else walletAddressUpdateSyncCacheState[walletAddress] = true;

                                    }

                                    while (walletAddressUpdateSyncCacheState.GetList.Count(x => x.Value == true) < totalTask)
                                    {
                                        if (_cancellationSyncCache.IsCancellationRequested)
                                            break;

                                        try
                                        {
                                            await Task.Delay(100, _cancellationSyncCache.Token);
                                        }
                                        catch
                                        {
                                            break;
                                        }
                                    }

                                    cancellationUpdateWalletSync.Cancel();
                                }

                            }
                        }



                        try
                        {
                            await Task.Delay(ClassWalletDefaultSetting.DefaultWalletUpdateSyncCacheInterval, _cancellationSyncCache.Token);
                        }
                        catch
                        {
                            break;
                        }

                    }
                }, _cancellationSyncCache.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Update the wallet sync transaction cache.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="cancellationUpdateWalletSync"></param>
        /// <returns></returns>
        private async Task UpdateWalletSyncTransactionCache(string walletAddress, string walletFileName, long lastBlockHeightTransactionConfirmation, CancellationTokenSource cancellationUpdateWalletSync)
        {

            try
            {
                bool cancelled = false;

                long totalTxUpdated = 0;

                if (DatabaseSyncCache[walletAddress].CountBlockHeight > 0)
                {
#region Update every cached transactions.

                    using (var blockHeightList = DatabaseSyncCache[walletAddress].BlockHeightKeys)
                    {
                        foreach (long blockHeight in blockHeightList.GetList)
                        {
                            cancellationUpdateWalletSync.Token.ThrowIfCancellationRequested();

                            if (!walletFileName.IsNullOrEmpty(false, out _))
                            {
                                if (ClassDesktopWalletCommonData.WalletDatabase.WalletOnResync(walletFileName))
                                {
                                    cancelled = true;
                                    break;
                                }
                            }

                            if (blockHeight < await GetLastBlockHeightSynced(cancellationUpdateWalletSync, true))
                            {
                                if (!await DatabaseSyncCache[walletAddress].CheckIfBlockTransactionFromHeightAreFullyConfirmed(blockHeight, cancellationUpdateWalletSync))
                                {
                                    if (!await DatabaseSyncCache[walletAddress].CheckIfBlockTransactionFromHeightAreConfirmed(blockHeight, lastBlockHeightTransactionConfirmation, cancellationUpdateWalletSync))
                                    {
                                        using (DisposableList<string> listTransactionHash = await DatabaseSyncCache[walletAddress].GetListBlockTransactionHashFromBlockHeight(blockHeight, false, cancellationUpdateWalletSync))
                                        {
                                            using (DisposableList<ClassBlockTransaction> listBlockTransaction = await GetWalletListBlockTransactionFromListTransactionHashAndBlockHeightFromSync(listTransactionHash.GetList, blockHeight, cancellationUpdateWalletSync))
                                            {
                                                foreach (ClassBlockTransaction blockTransaction in listBlockTransaction.GetList)
                                                {
                                                    cancellationUpdateWalletSync.Token.ThrowIfCancellationRequested();

                                                    if (ClassDesktopWalletCommonData.WalletDatabase.WalletOnResync(walletFileName))
                                                    {
                                                        cancelled = true;
                                                        break;
                                                    }

                                                    if (blockTransaction != null)
                                                    {
                                                        DatabaseSyncCache[walletAddress].UpdateBlockTransaction(blockTransaction, false);
                                                        totalTxUpdated++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
#region Update the amount of confirmations on block transactions on the block height if they are fully confirmed without to call the blockchain database or the API.

                                    using (DisposableList<string> listTransactionHash = await DatabaseSyncCache[walletAddress].GetListBlockTransactionHashFromBlockHeight(blockHeight, false, cancellationUpdateWalletSync))
                                    {
                                        foreach (string transactionHash in listTransactionHash.GetList)
                                        {
                                            cancellationUpdateWalletSync.Token.ThrowIfCancellationRequested();


                                            if (ClassDesktopWalletCommonData.WalletDatabase.WalletOnResync(walletFileName))
                                            {
                                                cancelled = true;
                                                break;
                                            }

                                            ClassSyncCacheBlockTransactionObject currentBlockTransaction = DatabaseSyncCache[walletAddress].GetSyncBlockTransactionCached(blockHeight, transactionHash);

                                            if (currentBlockTransaction != null)
                                            {
                                                if (currentBlockTransaction.BlockTransaction.TransactionStatus && !currentBlockTransaction.IsMemPool)
                                                {
                                                    if (blockHeight + currentBlockTransaction.BlockTransaction.TransactionTotalConfirmation >= lastBlockHeightTransactionConfirmation)
                                                        break;
                                                    else
                                                    {
                                                        currentBlockTransaction.BlockTransaction.TransactionTotalConfirmation = (lastBlockHeightTransactionConfirmation - blockHeight) + 1;
                                                        DatabaseSyncCache[walletAddress].UpdateBlockTransaction(currentBlockTransaction.BlockTransaction, false);
                                                        totalTxUpdated++;
                                                    }
                                                }
                                            }
                                        }
                                    }

#endregion
                                }
                            }

                            if (cancelled)
                                break;
                        }
                    }

#endregion


#region Calculate balances.

                    if (!cancelled)
                        await DatabaseSyncCache[walletAddress].UpdateWalletBalance(cancellationUpdateWalletSync);

#endregion
                }
#if DEBUG
                Debug.WriteLine("Wallet Address " + walletAddress + " sync cache updated. Total transaction updated: " + totalTxUpdated);
#endif
            }
            catch
            {
                // Ignored, catch the exception if the wallet file has been closed pending the scan.
            }
        }

        /// <summary>
        /// Stop the task who update the sync cache.
        /// </summary>
        public void StopTaskUpdateSyncCache()
        {
            if (_cancellationSyncCache != null)
            {
                if (!_cancellationSyncCache.IsCancellationRequested)
                    _cancellationSyncCache.Cancel();
            }
        }

        /// <summary>
        /// Clean sync cache of a wallet address target.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public void CleanSyncCacheOfWalletAddressTarget(string walletAddress, CancellationTokenSource cancellation)
        {
            StopTaskUpdateSyncCache();

            if (DatabaseSyncCache.ContainsKey(walletAddress))
                DatabaseSyncCache[walletAddress].Clear(cancellation).Wait();

            EnableTaskUpdateSyncCache();
        }

        /// <summary>
        /// Insert or a update block transaction to the sync cache.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <param name="isMemPool"></param>
        /// <param name="cancellation"></param>
        public async Task InsertOrUpdateBlockTransactionToSyncCache(string walletAddress, ClassBlockTransaction blockTransaction, bool isMemPool, bool updateWalletBalance, CancellationTokenSource cancellation)
        {

            bool init = false;

            bool isSender = blockTransaction.TransactionObject.WalletAddressSender == walletAddress;

#region Update sender cache.

            if (!DatabaseSyncCache.ContainsKey(walletAddress))
            {
                if (ClassWalletUtility.CheckWalletAddress(walletAddress))
                    init = DatabaseSyncCache.TryAdd(walletAddress, new ClassSyncCacheObject());
            }
            else
                init = true;

            if (init)
            {
                bool insertBlockHeight = true;

                if (!DatabaseSyncCache[walletAddress].ContainsBlockHeight(blockTransaction.TransactionObject.BlockHeightTransaction))
                    insertBlockHeight = await DatabaseSyncCache[walletAddress].InsertBlockHeight(blockTransaction.TransactionObject.BlockHeightTransaction, cancellation);

                while (!insertBlockHeight)
                {
                    cancellation?.Token.ThrowIfCancellationRequested();

                    if (!DatabaseSyncCache[walletAddress].ContainsBlockHeight(blockTransaction.TransactionObject.BlockHeightTransaction))
                        insertBlockHeight = await DatabaseSyncCache[walletAddress].InsertBlockHeight(blockTransaction.TransactionObject.BlockHeightTransaction, cancellation);
                    else
                        insertBlockHeight = true;
                }

                if (insertBlockHeight)
                {
                    if (!await DatabaseSyncCache[walletAddress].ContainsBlockTransactionFromTransactionHashAndBlockHeight(blockTransaction.TransactionObject.BlockHeightTransaction, blockTransaction.TransactionObject.TransactionHash, cancellation))
                    {
                        if (updateWalletBalance)
                            UpdateWalletSyncCacheBalances(walletAddress, isMemPool, blockTransaction);

                        await DatabaseSyncCache[walletAddress].InsertBlockTransaction(new ClassSyncCacheBlockTransactionObject()
                        {
                            BlockTransaction = blockTransaction,
                            IsMemPool = isMemPool,
                            IsSender = isSender
                        }, cancellation);
                    }
                    else
                        DatabaseSyncCache[walletAddress].UpdateBlockTransaction(blockTransaction, isMemPool);
                }
            }

#endregion

        }


        /// <summary>
        /// Update balances wallet inside of the sync cache.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="isMemPool"></param>
        /// <param name="blockTransaction"></param>
        private void UpdateWalletSyncCacheBalances(string walletAddress, bool isMemPool, ClassBlockTransaction blockTransaction)
        {

            if (blockTransaction.TransactionStatus)
            {
                if (isMemPool)
                {
                    if (blockTransaction.TransactionObject.WalletAddressSender == walletAddress)
                    {
                        if (DatabaseSyncCache[walletAddress].AvailableBalance - (blockTransaction.TransactionObject.Amount + blockTransaction.TransactionObject.Fee) >= 0)
                            DatabaseSyncCache[walletAddress].AvailableBalance -= (blockTransaction.TransactionObject.Amount + blockTransaction.TransactionObject.Fee);

                        DatabaseSyncCache[walletAddress].PendingBalance -= (blockTransaction.TransactionObject.Amount + blockTransaction.TransactionObject.Fee);
                    }
                    else
                        DatabaseSyncCache[walletAddress].PendingBalance += blockTransaction.TransactionObject.Amount;
                }
                else
                {
                    if (blockTransaction.TransactionObject.WalletAddressReceiver == walletAddress)
                    {
                        if (blockTransaction.IsConfirmed)
                            DatabaseSyncCache[walletAddress].AvailableBalance += blockTransaction.TransactionObject.Amount;
                        else
                            DatabaseSyncCache[walletAddress].PendingBalance += blockTransaction.TransactionObject.Amount;
                    }
                    else
                    {
                        if (blockTransaction.IsConfirmed)
                            DatabaseSyncCache[walletAddress].AvailableBalance -= (blockTransaction.TransactionObject.Amount + blockTransaction.TransactionObject.Fee);
                        else
                            DatabaseSyncCache[walletAddress].PendingBalance -= (blockTransaction.TransactionObject.Amount + blockTransaction.TransactionObject.Fee);
                    }
                }
            }
        }


        /// <summary>
        /// Get a block transaction from the sync cache.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="isMemPool"></param>
        /// <returns></returns>
        public ClassBlockTransaction GetBlockTransactionFromSyncCache(string walletAddress, string transactionHash, long blockHeight, out bool isMemPool)
        {
            ClassBlockTransaction blockTransaction = null;
            isMemPool = false;

            if (DatabaseSyncCache.Count > 0)
            {
                if (DatabaseSyncCache.ContainsKey(walletAddress))
                {
                    var syncBlockTransactionCached = DatabaseSyncCache[walletAddress].GetSyncBlockTransactionCached(blockHeight, transactionHash);
                    if (syncBlockTransactionCached != null)
                    {
                        isMemPool = syncBlockTransactionCached.IsMemPool;
                        blockTransaction = syncBlockTransactionCached.BlockTransaction;
                    }
                }
            }

            return blockTransaction;
        }

#endregion

#region Main sync functions.

        /// <summary>
        /// Initialize and start the sync system.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StartSync()
        {

#region Cancel external sync mode update task if necessary.

            if (_cancellationExternalSyncModeUpdateTask != null)
            {
                if (!_cancellationExternalSyncModeUpdateTask.IsCancellationRequested)
                    _cancellationExternalSyncModeUpdateTask.Cancel();
            }

#endregion

            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {

                        _nodeInstance = new ClassNodeInstance
                        {
                            PeerSettingObject = ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting
                        };

                        return _nodeInstance.NodeStart(true);
                    }
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_nodeInstance != null)
                            await _nodeInstance.NodeStop(false, true);

                        ServicePointManager.DefaultConnectionLimit = 65535;
                        ServicePointManager.Expect100Continue = false;

                        StartTaskUpdateExternalSyncMode();

                        return true;
                    }

            }
            return false;
        }

        /// <summary>
        /// Close the sync system.
        /// </summary>
        public async Task CloseSync()
        {
            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        if (_nodeInstance != null)
                            await _nodeInstance.NodeStop(false, true);
                    }
                    break;
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_cancellationExternalSyncModeUpdateTask != null)
                        {
                            if (!_cancellationExternalSyncModeUpdateTask.IsCancellationRequested)
                                _cancellationExternalSyncModeUpdateTask.Cancel();
                        }

                    }
                    break;
            }
        }

        /// <summary>
        /// Update the wallet sync of a wallet file target.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> UpdateWalletSync(string walletFileName, CancellationTokenSource cancellation)
        {
            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        return await UpdateWalletSyncFromInternalSyncMode(walletFileName, cancellation);
                    }
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive)
                            return await UpdateWalletSyncFromExternalSyncMode(walletFileName, cancellation);
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// Get the wallet balance from data synced.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// 
        /// <returns></returns>
        public ClassWalletBalanceObject GetWalletBalanceFromSyncedData(string walletFileName)
        {
            BigInteger availableBalance = 0;
            BigInteger pendingBalance = 0;
            BigInteger totalBalance = 0;

            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.ContainsKey(walletFileName))
            {
                string walletAddress = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletAddress;

                bool containAddress;
                if (!DatabaseSyncCache.ContainsKey(walletAddress))
                    containAddress = DatabaseSyncCache.TryAdd(walletAddress, new ClassSyncCacheObject());
                else
                    containAddress = true;

                if (containAddress)
                {
                    availableBalance = DatabaseSyncCache[walletAddress].AvailableBalance;
                    totalBalance = DatabaseSyncCache[walletAddress].AvailableBalance + DatabaseSyncCache[walletAddress].PendingBalance;
                    pendingBalance = DatabaseSyncCache[walletAddress].PendingBalance;
                }
            }

            return new ClassWalletBalanceObject()
            {
                WalletAvailableBalance = ClassTransactionUtility.GetFormattedAmountFromBigInteger(availableBalance),
                WalletPendingBalance = ClassTransactionUtility.GetFormattedAmountFromBigInteger(pendingBalance),
                WalletTotalBalance = ClassTransactionUtility.GetFormattedAmountFromBigInteger(totalBalance)
            };
        }

        /// <summary>
        /// Get the wallet balance from data synced.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// <returns></returns>
        public BigInteger GetWalletAvailableBalanceFromSyncedData(string walletFileName)
        {
            BigInteger availableBalance = 0;

            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.ContainsKey(walletFileName))
            {
                string walletAddress = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletAddress;


                bool containAddress;
                if (!DatabaseSyncCache.ContainsKey(walletAddress))
                    containAddress = DatabaseSyncCache.TryAdd(walletAddress, new ClassSyncCacheObject());
                else
                    containAddress = true;

                if (containAddress)
                    availableBalance = DatabaseSyncCache[walletAddress].AvailableBalance;
            }

            return availableBalance;
        }

        /// <summary>
        /// Retrieve back a transaction object from data synced by his hash and his block height if possible.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="fromSyncCacheUpdate"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<Tuple<bool, ClassBlockTransaction>> GetTransactionObjectFromSync(string walletAddress, string transactionHash, long blockHeight, bool fromSyncCacheUpdate, CancellationTokenSource cancellation)
        {
            ClassBlockTransaction blockTransaction = null;
            bool isMemPool = false;
            bool wasEmpty = false;

            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                blockHeight = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

            // Retrieve from the sync cache.
            if (!fromSyncCacheUpdate)
            {
                blockTransaction = GetBlockTransactionFromSyncCache(walletAddress, transactionHash, blockHeight, out isMemPool);
                wasEmpty = blockTransaction == null;
            }

            if (blockTransaction == null)
            {
                switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
                {
                    case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                        {
                            blockTransaction = await GetWalletBlockTransactionFromTransactionHashFromInternalSyncMode(transactionHash, blockHeight, cancellation);
                        }
                        break;
                    case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                        {
                            if (_apiIsAlive)
                                blockTransaction = await ClassApiClientUtility.GetBlockTransactionByTransactionHashAndBlockHeightFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, transactionHash, blockHeight, cancellation);
                        }
                        break;
                }

                if (blockTransaction != null)
                    isMemPool = false;
            }

            // Update sync cache.
            if (!fromSyncCacheUpdate || wasEmpty)
            {
                if (blockTransaction != null)
                    await InsertOrUpdateBlockTransactionToSyncCache(walletAddress, blockTransaction, isMemPool, !fromSyncCacheUpdate, cancellation);
            }


            return new Tuple<bool, ClassBlockTransaction>(isMemPool, blockTransaction);
        }

        /// <summary>
        /// Retrieve back a mem pool transaction object from the synced data by his hash.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<ClassTransactionObject> GetMemPoolTransactionObjectFromSync(string walletAddress, string transactionHash, bool fromSyncCache, CancellationTokenSource cancellation)
        {
            if (!fromSyncCache)
            {
                if (DatabaseSyncCache.ContainsKey(walletAddress))
                {
                    long blockHeightTransaction = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

                    var syncedBlockTransactionCache = DatabaseSyncCache[walletAddress].GetSyncBlockTransactionCached(blockHeightTransaction, transactionHash);

                    if (syncedBlockTransactionCache != null)
                        return syncedBlockTransactionCache.BlockTransaction.TransactionObject;
                }
            }

            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        return await GetWalletMemPoolTransactionFromTransactionHashFromInternalSyncMode(transactionHash, cancellation);
                    }
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive)
                            return await ClassApiClientUtility.GetWalletMemPoolTransactionFromTransactionHashFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, transactionHash, cancellation);
                    }
                    break;
            }
            return null;
        }

        /// <summary>
        /// Get the last block height from data synced.
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetLastBlockHeightUnlockedSynced(CancellationTokenSource cancellation, bool useInternalUpdate)
        {
            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        return await ClassBlockchainStats.GetLastBlockHeightUnlocked(cancellation);
                    }
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive || useInternalUpdate)
                            return useInternalUpdate && _lastBlockHeightUnlocked > 0 ? _lastBlockHeightUnlocked : await ClassApiClientUtility.GetLastBlockHeightUnlockedFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, cancellation);
                    }
                    break;
            }
            return 0;
        }

        /// <summary>
        /// Get the last block height from data synced.
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetLastBlockHeightSynced(CancellationTokenSource cancellation, bool useInternalUpdate)
        {
            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        return ClassBlockchainStats.GetLastBlockHeight();
                    }
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive || useInternalUpdate)
                            return useInternalUpdate && _lastBlockHeight > 0 ? _lastBlockHeight : await ClassApiClientUtility.GetLastBlockHeightFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, cancellation);
                    }
                    break;
            }
            return 0;
        }

        /// <summary>
        /// Get the last block height transaction confirmation done.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<long> GetLastBlockHeightTransactionConfirmation(CancellationTokenSource cancellation, bool useInternalUpdate)
        {
            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        return await ClassBlockchainStats.GetLastBlockHeightTransactionConfirmationDone(cancellation);
                    }
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive || useInternalUpdate)
                            return useInternalUpdate && _lastBlockHeightTransactionConfirmationDone > 0 ? _lastBlockHeightTransactionConfirmationDone : await ClassApiClientUtility.GetLastBlockHeightTransactionConfirmationDoneFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, cancellation);
                    }
                    break;
            }
            return 0;
        }

        /// <summary>
        /// Get the last blockchain network stats object.
        /// </summary>
        /// <returns></returns>
        public async Task<ClassBlockchainNetworkStatsObject> GetBlockchainNetworkStatsObject(CancellationTokenSource cancellation, bool useInternalUpdate)
        {
            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        return ClassBlockchainStats.BlockchainNetworkStatsObject;
                    }
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive)
                        {
                            if (useInternalUpdate && _lastBlockchainNetworkStatsObject != null)
                                return _lastBlockchainNetworkStatsObject;

                            return await ClassApiClientUtility.GetBlockchainNetworkStatsFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, cancellation);
                        }
                    }
                    break;
            }

            return null;
        }

        /// <summary>
        /// Get the total amount of mempool transaction(s)
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetTotalMemPoolTransactionFromSyncAsync(CancellationTokenSource cancellation, bool useInternalUpdate)
        {
            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        return ClassMemPoolDatabase.GetCountMemPoolTx;
                    }
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive || useInternalUpdate)
                            return useInternalUpdate && _lastTotalMemPoolTransaction > 0 ? _lastTotalMemPoolTransaction : await ClassApiClientUtility.GetMemPoolTotalTransactionCountFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, cancellation);
                    }
                    break;
            }
            return 0;
        }

        /// <summary>
        /// Get the timestamp create of a block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<long> GetBlockTimestampCreateFromBlockHeight(long blockHeight, CancellationTokenSource cancellation, bool useInternalUpdate)
        {
            long timestamp = 0;
            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {

                        var blockInformationsData = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockInformationDataStrategy(blockHeight, cancellation);
                        if (blockInformationsData != null)
                            timestamp = blockInformationsData.TimestampCreate;

                    }
                    break;
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive || useInternalUpdate)
                            return useInternalUpdate && _lastBlockHeightTimestampCreate > 0 ? _lastBlockHeightTimestampCreate : await ClassApiClientUtility.GetBlockTimestampCreateFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, blockHeight, cancellation);
                    }
                    break;
            }

            return timestamp;
        }

        /// <summary>
        /// Generate the block height start transaction confirmation from the sync.
        /// </summary>
        /// <param name="lastBlockHeightUnlocked"></param>
        /// <param name="lastBlockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<long> GenerateBlockHeightStartTransactionConfirmationFromSync(long lastBlockHeightUnlocked, long lastBlockHeight, CancellationTokenSource cancellation, bool useInternalUpdate)
        {
            long blockHeightStart = 0;

            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        blockHeightStart = await ClassTransactionUtility.GenerateBlockHeightStartTransactionConfirmation(lastBlockHeightUnlocked, lastBlockHeight, cancellation);
                    }
                    break;
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive || useInternalUpdate)
                            blockHeightStart = useInternalUpdate && _lastBlockHeightConfirmationTarget > 0 ? _lastBlockHeightConfirmationTarget : await ClassApiClientUtility.GetGenerateBlockHeightStartTransactionConfirmationFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, lastBlockHeightUnlocked, lastBlockHeight, cancellation);
                    }
                    break;
            }

            return blockHeightStart;
        }

        /// <summary>
        /// Get fee cost confirmation from the whole activity of the blockchain of the synced data.
        /// </summary>
        /// <param name="lastBlockHeightUnlocked"></param>
        /// <param name="blockHeightConfirmationStart"></param>
        /// <param name="blockHeightConfirmationTarget"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<Tuple<BigInteger, bool>> GetFeeCostConfirmationFromWholeActivityBlockchainFromSync(long lastBlockHeightUnlocked, long blockHeightConfirmationStart, long blockHeightConfirmationTarget, CancellationTokenSource cancellation)
        {
            Tuple<BigInteger, bool> calculationFeeCostConfirmation = new Tuple<BigInteger, bool>(0, false);

            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        calculationFeeCostConfirmation = await ClassTransactionUtility.GetFeeCostFromWholeBlockchainTransactionActivity(lastBlockHeightUnlocked, blockHeightConfirmationStart, blockHeightConfirmationTarget, cancellation);
                    }
                    break;
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive)
                            calculationFeeCostConfirmation = await ClassApiClientUtility.GetFeeCostTransactionFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, lastBlockHeightUnlocked, blockHeightConfirmationStart, blockHeightConfirmationTarget, cancellation);
                    }
                    break;
            }

            return calculationFeeCostConfirmation;
        }

        /// <summary>
        /// Get a list of block transaction synced by a list of transaction hash and a specific block height target.
        /// </summary>
        /// <param name="listTransactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<DisposableList<ClassBlockTransaction>> GetWalletListBlockTransactionFromListTransactionHashAndBlockHeightFromSync(List<string> listTransactionHash, long blockHeight, CancellationTokenSource cancellation)
        {

            switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
            {
                case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                    {
                        return await GetWalletListBlockTransactionFromListTransactionHashAndBlockHeightFromInternalSyncMode(listTransactionHash, blockHeight, cancellation);
                    }
                case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                    {
                        if (_apiIsAlive)
                            return await ClassApiClientUtility.GetBlockTransactionByHashFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, listTransactionHash, blockHeight, cancellation);
                    }
                    break;
            }

            return new DisposableList<ClassBlockTransaction>();
        }

#endregion

#region Sync wallet functions in internal mode.

        /// <summary>
        /// Update the wallet sync data in internal sync mode.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// <param name="cancellation"></param>
        private async Task<bool> UpdateWalletSyncFromInternalSyncMode(string walletFileName, CancellationTokenSource cancellation)
        {
            bool changeDone = false;

            if (ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
            {
                string walletAddress = ClassDesktopWalletCommonData.WalletDatabase.GetWalletAddressFromWalletFileName(walletFileName);


                long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();


                long blockHeightStart = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletLastBlockHeightSynced;

                bool cancelled = false;

                using (var memPoolList = await ClassMemPoolDatabase.GetMemPoolAllTxFromWalletAddressTargetAsync(walletAddress, cancellation))
                {

                    foreach (ClassTransactionObject memPoolTransactionObject in memPoolList.GetList)
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            cancelled = true;
                            break;
                        }

                        if (memPoolTransactionObject != null)
                        {
                            if (memPoolTransactionObject.WalletAddressReceiver == walletAddress || memPoolTransactionObject.WalletAddressSender == walletAddress)
                            {
                                bool alreadyConfirmed = false;
                                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList.ContainsKey(memPoolTransactionObject.BlockHeightTransaction))
                                {
                                    if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList[memPoolTransactionObject.BlockHeightTransaction].Contains(memPoolTransactionObject.TransactionHash))
                                        alreadyConfirmed = true;
                                }
                                if (!alreadyConfirmed)
                                {
                                    if (!ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Contains(memPoolTransactionObject.TransactionHash))
                                    {
                                        if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Add(memPoolTransactionObject.TransactionHash))
                                        {
                                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTotalMemPoolTransaction++;
                                            changeDone = true;

                                            await InsertOrUpdateBlockTransactionToSyncCache(walletAddress, new ClassBlockTransaction(0, memPoolTransactionObject)
                                            {
                                                TransactionObject = memPoolTransactionObject,
                                                TransactionStatus = true,
                                                TransactionBlockHeightInsert = memPoolTransactionObject.BlockHeightTransaction,
                                                TransactionBlockHeightTarget = memPoolTransactionObject.BlockHeightTransactionConfirmationTarget
                                            }, true, false, cancellation);
                                            ClassLog.WriteLine(memPoolTransactionObject.TransactionHash + " tx hash of wallet address: " + walletAddress + " from mempool has been synced successfully.", ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                        }
                                    }
                                }
                                else
                                {
                                    if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Contains(memPoolTransactionObject.TransactionHash))
                                    {
                                        ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Remove(memPoolTransactionObject.TransactionHash);
                                        changeDone = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (!cancelled)
                {
                    if (blockHeightStart <= lastBlockHeight)
                    {
                        ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletOnSync = true;

                        for (long blockHeight = blockHeightStart; blockHeight <= lastBlockHeight; blockHeight++)
                        {
                            cancellation.Token.ThrowIfCancellationRequested();

                            if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= lastBlockHeight)
                            {
                                ClassBlockObject blockObjectInformation = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockInformationDataStrategy(blockHeight, cancellation);

                                if (blockObjectInformation.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                {
                                    using (ClassBlockObject blockObject = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockDataStrategy(blockHeight, false, true, cancellation))
                                    {
                                        if (blockObject == null)
                                            cancelled = true;
                                        else
                                        {
                                            if (blockObject.BlockTransactions?.Count > 0)
                                            {
                                                foreach (var transactionPair in blockObject.BlockTransactions)
                                                {
                                                    cancellation.Token.ThrowIfCancellationRequested();

                                                    if (transactionPair.Value.TransactionObject.WalletAddressReceiver == walletAddress || transactionPair.Value.TransactionObject.WalletAddressSender == walletAddress)
                                                    {
                                                        if (!ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList.ContainsKey(blockHeight))
                                                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList.Add(blockHeight, new HashSet<string>());

                                                        if (!ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList[blockHeight].Contains(transactionPair.Key))
                                                        {
                                                            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList[blockHeight].Add(transactionPair.Key))
                                                            {
                                                                ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTotalTransaction++;
                                                                await InsertOrUpdateBlockTransactionToSyncCache(walletAddress, transactionPair.Value, false, false, cancellation);
                                                                changeDone = true;
                                                            }
                                                            else
                                                            {
                                                                cancelled = true;
#if DEBUG
                                                                Debug.WriteLine("Transaction hash: " + transactionPair.Key + " from the block height: " + blockHeight + "can't be inserted into the wallet file data: " + walletFileName);
#endif
                                                                ClassLog.WriteLine("Transaction hash: " + transactionPair.Key + " from the block height: " + blockHeight + "can't be inserted into the wallet file data: " + walletFileName, ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                                                break;
                                                            }
                                                        }

                                                        if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Contains(transactionPair.Key))
                                                        {
                                                            ClassLog.WriteLine(transactionPair.Key + " tx hash of block height: " + blockHeight + " seems to has been accepted by the network to be proceed, remove it from the mempool.", ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);

                                                            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Remove(transactionPair.Key))
                                                            {
                                                                ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTotalMemPoolTransaction--;
                                                                changeDone = true;
                                                            }
                                                            else
                                                            {
                                                                cancelled = true;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                cancelled = true;
#if DEBUG
                                                Debug.WriteLine("The block height " + blockHeight + " is empty or don't have any transactions for the wallet file data: " + walletFileName);
#endif
                                                ClassLog.WriteLine("The block height " + blockHeight + " is empty or don't have any transactions for the wallet file data: " + walletFileName, ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                            }
                                        }

                                        if (!cancelled)
                                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletLastBlockHeightSynced = blockObject.BlockHeight;
                                        else
                                            break;
                                    }
                                }
                                else
                                    break;
                            }
                        }
                        if (cancelled)
                        {
#if DEBUG
                            Debug.WriteLine("Sync cancelled for wallet address: " + walletAddress);
#endif
                            changeDone = false;
                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletEnableRescan = true;
                        }
                        else
                        {
                            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletLastBlockHeightSynced != blockHeightStart)
                            {
#if DEBUG
                                Debug.WriteLine("Change done on the sync of the wallet filename: " + walletFileName + " - Previous Height: " + blockHeightStart + " | New Height: " + ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletLastBlockHeightSynced);
#endif
                                changeDone = true;
                            }
                        }

                        ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletOnSync = false;

                    }
                    else if (blockHeightStart > lastBlockHeight)
                    {

                        ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletEnableRescan = true;
#if DEBUG
                        Debug.WriteLine("Warning the wallet file data: " + walletFileName + " last block height sync progress is above the current one synced: " + blockHeightStart + "/" + lastBlockHeight);
#endif
                        ClassLog.WriteLine("Warning the wallet file data: " + walletFileName + " last block height sync progress is above the current one synced: " + blockHeightStart + "/" + lastBlockHeight, ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);

                    }
                }
            }
            else
                ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletEnableRescan = true;


            return changeDone;
        }

        /// <summary>
        /// Retrieve back a block transaction from his hash and his block height in internal sync mode.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<ClassBlockTransaction> GetWalletBlockTransactionFromTransactionHashFromInternalSyncMode(string transactionHash, long blockHeight, CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockTransactionFromSpecificTransactionHashAndHeight(transactionHash, blockHeight, false, true, cancellation);
        }

        /// <summary>
        /// Retrieve back a list of block transaction by a list of transaction hash and a block height in internal sync mode.
        /// </summary>
        /// <param name="listTransactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<DisposableList<ClassBlockTransaction>> GetWalletListBlockTransactionFromListTransactionHashAndBlockHeightFromInternalSyncMode(List<string> listTransactionHash, long blockHeight, CancellationTokenSource cancellation)
        {
            return await ClassBlockchainDatabase.BlockchainMemoryManagement.GetListBlockTransactionFromListTransactionHashAndHeight(listTransactionHash, blockHeight, false, false, cancellation);
        }

        /// <summary>
        /// Retrieve back a transaction by his hash from the mem pool.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<ClassTransactionObject> GetWalletMemPoolTransactionFromTransactionHashFromInternalSyncMode(string transactionHash, CancellationTokenSource cancellation)
        {
            return await ClassMemPoolDatabase.GetMemPoolTxFromTransactionHash(transactionHash, 0, cancellation);
        }

#endregion

#region Sync wallet functions in external mode.

        /// <summary>
        /// Start the external sync mode update task.
        /// </summary>
        private void StartTaskUpdateExternalSyncMode()
        {
            _cancellationExternalSyncModeUpdateTask = new CancellationTokenSource();

            try
            {
                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        while (ClassDesktopWalletCommonData.DesktopWalletStarted)
                        {
                            _apiIsAlive = await ClassApiClientUtility.GetAliveFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, _cancellationExternalSyncModeUpdateTask);

                            if (_apiIsAlive)
                            {

                                var lastBlockchainNetworkStats = await ClassApiClientUtility.GetBlockchainNetworkStatsFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, _cancellationExternalSyncModeUpdateTask);

                                _lastBlockchainNetworkStatsObject = lastBlockchainNetworkStats != null ? lastBlockchainNetworkStats : _lastBlockchainNetworkStatsObject;

                                if (_lastBlockchainNetworkStatsObject != null)
                                {
                                    _lastBlockHeight = _lastBlockchainNetworkStatsObject.LastBlockHeight;
                                    _lastBlockHeightUnlocked = _lastBlockchainNetworkStatsObject.LastBlockHeightUnlocked;
                                }
                                else
                                {
                                    var lastBlockHeight = await GetLastBlockHeightSynced(_cancellationExternalSyncModeUpdateTask, false);
                                    _lastBlockHeight = lastBlockHeight > 0 ? lastBlockHeight : _lastBlockHeight;

                                    var lastBlockHeightUnlocked = await GetLastBlockHeightUnlockedSynced(_cancellationExternalSyncModeUpdateTask, false);
                                    _lastBlockHeightUnlocked = lastBlockHeightUnlocked > 0 ? lastBlockHeightUnlocked : _lastBlockHeightUnlocked;
                                }

                                var lastBlockHeightTransactionConfirmationDone = await GetLastBlockHeightTransactionConfirmation(_cancellationExternalSyncModeUpdateTask, false);
                                _lastBlockHeightTransactionConfirmationDone = lastBlockHeightTransactionConfirmationDone > 0 ? lastBlockHeightTransactionConfirmationDone : _lastBlockHeightTransactionConfirmationDone;

                                _lastTotalMemPoolTransaction = await ClassApiClientUtility.GetMemPoolTotalTransactionCountFromExternalSyncMode(
                                   ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, _cancellationExternalSyncModeUpdateTask);

                                if (_lastBlockHeight > 0)
                                {
                                    var lastBlockHeightTimestampCreate = await ClassApiClientUtility.GetBlockTimestampCreateFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, _lastBlockHeight, _cancellationExternalSyncModeUpdateTask);
                                    _lastBlockHeightTimestampCreate = lastBlockHeightTimestampCreate > 0 ? lastBlockHeightTimestampCreate : _lastBlockHeightTimestampCreate;
                                }

                                if (_lastBlockHeightUnlocked > 0)
                                {
                                    var lastBlockHeightConfirmationTarget = await ClassApiClientUtility.GetGenerateBlockHeightStartTransactionConfirmationFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, _lastBlockHeightUnlocked, _lastBlockHeight, _cancellationExternalSyncModeUpdateTask);
                                    _lastBlockHeightConfirmationTarget = lastBlockHeightConfirmationTarget > 0 ? lastBlockHeightConfirmationTarget : _lastBlockHeightConfirmationTarget;
                                }

                            }
#if DEBUG
                            else
                                Debug.WriteLine("The API " + ClassDesktopWalletCommonData.WalletSettingObject.ApiHost + ":" + ClassDesktopWalletCommonData.WalletSettingObject.ApiPort + " seems to be dead.");
#endif

                            await Task.Delay(1000, _cancellationExternalSyncModeUpdateTask.Token);
                        }
                    }
                    catch
                    {
                        // Ignored.
                    }
                }, _cancellationExternalSyncModeUpdateTask.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored.
            }
        }

        /// <summary>
        /// Update the wallet sync data in external sync mode.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// <param name="cancellation"></param>
        private async Task<bool> UpdateWalletSyncFromExternalSyncMode(string walletFileName, CancellationTokenSource cancellation)
        {

            bool changeDone = false;
            bool cancelled = false;
            string walletAddress = ClassDesktopWalletCommonData.WalletDatabase.GetWalletAddressFromWalletFileName(walletFileName);


#region Sync Mem Pool transaction.

            using (DisposableList<ClassTransactionObject> listTransactionObject = new DisposableList<ClassTransactionObject>())
            {
                long countMemPoolTransaction = await GetTotalMemPoolTransactionFromSyncAsync(cancellation, true);
                long totalRetrieved = 0;

                if (countMemPoolTransaction > 0)
                {
                    using (DisposableList<long> listBlockHeight = await ClassApiClientUtility.GetMemPoolListBlockHeights(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, cancellation))
                    {
                        if (listBlockHeight.Count > 0)
                        {
                            foreach (long blockHeight in listBlockHeight.GetList)
                            {
                                int transactionCount = await ClassApiClientUtility.GetMemPoolTransactionCountByBlockHeightFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, blockHeight, cancellation);

                                if (transactionCount > 0)
                                {
                                    int startRange = 0;
                                    int endRange = 0;

                                    while (startRange < transactionCount)
                                    {
                                        cancellation?.Token.ThrowIfCancellationRequested();

                                        // Increase end range.
                                        int incremented = 0;

                                        while (incremented < ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxRangeTransactionToSyncPerRequest)
                                        {
                                            if (endRange + 1 > transactionCount)
                                                break;

                                            endRange++;
                                            incremented++;

                                            if (incremented == ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxRangeTransactionToSyncPerRequest)
                                                break;
                                        }

                                        using (DisposableList<ClassTransactionObject> listMemPoolTransaction = await ClassApiClientUtility.GetMemPoolTransactionByRangeFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, blockHeight, startRange, endRange, cancellation))
                                        {
                                            foreach (ClassTransactionObject memPoolTransactionObject in listMemPoolTransaction.GetList)
                                            {
                                                if (memPoolTransactionObject?.WalletAddressSender == walletAddress || memPoolTransactionObject?.WalletAddressReceiver == walletAddress)
                                                    listTransactionObject.Add(memPoolTransactionObject);

                                                startRange++;
                                                totalRetrieved++;
                                            }
                                        }

                                        if (totalRetrieved >= transactionCount)
                                            break;
                                    }

                                }
                            }
                        }
                    }

                }

                if (totalRetrieved >= countMemPoolTransaction)
                {
                    foreach (ClassTransactionObject memPoolTransactionObject in listTransactionObject.GetList)
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            cancelled = true;
                            break;
                        }

                        if (memPoolTransactionObject != null)
                        {

                            bool alreadyConfirmed = false;
                            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList.ContainsKey(memPoolTransactionObject.BlockHeightTransaction))
                            {
                                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList[memPoolTransactionObject.BlockHeightTransaction].Contains(memPoolTransactionObject.TransactionHash))
                                    alreadyConfirmed = true;
                            }

                            if (!alreadyConfirmed)
                            {
                                if (!ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Contains(memPoolTransactionObject.TransactionHash))
                                {
                                    if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Add(memPoolTransactionObject.TransactionHash))
                                    {
                                        ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTotalMemPoolTransaction++;
                                        changeDone = true;

                                        await InsertOrUpdateBlockTransactionToSyncCache(walletAddress, new ClassBlockTransaction(0, memPoolTransactionObject)
                                        {
                                            TransactionObject = memPoolTransactionObject,
                                            TransactionStatus = true,
                                            TransactionBlockHeightInsert = memPoolTransactionObject.BlockHeightTransaction,
                                            TransactionBlockHeightTarget = memPoolTransactionObject.BlockHeightTransactionConfirmationTarget
                                        }, true, false, cancellation);

                                        ClassLog.WriteLine(memPoolTransactionObject.TransactionHash + " tx hash of wallet address: " + walletAddress + " from mempool has been synced successfully.", ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                    }
                                }
                            }
                            else
                            {
                                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList.ContainsKey(memPoolTransactionObject.BlockHeightTransaction))
                                {
                                    if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList[memPoolTransactionObject.BlockHeightTransaction].Contains(memPoolTransactionObject.TransactionHash))
                                    {
                                        ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Remove(memPoolTransactionObject.TransactionHash);
                                        changeDone = true;
                                    }
                                }
                            }

                        }
                    }
                }
            }

#endregion

            if (!cancelled)
            {
#region Sync transaction from blocks.

                long lastBlockHeight = await GetLastBlockHeightSynced(cancellation, true);

                if (lastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                {

                    long blockHeightStart = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletLastBlockHeightSynced;

                    if (!cancelled)
                    {
                        if (blockHeightStart <= lastBlockHeight)
                        {
                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletOnSync = true;

                            for (long blockHeight = blockHeightStart; blockHeight <= lastBlockHeight; blockHeight++)
                            {
                                cancellation.Token.ThrowIfCancellationRequested();

                                if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= lastBlockHeight)
                                {
                                    int totalRetry = 0;

                                    ClassBlockObject blockObjectInformation = await ClassApiClientUtility.GetBlockInformationFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, blockHeight, cancellation);

                                    while (blockObjectInformation == null)
                                    {
                                        cancellation.Token.ThrowIfCancellationRequested();

#if DEBUG
                                        Debug.WriteLine("Block Height " + blockHeight + " received from external sync is empty.");
#endif

                                        if (blockHeight > await GetLastBlockHeightSynced(cancellation, true))
                                        {
                                            cancelled = true;
                                            break;
                                        }

                                        blockObjectInformation = await ClassApiClientUtility.GetBlockInformationFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, blockHeight, cancellation);

                                        if (blockObjectInformation != null)
                                            break;
                                        else
                                        {
                                            totalRetry++;
                                            if (totalRetry >= ClassDesktopWalletCommonData.WalletSettingObject.ApiMaxRetry)
                                            {
#if DEBUG
                                                Debug.WriteLine("Max retries reach on retrieve transactions from the API. Cancel sync of " + walletFileName);
#endif
                                                cancelled = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (cancelled)
                                        break;

                                    if (blockObjectInformation.BlockStatus == ClassBlockEnumStatus.LOCKED)
                                        break;

                                    if (blockObjectInformation.TotalTransaction == 0)
                                        break;

                                    bool retrieved = false;


                                    while (!retrieved)
                                    {
                                        cancellation.Token.ThrowIfCancellationRequested();

                                        int totalRetrieved = 0;

                                        using (DisposableList<ClassBlockTransaction> listBlockTransaction = new DisposableList<ClassBlockTransaction>())
                                        {


                                            int startRange = 0;
                                            int endRange = 0;

                                            while (startRange < blockObjectInformation.TotalTransaction)
                                            {
                                                cancellation?.Token.ThrowIfCancellationRequested();

                                                // Increase end range.
                                                int incremented = 0;

                                                while (incremented < ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxRangeTransactionToSyncPerRequest)
                                                {
                                                    if (endRange + 1 > blockObjectInformation.TotalTransaction)
                                                        break;

                                                    endRange++;
                                                    incremented++;

                                                    if (incremented == ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxRangeTransactionToSyncPerRequest)
                                                        break;
                                                }

                                                using (DisposableList<ClassBlockTransaction> listBlockTransactionFetch = await ClassApiClientUtility.GetBlockTransactionByRangeFromExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, blockHeight, startRange, endRange, cancellation))
                                                {
                                                    foreach (ClassBlockTransaction blockTransactionObject in listBlockTransactionFetch.GetList)
                                                    {
                                                        if (blockTransactionObject?.TransactionObject.WalletAddressSender == walletAddress || blockTransactionObject?.TransactionObject.WalletAddressReceiver == walletAddress)
                                                            listBlockTransaction.Add(blockTransactionObject);

                                                        startRange++;
                                                        totalRetrieved++;
                                                    }
                                                }

                                                if (totalRetrieved >= blockObjectInformation.TotalTransaction)
                                                    break;
                                            }

                                            if (blockHeight > await GetLastBlockHeightSynced(cancellation, true))
                                            {
                                                cancelled = true;
                                                break;
                                            }

                                            if (totalRetrieved >= blockObjectInformation.TotalTransaction)
                                            {
                                                retrieved = true;
                                                foreach (var blockTransaction in listBlockTransaction.GetList)
                                                {
                                                    cancellation.Token.ThrowIfCancellationRequested();

                                                    if (blockTransaction.TransactionObject.WalletAddressReceiver == walletAddress || blockTransaction.TransactionObject.WalletAddressSender == walletAddress)
                                                    {
                                                        if (!ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList.ContainsKey(blockHeight))
                                                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList.Add(blockHeight, new HashSet<string>());

                                                        if (!ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList[blockHeight].Contains(blockTransaction.TransactionObject.TransactionHash))
                                                        {
                                                            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTransactionList[blockHeight].Add(blockTransaction.TransactionObject.TransactionHash))
                                                            {
                                                                await InsertOrUpdateBlockTransactionToSyncCache(walletAddress, blockTransaction, false, false, cancellation);
                                                                ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTotalTransaction++;
                                                                changeDone = true;
                                                            }
                                                            else
                                                            {
                                                                cancelled = true;
#if DEBUG
                                                                Debug.WriteLine("Transaction hash: " + blockTransaction.TransactionObject.TransactionHash + " from the block height: " + blockHeight + "can't be inserted into the wallet file data: " + walletFileName);
#endif
                                                                ClassLog.WriteLine("Transaction hash: " + blockTransaction.TransactionObject.TransactionHash + " from the block height: " + blockHeight + "can't be inserted into the wallet file data: " + walletFileName, ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                                                break;
                                                            }
                                                        }

                                                        if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Contains(blockTransaction.TransactionObject.TransactionHash))
                                                        {
                                                            ClassLog.WriteLine(blockTransaction.TransactionObject.TransactionHash + " tx hash of block height: " + blockHeight + " seems to has been accepted by the network to be proceed, remove it from the mempool.", ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);

                                                            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Remove(blockTransaction.TransactionObject.TransactionHash))
                                                            {
                                                                ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletTotalMemPoolTransaction--;
                                                                changeDone = true;
                                                            }
                                                            else
                                                            {
                                                                cancelled = true;
                                                                break;
                                                            }
                                                        }

                                                    }
                                                }
                                            }
                                            else
                                            {
                                                totalRetry++;

                                                if (totalRetry >= ClassDesktopWalletCommonData.WalletSettingObject.ApiMaxRetry)
                                                {
#if DEBUG
                                                    Debug.WriteLine("Max retries reach on retrieve transactions from the API. Cancel sync of " + walletFileName);
#endif
                                                    cancelled = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }


                                    if (!cancelled)
                                        ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletLastBlockHeightSynced = blockHeight;
                                    else
                                        break;
                                }
                            }

                            if (cancelled)
                            {
#if DEBUG
                                Debug.WriteLine("Sync cancelled for wallet address: " + walletAddress);
#endif
                                changeDone = false;
                                ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletEnableRescan = true;
                            }
                            else
                            {
                                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletLastBlockHeightSynced != blockHeightStart)
                                {
#if DEBUG
                                    Debug.WriteLine("Change done on the sync of the wallet filename: " + walletFileName + " - Previous Height: " + blockHeightStart + " | New Height: " + ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletLastBlockHeightSynced);
#endif
                                    changeDone = true;
                                }
                            }

                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletOnSync = false;

                        }
                        else if (blockHeightStart > lastBlockHeight)
                        {

                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletEnableRescan = true;
#if DEBUG
                            Debug.WriteLine("Warning the wallet file data: " + walletFileName + " last block height sync progress is above the current one synced: " + blockHeightStart + "/" + lastBlockHeight);
#endif
                            ClassLog.WriteLine("Warning the wallet file data: " + walletFileName + " last block height sync progress is above the current one synced: " + blockHeightStart + "/" + lastBlockHeight, ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);

                        }
                    }
                }
                else
                    ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileName].WalletEnableRescan = true;

#endregion
            }

            return cancelled ? false : changeDone;
        }


#endregion

#region Related of build & send transaction functions.

        /// <summary>
        /// Build and send a transaction.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="walletAddressTarget"></param>
        /// <param name="amountToSend"></param>
        /// <param name="feeToPay"></param>
        /// <param name="paymentId"></param>
        /// <param name="totalConfirmationTarget"></param>
        /// <param name="walletPrivateKeySender"></param>
        /// <param name="transactionAmountSourceList"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> BuildAndSendTransaction(string walletFileOpened, string walletAddressTarget, decimal amountToSend, decimal feeToPay, long paymentId, int totalConfirmationTarget, string walletPrivateKeySender, Dictionary<string, ClassTransactionHashSourceObject> transactionAmountSourceList, CancellationTokenSource cancellation)
        {
            bool sendTransactionStatus = false;

            try
            {
                long lastBlockHeight = await GetLastBlockHeightSynced(cancellation, true);
                long lastBlockHeightUnlocked = await GetLastBlockHeightUnlockedSynced(cancellation, true);
                long blockHeightTransaction = await GenerateBlockHeightStartTransactionConfirmationFromSync(lastBlockHeightUnlocked, lastBlockHeight, cancellation, true);
                long lastBlockHeightTimestampCreate = await GetBlockTimestampCreateFromBlockHeight(lastBlockHeight, cancellation, true);
                string walletAddressSender = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileOpened].WalletAddress;
                string walletPublicKeySender = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileOpened].WalletPublicKey;

                ClassTransactionObject transactionObject = ClassTransactionUtility.BuildTransaction(blockHeightTransaction,
                                                        blockHeightTransaction + totalConfirmationTarget,
                                                        walletAddressSender,
                                                        walletPublicKeySender,
                                                        string.Empty,
                                                        (BigInteger)(amountToSend * BlockchainSetting.CoinDecimal),
                                                        (BigInteger)(feeToPay * BlockchainSetting.CoinDecimal),
                                                        walletAddressTarget,
                                                        ClassUtility.GetCurrentTimestampInSecond(),
                                                        ClassTransactionEnumType.NORMAL_TRANSACTION,
                                                        paymentId,
                                                        string.Empty,
                                                        string.Empty,
                                                        walletPrivateKeySender,
                                                        string.Empty, transactionAmountSourceList, lastBlockHeightTimestampCreate, cancellation);

                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileOpened].WalletEncrypted)
                    walletPrivateKeySender.Clear();

                if (transactionObject != null)
                {
                    switch (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode)
                    {
                        case ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE:
                            {
                                string nodeLocalIp = _nodeInstance.PeerSettingObject.PeerNetworkSettingObject.ListenIp;
                                string openNatIp = _nodeInstance.PeerOpenNatPublicIp;

                                using (DisposableDictionary<string, ClassTransactionEnumStatus> sendTransactionResult = await ClassPeerNetworkBroadcastFunction.AskMemPoolTxVoteToPeerListsAsync(nodeLocalIp, openNatIp, openNatIp, new List<ClassTransactionObject>() { transactionObject }, _nodeInstance.PeerSettingObject.PeerNetworkSettingObject, _nodeInstance.PeerSettingObject.PeerFirewallSettingObject, cancellation, true))
                                {

                                    KeyValuePair<string, ClassTransactionEnumStatus> sendTransactionResultElement = sendTransactionResult.GetList.ElementAt(0);
#if DEBUG
                                    Debug.WriteLine("Send transaction request result: " + sendTransactionResultElement.Key + " | Tx response status: " + System.Enum.GetName(typeof(ClassTransactionEnumStatus), sendTransactionResultElement.Value));
#endif
                                    ClassLog.WriteLine("Send transaction request result: " + sendTransactionResultElement.Key + " | Tx response status: " + System.Enum.GetName(typeof(ClassTransactionEnumStatus), sendTransactionResultElement.Value), ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);

                                    if (sendTransactionResultElement.Value == ClassTransactionEnumStatus.VALID_TRANSACTION)
                                    {
                                        sendTransactionStatus = true;

                                        ClassMemPoolDatabase.InsertTxToMemPool(transactionObject);
                                    }
                                }
                            }
                            break;
                        case ClassWalletSettingEnumSyncMode.EXTERNAL_PEER_SYNC_MODE:
                            {
                                sendTransactionStatus = await ClassApiClientUtility.SendTransactionByExternalSyncMode(ClassDesktopWalletCommonData.WalletSettingObject.ApiHost, ClassDesktopWalletCommonData.WalletSettingObject.ApiPort, ClassDesktopWalletCommonData.WalletSettingObject.WalletInternalSyncNodeSetting.PeerNetworkSettingObject.PeerApiMaxConnectionDelay, transactionObject, cancellation);
                            }
                            break;
                    }
                }

                if (sendTransactionStatus)
                {
                    await InsertOrUpdateBlockTransactionToSyncCache(walletAddressSender, new ClassBlockTransaction(0, transactionObject)
                    {
                        TransactionObject = transactionObject,
                        TransactionStatus = true,
                        TransactionBlockHeightInsert = transactionObject.BlockHeightTransaction,
                        TransactionBlockHeightTarget = transactionObject.BlockHeightTransactionConfirmationTarget
                    }, true, true, cancellation);

                    ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileOpened].WalletTotalMemPoolTransaction++;

                    _lastTotalMemPoolTransaction++;
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine("Error on sending a transaction. Exception: " + error.Message);
#endif
                ClassLog.WriteLine("Error on sending a transaction. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
            }

            return sendTransactionStatus;
        }

        /// <summary>
        /// Calculate the fee cost size of the transaction virtually before to send it.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <param name="sendAmount"></param>
        /// <param name="totalConfirmationTarget"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<ClassSendTransactionFeeCostCalculationResultObject> GetTransactionFeeCostVirtuallyFromSync(string walletFileOpened, decimal sendAmount, int totalConfirmationTarget, CancellationTokenSource cancellation)
        {
            BigInteger amountToSpend = (BigInteger)(sendAmount * BlockchainSetting.CoinDecimal);
            BigInteger amountSpend = 0;

            ClassSendTransactionFeeCostCalculationResultObject sendTransactionFeeCostCalculationResult = new ClassSendTransactionFeeCostCalculationResultObject();

            using (DisposableDictionary<string, ClassWalletSyncAmountSpendObject> listUnspend = new DisposableDictionary<string, ClassWalletSyncAmountSpendObject>())
            {

                string walletAddress = ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFileOpened].WalletAddress;

                if (!DatabaseSyncCache.ContainsKey(walletAddress))
                {
#if DEBUG
                    Debug.WriteLine("No transaction synced on the cache for the wallet address: " + walletAddress);
#endif
                    sendTransactionFeeCostCalculationResult.Failed = true;
                    return sendTransactionFeeCostCalculationResult;
                }

                if (DatabaseSyncCache[walletAddress].CountBlockHeight == 0)
                {
#if DEBUG
                    Debug.WriteLine("No transaction synced on the cache for the wallet address: " + walletAddress);
#endif
                    sendTransactionFeeCostCalculationResult.Failed = true;
                    return sendTransactionFeeCostCalculationResult;
                }

#region Generated list unspend.

                using (DisposableDictionary<long, Dictionary<string, ClassSyncCacheBlockTransactionObject>> allBlockTransactions = await DatabaseSyncCache[walletAddress].GetAllBlockTransactionCached(cancellation))
                {
                    foreach (long blockHeight in allBlockTransactions.GetList.Keys)
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            sendTransactionFeeCostCalculationResult.Failed = true;
                            return sendTransactionFeeCostCalculationResult;
                        }

                        foreach (var transactionPair in allBlockTransactions[blockHeight])
                        {
                            if (cancellation.IsCancellationRequested)
                            {
                                sendTransactionFeeCostCalculationResult.Failed = true;
                                return sendTransactionFeeCostCalculationResult;
                            }

                            if (transactionPair.Value.BlockTransaction != null)
                            {
                                if (transactionPair.Value.BlockTransaction.TransactionStatus)
                                {
                                    if (transactionPair.Value.BlockTransaction.TransactionObject.WalletAddressSender == walletAddress)
                                    {
                                        foreach (var txAmountSpent in transactionPair.Value.BlockTransaction.TransactionObject.AmountTransactionSource)
                                        {
                                            cancellation?.Token.ThrowIfCancellationRequested();

                                            long blockHeightTxAmountSpend = ClassTransactionUtility.GetBlockHeightFromTransactionHash(txAmountSpent.Key);

                                            ClassBlockTransaction blockTransaction = allBlockTransactions[blockHeightTxAmountSpend].ContainsKey(txAmountSpent.Key) ? allBlockTransactions[blockHeightTxAmountSpend][txAmountSpent.Key].BlockTransaction : DatabaseSyncCache[walletAddress].GetSyncBlockTransactionCached(blockHeightTxAmountSpend, txAmountSpent.Key)?.BlockTransaction;


                                            if (blockTransaction == null)
                                            {
                                                sendTransactionFeeCostCalculationResult.Failed = true;
                                                return sendTransactionFeeCostCalculationResult;
                                            }


                                            if (blockTransaction.TransactionObject.WalletAddressReceiver == walletAddress)
                                            {
                                                if (!listUnspend.ContainsKey(txAmountSpent.Key))
                                                {
                                                    listUnspend.Add(txAmountSpent.Key, new ClassWalletSyncAmountSpendObject()
                                                    {
                                                        TxAmount = blockTransaction.TransactionObject.Amount,
                                                        AmountSpend = 0
                                                    });
                                                }

                                                if (listUnspend.ContainsKey(txAmountSpent.Key))
                                                {
                                                    if (!listUnspend[txAmountSpent.Key].Spend)
                                                    {
                                                        if (txAmountSpent.Value.Amount > 0)
                                                        {

                                                            listUnspend[txAmountSpent.Key].AmountSpend += txAmountSpent.Value.Amount;

                                                            if (listUnspend[txAmountSpent.Key].TxAmount <= listUnspend[txAmountSpent.Key].AmountSpend)
                                                            {
                                                                listUnspend[txAmountSpent.Key].Spend = true;
#if DEBUG
                                                                if (listUnspend[txAmountSpent.Key].TxAmount < listUnspend[txAmountSpent.Key].AmountSpend)
                                                                    Debug.WriteLine("Warning, the spending of the tx hash: " + txAmountSpent.Key + " is above the amount. Spend " + listUnspend[txAmountSpent.Key].AmountSpend + "/" + listUnspend[txAmountSpent.Key].TxAmount);
#endif
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else if (transactionPair.Value.BlockTransaction.TransactionObject.WalletAddressReceiver == walletAddress)
                                    {
                                        if (!listUnspend.ContainsKey(transactionPair.Key))
                                        {
                                            listUnspend.Add(transactionPair.Key, new ClassWalletSyncAmountSpendObject()
                                            {
                                                TxAmount = DatabaseSyncCache[walletAddress].GetSyncBlockTransactionCached(transactionPair.Value.BlockTransaction.TransactionObject.BlockHeightTransaction, transactionPair.Key).BlockTransaction.TransactionObject.Amount,
                                                AmountSpend = 0
                                            });
                                        }
                                    }
                                }
                            }
                        }

                    }
                }

#endregion

#region Generate transaction hash source list from the amount to spend.

                if (listUnspend.Count > 0)
                {
                    foreach (string transactionHash in listUnspend.GetList.Keys)
                    {
                        if (cancellation.IsCancellationRequested)
                        {
                            sendTransactionFeeCostCalculationResult.Failed = true;
                            return sendTransactionFeeCostCalculationResult;
                        }

                        if (!listUnspend[transactionHash].Spend)
                        {
                            BigInteger totalRest = listUnspend[transactionHash].TxAmount - listUnspend[transactionHash].AmountSpend;
                            if (totalRest + amountSpend > amountToSpend)
                            {
                                BigInteger spendRest = amountToSpend - amountSpend;

                                if (spendRest > 0)
                                {
                                    if (totalRest >= spendRest)
                                    {
                                        amountSpend += spendRest;

                                        sendTransactionFeeCostCalculationResult.TransactionAmountSourceList.Add(transactionHash, new ClassTransactionHashSourceObject()
                                        {
                                            Amount = spendRest,
                                        });
                                    }
                                }
                            }
                            else
                            {
                                amountSpend += totalRest;
                                sendTransactionFeeCostCalculationResult.TransactionAmountSourceList.Add(transactionHash, new ClassTransactionHashSourceObject()
                                {
                                    Amount = totalRest,
                                });
                            }
                        }

                        if (amountSpend == amountToSpend)
                            break;
                    }
                }

#endregion

                if (amountToSpend == amountSpend)
                {
                    sendTransactionFeeCostCalculationResult.Failed = false;

                    sendTransactionFeeCostCalculationResult.TotalFeeCost = ClassTransactionUtility.GetBlockTransactionVirtualMemorySizeOnSending(sendTransactionFeeCostCalculationResult.TransactionAmountSourceList, amountToSpend);
                    sendTransactionFeeCostCalculationResult.FeeSizeCost += sendTransactionFeeCostCalculationResult.TotalFeeCost;

                    long lastBlockHeight = await GetLastBlockHeightSynced(cancellation, true);
                    long lastBlockHeightUnlocked = await GetLastBlockHeightUnlockedSynced(cancellation, true);
                    long blockHeightConfirmationStart = await GenerateBlockHeightStartTransactionConfirmationFromSync(lastBlockHeightUnlocked, lastBlockHeight, cancellation, true);
                    long blockHeightConfirmationTarget = blockHeightConfirmationStart + totalConfirmationTarget;

                    Tuple<BigInteger, bool> calculationFeeCostConfirmation = await GetFeeCostConfirmationFromWholeActivityBlockchainFromSync(lastBlockHeightUnlocked, blockHeightConfirmationStart, blockHeightConfirmationTarget, cancellation);

                    if (calculationFeeCostConfirmation.Item2)
                    {
                        sendTransactionFeeCostCalculationResult.TotalFeeCost += calculationFeeCostConfirmation.Item1;
                        sendTransactionFeeCostCalculationResult.FeeConfirmationCost = calculationFeeCostConfirmation.Item1;

                        sendTransactionFeeCostCalculationResult.TransactionAmountSourceList.Clear();

                        amountToSpend = ((BigInteger)(sendAmount * BlockchainSetting.CoinDecimal)) + sendTransactionFeeCostCalculationResult.TotalFeeCost;
                        amountSpend = 0;

#region Regenerate amount hash transaction source list with fees calculated.

                        if (listUnspend.Count > 0)
                        {
                            foreach (string transactionHash in listUnspend.GetList.Keys)
                            {
                                if (cancellation.IsCancellationRequested)
                                {
                                    sendTransactionFeeCostCalculationResult.Failed = true;
                                    return sendTransactionFeeCostCalculationResult;
                                }

                                if (!listUnspend[transactionHash].Spend)
                                {
                                    BigInteger totalRest = listUnspend[transactionHash].TxAmount - listUnspend[transactionHash].AmountSpend;

                                    if (totalRest + amountSpend > amountToSpend)
                                    {
                                        BigInteger spendRest = amountToSpend - amountSpend;

                                        if (spendRest > 0)
                                        {
                                            if (totalRest >= spendRest)
                                            {
                                                amountSpend += spendRest;

                                                sendTransactionFeeCostCalculationResult.TransactionAmountSourceList.Add(transactionHash, new ClassTransactionHashSourceObject()
                                                {
                                                    Amount = spendRest,
                                                });
                                            }
                                        }
                                    }
                                    else
                                    {
                                        amountSpend += totalRest;
                                        sendTransactionFeeCostCalculationResult.TransactionAmountSourceList.Add(transactionHash, new ClassTransactionHashSourceObject()
                                        {
                                            Amount = totalRest,
                                        });
                                    }
                                }

                                if (amountSpend == amountToSpend)
                                    break;
                            }
                        }

#endregion

                        if (amountSpend > amountToSpend)
                            sendTransactionFeeCostCalculationResult.Failed = true;
                    }
                    else
                        sendTransactionFeeCostCalculationResult.Failed = true;
                }

                // Clean up.
                listUnspend.Clear();
            }

            return sendTransactionFeeCostCalculationResult;
        }

#endregion
    }
}
