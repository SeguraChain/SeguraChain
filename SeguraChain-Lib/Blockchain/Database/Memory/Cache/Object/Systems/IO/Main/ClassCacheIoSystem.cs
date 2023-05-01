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
#if DEBUG
            Debug.WriteLine("Start to initialize the cache system.");
#endif
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

#if DEBUG
            Debug.WriteLine("Cache system initialized.");
#endif
            return new Tuple<bool, HashSet<long>>(true, listBlockHeight);
        }

        /// <summary>
        /// Initialize a new cache io index.
        /// </summary>
        /// <param name="ioFileName"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, HashSet<long>>> InitializeNewCacheIoIndex(string ioFileName)
        {
#if DEBUG
            Debug.WriteLine("Initialize new io cache index file: " + ioFileName);
#endif
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
#if DEBUG
            Debug.WriteLine("Initialize new io cache index file: " + ioFileName + " done.");
#endif
            return new Tuple<bool, HashSet<long>>(true, listBlockHeight);
        }

        /// <summary>
        /// Purge the io cache system.
        /// </summary>
        /// <returns></returns>
        public async Task PurgeCacheIoSystem(CancellationTokenSource cancellation)
        {
#if DEBUG
            Debug.WriteLine("Purge cache IO System in pending.");
#endif
            if (_dictionaryCacheIoIndexObject.Count > 0)
            {
                string[] ioFileNameArray = _dictionaryCacheIoIndexObject.Keys.ToArray();
                int totalTaskToDo = ioFileNameArray.Length;
                int totalTaskDone = 0;

                foreach (string ioFileName in ioFileNameArray)
                {
                    if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask)
                    {
                        TaskManager.TaskManager.InsertTask(new Action(async () =>
                        {
                            try
                            {
                                await _dictionaryCacheIoIndexObject[ioFileName].PurgeIoBlockDataMemory(true, cancellation, 0, false);
                            }
                            catch
                            {
                                // The task has been cancelled.
                            }
                            totalTaskDone++;
                        }), 0, cancellation);
                    }
                    else
                        await _dictionaryCacheIoIndexObject[ioFileName].PurgeIoBlockDataMemory(false, cancellation, 0, false);
                }

                if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask)
                {
                    while (totalTaskToDo > totalTaskDone && !cancellation.IsCancellationRequested)
                        await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay);
                }

                long totalIoCacheMemoryUsage = GetIoCacheSystemMemoryConsumption(cancellation, out int totalBlockKeepAlive);


#if DEBUG
                Debug.WriteLine("Cache IO Index Object - Total block(s) keep alive: " + totalBlockKeepAlive + " | Total Memory usage from the cache: " + ClassUtility.ConvertBytesToMegabytes(totalIoCacheMemoryUsage));
#endif
                ClassLog.WriteLine("Cache IO Index Object - Total block(s) keep alive: " + totalBlockKeepAlive + " | Total Memory usage from the cache: " + ClassUtility.ConvertBytesToMegabytes(totalIoCacheMemoryUsage), ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            }

#if DEBUG
            Debug.WriteLine("Purge cache IO System in done.");
