using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LZ4;
using Newtonsoft.Json;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Function;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Checkpoint.Enum;
using SeguraChain_Lib.Blockchain.Checkpoint.Object;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Blockchain.Database.Function;
using SeguraChain_Lib.Blockchain.Database.Memory.Main;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Enum;
using SeguraChain_Lib.Blockchain.MemPool.Database;
using SeguraChain_Lib.Blockchain.Mining.Enum;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Database
{
    public class ClassBlockchainDatabase : ClassBlockchainDatabaseFunction
    {
        /// <summary>
        /// Blockchain Database.
        /// </summary>
        private static ClassBlockchainDatabaseFunction _blockchainDatabaseFunction;
        public static BlockchainMemoryManagement BlockchainMemoryManagement; // Contains Blocks and transactions.
        public static ConcurrentDictionary<ClassCheckpointEnumType, List<ClassCheckpointObject>> DictionaryCheckpointObjects;
        private static long _blockchainLoadTransactionCount;

        /// <summary>
        /// Encryptions key/IV data
        /// </summary>
        private static byte[] _blockchainDataStandardEncryptionKey;
        private static byte[] _blockchainDataStandardEncryptionKeyIv;

        /// <summary>
        /// Parallel save task.
        /// </summary>
        private static CancellationTokenSource _cancellationTokenStopBlockchain;

        /// <summary>
        /// Semaphore, block multithreading access on critical process.
        /// </summary>
        public static SemaphoreSlim SemaphoreUnlockBlock = new SemaphoreSlim(1, 1);
        private static SemaphoreSlim _semaphoreSaveBlockchain = new SemaphoreSlim(1, 1);

        #region Function to load/save blockchain data.

        /// <summary>
        /// Load blockchain database files.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> LoadBlockchainDatabase(ClassBlockchainDatabaseSetting blockchainDatabaseSetting, string encryptionDatabaseKey = null, bool resetBlockchain = false, bool fromWallet = false)
        {
            // Initialize main cancellation token.
            _cancellationTokenStopBlockchain = new CancellationTokenSource();
            _blockchainDatabaseFunction = new ClassBlockchainDatabaseFunction();

            #region Initialize static paths.

            if (blockchainDatabaseSetting.BlockchainCacheSetting.CacheName == CacheEnumName.IO_CACHE)
            {
                if (blockchainDatabaseSetting.BlockchainCacheSetting.CacheType == ClassBlockchainDatabaseCacheTypeEnum.CACHE_DISK)
                {
                    if (!Directory.Exists(blockchainDatabaseSetting.BlockchainCacheSetting.CacheDirectoryPath))
                    {
                        Directory.CreateDirectory(blockchainDatabaseSetting.BlockchainCacheSetting.CacheDirectoryPath);
                    }
                }
            }

            #endregion

            #region Initialize encryption key database.

            if (blockchainDatabaseSetting.DataSetting.EnableEncryptionDatabase)
            {
                if (encryptionDatabaseKey.IsNullOrEmpty(false, out _))
                {

                    ClassLog.WriteLine("The encryption key is empty, can't decrypt the database.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return false;

                }

                if (!ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringAscii(encryptionDatabaseKey), true, out _blockchainDataStandardEncryptionKey))
                {
                    ClassLog.WriteLine("Can't generate encryption key with custom key for decrypt blockchain database content.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return false;
                }
                _blockchainDataStandardEncryptionKeyIv = ClassAes.GenerateIv(_blockchainDataStandardEncryptionKey);

            }
            #endregion

            #region Initialize Blockchain Cache.

            BlockchainMemoryManagement = new BlockchainMemoryManagement(blockchainDatabaseSetting);

            if (!Directory.Exists(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryPath))
            {
                Directory.CreateDirectory(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryPath);

                ClassLog.WriteLine("Blockchain database not initialized. Initialized now.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            }

            if (blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                ClassLog.WriteLine("Load blockchain cache..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                if (await BlockchainMemoryManagement.LoadBlockchainCache())
                    ClassLog.WriteLine("Blockchain cache loaded successfully. Total blocks from cache loaded: " + BlockchainMemoryManagement.Count, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                else
                    ClassLog.WriteLine("Their is no blocks loaded from the cache.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            }

            #endregion

            #region Load blockchain database files.

  

            if (!Directory.Exists(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath))
                    Directory.CreateDirectory(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath);
            else
            {

                #region Load Block database file.

                
                foreach (ClassBlockObject blockObject in _blockchainDatabaseFunction.LoadBlockchainDatabaseEnumerable(blockchainDatabaseSetting, encryptionDatabaseKey, resetBlockchain, fromWallet))
                {
                    if (blockObject == null)
                        continue;

                    _blockchainLoadTransactionCount += blockObject.TotalTransaction;
                    if (blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
                    {
                        if (blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight)
                        {
                            if (await BlockchainMemoryManagement.BlockHeightIsCached(blockObject.BlockHeight, _cancellationTokenStopBlockchain))
                            {
                                ClassBlockObject blockInformationObject = await BlockchainMemoryManagement.GetBlockInformationDataStrategy(blockObject.BlockHeight, _cancellationTokenStopBlockchain);

                                if (blockInformationObject != null)
                                {
                                    if (!await BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObject, false, _cancellationTokenStopBlockchain))
                                        continue;
                                }
                                else
                                {
                                    if (!await BlockchainMemoryManagement.Add(blockObject.BlockHeight, blockObject, CacheBlockMemoryInsertEnumType.INSERT_IN_PERSISTENT_CACHE_OBJECT, _cancellationTokenStopBlockchain))
                                        continue;
                                }
                            }
                            else
                            {
                                if (!await BlockchainMemoryManagement.Add(blockObject.BlockHeight, blockObject, CacheBlockMemoryInsertEnumType.INSERT_IN_PERSISTENT_CACHE_OBJECT, _cancellationTokenStopBlockchain))
                                    continue;
                            }
                        }
                        else
                        {
                            if (!await BlockchainMemoryManagement.Add(blockObject.BlockHeight, blockObject, CacheBlockMemoryInsertEnumType.INSERT_IN_ACTIVE_MEMORY_OBJECT, _cancellationTokenStopBlockchain))
                                continue;
                        }
                    }
                    else
                    {
                        if (!await BlockchainMemoryManagement.Add(blockObject.BlockHeight, blockObject, CacheBlockMemoryInsertEnumType.INSERT_IN_ACTIVE_MEMORY_OBJECT, _cancellationTokenStopBlockchain))
                            continue;
                    }
                }


                ClassLog.WriteLine(BlockchainMemoryManagement.Count + " Block(s) loaded successfully from database file.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                ClassLog.WriteLine(_blockchainLoadTransactionCount + " transaction(s) loaded successfully from database file.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                #endregion

            }


            #endregion

            #region Load Blockchain checkpoint(s) database.

            DictionaryCheckpointObjects = new ConcurrentDictionary<ClassCheckpointEnumType, List<ClassCheckpointObject>>();

            if (File.Exists(blockchainDatabaseSetting.GetCheckpointDatabaseFilePath))
            {
                ClassLog.WriteLine("Load checkpoint(s) data file..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                if (LoadCheckpointData(blockchainDatabaseSetting))
                {
                    ClassLog.WriteLine("Total checkpoint(s) data loaded: " + DictionaryCheckpointObjects.Count + " sorting usefull checkpoint(s) data and remove old one.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                    if (DictionaryCheckpointObjects.ContainsKey(ClassCheckpointEnumType.WALLET_CHECKPOINT))
                    {
                        if (DictionaryCheckpointObjects[ClassCheckpointEnumType.WALLET_CHECKPOINT].Count > 0)
                        {
                            foreach (var walletCheckpointObject in DictionaryCheckpointObjects[ClassCheckpointEnumType.WALLET_CHECKPOINT].ToArray())
                            {
                                if (!walletCheckpointObject.PossibleWalletAddress.IsNullOrEmpty(false, out _))
                                {
                                    if (!BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.ContainsKey(walletCheckpointObject.PossibleWalletAddress, _cancellationTokenStopBlockchain, out _))
                                    {
                                        if (BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.TryAdd(walletCheckpointObject.PossibleWalletAddress, _cancellationTokenStopBlockchain))
                                        {
                                            var walletIndexData = BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject[walletCheckpointObject.PossibleWalletAddress, _cancellationTokenStopBlockchain];
                                            walletIndexData.InsertWalletBalanceCheckpoint(walletCheckpointObject.BlockHeight, walletCheckpointObject.PossibleValue, walletCheckpointObject.PossibleValue2, 0, walletCheckpointObject.PossibleWalletAddress);
                                            BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.AddOrUpdateWalletMemoryObject(walletCheckpointObject.PossibleWalletAddress, walletIndexData, _cancellationTokenStopBlockchain);
                                        }
                                    }
                                    else
                                    {
                                        var walletIndexData = BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject[walletCheckpointObject.PossibleWalletAddress, _cancellationTokenStopBlockchain];
                                        walletIndexData.InsertWalletBalanceCheckpoint(walletCheckpointObject.BlockHeight, walletCheckpointObject.PossibleValue, walletCheckpointObject.PossibleValue2, 0, walletCheckpointObject.PossibleWalletAddress);
                                        BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.AddOrUpdateWalletMemoryObject(walletCheckpointObject.PossibleWalletAddress, walletIndexData, _cancellationTokenStopBlockchain);
                                    }
                                }
                            }

                            ClassLog.WriteLine("Total wallet balance checkpoint(s) inserted: " + DictionaryCheckpointObjects[ClassCheckpointEnumType.WALLET_CHECKPOINT].Count, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                        }
                    }
                }
                else
                {
                    ClassLog.WriteLine("Load checkpoint(s) data file failed. No data available or the content is invalid.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                }
            }


            #endregion


            if (resetBlockchain)
            {
                #region Reset block confirmations if asked.

                ClassLog.WriteLine("Reset blockchain data. Please wait a moment..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                // Remove all blocks, expect the genesis block.
                using (DisposableList<long> listBlockHeight = BlockchainMemoryManagement.ListBlockHeight)
                {
                    foreach (long blockHeight in listBlockHeight.GetList)
                    {
                        if (!await BlockchainMemoryManagement.Remove(blockHeight, _cancellationTokenStopBlockchain))
                        {
                            ClassLog.WriteLine("Failed to reset the blockchain database. If it's not work after another attempt, it's suggested to do it manually.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                            return false;
                        }
                    }
                }

                // Clean up MemPool.
                ClassMemPoolDatabase.ResetMemPool();

                // Cleanup wallet balance checkpoints.
                DictionaryCheckpointObjects.Clear();

                // Clean up wallet transaction indexes.
                BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.Clear();

                ClassLog.WriteLine("Reset blockchain data done.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);


                #endregion
            }
            else
            {
                #region Check blockchain database.

                if (!await CheckBlockchainDatabase(blockchainDatabaseSetting))
                    return false;

                #endregion
            }

            #region Purge blockchain cache system.

            if (blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                if (!fromWallet)
                {
                    ClassLog.WriteLine("All data loaded, purge cache system..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    await BlockchainMemoryManagement.ForcePurgeCache(_cancellationTokenStopBlockchain);
                    ClassLog.WriteLine("Purge of the cache system done, retrieve most recent blocks into the active memory..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                }

                #region Retrieve block most recent blocks data to the active memory.

                long blockHeightRangeEnd = BlockchainMemoryManagement.GetLastBlockHeight;

                long blockHeightRangeStart = blockHeightRangeEnd - blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxBlockCountToKeepInMemory;

                if (blockHeightRangeStart < BlockchainSetting.GenesisBlockHeight)
                    blockHeightRangeStart = BlockchainSetting.GenesisBlockHeight;
                

                try
                {
                    await BlockchainMemoryManagement.RetrieveBlockFromBlockHeightRangeTargetFromMemoryDataCacheToActiveMemory(blockHeightRangeStart, blockHeightRangeEnd, _cancellationTokenStopBlockchain);

                    ClassLog.WriteLine("Most recent blocks from block height " + blockHeightRangeStart + " until the last block height " + blockHeightRangeEnd + " done propertly.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                }
                catch (Exception error)
                {
#if DEBUG
                    Debug.WriteLine("Error on trying to retrieve back most recent blocks data cached to the active memory. Exception: " + error.Message);
#endif
                    ClassLog.WriteLine("Error on trying to retrieve back most recent blocks data cached to the active memory. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                }

                #endregion
            }

            #endregion

            #region Enable Blockchain parallel tasks.

            // Enable task who manage the active memory.
            if (blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
                BlockchainMemoryManagement.StartTaskManageActiveMemory();

            #endregion

            #region Generate new block to mine if the latest block loaded is the genesis block.

            if (BlockchainMemoryManagement.Count > 0)
            {
                long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();

                ClassBlockObject lastBlockObject = BlockchainMemoryManagement[lastBlockHeight, null];
                if (lastBlockObject != null && lastBlockObject?.BlockStatus == ClassBlockEnumStatus.UNLOCKED && lastBlockHeight == BlockchainSetting.GenesisBlockHeight)
                {
                    long newBlockHeight = lastBlockHeight + 1;

                    if (await GenerateNewMiningBlockObject(lastBlockHeight, newBlockHeight, lastBlockObject.TimestampFound, lastBlockObject.BlockWalletAddressWinner, true, false, _cancellationTokenStopBlockchain))
                        ClassLog.WriteLine("Generate new block " + newBlockHeight + ". Previous block height: " + lastBlockHeight, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                }
            }

            #endregion

            #region Build network stats.

            ClassLog.WriteLine("Building blockchain network stats, please wait a momment..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            await ClassBlockchainStats.UpdateBlockchainNetworkStats(true, _cancellationTokenStopBlockchain);
            ClassLog.WriteLine("Done. Total tx(s) synced: " + ClassBlockchainStats.BlockchainNetworkStatsObject.TotalTransactions + " | Total block(s) synced: " + ClassBlockchainStats.BlockchainNetworkStatsObject.LastBlockHeight, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            #endregion

            ClassUtility.CleanGc();

            return true;
        }

        /// <summary>
        /// Function for save the whole blockchain data.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> SaveBlockchainDatabase(ClassBlockchainDatabaseSetting blockchainDatabaseSetting, bool fromTask = false, CancellationTokenSource cancellationTaskSaveBlockchain = null)
        {

            return await _semaphoreSaveBlockchain.TryWaitExecuteActionAsync(async () =>
            {
                if (!fromTask)
                    await BlockchainMemoryManagement.StopMemoryManagement();


                if (!Directory.Exists(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath))
                    Directory.CreateDirectory(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath);

                // Counter of data saved.
                long totalBlockSaved = 0;
                long totalTxSaved = 0;

                if (BlockchainMemoryManagement.Count > 0)
                {
                    long countBlock = BlockchainMemoryManagement.Count;
                    if (!fromTask)
                        ClassLog.WriteLine("Save " + countBlock + " block(s) file(s)..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                    UTF8Encoding utf8Encoding = new UTF8Encoding(true, false);

                    for (long i = 0; i < countBlock; i++)
                    {
                        if (cancellationTaskSaveBlockchain != null)
                        {
                            if (cancellationTaskSaveBlockchain.IsCancellationRequested)
                                break;
                        }

                        long blockHeight = i + 1;

                        string blockFileName = ClassBlockchainDatabaseDefaultSetting.BlockDatabaseFileName + blockHeight + ClassBlockchainDatabaseDefaultSetting.BlockDatabaseFileExtension;

                        if (File.Exists(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath + blockFileName))
                            File.Delete(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath + blockFileName);

                        File.Create(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath + blockFileName).Close();

                        // Initialize Stream Block Writer.
                        using (StreamWriter writerBlock = blockchainDatabaseSetting.DataSetting.EnableCompressDatabase ?
                        new StreamWriter(new LZ4Stream(new FileStream(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath + blockFileName, FileMode.Truncate), LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression, ClassBlockchainDatabaseDefaultSetting.Lz4CompressionBlockSize)) :
                        new StreamWriter(new FileStream(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryBlockPath + blockFileName, FileMode.Truncate)) { AutoFlush = true })
                        {
                            ClassBlockObject blockObject = await BlockchainMemoryManagement.GetBlockDataStrategy(blockHeight, !fromTask, true, _cancellationTokenStopBlockchain);

                            while (blockObject == null || blockObject?.BlockTransactions == null || blockObject?.BlockTransactions.Count != blockObject?.TotalTransaction)
                            {
                                if (cancellationTaskSaveBlockchain != null)
                                {
                                    if (cancellationTaskSaveBlockchain.IsCancellationRequested)
                                        break;
                                }

                                blockObject = await BlockchainMemoryManagement.GetBlockDataStrategy(blockHeight, !fromTask, true, cancellationTaskSaveBlockchain != null ? cancellationTaskSaveBlockchain : _cancellationTokenStopBlockchain);
                            }

                            foreach (string blockDataLine in ClassBlockUtility.BlockObjectToStringBlockData(blockObject, blockchainDatabaseSetting.DataSetting.DataFormatIsJson))
                            {
                                if (cancellationTaskSaveBlockchain != null)
                                {
                                    if (cancellationTaskSaveBlockchain.IsCancellationRequested)
                                        break;
                                }

                                byte[] blockDataLineCopy = utf8Encoding.GetBytes(blockDataLine);

                                if (blockchainDatabaseSetting.DataSetting.EnableEncryptionDatabase)
                                    ClassAes.EncryptionProcess(blockDataLineCopy, _blockchainDataStandardEncryptionKey, _blockchainDataStandardEncryptionKeyIv, out blockDataLineCopy);

                                await writerBlock.WriteLineAsync(utf8Encoding.GetString(blockDataLineCopy));

                                // Clean up.
                                Array.Resize(ref blockDataLineCopy, 0);

                            }

                            totalTxSaved += blockObject.BlockTransactions.Count;
                            totalBlockSaved++;

                            await writerBlock.FlushAsync();

                        }

                        await Task.Delay(1);
                    }

                    ClassUtility.CleanGc();

                }

                if (!fromTask)
                {
                    ClassLog.WriteLine(totalBlockSaved + " block(s) successfully saved into database file.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    ClassLog.WriteLine(totalTxSaved + " transaction(s) successfully saved into database file.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                }
            }, _cancellationTokenStopBlockchain);
        }

        /// <summary>
        /// Function for close the blockchain database.
        /// </summary>
        public static async Task CloseBlockchainDatabase(ClassBlockchainDatabaseSetting blockchainDatabaseSetting)
        {
            try
            {
                if (!_cancellationTokenStopBlockchain.IsCancellationRequested)
                    _cancellationTokenStopBlockchain.Cancel();
            }
            catch
            {
                // Ignored, try to force parallel tasks in waiting.
            }

            if (blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                ClassLog.WriteLine("Close and clear blockchain database cache..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                await BlockchainMemoryManagement.CloseCache();
                ClassLog.WriteLine("Blockchain database cache cleaned and closed.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            }


            ClassLog.WriteLine("Save checkpoint(s)..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);


            BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.SaveWalletMemoryIndex(false, new CancellationTokenSource());

            foreach (var tupleWalletIndexCached in BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.RetrieveAllWalletIndexCached(true, new CancellationTokenSource()))
            {
                foreach (var listCheckpoint in tupleWalletIndexCached.Item2.ListBlockchainWalletBalanceCheckpoints)
                    InsertCheckpoint(ClassCheckpointEnumType.WALLET_CHECKPOINT, listCheckpoint.Key, tupleWalletIndexCached.Item1, listCheckpoint.Value.LastWalletBalance, listCheckpoint.Value.LastWalletPendingBalance);
            }

            SaveCheckpointData(blockchainDatabaseSetting);

            BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.Clear();

            DictionaryCheckpointObjects.Clear();

            ClassUtility.CleanGc();
        }

        #region Functions dedicated to checkpoint(s) data.

        /// <summary>
        /// Load checkpoint(s) data.
        /// </summary>
        /// <returns></returns>
        private static bool LoadCheckpointData(ClassBlockchainDatabaseSetting blockchainDatabaseSetting)
        {
            if (!File.Exists(blockchainDatabaseSetting.GetCheckpointDatabaseFilePath))
                File.Create(blockchainDatabaseSetting.GetCheckpointDatabaseFilePath).Close();

            using (FileStream fileStream = new FileStream(blockchainDatabaseSetting.GetCheckpointDatabaseFilePath, FileMode.Open))
            {
                using (StreamReader reader = blockchainDatabaseSetting.DataSetting.EnableCompressDatabase ? new StreamReader(new LZ4Stream(fileStream, LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression)) : new StreamReader(fileStream))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (blockchainDatabaseSetting.DataSetting.EnableEncryptionDatabase)
                        {
                            if (ClassUtility.CheckBase64String(line))
                            {
                                if (ClassAes.DecryptionProcess(Convert.FromBase64String(line), _blockchainDataStandardEncryptionKey, _blockchainDataStandardEncryptionKeyIv, out byte[] result))
                                {
                                    if (result != null)
                                        line = result.GetStringFromByteArrayUtf8();
                                    else
                                        return false;
                                }
                                else
                                    return false;
                            }
                            else
                                return false;
                        }

                        if (!line.IsNullOrEmpty(false, out _))
                        {
                            if (ClassUtility.TryDeserialize(line, out ClassCheckpointObject checkpointObject))
                            {
                                if (checkpointObject != null)
                                {
                                    bool result = DictionaryCheckpointObjects.ContainsKey(checkpointObject.CheckpointType);

                                    if (!result)
                                    {
                                        if (DictionaryCheckpointObjects.TryAdd(checkpointObject.CheckpointType, new List<ClassCheckpointObject>()))
                                            DictionaryCheckpointObjects[checkpointObject.CheckpointType].Add(checkpointObject);
                                    }
                                }
                            }
                        }
                    }

                }
            }

            return true;
        }

        /// <summary>
        /// Save checkpoint(s) data.
        /// </summary>
        /// <returns></returns>
        private static void SaveCheckpointData(ClassBlockchainDatabaseSetting blockchainDatabaseSetting)
        {
            try
            {
                if (!File.Exists(blockchainDatabaseSetting.GetCheckpointDatabaseFilePath))
                    File.Create(blockchainDatabaseSetting.GetCheckpointDatabaseFilePath).Close();

                long totalCheckpointSaved = 0;

                if (DictionaryCheckpointObjects == null)
                    DictionaryCheckpointObjects = new ConcurrentDictionary<ClassCheckpointEnumType, List<ClassCheckpointObject>>();

                if (DictionaryCheckpointObjects.Count > 0)
                {
                    // Initialize Stream Checkpoint Writer.
                    using (StreamWriter writerCheckpoint = blockchainDatabaseSetting.DataSetting.EnableCompressDatabase ?
                        new StreamWriter(new LZ4Stream(new FileStream(blockchainDatabaseSetting.GetCheckpointDatabaseFilePath, FileMode.Truncate), LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression, ClassBlockchainDatabaseDefaultSetting.Lz4CompressionBlockSize)) :
                        new StreamWriter(new FileStream(blockchainDatabaseSetting.GetCheckpointDatabaseFilePath, FileMode.Truncate)))
                    {
                        foreach (var checkpointKey in DictionaryCheckpointObjects.Keys)
                        {
                            if (DictionaryCheckpointObjects[checkpointKey].Count > 0)
                            {
                                foreach (var checkpointObject in DictionaryCheckpointObjects[checkpointKey])
                                {
                                    string checkpointLineToWrite = ClassUtility.SerializeData(checkpointObject);

                                    if (blockchainDatabaseSetting.DataSetting.EnableEncryptionDatabase)
                                    {
                                        if (ClassAes.EncryptionProcess(ClassUtility.GetByteArrayFromStringAscii(checkpointLineToWrite), _blockchainDataStandardEncryptionKey, _blockchainDataStandardEncryptionKeyIv, out byte[] result))
                                        {
                                            if (result == null)
                                                continue;

                                            checkpointLineToWrite = Convert.ToBase64String(result);
                                        }
                                        else
                                            continue;
                                    }

                                    writerCheckpoint.WriteLine(checkpointLineToWrite);

                                    // Clean up.
                                    checkpointLineToWrite.Clear();

                                    totalCheckpointSaved++;
                                }
                            }
                        }

                    }
                }

                ClassLog.WriteLine("Total checkpoint(s) saved: " + totalCheckpointSaved + " sucessfully.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            }
            catch (Exception error)
            {
                ClassLog.WriteLine("Error on save checkpoint(s). Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            }
        }

        /// <summary>
        /// Insert a checkpoint.
        /// </summary>
        /// <param name="checkpointType"></param>
        /// <param name="blockHeight"></param>
        /// <param name="walletAddress"></param>
        /// <param name="value"></param>
        /// <param name="value2"></param>
        public static void InsertCheckpoint(ClassCheckpointEnumType checkpointType, long blockHeight, string walletAddress, BigInteger value, BigInteger value2)
        {
            bool result = true;
            if (!DictionaryCheckpointObjects.ContainsKey(checkpointType))
            {
                if (!DictionaryCheckpointObjects.TryAdd(checkpointType, new List<ClassCheckpointObject>()))
                    result = false;
            }

            if (result)
            {

                DictionaryCheckpointObjects[checkpointType].Add(new ClassCheckpointObject()
                {
                    CheckpointType = checkpointType,
                    BlockHeight = blockHeight,
                    PossibleWalletAddress = walletAddress,
                    PossibleValue = value,
                    PossibleValue2 = value2
                });

                ClassLog.WriteLine("Insert checkpoint type: " + checkpointType + " | Block Height: " + blockHeight + " | Wallet Address: " + walletAddress + " | Amount: " + value + " | Pending: " + value2, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
            }
        }

        /// <summary>
        /// Return the last block transaction checkpoint.
        /// </summary>
        /// <returns></returns>
        public static long GetLastBlockHeightTransactionCheckpoint()
        {
            if (DictionaryCheckpointObjects.ContainsKey(ClassCheckpointEnumType.BLOCK_HEIGHT_TRANSACTION_CHECKPOINT))
            {
                if (DictionaryCheckpointObjects[ClassCheckpointEnumType.BLOCK_HEIGHT_TRANSACTION_CHECKPOINT].Count > 0)
                    return DictionaryCheckpointObjects[ClassCheckpointEnumType.BLOCK_HEIGHT_TRANSACTION_CHECKPOINT].Max(x => x.BlockHeight);
            }

            return 0;
        }

        #endregion

        #endregion

        #region Functions to manage blockchain transactions data.

        /// <summary>
        /// Attempt to insert a block transaction into a block object target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="transactionObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static ClassBlockTransactionInsertEnumStatus InsertBlockTransaction(long blockHeight, ClassTransactionObject transactionObject, CancellationTokenSource cancellation)
        {

            ClassBlockTransactionInsertEnumStatus insertResult = ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INVALID;

            try
            {
                if (BlockchainMemoryManagement.ContainsKey(blockHeight))
                {
                    if (BlockchainMemoryManagement[blockHeight, cancellation].BlockTransactions.Count + 1 <= BlockchainSetting.MaxTransactionPerBlock || transactionObject.TransactionType == ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION || transactionObject.TransactionType == ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                    {
                        string blockTransactionHash = transactionObject.TransactionHash;

                        if (!BlockchainMemoryManagement[blockHeight, cancellation].BlockTransactions.ContainsKey(blockTransactionHash))
                        {

                            int countInsert = BlockchainMemoryManagement[blockHeight, cancellation].BlockTransactions.Count;
                            try
                            {
                                long blockHeightInsert = blockHeight;

                                if (transactionObject.BlockHeightTransaction > blockHeightInsert)
                                    blockHeightInsert = transactionObject.BlockHeightTransaction;


                                BlockchainMemoryManagement[blockHeight, cancellation].BlockTransactions.Add(blockTransactionHash, new ClassBlockTransaction(countInsert, transactionObject)
                                {
                                    IndexInsert = countInsert,
                                    TransactionObject = transactionObject,
                                    TransactionBlockHeightInsert = blockHeightInsert,
                                    TransactionBlockHeightTarget = transactionObject.BlockHeightTransactionConfirmationTarget,
                                    TransactionStatus = true
                                });
                                BlockchainMemoryManagement[blockHeight, cancellation].BlockTransactions[blockTransactionHash].IndexInsert = countInsert;
                                InsertWalletBlockTransactionHash(transactionObject, cancellation);
                                insertResult = ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INSERTED;
                            }
                            catch
                            {
                                insertResult = ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_HASH_ALREADY_EXIST;

                            }
                        }
                        else
                        {
                            insertResult = ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_HASH_ALREADY_EXIST;
                        }
                    }
                    else
                    {
                        insertResult = ClassBlockTransactionInsertEnumStatus.MAX_BLOCK_TRANSACTION_PER_BLOCK_HEIGHT_TARGET_REACH;
                    }
                }
                else
                {
                    insertResult = ClassBlockTransactionInsertEnumStatus.BLOCK_HEIGHT_NOT_EXIST;
                }

            }
            catch (Exception error)
            {
                ClassLog.WriteLine("Error on insert a transaction to the block height " + blockHeight + ". Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
            }

            return insertResult;
        }

        /// <summary>
        /// Insert the index of a block transaction inside wallet dictionnary's index.
        /// </summary>
        /// <param name="transactionObject"></param>
        /// <param name="cancellation"></param>
        public static void InsertWalletBlockTransactionHash(ClassTransactionObject transactionObject, CancellationTokenSource cancellation)
        {
            /*
            // Allow on types of tx who provide a wallet address has sender.
            if (transactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION && transactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION)
            {
                if (!transactionObject.WalletAddressSender.IsNullOrEmpty(false, out _))
                {
                    if (!BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.ContainsKey(transactionObject.WalletAddressSender, cancellation, out _))
                    {
                        BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.TryAdd(transactionObject.WalletAddressSender, cancellation);
                    }
                }
            }

            if (!transactionObject.WalletAddressReceiver.IsNullOrEmpty(false, out _))
            {
                if (!BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.ContainsKey(transactionObject.WalletAddressReceiver, cancellation, out _))
                {
                    BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.TryAdd(transactionObject.WalletAddressReceiver, cancellation);
                }
            }*/
        }


        /// <summary>
        /// Check if a transaction hash already exist on blocks.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeightTransaction"></param>
        /// <param name="useSemaphore"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> CheckIfTransactionHashAlreadyExist(string transactionHash, long blockHeightTransaction, CancellationTokenSource cancellation)
        {
            return await BlockchainMemoryManagement.CheckIfTransactionHashAlreadyExist(transactionHash, blockHeightTransaction, cancellation);
        }

        #endregion

        #region Functions dedicated to Mining Block unlock.

        /// <summary>
        /// Check if the block height contains block reward.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> CheckBlockHeightContainsBlockReward(long blockHeight, CancellationTokenSource cancellation)
        {
            return await BlockchainMemoryManagement.CheckBlockHeightContainsBlockReward(blockHeight, null, cancellation);
        }

        /// <summary>
        /// Unlock the current block.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="miningPowShareObject"></param>
        /// <param name="enableBroadcast"></param>
        /// <param name="apiServerIp"></param>
        /// <param name="apiServerOpenNatIp"></param>
        /// <param name="fromSync"></param>
        /// <param name="peerNetworkSetting"></param>
        /// <param name="unlockTest"></param>
        /// <param name="peerFirewallSettingObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<ClassBlockEnumMiningShareVoteStatus> UnlockCurrentBlockAsync(long blockHeight, ClassMiningPoWaCShareObject miningPowShareObject, bool enableBroadcast, string apiServerIp, string apiServerOpenNatIp, bool unlockTest, bool fromSync, ClassPeerNetworkSettingObject peerNetworkSetting, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation)
        {
            ClassBlockEnumMiningShareVoteStatus resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED;

            if (peerNetworkSetting == null)
            {
                peerNetworkSetting = new ClassPeerNetworkSettingObject();
                peerFirewallSettingObject = new ClassPeerFirewallSettingObject();
            }

            if (blockHeight <= BlockchainSetting.GenesisBlockHeight)
                return ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED;

            if (blockHeight > BlockchainMemoryManagement.GetLastBlockHeight)
                return ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOT_SYNCED;


            await SemaphoreUnlockBlock.TryWaitExecuteActionAsync(new Action(async () =>
            {
                 try
                 {
                     if (!BlockchainMemoryManagement.ContainsKey(blockHeight + 1))
                     {
                         try
                         {
                             long previousBlockHeight = blockHeight - 1;

                             if (BlockchainMemoryManagement.ContainsKey(blockHeight))
                             {
                                 if (await CheckBlockHeightContainsBlockReward(previousBlockHeight, cancellation))
                                 {

                                     if (BlockchainMemoryManagement[blockHeight, cancellation] != null)
                                     {
                                         if (miningPowShareObject.PoWaCShareDifficulty >= BlockchainMemoryManagement[blockHeight, cancellation].BlockDifficulty)
                                         {
                                             if (BlockchainMemoryManagement[blockHeight, cancellation].BlockHeight == blockHeight)
                                             {
                                                 if (BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject == null
                                                     && BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner.IsNullOrEmpty(false, out _)
                                                     && BlockchainMemoryManagement[blockHeight, cancellation].TimestampFound == 0
                                                     && BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus == ClassBlockEnumStatus.LOCKED)
                                                 {
                                                        // Directly from request.
                                                      long timestampDiff = TaskManager.TaskManager.CurrentTimestampSecond - miningPowShareObject.Timestamp;
                                                     bool timestampDiffCheck = miningPowShareObject.Timestamp >= BlockchainMemoryManagement[blockHeight, cancellation].TimestampCreate && timestampDiff <= BlockchainSetting.BlockMiningUnlockShareTimestampMaxDelay;

                                                     if ((!fromSync && timestampDiffCheck)
                                                         || fromSync)
                                                     {
                                                         string blockHash = BlockchainMemoryManagement[blockHeight, cancellation].BlockHash;
                                                         BigInteger blockDifficulty = BlockchainMemoryManagement[blockHeight, cancellation].BlockDifficulty;

                                                         ClassBlockObject previousBlockObject = await BlockchainMemoryManagement.GetBlockInformationDataStrategy(previousBlockHeight, cancellation);

                                                         if (previousBlockObject != null)
                                                         {
                                                             if (previousBlockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                                             {
                                                                 int previousTransactionCount = previousBlockObject.TotalTransaction;
                                                                 string previousFinalTransactionHash = previousBlockObject.BlockFinalHashTransaction;

                                                                 ClassMiningPoWaCEnumStatus checkShareStatus = ClassMiningPoWaCUtility.CheckPoWaCShare(BlockchainSetting.CurrentMiningPoWaCSettingObject(blockHeight), miningPowShareObject, blockHeight, blockHash, blockDifficulty, previousTransactionCount, previousFinalTransactionHash, out var jobDifficulty, out int jobCompatibilityValue);


                                                                 if (checkShareStatus == ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE)
                                                                 {
                                                                     if (jobDifficulty == miningPowShareObject.PoWaCShareDifficulty)
                                                                     {
                                                                         if (jobCompatibilityValue == previousTransactionCount)
                                                                         {
                                                                             try
                                                                             {
                                                                                    // Check if the block is still locked.
                                                                                    if (BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus == ClassBlockEnumStatus.LOCKED)
                                                                                 {
                                                                                        #region Broadcast the share to other peers, wait final vote from peers, if the result is ok the peer unlock the block.

                                                                                        if (enableBroadcast)
                                                                                     {
                                                                                         Tuple<ClassBlockEnumMiningShareVoteStatus, bool> blockMiningShareVoteStatus = await ClassPeerNetworkBroadcastFunction.AskBlockMiningShareVoteToPeerListsAsync(apiServerIp, apiServerOpenNatIp, string.Empty, blockHeight, miningPowShareObject, peerNetworkSetting, peerFirewallSettingObject, cancellation, true);


                                                                                         if (blockMiningShareVoteStatus.Item2)
                                                                                         {

                                                                                             if (ClassMiningPoWaCUtility.ComparePoWaCShare(BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject, miningPowShareObject)
                                                                                                 && BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner == miningPowShareObject.WalletAddress)
                                                                                             {
                                                                                                 ClassLog.WriteLine("The block height: " + blockHeight + " has been unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + ", transactions are already pushed and the next block already generated.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);
                                                                                                 resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                                                             }
                                                                                             else
                                                                                                 resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED;
                                                                                         }
                                                                                         else
                                                                                         {

                                                                                             if (BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus == ClassBlockEnumStatus.LOCKED)
                                                                                             {
                                                                                                 switch (blockMiningShareVoteStatus.Item1)
                                                                                                 {
                                                                                                        // In this case the share is completly accepted by the peer.
                                                                                                        case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED:
                                                                                                         {
                                                                                                             if (await PushBlockRewardTransactionToMemPool(blockHeight, miningPowShareObject.WalletAddress, miningPowShareObject.Timestamp, false, cancellation))
                                                                                                             {
                                                                                                                 await InsertWholeMemPoolTransactionFromBlockHeight(blockHeight, cancellation);
                                                                                                                 long newBlockHeight = blockHeight + 1;

                                                                                                                 if (await GenerateNewMiningBlockObject(blockHeight, newBlockHeight, miningPowShareObject.Timestamp, miningPowShareObject.WalletAddress, false, false, cancellation))
                                                                                                                 {
                                                                                                                     BlockchainMemoryManagement[blockHeight, cancellation].TimestampFound = miningPowShareObject.Timestamp;
                                                                                                                     BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject = miningPowShareObject;
                                                                                                                     BlockchainMemoryManagement[blockHeight, cancellation].BlockUnlockValid = false;
                                                                                                                     BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus = ClassBlockEnumStatus.UNLOCKED;
                                                                                                                     BlockchainMemoryManagement[blockHeight, cancellation].BlockLastChangeTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;

                                                                                                                     if (ClassBlockchainStats.BlockchainNetworkStatsObject != null)
                                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " has been unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + " push transaction reward to Mem Pool.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, blockHeight < ClassBlockchainStats.BlockchainNetworkStatsObject.LastNetworkBlockHeight, ConsoleColor.Cyan);
                                                                                                                     else
                                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " has been unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + " push transaction reward to Mem Pool.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);

                                                                                                                     resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                                                                                 }
                                                                                                                 else
                                                                                                                 {
                                                                                                                     if (ClassMiningPoWaCUtility.ComparePoWaCShare(BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject, miningPowShareObject) && BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner == miningPowShareObject.WalletAddress)
                                                                                                                     {
                                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " is already unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + " and accepted by peers already.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);
                                                                                                                         resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                                                                                     }
                                                                                                                     else
                                                                                                                     {
                                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " is already found and can't generate the next block height.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkCyan);
                                                                                                                         resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED;
                                                                                                                     }
                                                                                                                 }
                                                                                                             }
                                                                                                             else
                                                                                                             {
                                                                                                                 if (ClassMiningPoWaCUtility.ComparePoWaCShare(BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject, miningPowShareObject) && BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner == miningPowShareObject.WalletAddress)
                                                                                                                 {
                                                                                                                     if (ClassBlockchainStats.BlockchainNetworkStatsObject != null)
                                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " has been unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + " push transaction reward to Mem Pool.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, blockHeight < ClassBlockchainStats.BlockchainNetworkStatsObject.LastNetworkBlockHeight, ConsoleColor.Cyan);
                                                                                                                     else
                                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " has been unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + " push transaction reward to Mem Pool.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);
                                                                                                                     resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                                                                                 }
                                                                                                                 else
                                                                                                                 {
                                                                                                                     ClassLog.WriteLine("The block height: " + blockHeight + " is already found and can't generate the new block height because the block reward already exist.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkCyan);
                                                                                                                     resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;
                                                                                                                 }
                                                                                                             }
                                                                                                         }
                                                                                                         break;
                                                                                                     case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED:
                                                                                                         {
                                                                                                             ClassLog.WriteLine("The block height: " + blockHeight + " mining share from wallet address: " + miningPowShareObject.WalletAddress + " refused by peers.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                                                                                             resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED;
                                                                                                         }
                                                                                                         break;
                                                                                                        // In this case the share is saved on mem pool to do another vote later.
                                                                                                        case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOCONSENSUS:
                                                                                                         {
                                                                                                             ClassLog.WriteLine("The block height: " + blockHeight + " has been found by the wallet address: " + miningPowShareObject.WalletAddress + " but no consensus with peers has been found.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);

                                                                                                             resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOCONSENSUS;
                                                                                                         }
                                                                                                         break;
                                                                                                 }
                                                                                             }
                                                                                             else
                                                                                             {
                                                                                                 if (ClassMiningPoWaCUtility.ComparePoWaCShare(BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject, miningPowShareObject)
                                                                                                 && BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner == miningPowShareObject.WalletAddress)
                                                                                                 {
                                                                                                     if (ClassBlockchainStats.BlockchainNetworkStatsObject != null)
                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " has been unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + " push transaction reward to Mem Pool.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, blockHeight < ClassBlockchainStats.BlockchainNetworkStatsObject.LastNetworkBlockHeight, ConsoleColor.Cyan);
                                                                                                     else
                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " has been unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + " push transaction reward to Mem Pool.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);
                                                                                                     resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                                                                 }
                                                                                                 else
                                                                                                 {
                                                                                                     ClassLog.WriteLine("The block height: " + blockHeight + " is already found by " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                                                                                     resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;
                                                                                                 }
                                                                                             }
                                                                                         }
                                                                                     }

                                                                                        #endregion

                                                                                        else
                                                                                     {
                                                                                         if (!unlockTest)
                                                                                         {
                                                                                             if (await PushBlockRewardTransactionToMemPool(blockHeight, miningPowShareObject.WalletAddress, miningPowShareObject.Timestamp, fromSync, cancellation))
                                                                                             {
                                                                                                 await InsertWholeMemPoolTransactionFromBlockHeight(blockHeight, cancellation);
                                                                                                 long newBlockHeight = blockHeight + 1;

                                                                                                 if (await GenerateNewMiningBlockObject(blockHeight, newBlockHeight, miningPowShareObject.Timestamp, miningPowShareObject.WalletAddress, false, false, cancellation))
                                                                                                 {

                                                                                                     BlockchainMemoryManagement[blockHeight, cancellation].TimestampFound = miningPowShareObject.Timestamp;
                                                                                                     BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject = miningPowShareObject;
                                                                                                     BlockchainMemoryManagement[blockHeight, cancellation].BlockUnlockValid = false;
                                                                                                     BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus = ClassBlockEnumStatus.UNLOCKED;
                                                                                                     BlockchainMemoryManagement[blockHeight, cancellation].BlockLastChangeTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;

                                                                                                     if (ClassBlockchainStats.BlockchainNetworkStatsObject != null)
                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " has been unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + " push transaction reward to Mem Pool.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, blockHeight < ClassBlockchainStats.BlockchainNetworkStatsObject.LastNetworkBlockHeight, ConsoleColor.Cyan);
                                                                                                     else
                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " has been unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + " push transaction reward to Mem Pool.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);

                                                                                                     resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                                                                 }
                                                                                                 else
                                                                                                 {


                                                                                                     if (BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus == ClassBlockEnumStatus.UNLOCKED
                                                                                                         && ClassMiningPoWaCUtility.ComparePoWaCShare(BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject, miningPowShareObject)
                                                                                                         && BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner == miningPowShareObject.WalletAddress)
                                                                                                     {
                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " is already unlocked by the wallet address: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner + " and accepted by peers already.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);
                                                                                                         resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                                                                     }
                                                                                                     else
                                                                                                     {
                                                                                                         ClassLog.WriteLine("The block height: " + blockHeight + " is already found by: " + BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkCyan);
                                                                                                         resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;
                                                                                                     }
                                                                                                 }
                                                                                             }
                                                                                             else
                                                                                             {
                                                                                                 ClassLog.WriteLine("The block height: " + blockHeight + " has been found by the wallet address: " + miningPowShareObject.WalletAddress + " accepted but can't push the transaction reward to Mem Pool.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                                                 resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;
                                                                                             }
                                                                                         }
                                                                                         else
                                                                                             resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                                                     }
                                                                                 }
                                                                                 else
                                                                                 {
                                                                                     if (BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus == ClassBlockEnumStatus.UNLOCKED
                                                                                         && ClassMiningPoWaCUtility.ComparePoWaCShare(BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject, miningPowShareObject)
                                                                                         && BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner == miningPowShareObject.WalletAddress)
                                                                                     {
                                                                                         resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                                                     }
                                                                                     else
                                                                                         resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;
                                                                                 }
                                                                             }
                                                                             catch (Exception error)
                                                                             {
                                                                                 ClassLog.WriteLine("Can't valid block share to unlock the block height: " + blockHeight + ". Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                             }

                                                                         }
                                                                         else
                                                                             ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " is not valid because the share compatibility is not the same translated: " + jobCompatibilityValue + "/" + previousTransactionCount, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                                                     }
                                                                     else
                                                                         ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " is not valid because the share difficulty is not the same translated: " + jobDifficulty + "/" + miningPowShareObject.PoWaCShareDifficulty, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                                                 }
                                                                 else
                                                                 {
                                                                     ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " is not valid for unlock the Block Height: " + blockHeight + ". Check result: " + checkShareStatus, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                                                     resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED;
                                                                 }
                                                             }
                                                             else
                                                             {
                                                                 ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " failed, the previous block height is locked.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);

                                                                 resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED;
                                                             }
                                                         }
                                                         else
                                                         {
                                                             ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " failed, the previous block height is empty.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);

                                                             resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOT_SYNCED;
                                                         }
                                                     }
                                                     else
                                                     {
                                                         ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " have an invalid timestamp of packet.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);
                                                         resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_INVALID_TIMESTAMP;
                                                     }
                                                 }
                                                 else
                                                 {
                                                     if (BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus == ClassBlockEnumStatus.UNLOCKED
                                                         && ClassMiningPoWaCUtility.ComparePoWaCShare(BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject, miningPowShareObject)
                                                             && BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner == miningPowShareObject.WalletAddress)
                                                     {
                                                         if (await GenerateNewMiningBlockObject(blockHeight, blockHeight + 1, miningPowShareObject.Timestamp, miningPowShareObject.WalletAddress, false, false, cancellation))
                                                             resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                         else
                                                             resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;
                                                     }
                                                     else
                                                     {
                                                         ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " is already unlocked.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                                         resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;
                                                     }
                                                 }
                                             }
                                             else
                                                 ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " is not right on the blockchain data.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                         }
                                         else
                                             ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " have an invalid share difficulty. " + miningPowShareObject.PoWaCShareDifficulty + "/" + BlockchainMemoryManagement[blockHeight, cancellation].BlockDifficulty, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                     }
                                     else
                                     {
                                         ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " failed, the block height is empty.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);

                                         resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOT_SYNCED;
                                     }
                                 }
                                 else
                                     ClassLog.WriteLine("The mining share from: " + miningPowShareObject.WalletAddress + " who target block height: " + blockHeight + " cannot be used to unlock the current block, the previous block have not indexed his block reward tx yet.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                             }
                             else
                                 resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOT_SYNCED;
                         }
                         catch (Exception error)
                         {
                             ClassLog.WriteLine("Error from a received attempt to unlock the Block Height: " + blockHeight + ". | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
#if DEBUG
                                Debug.WriteLine("Error from a received attempt to unlock the Block Height: " + blockHeight + ". | Exception: " + error.Message);
#endif
                            }
                     }
                     else
                     {
                        try
                        {
                            ClassBlockObject blockInformationObject = await BlockchainMemoryManagement.GetBlockInformationDataStrategy(blockHeight, cancellation);

                            if (blockInformationObject != null)
                            {
                                if (blockInformationObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED
                                    && ClassMiningPoWaCUtility.ComparePoWaCShare(blockInformationObject.BlockMiningPowShareUnlockObject, miningPowShareObject)
                                    && blockInformationObject.BlockWalletAddressWinner == miningPowShareObject.WalletAddress)
                                {
                                    ClassLog.WriteLine("The block height: " + blockHeight + " has been unlocked by the wallet address: " + blockInformationObject.BlockWalletAddressWinner + ", transactions are already pushed and the next block already generated.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                    resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                }
                                else
                                {
                                    ClassLog.WriteLine("The block height: " + blockHeight + " is already found by: " + blockInformationObject.BlockWalletAddressWinner, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);

                                    resultUnlock = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            ClassLog.WriteLine("Error from a received attempt to unlock the Block Height: " + blockHeight + ". | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
#if DEBUG
                            Debug.WriteLine("Error from a received attempt to unlock the Block Height: " + blockHeight + ". | Exception: " + error.Message);

#endif
                        }
                     }
                 }
                    // Cancellation dead.
                    catch (Exception error)
                 {
                     ClassLog.WriteLine("Error from a received attempt to unlock the Block Height: " + blockHeight + ". | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
#if DEBUG
                        Debug.WriteLine("Error from a received attempt to unlock the Block Height: " + blockHeight + ". | Exception: " + error.Message);
#endif
                    }
             }), cancellation);

            return resultUnlock;
        }

        /// <summary>
        /// Push a block transaction reward to the wallet address who unlock the block.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="walletAddress"></param>
        /// <param name="timestampShare"></param>
        /// <param name="fromSync"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> PushBlockRewardTransactionToMemPool(long blockHeight, string walletAddress, long timestampShare, bool fromSync, CancellationTokenSource cancellation)
        {
            bool result = false;

            long blockHeightConfirmationTarget = blockHeight + BlockchainSetting.TransactionMandatoryBlockRewardConfirmations;

            if (BlockchainMemoryManagement.ContainsKey(blockHeight))
            {
                if (BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus == ClassBlockEnumStatus.LOCKED || fromSync)
                {

                    bool doBlockReward = false;
                    if (BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner.IsNullOrEmpty(false, out _) &&
                        BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject == null ||
                        fromSync)
                    {
                        doBlockReward = true;
                    }
                    else if (BlockchainMemoryManagement[blockHeight, cancellation].BlockWalletAddressWinner == walletAddress)
                        doBlockReward = true;
                    else if (BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject != null)
                    {
                        if (BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject.WalletAddress == walletAddress)
                            doBlockReward = true;
                    }

                    if (doBlockReward)
                    {
                        BigInteger devFee = BlockchainSetting.BlockDevFee(blockHeight);

                        #region With dev fee.

                        if (devFee > 0)
                        {

                            try
                            {
                                ClassTransactionObject blockTransactionRewardObject = ClassTransactionUtility.BuildTransaction(blockHeight, blockHeightConfirmationTarget, BlockchainSetting.BlockRewardName, string.Empty, string.Empty, BlockchainSetting.BlockRewardWithDevFee(blockHeight), devFee, walletAddress, timestampShare, ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION, 0, BlockchainMemoryManagement[blockHeight, cancellation].BlockHash, string.Empty, null, null, null, timestampShare, cancellation);

                                if (!await ClassMemPoolDatabase.CheckTxHashExist(blockTransactionRewardObject.TransactionHash, cancellation))
                                {
                                    ClassBlockTransactionInsertEnumStatus insertStatus = await InsertTransactionToMemPool(blockTransactionRewardObject, true, true, false, cancellation);
                                    if (insertStatus != ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INSERTED)
                                        ClassLog.WriteLine("Warning, can't push the block transaction reward of the unlocked block height: " + blockHeight + " in MemPool for the wallet address: " + walletAddress + ". Result: " + insertStatus, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    else
                                    {
                                        ClassTransactionObject blockTransactionDevFeeObject = ClassTransactionUtility.BuildTransaction(blockHeight, blockHeightConfirmationTarget, BlockchainSetting.BlockRewardName, string.Empty, string.Empty, devFee, 0, BlockchainSetting.WalletAddressDev(blockTransactionRewardObject.TimestampSend), timestampShare, ClassTransactionEnumType.DEV_FEE_TRANSACTION, 0, BlockchainMemoryManagement[blockHeight, cancellation].BlockHash, blockTransactionRewardObject.TransactionHash, null, null, null, timestampShare, cancellation);

                                        if (!await ClassMemPoolDatabase.CheckTxHashExist(blockTransactionDevFeeObject.TransactionHash, cancellation))
                                        {
                                            insertStatus = await InsertTransactionToMemPool(blockTransactionDevFeeObject, true, true, false, cancellation);

                                            if (insertStatus == ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INSERTED)
                                                result = true;
                                            else
                                            {
                                                await ClassMemPoolDatabase.RemoveMemPoolTxObject(blockTransactionRewardObject.TransactionHash, cancellation);
                                                ClassLog.WriteLine("Warning, can't push the block transaction dev fee reward of the unlocked block height: " + blockHeight + " in MemPool for the wallet address: " + walletAddress + ". Result: " + insertStatus, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                            }
                                        }
                                        else
                                            ClassLog.WriteLine("Warning, can't push the block transaction dev fee of the unlocked block height: " + blockHeight + " in MemPool for the wallet address: " + walletAddress + ". The block dev fee already exist.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    }
                                }
                                else
                                    ClassLog.WriteLine("Warning, can't push the block transaction reward of the unlocked block height: " + blockHeight + " in MemPool for the wallet address: " + walletAddress + ". The block reward already exist.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            }
                            catch (Exception error)
                            {
                                ClassLog.WriteLine("Warning, can't push the block transaction reward of the unlocked block height: " + blockHeight + " in MemPool for the wallet address: " + walletAddress + ". Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                result = false;
                            }

                        }

                        #endregion

                        else
                        {
                            #region No dev fee.

                            try
                            {

                                ClassTransactionObject blockTransactionRewardObject = ClassTransactionUtility.BuildTransaction(blockHeight, blockHeightConfirmationTarget, BlockchainSetting.BlockRewardName, string.Empty, string.Empty, BlockchainSetting.BlockReward(blockHeight), 0, walletAddress, timestampShare, ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION, 0, BlockchainMemoryManagement[blockHeight, cancellation].BlockHash, string.Empty, null, null, null, timestampShare, cancellation);

                                if (!await ClassMemPoolDatabase.CheckTxHashExist(blockTransactionRewardObject.TransactionHash, cancellation))
                                {
                                    ClassBlockTransactionInsertEnumStatus insertStatus = await InsertTransactionToMemPool(blockTransactionRewardObject, true, true, false, cancellation);

                                    if (insertStatus == ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INSERTED)
                                        result = true;
                                    else
                                        ClassLog.WriteLine("Warning, can't push the block transaction reward of the unlocked block height: " + blockHeight + " in MemPool for the wallet address: " + walletAddress + ". Result: " + insertStatus, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                }
                                else
                                    ClassLog.WriteLine("Warning, can't push the block transaction dev fee reward of the unlocked block height: " + blockHeight + " in MemPool for the wallet address: " + walletAddress + ". The block reward already exist.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            }
                            catch (Exception error)
                            {
                                ClassLog.WriteLine("Warning, can't push the block transaction reward of the unlocked block height: " + blockHeight + " in MemPool for the wallet address: " + walletAddress + ". Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                result = false;
                            }

                            #endregion
                        }
                    }
                }

            }

            return result;
        }

        /// <summary>
        /// Generate new mining block object.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="newBlockHeight"></param>
        /// <param name="timestampFound"></param>
        /// <param name="walletAddressWinner"></param>
        /// <param name="isGenesis"></param>
        /// <param name="cancellation"></param>
        public static async Task<bool> GenerateNewMiningBlockObject(long blockHeight, long newBlockHeight, long timestampFound, string walletAddressWinner, bool isGenesis, bool remakeBlockHeight, CancellationTokenSource cancellation)
        {
            if (BlockchainMemoryManagement.ContainsKey(blockHeight))
            {
                if (BlockchainMemoryManagement[blockHeight, cancellation].TimestampCreate <= timestampFound)
                {
                    if (!BlockchainMemoryManagement.ContainsKey(newBlockHeight) || remakeBlockHeight)
                    {
                        BlockchainMemoryManagement[blockHeight, cancellation].BlockFinalHashTransaction = ClassBlockUtility.GetFinalTransactionHashList(BlockchainMemoryManagement[blockHeight, cancellation].BlockTransactions.Keys.ToList(), BlockchainMemoryManagement[blockHeight, cancellation].BlockHash);

                        BigInteger newBlockDifficulty;

                        if (!isGenesis)
                            newBlockDifficulty = await ClassBlockUtility.GenerateNextBlockDifficulty(blockHeight, BlockchainMemoryManagement[blockHeight, cancellation].TimestampCreate, timestampFound, BlockchainMemoryManagement[blockHeight, cancellation].BlockDifficulty, cancellation);
                        else
                            newBlockDifficulty = BlockchainSetting.MiningMinDifficulty;

                        string newBlockHash = ClassBlockUtility.GenerateBlockHash(newBlockHeight, newBlockDifficulty, BlockchainMemoryManagement[blockHeight, cancellation].BlockTransactions.Count, BlockchainMemoryManagement[blockHeight, cancellation].BlockFinalHashTransaction, walletAddressWinner);

                        if (!remakeBlockHeight)
                        {

                            if (await BlockchainMemoryManagement.Add(newBlockHeight, new ClassBlockObject(newBlockHeight, newBlockDifficulty, newBlockHash, timestampFound, 0, ClassBlockEnumStatus.LOCKED, false, false), CacheBlockMemoryInsertEnumType.INSERT_IN_ACTIVE_MEMORY_OBJECT, cancellation))
                            {
                                if (ClassBlockchainStats.BlockchainNetworkStatsObject != null)
                                    ClassLog.WriteLine("New block " + newBlockHeight + "/" + ClassBlockchainStats.BlockchainNetworkStatsObject.LastNetworkBlockHeight + " | Difficulty: " + newBlockDifficulty + " | Hash: " + newBlockHash + " generated.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, ClassBlockchainStats.BlockchainNetworkStatsObject.LastNetworkBlockHeight > newBlockHeight, ConsoleColor.Green);
                                else
                                    ClassLog.WriteLine("New block " + newBlockHeight + " | Difficulty: " + newBlockDifficulty + " | Hash: " + newBlockHash + " generated.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                                return true;
                            }

                            ClassLog.WriteLine("Can't generate the next block height " + newBlockHeight + " because this one is already inserted just before.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                        }
                        else
                        {
                            BlockchainMemoryManagement[newBlockHeight, cancellation] = new ClassBlockObject(newBlockHeight, newBlockDifficulty, newBlockHash, timestampFound, 0, ClassBlockEnumStatus.LOCKED, false, false);

                            if (ClassBlockchainStats.BlockchainNetworkStatsObject != null)
                                ClassLog.WriteLine("Regenerated block height " + newBlockHeight + "/" + ClassBlockchainStats.BlockchainNetworkStatsObject.LastNetworkBlockHeight + " | Difficulty: " + newBlockDifficulty + " | Hash: " + newBlockHash + " generated.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, ClassBlockchainStats.BlockchainNetworkStatsObject.LastNetworkBlockHeight > newBlockHeight, ConsoleColor.Green);
                            else
                                ClassLog.WriteLine("Regenerated block height " + newBlockHeight + " | Difficulty: " + newBlockDifficulty + " | Hash: " + newBlockHash + " generated.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                            return true;
                        }
                    }
                    /*else
                        ClassLog.WriteLine("Can't generate the next block height " + newBlockHeight + " because this one already exist.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                    */
                }
            }
            return false;
        }

        #endregion

        #region Functions dedicated to insert data to MemPool.

        /// <summary>
        /// Insert a transaction in MemPool.
        /// </summary>
        /// <param name="transactionObject"></param>
        /// <param name="checkTxHash"></param>
        /// <param name="isBlockReward"></param>
        /// <param name="fromOutside"></param>
        /// <param name="cancellation"></param>
        /// 
        /// <returns></returns>
        public static async Task<ClassBlockTransactionInsertEnumStatus> InsertTransactionToMemPool(ClassTransactionObject transactionObject, bool checkTxHash, bool isBlockReward, bool fromOutside, CancellationTokenSource cancellation)
        {
            if (checkTxHash)
            {
                if (await CheckIfTransactionHashAlreadyExist(transactionObject.TransactionHash, transactionObject.BlockHeightTransaction, cancellation))
                    return ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_HASH_ALREADY_EXIST;
            }

            if (!isBlockReward && !fromOutside)
            {
                if (transactionObject.BlockHeightTransaction < BlockchainMemoryManagement.GetLastBlockHeight)
                    return ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INVALID;

                if (BlockchainMemoryManagement.ContainsKey(transactionObject.BlockHeightTransaction))
                {
                    ClassBlockObject blockObjectInformations = await BlockchainMemoryManagement.GetBlockInformationDataStrategy(transactionObject.BlockHeightTransaction, cancellation);


                    if (blockObjectInformations != null)
                    {
                        if (blockObjectInformations.TotalTransaction + 1 >= BlockchainSetting.MaxTransactionPerBlock)
                            return ClassBlockTransactionInsertEnumStatus.MAX_BLOCK_TRANSACTION_PER_BLOCK_HEIGHT_TARGET_REACH;
                    }
                    else
                        return ClassBlockTransactionInsertEnumStatus.BLOCK_HEIGHT_NOT_EXIST;
                }
                if (await ClassMemPoolDatabase.GetCountMemPoolTxFromBlockHeight(transactionObject.BlockHeightTransaction, false, cancellation) + 1 >= BlockchainSetting.MaxTransactionPerBlock)
                    return ClassBlockTransactionInsertEnumStatus.MAX_BLOCK_TRANSACTION_PER_BLOCK_HEIGHT_TARGET_REACH;
            }

            if (ClassMemPoolDatabase.InsertTxToMemPool(transactionObject))
            {
                ClassLog.WriteLine("Transaction hash: " + transactionObject.TransactionHash + " of type: " + transactionObject.TransactionType + " who target the block height: " + transactionObject.BlockHeightTransaction + " has been inserted to the mempool.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);

                return ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INSERTED;
            }



            return ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_HASH_ALREADY_EXIST;
        }

        /// <summary>
        /// Insert every transactions in mem pool from a block height selected.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        public static async Task InsertWholeMemPoolTransactionFromBlockHeight(long blockHeight, CancellationTokenSource cancellation)
        {
            try
            {
                if (await ClassMemPoolDatabase.GetCountMemPoolTxFromBlockHeight(blockHeight, false, cancellation) > 0)
                {
                    using (DisposableList<ClassTransactionObject> listMemPoolTransactionObject = await ClassMemPoolDatabase.GetMemPoolTxObjectFromBlockHeight(blockHeight, false, cancellation))
                    {
                        foreach (var txTransactionObject in listMemPoolTransactionObject.GetList)
                        {
                            ClassBlockTransactionInsertEnumStatus insertResult = InsertBlockTransaction(blockHeight, txTransactionObject, cancellation);

                            if (insertResult != ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INSERTED && insertResult != ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_HASH_ALREADY_EXIST)
                            {
                                ClassLog.WriteLine("Can't insert tx in mempool who target the block height: " + blockHeight + ", try again later. Result: " + insertResult, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                break;
                            }
                        }
                    }

                    await ClassMemPoolDatabase.RemoveMemPoolAllTxFromBlockHeightTarget(blockHeight, cancellation);
                }

                BlockchainMemoryManagement[blockHeight, cancellation].TotalTransaction = BlockchainMemoryManagement[blockHeight, cancellation].BlockTransactions.Count;

            }
            catch (Exception error)
            {
                ClassLog.WriteLine("Error on insert every transactions inside mempool indexed at the block height: " + blockHeight + " into the block. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
            }
        }

        #endregion

        #region Functions dedicated to check blockchain data.

        /// <summary>
        /// Check the blockchain database.
        /// </summary>
        /// <param name="blockchainDatabaseSetting"></param>
        /// <returns></returns>
        private static async Task<bool> CheckBlockchainDatabase(ClassBlockchainDatabaseSetting blockchainDatabaseSetting)
        {

            ClassLog.WriteLine("Checking block(s) synced please wait a moment..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            if (BlockchainMemoryManagement.Count > 0)
            {
                long blockHeightExpected = BlockchainSetting.GenesisBlockHeight;
                bool missingBlock = false;
                int totalBlockMissing = 0;
                long lastBlockHeight = BlockchainMemoryManagement.GetLastBlockHeight;


                while (blockHeightExpected <= lastBlockHeight)
                {
                    if (!BlockchainMemoryManagement.ContainsKey(blockHeightExpected))
                    {
#if DEBUG
                        Debug.WriteLine("Block height: " + blockHeightExpected + " is missing.");
#endif
                        missingBlock = true;
                        totalBlockMissing++;
                        break;
                    }
                    blockHeightExpected++;
                }


                if (missingBlock)
                {
                    ClassLog.WriteLine("[Warning] " + totalBlockMissing + " total block(s) expected not exist. Please resync your node by security.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return false;
                }
            }

            ClassBlockObject previousBlockObject = null;
            bool needConfirmAgainTx = false;
            long lastBlockHeightUnlockedChecked = await BlockchainMemoryManagement.GetLastBlockHeightConfirmationNetworkChecked(_cancellationTokenStopBlockchain);

            // Check data integrity and transactions confirmations.

            using (DisposableList<long> listBlockHeight = BlockchainMemoryManagement.ListBlockHeight)
            {
                foreach (var blockHeight in listBlockHeight.GetList)
                {
                    if (needConfirmAgainTx)
                        break;

                    ClassBlockObject blockObject = await BlockchainMemoryManagement.GetBlockDataStrategy(blockHeight, false, false, _cancellationTokenStopBlockchain);

                    if (blockObject == null)
                    {
                        ClassLog.WriteLine("[Warning] The block height " + blockHeight + " is empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                        return false;
                    }

                    if (blockObject.BlockHeight == BlockchainSetting.GenesisBlockHeight)
                    {
                        if (blockObject.BlockTransactions.Count != BlockchainSetting.GenesisBlockTransactionCount)
                        {
                            ClassLog.WriteLine("[Warning] The genesis block is not correct, the amount of tx expected is not the right one.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                            return false;
                        }

                        if (!CheckBlockObjectTransactionConfirmationsProgressDone(blockObject, lastBlockHeightUnlockedChecked))
                        {
                            needConfirmAgainTx = true;
                            break;
                        }

                        foreach (var tx in blockObject.BlockTransactions)
                        {
                            if (tx.Value.TransactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION)
                            {
                                ClassLog.WriteLine("[Warning] The genesis block is not correct, the type expected has not the right one.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                return false;
                            }
                            if (tx.Value.TransactionObject.Amount != BlockchainSetting.GenesisBlockAmount)
                            {
                                ClassLog.WriteLine("[Warning] The genesis block is not correct, the amount expected has not the right one. " + tx.Value.TransactionObject.Amount + "/" + BlockchainSetting.GenesisBlockAmount, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                return false;
                            }
                            if (tx.Value.TransactionObject.WalletAddressReceiver != BlockchainSetting.WalletAddressDev(tx.Value.TransactionObject.TimestampSend))
                            {
                                ClassLog.WriteLine("[Warning] The genesis block is not correct, the receiver expected has not the right one. " + tx.Value.TransactionObject.WalletAddressReceiver + "/" + BlockchainSetting.WalletAddressDev(tx.Value.TransactionObject.TimestampSend), ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                return false;
                            }

                            if (!CheckBlockTransactionConfirmationDone(blockObject.BlockStatus, tx.Value, blockObject.BlockHeight, lastBlockHeightUnlockedChecked))
                            {
                                needConfirmAgainTx = true;
                                break;
                            }
                        }
                    }
                    else
                    {

                        ClassBlockEnumCheckStatus blockCheckStatus = ClassBlockUtility.CheckBlockHash(blockObject.BlockHash, blockObject.BlockHeight, blockObject.BlockDifficulty, previousBlockObject.TotalTransaction, previousBlockObject.BlockFinalHashTransaction);

                        if (blockCheckStatus != ClassBlockEnumCheckStatus.VALID_BLOCK_HASH)
                        {
                            if (blockCheckStatus == ClassBlockEnumCheckStatus.INVALID_BLOCK_TRANSACTION_COUNT)
                            {
                                if (!previousBlockObject.IsConfirmedByNetwork && !previousBlockObject.IsChecked)
                                {
                                    ClassLog.WriteLine("[Warning] The block hash from the block height: " + blockObject.BlockHeight + " is not valid. Result: " + blockCheckStatus + ". A resync from the scratch is necessary.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                    return false;
                                }
                            }
                        }

                        if (!CheckBlockObjectTransactionConfirmationsProgressDone(blockObject, lastBlockHeightUnlockedChecked))
                            needConfirmAgainTx = true;
                        else
                        {
                            BigInteger totalConfirmed = 0;
                            BigInteger totalFee = 0;
                            BigInteger totalPending = 0;

                            foreach (var tx in blockObject.BlockTransactions)
                            {
                                if (!CheckBlockTransactionConfirmationDone(blockObject.BlockStatus, tx.Value, blockObject.BlockHeight, lastBlockHeightUnlockedChecked))
                                {
                                    needConfirmAgainTx = true;
                                    break;
                                }
                                else
                                {
                                    if (tx.Value.TransactionStatus)
                                    {
                                        if (tx.Value.IsConfirmed)
                                            totalConfirmed += (tx.Value.TransactionObject.Amount - tx.Value.TotalSpend);
                                        else
                                            totalPending += tx.Value.TransactionObject.Amount;

                                        if (tx.Value.TransactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION &&
                                            tx.Value.TransactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                                            totalPending += tx.Value.TransactionObject.Fee;
                                    }
                                }
                            }

                            if (!needConfirmAgainTx)
                            {
                                blockObject.TotalCoinConfirmed = totalConfirmed;
                                blockObject.TotalCoinPending = totalPending;
                                blockObject.TotalFee = totalFee;
                                blockObject.TotalTransaction = blockObject.BlockTransactions.Count;
                            }
                        }
                    }

                    if (blockObject.BlockStatus == ClassBlockEnumStatus.LOCKED)
                    {
                        blockObject.BlockLastHeightTransactionConfirmationDone = 0;
                        blockObject.BlockTotalTaskTransactionConfirmationDone = 0;
                        blockObject.BlockNetworkAmountConfirmations = 0;
                        blockObject.BlockUnlockValid = false;
                        blockObject.BlockTransactions?.Clear();
                        blockObject.TotalTransaction = 0;
                        blockObject.TotalCoinConfirmed = 0;
                        blockObject.TotalCoinPending = 0;
                        blockObject.TotalFee = 0;
                        blockObject.TimestampFound = 0;
                        blockObject.BlockTransactionFullyConfirmed = false;
                        blockObject.TotalTransaction = blockObject.BlockTransactions.Count;
                        blockObject.BlockTransactionConfirmationCheckTaskDone = false;
                        blockObject.BlockLastChangeTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;
                        blockObject.BlockMiningPowShareUnlockObject = null;
                        if (!await BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObject, true, _cancellationTokenStopBlockchain))
                        {
                            ClassLog.WriteLine("[Warning] Failed to save corrections provided on a block who failed to checker.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                            return false;
                        }
                    }

                    blockObject.DeepCloneBlockObject(false, out previousBlockObject);
                }
            }

            #region Reset transactions confirmations done and network confirmations.

            if (needConfirmAgainTx)
            {

                ClassLog.WriteLine("[Warning] An error on transactions confirmations done has been found, every check and confirmations need to be done again.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

                if (DictionaryCheckpointObjects.ContainsKey(ClassCheckpointEnumType.WALLET_CHECKPOINT))
                    DictionaryCheckpointObjects[ClassCheckpointEnumType.WALLET_CHECKPOINT]?.Clear();

                if (DictionaryCheckpointObjects.ContainsKey(ClassCheckpointEnumType.BLOCK_HEIGHT_TRANSACTION_CHECKPOINT))
                    DictionaryCheckpointObjects[ClassCheckpointEnumType.BLOCK_HEIGHT_TRANSACTION_CHECKPOINT]?.Clear();

                ClassLog.WriteLine("All checkpoints removed.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

                using (DisposableList<long> listBlockHeight = BlockchainMemoryManagement.ListBlockHeight)
                {
                    foreach (var blockHeight in listBlockHeight.GetList)
                    {
                        ClassBlockObject blockObject = await BlockchainMemoryManagement.GetBlockDataStrategy(blockHeight, false, false, _cancellationTokenStopBlockchain);

                        if (blockObject == null)
                        {
                            ClassLog.WriteLine("[Warning] The block height " + blockHeight + " is empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                            return false;
                        }

                        blockObject.BlockLastHeightTransactionConfirmationDone = 0;
                        blockObject.BlockTotalTaskTransactionConfirmationDone = 0;
                        blockObject.BlockNetworkAmountConfirmations = 0;
                        blockObject.BlockUnlockValid = false;
                        blockObject.TotalTransaction = blockObject.BlockTransactions.Count;
                        blockObject.TotalCoinConfirmed = 0;
                        blockObject.TotalCoinPending = 0;
                        blockObject.TotalFee = 0;
                        blockObject.BlockTransactionFullyConfirmed = false;
                        blockObject.TotalTransaction = blockObject.BlockTransactions.Count;
                        blockObject.BlockTransactionConfirmationCheckTaskDone = false;
                        blockObject.BlockLastChangeTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;

                        foreach (var txHash in blockObject.BlockTransactions.Keys)
                        {
                            blockObject.BlockTransactions[txHash].TotalSpend = 0;
                            blockObject.BlockTransactions[txHash].TransactionInvalidStatus = ClassTransactionEnumStatus.VALID_TRANSACTION;
                            blockObject.BlockTransactions[txHash].TransactionStatus = true;
                            blockObject.BlockTransactions[txHash].TransactionTotalConfirmation = 0;
                        }

                        bool blockIsCached = await BlockchainMemoryManagement.BlockHeightIsCached(blockHeight, _cancellationTokenStopBlockchain);

                        if (!await BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObject, !blockIsCached, _cancellationTokenStopBlockchain))
                        {
                            ClassLog.WriteLine("[Warning] Can't save reset block height " + blockObject.BlockHeight + " confirmations done.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                            return false;
                        }
                    }
                }
            }

            #endregion

            ClassLog.WriteLine("Check of block(s) synced done.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            return true;
        }

        /// <summary>
        /// Check block object confirmations progress done.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="lastBlockHeightUnlockedChecked"></param>
        /// <returns></returns>
        private static bool CheckBlockObjectTransactionConfirmationsProgressDone(ClassBlockObject blockObject, long lastBlockHeightUnlockedChecked)
        {
            if (blockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
            {
                if (blockObject.BlockTransactionConfirmationCheckTaskDone && blockObject.IsConfirmedByNetwork)
                {
                    if (blockObject.BlockTotalTaskTransactionConfirmationDone > 0 && blockObject.BlockLastHeightTransactionConfirmationDone > lastBlockHeightUnlockedChecked)
                    {
#if DEBUG
                        Debug.WriteLine("Invalid confirmations on block height: " + blockObject.BlockHeight + " | Total task confirmations done: " + blockObject.BlockTotalTaskTransactionConfirmationDone + " | Last height confirmations done: " + blockObject.BlockLastHeightTransactionConfirmationDone + "/" + lastBlockHeightUnlockedChecked);
#endif
                        return false;
                    }
                    if (blockObject.BlockTotalTaskTransactionConfirmationDone > (lastBlockHeightUnlockedChecked - blockObject.BlockHeight))
                    {
#if DEBUG
                        Debug.WriteLine("Invalid confirmations on block height: " + blockObject.BlockHeight + " | Total task confirmations done: " + blockObject.BlockTotalTaskTransactionConfirmationDone + " is above: " + (lastBlockHeightUnlockedChecked - blockObject.BlockHeight));
#endif
                        return false;
                    }
                }
                else
                {
                    if (blockObject.BlockTransactionConfirmationCheckTaskDone && (!blockObject.BlockUnlockValid || blockObject.BlockNetworkAmountConfirmations < BlockchainSetting.BlockAmountNetworkConfirmations))
                    {
#if DEBUG
                        Debug.WriteLine("Invalid confirmations check task done on block height: " + blockObject.BlockHeight + " | the block is not set has valid by network confirmation.");
#endif
                        return false;
                    }

                    if (blockObject.BlockTotalTaskTransactionConfirmationDone > 0 || blockObject.BlockLastHeightTransactionConfirmationDone > 0 && (!blockObject.BlockUnlockValid || blockObject.BlockNetworkAmountConfirmations < BlockchainSetting.BlockAmountNetworkConfirmations || !blockObject.BlockTransactionConfirmationCheckTaskDone))
                    {
#if DEBUG
                        Debug.WriteLine("Invalid confirmations on block height: " + blockObject.BlockHeight + " | has confirmations done without to has been set has valid before.");
#endif
                        return false;
                    }

                }
            }
            else
            {
                if (blockObject.BlockTotalTaskTransactionConfirmationDone > 0 || blockObject.BlockUnlockValid || blockObject.BlockTransactionConfirmationCheckTaskDone || blockObject.BlockNetworkAmountConfirmations > 0 || blockObject.BlockLastHeightTransactionConfirmationDone > 0)
                {
#if DEBUG
                    Debug.WriteLine("Invalid confirmations on block height: " + blockObject.BlockHeight + " | has confirmations done without to be unlocked.");
#endif
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check block transaction confirmations done.
        /// </summary>
        /// <param name="blockStatus"></param>
        /// <param name="blockTransaction"></param>
        /// <param name="blockHeight"></param>
        /// <param name="lastBlockHeightUnlockedChecked"></param>
        /// <returns></returns>
        private static bool CheckBlockTransactionConfirmationDone(ClassBlockEnumStatus blockStatus, ClassBlockTransaction blockTransaction, long blockHeight, long lastBlockHeightUnlockedChecked)
        {
            if (blockStatus == ClassBlockEnumStatus.UNLOCKED)
            {
                if (blockTransaction.TransactionTotalConfirmation > 0)
                {
                    if (blockTransaction.TransactionTotalConfirmation > (lastBlockHeightUnlockedChecked - blockHeight) + 1)
                    {
#if DEBUG
                        Debug.WriteLine("Invalid of block transactions confirmations done on the transaction hash: " + blockTransaction.TransactionObject.TransactionHash + "." +
                                        " Confirmation(s) done: " + blockTransaction.TransactionTotalConfirmation + "/" + (lastBlockHeightUnlockedChecked - blockHeight) +
                                        "\nBlock heights: " + blockHeight + "/" + lastBlockHeightUnlockedChecked);

#endif
                        return false;
                    }
                }
            }
            else
            {
                if (blockTransaction.TransactionTotalConfirmation > 0)
                {
#if DEBUG
                    Debug.WriteLine("Invalid of block transactions confirmations done on the transaction hash: " + blockTransaction.TransactionObject.TransactionHash + " because the block height: " + blockHeight + " is locked. Confirmation(s) done: " + blockTransaction.TransactionTotalConfirmation);
#endif
                    return false;
                }
            }

            return true;
        }

        #endregion

    }
}
