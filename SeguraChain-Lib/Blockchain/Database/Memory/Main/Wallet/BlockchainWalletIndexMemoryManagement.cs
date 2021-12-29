using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Wallet.Object;
using SeguraChain_Lib.Blockchain.Wallet.Object.Blockchain;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Main.Wallet
{
    /// <summary>
    /// Store wallet address and checkpoints.
    /// </summary>
    public class BlockchainWalletIndexMemoryManagement
    {
        /// <summary>
        /// Wallet index files.
        /// </summary>
        private const string WalletIndexFilename = "wallet";
        private const string WalletIndexFileExtension = ".index";

        /// <summary>
        /// Wallet index file data content format.
        /// </summary>
        private const string WalletIndexBegin = ">WALLET-INDEX-BEGIN=";
        private const string WalletIndexBeginStringClose = "~";
        private const string WalletIndexEnd = "WALLET-INDEX-END<";
        private const string WalletIndexCheckpointLineDataSeperator = ";";
        private const string WalletCheckpointDataSeperator = "|";

        /// <summary>
        /// Store the range of amount of wallet address index files.
        /// </summary>
        private BigInteger _walletAddressMaxPossibilities = BigInteger.Pow(2, 512);

        /// <summary>
        /// Database.
        /// </summary>
        private ConcurrentDictionary<string, BlockchainWalletMemoryIndexObject> _dictionaryBlockchainWalletIndexDataObjectMemory;

        /// <summary>
        /// Settings.
        /// </summary>
        private ClassBlockchainDatabaseSetting _blockchainDatabaseSetting;

        /// <summary>
        /// Multithreading access.
        /// </summary>
        private SemaphoreSlim _semaphoreBlockchainWalletIndexDataAccess;

        /// <summary>
        /// Store the last memory usage.
        /// </summary>
        public long LastMemoryUsage { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="blockchainDatabaseSetting"></param>
        public BlockchainWalletIndexMemoryManagement(ClassBlockchainDatabaseSetting blockchainDatabaseSetting)
        {
            _blockchainDatabaseSetting = blockchainDatabaseSetting;
         
            _dictionaryBlockchainWalletIndexDataObjectMemory = new ConcurrentDictionary<string, BlockchainWalletMemoryIndexObject>();
            _semaphoreBlockchainWalletIndexDataAccess = new SemaphoreSlim(1, 1);

            if (!Directory.Exists(_blockchainDatabaseSetting.GetBlockchainWalletIndexCacheDirectoryPath))
                Directory.CreateDirectory(_blockchainDatabaseSetting.GetBlockchainWalletIndexCacheDirectoryPath);
        }

        #region Original Dictionary functions.
        

        public BlockchainWalletMemoryObject this[string walletAddress, CancellationTokenSource cancellation]
        {
            get
            {
                string walletMemoryFileIndex = GetWalletIndexDataCacheFilename(walletAddress);
                if (!_dictionaryBlockchainWalletIndexDataObjectMemory.ContainsKey(walletMemoryFileIndex))
                {
                    if (RetrieveWalletMemoryIndex(_blockchainDatabaseSetting.GetBlockchainWalletIndexCacheDirectoryPath + walletMemoryFileIndex, walletAddress, cancellation))
                    {
                        if (_dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.ContainsKey(walletAddress))
                            return _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject[walletAddress];

                    }
                    else
                    {
                        if (!_dictionaryBlockchainWalletIndexDataObjectMemory.ContainsKey(walletMemoryFileIndex))
                            _dictionaryBlockchainWalletIndexDataObjectMemory.TryAdd(walletMemoryFileIndex, new BlockchainWalletMemoryIndexObject());

                        _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.Add(walletAddress, new BlockchainWalletMemoryObject());
                        return _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject[walletAddress];
                    }
                }
                else
                {
                    if (_dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.ContainsKey(walletAddress))
                        return _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject[walletAddress];
                    else
                    {
                        _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.Add(walletAddress, new BlockchainWalletMemoryObject());
                        return _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject[walletAddress];
                    }
                }

                return new BlockchainWalletMemoryObject();
            }
        }

        public bool ContainsKey(string walletAddress, CancellationTokenSource cancellation, out BlockchainWalletMemoryObject blockchainWalletMemory)
        {
            blockchainWalletMemory = null; // Default;

            string walletMemoryFileIndex = GetWalletIndexDataCacheFilename(walletAddress);
            if (!_dictionaryBlockchainWalletIndexDataObjectMemory.ContainsKey(walletMemoryFileIndex))
            {
                if (RetrieveWalletMemoryIndex(_blockchainDatabaseSetting.GetBlockchainWalletIndexCacheDirectoryPath + walletMemoryFileIndex, walletAddress, cancellation))
                {
                    blockchainWalletMemory = _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject[walletAddress];
                    return true;
                }
            }
            else
            {
                if (_dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.ContainsKey(walletAddress))
                {
                    blockchainWalletMemory = _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject[walletAddress];
                    return true;
                }
                else
                {
                    if (RetrieveWalletMemoryIndex(_blockchainDatabaseSetting.GetBlockchainWalletIndexCacheDirectoryPath + walletMemoryFileIndex, walletAddress, cancellation))
                    {
                        blockchainWalletMemory = _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject[walletAddress];
                        return true;
                    }
                }
            }

            return false;
        }

        public void Clear()
        {
            foreach(var walletMemoryFileIndex in _dictionaryBlockchainWalletIndexDataObjectMemory.Keys)
                _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.Clear();

            _dictionaryBlockchainWalletIndexDataObjectMemory.Clear();
        }

        #endregion

        #region Add/Update wallet memory object.

        public bool TryAdd(string walletAddress, CancellationTokenSource cancellation)
        {
            string walletMemoryFileIndex = GetWalletIndexDataCacheFilename(walletAddress);

            if (!_dictionaryBlockchainWalletIndexDataObjectMemory.ContainsKey(walletMemoryFileIndex))
            {
                if (_dictionaryBlockchainWalletIndexDataObjectMemory.TryAdd(walletMemoryFileIndex, new BlockchainWalletMemoryIndexObject()))
                {
                    if (!RetrieveWalletMemoryIndex(_blockchainDatabaseSetting.GetBlockchainWalletIndexCacheDirectoryPath + walletMemoryFileIndex, walletAddress, cancellation))
                    {
                        _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.Add(walletAddress, new BlockchainWalletMemoryObject());
                        return true;
                    }
                }
            }
            else
            {
                if (!_dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.ContainsKey(walletAddress))
                {
                    if (!RetrieveWalletMemoryIndex(_blockchainDatabaseSetting.GetBlockchainWalletIndexCacheDirectoryPath + walletMemoryFileIndex, walletAddress, cancellation))
                    {
                        _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.Add(walletAddress, new BlockchainWalletMemoryObject());
                        return true;
                    }
                }
                else return true;
            }
            return false;
        }

        /// <summary>
        /// Add or update wallet memory object.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="blockchainWalletMemoryObject"></param>
        /// <param name="cancellation"></param>
        public void AddOrUpdateWalletMemoryObject(string walletAddress, BlockchainWalletMemoryObject blockchainWalletMemoryObject, CancellationTokenSource cancellation)
        {
            string walletMemoryFileIndex = GetWalletIndexDataCacheFilename(walletAddress);
            if (!_dictionaryBlockchainWalletIndexDataObjectMemory.ContainsKey(walletMemoryFileIndex))
            {
                if (!RetrieveWalletMemoryIndex(_blockchainDatabaseSetting.GetBlockchainWalletIndexCacheDirectoryPath + walletMemoryFileIndex, null, cancellation))
                {
                    if (InsertNewWalletFileIndex(walletMemoryFileIndex))
                        _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.Add(walletAddress, blockchainWalletMemoryObject);
                }
                else
                    InsertUpdateWalletMemoryObject(walletMemoryFileIndex, walletAddress, blockchainWalletMemoryObject);
            }
            else
                InsertUpdateWalletMemoryObject(walletMemoryFileIndex, walletAddress, blockchainWalletMemoryObject);
        }

        /// <summary>
        /// Insert a wallet file index.
        /// </summary>
        /// <param name="walletMemoryFileIndex"></param>
        /// <returns></returns>
        private bool InsertNewWalletFileIndex(string walletMemoryFileIndex)
        {
            return _dictionaryBlockchainWalletIndexDataObjectMemory.TryAdd(walletMemoryFileIndex, new BlockchainWalletMemoryIndexObject());
        }

        /// <summary>
        /// Insert or update wallet memory data.
        /// </summary>
        /// <param name="walletMemoryFileIndex"></param>
        /// <param name="walletAddress"></param>
        /// <param name="blockchainWalletMemoryObject"></param>
        private void InsertUpdateWalletMemoryObject(string walletMemoryFileIndex, string walletAddress, BlockchainWalletMemoryObject blockchainWalletMemoryObject)
        {
            if (!_dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.ContainsKey(walletAddress))
                _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.Add(walletAddress, blockchainWalletMemoryObject);
            else
                _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject[walletAddress] = blockchainWalletMemoryObject;
        }

        public IEnumerable<Tuple<string, BlockchainWalletMemoryObject>> RetrieveAllWalletIndexCached(bool delete, CancellationTokenSource cancellation)
        {

            foreach (string walletIndexCacheFile in Directory.GetFiles(_blockchainDatabaseSetting.GetBlockchainWalletIndexCacheDirectoryPath))
            {
                if (RetrieveWalletMemoryIndex(walletIndexCacheFile, null, cancellation))
                {
                    foreach (var blockchainWalletMemoryPair in _dictionaryBlockchainWalletIndexDataObjectMemory[walletIndexCacheFile].DictionaryBlockchainWalletMemoryObject)
                        yield return new Tuple<string, BlockchainWalletMemoryObject>(blockchainWalletMemoryPair.Key, blockchainWalletMemoryPair.Value);

                    if (delete)
                    {
                        _dictionaryBlockchainWalletIndexDataObjectMemory[walletIndexCacheFile].DictionaryBlockchainWalletMemoryObject.Clear();
                        _dictionaryBlockchainWalletIndexDataObjectMemory.TryRemove(walletIndexCacheFile, out _);
                    }
                }
            }
            
        }

        #endregion

        #region Load/Save wallet memory object.

        /// <summary>
        /// Retrive a wallet memory index data from a file.
        /// </summary>
        /// <param name="walletMemoryFileIndex"></param>
        /// <param name="walletAddressTarget"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private bool RetrieveWalletMemoryIndex(string walletMemoryFileIndex, string walletAddressTarget, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;

            try
            {
                _semaphoreBlockchainWalletIndexDataAccess.Wait(cancellation.Token);
                semaphoreUsed = true;

                if (!File.Exists(walletMemoryFileIndex))
                    return false;

                bool search = walletAddressTarget != null;

                using (StreamReader reader = new StreamReader(walletMemoryFileIndex))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();

                        string walletAddress = null;

                        if (line.StartsWith(WalletIndexBegin))
                            walletAddress = line.GetStringBetweenTwoStrings(WalletIndexBegin, WalletIndexBeginStringClose);

                        bool success = true;

                        BlockchainWalletMemoryObject blockchainWalletMemoryObject = new BlockchainWalletMemoryObject();

                        using (DisposableList<string> listCheckpointCombinedSplitted = line.DisposableSplit(WalletIndexCheckpointLineDataSeperator))
                        {
                            if (listCheckpointCombinedSplitted.Count > 0)
                            {
                                foreach (string checkpointCombinedLineData in listCheckpointCombinedSplitted.GetAll)
                                {
                                    cancellation.Token.ThrowIfCancellationRequested();

                                    using (DisposableList<string> checkpointDataSplitted = checkpointCombinedLineData.DisposableSplit(WalletCheckpointDataSeperator))
                                    {
                                        if (!long.TryParse(checkpointDataSplitted[0], out long blockHeight))
                                        {
                                            success = false;
                                            break;
                                        }

                                        if (!BigInteger.TryParse(checkpointDataSplitted[1], out BigInteger lastWalletBalance))
                                        {
                                            success = false;
                                            break;
                                        }

                                        if (!BigInteger.TryParse(checkpointDataSplitted[2], out BigInteger lastWalletPendingBalance))
                                        {
                                            success = false;
                                            break;
                                        }

                                        if (!int.TryParse(checkpointDataSplitted[3], out int totalTx))
                                        {
                                            success = false;
                                            break;
                                        }

                                        if (!blockchainWalletMemoryObject.ListBlockchainWalletBalanceCheckpoints.ContainsKey(blockHeight))
                                        {
                                            blockchainWalletMemoryObject.ListBlockchainWalletBalanceCheckpoints.Add(blockHeight, new ClassBlockchainWalletBalanceCheckpointObject()
                                            {
                                                BlockHeight = blockHeight,
                                                LastWalletBalance = lastWalletBalance,
                                                LastWalletPendingBalance = lastWalletPendingBalance,
                                                TotalTx = totalTx
                                            });
                                        }

                                        if (!success)
                                            break;
                                    }
                                }
                            }
                        }

                        if (!walletAddress.IsNullOrEmpty(out _))
                        {
                            if (search)
                            {
                                if (walletAddressTarget == walletAddress)
                                {
                                    if (!_dictionaryBlockchainWalletIndexDataObjectMemory.ContainsKey(walletMemoryFileIndex))
                                    {
                                        if (!_dictionaryBlockchainWalletIndexDataObjectMemory.TryAdd(walletMemoryFileIndex, new BlockchainWalletMemoryIndexObject()))
                                            return false;
                                    }

                                    if (!_dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.ContainsKey(walletAddressTarget))
                                        _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.Add(walletAddressTarget, blockchainWalletMemoryObject);

                                    return true;
                                }
                            }
                            else
                            {
                                if (!_dictionaryBlockchainWalletIndexDataObjectMemory.ContainsKey(walletMemoryFileIndex))
                                {
                                    if (!_dictionaryBlockchainWalletIndexDataObjectMemory.TryAdd(walletMemoryFileIndex, new BlockchainWalletMemoryIndexObject()))
                                        return false;
                                }

                                if (!_dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.ContainsKey(walletAddress))
                                    _dictionaryBlockchainWalletIndexDataObjectMemory[walletMemoryFileIndex].DictionaryBlockchainWalletMemoryObject.Add(walletAddress, blockchainWalletMemoryObject);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreBlockchainWalletIndexDataAccess.Release();
            }

            return true;
        }

        /// <summary>
        /// Save all wallet memory index to files.
        /// </summary>
        /// <param name="delete"></param>
        /// <param name="cancellation"></param>
        public void SaveWalletMemoryIndex(bool delete, CancellationTokenSource cancellation)
        {
            foreach (string walletIndexCacheFile in Directory.GetFiles(_blockchainDatabaseSetting.GetBlockchainWalletIndexCacheDirectoryPath))
            {
                if (RetrieveWalletMemoryIndex(walletIndexCacheFile, null, cancellation))
                {
                    using (StreamWriter writer = new StreamWriter(walletIndexCacheFile))
                    {
                        foreach (var blockchainWalletMemoryPair in _dictionaryBlockchainWalletIndexDataObjectMemory[walletIndexCacheFile].DictionaryBlockchainWalletMemoryObject)
                        {
                            foreach (string walletDataLine in WalletMemoryObjectToWalletFileStringDataCache(blockchainWalletMemoryPair.Key, blockchainWalletMemoryPair.Value))
                                writer.WriteLine(walletDataLine);
                        }
                    }

                    if (delete)
                    {
                        _dictionaryBlockchainWalletIndexDataObjectMemory[walletIndexCacheFile].DictionaryBlockchainWalletMemoryObject.Clear();
                        _dictionaryBlockchainWalletIndexDataObjectMemory.TryRemove(walletIndexCacheFile, out _);
                    }
                }
            }
        }

        #endregion

        #region Wallet index indexing functions.

        /// <summary>
        /// Calculate the wallet address index file name.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <returns></returns>
        private string GetWalletIndexDataCacheFilename(string walletAddress)
        {
            BigInteger indexFile = new BigInteger(ClassBase58.DecodeWithCheckSum(walletAddress, true));

            if (indexFile.Sign < 0)
                indexFile *= -1;


            if (indexFile > 0)
            {
                double percent = (((double)indexFile / (double)_walletAddressMaxPossibilities) * 100d);

                if (percent > 0)
                {
                    double indexCalculated = ((_blockchainDatabaseSetting.BlockchainCacheSetting.GlobalCacheMaxWalletIndexPerFile * percent));

                    indexFile = (BigInteger)indexCalculated;
                }
                else
                    indexFile = 1;
            }

            return WalletIndexFilename + indexFile + WalletIndexFileExtension;
        }

        #endregion


        #region Wallet Index data format functions.

        /// <summary>
        /// Convert a wallet memory object data into a wallet file string data cache.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="blockchainWalletMemoryObject"></param>
        /// <returns></returns>
        private IEnumerable<string> WalletMemoryObjectToWalletFileStringDataCache(string walletAddress, BlockchainWalletMemoryObject blockchainWalletMemoryObject)
        {
            yield return WalletIndexBegin + walletAddress + WalletIndexBeginStringClose;

            if (blockchainWalletMemoryObject.ListBlockchainWalletBalanceCheckpoints.Count > 0)
            {
                int totalWritten = 0;

                string blockchainWalletCheckpoints = string.Empty;

                foreach (var checkpoint in blockchainWalletMemoryObject.ListBlockchainWalletBalanceCheckpoints)
                {
                    // Block height | last balance | last pending balance | total tx.
                    blockchainWalletCheckpoints += checkpoint.Key + WalletCheckpointDataSeperator + checkpoint.Value.LastWalletBalance + WalletCheckpointDataSeperator + checkpoint.Value.TotalTx + WalletCheckpointDataSeperator + checkpoint.Value.LastWalletPendingBalance + WalletIndexCheckpointLineDataSeperator;
                    totalWritten++;

                    if (totalWritten >= _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalCacheMaxWalletIndexCheckpointsPerLine)
                    {
                        yield return blockchainWalletCheckpoints;
                        totalWritten = 0;
                        blockchainWalletCheckpoints = string.Empty;
                    }
                }

                if (!blockchainWalletCheckpoints.IsNullOrEmpty(out _))
                {
                    yield return blockchainWalletCheckpoints;
                }
            }

            yield return WalletIndexEnd;
        }

        #endregion
    }
}