#endif
        }

        /// <summary>
        /// Clean the io cache system.
        /// </summary>
        public async Task CleanCacheIoSystem(CancellationTokenSource cancellationAccess)
        {
#if DEBUG
            Debug.WriteLine("Clean cache IO System in pending.");
#endif
            bool semaphoreUsed = false;

            try
            {
                semaphoreUsed = await _semaphoreIoCacheIndexAccess.TryWaitAsync(cancellationAccess);

                if (_dictionaryCacheIoIndexObject.Count > 0 && semaphoreUsed)
                {
                    string[] ioFileNameArray = _dictionaryCacheIoIndexObject.Keys.ToArray();
                    int totalTaskToDo = ioFileNameArray.Length;
                    int totalTaskDone = 0;
                    CancellationTokenSource cancellation = new CancellationTokenSource();

                    foreach (string ioFileName in ioFileNameArray)
                    {
                        if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && ioFileNameArray.Length > 1)
                        {
                            TaskManager.TaskManager.InsertTask(new Action(() =>
                            {
                                try
                                {
                                    string ioFilePath = _blockchainDatabaseSetting.GetBlockchainCacheDirectoryPath + ioFileName;
                                    _dictionaryCacheIoIndexObject[ioFileName].CloseLockStream();
                                    _dictionaryCacheIoIndexObject.Remove(ioFilePath);
                                    File.Delete(ioFilePath);
                                }
                                catch
                                {
                                    // The task has been cancelled or the file stream is locked, closed.
                                }
                                totalTaskDone++;
                            }), 0, cancellation);
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
                            await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay);
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
#if DEBUG
            Debug.WriteLine("Purge cache IO System in done.");
#endif
        }

        /// <summary>
        /// Do a purge of the io cache system from a io cache file index to except.
        /// </summary>
        /// <param name="ioFileNameSource"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> DoPurgeFromIoCacheIndex(string ioFileNameSource, long memoryAsked, CancellationTokenSource cancellation)
        {
#if DEBUG
            Debug.WriteLine("Purge cache IO file index " + ioFileNameSource + " in pending.");
#endif
            if (memoryAsked == 0)
            {
#if DEBUG
                Debug.WriteLine("Purge cache IO System index " + ioFileNameSource + " done.");
#endif
                return true;
            }
            long totalMemoryRetrieved = 0;

            foreach (string ioFileFileIndex in _dictionaryCacheIoIndexObject.Keys.ToArray())
            {

                if (ioFileFileIndex != ioFileNameSource)
                {
                    long restMemoryToTask = memoryAsked - totalMemoryRetrieved;

                    if (GetIoCacheSystemMemoryConsumption(cancellation, out _) + restMemoryToTask <= _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxActiveMemoryAllocationFromCache)
                    {
#if DEBUG
                        Debug.WriteLine("Purge cache IO System index " + ioFileNameSource + " done.");
#endif
                        return true;
                    }
                    long totalMemoryFreeRetrieved = await _dictionaryCacheIoIndexObject[ioFileFileIndex].PurgeIoBlockDataMemory(false, cancellation, restMemoryToTask, true);

                    totalMemoryRetrieved += totalMemoryFreeRetrieved;

                    if (totalMemoryRetrieved >= memoryAsked)
                    {
#if DEBUG
                        Debug.WriteLine("Purge cache IO System index " + ioFileNameSource + " done.");
#endif
                        return true;
                    }
                }
            }

#if DEBUG
            Debug.WriteLine("Purge cache IO System index " + ioFileNameSource + " failed.");
#endif

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
#if DEBUG
            Debug.WriteLine("Get IO List block information count: " + listBlockHeight.Count);
#endif
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

#if DEBUG
            Debug.WriteLine("Get IO List block information count: " + listBlockHeight.Count + " done.");
#endif
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
#if DEBUG
            Debug.WriteLine("Get IO block information block height: " + blockHeight);
#endif
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
            {

                var result = await _dictionaryCacheIoIndexObject[ioFileName].GetIoBlockDataInformationFromBlockHeight(blockHeight, cancellationIoCache);
#if DEBUG
                Debug.WriteLine("Get IO block information block height: " + blockHeight + " " + (result != null ? "done" : "failed") + ".");
#endif
                return result;
            }

#if DEBUG
            Debug.WriteLine("Get IO block information block height: " + blockHeight + " failed.");
#endif
            return null;
        }

        /// <summary>
        /// Retrieve back a block information object from the io cache object.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<int> GetIoBlockTransactionCount(long blockHeight, CancellationTokenSource cancellationIoCache)
        {
#if DEBUG
            Debug.WriteLine("Get IO block transaction count block height: " + blockHeight + " in pending.");
#endif
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
            {
                var result = await _dictionaryCacheIoIndexObject[ioFileName].GetIoBlockTransactionCountFromBlockHeight(blockHeight, cancellationIoCache);
#if DEBUG
                Debug.WriteLine("Get IO block transaction count block height: " + blockHeight + " done.");
#endif
                return result;
            }
#if DEBUG
            Debug.WriteLine("Get IO block transaction count block height: " + blockHeight + " failed.");
#endif
            return 0;
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
#if DEBUG
            Debug.WriteLine("Get IO block object block height: " + blockHeight + " in pending.");
#endif
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
            {
                var result = await _dictionaryCacheIoIndexObject[ioFileName].GetIoBlockDataFromBlockHeight(blockHeight, keepAlive, clone, cancellationIoCache);
#if DEBUG
                Debug.WriteLine("Get IO block object block height: " + blockHeight + " " + (result != null ? "done" : "failed") + ".");
#endif
                return result;
            }
#if DEBUG
            Debug.WriteLine("Get IO block object block height: " + blockHeight + " failed.");
#endif
            return null;
        }

        /// <summary>
        /// Push or update a block object to the io cache object.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> PushOrUpdateIoBlockObject(ClassBlockObject blockObject, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
#if DEBUG
            Debug.WriteLine("Push IO block object block height: " + blockObject.BlockHeight + " in pending.");
#endif
            string ioFileName = GetIoFileNameFromBlockHeight(blockObject.BlockHeight);

            bool existOrInsertIndex = _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? true : (await InitializeNewCacheIoIndex(ioFileName)).Item1;

            if (existOrInsertIndex)
            {
                var result = await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateIoBlockData(blockObject, keepAlive, cancellationIoCache);
#if DEBUG
                Debug.WriteLine("Push IO block object block height: " + blockObject.BlockHeight + " " + (result ? "done" : "failed") + ".");
#endif
                return result;
            }
#if DEBUG
            Debug.WriteLine("Push IO block object block height: " + blockObject.BlockHeight + " failed.");
#endif
            return false;
        }

        /// <summary>
        /// Push directly a list of object to insert/update directly to each io cache file indexed.
        /// </summary>
        /// <param name="blockObjectList"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> PushOrUpdateListIoBlockObject(List<ClassBlockObject> blockObjectList, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
#if DEBUG
            Debug.WriteLine("Push List of block object count: " + blockObjectList.Count + " in pending.");
#endif
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
                                TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {
                                    try
                                    {
                                        if (!cancel)
                                            result = await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateListIoBlockData(listBlockObject[ioFileName], keepAlive, cancellation);

                                    }
                                    catch
                                    {
                                        // The task has been cancelled.
                                    }

                                    totalTaskDone++;
                                }), 0, cancellation);
                            }
                            else
                            {
                                if (!cancel && result)
                                {
                                    if (!await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateListIoBlockData(listBlockObject[ioFileName], keepAlive, cancellation))
                                    {
                                        result = false;
                                        break;
                                    }
                                }
                            }
                        }


                        if (_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && ioFileNameArray.Length > 1)
                        {
                            while (totalTaskDone < totalTaskToDo)
                            {
                                if (cancel || !result || !cancellationIoCache.IsCancellationRequested)
                                    break;

                                await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay);
                            }
                        }
                    }
                }
            }

