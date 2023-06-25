using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Desktop_Wallet.Sync.Object
{
    public class ClassSyncCacheObject
    {
        /// <summary>
        /// Handle multithreading access.
        /// </summary>
        private SemaphoreSlim _semaphoreDictionaryAccess;

        /// <summary>
        /// Store the cache.
        /// </summary>
        private ConcurrentDictionary<long, ConcurrentDictionary<string, ClassSyncCacheBlockTransactionObject>> _syncCacheDatabase;


        /// <summary>
        /// Store the available balance.
        /// </summary>
        public BigInteger AvailableBalance;
        public BigInteger PendingBalance;

        /// <summary>
        /// Get the total amount of transactions cached.
        /// </summary>
        public long TotalTransactions
        {
            get
            {
                long totalTransactions = 0;

                using (var listBlockHeight = BlockHeightKeys)
                    foreach (long blockHeight in listBlockHeight.GetList)
                        totalTransactions += _syncCacheDatabase[blockHeight].Count;

                return totalTransactions;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassSyncCacheObject()
        {
            _syncCacheDatabase = new ConcurrentDictionary<long, ConcurrentDictionary<string, ClassSyncCacheBlockTransactionObject>>();
            _semaphoreDictionaryAccess = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Get the amount of block height.
        /// </summary>
        public int CountBlockHeight => _syncCacheDatabase.Count;


        /// <summary>
        /// Get the list of block heights.
        /// </summary>
        /// <returns></returns>
        public DisposableList<long> BlockHeightKeys => new DisposableList<long>(true, 0, _syncCacheDatabase.Keys.ToList());

      
        /// <summary>
        /// Check if a block height is stored.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public bool ContainsBlockHeight(long blockHeight) => _syncCacheDatabase.ContainsKey(blockHeight);

        /// <summary>
        /// Insert a block height to the cache.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> InsertBlockHeight(long blockHeight, CancellationTokenSource cancellation)
        {
            bool result = false;
            bool semaphoreUsed = false;
            try
            {
                semaphoreUsed = await _semaphoreDictionaryAccess.TryWaitAsync(cancellation);

                if (!semaphoreUsed)
                    return result;

                result = _syncCacheDatabase.ContainsKey(blockHeight);

                if (!result)
                    result = _syncCacheDatabase.TryAdd(blockHeight, new ConcurrentDictionary<string, ClassSyncCacheBlockTransactionObject>());
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreDictionaryAccess.Release();
            }
            return result;
        }

        /// <summary>
        /// Count the amount of block transaction stored at a specific block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<int> CountBlockTransactionFromBlockHeight(long blockHeight, CancellationTokenSource cancellation)
        {
            int count = 0;

            bool semaphoreUsed = false;
            try
            {
                semaphoreUsed = await _semaphoreDictionaryAccess.TryWaitAsync(cancellation);

                if (!semaphoreUsed)
                    return count;

                if (_syncCacheDatabase.ContainsKey(blockHeight))
                    count = _syncCacheDatabase[blockHeight].Count;

            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreDictionaryAccess.Release();
            }

            return count;
        }

        /// <summary>
        /// Check if a block transaction is stored at a specific block height with a transaction hash provided.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="transactionHash"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> ContainsBlockTransactionFromTransactionHashAndBlockHeight(long blockHeight, string transactionHash, CancellationTokenSource cancellation)
        {
            bool result = false;
            bool semaphoreUsed = false;
            try
            {
                semaphoreUsed = await _semaphoreDictionaryAccess.TryWaitAsync(cancellation);

                if (!semaphoreUsed)
                    return result;

                if (_syncCacheDatabase.ContainsKey(blockHeight))
                    result = _syncCacheDatabase[blockHeight].ContainsKey(transactionHash);
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreDictionaryAccess.Release();
            }

            return result;
        }

        /// <summary>
        /// Insert a block transaction to the cache.
        /// </summary>
        /// <param name="syncCacheBlockTransactionObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> InsertBlockTransaction(ClassSyncCacheBlockTransactionObject syncCacheBlockTransactionObject, CancellationTokenSource cancellation)
        {
            bool result = false;
            bool semaphoreUsed = false;
            try
            {
                semaphoreUsed = await _semaphoreDictionaryAccess.TryWaitAsync(cancellation);

                if (!semaphoreUsed)
                    return result;

                bool insertBlockHeight = true;

                if (!_syncCacheDatabase.ContainsKey(syncCacheBlockTransactionObject.BlockTransaction.TransactionObject.BlockHeightTransaction))
                    insertBlockHeight = _syncCacheDatabase.TryAdd(syncCacheBlockTransactionObject.BlockTransaction.TransactionObject.BlockHeightTransaction, new ConcurrentDictionary<string, ClassSyncCacheBlockTransactionObject>()); ;

                if (insertBlockHeight)
                {
                    if (!_syncCacheDatabase[syncCacheBlockTransactionObject.BlockTransaction.TransactionObject.BlockHeightTransaction].ContainsKey(syncCacheBlockTransactionObject.BlockTransaction.TransactionObject.TransactionHash))
                        result = _syncCacheDatabase[syncCacheBlockTransactionObject.BlockTransaction.TransactionObject.BlockHeightTransaction].TryAdd(syncCacheBlockTransactionObject.BlockTransaction.TransactionObject.TransactionHash, syncCacheBlockTransactionObject);
                    else
                    {
                        _syncCacheDatabase[syncCacheBlockTransactionObject.BlockTransaction.TransactionObject.BlockHeightTransaction][syncCacheBlockTransactionObject.BlockTransaction.TransactionObject.TransactionHash] = syncCacheBlockTransactionObject;
                        result = true;
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreDictionaryAccess.Release();
            }

            return result;
        }

        /// <summary>
        /// Update a block transaction stored into the cache.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <param name="isMemPool"></param>
        public void UpdateBlockTransaction(ClassBlockTransaction blockTransaction, bool isMemPool)
        {
            if (_syncCacheDatabase.ContainsKey(blockTransaction.TransactionObject.BlockHeightTransaction))
            {
                if (_syncCacheDatabase[blockTransaction.TransactionObject.BlockHeightTransaction].ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                {
                    _syncCacheDatabase[blockTransaction.TransactionObject.BlockHeightTransaction][blockTransaction.TransactionObject.TransactionHash].BlockTransaction = blockTransaction;
                    _syncCacheDatabase[blockTransaction.TransactionObject.BlockHeightTransaction][blockTransaction.TransactionObject.TransactionHash].IsMemPool = isMemPool;
                }
            }
        }

        /// <summary>
        /// Clear the cache.
        /// </summary>
        /// <param name="cancellation"></param>
        public async Task Clear(CancellationTokenSource cancellation)
        {

            await _semaphoreDictionaryAccess.TryWaitExecuteActionAsync(
            new Action(() =>
            {
                foreach (long blockHeight in BlockHeightKeys.GetAll)
                    _syncCacheDatabase[blockHeight].Clear();

                _syncCacheDatabase.Clear();

                AvailableBalance = 0;
                PendingBalance = 0;
            }), cancellation);

        }

        /// <summary>
        /// Return every block transactions stored at a specific block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<DisposableDictionary<string, ClassSyncCacheBlockTransactionObject>> GetBlockTransactionFromBlockHeight(long blockHeight, CancellationTokenSource cancellation)
        {
            DisposableDictionary<string, ClassSyncCacheBlockTransactionObject> listBlockTransaction = new DisposableDictionary<string, ClassSyncCacheBlockTransactionObject>();

            bool semaphoreUsed = false;

            try
            {
                semaphoreUsed = await _semaphoreDictionaryAccess.TryWaitAsync(cancellation);

                if (!semaphoreUsed)
                    return listBlockTransaction;

                if (_syncCacheDatabase.ContainsKey(blockHeight))
                    listBlockTransaction.GetList = new Dictionary<string, ClassSyncCacheBlockTransactionObject>(_syncCacheDatabase[blockHeight]);
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreDictionaryAccess.Release();
            }

            return listBlockTransaction;
        }

        /// <summary>
        /// Return a list of block transaction at a specific block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<DisposableList<string>> GetListBlockTransactionHashFromBlockHeight(long blockHeight, bool exceptMemPool, CancellationTokenSource cancellation)
        {
            DisposableList<string> listBlockTransactionHash = new DisposableList<string>();
            bool semaphoreUsed = false;

            try
            {
                semaphoreUsed = await _semaphoreDictionaryAccess.TryWaitAsync(cancellation);

                if (!semaphoreUsed)
                    return listBlockTransactionHash;

                if (_syncCacheDatabase.ContainsKey(blockHeight))
                {
                    if (!exceptMemPool)
                        listBlockTransactionHash.GetList = _syncCacheDatabase[blockHeight].Keys.ToList();
                    else
                    {
                        foreach (string transactionHash in _syncCacheDatabase[blockHeight].Keys.ToArray())
                        {
                            if (cancellation.IsCancellationRequested)
                                break;

                            if (!_syncCacheDatabase[blockHeight][transactionHash].IsMemPool)
                                listBlockTransactionHash.Add(transactionHash);
                        }
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreDictionaryAccess.Release();
            }

            return listBlockTransactionHash;
        }

        /// <summary>
        /// Check if block transaction from the block height target are all confirmed.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> CheckIfBlockTransactionFromHeightAreFullyConfirmed(long blockHeight, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;

            int countValidPassed = 0;
            int countConfirmedPassed = 0;
            int countMemPoolPassed = 0;

            try
            {
                semaphoreUsed = await _semaphoreDictionaryAccess.TryWaitAsync(cancellation);

                if (!semaphoreUsed)
                    return false;

                if (_syncCacheDatabase.ContainsKey(blockHeight))
                {
                    foreach (string transactionHash in _syncCacheDatabase[blockHeight].Keys.ToArray())
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        if (!_syncCacheDatabase[blockHeight][transactionHash].IsMemPool && _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionStatus)
                        {
                            countValidPassed++;

                            if (_syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionTotalConfirmation + _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.BlockHeightTransaction >=
                                _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.BlockHeightTransactionConfirmationTarget)
                            {
                                countConfirmedPassed++;
                            }
                        }
                        else if (_syncCacheDatabase[blockHeight][transactionHash].IsMemPool)
                            countMemPoolPassed++;
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreDictionaryAccess.Release();
            }

            // If every transactions are invalid, it's unecessary to try to update them.
            return countConfirmedPassed == countValidPassed || (countValidPassed == 0 && countMemPoolPassed == 0);
        }

        /// <summary>
        /// Check if block transaction from the block height target is already passed on confirmation.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="lastBlockHeightTransactionConfirmation"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> CheckIfBlockTransactionFromHeightAreConfirmed(long blockHeight, long lastBlockHeightTransactionConfirmation, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;

            bool isConfirmed = false;

            try
            {
                semaphoreUsed = await _semaphoreDictionaryAccess.TryWaitAsync(cancellation);

                if (!semaphoreUsed)
                    return isConfirmed;

                if (_syncCacheDatabase.ContainsKey(blockHeight))
                {
                    foreach (string transactionHash in _syncCacheDatabase[blockHeight].Keys.ToArray())
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        if (!_syncCacheDatabase[blockHeight][transactionHash].IsMemPool && _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionStatus)
                        {
                            if (_syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionTotalConfirmation + _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.BlockHeightTransaction >=
                                lastBlockHeightTransactionConfirmation)
                            {
                                isConfirmed = true;
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreDictionaryAccess.Release();
            }

            return isConfirmed;
        }

        /// <summary>
        /// Return a list of all block transaction.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<DisposableDictionary<long, List<string>>> GetListOfAllBlockTransactionHash(CancellationTokenSource cancellation)
        {
            using (DisposableDictionary<long, List<string>> listBlockTransactionHash = new DisposableDictionary<long, List<string>>())
            {
                bool semaphoreUsed = false;

                try
                {
                    
                    semaphoreUsed = await _semaphoreDictionaryAccess.TryWaitAsync(cancellation);

                    if (!semaphoreUsed)
                        return listBlockTransactionHash;

                    using (DisposableList<long> listBlockHeights = BlockHeightKeys)
                    {
                        foreach (long blockHeight in listBlockHeights.GetList)
                        {
                            foreach (string transactionHash in _syncCacheDatabase[blockHeight].Keys.ToArray())
                            {
                                if (cancellation.IsCancellationRequested)
                                    break;

                                if (!_syncCacheDatabase[blockHeight][transactionHash].IsMemPool)
                                {
                                    if (!listBlockTransactionHash.ContainsKey(blockHeight))
                                        listBlockTransactionHash.Add(blockHeight, new List<string>());

                                    listBlockTransactionHash[blockHeight].Add(transactionHash);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (semaphoreUsed)
                        _semaphoreDictionaryAccess.Release();
                }

                return listBlockTransactionHash;
            }
        }

        /// <summary>
        /// Return a block transaction cached at a specific block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="transactionHash"></param>
        /// <returns></returns>
        public ClassSyncCacheBlockTransactionObject GetSyncBlockTransactionCached(long blockHeight, string transactionHash)
        {
            if (_syncCacheDatabase.ContainsKey(blockHeight))
            {
                if (_syncCacheDatabase[blockHeight].ContainsKey(transactionHash))
                {
                    if (_syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionStatus && _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionTotalConfirmation > 0)
                        _syncCacheDatabase[blockHeight][transactionHash].IsMemPool = false;

                    return _syncCacheDatabase[blockHeight][transactionHash];
                }
            }
            return null;
        }

        /// <summary>
        /// Remove a synced block transaction cached.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="transactionHash"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task RemoveSyncedBlockTransactionCached(long blockHeight, string transactionHash, CancellationTokenSource cancellation)
        {

            await _semaphoreDictionaryAccess.TryWaitExecuteActionAsync(
            new Action(() =>
            {
                if (_syncCacheDatabase.ContainsKey(blockHeight))
                {
                    if (_syncCacheDatabase[blockHeight].ContainsKey(transactionHash))
                        _syncCacheDatabase[blockHeight].TryRemove(transactionHash, out _);

                    if (_syncCacheDatabase[blockHeight].Count == 0)
                        _syncCacheDatabase.TryRemove(blockHeight, out _);
                }

            }), cancellation);

        }

        /// <summary>
        /// Clear every block transaction on the block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task ClearBlockTransactionOnBlockHeight(long blockHeight, CancellationTokenSource cancellation)
        {

            await _semaphoreDictionaryAccess.TryWaitExecuteActionAsync(
            new Action(() =>
            {

                if (_syncCacheDatabase.ContainsKey(blockHeight))
                    _syncCacheDatabase.Clear();

            }), cancellation); 
            

        }

        /// <summary>
        /// Return all block heights with every block transaction synced.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<DisposableDictionary<long, Dictionary<string, ClassSyncCacheBlockTransactionObject>>> GetAllBlockTransactionCached(CancellationTokenSource cancellation)
        {
            DisposableDictionary<long, Dictionary<string, ClassSyncCacheBlockTransactionObject>> listBlockTransactionSynced = new DisposableDictionary<long, Dictionary<string, ClassSyncCacheBlockTransactionObject>>();
            bool semaphoreUsed = false;

            try
            {
                semaphoreUsed = await _semaphoreDictionaryAccess.TryWaitAsync(cancellation);

                if (!semaphoreUsed)
                    return listBlockTransactionSynced;

                using (var listBlockHeight = BlockHeightKeys)
                {
                    foreach (long blockHeight in listBlockHeight.GetList)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        listBlockTransactionSynced.Add(blockHeight, new Dictionary<string, ClassSyncCacheBlockTransactionObject>());
                        listBlockTransactionSynced[blockHeight] = _syncCacheDatabase[blockHeight].ToDictionary(x => x.Key, x => x.Value);

                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreDictionaryAccess.Release();
            }

            return listBlockTransactionSynced;
        }

        /// <summary>
        /// Update wallet balances from sync cached data.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task UpdateWalletBalance(CancellationTokenSource cancellation)
        {

            await _semaphoreDictionaryAccess.TryWaitExecuteActionAsync(
                new Action(() =>
            {
                BigInteger availableBalance = 0;
                BigInteger pendingBalance = 0;

                using (DisposableList<long> listBlockHeight = new DisposableList<long>(false, 0, BlockHeightKeys.GetList))
                {

                    foreach (long blockHeight in listBlockHeight.GetList)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        using (DisposableList<string> listTransactionHash = new DisposableList<string>(false, 0, _syncCacheDatabase[blockHeight].Keys.ToList()))
                        {
                            foreach (var transactionHash in listTransactionHash.GetList)
                            {
                                if (cancellation.IsCancellationRequested)
                                    break;

                                if (_syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionStatus)
                                {
                                    if (!_syncCacheDatabase[blockHeight][transactionHash].IsMemPool)
                                    {
                                        if (_syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.IsConfirmed)
                                        {
                                            if (!_syncCacheDatabase[blockHeight][transactionHash].IsSender)
                                                availableBalance += _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Amount;
                                            else
                                                availableBalance -= (_syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Amount + _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Fee);
                                        }
                                        else
                                        {
                                            if (!_syncCacheDatabase[blockHeight][transactionHash].IsSender)
                                                pendingBalance += _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Amount;
                                            else
                                                pendingBalance -= (_syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Amount + _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Fee);
                                        }
                                    }
                                    else
                                    {
                                        if (_syncCacheDatabase[blockHeight][transactionHash].IsSender)
                                        {
                                            if (availableBalance - (_syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Amount + _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Fee) >= 0)
                                                availableBalance -= (_syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Amount + _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Fee);

                                            pendingBalance -= (_syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Amount + _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Fee);
                                        }
                                        else
                                            pendingBalance += _syncCacheDatabase[blockHeight][transactionHash].BlockTransaction.TransactionObject.Amount;

                                    }
                                }
                            }
                        }
                    }
                }

                AvailableBalance = availableBalance;
                PendingBalance = pendingBalance;
            }), cancellation);

        }
    }
}
