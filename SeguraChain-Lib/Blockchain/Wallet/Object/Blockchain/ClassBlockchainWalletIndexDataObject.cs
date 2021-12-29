using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Blockchain.Checkpoint.Enum;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Other.Object.List;

using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Wallet.Object.Blockchain
{
    public class ClassBlockchainWalletIndexDataObject
    {
        /// <summary>
        /// Wallet Address.
        /// </summary>
        private string _walletAddress;

        /// <summary>
        /// About listed transactions on blocks.
        /// </summary>
        private SortedDictionary<long, int> _dictionaryWalletTransactionIndex;

        /// <summary>
        /// About mem pool transactions.
        /// </summary>
        private SortedDictionary<string, long> _dictionaryWalletMemPoolTransactionIndex;

        /// <summary>
        /// Wallet balance checkpoint.
        /// </summary>
        private SortedDictionary<long, ClassBlockchainWalletBalanceCheckpointObject> _blockchainWalletBalanceCheckpointObject;


        /// <summary>
        /// Semaphore used to prevent multithreading access.
        /// </summary>
        private SemaphoreSlim _semaphoreAccessWalletTxIndex;
        private SemaphoreSlim _semaphoreAccessWalletMemPoolTxIndex;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassBlockchainWalletIndexDataObject(string walletAddress)
        {
            _walletAddress = walletAddress;
            _dictionaryWalletTransactionIndex = new SortedDictionary<long, int>();
            _dictionaryWalletMemPoolTransactionIndex = new SortedDictionary<string, long>();
            _blockchainWalletBalanceCheckpointObject = new SortedDictionary<long, ClassBlockchainWalletBalanceCheckpointObject>();
            _semaphoreAccessWalletTxIndex = new SemaphoreSlim(1, 1);
            _semaphoreAccessWalletMemPoolTxIndex = new SemaphoreSlim(1, 1);
        }

        #region Manage wallet transaction indexed.

        /// <summary>
        /// Insert a transaction index from tx hash/block height provided.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="useSemaphore"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> InsertWalletTransactionHash(string transactionHash, long blockHeight, CancellationTokenSource cancellation)
        {
            bool result = true;

            bool semaphoreUsed = false;

            try
            {

                await _semaphoreAccessWalletTxIndex.WaitAsync(cancellation.Token);
                semaphoreUsed = true;

                // Force to use semaphore if the block height is not indexed.
                if (!_dictionaryWalletTransactionIndex.ContainsKey(blockHeight))
                {
                    try
                    {
                        _dictionaryWalletTransactionIndex.Add(blockHeight, 1);
                    }
                    catch
                    {
                        // Check again after to have use the semaphore wait.
                        if (!_dictionaryWalletTransactionIndex.ContainsKey(blockHeight))
                        {
                            result = false;
                        }
                    }
                }
                else
                    _dictionaryWalletTransactionIndex[blockHeight]++;

                /*
                if (result)
                {
                    if (!_dictionaryWalletTransactionIndex[blockHeight].Contains(transactionHash))
                    {
                        if (!_dictionaryWalletTransactionIndex[blockHeight].Add(transactionHash))
                        {
                            result = false;
                        }
                    }
                }*/

                if (semaphoreUsed)
                {
                    _semaphoreAccessWalletTxIndex.Release();
                    semaphoreUsed = false;
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreAccessWalletTxIndex.Release();

            }
            return result;
        }


        /// <summary>
        /// RemoveFromCache a transaction hash with the block index.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> RemoveWalletTransactionHash(string transactionHash, long blockHeight, CancellationTokenSource cancellation)
        {
            bool result = false;
            bool semaphoreUsed = false;

            try
            {
                await _semaphoreAccessWalletTxIndex.WaitAsync(cancellation.Token);
                semaphoreUsed = true;

                if (_dictionaryWalletTransactionIndex.ContainsKey(blockHeight))
                {
                    _dictionaryWalletTransactionIndex[blockHeight]--;

                    if (_dictionaryWalletTransactionIndex[blockHeight] <= 0)
                        _dictionaryWalletTransactionIndex.Remove(blockHeight);

                    result = true;
                }

                _semaphoreAccessWalletTxIndex.Release();
                semaphoreUsed = false;
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreAccessWalletTxIndex.Release();
            }

            return result;
        }


        #endregion

        #region Manage wallet mem pool transaction indexed.

        /// <summary>
        /// Insert a mem pool transaction hash linked to the wallet address.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public async Task<bool> InsertWalletMemPoolTransactionIndexAsync(string transactionHash, long blockHeight, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;
            bool result = false;

            try
            {
                await _semaphoreAccessWalletMemPoolTxIndex.WaitAsync(cancellation.Token);
                semaphoreUsed = true;

                if (!_dictionaryWalletMemPoolTransactionIndex.ContainsKey(transactionHash))
                {
                    try
                    {
                        _dictionaryWalletMemPoolTransactionIndex.Add(transactionHash, blockHeight);
                        result = true;
                    }
                    catch
                    {
                        result = false;
                    }
                }
            }
            finally
            {
                if(semaphoreUsed)
                    _semaphoreAccessWalletMemPoolTxIndex.Release();
            }

            return result;
        }

        /// <summary>
        /// Remove a mem pool transaction linked to the wallet address.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <returns></returns>
        public async Task<bool> RemoveWalletMemPoolTransactionIndexAsync(string transactionHash, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;
            bool result = false;

            try
            {
                await _semaphoreAccessWalletMemPoolTxIndex.WaitAsync(cancellation.Token);
                semaphoreUsed = true;

                if (_dictionaryWalletMemPoolTransactionIndex.ContainsKey(transactionHash))
                    result = _dictionaryWalletMemPoolTransactionIndex.Remove(transactionHash);

            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreAccessWalletMemPoolTxIndex.Release();
            }

            return result;
        }

        /// <summary>
        /// Retrieve back all mem pool transaction hash linked to the wallet address.
        /// </summary>
        /// <returns></returns>
        public List<string> GetMemPoolTransactionIndexedList()
        {
            return new List<string>(_dictionaryWalletMemPoolTransactionIndex.OrderBy(x => x.Value).OfType<string>());
        }

        /// <summary>
        /// Count the amount of mem pool transaction linked to the wallet address.
        /// </summary>
        /// <returns></returns>
        public int GetCountWalletMemPoolTransaction()
        {
            return _dictionaryWalletMemPoolTransactionIndex.Count;
        }

        #endregion

        #region Manage Wallet Checkpoint Object.

        /// <summary>
        /// Get last block height of the last wallet balance checkpoint.
        /// </summary>
        /// <returns></returns>
        public long GetLastBlockHeightCheckPoint()
        {
            if (_blockchainWalletBalanceCheckpointObject.Count > 0)
            {
                return _blockchainWalletBalanceCheckpointObject.Keys.Last();
            }
            return 0;
        }

        /// <summary>
        /// Check if the block height is contained on the list of wallet balance checkpoint.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public bool ContainsBlockHeightCheckpoint(long blockHeight)
        {
            return _blockchainWalletBalanceCheckpointObject.ContainsKey(blockHeight);
        }


        /// <summary>
        /// Get wallet total tx.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public long GetWalletTotalTxCheckpoint(long blockHeight)
        {
            long totalTx = 0;
            if (_blockchainWalletBalanceCheckpointObject.ContainsKey(blockHeight))
            {
                foreach (var blockHeightKey in _blockchainWalletBalanceCheckpointObject.Keys.ToArray())
                {
                    totalTx += _blockchainWalletBalanceCheckpointObject[blockHeightKey].TotalTx;
                }
            }
            return totalTx;
        }

        /// <summary>
        /// Get the wallet balance of a checkpoint from a specific block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public BigInteger GetWalletBalanceCheckpoint(long blockHeight)
        {
            if (_blockchainWalletBalanceCheckpointObject.ContainsKey(blockHeight))
            {
                return _blockchainWalletBalanceCheckpointObject[blockHeight].LastWalletBalance;
            }
            return 0;
        }

        /// <summary>
        /// Add/Update wallet balance checkpoint.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="walletBalance"></param>
        /// <param name="walletPendingBalance"></param>
        /// <param name="totalTx"></param>
        public void InsertWalletBalanceCheckpoint(long blockHeight, BigInteger walletBalance, BigInteger walletPendingBalance, int totalTx)
        {

            if (!_blockchainWalletBalanceCheckpointObject.ContainsKey(blockHeight))
            {
                if (blockHeight > GetLastBlockHeightCheckPoint())
                {

                    try
                    {
                        _blockchainWalletBalanceCheckpointObject.Add(blockHeight, new ClassBlockchainWalletBalanceCheckpointObject()
                        {
                            BlockHeight = blockHeight,
                            LastWalletBalance = walletBalance,
                            LastWalletPendingBalance = walletPendingBalance,
                            TotalTx = totalTx
                        });
                        ClassBlockchainDatabase.InsertCheckpoint(ClassCheckpointEnumType.WALLET_CHECKPOINT, blockHeight, _walletAddress, walletBalance, walletPendingBalance);

#if DEBUG
                        Debug.WriteLine("Update wallet address: " + _walletAddress + " checkpoint. Block Height: " + blockHeight + " | New Balance: " + (walletBalance / BlockchainSetting.CoinDecimal) + " | New Pending Balance: " + (walletPendingBalance / BlockchainSetting.CoinDecimal));
#endif
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            }
        }

        /// <summary>
        /// Get the amount of wallet balance checkpoint stored.
        /// </summary>
        /// <returns></returns>
        public int GetCountWalletBalanceCheckpoint()
        {
            return _blockchainWalletBalanceCheckpointObject.Count;
        }

        public void ClearWalletBalanceCheckpoint()
        {
            _blockchainWalletBalanceCheckpointObject.Clear();
        }

        /// <summary>
        /// Return every wallet balance block height checkpoint.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<long> GetListWalletBalanceBlockHeightCheckPoint()
        {
            foreach (var blockHeight in _blockchainWalletBalanceCheckpointObject.Keys)
            {
                yield return blockHeight;
            }
        }

        #endregion
    }
}