#if DEBUG
            Debug.WriteLine("Push List of block object count: " + blockObjectList.Count + " " + (result ? "done" : "failed") + ".");
#endif
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
#if DEBUG
            Debug.WriteLine("Try delete of block object block height: " + blockHeight + " in pending.");
#endif
            bool result = true;

            bool semaphoreUsed = false;

            try
            {

                semaphoreUsed = await _semaphoreIoCacheIndexAccess.TryWaitAsync(cancellationIoCache);

                if (semaphoreUsed)
                {
                    string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                    result = _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? await _dictionaryCacheIoIndexObject[ioFileName].TryDeleteIoBlockData(blockHeight, cancellationIoCache) : false;
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreIoCacheIndexAccess.Release();
            }

#if DEBUG
            Debug.WriteLine("Try delete of block object block height: " + blockHeight + " " + (result ? "done" : "failed") + ".");
#endif
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
#if DEBUG
            Debug.WriteLine("Try check contains of block object block height: " + blockHeight + " in pending.");
#endif
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
            {
                var result = await _dictionaryCacheIoIndexObject[ioFileName].ContainsIoBlockHeight(blockHeight, cancellationIoCache);
#if DEBUG
                Debug.WriteLine("Try check contains of block object block height: " + blockHeight + " " + (result ? "done" : "failed") + ".");
#endif
                return result;
            }

#if DEBUG
            Debug.WriteLine("Try check contains of block object block height: " + blockHeight + " failed.");
#endif
            return false;
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
#if DEBUG
            Debug.WriteLine("Insert of transaction of block transaction, block height: " + blockHeight + " in pending.");
#endif
            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                blockHeight = blockTransaction.TransactionBlockHeightInsert;

            // If the block height is provided.
            if (blockHeight >= BlockchainSetting.GenesisBlockHeight)
            {
                string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                {
                    var result = await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateTransactionOnIoBlockData(blockTransaction, blockHeight, keepAlive, cancellationIoCache);
#if DEBUG
                    Debug.WriteLine("Insert of transaction of block transaction, block height: " + blockHeight + " " + (result ? "done" : "failed") + ".");
#endif
                }
            }

#if DEBUG
            Debug.WriteLine("Insert of transaction of block transaction, block height: " + blockHeight + " failed.");
#endif
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
#if DEBUG
            Debug.WriteLine("Insert list of transaction of transaction count: " + listBlockTransaction.Count + " in pending.");
#endif
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
                                TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {
                                    if (!cancel)
                                    {
                                        try
                                        {
                                            result = await _dictionaryCacheIoIndexObject[ioFileName].PushOrUpdateListIoBlockTransactionData(listBlockTransactionToInsertOrUpdate[ioFileName], keepAlive, cancellation);
                                        }

#if !DEBUG
                                        catch
                                        {

#else
                                        catch (Exception error)
                                        {
                                            Debug.WriteLine("Failed to push the list of io block transaction data to the cache: " + error.Message);
#endif
                                        }
                                    }
                                    totalTaskDone++;

                                }), 0, cancellation);
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
                            while (totalTaskDone < totalTaskToDo && !cancel && result || !cancellationIoCache.IsCancellationRequested)
                                await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay);
                        }
                    }
                }
            }

