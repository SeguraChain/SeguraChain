using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Disk.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;

using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Main
{
    public class ClassCacheIoSystem
    {


        /// <summary>
        /// Objects and settings of the IO cache in disk mode.
        /// </summary>
        private const string IoFileExtension = ".ioblock";
        private Dictionary<string, ClassCacheIoIndexObject> _dictionaryCacheIoIndexObject;
        private string _cacheIoDirectoryPath;

        /// <summary>
        /// Blockchain database settings.
        /// </summary>
        private ClassBlockchainDatabaseSetting _blockchainDatabaseSetting;

        /// <summary>
        /// Multithreading settings.
        /// </summary>
        private SemaphoreSlim _semaphoreIoCacheIndexAccess;

        /// <summary>
        /// Save the last memory usage of the io cache system.
        /// </summary>
        private long _totalIoCacheSystemMemoryUsage;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="blockchainDatabaseSetting"></param>
        public ClassCacheIoSystem(ClassBlockchainDatabaseSetting blockchainDatabaseSetting)
        {
            _blockchainDatabaseSetting = blockchainDatabaseSetting;
            _cacheIoDirectoryPath = _blockchainDatabaseSetting.GetBlockchainCacheDirectoryPath;
            _semaphoreIoCacheIndexAccess = new SemaphoreSlim(1, ClassUtility.GetMaxAvailableProcessorCount());
            _dictionaryCacheIoIndexObject = new Dictionary<string, ClassCacheIoIndexObject>();
        }

        #region Manage IO Cache system.

        /// <summary>
        /// Initialize the IO cache system.
        /// </summary>
        /// <returns></returns>
        public async Task<Tuple<bool, HashSet<long>>> InitializeCacheIoSystem()
        {
            HashSet<long> listBlockHeight = new HashSet<long>();

            if (!Directory.Exists(_cacheIoDirectoryPath))
                Directory.CreateDirectory(_cacheIoDirectoryPath);
            else
            {
                string[] cacheIoFileList = Directory.GetFiles(_cacheIoDirectoryPath, "*" + IoFileExtension);

                if (cacheIoFileList.Length > 0)
                {
                    foreach (var ioFileName in cacheIoFileList)
                    {
                        Tuple<bool, HashSet<long>> result = await InitializeNewCacheIoIndex(Path.GetFileName(ioFileName));

                        foreach (long blockHeight in result.Item2)
                            listBlockHeight.Add(blockHeight);
                    }
                }
            }

            return new Tuple<bool, HashSet<long>>(true, listBlockHeight);
        }

        /// <summary>
        /// Initialize a new cache io index.
        /// </summary>
        /// <param name="ioFileName"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, HashSet<long>>> InitializeNewCacheIoIndex(string ioFileName)
        {
            HashSet<long> listBlockHeight = new HashSet<long>();

            if (!_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
            {
                ClassCacheIoIndexObject cacheIoIndexObject = new ClassCacheIoIndexObject(ioFileName, _blockchainDatabaseSetting, this);

                Tuple<bool, HashSet<long>> result = await cacheIoIndexObject.InitializeIoCacheObjectAsync();

                if (result.Item1)
                {
                    try
                    {
                        _dictionaryCacheIoIndexObject.Add(ioFileName, cacheIoIndexObject);

                        foreach (long blockHeight in result.Item2)
                            listBlockHeight.Add(blockHeight);
                    }
                    catch
                    {
#if DEBUG
                        Debug.WriteLine("Cache IO System - Failed to index the new io cache file: " + ioFileName);
#endif
                        ClassLog.WriteLine("Cache IO System - Failed to index the new io cache file: " + ioFileName, ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                        
                        return result;
                    }
                }
                else
                {
#if DEBUG
                    Debug.WriteLine("Cache IO System - Failed to initialize the new io cache file: " + ioFileName);
#endif

                    ClassLog.WriteLine("Cache IO System - Failed to initialize the new io cache file: " + ioFileName, ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                }
            }

            return new Tuple<bool, HashSet<long>>(true, listBlockHeight);
        }

        /// <summary>
        /// Purge the io cache system.
        /// </summary>
        /// <returns></returns>
        public async Task PurgeCacheIoSystem(CancellationTokenSource cancellation)
        {
            if (_dictionaryCacheIoIndexObject.Count > 0)
            {
                string[] ioFileNameArray = _dictionaryCacheIoIndexObject.Keys.ToArray();
                int totalTaskToDo = ioFileNameArray.Length;
                int totalTaskDone = 0;

                foreach (string ioFileName in ioFileNameArray)
                {
                    if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask)
                    {
                        try
                        {
                            await Task.Factory.StartNew(async () =>
                            {
                                await _dictionaryCacheIoIndexObject[ioFileName].PurgeIoBlockDataMemory(false, cancellation, 0, false);
                                totalTaskDone++;

                            }, cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                        }
                        catch
                        {
                            totalTaskDone++;
                        }
                    }
                    else
                        await _dictionaryCacheIoIndexObject[ioFileName].PurgeIoBlockDataMemory(false, cancellation, 0, false);
                }

                if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask)
                {
                    while (totalTaskToDo > totalTaskDone)
                    {
                        try
                        {
                            await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay, cancellation.Token);
                        }
                        catch
                        {
                            break;
                        }
                    }
                }

                long totalIoCacheMemoryUsage = GetIoCacheSystemMemoryConsumption(cancellation, out int totalBlockKeepAlive);


#if DEBUG
                Debug.WriteLine("Cache IO Index Object - Total block(s) keep alive: " + totalBlockKeepAlive + " | Total Memory usage from the cache: " + ClassUtility.ConvertBytesToMegabytes(totalIoCacheMemoryUsage));
#endif
                ClassLog.WriteLine("Cache IO Index Object - Total block(s) keep alive: " + totalBlockKeepAlive + " | Total Memory usage from the cache: " + ClassUtility.ConvertBytesToMegabytes(totalIoCacheMemoryUsage), ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            }
        }

        /// <summary>
        /// Clean the io cache system.
        /// </summary>
        public async Task CleanCacheIoSystem()
        {
            bool semaphoreUsed = false;

            try
            {
                await _semaphoreIoCacheIndexAccess.WaitAsync();
                semaphoreUsed = true;

                if (_dictionaryCacheIoIndexObject.Count > 0)
                {
                    string[] ioFileNameArray = _dictionaryCacheIoIndexObject.Keys.ToArray();
                    int totalTaskToDo = ioFileNameArray.Length;
                    int totalTaskDone = 0;
                    CancellationTokenSource cancellation = new CancellationTokenSource();

                    foreach (string ioFileName in ioFileNameArray)
                    {
                        if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && ioFileNameArray.Length > 1)
                        {
                            try
                            {
                                await Task.Factory.StartNew(() =>
                                {
                                    string ioFilePath = _blockchainDatabaseSetting.GetBlockchainCacheDirectoryPath + ioFileName;
                                    _dictionaryCacheIoIndexObject[ioFileName].CloseLockStream();
                                    _dictionaryCacheIoIndexObject.Remove(ioFilePath);
                                    File.Delete(ioFilePath);
                                    totalTaskDone++;

                                }, cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                            }
                            catch
                            {
                                totalTaskDone++;
                            }
                        }
                        else
                        {
                            string ioFilePath = _blockchainDatabaseSetting.GetBlockchainCacheDirectoryPath + ioFileName;
                            _dictionaryCacheIoIndexObject[ioFileName].CloseLockStream();
                            _dictionaryCacheIoIndexObject.Remove(ioFilePath);
                            File.Delete(ioFilePath);
                        }
                    }

                    if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && ioFileNameArray.Length > 1)
                    {
                        while (totalTaskToDo > totalTaskDone)
                        {
                            try
                            {
                                await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay, cancellation.Token);
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }

                    cancellation.Cancel();

                    try
                    {
                        Directory.Delete(_blockchainDatabaseSetting.GetBlockchainCacheDirectoryPath, true);
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreIoCacheIndexAccess.Release();
            }
        }

        /// <summary>
        /// Do a purge of the io cache system from a io cache file index to except.
        /// </summary>
        /// <param name="ioFileNameSource"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> DoPurgeFromIoCacheIndex(string ioFileNameSource, long memoryAsked, CancellationTokenSource cancellation)
        {
            if (memoryAsked == 0)
                return true;

            long totalMemoryRetrieved = 0;

            foreach (string ioFileFileIndex in _dictionaryCacheIoIndexObject.Keys.ToArray())
            {

                if (ioFileFileIndex != ioFileNameSource)
                {
                    long restMemoryToTask = memoryAsked - totalMemoryRetrieved;

                    if (GetIoCacheSystemMemoryConsumption(cancellation, out _) + restMemoryToTask <= _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxActiveMemoryAllocationFromCache)
                        return true;

                    long totalMemoryFreeRetrieved = await _dictionaryCacheIoIndexObject[ioFileFileIndex].PurgeIoBlockDataMemory(false, cancellation, restMemoryToTask, true);

                    totalMemoryRetrieved += totalMemoryFreeRetrieved;

                    if (totalMemoryRetrieved >= memoryAsked)
                        return true;
                }
            }

            return false;
        }

        #endregion

        #region Get/Set/Update IO Cache data.

        /// <summary>
        /// Get io list block information objects by a list of block height from io cache files.
        /// </summary>
        /// <param name="listBlockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<SortedList<long, ClassBlockObject>> GetIoListBlockInformationObject(DisposableList<long> listBlockHeight, CancellationTokenSource cancellationIoCache)
        {
            SortedList<long, ClassBlockObject> listBlockInformation = new SortedList<long, ClassBlockObject>();

            if (listBlockHeight.Count > 0)
            {
                if (listBlockHeight.Count > 0)
                {
                    using (DisposableDictionary<string, List<long>> dictionaryRangeBlockHeightIoCacheFile = new DisposableDictionary<string, List<long>>())
                    {

                        #region Generate list of io cache file index name.

                        foreach (var blockHeight in listBlockHeight.GetList)
                        {

                            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                            if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                            {
                                if (!dictionaryRangeBlockHeightIoCacheFile.ContainsKey(ioFileName))
                                    dictionaryRangeBlockHeightIoCacheFile.Add(ioFileName, new List<long>());

                                dictionaryRangeBlockHeightIoCacheFile[ioFileName].Add(blockHeight);
                            }
                        }

                        #endregion

                        if (dictionaryRangeBlockHeightIoCacheFile.Count > 0)
                        {
                            foreach (string ioFileName in dictionaryRangeBlockHeightIoCacheFile.GetList.Keys)
                            {

                                foreach (ClassBlockObject blockObject in await _dictionaryCacheIoIndexObject[ioFileName].GetIoListBlockDataInformationFromListBlockHeight(dictionaryRangeBlockHeightIoCacheFile[ioFileName], cancellationIoCache))
                                {

                                    if (blockObject != null)
                                        listBlockInformation.Add(blockObject.BlockHeight, blockObject);
                                }
                            }
                        }
                    }
                }
            }

            return listBlockInformation;
        }

        /// <summary>
        /// Retrieve back a block information object from the io cache object.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<ClassBlockObject> GetIoBlockInformationObject(long blockHeight, CancellationTokenSource cancellationIoCache)
        {
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            return _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? await _dictionaryCacheIoIndexObject[ioFileName].GetIoBlockDataInformationFromBlockHeight(blockHeight, cancellationIoCache) : null;
        }

        /// <summary>
        /// Retrieve back a block information object from the io cache object.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<int> GetIoBlockTransactionCount(long blockHeight, CancellationTokenSource cancellationIoCache)
        {
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            return _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? await _dictionaryCacheIoIndexObject[ioFileName].GetIoBlockTransactionCountFromBlockHeight(blockHeight, cancellationIoCache) : 0;
        }

        /// <summary>
        /// Retrieve back a block object from the io cache object.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive">Keep alive or not the data retrieved into the active memory.</param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<ClassBlockObject> GetIoBlockObject(long blockHeight, bool keepAlive, bool clone, CancellationTokenSource cancellationIoCache)
        {
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            return _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? await _dictionaryCacheIoIndexObject[ioFileName].GetIoBlockDataFromBlockHeight(blockHeight, keepAlive, clone, cancellationIoCache) : null;
        }

        /// <summary>
        /// Push or update a block object to the io cache object.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> PushOrUpdateIoBlockObject(ClassBlockObject blockObject, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            string ioFileName = GetIoFileNameFromBlockHeight(blockObject.BlockHeight);

            bool existOrInsertIndex = _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? true : (await InitializeNewCacheIoIndex(ioFileName)).Item1;

            return existOrInsertIndex ? await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateIoBlockData(blockObject, keepAlive, cancellationIoCache) : false;
        }

        /// <summary>
        /// Push directly a list of object to insert/update directly to each io cache file indexed.
        /// </summary>
        /// <param name="blockObjectList"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> PushOrUpdateListIoBlockObject(List<ClassBlockObject> blockObjectList, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            bool result = true;

            using (DisposableDictionary<string, List<ClassBlockObject>> listBlockObject = new DisposableDictionary<string, List<ClassBlockObject>>())
            {

                // Generate a list of block object linked to the io file index.
                foreach (var blockObject in blockObjectList)
                {

                    string ioFileName = GetIoFileNameFromBlockHeight(blockObject.BlockHeight);

                    if (!_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                    {
                        Tuple<bool, HashSet<long>> resultInit = await InitializeNewCacheIoIndex(ioFileName);

                        if (!resultInit.Item1)
                        {
                            result = false;

                            break;
                        }
                    }

                    if (!listBlockObject.ContainsKey(ioFileName))
                    {
                        listBlockObject.Add(ioFileName, new List<ClassBlockObject>()
                        {
                            blockObject
                        });
                    }
                    else
                        listBlockObject[ioFileName].Add(blockObject);
                }

                // Much faster insert/update.
                if (listBlockObject.Count > 0 && result)
                {
                    string[] ioFileNameArray = listBlockObject.GetList.Keys.ToArray();
                    int totalTaskToDo = ioFileNameArray.Length;
                    int totalTaskDone = 0;

                    using (CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationIoCache.Token))
                    {
                        bool cancel = false;

                        foreach (string ioFileName in ioFileNameArray)
                        {
                            if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && ioFileNameArray.Length > 1)
                            {
                                await Task.Factory.StartNew(async () =>
                                {

                                    if (!cancel)
                                    {
                                        if (!await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateListIoBlockData(listBlockObject[ioFileName], keepAlive, cancellation))
                                            result = false;
                                    }

                                    totalTaskDone++;
                                }, cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                            }
                            else
                            {

                                if (!cancel)
                                {
                                    if (result)
                                    {
                                        if (!await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateListIoBlockData(listBlockObject[ioFileName], keepAlive, cancellation))
                                        {
                                            result = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }


                        if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && ioFileNameArray.Length > 1)
                        {
                            while (totalTaskDone < totalTaskToDo)
                            {
                                if (cancel || !result)
                                    break;

                                if (cancellationIoCache != null)
                                {
                                    try
                                    {
                                        await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay, cancellationIoCache.Token);
                                    }
                                    catch
                                    {
                                        break;
                                    }
                                }
                                else
                                    await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Try to delete io block data object from the io cache.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> TryDeleteIoBlockObject(long blockHeight, CancellationTokenSource cancellationIoCache)
        {
            bool result = true;

            bool semaphoreUsed = false;

            try
            {

                await _semaphoreIoCacheIndexAccess.WaitAsync(cancellationIoCache.Token);
                semaphoreUsed = true;

                string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                result = _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? await _dictionaryCacheIoIndexObject[ioFileName].TryDeleteIoBlockData(blockHeight, cancellationIoCache) : false;

            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreIoCacheIndexAccess.Release();
            }

            return result;
        }

        /// <summary>
        /// Check if the io cache system contain the block height indexed.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> ContainIoBlockHeight(long blockHeight, CancellationTokenSource cancellationIoCache)
        {
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            return _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? await _dictionaryCacheIoIndexObject[ioFileName].ContainsIoBlockHeight(blockHeight, cancellationIoCache) : false;
        }

        /// <summary>
        /// Insert or update a block transaction directly to the io cache system.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> InsertOrUpdateBlockTransactionObject(ClassBlockTransaction blockTransaction, long blockHeight, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                blockHeight = blockTransaction.TransactionBlockHeightInsert;

            // If the block height is provided.
            if (blockHeight >= BlockchainSetting.GenesisBlockHeight)
            {
                string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                return _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateTransactionOnIoBlockData(blockTransaction, blockHeight, keepAlive, cancellationIoCache) : false;
            }

            return false;
        }


        /// <summary>
        /// Insert or update a block transaction directly to the io cache system.
        /// </summary>
        /// <param name="listBlockTransaction"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> InsertOrUpdateListBlockTransactionObject(List<ClassBlockTransaction> listBlockTransaction, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            bool result = true;

            using (DisposableDictionary<string, List<ClassBlockTransaction>> listBlockTransactionToInsertOrUpdate = new DisposableDictionary<string, List<ClassBlockTransaction>>())
            {

                // Generate a list of block transaction object linked to the io file index.
                foreach (var blockTransaction in listBlockTransaction)
                {

                    string ioFileName = GetIoFileNameFromBlockHeight(blockTransaction.TransactionObject.BlockHeightTransaction);

                    if (!_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                    {
                        Tuple<bool, HashSet<long>> resultInit = await InitializeNewCacheIoIndex(ioFileName);

                        if (!resultInit.Item1)
                        {
                            result = false;
                            break;
                        }
                    }

                    if (!listBlockTransactionToInsertOrUpdate.ContainsKey(ioFileName))
                    {
                        listBlockTransactionToInsertOrUpdate.Add(ioFileName, new List<ClassBlockTransaction>()
                        {
                             blockTransaction
                        });
                    }
                    else
                        listBlockTransactionToInsertOrUpdate[ioFileName].Add(blockTransaction);
                }

                // Much faster insert/update.
                if (listBlockTransactionToInsertOrUpdate.Count > 0 && result)
                {
                    string[] ioFileNameArray = listBlockTransactionToInsertOrUpdate.GetList.Keys.ToArray();
                    int totalTaskToDo = ioFileNameArray.Length;
                    int totalTaskDone = 0;

                    using (CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationIoCache.Token))
                    {
                        bool cancel = false;

                        foreach (string ioFileName in ioFileNameArray)
                        {
                            if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && ioFileNameArray.Length > 1)
                            {
                                try
                                {
                                    await Task.Factory.StartNew(async () =>
                                    {

                                        if (!await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateListIoBlockTransactionData(listBlockTransactionToInsertOrUpdate[ioFileName], keepAlive, cancellation))
                                            result = false;

                                        totalTaskDone++;
                                    }, cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                                }
                                catch
                                {
                                    totalTaskDone++;
                                }
                            }
                            else
                            {
                                if (!await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateListIoBlockTransactionData(listBlockTransactionToInsertOrUpdate[ioFileName], keepAlive, cancellation))
                                {
                                    result = false;
                                    break;
                                }
                            }
                        }

                        if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && ioFileNameArray.Length > 1)
                        {
                            while (totalTaskDone < totalTaskToDo)
                            {
                                if (cancel || !result)
                                    break;

                                try
                                {
                                    await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay, cancellationIoCache.Token);
                                }
                                catch
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check if a transaction hash exist on io blocks cached.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> CheckTransactionHashExistOnIoBlockCached(string transactionHash, long blockHeight, CancellationTokenSource cancellationIoCache)
        {
            bool result = false;

            long blockHeightFromTransactionHash = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

            if (blockHeight != blockHeightFromTransactionHash || blockHeight < BlockchainSetting.GenesisBlockHeight)
                blockHeight = blockHeightFromTransactionHash;

            if (_dictionaryCacheIoIndexObject.Count > 0)
            {
                string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                return _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? await _dictionaryCacheIoIndexObject[ioFileName].ContainIoBlockTransactionHash(transactionHash, blockHeight, cancellationIoCache) : false;
            }

            return result;
        }

        /// <summary>
        /// Retrieve every block transactions from a block object cached.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<SortedList<string, ClassBlockTransaction>> GetBlockTransactionListFromBlockHeightTarget(long blockHeight, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            SortedList<string, ClassBlockTransaction> listBlockTransactions = new SortedList<string, ClassBlockTransaction>();


            if (blockHeight >= BlockchainSetting.GenesisBlockHeight)
            {
                string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                {
                    ClassBlockObject blockObject = await _dictionaryCacheIoIndexObject[ioFileName].GetIoBlockDataFromBlockHeight(blockHeight, keepAlive, false, cancellationIoCache);

                    if (blockObject != null)
                        return new SortedList<string, ClassBlockTransaction>(blockObject.BlockTransactions.ToDictionary(x => x.Key, x => x.Value));
                }
            }


            return listBlockTransactions;
        }

        /// <summary>
        /// Retrieve every block transactions by a list of transaction hash a block height from the cache.
        /// </summary>
        /// <param name="listTransactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive"></param>
        /// <returns></returns>
        public async Task<List<ClassBlockTransaction>> GetListBlockTransactionFromListTransactionHashAndBlockHeightTarget(List<string> listTransactionHash, long blockHeight, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            List<ClassBlockTransaction> listBlockTransaction = new List<ClassBlockTransaction>();


            if (blockHeight >= BlockchainSetting.GenesisBlockHeight)
            {

                string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                {
                    ClassBlockObject blockObject = await _dictionaryCacheIoIndexObject[ioFileName].GetIoBlockDataFromBlockHeight(blockHeight, keepAlive, false, cancellationIoCache);

                    if (blockObject != null)
                    {
                        foreach(string transactionHash in listTransactionHash)
                        {
                            if (blockObject.BlockTransactions.ContainsKey(transactionHash))
                                listBlockTransaction.Add(blockObject.BlockTransactions[transactionHash].Clone());
                        }
                    }
                }
            }

            return listBlockTransaction;
        }

        /// <summary>
        /// Insert or update a block transaction directly to the io cache system.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<ClassBlockTransaction> GetBlockTransactionFromTransactionHashOnIoBlockCached(string transactionHash, long blockHeight, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            // If the block height is provided.
            if (blockHeight >= BlockchainSetting.GenesisBlockHeight)
            {
                string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                return _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? await _dictionaryCacheIoIndexObject[ioFileName].GetBlockTransactionFromIoBlockHeightByTransactionHash(blockHeight, transactionHash, keepAlive, cancellationIoCache) : null;
            }

            return null;
        }

        /// <summary>
        /// Retrieve back a list of block object between a range.
        /// </summary>
        /// <param name="blockHeightStart"></param>
        /// <param name="blockHeightEnd"></param>
        /// <param name="listBlockHeightAlreadyCached"></param>
        /// <param name="listBlockAlreadyCached"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<SortedList<long, ClassBlockObject>> GetBlockObjectListFromBlockHeightRange(long blockHeightStart, long blockHeightEnd, HashSet<long> listBlockHeightAlreadyCached, SortedList<long, ClassBlockObject> listBlockAlreadyCached, bool keepAlive, bool clone, CancellationTokenSource cancellationIoCache)
        {
            using (DisposableDictionary<string, List<long>> listBlockHeightIndexedByIoFile = new DisposableDictionary<string, List<long>>())
            {
                bool error = false;

                for (long i = blockHeightStart - 1; i < blockHeightEnd; i++)
                {
                    long blockHeight = i + 1;

                    if (blockHeight <= blockHeightEnd)
                    {
                        string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                        if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                        {
                            if (!listBlockHeightIndexedByIoFile.ContainsKey(ioFileName))
                                listBlockHeightIndexedByIoFile.Add(ioFileName, new List<long>());

                            if (!listBlockAlreadyCached.ContainsKey(blockHeight))
                                listBlockHeightIndexedByIoFile[ioFileName].Add(blockHeight);
                        }
                    }
                    else
                        break;
                }

                if (listBlockHeightIndexedByIoFile.Count > 0)
                {
                    if (!error)
                    {
                        string[] ioFileNameArray = listBlockHeightIndexedByIoFile.GetList.Keys.ToArray();

                        int totalTaskToDo = ioFileNameArray.Length;
                        int totalTaskDone = 0;
                        bool cancel = false;

                        if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && ioFileNameArray.Length > 1)
                        {
                            using (CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationIoCache.Token))
                            {

                                foreach (string ioFileName in ioFileNameArray)
                                {
                                    try
                                    {
                                        await Task.Factory.StartNew(async () =>
                                        {

                                            if (listBlockHeightIndexedByIoFile[ioFileName].Count > 0)
                                            {
                                                if (!cancel)
                                                {
                                                    using (DisposableList<ClassBlockObject> listBlockObject = await _dictionaryCacheIoIndexObject[ioFileName].GetIoListBlockDataFromListBlockHeight(new HashSet<long>(listBlockHeightIndexedByIoFile[ioFileName]), keepAlive, clone, cancellationIoCache))
                                                    {
                                                        if (listBlockObject.Count > 0)
                                                        {
                                                            foreach (ClassBlockObject blockObject in listBlockObject.GetAll)
                                                            {

                                                                if (blockObject != null)
                                                                {
                                                                    if (!listBlockAlreadyCached.ContainsKey(blockObject.BlockHeight) && !listBlockHeightAlreadyCached.Contains(blockObject.BlockHeight))
                                                                        listBlockAlreadyCached.Add(blockObject.BlockHeight, blockObject);
                                                                }
                                                            }

                                                        }
                                                    }
                                                }

                                                totalTaskDone++;
                                            }

                                        }, cancellation.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
                                    }
                                    catch
                                    {
                                        totalTaskDone++;
                                    }
                                }

                                while (totalTaskDone < totalTaskToDo)
                                {
                                    if (cancel)
                                        break;

                                    if (cancellationIoCache != null)
                                    {
                                        try
                                        {
                                            await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay, cancellationIoCache.Token);
                                        }
                                        catch
                                        {
                                            break;
                                        }
                                    }
                                    else
                                        await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay);
                                }
                            }
                        }
                        else
                        {
                            foreach (string ioFileName in ioFileNameArray)
                            {

                                if (listBlockHeightIndexedByIoFile[ioFileName].Count > 0)
                                {
                                    if (!cancel)
                                    {
                                        using (DisposableList<ClassBlockObject> listBlockObject = await _dictionaryCacheIoIndexObject[ioFileName].GetIoListBlockDataFromListBlockHeight(new HashSet<long>(listBlockHeightIndexedByIoFile[ioFileName]), keepAlive, clone, cancellationIoCache))
                                        {
                                            if (listBlockObject.Count > 0)
                                            {
                                                foreach (ClassBlockObject blockObject in listBlockObject.GetAll)
                                                {

                                                    if (blockObject != null)
                                                    {
                                                        if (!listBlockAlreadyCached.ContainsKey(blockObject.BlockHeight) && !listBlockHeightAlreadyCached.Contains(blockObject.BlockHeight))
                                                            listBlockAlreadyCached.Add(blockObject.BlockHeight, blockObject);
                                                    }
                                                }

                                            }
                                        }
                                    }
                                    else
                                        break;
                                }
                            }
                        }

                    }
                }


                return listBlockAlreadyCached;
            }
        }

        /// <summary>
        /// Return the block height start and the block height end indexed.
        /// </summary>
        /// <returns></returns>
        public async Task<Tuple<long, long>> GetIoCacheBlockIndexes(CancellationTokenSource cancellation)
        {
            using (DisposableList<string> listIndexes = new DisposableList<string>(true, 0, _dictionaryCacheIoIndexObject.Keys.ToList()))
            {
                var indexListFirst = await _dictionaryCacheIoIndexObject[listIndexes.GetList[0]].GetIoBlockHeightListIndexed(cancellation);
                var indexListLast = await _dictionaryCacheIoIndexObject[listIndexes.GetList[listIndexes.Count -1]].GetIoBlockHeightListIndexed(cancellation);

                return new Tuple<long, long>(indexListFirst.First(), indexListLast.Last());
            }
        }

        #endregion

        #region Functions dedicated to io files indexing.

        /// <summary>
        /// Return the io file name depending of the block height and the limit of max block inside a io cache file.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        private string GetIoFileNameFromBlockHeight(long blockHeight)
        {
            return ((blockHeight / _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskMaxBlockPerFile) + IoFileExtension);
        }

        /// <summary>
        /// Return the total memory usage from the io cache system.
        /// </summary>
        /// <returns></returns>
        public long GetIoCacheSystemMemoryConsumption(CancellationTokenSource cancellation, out int totalBlockKeepAlive)
        {
            totalBlockKeepAlive = 0; // Default.

            if (_dictionaryCacheIoIndexObject.Count > 0)
            {
                long totalMemoryUsagePendingCalculation = 0;

                foreach (string ioFileName in _dictionaryCacheIoIndexObject.Keys.ToArray())
                {
                    totalMemoryUsagePendingCalculation += _dictionaryCacheIoIndexObject[ioFileName].GetIoMemoryUsage(cancellation, out int indexTotalBlockKeepAlive);
                    totalBlockKeepAlive += indexTotalBlockKeepAlive;
                }

                _totalIoCacheSystemMemoryUsage = totalMemoryUsagePendingCalculation;
            }


            return _totalIoCacheSystemMemoryUsage;
        }

        #endregion
    }
}
