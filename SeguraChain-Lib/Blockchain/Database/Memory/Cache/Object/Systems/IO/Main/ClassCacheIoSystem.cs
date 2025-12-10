using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        #region Private Fields

        /// <summary>
        /// Objects and settings of the IO cache in disk mode.
        /// </summary>
        private const string IoFileExtension = ".ioblock";
        private ConcurrentDictionary<string, ClassCacheIoIndexObject> _dictionaryCacheIoIndexObject;
        private string _cacheIoDirectoryPath;

        /// <summary>
        /// Blockchain database settings.
        /// </summary>
        private ClassBlockchainDatabaseSetting _blockchainDatabaseSetting;

        /// <summary>
        /// Multithreading settings - verrouillage par fichier pour meilleure concurrence.
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks;
        private readonly SemaphoreSlim _globalLock;

        /// <summary>
        /// Cache de la consommation mémoire.
        /// </summary>
        private long _cachedMemoryUsage;
        private DateTime _lastMemoryCalculation;
        private readonly TimeSpan _memoryCalculationInterval = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Cache pour les noms de fichiers calculés.
        /// </summary>
        private readonly ConcurrentDictionary<long, string> _fileNameCache;

        /// <summary>
        /// Pool de listes pour réduire les allocations.
        /// </summary>
        private readonly ObjectPool<List<long>> _listPool;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="blockchainDatabaseSetting"></param>
        public ClassCacheIoSystem(ClassBlockchainDatabaseSetting blockchainDatabaseSetting)
        {
            _blockchainDatabaseSetting = blockchainDatabaseSetting;
            _cacheIoDirectoryPath = _blockchainDatabaseSetting.GetBlockchainCacheDirectoryPath;
            _dictionaryCacheIoIndexObject = new ConcurrentDictionary<string, ClassCacheIoIndexObject>();
            _fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
            _globalLock = new SemaphoreSlim(1, 1);
            _fileNameCache = new ConcurrentDictionary<long, string>();
            _listPool = new ObjectPool<List<long>>(() => new List<long>(100), list => list.Clear());
            _lastMemoryCalculation = DateTime.MinValue;
        }

        #endregion

        #region Manage IO Cache system

        /// <summary>
        /// Initialize the IO cache system.
        /// </summary>
        /// <returns></returns>
        public async Task<Tuple<bool, HashSet<long>>> InitializeCacheIoSystem()
        {
            HashSet<long> listBlockHeight = new HashSet<long>();

            if (!Directory.Exists(_cacheIoDirectoryPath))
            {
                Directory.CreateDirectory(_cacheIoDirectoryPath);
            }
            else
            {
                string[] cacheIoFileList = Directory.GetFiles(_cacheIoDirectoryPath, "*" + IoFileExtension);

                if (cacheIoFileList.Length > 0)
                {
                    // Initialisation parallèle des fichiers
                    var initTasks = cacheIoFileList.Select(ioFilePath =>
                        InitializeNewCacheIoIndex(Path.GetFileName(ioFilePath))
                    );

                    var results = await Task.WhenAll(initTasks);

                    foreach (var result in results)
                    {
                        if (result.Item1)
                        {
                            foreach (long blockHeight in result.Item2)
                                listBlockHeight.Add(blockHeight);
                        }
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

            if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                return new Tuple<bool, HashSet<long>>(true, listBlockHeight);

            var semaphore = GetFileLock(ioFileName);
            bool semaphoreUsed = false;

            try
            {
                await semaphore.WaitAsync();
                semaphoreUsed = true;

                // Double-check après acquisition du lock
                if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                    return new Tuple<bool, HashSet<long>>(true, listBlockHeight);

                ClassCacheIoIndexObject cacheIoIndexObject = new ClassCacheIoIndexObject(
                    ioFileName,
                    _blockchainDatabaseSetting,
                    this
                );

                Tuple<bool, HashSet<long>> result = await cacheIoIndexObject.InitializeIoCacheObjectAsync();

                if (result.Item1)
                {
                    if (_dictionaryCacheIoIndexObject.TryAdd(ioFileName, cacheIoIndexObject))
                    {
                        foreach (long blockHeight in result.Item2)
                            listBlockHeight.Add(blockHeight);
                    }
                    else
                    {
#if DEBUG
                        Debug.WriteLine("Cache IO System - Failed to add the new io cache file to dictionary: " + ioFileName);
#endif
                        ClassLog.WriteLine(
                            "Cache IO System - Failed to add the new io cache file to dictionary: " + ioFileName,
                            ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER,
                            ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY,
                            false,
                            ConsoleColor.Red
                        );
                    }
                }
                else
                {
#if DEBUG
                    Debug.WriteLine("Cache IO System - Failed to initialize the new io cache file: " + ioFileName);
#endif
                    ClassLog.WriteLine(
                        "Cache IO System - Failed to initialize the new io cache file: " + ioFileName,
                        ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER,
                        ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY,
                        false,
                        ConsoleColor.Red
                    );
                }

                return new Tuple<bool, HashSet<long>>(result.Item1, listBlockHeight);
            }
            finally
            {
                if (semaphoreUsed)
                    semaphore.Release();
            }
        }

        /// <summary>
        /// Purge the io cache system.
        /// </summary>
        /// <returns></returns>
        public async Task PurgeCacheIoSystem(CancellationTokenSource cancellation)
        {
            if (_dictionaryCacheIoIndexObject.Count == 0)
                return;

            // Purge en parallèle avec limite de concurrence
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
            var purgeTasks = new List<Task>();

            foreach (string ioFileName in _dictionaryCacheIoIndexObject.Keys)
            {
                await semaphore.WaitAsync(cancellation.Token);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
                        {
                            await indexObject.PurgeIoBlockDataMemory(false, cancellation, 0, false);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellation.Token);

                purgeTasks.Add(task);
            }

            await Task.WhenAll(purgeTasks);

            long totalIoCacheMemoryUsage = GetIoCacheSystemMemoryConsumption(cancellation, out int totalBlockKeepAlive);

#if DEBUG
            Debug.WriteLine($"Cache IO Index Object - Total block(s) keep alive: {totalBlockKeepAlive} | Total Memory usage from the cache: {ClassUtility.ConvertBytesToMegabytes(totalIoCacheMemoryUsage)}");
#endif
            ClassLog.WriteLine(
                $"Cache IO Index Object - Total block(s) keep alive: {totalBlockKeepAlive} | Total Memory usage from the cache: {ClassUtility.ConvertBytesToMegabytes(totalIoCacheMemoryUsage)}",
                ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER,
                ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY
            );
        }

        /// <summary>
        /// Clean the io cache system.
        /// </summary>
        public async Task CleanCacheIoSystem()
        {
            bool semaphoreUsed = false;

            try
            {
                await _globalLock.WaitAsync();
                semaphoreUsed = true;

                if (_dictionaryCacheIoIndexObject.Count > 0)
                {
                    foreach (string ioFileName in _dictionaryCacheIoIndexObject.Keys.ToArray())
                    {
                        string ioFilePath = _blockchainDatabaseSetting.GetBlockchainCacheDirectoryPath + ioFileName;

                        if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
                        {
                            indexObject.CloseLockStream();
                        }

                        try
                        {
                            if (File.Exists(ioFilePath))
                                File.Delete(ioFilePath);
                        }
                        catch (Exception error)
                        {
#if DEBUG
                            Debug.WriteLine($"Failed to delete the IO Cache file: {ioFileName}\nException: {error.Message}");
#endif
                        }

                        _dictionaryCacheIoIndexObject.TryRemove(ioFileName, out _);
                    }

                    try
                    {
                        Directory.Delete(_blockchainDatabaseSetting.GetBlockchainCacheDirectoryPath, true);
                    }
                    catch
                    {
                        // Ignored.
                    }

                    // Nettoyer les caches
                    _fileNameCache.Clear();
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _globalLock.Release();
            }
        }

        /// <summary>
        /// Do a purge of the io cache system from a io cache file index to except.
        /// </summary>
        /// <param name="ioFileNameSource"></param>
        /// <param name="memoryAsked"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> DoPurgeFromIoCacheIndex(string ioFileNameSource, long memoryAsked, CancellationTokenSource cancellation)
        {
            if (memoryAsked == 0)
                return true;

            long totalMemoryRetrieved = 0;
            var targetFiles = _dictionaryCacheIoIndexObject.Keys.Where(k => k != ioFileNameSource).ToArray();

            foreach (string ioFileFileIndex in targetFiles)
            {
                if (cancellation.IsCancellationRequested)
                    break;

                long restMemoryToTask = memoryAsked - totalMemoryRetrieved;

                if (GetIoCacheSystemMemoryConsumption(cancellation, out _) + restMemoryToTask <=
                    _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxActiveMemoryAllocationFromCache)
                {
                    return true;
                }

                if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileFileIndex, out var indexObject))
                {
                    long totalMemoryFreeRetrieved = await indexObject.PurgeIoBlockDataMemory(
                        false,
                        cancellation,
                        restMemoryToTask,
                        true
                    );

                    totalMemoryRetrieved += totalMemoryFreeRetrieved;

                    if (totalMemoryRetrieved >= memoryAsked)
                        return true;
                }
            }

            return totalMemoryRetrieved >= memoryAsked;
        }

        #endregion

        #region Get/Set/Update IO Cache data

        /// <summary>
        /// Get io list block information objects by a list of block height from io cache files.
        /// </summary>
        /// <param name="listBlockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<SortedList<long, ClassBlockObject>> GetIoListBlockInformationObject(
            DisposableList<long> listBlockHeight,
            CancellationTokenSource cancellationIoCache)
        {
            var listBlockInformation = new SortedList<long, ClassBlockObject>(listBlockHeight.Count);

            if (listBlockHeight.Count == 0)
                return listBlockInformation;

            var dictionaryRangeBlockHeightIoCacheFile = new Dictionary<string, List<long>>();

            try
            {
                // Groupement des block heights par fichier
                foreach (var blockHeight in listBlockHeight.GetList)
                {
                    string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

                    if (_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                    {
                        if (!dictionaryRangeBlockHeightIoCacheFile.TryGetValue(ioFileName, out var list))
                        {
                            list = _listPool.Get();
                            dictionaryRangeBlockHeightIoCacheFile[ioFileName] = list;
                        }
                        list.Add(blockHeight);
                    }
                }

                if (dictionaryRangeBlockHeightIoCacheFile.Count == 0)
                    return listBlockInformation;

                // Traitement parallèle des fichiers
                var fetchTasks = dictionaryRangeBlockHeightIoCacheFile.Select(kvp =>
                    FetchBlocksFromFile(kvp.Key, kvp.Value, cancellationIoCache)
                );

                var results = await Task.WhenAll(fetchTasks);

                // Agréger les résultats
                foreach (var blocks in results)
                {
                    foreach (var blockObject in blocks)
                    {
                        if (blockObject != null && !listBlockInformation.ContainsKey(blockObject.BlockHeight))
                        {
                            listBlockInformation.Add(blockObject.BlockHeight, blockObject);
                        }
                    }
                }
            }
            finally
            {
                // Retourner les listes au pool
                foreach (var list in dictionaryRangeBlockHeightIoCacheFile.Values)
                {
                    _listPool.Return(list);
                }
            }

            return listBlockInformation;
        }

        private async Task<List<ClassBlockObject>> FetchBlocksFromFile(
            string ioFileName,
            List<long> blockHeights,
            CancellationTokenSource cancellation)
        {
            var result = new List<ClassBlockObject>();

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                var blocks = await indexObject.GetIoListBlockDataInformationFromListBlockHeight(
                    blockHeights,
                    cancellation
                );

                result.AddRange(blocks.Where(b => b != null));
            }

            return result;
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

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                return await indexObject.GetIoBlockDataInformationFromBlockHeight(blockHeight, cancellationIoCache);
            }

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
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                return await indexObject.GetIoBlockTransactionCountFromBlockHeight(blockHeight, cancellationIoCache);
            }

            return 0;
        }

        /// <summary>
        /// Retrieve back a block object from the io cache object.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive">Keep alive or not the data retrieved into the active memory.</param>
        /// <param name="clone"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<ClassBlockObject> GetIoBlockObject(
            long blockHeight,
            bool keepAlive,
            bool clone,
            CancellationTokenSource cancellationIoCache)
        {
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                return await indexObject.GetIoBlockDataFromBlockHeight(blockHeight, keepAlive, clone, cancellationIoCache);
            }

            return null;
        }

        /// <summary>
        /// Push or update a block object to the io cache object.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> PushOrUpdateIoBlockObject(
            ClassBlockObject blockObject,
            bool keepAlive,
            CancellationTokenSource cancellationIoCache)
        {
            string ioFileName = GetIoFileNameFromBlockHeight(blockObject.BlockHeight);

            // Initialiser si nécessaire
            if (!_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
            {
                var result = await InitializeNewCacheIoIndex(ioFileName);
                if (!result.Item1)
                    return false;
            }

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                return await indexObject.PushOrUpdateIoBlockData(blockObject, keepAlive, cancellationIoCache);
            }

            return false;
        }

        /// <summary>
        /// Push directly a list of object to insert/update directly to each io cache file indexed.
        /// </summary>
        /// <param name="blockObjectList"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> PushOrUpdateListIoBlockObject(
            List<ClassBlockObject> blockObjectList,
            bool keepAlive,
            CancellationTokenSource cancellationIoCache)
        {
            if (blockObjectList == null || blockObjectList.Count == 0)
                return true;

            // Groupement par fichier
            var groupedBlocks = blockObjectList
                .GroupBy(b => GetIoFileNameFromBlockHeight(b.BlockHeight))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Initialiser les fichiers manquants en parallèle
            var missingFiles = groupedBlocks.Keys.Where(f => !_dictionaryCacheIoIndexObject.ContainsKey(f)).ToList();

            if (missingFiles.Count > 0)
            {
                var initTasks = missingFiles.Select(InitializeNewCacheIoIndex);
                var initResults = await Task.WhenAll(initTasks);

                if (initResults.Any(r => !r.Item1))
                    return false;
            }

            // Traitement parallèle avec limite de concurrence
            int maxParallel = _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && groupedBlocks.Count > 1
                ? Environment.ProcessorCount
                : 1;

            var semaphore = new SemaphoreSlim(maxParallel);
            var tasks = new List<Task<bool>>();

            foreach (var kvp in groupedBlocks)
            {
                await semaphore.WaitAsync(cancellationIoCache.Token);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        if (_dictionaryCacheIoIndexObject.TryGetValue(kvp.Key, out var indexObject))
                        {
                            return await indexObject.PushOrUpdateListIoBlockData(
                                kvp.Value,
                                keepAlive,
                                cancellationIoCache
                            );
                        }
                        return false;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationIoCache.Token);

                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }

        /// <summary>
        /// Try to delete io block data object from the io cache.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> TryDeleteIoBlockObject(long blockHeight, CancellationTokenSource cancellationIoCache)
        {
            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (!_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                return false;

            var semaphore = GetFileLock(ioFileName);
            bool semaphoreUsed = false;

            try
            {
                await semaphore.WaitAsync(cancellationIoCache.Token);
                semaphoreUsed = true;

                if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
                {
                    return await indexObject.TryDeleteIoBlockData(blockHeight, cancellationIoCache);
                }

                return false;
            }
            finally
            {
                if (semaphoreUsed)
                    semaphore.Release();
            }
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

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                return await indexObject.ContainsIoBlockHeight(blockHeight, cancellationIoCache);
            }

            return false;
        }

        /// <summary>
        /// Insert or update a block transaction directly to the io cache system.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> InsertOrUpdateBlockTransactionObject(
            ClassBlockTransaction blockTransaction,
            long blockHeight,
            bool keepAlive,
            CancellationTokenSource cancellationIoCache)
        {
            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                blockHeight = blockTransaction.TransactionBlockHeightInsert;

            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                return false;

            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                return await indexObject.PushOrUpdateTransactionOnIoBlockData(
                    blockTransaction,
                    blockHeight,
                    keepAlive,
                    cancellationIoCache
                );
            }

            return false;
        }

        /// <summary>
        /// Insert or update a block transaction directly to the io cache system.
        /// </summary>
        /// <param name="listBlockTransaction"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> InsertOrUpdateListBlockTransactionObject(
            List<ClassBlockTransaction> listBlockTransaction,
            bool keepAlive,
            CancellationTokenSource cancellationIoCache)
        {
            if (listBlockTransaction == null || listBlockTransaction.Count == 0)
                return true;

            // Groupement par fichier
            var groupedTransactions = listBlockTransaction
                .GroupBy(t => GetIoFileNameFromBlockHeight(t.TransactionObject.BlockHeightTransaction))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Initialiser les fichiers manquants
            var missingFiles = groupedTransactions.Keys.Where(f => !_dictionaryCacheIoIndexObject.ContainsKey(f)).ToList();

            if (missingFiles.Count > 0)
            {
                var initTasks = missingFiles.Select(InitializeNewCacheIoIndex);
                var initResults = await Task.WhenAll(initTasks);

                if (initResults.Any(r => !r.Item1))
                    return false;
            }

            // Traitement parallèle
            int maxParallel = _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && groupedTransactions.Count > 1
                ? Environment.ProcessorCount
                : 1;

            var semaphore = new SemaphoreSlim(maxParallel);
            var tasks = new List<Task<bool>>();

            foreach (var kvp in groupedTransactions)
            {
                await semaphore.WaitAsync(cancellationIoCache.Token);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        if (_dictionaryCacheIoIndexObject.TryGetValue(kvp.Key, out var indexObject))
                        {
                            return await indexObject.PushOrUpdateListIoBlockTransactionData(
                                kvp.Value,
                                keepAlive,
                                cancellationIoCache
                            );
                        }
                        return false;
                    }
                    catch
                    {
                        return false;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationIoCache.Token);

                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }

        /// <summary>
        /// Check if a transaction hash exist on io blocks cached.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> CheckTransactionHashExistOnIoBlockCached(
            string transactionHash,
            long blockHeight,
            CancellationTokenSource cancellationIoCache)
        {
            long blockHeightFromTransactionHash = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

            if (blockHeight != blockHeightFromTransactionHash || blockHeight < BlockchainSetting.GenesisBlockHeight)
                blockHeight = blockHeightFromTransactionHash;

            if (_dictionaryCacheIoIndexObject.Count == 0)
                return false;

            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                return await indexObject.ContainIoBlockTransactionHash(transactionHash, blockHeight, cancellationIoCache);
            }

            return false;
        }

        /// <summary>
        /// Retrieve every block transactions from a block object cached.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<SortedList<string, ClassBlockTransaction>> GetBlockTransactionListFromBlockHeightTarget(
            long blockHeight,
            bool keepAlive,
            CancellationTokenSource cancellationIoCache)
        {
            var listBlockTransactions = new SortedList<string, ClassBlockTransaction>();

            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                return listBlockTransactions;

            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                ClassBlockObject blockObject = await indexObject.GetIoBlockDataFromBlockHeight(
                    blockHeight,
                    keepAlive,
                    false,
                    cancellationIoCache
                );

                if (blockObject?.BlockTransactions != null)
                {
                    return new SortedList<string, ClassBlockTransaction>(
                        blockObject.BlockTransactions.ToDictionary(x => x.Key, x => x.Value)
                    );
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
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<List<ClassBlockTransaction>> GetListBlockTransactionFromListTransactionHashAndBlockHeightTarget(
            List<string> listTransactionHash,
            long blockHeight,
            bool keepAlive,
            CancellationTokenSource cancellationIoCache)
        {
            var listBlockTransaction = new List<ClassBlockTransaction>();

            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                return listBlockTransaction;

            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                ClassBlockObject blockObject = await indexObject.GetIoBlockDataFromBlockHeight(
                    blockHeight,
                    keepAlive,
                    false,
                    cancellationIoCache
                );

                if (blockObject?.BlockTransactions != null)
                {
                    foreach (string transactionHash in listTransactionHash)
                    {
                        if (blockObject.BlockTransactions.TryGetValue(transactionHash, out var transaction))
                        {
                            listBlockTransaction.Add(transaction.Clone());
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
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<ClassBlockTransaction> GetBlockTransactionFromTransactionHashOnIoBlockCached(
            string transactionHash,
            long blockHeight,
            bool keepAlive,
            CancellationTokenSource cancellationIoCache)
        {
            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                return null;

            string ioFileName = GetIoFileNameFromBlockHeight(blockHeight);

            if (_dictionaryCacheIoIndexObject.TryGetValue(ioFileName, out var indexObject))
            {
                return await indexObject.GetBlockTransactionFromIoBlockHeightByTransactionHash(
                    blockHeight,
                    transactionHash,
                    keepAlive,
                    cancellationIoCache
                );
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
        /// <param name="clone"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<SortedList<long, ClassBlockObject>> GetBlockObjectListFromBlockHeightRange(
            long blockHeightStart,
            long blockHeightEnd,
            HashSet<long> listBlockHeightAlreadyCached,
            SortedList<long, ClassBlockObject> listBlockAlreadyCached,
            bool keepAlive,
            bool clone,
            CancellationTokenSource cancellationIoCache)
        {
            // Groupement par fichier optimisé
            var blocksToFetch = new Dictionary<string, List<long>>();

            for (long height = blockHeightStart; height <= blockHeightEnd; height++)
            {
                if (listBlockAlreadyCached.ContainsKey(height))
                    continue;

                string ioFileName = GetIoFileNameFromBlockHeight(height);

                if (!_dictionaryCacheIoIndexObject.ContainsKey(ioFileName))
                    continue;

                if (!blocksToFetch.TryGetValue(ioFileName, out var heights))
                {
                    heights = _listPool.Get();
                    blocksToFetch[ioFileName] = heights;
                }
                heights.Add(height);
            }

            if (blocksToFetch.Count == 0)
                return listBlockAlreadyCached;

            try
            {
                // Traitement parallèle avec gestion d'erreur
                int maxParallel = _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskEnableMultiTask && blocksToFetch.Count > 1
                    ? Environment.ProcessorCount
                    : 1;

                var semaphore = new SemaphoreSlim(maxParallel);
                var fetchTasks = new List<Task<(bool success, List<ClassBlockObject> blocks)>>();

                foreach (var kvp in blocksToFetch)
                {
                    await semaphore.WaitAsync(cancellationIoCache.Token);

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            if (_dictionaryCacheIoIndexObject.TryGetValue(kvp.Key, out var indexObject))
                            {
                                using (var blockList = await indexObject.GetIoListBlockDataFromListBlockHeight(
                                    new HashSet<long>(kvp.Value),
                                    keepAlive,
                                    clone,
                                    cancellationIoCache))
                                {
                                    return (true, new List<ClassBlockObject>(blockList.GetAll.Where(b => b != null)));
                                }
                            }
                            return (false, new List<ClassBlockObject>());
                        }
                        catch
                        {
                            return (false, new List<ClassBlockObject>());
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationIoCache.Token);

                    fetchTasks.Add(task);
                }

                var results = await Task.WhenAll(fetchTasks);

                // Agréger les résultats
                lock (listBlockAlreadyCached)
                {
                    foreach (var result in results)
                    {
                        if (result.success)
                        {
                            foreach (var block in result.blocks)
                            {
                                if (!listBlockAlreadyCached.ContainsKey(block.BlockHeight) &&
                                    !listBlockHeightAlreadyCached.Contains(block.BlockHeight))
                                {
                                    listBlockAlreadyCached.Add(block.BlockHeight, block);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                // Retourner les listes au pool
                foreach (var list in blocksToFetch.Values)
                {
                    _listPool.Return(list);
                }
            }

            return listBlockAlreadyCached;
        }

        /// <summary>
        /// Return the block height start and the block height end indexed.
        /// </summary>
        /// <returns></returns>
        public async Task<Tuple<long, long>> GetIoCacheBlockIndexes(CancellationTokenSource cancellation)
        {
            var fileNames = _dictionaryCacheIoIndexObject.Keys.OrderBy(k => k).ToArray();

            if (fileNames.Length == 0)
                return new Tuple<long, long>(0, 0);

            var indexListFirst = await _dictionaryCacheIoIndexObject[fileNames[0]].GetIoBlockHeightListIndexed(cancellation);
            var indexListLast = await _dictionaryCacheIoIndexObject[fileNames[fileNames.Length - 1]].GetIoBlockHeightListIndexed(cancellation);

            return new Tuple<long, long>(indexListFirst.First(), indexListLast.Last());
        }

        #endregion

        #region Functions dedicated to io files indexing

        /// <summary>
        /// Return the io file name depending of the block height and the limit of max block inside a io cache file.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        private string GetIoFileNameFromBlockHeight(long blockHeight)
        {
            // Utiliser le cache pour éviter les allocations répétées
            return _fileNameCache.GetOrAdd(blockHeight, bh =>
            {
                long fileIndex = bh / _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskMaxBlockPerFile;
                return $"{fileIndex}{IoFileExtension}";
            });
        }

        /// <summary>
        /// Obtenir le verrou pour un fichier spécifique.
        /// </summary>
        /// <param name="ioFileName"></param>
        /// <returns></returns>
        private SemaphoreSlim GetFileLock(string ioFileName)
        {
            return _fileLocks.GetOrAdd(ioFileName, _ => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Return the total memory usage from the io cache system.
        /// </summary>
        /// <returns></returns>
        public long GetIoCacheSystemMemoryConsumption(CancellationTokenSource cancellation, out int totalBlockKeepAlive)
        {
            totalBlockKeepAlive = 0;

            if (_dictionaryCacheIoIndexObject.Count == 0)
                return 0;

            // Cache pendant l'intervalle défini
            if (DateTime.UtcNow - _lastMemoryCalculation < _memoryCalculationInterval)
            {
                // Recalculer seulement totalBlockKeepAlive
                foreach (var indexObject in _dictionaryCacheIoIndexObject.Values)
                {
                    indexObject.GetIoMemoryUsage(cancellation, out int keepAlive);
                    totalBlockKeepAlive += keepAlive;
                }
                return _cachedMemoryUsage;
            }

            // Calcul complet
            long totalMemoryUsage = 0;
            foreach (var indexObject in _dictionaryCacheIoIndexObject.Values)
            {
                totalMemoryUsage += indexObject.GetIoMemoryUsage(cancellation, out int keepAlive);
                totalBlockKeepAlive += keepAlive;
            }

            _cachedMemoryUsage = totalMemoryUsage;
            _lastMemoryCalculation = DateTime.UtcNow;

            return _cachedMemoryUsage;
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Pool d'objets simple pour réutiliser les listes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;
        private readonly Action<T> _resetAction;

        public ObjectPool(Func<T> objectGenerator, Action<T> resetAction = null)
        {
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _resetAction = resetAction;
        }

        public T Get()
        {
            if (_objects.TryTake(out T item))
                return item;

            return _objectGenerator();
        }

        public void Return(T item)
        {
            if (item == null)
                return;

            _resetAction?.Invoke(item);
            _objects.Add(item);
        }
    }

    #endregion
}