#if DEBUG
            Debug.WriteLine("Insert list of transaction of transaction count: " + listBlockTransaction.Count + " " + (result ? "done" : "failed") + ".");
#endif
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
#if DEBUG
            Debug.WriteLine("Check transaction exist " + transactionHash + " in pending.");
#endif
            bool result = false;

            long blockHeightFromTransactionHash = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

            if (blockHeight != blockHeightFromTransactionHash || blockHeight < BlockchainSetting.GenesisBlockHeight)
                blockHeight = blockHeightFromTransactionHash;

            if (_dictionaryCacheIoIndexObject.Count > 0)
            {
                string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                return _dictionaryCacheIoIndexObject.ContainsKey(ioFileName) ? await _dictionaryCacheIoIndexObject[ioFileName].ContainIoBlockTransactionHash(transactionHash, blockHeight, cancellationIoCache) : false;
            }
#if DEBUG
            Debug.WriteLine("Check transaction exist " + transactionHash + " " + (result ? "done" : "failed") + ".");
#endif
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
#if DEBUG
            Debug.WriteLine("Get block transaction list from block height " + blockHeight + " in pending.");
#endif
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

#if DEBUG
            Debug.WriteLine("Get block transaction list from block height " + blockHeight + " " + (listBlockTransactions.Count > 0 ? "done" : "failed") + ".");
#endif

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
#if DEBUG
            Debug.WriteLine("Get block transaction list from an hash list and from block height " + blockHeight + " in pending.");
#endif
            List<ClassBlockTransaction> listBlockTransaction = new List<ClassBlockTransaction>();


            if (blockHeight >= BlockchainSetting.GenesisBlockHeight)
            {

                string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                {
                    ClassBlockObject blockObject = await _dictionaryCacheIoIndexObject[ioFileName].GetIoBlockDataFromBlockHeight(blockHeight, keepAlive, false, cancellationIoCache);

                    if (blockObject != null)
                    {
                        foreach (string transactionHash in listTransactionHash)
                        {
                            if (blockObject.BlockTransactions.ContainsKey(transactionHash))
                                listBlockTransaction.Add(blockObject.BlockTransactions[transactionHash].Clone());
                        }
                    }
                }
            }

#if DEBUG
            Debug.WriteLine("Get block transaction list from an hash list and from block height " + blockHeight + " " + (listBlockTransaction.Count > 0 ? "done" : "failed") + ".");
#endif
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
#if DEBUG
            Debug.WriteLine("Get block transaction list from block height " + blockHeight + " and transaction hash " + transactionHash + " in pending.");
#endif
            // If the block height is provided.
            if (blockHeight >= BlockchainSetting.GenesisBlockHeight)
            {
                string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                {
                    var result = await _dictionaryCacheIoIndexObject[ioFileName].GetBlockTransactionFromIoBlockHeightByTransactionHash(blockHeight, transactionHash, keepAlive, cancellationIoCache);
#if DEBUG
                    Debug.WriteLine("Get block transaction list from block height " + blockHeight + " and transaction hash " + transactionHash + " " + (result != null ? "done" : "failed") + ".");
#endif
                    return result;
                }
            }

#if DEBUG
            Debug.WriteLine("Get block transaction list from block height " + blockHeight + " and transaction hash " + transactionHash + " failed.");
#endif

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
#if DEBUG
            Debug.WriteLine("Get block object list  range from block height starting by " + blockHeightStart + " and block height ending by " + blockHeightEnd + " in pending.");
#endif
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
                                    TaskManager.TaskManager.InsertTask(new Action(async () =>
                                    {
                                        try
                                        {
                                            if (listBlockHeightIndexedByIoFile[ioFileName].Count > 0 && !cancel)
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
                                        }
#if !DEBUG
                                        catch
                                        {
#else
                                        catch (Exception exception)
                                        {
                                            Debug.WriteLine("Error to get the list of block data target by the block height range: " + blockHeightStart + "/" + blockHeightEnd + " | Exception: " + exception.Message);
#endif

                                            // The task has been cancelled.
                                        }

                                        totalTaskDone++;
                                    }), 0, cancellation);
                                }

                                while (totalTaskDone < totalTaskToDo && !cancel && !cancellationIoCache.IsCancellationRequested)
                                    await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay);

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


#if DEBUG
                Debug.WriteLine("Get block object list range from block height starting by " + blockHeightStart + " and block height ending by " + blockHeightEnd + " " + (listBlockAlreadyCached.Count > 0 ? "done" : "failed") + ".");
#endif
                return listBlockAlreadyCached;
            }
        }

        /// <summary>
        /// Return the block height start and the block height end indexed.
        /// </summary>
        /// <returns></returns>
        public async Task<Tuple<long, long>> GetIoCacheBlockIndexes(CancellationTokenSource cancellation)
        {
#if DEBUG
            Debug.WriteLine("Get IO Cache block indexes in pending.");
#endif
            using (DisposableList<string> listIndexes = new DisposableList<string>(true, 0, _dictionaryCacheIoIndexObject.Keys.ToList()))
            {
                var indexListFirst = await _dictionaryCacheIoIndexObject[listIndexes.GetList[0]].GetIoBlockHeightListIndexed(cancellation);
                var indexListLast = await _dictionaryCacheIoIndexObject[listIndexes.GetList[listIndexes.Count - 1]].GetIoBlockHeightListIndexed(cancellation);

#if DEBUG
                Debug.WriteLine("Get IO Cache block indexes done.");
#endif
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
#if DEBUG
            Debug.WriteLine("Get IO Cache memory consumption in pending.");
#endif
            totalBlockKeepAlive = 0; // Default.

            if (_dictionaryCacheIoIndexObject.Count > 0)
            {
                long totalMemoryUsagePendingCalculation = 0;

                using (DisposableList<string> listIoBlockFile = new DisposableList<string>(false, 0, _dictionaryCacheIoIndexObject.Keys.ToList()))
                {
                    foreach (string ioFileName in listIoBlockFile.GetList)
                    {
                        totalMemoryUsagePendingCalculation += _dictionaryCacheIoIndexObject[ioFileName].GetIoMemoryUsage(cancellation, out int indexTotalBlockKeepAlive);
                        totalBlockKeepAlive += indexTotalBlockKeepAlive;
                    }
                }

                _totalIoCacheSystemMemoryUsage = totalMemoryUsagePendingCalculation;
            }

#if DEBUG
            Debug.WriteLine("Get IO Cache memory consumption done " + _totalIoCacheSystemMemoryUsage);
#endif
            return _totalIoCacheSystemMemoryUsage;
        }

        #endregion
    }
}
