using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Function;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Checkpoint.Enum;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Disk.Object;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Main;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Enum;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Object;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Wallet;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Wallet.Object;
using SeguraChain_Lib.Blockchain.MemPool.Database;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Object;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Blockchain.Wallet.Object.Blockchain;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Main
{
    /// <summary>
    /// Persistant Dictionary class, using IO Cache disk in attempt to reduce memory usage (RAM)
    /// and also to save data updated, and recover them if a crash happen.
    /// </summary>
    public class BlockchainMemoryManagement
    {
        /// <summary>
        /// Block transaction cache system in front of IO cache files/network.
        /// </summary>
        private HashSet<string> _listWalletAddressReservedForBlockTransactionCache;
        private long _totalBlockTransactionCacheCount;
        private long _totalBlockTransactionMemorySize;

        /// <summary>
        /// IO Cache disk system object.
        /// </summary>
        private ClassCacheIoSystem _cacheIoSystem;

        /// <summary>
        /// Database.
        /// </summary>
        private Dictionary<long, BlockchainMemoryObject> _dictionaryBlockObjectMemory;
        public BlockchainWalletIndexMemoryManagement BlockchainWalletIndexMemoryCacheObject; // Contains block height/transactions linked to a wallet address, usually used for provide an accurate sync of data.

        /// <summary>
        /// Management of multithreading access.
        /// </summary>
        private SemaphoreSlim _semaphoreSlimUpdateTransactionConfirmations;
        private SemaphoreSlim _semaphoreSlimMemoryAccess;
        private SemaphoreSlim _semaphoreSlimGetWalletBalance;
        private SemaphoreSlim _semaphoreSlimCacheBlockTransactionAccess;

        /// <summary>
        /// Management of memory.
        /// </summary>
        private CancellationTokenSource _cancellationTokenMemoryManagement;

        /// <summary>
        /// Cache status.
        /// </summary>
        private bool _cacheStatus;
        private bool _pauseMemoryManagement;

        /// <summary>
        /// Cache settings.
        /// </summary>
        private ClassBlockchainDatabaseSetting _blockchainDatabaseSetting;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="blockchainDatabaseSetting">Database setting.</param>
        public BlockchainMemoryManagement(ClassBlockchainDatabaseSetting blockchainDatabaseSetting)
        {
            _blockchainDatabaseSetting = blockchainDatabaseSetting;
            _cacheStatus = _blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase;

            // Cache status.
            _pauseMemoryManagement = false;

            _dictionaryBlockObjectMemory = new Dictionary<long, BlockchainMemoryObject>();
            _listWalletAddressReservedForBlockTransactionCache = new HashSet<string>();
            BlockchainWalletIndexMemoryCacheObject = new BlockchainWalletIndexMemoryManagement(blockchainDatabaseSetting);

            // Protect against multithreading access.
            _semaphoreSlimMemoryAccess = new SemaphoreSlim(1, 1);
            _semaphoreSlimGetWalletBalance = new SemaphoreSlim(1, 1);
            _semaphoreSlimUpdateTransactionConfirmations = new SemaphoreSlim(1, 1);
            _semaphoreSlimCacheBlockTransactionAccess = new SemaphoreSlim(1, ClassUtility.GetMaxAvailableProcessorCount() * ClassUtility.GetMaxAvailableProcessorCount());

            // Cancellation token of memory management.
            _cancellationTokenMemoryManagement = new CancellationTokenSource();
        }

        #region Manage cache functions.

        /// <summary>
        /// Load the blockchain cache.
        /// </summary>
        public async Task<bool> LoadBlockchainCache()
        {
            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {

                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            _cacheIoSystem = new ClassCacheIoSystem(_blockchainDatabaseSetting);

                            Tuple<bool, HashSet<long>> result = await _cacheIoSystem.InitializeCacheIoSystem();

                            if (result.Item2.Count > 0)
                            {
                                foreach (long blockHeight in result.Item2)
                                {
                                    _dictionaryBlockObjectMemory.Add(blockHeight, new BlockchainMemoryObject()
                                    {
                                        ObjectCacheType = CacheBlockMemoryEnumState.IN_PERSISTENT_CACHE,
                                        ObjectIndexed = true,
                                        CacheUpdated = true
                                    });
                                }
                            }
                        }
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Close the cache.
        /// </summary>
        /// <returns></returns>
        public async Task CloseCache()
        {
            switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
            {
                case CacheEnumName.IO_CACHE:
                    await _cacheIoSystem.CleanCacheIoSystem();
                    break;
            }
        }

        /// <summary>
        /// Attempt to force to purge the cache.
        /// </summary>
        /// <returns></returns>
        public async Task ForcePurgeCache(CancellationTokenSource cancellation)
        {
            await ForcePurgeMemoryDataCache(cancellation);
        }

        #endregion


        #region Original dictionary functions.

        /// <summary>
        /// Emulate dictionary key index. Permit to get/set a value.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public ClassBlockObject this[long blockHeight, CancellationTokenSource cancellation]
        {
            get
            {

                if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                {

                    if (!CheckIfBlockHeightOutOfActiveMemory(blockHeight))
                    {
                        if (_dictionaryBlockObjectMemory[blockHeight].Content?.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                            AddOrUpdateBlockMirrorObject(_dictionaryBlockObjectMemory[blockHeight].Content);

                        return _dictionaryBlockObjectMemory[blockHeight].Content;
                    }

                    return GetObjectByKeyFromMemoryOrCacheAsync(blockHeight, cancellation).Result;
                }

                if (BlockHeightIsCached(blockHeight, cancellation).Result)
                    return GetObjectByKeyFromMemoryOrCacheAsync(blockHeight, cancellation).Result;
#if DEBUG
                Debug.WriteLine("Blockchain database - The block height: " + blockHeight + " is missing.");
#endif

                ClassLog.WriteLine("Blockchain database - The block height: " + blockHeight + " is missing.", ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);

                return null;
            }
            set
            {
                if (value != null)
                {
                    try
                    {
                        value.BlockCloned = false;

                        if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
                        {

                            if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                            {
                                _dictionaryBlockObjectMemory[blockHeight].Content = value;
                                _dictionaryBlockObjectMemory[blockHeight].CacheUpdated = false;
                                _dictionaryBlockObjectMemory[blockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;
                            }
                            else
                            {
                                if (!DirectUpdateFromSetter(value, cancellation))
                                {
                                    _dictionaryBlockObjectMemory[blockHeight].Content = value;
                                    _dictionaryBlockObjectMemory[blockHeight].CacheUpdated = false;
                                    _dictionaryBlockObjectMemory[blockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;
                                }
                            }
                        }
                        else
                        {
                            _dictionaryBlockObjectMemory[blockHeight].Content = value;
                            _dictionaryBlockObjectMemory[blockHeight].CacheUpdated = false;
                            _dictionaryBlockObjectMemory[blockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;
                        }

                        if (value?.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                            AddOrUpdateBlockMirrorObject(value);
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            }
        }

        /// <summary>
        /// Update data from setter directly.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private bool DirectUpdateFromSetter(ClassBlockObject value, CancellationTokenSource cancellation)
        {
            return InsertOrUpdateBlockObjectToCache(value, true, cancellation).Result;
        }

        /// <summary>
        /// Check if the key exist.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public bool ContainsKey(long blockHeight)
        {
            if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                return _dictionaryBlockObjectMemory.ContainsKey(blockHeight);

            return false;
        }

        /// <summary>
        /// Get the list of keys of elements stored.
        /// </summary>
        public DisposableList<long> ListBlockHeight => new DisposableList<long>(true, 0, _dictionaryBlockObjectMemory.Keys.ToList());

        /// <summary>
        /// Get the last key stored.
        /// </summary>
        public long GetLastBlockHeight => Count;

        /// <summary>
        /// Get the amount of elements.
        /// </summary>
        public int Count => _dictionaryBlockObjectMemory.Count;

        /// <summary>
        /// Insert an element to the active memory.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="value"></param>
        /// <param name="insertEnumTypeStatus"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> Add(long blockHeight, ClassBlockObject value, CacheBlockMemoryInsertEnumType insertEnumTypeStatus, CancellationTokenSource cancellation)
        {
            bool result = false;
            bool semaphoreUsed = false;
            try
            {
                try
                {
                    await _semaphoreSlimMemoryAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;
                }
                catch
                {
                    // Ignored, the task has been cancelled.
                }

                if (semaphoreUsed)
                {
                    // Never put locked blocks into the cache. Keep always alive the genesis block height in the active memory, this one is always low.
                    if (value.BlockStatus == ClassBlockEnumStatus.LOCKED ||
                        value.BlockHeight == BlockchainSetting.GenesisBlockHeight || !_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
                        insertEnumTypeStatus = CacheBlockMemoryInsertEnumType.INSERT_IN_ACTIVE_MEMORY_OBJECT;

                    switch (insertEnumTypeStatus)
                    {
                        case CacheBlockMemoryInsertEnumType.INSERT_IN_ACTIVE_MEMORY_OBJECT:
                            {
                                if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight && ContainsKey(blockHeight))
                                {
                                    _dictionaryBlockObjectMemory[blockHeight].Content = value;
                                    _dictionaryBlockObjectMemory[blockHeight].CacheUpdated = false;
                                    _dictionaryBlockObjectMemory[blockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;
                                }
                                else
                                {
                                    if (!TryAdd(blockHeight, new BlockchainMemoryObject()
                                    {
                                        Content = value,
                                        CacheUpdated = false,
                                        ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY
                                    }))
                                    {
                                        cancellation?.Token.ThrowIfCancellationRequested();

                                        if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                                        {
                                            _dictionaryBlockObjectMemory[blockHeight].Content = value;
                                            _dictionaryBlockObjectMemory[blockHeight].CacheUpdated = false;
                                            _dictionaryBlockObjectMemory[blockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;
                                            result = true;
                                        }
                                        else result = false;
                                    }
                                    else result = true;
                                }
                            }
                            break;
                        case CacheBlockMemoryInsertEnumType.INSERT_IN_PERSISTENT_CACHE_OBJECT:
                            {
                                if (_cacheStatus && _blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
                                    result = await AddOrUpdateMemoryDataToCache(value, true, cancellation);

                                if (result)
                                {
                                    if (blockHeight > GetLastBlockHeight)
                                    {
                                        if (!TryAdd(blockHeight, new BlockchainMemoryObject()
                                        {
                                            Content = null,
                                            CacheUpdated = true,
                                            ObjectIndexed = true,
                                            ObjectCacheType = CacheBlockMemoryEnumState.IN_PERSISTENT_CACHE
                                        }))
                                        {
                                            cancellation?.Token.ThrowIfCancellationRequested();

                                            if (_dictionaryBlockObjectMemory[blockHeight].Content == null)
                                            {
                                                _dictionaryBlockObjectMemory[blockHeight].ObjectIndexed = true;
                                                _dictionaryBlockObjectMemory[blockHeight].Content = null;
                                                _dictionaryBlockObjectMemory[blockHeight].CacheUpdated = true;
                                                _dictionaryBlockObjectMemory[blockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_PERSISTENT_CACHE;
                                            }

                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    if (!TryAdd(blockHeight, new BlockchainMemoryObject()
                                    {
                                        Content = value,
                                        CacheUpdated = false,
                                        ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY
                                    }))
                                    {
                                        cancellation?.Token.ThrowIfCancellationRequested();

                                        if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                                        {
                                            _dictionaryBlockObjectMemory[blockHeight].Content = value;
                                            _dictionaryBlockObjectMemory[blockHeight].CacheUpdated = false;
                                            _dictionaryBlockObjectMemory[blockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;
                                            result = true;
                                        }
                                    }
                                    else result = true;
                                }

                            }
                            break;
                    }

                    if (result && value?.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                        AddOrUpdateBlockMirrorObject(value);
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreSlimMemoryAccess.Release();
            }
            return result;
        }

        /// <summary>
        /// Attempt to remove an element of disk cache, and on memory.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> Remove(long blockHeight, CancellationTokenSource cancellation)
        {
            bool result = false;

            bool semaphoreUsed = false;

            try
            {
                try
                {
                    await _semaphoreSlimMemoryAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;
                }
                catch
                {
                    // Ignored, the task has been cancelled.
                }

                if (semaphoreUsed)
                {
                    if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                    {
                        // Remove from cache if indexed and if the cache is enabled.
                        if (_dictionaryBlockObjectMemory[blockHeight].ObjectIndexed && _blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
                        {
                            #region Remove from cache first, then from the active memory.

                            if (await RemoveMemoryDataOfCache(blockHeight, cancellation))
                                result = _dictionaryBlockObjectMemory.Remove(blockHeight);

                            #endregion
                        }
                        else
                        {
                            #region Remove from the active memory.

                            result = _dictionaryBlockObjectMemory.Remove(blockHeight);

                            #endregion
                        }
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreSlimMemoryAccess.Release();
            }

            return result;
        }

        /// <summary>
        /// Try to add an element.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="blockchainMemoryObject"></param>
        /// <returns></returns>
        private bool TryAdd(long blockHeight, BlockchainMemoryObject blockchainMemoryObject)
        {
            try
            {
                if (ContainsKey(blockHeight))
                    return true;
                else
                {
#if NET5_0_OR_GREATER
                    return _dictionaryBlockObjectMemory.TryAdd(blockHeight, blockchainMemoryObject);
#else
                    _dictionaryBlockObjectMemory.Add(blockHeight, blockchainMemoryObject);
                    return true;
#endif
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the block height data is cached and removed from the active memory.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public bool CheckIfBlockHeightOutOfActiveMemory(long blockHeight)
        {
            return _dictionaryBlockObjectMemory[blockHeight].Content == null;
        }

        #endregion


        #region Blockchain functions.

        #region Specific Insert/Update blocks data with the cache system.

        /// <summary>
        /// Insert or update a block object in the cache.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> InsertOrUpdateBlockObjectToCache(ClassBlockObject blockObject, bool keepAlive, CancellationTokenSource cancellation)
        {

            if (!ContainsKey(blockObject.BlockHeight))
            {
                _dictionaryBlockObjectMemory.Add(blockObject.BlockHeight, new BlockchainMemoryObject()
                {
                    Content = blockObject,
                    ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY,
                    ObjectIndexed = true,
                    CacheUpdated = false
                });

                return true;
            }
            else
            {
                // Update the active memory if the content of the block height target if this one is not empty.
                if (_dictionaryBlockObjectMemory[blockObject.BlockHeight].Content != null ||
                    keepAlive || blockObject.BlockHeight == BlockchainSetting.GenesisBlockHeight)
                {
                    _dictionaryBlockObjectMemory[blockObject.BlockHeight].Content = blockObject;
                    _dictionaryBlockObjectMemory[blockObject.BlockHeight].CacheUpdated = false;
                    _dictionaryBlockObjectMemory[blockObject.BlockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;

                    return true;
                }
            }

            // Try to update or add the block data updated to the cache.
            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase &&
                blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight)
            {
                if (blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight)
                    return await AddOrUpdateMemoryDataToCache(blockObject, keepAlive, cancellation);
            }

            return false;
        }

        /// <summary>
        /// Insert a block transaction in the cache.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> InsertBlockTransactionToCache(ClassBlockTransaction blockTransaction, long blockHeight, bool keepAlive, CancellationTokenSource cancellation)
        {
            return await InsertBlockTransactionToMemoryDataCache(blockTransaction, blockHeight, keepAlive, cancellation);
        }

        #endregion

        #region Specific research of data on blocks managed with the cache system.

        /// <summary>
        /// Get a block transaction count by cache strategy.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<int> GetBlockTransactionCountStrategy(long blockHeight, CancellationTokenSource cancellation)
        {
            if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
            {

                if (_dictionaryBlockObjectMemory[blockHeight].ContentMirror != null)
                    return _dictionaryBlockObjectMemory[blockHeight].ContentMirror.TotalTransaction;

                ClassBlockObject blockObject = await GetBlockInformationDataStrategy(blockHeight, cancellation);

                if (blockObject == null)
                    return await GetBlockTransactionCountMemoryDataFromCacheByKey(blockHeight, cancellation);
                else
                {
                    if (blockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED && blockObject.TotalTransaction == 0)
                        return await GetBlockTransactionCountMemoryDataFromCacheByKey(blockHeight, cancellation);
                    else
                        return blockObject.TotalTransaction;

                }
            }

            return 0;
        }

        /// <summary>
        /// Get a block information data by cache strategy.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<ClassBlockObject> GetBlockInformationDataStrategy(long blockHeight, CancellationTokenSource cancellation)
        {
            return blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight ? await GetBlockMirrorObject(blockHeight, cancellation) : null;
        }

        /// <summary>
        /// Get a list of block informations data by cache strategy.
        /// </summary>
        /// <param name="listBlockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<List<ClassBlockObject>> GetListBlockInformationDataFromListBlockHeightStrategy(DisposableList<long> listBlockHeight, CancellationTokenSource cancellation)
        {
            List<ClassBlockObject> listBlockInformationData = new List<ClassBlockObject>();

            if (listBlockHeight.Count > 0)
            {
                foreach (long blockHeight in listBlockHeight.GetList.OrderBy(x => x))
                {
                    

                    ClassBlockObject blockObject = await GetBlockMirrorObject(blockHeight, cancellation);

                    if (blockObject != null)
                        listBlockInformationData.Add(blockObject);
                }
            }


            return listBlockInformationData;
        }

        /// <summary>
        /// Get a block data by cache strategy.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<ClassBlockObject> GetBlockDataStrategy(long blockHeight, bool keepAlive, bool clone, CancellationTokenSource cancellation)
        {
            if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
            {
                if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                    return clone ? _dictionaryBlockObjectMemory[blockHeight].Content.DirectCloneBlockObject() : _dictionaryBlockObjectMemory[blockHeight].Content;
                else
                    return await GetBlockMemoryDataFromCacheByKey(blockHeight, keepAlive, clone, cancellation);
            }

            return null;
        }

        /// <summary>
        /// Check if the block height target is cached.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> BlockHeightIsCached(long blockHeight, CancellationTokenSource cancellation)
        {
            if (ContainsKey(blockHeight))
                return _dictionaryBlockObjectMemory[blockHeight].Content != null ? true : false;
            
            if (await CheckBlockHeightExistOnMemoryDataCache(blockHeight, cancellation))
            {
                // Insert the block height.
                return TryAdd(blockHeight, new BlockchainMemoryObject()
                {
                    Content = null,
                    ObjectIndexed = true,
                    CacheUpdated = true,
                    ObjectCacheType = CacheBlockMemoryEnumState.IN_PERSISTENT_CACHE
                });
            }

            return false;
        }

        /// <summary>
        /// Get the amount of blocks locked.
        /// </summary>
        /// <returns></returns>
        public long GetCountBlockLocked()
        {
            long totalBlockLocked = 0;

            if (Count > 0)
            {
                long lastBlockHeight = GetLastBlockHeight;

                long startHeight = lastBlockHeight;
                while (startHeight >= BlockchainSetting.GenesisBlockHeight)
                {
                    if (_dictionaryBlockObjectMemory[startHeight].Content != null)
                    {
                        if (_dictionaryBlockObjectMemory[startHeight].Content.BlockStatus == ClassBlockEnumStatus.LOCKED)
                            totalBlockLocked++;
                        else
                        {
                            if (startHeight < lastBlockHeight)
                                break;
                        }
                    }
                    startHeight--;

                    if (startHeight < BlockchainSetting.GenesisBlockHeight)
                        break;
                }
            }

            return totalBlockLocked;
        }

        /// <summary>
        /// Get the listing of block unconfirmed totally with the network.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<DisposableList<long>> GetListBlockNetworkUnconfirmed(CancellationTokenSource cancellation)
        {
            DisposableList<long> blockNetworkUnconfirmedList = new DisposableList<long>();

            using (DisposableList<long> listBlockHeight = new DisposableList<long>())
            {

                long lastBlockHeight = GetLastBlockHeight;

                for (long i = 0; i < lastBlockHeight; i++)
                {
                    if (cancellation.IsCancellationRequested)
                        break;

                    long blockHeight = i + 1;

                    bool found = false;

                    ClassBlockObject blockObject = await GetBlockMirrorObject(blockHeight, cancellation);

                    if (blockObject != null)
                    {
                        if (blockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                        {
                            if (!blockObject.IsConfirmedByNetwork)
                                blockNetworkUnconfirmedList.Add(blockHeight);
                            else
                            {
                                if (ClassBlockUtility.GetBlockTemplateFromBlockHash(blockObject.BlockHash, out ClassBlockTemplateObject blockTemplateObject))
                                {
                                    if (blockObject.BlockDifficulty != blockTemplateObject.BlockDifficulty)
                                        blockNetworkUnconfirmedList.Add(blockHeight);
                                }
                                else blockNetworkUnconfirmedList.Add(blockHeight);
                            }

                            found = true;
                        }
                    }


                    if (!found)
                        listBlockHeight.Add(blockHeight);
                }

                if (listBlockHeight.Count > 0)
                {
                    using (var listBlockObject = await GetBlockInformationListByBlockHeightListTargetFromMemoryDataCache(listBlockHeight, cancellation))
                    {
                        foreach (var blockObjectPair in listBlockObject.GetList)
                        {
                            

                            if (blockObjectPair.Value != null)
                            {
                                if (!blockObjectPair.Value.IsConfirmedByNetwork)
                                    blockNetworkUnconfirmedList.Add(blockObjectPair.Value.BlockHeight);
                                else
                                {
                                    if (ClassBlockUtility.GetBlockTemplateFromBlockHash(blockObjectPair.Value.BlockHash, out ClassBlockTemplateObject blockTemplateObject))
                                    {
                                        if (blockObjectPair.Value.BlockDifficulty != blockTemplateObject.BlockDifficulty)
                                            blockNetworkUnconfirmedList.Add(blockObjectPair.Value.BlockHeight);
                                    }
                                    else blockNetworkUnconfirmedList.Add(blockObjectPair.Value.BlockHeight);
                                }
                            }
                        }
                    }
                }
            }

            return blockNetworkUnconfirmedList;
        }

        #endregion

        #region Specific getting of data managed with the cache system.

        /// <summary>
        /// Get the much closer block height from a timestamp provided.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<long> GetCloserBlockHeightFromTimestamp(long timestamp, CancellationTokenSource cancellation)
        {
            long closerBlockHeight = 0;

            if (Count > 0)
            {
                long lastBlockHeight = GetLastBlockHeight;

                while (lastBlockHeight > 0)
                {
                    cancellation?.Token.ThrowIfCancellationRequested();

                    bool found = false;

                    ClassBlockObject blockObject = await GetBlockMirrorObject(lastBlockHeight, cancellation);

                    if (blockObject != null)
                    {
                        found = true;

                        if (timestamp == blockObject.TimestampCreate)
                            return lastBlockHeight;
                    }

                    if (!found)
                    {
                        if (_dictionaryBlockObjectMemory[lastBlockHeight].Content != null)
                        {
                            if (timestamp == _dictionaryBlockObjectMemory[lastBlockHeight].Content.TimestampCreate)
                                return lastBlockHeight;
                        }
                    }

                    lastBlockHeight--;

                    if (lastBlockHeight < BlockchainSetting.GenesisBlockHeight)
                        break;
                }
            }

            return closerBlockHeight;
        }

        /// <summary>
        /// Get the last block height unlocked.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<long> GetLastBlockHeightUnlocked(CancellationTokenSource cancellation)
        {
            long lastBlockHeightUnlocked = 0;

            if (Count > 0)
            {
                long lastBlockHeight = GetLastBlockHeight;

                while (lastBlockHeight > 0)
                {
                    cancellation?.Token.ThrowIfCancellationRequested();

                    long blockHeight = lastBlockHeight;

                    ClassBlockObject blockObject = await GetBlockMirrorObject(blockHeight, cancellation);

                    if (blockObject != null)
                    {
                        if (blockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                        {
                            lastBlockHeightUnlocked = blockObject.BlockHeight;
                            break;
                        }
                    }


                    lastBlockHeight--;

                    if (lastBlockHeight < BlockchainSetting.GenesisBlockHeight)
                        break;
                }
            }

            return lastBlockHeightUnlocked;
        }

        /// <summary>
        /// Get the last block height confirmed with the network.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<long> GetLastBlockHeightConfirmationNetworkChecked(CancellationTokenSource cancellation)
        {
            long lastBlockHeightUnlockedChecked = 0;

            if (Count > 0)
            {
                #region If the block height is not provided or if the block height provided is not the correct one.

                using (DisposableList<long> listBlockHeight = new DisposableList<long>())
                {

                    long lastBlockHeight = GetLastBlockHeight;

                    for (long i = 0; i < lastBlockHeight; i++)
                    {
                        cancellation?.Token.ThrowIfCancellationRequested();

                        long blockHeight = i + 1;

                        bool found = false;
                        bool locked = false;

                        ClassBlockObject blockObject = await GetBlockMirrorObject(blockHeight, cancellation);
                        if (blockObject != null)
                        {
                            if (blockObject.BlockStatus == ClassBlockEnumStatus.LOCKED)
                                locked = true;
                            else
                            {
                                if (blockObject.IsConfirmedByNetwork)
                                    lastBlockHeightUnlockedChecked = blockObject.BlockHeight;
                                else
                                    break;
                            }
                            found = true;
                        }

                        if (!found && !locked)
                            listBlockHeight.Add(blockHeight);
                    }

                    if (listBlockHeight.Count > 0)
                    {
                        using (var listBlockObject = await GetBlockInformationListByBlockHeightListTargetFromMemoryDataCache(listBlockHeight, cancellation))
                        {
                            foreach (var blockObjectPair in listBlockObject.GetList)
                            {
                                

                                if (blockObjectPair.Value != null)
                                {
                                    if (blockObjectPair.Value.IsConfirmedByNetwork)
                                        lastBlockHeightUnlockedChecked = blockObjectPair.Value.BlockHeight;
                                    else
                                        break;
                                }
                            }
                        }
                    }

                }

                #endregion
            }

            return lastBlockHeightUnlockedChecked;
        }

        /// <summary>
        /// Get the last block height transaction confirmation done.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<long> GetLastBlockHeightTransactionConfirmationDone(CancellationTokenSource cancellation)
        {
            long lastBlockHeightTransactionConfirmationDone = 0;

            if (Count > 0)
            {
                #region If the block height is not provided or if the block height provided is not the correct one.

                using (DisposableList<long> listBlockHeight = new DisposableList<long>())
                {
                    long lastBlockHeight = GetLastBlockHeight;

                    for (long blockHeight = lastBlockHeight; blockHeight > 0; blockHeight--)
                    {
                        cancellation?.Token.ThrowIfCancellationRequested();

                        if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                            break;

                        bool found = false;

                        if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                        {
                            if (_dictionaryBlockObjectMemory[blockHeight].Content.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                            {
                                found = true;

                                if (_dictionaryBlockObjectMemory[blockHeight].Content.BlockLastHeightTransactionConfirmationDone > 0 &&
                                    _dictionaryBlockObjectMemory[blockHeight].Content.IsConfirmedByNetwork)
                                {
                                    lastBlockHeightTransactionConfirmationDone = blockHeight;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            ClassBlockObject blockObject = await GetBlockMirrorObject(blockHeight, cancellation);

                            if (blockObject != null)
                            {
                                if (blockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                {
                                    found = true;

                                    if (blockObject.BlockLastHeightTransactionConfirmationDone > 0 &&
                                        blockObject.IsConfirmedByNetwork)
                                    {
                                        lastBlockHeightTransactionConfirmationDone = blockHeight;
                                        break;
                                    }
                                }
                            }
                        }

                        if (!found)
                            listBlockHeight.Add(blockHeight);
                    }

                    if (listBlockHeight.Count > 0)
                    {
                        using (var listBlockInformation = await GetBlockInformationListByBlockHeightListTargetFromMemoryDataCache(listBlockHeight, cancellation))
                        {
                            foreach (var blockObjectPair in listBlockInformation.GetList)
                            {
                                

                                if (blockObjectPair.Value != null)
                                {
                                    if (blockObjectPair.Value.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                    {
                                        if (blockObjectPair.Value.BlockLastHeightTransactionConfirmationDone > 0 &&
                                            blockObjectPair.Value.IsConfirmedByNetwork)
                                        {
                                            lastBlockHeightTransactionConfirmationDone = blockObjectPair.Value.BlockHeight;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion
            }

            return lastBlockHeightTransactionConfirmationDone;
        }

        /// <summary>
        /// Return a list of block height who is missing.
        /// </summary>
        /// <param name="blockHeightTarget"></param>
        /// <param name="enableMaxRange"></param>
        /// <param name="ignoreLockedBlocks"></param>
        /// <param name="cancellation"></param>
        /// <param name="maxRange"></param>
        /// <returns></returns>
        public DisposableList<long> GetListBlockMissing(long blockHeightTarget, bool enableMaxRange, bool ignoreLockedBlocks, CancellationTokenSource cancellation, int maxRange)
        {
            DisposableList<long> blockMiss = new DisposableList<long>();

            long blockHeightExpected = 0;
            int countBlockListed = 0;

            for (long i = 0; i < blockHeightTarget; i++)
            {
                cancellation?.Token.ThrowIfCancellationRequested();

                if (enableMaxRange)
                    if (countBlockListed >= maxRange)
                        break;

                blockHeightExpected++;

                bool found = false;

                if (!(blockHeightExpected >= BlockchainSetting.GenesisBlockHeight && blockHeightExpected <= GetLastBlockHeight))
                {
                    blockMiss.Add(blockHeightExpected);
                    countBlockListed++;
                    found = true;
                }

                if (!found)
                {
                    if (!ignoreLockedBlocks)
                    {
                        if (_dictionaryBlockObjectMemory[blockHeightExpected].Content != null)
                        {
                            if (_dictionaryBlockObjectMemory[blockHeightExpected].Content.BlockStatus == ClassBlockEnumStatus.LOCKED)
                            {
                                blockMiss.Add(blockHeightExpected);
                                countBlockListed++;
                            }
                        }
                    }
                }
            }

            return blockMiss;
        }

        /// <summary>
        /// Get a block transaction target by his transaction hash his block height.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <param name="useBlockTransactionCache"></param>
        /// <returns></returns>
        public async Task<ClassBlockTransaction> GetBlockTransactionFromSpecificTransactionHashAndHeight(string transactionHash, long blockHeight, bool useBlockTransactionCache, bool keepAlive, CancellationTokenSource cancellation)
        {
            if (transactionHash.IsNullOrEmpty(false, out _))
                return null;

            if (blockHeight == 0)
                blockHeight = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

            if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
            {
                ClassBlockTransaction blockTransaction = await GetBlockTransactionByTransactionHashFromMemoryDataCache(transactionHash, blockHeight, useBlockTransactionCache, keepAlive, cancellation);

                if (blockTransaction != null)
                    return blockTransaction;
            }

            return null;
        }

        /// <summary>
        /// Get a list of transaction by a list of transaction hash and a list of block height.
        /// </summary>
        /// <param name="listTransactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="useBlockTransactionCache"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<DisposableList<ClassBlockTransaction>> GetListBlockTransactionFromListTransactionHashAndHeight(List<string> listTransactionHash, long blockHeight, bool useBlockTransactionCache, bool keepAlive, CancellationTokenSource cancellation)
        {
            return await GetListBlockTransactionByListTransactionHashFromMemoryDataCache(listTransactionHash, blockHeight, useBlockTransactionCache, keepAlive, cancellation);
        }

        /// <summary>
        /// Get a block transaction list by a block height target from the active memory or the cache.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<DisposableSortedList<string, ClassBlockTransaction>> GetTransactionListFromBlockHeightTarget(long blockHeight, bool keepAlive, CancellationTokenSource cancellation)
        {
            return await GetTransactionListFromBlockHeightTargetFromMemoryDataCache(blockHeight, keepAlive, cancellation);
        }


        #endregion

        #region Specific update of data managed with the cache system.

        /// <summary>
        /// Get the last blockchain stats.
        /// </summary>
        /// <param name="blockchainNetworkStatsObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<ClassBlockchainNetworkStatsObject> GetBlockchainNetworkStatsObjectAsync(ClassBlockchainNetworkStatsObject blockchainNetworkStatsObject, CancellationTokenSource cancellation)
        {
            if (blockchainNetworkStatsObject == null)
                blockchainNetworkStatsObject = new ClassBlockchainNetworkStatsObject();

            bool semaphoreUsed = false;

            try
            {
                try
                {
                    await _semaphoreSlimUpdateTransactionConfirmations.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;
                }
                catch
                {
                    // The task has been cancelled.
                }

                if (Count > 0 && semaphoreUsed)
                {

                    if (blockchainNetworkStatsObject.LastNetworkBlockHeight == 0)
                        blockchainNetworkStatsObject.LastNetworkBlockHeight = GetLastBlockHeight;

                    long lastBlockHeight = GetLastBlockHeight;

                    if (lastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                    {
                        blockchainNetworkStatsObject.LastBlockHeight = lastBlockHeight;

                        long totalMemPoolTransaction = ClassMemPoolDatabase.GetCountMemPoolTx;
                        long lastBlockHeightUnlocked = await GetLastBlockHeightUnlocked(cancellation);
                        blockchainNetworkStatsObject.LastBlockHeightUnlocked = lastBlockHeightUnlocked;
                        long lastBlockHeightTransactionConfirmationDone = await GetLastBlockHeightTransactionConfirmationDone(cancellation);

                        ClassBlockObject lastBlock = await GetBlockInformationDataStrategy(lastBlockHeight, cancellation);

                        if (lastBlock != null)
                        {
                            blockchainNetworkStatsObject.LastBlockHeight = lastBlock.BlockHeight;
                            blockchainNetworkStatsObject.LastBlockDifficulty = lastBlock.BlockDifficulty;
                            blockchainNetworkStatsObject.LastBlockHash = lastBlock.BlockHash;
                            blockchainNetworkStatsObject.LastBlockStatus = lastBlock.BlockStatus;
                        }


                        bool canceled = false;

                        long startHeightAvgMining = (lastBlockHeightUnlocked - BlockchainSetting.BlockDifficultyRangeCalculation);

                        if (startHeightAvgMining <= 0)
                            startHeightAvgMining = 1;

                        long endHeightAvgMining = startHeightAvgMining + BlockchainSetting.BlockDifficultyRangeCalculation;

                        BigInteger totalFeeCirculating = 0;
                        BigInteger totalCoinPending = 0;
                        BigInteger totalCoinCirculating = 0;
                        long totalTransaction = 0;
                        long totalTransactionConfirmed = 0;
                        long averageMiningTotalTimespend = 0;
                        long averageMiningTimespendExpected = 0;

                        int totalTravel = 0;

                        long timestampTaskStart = ClassUtility.GetCurrentTimestampInMillisecond();

                        try
                        {

                            for (long i = 0; i < lastBlockHeightTransactionConfirmationDone; i++)
                            {
                                if (cancellation.IsCancellationRequested)
                                    break;

                                ClassBlockObject blockObject = await GetBlockInformationDataStrategy(i + 1, cancellation);

                                if (blockObject == null)
                                {
                                    canceled = true;
                                    break;
                                }

                                if (totalTravel >= startHeightAvgMining && totalTravel <= endHeightAvgMining)
                                {
                                    averageMiningTotalTimespend += blockObject.TimestampFound - blockObject.TimestampCreate;
                                    averageMiningTimespendExpected += BlockchainSetting.BlockTime;
                                }

                                lastBlockHeightUnlocked = blockObject.BlockHeight;

                                totalTransaction += blockObject.TotalTransaction;
                                totalTransactionConfirmed += blockObject.TotalTransactionConfirmed;
                                totalCoinPending += blockObject.TotalCoinPending;
                                totalCoinCirculating += blockObject.TotalCoinConfirmed;
                                totalFeeCirculating += blockObject.TotalFee;

                                totalTravel++;
                            }
                        }
                        catch (Exception error)
                        {
                            canceled = true;

                            ClassLog.WriteLine("Error on building latest blockchain stats. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
#if DEBUG
                            Debug.WriteLine("Error on building latest blockchain stats. Exception: " + error.Message);
#endif
                        }


                        // Retrieve latest stats calculated if their is any cancellation done.
                        if (!canceled)
                        {
                            // Update tasks stats confirmed.
                            blockchainNetworkStatsObject.TotalCoinCirculating = totalCoinCirculating;
                            blockchainNetworkStatsObject.TotalCoinPending = totalCoinPending;
                            blockchainNetworkStatsObject.TotalFee = totalFeeCirculating;
                            blockchainNetworkStatsObject.TotalTransactionsConfirmed = totalTransactionConfirmed;
                            blockchainNetworkStatsObject.TotalTransactions = totalTransaction;
                            blockchainNetworkStatsObject.LastAverageMiningTimespendDone = averageMiningTotalTimespend;
                            blockchainNetworkStatsObject.LastAverageMiningTimespendExpected = averageMiningTimespendExpected;
                            blockchainNetworkStatsObject.LastBlockHeightTransactionConfirmationDone = lastBlockHeightTransactionConfirmationDone;
                            blockchainNetworkStatsObject.LastUpdateStatsDateTime = ClassUtility.GetDatetimeFromTimestamp(ClassUtility.GetCurrentTimestampInSecond());
                            blockchainNetworkStatsObject.BlockchainStatsTimestampToGenerate = ClassUtility.GetCurrentTimestampInMillisecond() - timestampTaskStart;
                        }

                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreSlimUpdateTransactionConfirmations.Release();
            }

            return blockchainNetworkStatsObject;
        }

        /// <summary>
        /// Update the amount of confirmations of transactions from a block target.
        /// </summary>
        /// <param name="lastBlockHeightUnlockedChecked"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<ClassBlockchainBlockConfirmationResultObject> UpdateBlockDataTransactionConfirmations(long lastBlockHeightUnlockedChecked, CancellationTokenSource cancellation)
        {
            return await IncrementBlockTransactionConfirmation(lastBlockHeightUnlockedChecked, cancellation);
        }

        #region Functions dedicated to confirm block transaction(s).

        /// <summary>
        /// Increment block transactions confirmations once the block share is voted and valid.
        /// </summary>
        private async Task<ClassBlockchainBlockConfirmationResultObject> IncrementBlockTransactionConfirmation(long lastBlockHeightUnlockedChecked, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;

            ClassBlockchainBlockConfirmationResultObject blockchainBlockConfirmationResultObject = new ClassBlockchainBlockConfirmationResultObject();

            try
            {
                try
                {
                    await _semaphoreSlimUpdateTransactionConfirmations.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;
                }
                catch
                {
                    // The task has been cancelled.
                }

                if (semaphoreUsed)
                {
                    try
                    {
                        long lastBlockHeightUnlocked = await GetLastBlockHeightUnlocked(cancellation);

                        long totalBlockTravel = 0;
                        bool canceled = false;
                        bool changeDone = false;

                        long timestampStart = ClassUtility.GetCurrentTimestampInMillisecond();

                        ClassBlockObject lastBlockHeightUnlockedObject = await GetBlockInformationDataStrategy(lastBlockHeightUnlockedChecked, cancellation);
                        long lastBlockHeightTransactionConfirmationDone = await GetLastBlockHeightTransactionConfirmationDone(cancellation);

                        if (lastBlockHeightUnlockedObject == null)
                            canceled = true;
                        else
                        {
                            long totalTask = lastBlockHeightUnlockedChecked - lastBlockHeightTransactionConfirmationDone;

                            if (totalTask > 0)
                            {
#if DEBUG
                                Debug.WriteLine("Total task to do " + totalTask + " -> Last unlocked checked: " + lastBlockHeightUnlockedChecked + " | Last block height progress confirmation height: " + lastBlockHeightTransactionConfirmationDone);
#endif

                                ClassLog.WriteLine("Total task to do " + totalTask + " -> Last unlocked checked: " + lastBlockHeightUnlockedChecked + " | Last block height progress confirmation height: " + lastBlockHeightTransactionConfirmationDone, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);


                                #region Travel each block ranges generated, retrieve blocks by range and update confirmations.

                                long lastBlockHeightCheckpoint = ClassBlockchainDatabase.GetLastBlockHeightTransactionCheckpoint();

                                HashSet<long> listBlockHeightFullyConfirmed = new HashSet<long>();

                                #region Generate a list of block height where tx's are fully confirmed.

                                for (long i = 0; i < lastBlockHeightTransactionConfirmationDone; i++)
                                {
                                    

                                    long blockHeight = i + 1;

                                    ClassBlockObject blockInformationObject = await GetBlockInformationDataStrategy(blockHeight, cancellation);

                                    bool isFullyConfirmed = true;

                                    if (blockInformationObject == null)
                                        isFullyConfirmed = false;
                                    else
                                    {
                                        isFullyConfirmed = !blockInformationObject.BlockTransactionConfirmationCheckTaskDone || !blockInformationObject.BlockUnlockValid || !blockInformationObject.BlockTransactionFullyConfirmed ? false : true;

                                        if (isFullyConfirmed)
                                        {
                                            if (blockHeight > BlockchainSetting.GenesisBlockHeight)
                                            {
                                                ClassBlockObject previousBlockInformationObject = await GetBlockInformationDataStrategy(blockHeight - 1, cancellation);

                                                if (previousBlockInformationObject == null)
                                                    isFullyConfirmed = false;
                                                else
                                                {
                                                    if (!previousBlockInformationObject.BlockTransactionConfirmationCheckTaskDone || !previousBlockInformationObject.BlockUnlockValid || !previousBlockInformationObject.BlockTransactionFullyConfirmed)
                                                        isFullyConfirmed = false;
                                                }
                                            }
                                        }

                                        if (isFullyConfirmed)
                                        {
                                            listBlockHeightFullyConfirmed.Add(blockHeight);
                                            blockchainBlockConfirmationResultObject.ListBlockHeightConfirmed.Add(blockHeight);
                                        }
                                    }

                                    totalBlockTravel++;
                                }

                                #endregion

                                using (DisposableList<List<long>> listRange = new DisposableList<List<long>>())
                                {
                                    listRange.GetList = BuildListBlockHeightByRange(lastBlockHeightUnlockedChecked, listBlockHeightFullyConfirmed, cancellation);

                                    // Build a list of block height by range to proceed and ignore block height's fully confirmed.
                                    foreach (List<long> blockRange in listRange.GetList)
                                    {
                                        

                                        if (blockRange.Count > 0)
                                        {
                                            blockRange.Sort();

                                            if (blockRange.Count > 0)
                                            {
                                                long blockMinRange = blockRange[0];
                                                long blockMaxRange = blockRange[blockRange.Count - 1];

                                                using (DisposableSortedList<long, ClassBlockObject> blockDataListRetrievedRead = await GetBlockListFromBlockHeightRangeTargetFromMemoryDataCache(blockMinRange, blockMaxRange, false, true, cancellation))
                                                {
                                                    if (blockDataListRetrievedRead.Count > 0)
                                                    {
                                                        // List of blocks updated.
                                                        using (DisposableDictionary<long, ClassBlockObject> listBlockObjectUpdated = new DisposableDictionary<long, ClassBlockObject>())
                                                        {

                                                            // Retrieve by range blocks from the active memory if possible or by the cache system.
                                                            foreach (ClassBlockObject blockObject in blockDataListRetrievedRead.GetList.Values)
                                                            {
                                                                

                                                                if (blockObject != null)
                                                                {
                                                                    long blockHeight = blockObject.BlockHeight;

                                                                    canceled = blockObject.BlockTransactions == null ||
                                                                               blockHeight < BlockchainSetting.GenesisBlockHeight || blockHeight > lastBlockHeightUnlockedChecked ||
                                                                               blockObject.BlockStatus == ClassBlockEnumStatus.LOCKED ||
                                                                               !blockObject.IsConfirmedByNetwork;

                                                                    if (canceled)
                                                                        break;

                                                                    if (blockObject.IsConfirmedByNetwork)
                                                                    {
                                                                        #region Check the block data integrety if it's not done yet.

                                                                        // Clean up invalid tx's probably late or push by something else on the bad moment.
                                                                        var memPoolCheckBlockHeight = await ClassMemPoolDatabase.MemPoolContainsBlockHeight(blockObject.BlockHeight, cancellation);

                                                                        if (memPoolCheckBlockHeight.Item1 && memPoolCheckBlockHeight.Item2 > 0)
                                                                            await ClassMemPoolDatabase.RemoveMemPoolAllTxFromBlockHeightTarget(blockHeight, cancellation);

                                                                        if (!blockObject.BlockTransactionConfirmationCheckTaskDone)
                                                                        {
                                                                            blockObject.BlockTransactionConfirmationCheckTaskDone = ClassBlockUtility.DoCheckBlockTransactionConfirmation(blockObject, await GetBlockInformationDataStrategy(blockHeight - 1, cancellation));
                                                                            if (!blockObject.BlockTransactionConfirmationCheckTaskDone)
                                                                            {
                                                                                if (_dictionaryBlockObjectMemory[blockObject.BlockHeight].Content != null)
                                                                                {
                                                                                    _dictionaryBlockObjectMemory[blockObject.BlockHeight].Content.BlockTransactionConfirmationCheckTaskDone = false;
                                                                                    _dictionaryBlockObjectMemory[blockObject.BlockHeight].Content.BlockUnlockValid = false;
                                                                                    _dictionaryBlockObjectMemory[blockObject.BlockHeight].Content.BlockNetworkAmountConfirmations = 0;
                                                                                    _dictionaryBlockObjectMemory[blockObject.BlockHeight].Content.BlockSlowNetworkAmountConfirmations = 0;

                                                                                    AddOrUpdateBlockMirrorObject(_dictionaryBlockObjectMemory[blockObject.BlockHeight].Content);
                                                                                }
                                                                                else
                                                                                {
                                                                                    blockObject.BlockTransactionConfirmationCheckTaskDone = false;
                                                                                    blockObject.BlockUnlockValid = false;
                                                                                    blockObject.BlockNetworkAmountConfirmations = 0;
                                                                                    blockObject.BlockSlowNetworkAmountConfirmations = 0;

                                                                                    await AddOrUpdateMemoryDataToCache(blockObject, true, cancellation);
                                                                                }
#if DEBUG
                                                                                Debug.WriteLine("Failed to update block transaction(s) confirmation(s) on the block height: " + blockObject.BlockHeight + ", the confirmation check task failed.");
#endif
                                                                                ClassLog.WriteLine("Failed to update block transaction(s) confirmation(s) on the block height: " + blockObject.BlockHeight + ", the confirmation check task failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

                                                                                canceled = true;
                                                                                break;
                                                                            }
                                                                        }

                                                                        #endregion
                                                                    }
                                                                    else break;

                                                                    #region Attempt to increment block transaction confirmation(s).

                                                                    ClassBlockObject blockObjectUpdated = null;

                                                                    if (!blockObject.BlockTransactionFullyConfirmed)
                                                                        blockObjectUpdated = await TaskTravelBlockTransactionAsync(blockObject, lastBlockHeightUnlockedObject, listBlockObjectUpdated.GetList, cancellation);
                                                                    else
                                                                        blockObjectUpdated = blockObject;

                                                                    lastBlockHeightTransactionConfirmationDone = blockHeight;

                                                                    blockObjectUpdated.BlockTransactionConfirmationCheckTaskDone = true;
                                                                    blockObjectUpdated.BlockTotalTaskTransactionConfirmationDone = lastBlockHeightUnlockedChecked - blockObject.BlockHeight;
                                                                    blockObjectUpdated.BlockLastHeightTransactionConfirmationDone = lastBlockHeightUnlockedChecked;
                                                                    blockObjectUpdated.BlockUnlockValid = true;
                                                                    blockObjectUpdated.BlockNetworkAmountConfirmations = BlockchainSetting.BlockAmountNetworkConfirmations;
                                                                    blockObjectUpdated.TotalTransaction = blockObject.BlockTransactions.Count;
                                                                    blockObjectUpdated.TotalCoinConfirmed = 0;
                                                                    blockObjectUpdated.TotalFee = 0;
                                                                    blockObjectUpdated.TotalCoinPending = 0;
                                                                    blockObjectUpdated.TotalTransactionConfirmed = 0;

                                                                    // Update block cached confirmed retrieved.
                                                                    foreach (var txHash in blockObjectUpdated.BlockTransactions.Keys)
                                                                    {
                                                                        

                                                                        if (blockObjectUpdated.BlockTransactions[txHash].TransactionStatus)
                                                                        {
                                                                            blockObjectUpdated.BlockTransactions[txHash].TransactionTotalConfirmation = (lastBlockHeightUnlockedChecked - blockObject.BlockHeight) + 1;
                                                                            if (blockObjectUpdated.BlockTransactions[txHash].IsConfirmed)
                                                                            {
                                                                                blockObjectUpdated.TotalCoinConfirmed += blockObjectUpdated.BlockTransactions[txHash].TransactionObject.Amount - blockObjectUpdated.BlockTransactions[txHash].TotalSpend;
                                                                                blockObjectUpdated.TotalTransactionConfirmed++;
                                                                            }
                                                                            else
                                                                                blockObjectUpdated.TotalCoinPending += blockObjectUpdated.BlockTransactions[txHash].TransactionObject.Amount;


                                                                            if (blockObjectUpdated.BlockTransactions[txHash].TransactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION &&
                                                                                blockObjectUpdated.BlockTransactions[txHash].TransactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                                                                            {
                                                                                blockObjectUpdated.TotalFee += blockObject.BlockTransactions[txHash].TransactionObject.Fee;
                                                                            }
                                                                        }
                                                                    }

                                                                    blockObjectUpdated.BlockLastChangeTimestamp = ClassUtility.GetCurrentTimestampInMillisecond();


                                                                    changeDone = true;

                                                                    if (blockObject.BlockHeight >= lastBlockHeightCheckpoint + BlockchainSetting.TaskVirtualBlockCheckpoint)
                                                                    {
                                                                        ClassBlockchainDatabase.InsertCheckpoint(ClassCheckpointEnumType.BLOCK_HEIGHT_TRANSACTION_CHECKPOINT, blockObjectUpdated.BlockHeight, string.Empty, 0, 0);
                                                                        lastBlockHeightCheckpoint = blockObjectUpdated.BlockHeight;
                                                                    }

                                                                    totalBlockTravel++;
                                                                    listBlockObjectUpdated.Add(blockObject.BlockHeight, blockObjectUpdated);

                                                                    #endregion

                                                                }
                                                                else
                                                                {
#if DEBUG
                                                                    Debug.WriteLine("Failed to update block transaction(s) confirmation(s) on the block height range: " + blockMinRange + "/" + blockMaxRange + " can't retrieve back propertly data.");
#endif
                                                                    ClassLog.WriteLine("Failed to update block transaction(s) confirmation(s) on the block height range: " + blockMinRange + "/" + blockMaxRange + " can't retrieve back propertly data.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

                                                                    canceled = true;
                                                                    break;
                                                                }

                                                                if (canceled)
                                                                    break;

                                                            }

                                                            // Apply updates done.
                                                            if (changeDone && !canceled)
                                                            {
                                                                if (listBlockObjectUpdated.Count > 0)
                                                                {
                                                                    if (!await AddOrUpdateListBlockObjectOnMemoryDataCache(listBlockObjectUpdated.GetList.Values.ToList(), false, cancellation))
                                                                    {
#if DEBUG
                                                                        Debug.WriteLine("Can't update block(s) updated on the cache system, cancel task of block transaction confirmation.");
#endif
                                                                        ClassLog.WriteLine("Can't update block(s) updated on the cache system, cancel task of block transaction confirmation.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                                                        canceled = true;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }

                                                }
                                            }
                                        }
                                        if (canceled)
                                        {
                                            changeDone = false;
                                            break;
                                        }
                                    }
                                }

                                // Clean up.
                                listBlockHeightFullyConfirmed.Clear();

                                #endregion

                            }
                            else
                                changeDone = true;
                        }

                        if (changeDone && !canceled)
                        {
                            if (totalBlockTravel >= lastBlockHeightUnlockedChecked)
                            {
                                long timestampProceedTx = ClassUtility.GetCurrentTimestampInMillisecond() - timestampStart;
#if DEBUG
                                Debug.WriteLine("Time spend to confirm: " + lastBlockHeightUnlockedChecked + " blocks and to travel: " + lastBlockHeightUnlocked + " blocks: " + timestampProceedTx + " ms. Last block height confirmation done:" + lastBlockHeightTransactionConfirmationDone);
#endif
                                ClassLog.WriteLine("Time spend to confirm: " + lastBlockHeightUnlockedChecked + " blocks and to travel: " + lastBlockHeightUnlocked + " blocks: " + timestampProceedTx + " ms. Last block height confirmation done:" + lastBlockHeightTransactionConfirmationDone, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                                blockchainBlockConfirmationResultObject.Status = true;
                                blockchainBlockConfirmationResultObject.LastBlockHeightConfirmationDone = lastBlockHeightTransactionConfirmationDone;
                            }
                            else
                            {
                                blockchainBlockConfirmationResultObject.Status = true;
                                blockchainBlockConfirmationResultObject.LastBlockHeightConfirmationDone = 0;
                            }
                        }
                    }
                    // Catch the exception once cancelled.
                    catch (Exception error)
                    {
#if DEBUG
                        Debug.WriteLine("Exception pending to confirm block transaction. Details: " + error.Message);
#endif
                        ClassLog.WriteLine("Exception pending to confirm block transaction. Details: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreSlimUpdateTransactionConfirmations.Release();
            }

            return blockchainBlockConfirmationResultObject;
        }

        /// <summary>
        /// Increment block transactions confirmations on blocks fully confirmed.
        /// </summary>
        /// <param name="listBlockHeightConfirmed"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> IncrementBlockTransactionConfirmationOnBlockFullyConfirmed(List<long> listBlockHeightConfirmed, long lastBlockHeightTransactionConfirmationDone, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;

            bool result = true;
            try
            {
                try
                {
                    await _semaphoreSlimUpdateTransactionConfirmations.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;
                }
                catch
                {
                    // The task has been cancelled.
                }

                if (semaphoreUsed)
                {
                    long totalPassed = 0;
                    long lastBlockHeight = GetLastBlockHeight;
                    long taskStartTimestamp = ClassUtility.GetCurrentTimestampInMillisecond();

                    foreach (long blockHeight in listBlockHeightConfirmed)
                    {
                        

                        try
                        {
                            bool cancelConfirmations = false;

                            if (lastBlockHeight != GetLastBlockHeight)
                                break;

                            ClassBlockObject blockObjectInformations = await GetBlockInformationDataStrategy(blockHeight, cancellation);

                            if (blockObjectInformations.BlockTotalTaskTransactionConfirmationDone >= lastBlockHeightTransactionConfirmationDone ||
                                blockObjectInformations.BlockTotalTaskTransactionConfirmationDone >= lastBlockHeightTransactionConfirmationDone - blockHeight ||
                                !blockObjectInformations.BlockTransactionFullyConfirmed)
                                continue;

                            ClassBlockObject blockObjectUpdated = _dictionaryBlockObjectMemory[blockHeight].Content != null ? _dictionaryBlockObjectMemory[blockHeight].Content : await GetBlockDataStrategy(blockHeight, false, true, cancellation);

                            if (blockObjectUpdated == null)
                            {
                                result = false;
                                break;
                            }

                            blockObjectUpdated.BlockTransactionConfirmationCheckTaskDone = true;
                            blockObjectUpdated.BlockTotalTaskTransactionConfirmationDone = lastBlockHeightTransactionConfirmationDone - blockHeight;
                            blockObjectUpdated.BlockLastHeightTransactionConfirmationDone = lastBlockHeightTransactionConfirmationDone;
                            blockObjectUpdated.BlockUnlockValid = true;
                            blockObjectUpdated.BlockNetworkAmountConfirmations = BlockchainSetting.BlockAmountNetworkConfirmations;
                            blockObjectUpdated.TotalTransaction = blockObjectUpdated.BlockTransactions.Count;
                            blockObjectUpdated.TotalCoinConfirmed = 0;
                            blockObjectUpdated.TotalFee = 0;
                            blockObjectUpdated.TotalCoinPending = 0;
                            blockObjectUpdated.TotalTransactionConfirmed = 0;

                            // Update block cached confirmed retrieved.
                            foreach (var txHash in blockObjectUpdated.BlockTransactions.Keys)
                            {
                                

                                if (lastBlockHeight != GetLastBlockHeight)
                                {
                                    cancelConfirmations = true;
                                    break;
                                }

                                if (blockObjectUpdated.BlockTransactions[txHash].TransactionStatus)
                                {
                                    blockObjectUpdated.BlockTransactions[txHash].TransactionTotalConfirmation = (lastBlockHeightTransactionConfirmationDone - blockHeight) + 1;

                                    if (blockObjectUpdated.BlockTransactions[txHash].IsConfirmed)
                                    {
                                        blockObjectUpdated.TotalCoinConfirmed += blockObjectUpdated.BlockTransactions[txHash].TransactionObject.Amount - blockObjectUpdated.BlockTransactions[txHash].TotalSpend;
                                        blockObjectUpdated.TotalTransactionConfirmed++;
                                    }
                                    else
                                        blockObjectUpdated.TotalCoinPending += blockObjectUpdated.BlockTransactions[txHash].TransactionObject.Amount;


                                    if (blockObjectUpdated.BlockTransactions[txHash].TransactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION &&
                                        blockObjectUpdated.BlockTransactions[txHash].TransactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                                        blockObjectUpdated.TotalFee += blockObjectUpdated.BlockTransactions[txHash].TransactionObject.Fee;
                                }
                            }

                            if (cancelConfirmations)
                                break;

                            blockObjectUpdated.BlockLastChangeTimestamp = ClassUtility.GetCurrentTimestampInMillisecond();

                            // Update the active memory if this one is available on the active memory.
                            if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                                _dictionaryBlockObjectMemory[blockHeight].Content = blockObjectUpdated;

                            if (!await AddOrUpdateMemoryDataToCache(blockObjectUpdated, false, cancellation))
                            {
                                cancelConfirmations = true;
                                break;
                            }

                            totalPassed++;

                        }
                        catch
                        {
                            result = false;
                            break;
                        }
                    }

                    long taskDoneTimestamp = ClassUtility.GetCurrentTimestampInMillisecond();

#if DEBUG
                    Debug.WriteLine("Total block fully confirmed on their tx passed: " + totalPassed + ". Task done in: " + (taskDoneTimestamp - taskStartTimestamp) + "ms.");
#endif

                    ClassLog.WriteLine("Total block fully confirmed on their tx passed: " + totalPassed + ". Task done in: " + (taskDoneTimestamp - taskStartTimestamp) + "ms.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreSlimUpdateTransactionConfirmations.Release();
            }
            return result;
        }

        /// <summary>
        /// Travel block transactions.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="lastBlockHeightUnlockedObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<ClassBlockObject> TaskTravelBlockTransactionAsync(ClassBlockObject blockObject, ClassBlockObject lastBlockHeightUnlockedObject, Dictionary<long, ClassBlockObject> listBlockObjectUpdated, CancellationTokenSource cancellation)
        {
            if (blockObject.BlockHeight >= BlockchainSetting.GenesisBlockHeight)
            {
                long totalTaskToDo = lastBlockHeightUnlockedObject.BlockHeight - blockObject.BlockHeight;

                if (blockObject.BlockTotalTaskTransactionConfirmationDone <= totalTaskToDo && blockObject.BlockLastHeightTransactionConfirmationDone < lastBlockHeightUnlockedObject.BlockHeight)
                {
                    if (!await TravelBlockTransactionsToConfirmAsync(blockObject, lastBlockHeightUnlockedObject, listBlockObjectUpdated, cancellation))
                    {

                        ClassLog.WriteLine("Failed to increment confirmations on block height: " + blockObject.BlockHeight, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
#if DEBUG
                        Debug.WriteLine("Failed to increment confirmations on block height: " + blockObject.BlockHeight);
#endif
                    }
                }
                else
                {
                    ClassLog.WriteLine("Increment transactions confirmations on the block height: " + blockObject.BlockHeight + " already done. Heights: " + blockObject.BlockLastHeightTransactionConfirmationDone + "/" + lastBlockHeightUnlockedObject.BlockHeight, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
#if DEBUG
                    Debug.WriteLine("Increment transactions confirmations on the block height: " + blockObject.BlockHeight + " already done. Heights: " + blockObject.BlockLastHeightTransactionConfirmationDone + "/" + lastBlockHeightUnlockedObject.BlockHeight);
#endif
                }
            }
            else
                ClassLog.WriteLine("The block height: " + blockObject.BlockHeight + " is invalid.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

            return blockObject;
        }

        /// <summary>
        /// Travel block transaction(s) to confirm.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="lastBlockObjectUnlocked"></param>
        private async Task<bool> TravelBlockTransactionsToConfirmAsync(ClassBlockObject blockObject, ClassBlockObject lastBlockObjectUnlocked, Dictionary<long, ClassBlockObject> listBlockObjectUpdated, CancellationTokenSource cancellation)
        {
            bool ConfirmationTaskStatus = blockObject != null && blockObject.BlockTransactions != null;

            if (ConfirmationTaskStatus)
            {
                if (blockObject.BlockTransactions == null)
                    return false;

                try
                {
                    if (blockObject.BlockTransactions.Count > 0 && blockObject.TotalTransaction == 0 && blockObject.BlockTotalTaskTransactionConfirmationDone == 0)
                        blockObject.TotalTransaction = blockObject.BlockTransactions.Count;

                    if (blockObject.BlockTransactions.Count != blockObject.TotalTransaction)
                        return false;

                    long lastBlockHeightTransactionCheckpoint = ClassBlockchainDatabase.GetLastBlockHeightTransactionCheckpoint();

                    if (blockObject.BlockHeight <= lastBlockObjectUnlocked.BlockHeight)
                    {
                        switch (blockObject.BlockStatus)
                        {
                            case ClassBlockEnumStatus.UNLOCKED when blockObject.BlockUnlockValid
                                                                    && blockObject.BlockTransactions.Count > 0:
                                {
                                    int countTx = 0;
                                    int countTxConfirmed = 0;

                                    using (DisposableDictionary<string, string> listWalletPublicKeyCache = new DisposableDictionary<string, string>())
                                    {
                                        foreach (var blockTxPair in blockObject.BlockTransactions.OrderBy(x => x.Value.TransactionObject.TimestampSend))
                                        {

                                            

                                            string blockTransactionHash = blockTxPair.Key;

                                            if (blockObject.BlockTransactions[blockTransactionHash] != null && blockObject.BlockTransactions[blockTransactionHash].TransactionStatus)
                                            {
                                                if (blockObject.BlockTransactions[blockTransactionHash].TransactionObject.BlockHeightTransaction <= lastBlockObjectUnlocked.BlockHeight)
                                                {
                                                    countTx++;

                                                    ClassBlockTransactionEnumStatus blockTransactionStatus = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_HEIGHT_NOT_REACH;

                                                    long totalConfirmationsDone = blockObject.BlockTransactions[blockTransactionHash].TransactionTotalConfirmation;
                                                    bool updateTransactionAmountSourceList = !blockObject.BlockTransactions[blockTransactionHash].NeedUpdateAmountTransactionSource ? true : await UpdateBlockTransactionFromAmountSourceList(blockObject.BlockTransactions[blockTransactionHash], listBlockObjectUpdated, cancellation);

                                                    if (!updateTransactionAmountSourceList)
                                                    {
                                                        blockObject.BlockTransactions[blockTransactionHash].TransactionStatus = false;
                                                        blockObject.BlockTransactions[blockTransactionHash].TransactionInvalidStatus = ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;
                                                        blockObject.BlockTransactions[blockTransactionHash].TransactionInvalidRemoveTimestamp = blockObject.BlockTransactions[blockTransactionHash].TransactionObject.TimestampSend + BlockchainSetting.TransactionInvalidDelayRemove;

                                                        ClassLog.WriteLine(blockObject.BlockHeight + " | Invalid amount sources | last unlocked: " + lastBlockObjectUnlocked.BlockHeight, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                        ClassLog.WriteLine("Transaction hash: " + blockObject.BlockTransactions[blockTransactionHash].TransactionObject.TransactionHash, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                        ClassLog.WriteLine("Transaction Type: " + System.Enum.GetName(typeof(ClassTransactionEnumType), blockObject.BlockTransactions[blockTransactionHash].TransactionObject.TransactionType), ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                    }
                                                    else
                                                    {
                                                        long transactionBlockHeightStart = blockObject.BlockTransactions[blockTransactionHash].TransactionBlockHeightInsert;
                                                        long transactionBlockHeightTarget = blockObject.BlockTransactions[blockTransactionHash].TransactionBlockHeightTarget;
                                                        long transactionTotalConfirmationsToReach = transactionBlockHeightTarget - transactionBlockHeightStart;

                                                        if (totalConfirmationsDone < transactionTotalConfirmationsToReach || blockObject.BlockTransactions[blockTransactionHash].TransactionBlockHeightInsert + totalConfirmationsDone < lastBlockHeightTransactionCheckpoint)
                                                            blockTransactionStatus = await IncreaseBlockTransactionConfirmationFromTxHash(blockObject, blockTransactionHash, lastBlockObjectUnlocked, listWalletPublicKeyCache, false, true, listBlockObjectUpdated, cancellation);
                                                        // If the block height transaction is behind the last block height transaction checkpoint, we admit to have a transaction fully valided.
                                                        else
                                                            blockTransactionStatus = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_ENOUGH_CONFIRMATIONS_REACH;
                                                    }

                                                    switch (blockTransactionStatus)
                                                    {
                                                        case ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_ENOUGH_CONFIRMATIONS_REACH:
                                                        case ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_CONFIRMATIONS_INCREMENTED:
                                                            {
                                                                blockObject.BlockTransactions[blockTransactionHash].TransactionTotalConfirmation++;

                                                                if (blockTransactionStatus == ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_ENOUGH_CONFIRMATIONS_REACH)
                                                                    countTxConfirmed++;
                                                            }
                                                            break;
                                                        case ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_HEIGHT_NOT_REACH:
                                                            {
                                                                blockObject.BlockTransactions[blockTransactionHash].TransactionStatus = false;
                                                                blockObject.BlockTransactions[blockTransactionHash].TransactionInvalidRemoveTimestamp = blockObject.BlockTransactions[blockTransactionHash].TransactionObject.TimestampSend + BlockchainSetting.TransactionInvalidDelayRemove;

                                                                ClassLog.WriteLine(blockObject.BlockHeight + " | Status " + blockTransactionStatus + " | last unlocked: " + lastBlockObjectUnlocked.BlockHeight, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                            }
                                                            break;
                                                        case ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID_DATA:
                                                        case ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID:
                                                        case ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_AMOUNT_OF_CONFIRMATIONS_WRONG:
                                                            {
                                                                blockObject.BlockTransactions[blockTransactionHash].TransactionStatus = false;
                                                                blockObject.BlockTransactions[blockTransactionHash].TransactionInvalidRemoveTimestamp = blockObject.BlockTransactions[blockTransactionHash].TransactionObject.TimestampSend + BlockchainSetting.TransactionInvalidDelayRemove;

                                                                ClassLog.WriteLine(blockObject.BlockHeight + " | Status " + blockTransactionStatus + " | last unlocked: " + lastBlockObjectUnlocked.BlockHeight, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                                ClassLog.WriteLine("Transaction hash: " + blockObject.BlockTransactions[blockTransactionHash].TransactionObject.TransactionHash, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                                ClassLog.WriteLine("Transaction Type: " + System.Enum.GetName(typeof(ClassTransactionEnumType), blockObject.BlockTransactions[blockTransactionHash].TransactionObject.TransactionType), ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                            }
                                                            break;

                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (countTxConfirmed == countTx)
                                    {
                                        blockObject.BlockTransactionFullyConfirmed = true;
#if DEBUG
                                        Debug.WriteLine("Every transactions of the block height " + blockObject.BlockHeight + " are fully confirmed.");
#endif
                                        ClassLog.WriteLine("Every transactions of the block height " + blockObject.BlockHeight + " are fully confirmed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                                    }
                                    break;
                                }
                            default:
                                ConfirmationTaskStatus = false;
                                break;
                        }
                    }

                }
                catch
                {
                    ConfirmationTaskStatus = false;
                }
            }

            return ConfirmationTaskStatus;
        }

        /// <summary>
        /// Update a block transaction from his amount source list.
        /// </summary>
        /// <param name="blockTransactionToCheck"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> UpdateBlockTransactionFromAmountSourceList(ClassBlockTransaction blockTransactionToCheck, Dictionary<long, ClassBlockObject> listBlockObjectUpdated, CancellationTokenSource cancellation)
        {
            ClassTransactionEnumStatus transactionCheckStatus = await CheckTransactionAmountSourceList(blockTransactionToCheck.TransactionObject, listBlockObjectUpdated, cancellation);

            if (transactionCheckStatus == ClassTransactionEnumStatus.VALID_TRANSACTION)
            {
                using (DisposableDictionary<long, List<ClassBlockTransaction>> listBlockTransactionSpendUpdated = new DisposableDictionary<long, List<ClassBlockTransaction>>())
                {
                    using (DisposableDictionary<long, List<ClassBlockTransaction>> listBlockTransactionSpendOriginal = new DisposableDictionary<long, List<ClassBlockTransaction>>())
                    {

                        BigInteger totalToSpend = blockTransactionToCheck.TransactionObject.Amount + blockTransactionToCheck.TransactionObject.Fee;
                        BigInteger totalSpend = 0;

                        if (blockTransactionToCheck.TransactionObject.AmountTransactionSource == null)
                            return false;

                        if (blockTransactionToCheck.TransactionObject.AmountTransactionSource.Count == 0)
                            return false;

                        foreach (var amountHashSourceObject in blockTransactionToCheck.TransactionObject.AmountTransactionSource)
                        {

                            

                            long blockHeightSource = ClassTransactionUtility.GetBlockHeightFromTransactionHash(amountHashSourceObject.Key);

                            if (blockHeightSource < BlockchainSetting.GenesisBlockHeight || blockHeightSource > GetLastBlockHeight)
                                return false;

                            string transactionHash = amountHashSourceObject.Key;

                            ClassBlockTransaction blockTransaction = null;

                            if (listBlockObjectUpdated.ContainsKey(blockHeightSource))
                                if (listBlockObjectUpdated[blockHeightSource].BlockTransactions.ContainsKey(transactionHash))
                                    blockTransaction = listBlockObjectUpdated[blockHeightSource].BlockTransactions[transactionHash].Clone();

                            if (blockTransaction == null)
                                blockTransaction = (await GetBlockTransactionFromSpecificTransactionHashAndHeight(transactionHash, blockHeightSource, false, false, cancellation))?.Clone();

                            if (blockTransaction?.TransactionObject != null)
                            {
                                if (blockTransaction.TransactionObject.WalletAddressReceiver == blockTransactionToCheck.TransactionObject.WalletAddressSender)
                                {
                                    if (blockTransaction.TransactionStatus && !blockTransaction.Spent)
                                    {
                                        if (blockTransaction.TotalSpend + amountHashSourceObject.Value.Amount <=
                                            blockTransaction.TransactionObject.Amount)
                                        {
                                            if (!listBlockTransactionSpendOriginal.ContainsKey(blockHeightSource))
                                                listBlockTransactionSpendOriginal.Add(blockHeightSource, new List<ClassBlockTransaction>());

                                            listBlockTransactionSpendOriginal[blockHeightSource].Add(blockTransaction.Clone());

                                            blockTransaction.TotalSpend += amountHashSourceObject.Value.Amount;
                                            totalSpend += amountHashSourceObject.Value.Amount;

                                            if (!listBlockTransactionSpendUpdated.ContainsKey(blockHeightSource))
                                                listBlockTransactionSpendUpdated.Add(blockHeightSource, new List<ClassBlockTransaction>());

                                            listBlockTransactionSpendUpdated[blockHeightSource].Add(blockTransaction);
                                        }
                                        else
                                        {
#if DEBUG
                                            Debug.WriteLine(blockTransaction.TransactionObject.TransactionHash + " - Transaction already spent. ->  Spent: " + blockTransaction.TotalSpend + " | result: " + (blockTransaction.TotalSpend + blockTransaction.TransactionObject.Amount));
#endif
                                            return false;
                                        }
                                    }
                                    else
                                    {
#if DEBUG
                                        Debug.WriteLine(blockTransaction.TransactionObject.TransactionHash + " - Transaction already spent or invalid. -> Status " + blockTransaction.TransactionStatus + " | Spent: " + blockTransaction.TotalSpend + "/" + blockTransaction.TransactionObject.Amount);
#endif
                                        return false;
                                    }
                                }
                                else
                                {
#if DEBUG
                                    Debug.WriteLine(blockTransaction.TransactionObject.TransactionHash + " - Invalid spending:" + blockTransactionToCheck.TransactionObject.WalletAddressSender + "/" + blockTransaction.TransactionObject.WalletAddressReceiver); ;
#endif
                                    return false;
                                }
                            }
                            else return false;

                        }

                        if (totalToSpend != totalSpend)
                            return false;

                        if (!await InsertListBlockTransactionToMemoryDataCache(listBlockTransactionSpendUpdated.GetList.SelectMany(x => x.Value).ToList(), false, cancellation))
                        {
                            // Push back original block transactions if an update failed.
                            if (await InsertListBlockTransactionToMemoryDataCache(listBlockTransactionSpendOriginal.GetList.SelectMany(x => x.Value).ToList(), false, cancellation))
                            {
                                foreach (long blockHeightSource in listBlockTransactionSpendOriginal.GetList.Keys)
                                {
                                    

                                    if (listBlockObjectUpdated.ContainsKey(blockHeightSource))
                                    {
                                        foreach (ClassBlockTransaction blockTransaction in listBlockTransactionSpendOriginal[blockHeightSource])
                                        {
                                            
                                            listBlockObjectUpdated[blockHeightSource].BlockTransactions[blockTransaction.TransactionObject.TransactionHash] = blockTransaction;
                                        }
                                    }
                                }
                            }

                            return false;
                        }
                        // Push updated block transactions from the source list of a block transaction spend.
                        else
                        {
                            foreach (long blockHeightSource in listBlockTransactionSpendUpdated.GetList.Keys)
                            {
                                

                                if (listBlockObjectUpdated.ContainsKey(blockHeightSource))
                                {
                                    foreach (ClassBlockTransaction blockTransaction in listBlockTransactionSpendUpdated[blockHeightSource])
                                    {
                                        
                                        listBlockObjectUpdated[blockHeightSource].BlockTransactions[blockTransaction.TransactionObject.TransactionHash] = blockTransaction;
                                    }
                                }
                            }

                            return true;
                        }
                    }
                }
            }
#if DEBUG
            else
                Debug.WriteLine("Invalid transaction amount source list for: " + blockTransactionToCheck.TransactionObject.TransactionHash + " | Check result: " + transactionCheckStatus);
#endif

            return false;
        }

        /// <summary>
        /// Increase the amount of confirmations of a transaction once a block is unlocked.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="transactionHash"></param>
        /// <param name="lastBlockHeightUnlocked"></param>
        /// <param name="useSemaphore"></param>
        /// <param name="useCheckpoint"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<ClassBlockTransactionEnumStatus> IncreaseBlockTransactionConfirmationFromTxHash(ClassBlockObject blockObject, string transactionHash, ClassBlockObject lastBlockHeightUnlocked, DisposableDictionary<string, string> listWalletAndPublicKeysCache, bool useSemaphore, bool useCheckpoint, Dictionary<long, ClassBlockObject> listBlockObjectUpdated, CancellationTokenSource cancellation)
        {
            ClassBlockTransactionEnumStatus transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_HEIGHT_NOT_REACH;

            if (blockObject.BlockUnlockValid)
            {
                switch (lastBlockHeightUnlocked.BlockStatus)
                {
                    case ClassBlockEnumStatus.UNLOCKED:
                        {
                            long totalConfirmationsDone = blockObject.BlockTransactions[transactionHash].TransactionTotalConfirmation;
                            long transactionBlockHeightStart = blockObject.BlockTransactions[transactionHash].TransactionBlockHeightInsert;
                            long transactionBlockHeightTarget = blockObject.BlockTransactions[transactionHash].TransactionBlockHeightTarget;
                            long transactionTotalConfirmationsToReach = transactionBlockHeightTarget - transactionBlockHeightStart;


                            if (transactionBlockHeightTarget < transactionBlockHeightStart)
                                transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID_DATA;
                            else
                            {
                                if (transactionTotalConfirmationsToReach < BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations)
                                    transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID_DATA;
                                else
                                {
                                    bool cancel = false;

                                    if (totalConfirmationsDone + transactionBlockHeightStart > lastBlockHeightUnlocked.BlockHeight)
                                    {
                                        if (totalConfirmationsDone + transactionBlockHeightStart > lastBlockHeightUnlocked.BlockHeight)
                                        {
                                            cancel = true;
                                            transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_AMOUNT_OF_CONFIRMATIONS_WRONG;
                                        }
                                    }

                                    if (!cancel)
                                    {
                                        if (totalConfirmationsDone + transactionBlockHeightStart <= lastBlockHeightUnlocked.BlockHeight)
                                        {
                                            ClassTransactionEnumStatus transactionStatus;

                                            if (totalConfirmationsDone > 0)
                                                transactionStatus = ClassTransactionEnumStatus.VALID_TRANSACTION;
                                            else
                                                transactionStatus = await ClassTransactionUtility.CheckTransactionWithBlockchainData(blockObject.BlockTransactions[transactionHash].TransactionObject, false, false, false, blockObject, totalConfirmationsDone, listWalletAndPublicKeysCache, useSemaphore, cancellation);

                                            if (transactionStatus == ClassTransactionEnumStatus.VALID_TRANSACTION)
                                            {
                                                switch (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionType)
                                                {
                                                    case ClassTransactionEnumType.NORMAL_TRANSACTION:
                                                    case ClassTransactionEnumType.TRANSFER_TRANSACTION:
                                                        {
                                                            if (listWalletAndPublicKeysCache != null)
                                                            {
                                                                if (!listWalletAndPublicKeysCache.ContainsKey(blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressSender))
                                                                    listWalletAndPublicKeysCache.Add(blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressSender, blockObject.BlockTransactions[transactionHash].TransactionObject.WalletPublicKeySender);

                                                                if (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
                                                                    if (!listWalletAndPublicKeysCache.ContainsKey(blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressReceiver))
                                                                        listWalletAndPublicKeysCache.Add(blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressReceiver, blockObject.BlockTransactions[transactionHash].TransactionObject.WalletPublicKeyReceiver);
                                                            }

                                                            if (blockObject.BlockTransactions[transactionHash].TransactionTotalConfirmation < transactionTotalConfirmationsToReach)
                                                            {
                                                                var walletBalanceSender = await GetWalletBalanceFromTransaction(blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressSender, blockObject.BlockHeight, useCheckpoint, true, false, false, listBlockObjectUpdated, cancellation);
                                                                var walletBalanceReceiver = await GetWalletBalanceFromTransaction(blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressReceiver, blockObject.BlockHeight, useCheckpoint, true, false, false, listBlockObjectUpdated, cancellation);

                                                                if (walletBalanceSender.WalletBalance >= 0 && walletBalanceReceiver.WalletBalance >= 0)
                                                                    transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_CONFIRMATIONS_INCREMENTED;
                                                                else
                                                                    transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID;
                                                            }
                                                            else
                                                                transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_ENOUGH_CONFIRMATIONS_REACH;
                                                        }
                                                        break;
                                                    case ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION:
                                                    case ClassTransactionEnumType.DEV_FEE_TRANSACTION:
                                                        {
                                                            if (blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight)
                                                            {
                                                                if (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionType == ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION)
                                                                {
                                                                    if (blockObject.BlockUnlockValid)
                                                                    {
                                                                        if (blockObject.BlockWalletAddressWinner != blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressReceiver)
                                                                        {
                                                                            cancel = true;
                                                                            transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        cancel = true;
                                                                        transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressReceiver != BlockchainSetting.WalletAddressDev(blockObject.BlockTransactions[transactionHash].TransactionObject.TimestampSend))
                                                                    {
                                                                        cancel = true;
                                                                        transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID;
                                                                    }
                                                                }
                                                            }

                                                            if (!cancel)
                                                            {
                                                                if (blockObject.BlockTransactions[transactionHash].TransactionTotalConfirmation < transactionTotalConfirmationsToReach)
                                                                {
                                                                    var walletBalanceReceiver = await GetWalletBalanceFromTransaction(blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressReceiver, blockObject.BlockHeight, useCheckpoint, true, false, false, listBlockObjectUpdated, cancellation);

                                                                    if (walletBalanceReceiver.WalletBalance >= 0)
                                                                        transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_CONFIRMATIONS_INCREMENTED;
                                                                    else
                                                                        transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID;
                                                                }
                                                                else
                                                                    transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_ENOUGH_CONFIRMATIONS_REACH;
                                                            }
                                                        }
                                                        break;
                                                    default:
                                                        transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID;
                                                        break;
                                                }


                                            }
                                            else
                                            {
                                                blockObject.BlockTransactions[transactionHash].TransactionInvalidStatus = transactionStatus;
#if DEBUG
                                                Debug.WriteLine(transactionHash + " is invalid:  " + transactionStatus);
#endif
                                                ClassLog.WriteLine(transactionHash + " is invalid:  " + transactionStatus, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                                transactionResult = ClassBlockTransactionEnumStatus.TRANSACTION_BLOCK_INVALID;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            return transactionResult;
        }

        #endregion

        #endregion

        #region Specific check of data managed with the cache system.

        /// <summary>
        /// Check if a transaction hash provided already exist on blocks.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> CheckIfTransactionHashAlreadyExist(string transactionHash, long blockHeight, CancellationTokenSource cancellation)
        {
            bool inMemory = false;

            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                blockHeight = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

            #region Check on the active memory first.

            if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
            {
                if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                {
                    inMemory = true;

                    try
                    {
                        if (_dictionaryBlockObjectMemory[blockHeight].Content.BlockTransactions.ContainsKey(transactionHash))
                            return true;
                    }
                    catch
                    {
                        inMemory = false;
                    }
                }
            }

            #endregion

            if (!inMemory)
                return await CheckTransactionHashExistOnMemoryDataOnCache(transactionHash, blockHeight, cancellation);

            return false;
        }

        /// <summary>
        ///  Check if the block height contains block reward.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="blockData"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> CheckBlockHeightContainsBlockReward(long blockHeight, ClassBlockObject blockData, CancellationTokenSource cancellation)
        {
            DisposableSortedList<string, ClassBlockTransaction> blockTransactions = new DisposableSortedList<string, ClassBlockTransaction>();

            if (blockData != null)
            {
                foreach (var blockTransaction in blockData.BlockTransactions)
                {
                    

                    if (blockTransaction.Value.TransactionObject.TransactionType == ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION ||
                        blockTransaction.Value.TransactionObject.TransactionType == ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                        return true;
                }
            }
            else
            {
                if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                {
                    if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                    {
                        foreach (var blockTransaction in _dictionaryBlockObjectMemory[blockHeight].Content.BlockTransactions)
                        {
                            

                            if (blockTransaction.Value.TransactionObject.TransactionType == ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION ||
                                blockTransaction.Value.TransactionObject.TransactionType == ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                                return true;
                        }
                    }
                    else
                    {
                        if (blockTransactions.Count == 0)
                            blockTransactions = await GetTransactionListFromBlockHeightTargetFromMemoryDataCache(blockHeight, false, cancellation);
                    }
                }
            }

            if (blockTransactions.Count > 0)
            {
                bool containBlockReward = false;
                bool containDevFee = BlockchainSetting.BlockDevFee(blockHeight) == 0;

                foreach (var tx in blockTransactions.GetList)
                {
                    

                    if (tx.Value.TransactionObject.TransactionType == ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION)
                    {
                        containBlockReward = true;
                        if (BlockchainSetting.BlockDevFee(blockHeight) > 0)
                        {
                            if (containDevFee)
                                break;
                        }
                        else
                            break;
                    }
                    if (blockHeight > BlockchainSetting.GenesisBlockHeight)
                    {
                        if (BlockchainSetting.BlockDevFee(blockHeight) > 0)
                        {
                            if (tx.Value.TransactionObject.TransactionType == ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                            {
                                containDevFee = true;
                                if (containBlockReward)
                                    break;
                            }
                        }
                    }
                    else
                        containDevFee = true;
                }

                if (containDevFee || containBlockReward)
                    return true;

                using (DisposableList<ClassTransactionObject> listMemPoolTransactionObject = await ClassMemPoolDatabase.GetMemPoolTxObjectFromBlockHeight(blockHeight, false, cancellation))
                {
                    foreach (var tx in listMemPoolTransactionObject.GetList)
                    {
                        

                        if (tx.TransactionType == ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION)
                        {
                            containBlockReward = true;
                            if (containDevFee)
                                return true;
                        }
                        if (tx.TransactionType == ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                        {
                            containDevFee = true;
                            if (containBlockReward)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check amount source list of a transaction.
        /// </summary>
        /// <param name="transactionObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<ClassTransactionEnumStatus> CheckTransactionAmountSourceList(ClassTransactionObject transactionObject, Dictionary<long, ClassBlockObject> listBlockObjectUpdated, CancellationTokenSource cancellation)
        {
            if (transactionObject.AmountTransactionSource == null)
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

            if (transactionObject.AmountTransactionSource.Count == 0)
                return ClassTransactionEnumStatus.EMPTY_TRANSACTION_SOURCE_LIST;

            if (transactionObject.AmountTransactionSource.Count(x => x.Key.IsNullOrEmpty(false, out _)) > 0)
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

            if (transactionObject.AmountTransactionSource.Count(x => x.Value == null) > 0)
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

            if (transactionObject.AmountTransactionSource.Count(x => x.Value.Amount < BlockchainSetting.MinAmountTransaction) > 0)
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;


            // Check transaction source list spending.
            int countSource = transactionObject.AmountTransactionSource.Count;
            int countSourceValid = 0;
            string walletAddress = transactionObject.WalletAddressSender;

            BigInteger totalAmount = 0;

            foreach (string transactionHash in transactionObject.AmountTransactionSource.Keys)
            {
                

                long blockHeightSource = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

                if (blockHeightSource >= BlockchainSetting.GenesisBlockHeight && blockHeightSource <= GetLastBlockHeight)
                {
                    ClassBlockTransaction blockTransaction = null;

                    if (listBlockObjectUpdated.ContainsKey(blockHeightSource))
                        blockTransaction = listBlockObjectUpdated[blockHeightSource].BlockTransactions.ContainsKey(transactionHash) ? listBlockObjectUpdated[blockHeightSource].BlockTransactions[transactionHash] : await GetBlockTransactionFromSpecificTransactionHashAndHeight(transactionHash, blockHeightSource, false, false, cancellation);
                    else
                        blockTransaction = await GetBlockTransactionFromSpecificTransactionHashAndHeight(transactionHash, blockHeightSource, false, false, cancellation);

                    if (blockTransaction == null)
                        return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

                    if (blockTransaction.TransactionObject == null)
                        return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

                    if (!blockTransaction.TransactionStatus || blockTransaction.Spent)
                        return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

                    if (blockTransaction.TransactionObject.WalletAddressReceiver != walletAddress)
                        return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

                    if (!blockTransaction.IsConfirmed)
                        return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

                    if (blockTransaction.TransactionObject.Amount < transactionObject.AmountTransactionSource[transactionHash].Amount)
                        return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

                    if (blockTransaction.TransactionObject.Amount < transactionObject.AmountTransactionSource[transactionHash].Amount + blockTransaction.TotalSpend)
                        return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

                    countSourceValid++;
                    totalAmount += transactionObject.AmountTransactionSource[transactionHash].Amount;
                }
                else return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

            }

            if (countSource != countSourceValid)
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

            if (totalAmount != transactionObject.Amount + transactionObject.Fee)
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

            return ClassTransactionEnumStatus.VALID_TRANSACTION;
        }

        /// <summary>
        /// Check a transaction, this function is basic, she is normally used alone when an incoming transaction is sent by a user.
        /// This function is also used has completement with the Peer transaction check for increment block confirmations.
        /// </summary>
        /// <param name="transactionObject">The transaction object data to check.</param>
        /// <param name="blockObjectSource">The block object source if provided.</param>
        /// <param name="checkFromBlockData">If true, check the transaction with the blockchain data.</param>
        /// <param name="listWalletAndPublicKeys">Speed up lookup wallet address with public keys already checked previously if provided.</param>
        /// <param name="cancellation"></param>
        /// <returns>Return the check status result of the transaction.</returns>
        public async Task<ClassTransactionEnumStatus> CheckTransaction(ClassTransactionObject transactionObject, ClassBlockObject blockObjectSource, bool checkFromBlockData, DisposableDictionary<string, string> listWalletAndPublicKeys, CancellationTokenSource cancellation, bool external)
        {
            #region Check transaction content.

            if (transactionObject.WalletAddressSender.IsNullOrEmpty(false, out _))
                return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_SENDER;

            if (transactionObject.WalletAddressReceiver.IsNullOrEmpty(false, out _))
                return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_SENDER;

            if (transactionObject.WalletAddressSender == transactionObject.WalletAddressReceiver)
                return ClassTransactionEnumStatus.SAME_WALLET_ADDRESS;

            if (transactionObject.TransactionHash.IsNullOrEmpty(false, out _))
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_NULL_HASH;

            if (transactionObject.TransactionHash.Length != BlockchainSetting.TransactionHashSize)
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_HASH;

            // Check if the hash is in hex format.
            if (!ClassUtility.CheckHexStringFormat(transactionObject.TransactionHash))
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_FORMAT_HASH;

            if (!transactionObject.TransactionHash.Any(char.IsUpper))
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_HASH;

            if (ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionObject.TransactionHash) != transactionObject.BlockHeightTransaction)
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_HASH;

            if (transactionObject.PaymentId < 0)
                return ClassTransactionEnumStatus.INVALID_PAYMENT_ID;

            if (transactionObject.TransactionVersion != BlockchainSetting.TransactionVersion)
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_VERSION;

            #endregion

            #region Check block target confirmations.

            if (transactionObject.BlockHeightTransactionConfirmationTarget - transactionObject.BlockHeightTransaction < BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations)
                return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT_TARGET_CONFIRMATION;

            #endregion

            if (transactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION &&
                transactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION)
            {
                if (transactionObject.Fee < BlockchainSetting.MinFeeTransaction)
                    return ClassTransactionEnumStatus.INVALID_TRANSACTION_FEE;
            }

            if (transactionObject.Amount < BlockchainSetting.MinAmountTransaction)
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_AMOUNT;

            if (!ClassTransactionUtility.CheckTransactionHash(transactionObject))
                return ClassTransactionEnumStatus.INVALID_TRANSACTION_HASH;

            if (blockObjectSource != null)
                if (ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionObject.TransactionHash) != blockObjectSource.BlockHeight)
                    return ClassTransactionEnumStatus.INVALID_TRANSACTION_HASH;

            switch (transactionObject.TransactionType)
            {
                case ClassTransactionEnumType.DEV_FEE_TRANSACTION:
                    {
                        if (!transactionObject.WalletPublicKeyReceiver.IsNullOrEmpty(false, out _) ||
                            !transactionObject.WalletPublicKeySender.IsNullOrEmpty(false, out _) ||
                            transactionObject.PaymentId != 0 ||
                            transactionObject.TimestampBlockHeightCreateSend != transactionObject.TimestampSend ||
                            !transactionObject.TransactionSignatureReceiver.IsNullOrEmpty(false, out _) ||
                            !transactionObject.TransactionSignatureSender.IsNullOrEmpty(false, out _))
                        {
                            return ClassTransactionEnumStatus.INVALID_TRANSACTION_TYPE;
                        }

                        if (transactionObject.WalletAddressSender != BlockchainSetting.BlockRewardName)
                            return ClassTransactionEnumStatus.INVALID_BLOCK_REWARD_SENDER_NAME;

                        if (!ClassWalletUtility.CheckWalletAddress(transactionObject.WalletAddressReceiver))
                            return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_RECEIVER;

                        if (transactionObject.Amount != BlockchainSetting.BlockDevFee(transactionObject.BlockHeightTransaction))
                            return ClassTransactionEnumStatus.INVALID_TRANSACTION_DEV_FEE_AMOUNT;

                        if (transactionObject.AmountTransactionSource != null)
                        {
                            if (transactionObject.AmountTransactionSource.Count > 0)
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;
                        }

                        if (checkFromBlockData)
                        {
                            if (blockObjectSource != null)
                            {
                                if (blockObjectSource.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                {
                                    if (transactionObject.WalletAddressReceiver != BlockchainSetting.WalletAddressDev(transactionObject.TimestampSend))
                                        return ClassTransactionEnumStatus.INVALID_BLOCK_DEV_FEE_WALLET_ADDRESS_RECEIVER;

                                    if (transactionObject.BlockHeightTransaction > BlockchainSetting.GenesisBlockHeight)
                                    {
                                        if (transactionObject.BlockHash != blockObjectSource.BlockHash)
                                            return ClassTransactionEnumStatus.INVALID_TRANSACTION_HASH;

                                        if (ClassUtility.GetHexStringFromByteArray(BitConverter.GetBytes(transactionObject.BlockHeightTransaction)) + ClassUtility.GenerateSha3512FromString(transactionObject.TransactionHashBlockReward) != transactionObject.TransactionHash)
                                            return ClassTransactionEnumStatus.INVALID_TRANSACTION_HASH;

                                        if (transactionObject.BlockHeightTransactionConfirmationTarget - transactionObject.BlockHeightTransaction != BlockchainSetting.TransactionMandatoryBlockRewardConfirmations)
                                            return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT_TARGET_CONFIRMATION;
                                    }
                                }
                                else
                                    return ClassTransactionEnumStatus.BLOCK_HEIGHT_LOCKED;
                            }
                            else
                                return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;
                        }
                    }
                    break;
                case ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION:
                    {
                        if (transactionObject.BlockHeightTransaction > BlockchainSetting.GenesisBlockHeight)
                        {
                            if (!transactionObject.WalletPublicKeyReceiver.IsNullOrEmpty(false, out _) ||
                                !transactionObject.WalletPublicKeySender.IsNullOrEmpty(false, out _) ||
                                transactionObject.PaymentId != 0 ||
                                transactionObject.TimestampBlockHeightCreateSend != transactionObject.TimestampSend ||
                                !transactionObject.TransactionSignatureReceiver.IsNullOrEmpty(false, out _) ||
                                !transactionObject.TransactionSignatureSender.IsNullOrEmpty(false, out _))
                            {
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_TYPE;
                            }
                        }
                        else
                        {
                            if (!transactionObject.WalletPublicKeyReceiver.IsNullOrEmpty(false, out _) ||
                                transactionObject.PaymentId != 0 ||
                                transactionObject.TimestampBlockHeightCreateSend != transactionObject.TimestampSend ||
                                !transactionObject.TransactionSignatureReceiver.IsNullOrEmpty(false, out _))
                            {
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_TYPE;
                            }
                        }

                        if (transactionObject.WalletAddressSender != BlockchainSetting.BlockRewardName)
                            return ClassTransactionEnumStatus.INVALID_BLOCK_REWARD_SENDER_NAME;

                        if (!ClassWalletUtility.CheckWalletAddress(transactionObject.WalletAddressReceiver))
                            return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_RECEIVER;

                        if (transactionObject.AmountTransactionSource != null)
                            if (transactionObject.AmountTransactionSource.Count > 0)
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

                        if (transactionObject.BlockHeightTransaction > BlockchainSetting.GenesisBlockHeight)
                        {
                            if (BlockchainSetting.BlockDevFee(transactionObject.BlockHeightTransaction) > 0)
                            {
                                if (transactionObject.Amount < BlockchainSetting.BlockRewardWithDevFee(transactionObject.BlockHeightTransaction) || transactionObject.Amount > BlockchainSetting.BlockRewardWithDevFee(transactionObject.BlockHeightTransaction))
                                    return ClassTransactionEnumStatus.INVALID_TRANSACTION_BLOCK_AMOUNT;

                                if (transactionObject.Fee != BlockchainSetting.BlockDevFee(transactionObject.BlockHeightTransaction))
                                    return ClassTransactionEnumStatus.INVALID_TRANSACTION_DEV_FEE_AMOUNT;
                            }
                            else
                            {
                                if (transactionObject.Amount != BlockchainSetting.BlockReward(transactionObject.BlockHeightTransaction))
                                    return ClassTransactionEnumStatus.INVALID_TRANSACTION_BLOCK_AMOUNT;

                                // Always 0 timestamp.
                                if (transactionObject.WalletAddressReceiver != BlockchainSetting.WalletAddressDev(0))
                                    return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_RECEIVER;
                            }

                            if (transactionObject.BlockHeightTransactionConfirmationTarget - transactionObject.BlockHeightTransaction != BlockchainSetting.TransactionMandatoryBlockRewardConfirmations)
                                return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT_TARGET_CONFIRMATION;
                        }
                        else
                        {
                            if (transactionObject.Amount != BlockchainSetting.GenesisBlockAmount)
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_AMOUNT;

                            if (checkFromBlockData)
                            {
                                if (blockObjectSource != null)
                                {
                                    if (transactionObject.WalletAddressReceiver != blockObjectSource.BlockWalletAddressWinner)
                                        return ClassTransactionEnumStatus.INVALID_BLOCK_WALLET_ADDRESS_WINNER;
                                }
                                else
                                    return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;
                            }
                            else
                            {
                                //Debug.WriteLine("Final transaction hash: "+ClassBlockUtility.GetFinalTransactionHashList(new List<string>() { transactionObject.TransactionHash }, string.Empty));
                                if (ClassBlockUtility.GetFinalTransactionHashList(new List<string>() { transactionObject.TransactionHash }, string.Empty) != BlockchainSetting.GenesisBlockFinalTransactionHash)
                                    return ClassTransactionEnumStatus.INVALID_TRANSACTION_HASH;
                            }

                            #region Check signature(s) with public key(s).

                            if (!ClassWalletUtility.WalletCheckSignature(transactionObject.TransactionHash, transactionObject.TransactionSignatureSender, BlockchainSetting.WalletAddressDevPublicKey(transactionObject.TimestampSend)))
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SIGNATURE;

                            #endregion
                        }


                        if (checkFromBlockData)
                        {
                            if (blockObjectSource != null)
                            {
                                if (blockObjectSource.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                {
                                    if (transactionObject.BlockHeightTransaction > BlockchainSetting.GenesisBlockHeight)
                                    {
                                        if (transactionObject.BlockHash != blockObjectSource.BlockHash)
                                            return ClassTransactionEnumStatus.INVALID_TRANSACTION_HASH;
                                    }
                                }
                                else
                                    return ClassTransactionEnumStatus.BLOCK_HEIGHT_LOCKED;
                            }
                            else
                                return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;
                        }
                    }
                    break;
                case ClassTransactionEnumType.NORMAL_TRANSACTION:
                case ClassTransactionEnumType.TRANSFER_TRANSACTION:
                    {
                        bool checkAddressSender = false;

                        if (listWalletAndPublicKeys != null)
                        {
                            if (listWalletAndPublicKeys.ContainsKey(transactionObject.WalletAddressSender))
                            {
                                if (listWalletAndPublicKeys[transactionObject.WalletAddressSender] == transactionObject.WalletPublicKeySender)
                                    checkAddressSender = true;
                                else
                                    return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_SENDER;
                            }
                        }

                        if (!checkAddressSender)
                        {
                            if (!ClassWalletUtility.CheckWalletAddress(transactionObject.WalletAddressSender))
                                return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_SENDER;

                            if (!ClassWalletUtility.CheckWalletPublicKey(transactionObject.WalletPublicKeySender))
                                return ClassTransactionEnumStatus.INVALID_WALLET_PUBLIC_KEY;
                        }

                        bool checkAddressReceiver = false;

                        if (transactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
                        {
                            if (listWalletAndPublicKeys != null)
                            {
                                if (listWalletAndPublicKeys.ContainsKey(transactionObject.WalletAddressReceiver))
                                {
                                    if (listWalletAndPublicKeys[transactionObject.WalletAddressReceiver] == transactionObject.WalletPublicKeyReceiver)
                                        checkAddressReceiver = true;
                                    else
                                        return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_RECEIVER;
                                }
                            }
                        }

                        if (!checkAddressReceiver)
                        {
                            if (!ClassWalletUtility.CheckWalletAddress(transactionObject.WalletAddressReceiver))
                                return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_RECEIVER;

                            if (transactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
                            {
                                if (!ClassWalletUtility.CheckWalletPublicKey(transactionObject.WalletPublicKeyReceiver))
                                    return ClassTransactionEnumStatus.INVALID_WALLET_PUBLIC_KEY;
                            }
                        }

                        if (transactionObject.AmountTransactionSource != null)
                        {
                            if (transactionObject.AmountTransactionSource.Count == 0)
                                return ClassTransactionEnumStatus.EMPTY_TRANSACTION_SOURCE_LIST;

                            if (transactionObject.AmountTransactionSource.Count(x => x.Key.IsNullOrEmpty(false, out _)) > 0)
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

                            if (transactionObject.AmountTransactionSource.Count(x => x.Value == null) > 0)
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;

                            if (transactionObject.AmountTransactionSource.Count(x => x.Value.Amount < BlockchainSetting.MinAmountTransaction) > 0)
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SOURCE_LIST;
                        }
                        else
                            return ClassTransactionEnumStatus.EMPTY_TRANSACTION_SOURCE_LIST;

                        #region Check Base 64 Signature formatting.

                        if (!ClassUtility.CheckBase64String(transactionObject.TransactionSignatureSender))
                            return ClassTransactionEnumStatus.INVALID_TRANSACTION_SIGNATURE;

                        if (!ClassUtility.CheckBase64String(transactionObject.TransactionBigSignatureSender))
                            return ClassTransactionEnumStatus.INVALID_TRANSACTION_SIGNATURE;

                        if (transactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
                        {
                            if (!ClassUtility.CheckBase64String(transactionObject.TransactionSignatureReceiver))
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SIGNATURE;

                            if (!ClassUtility.CheckBase64String(transactionObject.TransactionBigSignatureReceiver))
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SIGNATURE;
                        }

                        #endregion

                        #region Check Fee.

                        if (checkFromBlockData)
                        {
                            long blockHeightSendExpected = await GetCloserBlockHeightFromTimestamp(transactionObject.TimestampBlockHeightCreateSend, cancellation);

                            long blockHeightConfirmationStartExpected = await ClassTransactionUtility.GenerateBlockHeightStartTransactionConfirmation(blockHeightSendExpected - 1, blockHeightSendExpected, cancellation);

                            long blockHeightStartConfirmationExpected = blockHeightConfirmationStartExpected + (transactionObject.BlockHeightTransactionConfirmationTarget - transactionObject.BlockHeightTransaction);

                            if (blockHeightConfirmationStartExpected != transactionObject.BlockHeightTransaction)
                                return ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT_TARGET_CONFIRMATION;

                            long blockHeightUnlockedExpected = blockHeightSendExpected - 1;

                            var feeCostFromBlockchainActivity = await ClassTransactionUtility.GetFeeCostFromWholeBlockchainTransactionActivity(blockHeightUnlockedExpected, transactionObject.BlockHeightTransaction, transactionObject.BlockHeightTransactionConfirmationTarget, cancellation);

                            if (!feeCostFromBlockchainActivity.Item2)
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_FEE;

                            var feeCostTransaction = ClassTransactionUtility.GetFeeCostSizeFromTransactionData(transactionObject) + feeCostFromBlockchainActivity.Item1;

                            if (feeCostTransaction > transactionObject.Fee)
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_FEE;

                        }
                        else
                        {
                            if (transactionObject.Fee < BlockchainSetting.MinFeeTransaction)
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_FEE;
                        }


                        #endregion

                        #region Check Public Keys results.

                        if (!checkAddressSender)
                        {
                            if (ClassWalletUtility.GenerateWalletAddressFromPublicKey(transactionObject.WalletPublicKeySender) != transactionObject.WalletAddressSender)
                                return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_SENDER_FROM_PUBLIC_KEY;
                        }

                        if (transactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
                        {
                            if (!checkAddressReceiver)
                            {
                                if (ClassWalletUtility.GenerateWalletAddressFromPublicKey(transactionObject.WalletPublicKeyReceiver) != transactionObject.WalletAddressReceiver)
                                    return ClassTransactionEnumStatus.INVALID_WALLET_ADDRESS_RECEIVER_FROM_PUBLIC_KEY;
                            }
                        }

                        #endregion

                        #region Check signature(s) with public key(s).

                        if (!ClassWalletUtility.WalletCheckSignature(transactionObject.TransactionHash, transactionObject.TransactionSignatureSender, transactionObject.WalletPublicKeySender))
                            return ClassTransactionEnumStatus.INVALID_TRANSACTION_SIGNATURE;

                        if (transactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
                        {
                            if (!ClassWalletUtility.WalletCheckSignature(transactionObject.TransactionHash, transactionObject.TransactionSignatureReceiver, transactionObject.WalletPublicKeyReceiver))
                                return ClassTransactionEnumStatus.INVALID_TRANSACTION_SIGNATURE;
                        }

                        // Check only big signature to reduce cpu cost once the block is provided and don't have pass any confirmation task.
                        if (blockObjectSource != null)
                        {
                            if (blockObjectSource.BlockTotalTaskTransactionConfirmationDone == 0)
                            {
                                if (!ClassTransactionUtility.CheckBigTransactionSignature(transactionObject, cancellation))
                                    return ClassTransactionEnumStatus.INVALID_TRANSACTION_SIGNATURE;

                            }
                        }
                        #endregion
                    }
                    break;
                default:
                    return ClassTransactionEnumStatus.INVALID_TRANSACTION_TYPE;
            }

            return ClassTransactionEnumStatus.VALID_TRANSACTION;
        }

        #endregion

        #region Misc functions who can be managed with the cache system.

        /// <summary>
        /// Calculate a wallet balance from blockchain data.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="maxBlockHeightTarget"></param>
        /// <param name="useCheckpoint"></param>
        /// <param name="buildCheckpoint"></param>
        /// <param name="isWallet"></param>
        /// <param name="useSemaphore"></param>
        /// <param name="cancellation"></param>
        /// <returns>Return the wallet balance.</returns>
        public async Task<ClassBlockchainWalletBalanceCalculatedObject> GetWalletBalanceFromTransaction(string walletAddress, long maxBlockHeightTarget, bool useCheckpoint, bool buildCheckpoint, bool isWallet, bool useSemaphore, Dictionary<long, ClassBlockObject> listBlockObjectUpdated, CancellationTokenSource cancellation)
        {
            ClassBlockchainWalletBalanceCalculatedObject blockchainWalletBalance = new ClassBlockchainWalletBalanceCalculatedObject { WalletBalance = 0, WalletPendingBalance = 0 };

            bool semaphoreEnabled = false;

            if (useSemaphore)
            {
                try
                {
                    await _semaphoreSlimGetWalletBalance.WaitAsync(cancellation.Token);
                    semaphoreEnabled = true;
                }
                catch
                {
                    // The task has been cancelled.
                }
            }

            try
            {
                if (semaphoreEnabled || !useSemaphore)
                {
                    if (useCheckpoint)
                    {
                        #region Use checkpoint for speed up the wallet balance calculation.

                        long lastBlockHeightWalletCheckpoint = 0;

                        BlockchainWalletMemoryObject blockchainWalletMemoryObject = BlockchainWalletIndexMemoryCacheObject[walletAddress, cancellation];

                        if (blockchainWalletMemoryObject != null)
                        {
                            lastBlockHeightWalletCheckpoint = blockchainWalletMemoryObject.GetLastWalletBlockHeightCheckpoint();

                            if (lastBlockHeightWalletCheckpoint > 0)
                                blockchainWalletBalance.WalletBalance = blockchainWalletMemoryObject.GetWalletBalanceCheckpoint(lastBlockHeightWalletCheckpoint);
                        }


                        long lastBlockHeightFromTransaction = 0;

                        int totalTx = 0;

                        bool allTxConfirmed = true;


                        // Travel all block height and transactions hash indexed to the wallet address.
                        for (long i = lastBlockHeightFromTransaction; i < maxBlockHeightTarget; i++)
                        {
                            long blockHeight = i;

                            cancellation?.Token.ThrowIfCancellationRequested();

                            if (blockHeight > lastBlockHeightWalletCheckpoint && blockHeight <= maxBlockHeightTarget)
                            {
                                DisposableList<ClassBlockTransaction> listTxFromCacheOrMemory;

                                if (listBlockObjectUpdated != null)
                                    listTxFromCacheOrMemory = listBlockObjectUpdated.ContainsKey(blockHeight) ? new DisposableList<ClassBlockTransaction>(false, 0, listBlockObjectUpdated[blockHeight].BlockTransactions.Values.ToArray()) : new DisposableList<ClassBlockTransaction>(false, 0, (await GetTransactionListFromBlockHeightTargetFromMemoryDataCache(blockHeight, false, cancellation))?.GetList.Values.ToArray());
                                else
                                    listTxFromCacheOrMemory = new DisposableList<ClassBlockTransaction>(false, 0, (await GetTransactionListFromBlockHeightTargetFromMemoryDataCache(blockHeight, false, cancellation))?.GetList.Values.ToArray());

                                foreach (ClassBlockTransaction blockTransaction in listTxFromCacheOrMemory.GetList.OrderBy(x => x.TransactionObject.TimestampSend))
                                {
                                    cancellation?.Token.ThrowIfCancellationRequested();

                                    if (blockTransaction != null)
                                    {
                                        if (blockTransaction.TransactionStatus)
                                        {
                                            if (blockTransaction.TransactionBlockHeightInsert <= maxBlockHeightTarget)
                                            {
                                                bool txConfirmed = false;

                                                if (blockTransaction.TransactionTotalConfirmation >= BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations)
                                                {
                                                    if (blockTransaction.IsConfirmed)
                                                    {
                                                        txConfirmed = true;
                                                        totalTx++;
                                                    }
                                                }

                                                if (txConfirmed)
                                                {
                                                    int typeTx = 0;

                                                    if (blockTransaction.TransactionObject.WalletAddressReceiver == walletAddress)
                                                        typeTx = 1;
                                                    else if (blockTransaction.TransactionObject.WalletAddressSender == walletAddress)
                                                        typeTx = 2;

                                                    switch (typeTx)
                                                    {
                                                        // Received.
                                                        case 1:
                                                            blockchainWalletBalance.WalletBalance += blockTransaction.TransactionObject.Amount;
                                                            break;
                                                        // Sent.
                                                        case 2:
                                                            blockchainWalletBalance.WalletBalance -= blockTransaction.TransactionObject.Amount;
                                                            if (blockTransaction.TransactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION)
                                                                blockchainWalletBalance.WalletBalance -= blockTransaction.TransactionObject.Fee;
                                                            break;
                                                    }
                                                }
                                                //  Take in count only sent transactions. Received tx not confirmed not increment the balance.
                                                else
                                                {
                                                    //allTxConfirmed = false;
                                                    if (blockTransaction.TransactionObject.WalletAddressSender == walletAddress)
                                                    {
                                                        blockchainWalletBalance.WalletBalance -= blockTransaction.TransactionObject.Amount;
                                                        if (blockTransaction.TransactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION)
                                                            blockchainWalletBalance.WalletBalance -= blockTransaction.TransactionObject.Fee;
                                                    }
                                                    if (blockTransaction.TransactionObject.WalletAddressReceiver == walletAddress)
                                                        blockchainWalletBalance.WalletPendingBalance += blockTransaction.TransactionObject.Amount;
                                                }


                                                if (blockHeight > lastBlockHeightFromTransaction)
                                                    lastBlockHeightFromTransaction = blockHeight;
                                            }
                                        }
                                    }
                                }

                                listTxFromCacheOrMemory?.Dispose();
                            }
                        }

                        // Do a wallet balance checkpoint only if all blocks transactions travelled are confirmed.
                        if (!isWallet)
                        {
                            if (buildCheckpoint && blockchainWalletMemoryObject != null)
                            {
                                if (allTxConfirmed && totalTx > 0)
                                {
                                    long blockHeightDifference = lastBlockHeightFromTransaction - lastBlockHeightWalletCheckpoint;

                                    if (blockHeightDifference >= BlockchainSetting.TaskVirtualWalletBalanceCheckpoint)
                                    {
                                        blockchainWalletMemoryObject.InsertWalletBalanceCheckpoint(lastBlockHeightFromTransaction, blockchainWalletBalance.WalletBalance, blockchainWalletBalance.WalletPendingBalance, totalTx, walletAddress);
                                        BlockchainWalletIndexMemoryCacheObject.AddOrUpdateWalletMemoryObject(walletAddress, blockchainWalletMemoryObject, cancellation);
                                    }
                                }
                            }
                        }

                        #endregion
                    }

                    if (!buildCheckpoint)
                    {
                        // Take in count mem pool transaction indexed only sending.
                        if (ClassMemPoolDatabase.GetCountMemPoolTx > 0)
                        {
                            using (DisposableList<ClassTransactionObject> listMemPoolTransactionObject = await ClassMemPoolDatabase.GetMemPoolTxFromWalletAddressTargetAsync(walletAddress, maxBlockHeightTarget, cancellation))
                            {
                                foreach (var memPoolTransactionIndexed in listMemPoolTransactionObject.GetList)
                                {
                                    cancellation?.Token.ThrowIfCancellationRequested();

                                    if (memPoolTransactionIndexed != null)
                                    {
                                        if (memPoolTransactionIndexed.WalletAddressSender == walletAddress)
                                        {
                                            blockchainWalletBalance.WalletBalance -= memPoolTransactionIndexed.Amount;

                                            if (memPoolTransactionIndexed.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION)
                                                blockchainWalletBalance.WalletBalance -= memPoolTransactionIndexed.Fee;
                                        }
                                        else if (memPoolTransactionIndexed.WalletAddressReceiver == walletAddress)
                                            blockchainWalletBalance.WalletPendingBalance += memPoolTransactionIndexed.Amount;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (semaphoreEnabled)
                    _semaphoreSlimGetWalletBalance.Release();
            }

            return blockchainWalletBalance;
        }

        #endregion

        #endregion


        #region Functions to manage memory.

        /// <summary>
        /// Task who automatically save objects in memory not used into cache disk.
        /// </summary>
        public void StartTaskManageActiveMemory()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    bool useSemaphoreMemory = false;
                    bool useSemaphoreUpdateTransactionConfirmation = false;

                    while (_cacheStatus)
                    {
                        if (_cancellationTokenMemoryManagement.IsCancellationRequested)
                            break;

                        if (!_pauseMemoryManagement)
                        {
                            try
                            {
                                try
                                {
                                    if (Count > 0)
                                    {
                                        await _semaphoreSlimUpdateTransactionConfirmations.WaitAsync(_cancellationTokenMemoryManagement.Token);
                                        useSemaphoreUpdateTransactionConfirmation = true;

                                        await _semaphoreSlimMemoryAccess.WaitAsync(_cancellationTokenMemoryManagement.Token);
                                        useSemaphoreMemory = true;


                                        long timestamp = ClassUtility.GetCurrentTimestampInSecond();
                                        long lastBlockHeight = GetLastBlockHeight;
                                        long limitIndexToCache = lastBlockHeight - _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxBlockCountToKeepInMemory;
                                        bool changeDone = false;

                                        // Update the memory depending of the cache system selected.
                                        switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                                        {
                                            case CacheEnumName.IO_CACHE:
                                                {
                                                    using (DisposableDictionary<long, Tuple<bool, ClassBlockObject>> dictionaryCache = new DisposableDictionary<long, Tuple<bool, ClassBlockObject>>())
                                                    {
                                                        #region List blocks to update on the cache and list blocks to push out of the memory.

                                                        for (long i = 0; i < lastBlockHeight; i++)
                                                        {
                                                            _cancellationTokenMemoryManagement.Token.ThrowIfCancellationRequested();

                                                            if (i < lastBlockHeight)
                                                            {
                                                                long blockHeight = i + 1;

                                                                // Do not cache the genesis block.
                                                                if (blockHeight > BlockchainSetting.GenesisBlockHeight)
                                                                {
                                                                    if (_pauseMemoryManagement)
                                                                        break;

                                                                    if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                                                                    {
                                                                        #region Insert/Update data cache from an element of memory recently updated. 

                                                                        // Ignore locked block.
                                                                        if (_dictionaryBlockObjectMemory[blockHeight].Content.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                                                        {
                                                                            // Used and updated frequently, update disk data to keep changes if a crash happen.
                                                                            if ((_dictionaryBlockObjectMemory[blockHeight].Content.BlockLastChangeTimestamp + _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalObjectCacheUpdateLimitTime >= timestamp ||
                                                                                !_dictionaryBlockObjectMemory[blockHeight].ObjectIndexed ||
                                                                                !_dictionaryBlockObjectMemory[blockHeight].CacheUpdated) &&
                                                                                (_dictionaryBlockObjectMemory[blockHeight].Content.IsConfirmedByNetwork
                                                                                && blockHeight >= limitIndexToCache))
                                                                            {
                                                                                dictionaryCache.Add(blockHeight, new Tuple<bool, ClassBlockObject>(false, _dictionaryBlockObjectMemory[blockHeight].Content));
                                                                            }
                                                                            // Unused elements.
                                                                            else
                                                                            {

                                                                                if (blockHeight < limitIndexToCache &&
                                                                                    _dictionaryBlockObjectMemory[blockHeight].Content.IsConfirmedByNetwork)
                                                                                {
                                                                                    if (_dictionaryBlockObjectMemory[blockHeight].Content.BlockLastChangeTimestamp + _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalBlockActiveMemoryKeepAlive <= timestamp)
                                                                                        dictionaryCache.Add(blockHeight, new Tuple<bool, ClassBlockObject>(true, _dictionaryBlockObjectMemory[blockHeight].Content));
                                                                                }
                                                                            }
                                                                        }

                                                                        #endregion
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        #endregion

                                                        #region Push data updated to the cache, released old data from the active memory.

                                                        if (dictionaryCache.Count > 0)
                                                        {

                                                            #region Push blocks to put out of memory to the cache system.

                                                            using (DisposableList<ClassBlockObject> blockObjectToCacheOut = new DisposableList<ClassBlockObject>())
                                                            {

                                                                foreach (var blockPair in dictionaryCache.GetList.Where(x => x.Value.Item1))
                                                                {
                                                                    _cancellationTokenMemoryManagement.Token.ThrowIfCancellationRequested();
                                                                    blockObjectToCacheOut.Add(blockPair.Value.Item2);
                                                                }

                                                                if (blockObjectToCacheOut.Count > 0)
                                                                {
                                                                    if (await AddOrUpdateListBlockObjectOnMemoryDataCache(blockObjectToCacheOut.GetList, true, _cancellationTokenMemoryManagement))
                                                                    {
                                                                        foreach (var block in blockObjectToCacheOut.GetList)
                                                                        {
                                                                            _cancellationTokenMemoryManagement.Token.ThrowIfCancellationRequested();
                                                                            _dictionaryBlockObjectMemory[block.BlockHeight].CacheUpdated = true;
                                                                            _dictionaryBlockObjectMemory[block.BlockHeight].ObjectIndexed = true;
                                                                            _dictionaryBlockObjectMemory[block.BlockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_PERSISTENT_CACHE;
                                                                            _dictionaryBlockObjectMemory[block.BlockHeight].Content = null;
                                                                        }
                                                                        changeDone = true;
#if DEBUG
                                                                        Debug.WriteLine("Total block object(s) cached out of memory: " + blockObjectToCacheOut.Count);
#endif
                                                                        ClassLog.WriteLine("Memory management - Total block object(s) cached out of memory: " + blockObjectToCacheOut.Count, ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                                                    }
                                                                }
                                                            }

                                                            #endregion

                                                            #region Push updated blocks to the cache system.

                                                            using (DisposableList<ClassBlockObject> blockObjectToCacheUpdate = new DisposableList<ClassBlockObject>())
                                                            {
                                                                foreach (var blockPair in dictionaryCache.GetList.Where(x => !x.Value.Item1))
                                                                {
                                                                    _cancellationTokenMemoryManagement.Token.ThrowIfCancellationRequested();
                                                                    blockObjectToCacheUpdate.Add(blockPair.Value.Item2);
                                                                }

                                                                if (blockObjectToCacheUpdate.Count > 0)
                                                                {
                                                                    if (await AddOrUpdateListBlockObjectOnMemoryDataCache(blockObjectToCacheUpdate.GetList, true, _cancellationTokenMemoryManagement))
                                                                    {
                                                                        changeDone = true;
                                                                        foreach (var block in blockObjectToCacheUpdate.GetList)
                                                                        {
                                                                            _cancellationTokenMemoryManagement.Token.ThrowIfCancellationRequested();
                                                                            _dictionaryBlockObjectMemory[block.BlockHeight].CacheUpdated = true;
                                                                            _dictionaryBlockObjectMemory[block.BlockHeight].ObjectIndexed = true;
                                                                            _dictionaryBlockObjectMemory[block.BlockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_CACHE;
                                                                        }
#if DEBUG
                                                                        Debug.WriteLine("Total block object(s) cached updated and keep in memory: " + blockObjectToCacheUpdate.Count);
#endif
                                                                        ClassLog.WriteLine("Memory management - Total block object(s) cached updated and keep in memory: " + blockObjectToCacheUpdate.Count, ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                                                    }
                                                                }
                                                            }

                                                            #endregion
                                                        }

                                                        #endregion

                                                        // Purge the IO Cache system.
                                                        if (changeDone)
                                                            await _cacheIoSystem.PurgeCacheIoSystem(_cancellationTokenMemoryManagement);
                                                    }
                                                }
                                                break;

                                        }

                                        // Update the block transaction cache.
                                        long totalRemoved = await UpdateBlockTransactionCacheTask(_cancellationTokenMemoryManagement);

                                        if (totalRemoved > 0)
                                            changeDone = true;

                                        // Update the blockchain wallet index memory cache.
                                        //totalRemoved = await BlockchainWalletIndexMemoryCacheObject.UpdateBlockchainWalletIndexMemoryCache(_cancellationTokenMemoryManagement);

                                        if (totalRemoved > 0)
                                            changeDone = true;

                                        if (changeDone)
                                            ClassUtility.CleanGc();
                                    }
                                }
                                catch (Exception error)
                                {
#if DEBUG
                                    Debug.WriteLine("Error on the memory update task. Exception: " + error.Message);
#endif
                                    ClassLog.WriteLine("Error on the memory update task. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                }
                            }
                            finally
                            {
                                if (useSemaphoreUpdateTransactionConfirmation)
                                {
                                    _semaphoreSlimUpdateTransactionConfirmations.Release();
                                    useSemaphoreUpdateTransactionConfirmation = false;
                                }
                                if (useSemaphoreMemory)
                                {
                                    _semaphoreSlimMemoryAccess.Release();
                                    useSemaphoreMemory = false;
                                }
                            }
                        }
#if DEBUG
                        else
                            Debug.WriteLine("Memory management in pause status.");
#endif
                        await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.GlobalTaskManageMemoryInterval);
                    }

                }, _cancellationTokenMemoryManagement.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Stop the task of memory management who save memory in cache disk (SQLite).
        /// </summary>
        public void SetMemoryManagementPauseStatus(bool pauseStatus)
        {
            _pauseMemoryManagement = pauseStatus;
        }

        /// <summary>
        /// Stop Memory Management.
        /// </summary>
        public async Task StopMemoryManagement()
        {
            SetMemoryManagementPauseStatus(true);
            bool semaphoreUpdateTransactionConfirmationUsed = false;
            bool semaphoreMemoryAccessUsed = false;

            try
            {
                await _semaphoreSlimUpdateTransactionConfirmations.WaitAsync();
                semaphoreUpdateTransactionConfirmationUsed = true;
                await _semaphoreSlimMemoryAccess.WaitAsync();
                semaphoreMemoryAccessUsed = true;

                if (_cancellationTokenMemoryManagement != null)
                    if (!_cancellationTokenMemoryManagement.IsCancellationRequested)
                        _cancellationTokenMemoryManagement.Cancel();
            }
            finally
            {
                if (semaphoreUpdateTransactionConfirmationUsed)
                    _semaphoreSlimUpdateTransactionConfirmations.Release();

                if (semaphoreMemoryAccessUsed)
                    _semaphoreSlimMemoryAccess.Release();
            }
        }

        /// <summary>
        /// Force to purge the memory data cache.
        /// </summary>
        /// <param name="useSemaphore">Lock or not the access of the cache, determine also if the access require a simple get of the data cache or not.</param>
        /// <returns></returns>
        private async Task ForcePurgeMemoryDataCache(CancellationTokenSource cancellation)
        {
            switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
            {
                case CacheEnumName.IO_CACHE:
                    await _cacheIoSystem.PurgeCacheIoSystem(cancellation);
                    break;
            }
        }

        /// <summary>
        /// Get block memory data from cache, depending of the key selected and depending of the cache system selected.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive">Determine if the access require a simple get of the data cache or not.</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<ClassBlockObject> GetBlockMemoryDataFromCacheByKey(long blockHeight, bool keepAlive, bool clone, CancellationTokenSource cancellation)
        {
            ClassBlockObject blockObject = null;

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            blockObject = await _cacheIoSystem.GetIoBlockObject(blockHeight, keepAlive, clone, cancellation);

                            if (blockObject?.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                AddOrUpdateBlockMirrorObject(blockObject);
                        }
                        break;
                }
            }
            return blockObject;
        }

        /// <summary>
        /// Get block information memory data from cache, depending of the key selected and depending of the cache system selected.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<ClassBlockObject> GetBlockInformationMemoryDataFromCacheByKey(long blockHeight, CancellationTokenSource cancellation)
        {
            ClassBlockObject blockObject = null;

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            blockObject = await _cacheIoSystem.GetIoBlockInformationObject(blockHeight, cancellation);

                            if (blockObject?.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                AddOrUpdateBlockMirrorObject(blockObject);
                        }
                        break;
                }

            }

            return blockObject;
        }

        /// <summary>
        /// Get block information memory data from cache, depending of the key selected and depending of the cache system selected.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<int> GetBlockTransactionCountMemoryDataFromCacheByKey(long blockHeight, CancellationTokenSource cancellation)
        {
            int transactionCount = 0;

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        transactionCount = await _cacheIoSystem.GetIoBlockTransactionCount(blockHeight, cancellation);
                        break;
                }
            }
            return transactionCount;
        }

        /// <summary>
        /// Add or update data to cache.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="keepAlive">Keep alive or not the data provided to the cache in the active memory.</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> AddOrUpdateMemoryDataToCache(ClassBlockObject blockObject, bool keepAlive, CancellationTokenSource cancellation)
        {

            bool updateAddResult = !_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase;

            if (GetLastBlockHeight < blockObject.BlockHeight || !ContainsKey(blockObject.BlockHeight))
                _dictionaryBlockObjectMemory.Add(blockObject.BlockHeight, new BlockchainMemoryObject());

            if (_dictionaryBlockObjectMemory[blockObject.BlockHeight].Content != null)
                _dictionaryBlockObjectMemory[blockObject.BlockHeight].Content = blockObject;

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            updateAddResult = await _cacheIoSystem.PushOrUpdateIoBlockObject(blockObject, keepAlive, cancellation);
                        }
                        break;
                }
            }

            if (updateAddResult)
                await UpdateListBlockTransactionCache(blockObject.BlockTransactions.Values.ToList(), cancellation, true);

            return updateAddResult;
        }

        /// <summary>
        /// Remove data from the cache.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> RemoveMemoryDataOfCache(long blockHeight, CancellationTokenSource cancellation)
        {
            bool deleteResult = false;

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        deleteResult = await _cacheIoSystem.TryDeleteIoBlockObject(blockHeight, cancellation);
                        break;
                }
            }
            return deleteResult;
        }

        /// <summary>
        /// Insert directly a block transaction into the memory cache.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive">Keep alive or not the data updated in the active memory.</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> InsertBlockTransactionToMemoryDataCache(ClassBlockTransaction blockTransaction, long blockHeight, bool keepAlive, CancellationTokenSource cancellation)
        {
            bool result = false;

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            result = await _cacheIoSystem.InsertOrUpdateBlockTransactionObject(blockTransaction, blockHeight, keepAlive, cancellation);

                            if (result)
                                await UpdateBlockTransactionCache(blockTransaction.Clone(), cancellation);
                        }
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Insert directly a list of block transaction into the memory cache.
        /// </summary>
        /// <param name="listBlockTransaction"></param>
        /// <param name="keepAlive">Keep alive or not the data updated in the active memory.</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> InsertListBlockTransactionToMemoryDataCache(List<ClassBlockTransaction> listBlockTransaction, bool keepAlive, CancellationTokenSource cancellation)
        {
            bool result = false;

            using (DisposableList<ClassBlockTransaction> listBlockTransactionToPushOnCache = new DisposableList<ClassBlockTransaction>())
            {

                foreach (var blockTransaction in listBlockTransaction)
                {
                    

                    if (_dictionaryBlockObjectMemory[blockTransaction.TransactionBlockHeightInsert].Content != null)
                        _dictionaryBlockObjectMemory[blockTransaction.TransactionBlockHeightInsert].Content.BlockTransactions[blockTransaction.TransactionObject.TransactionHash] = blockTransaction;
                    else
                        listBlockTransactionToPushOnCache.Add(blockTransaction);
                }

                if (listBlockTransactionToPushOnCache.Count == 0)
                    return true;

                if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
                {
                    switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                    {
                        case CacheEnumName.IO_CACHE:
                            {
                                result = await _cacheIoSystem.InsertOrUpdateListBlockTransactionObject(listBlockTransactionToPushOnCache.GetList, keepAlive, cancellation);

                                if (result && keepAlive)
                                    await UpdateListBlockTransactionCache(listBlockTransaction.ToList(), cancellation, false);
                            }
                            break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check if the block height already exist on the cache.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> CheckBlockHeightExistOnMemoryDataCache(long blockHeight, CancellationTokenSource cancellation)
        {
            bool result = false;

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            result = await _cacheIoSystem.ContainIoBlockHeight(blockHeight, cancellation);
                        }
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Check if the transaction hash exist on the cache.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> CheckTransactionHashExistOnMemoryDataOnCache(string transactionHash, long blockHeight, CancellationTokenSource cancellation)
        {

            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                blockHeight = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

            if (await ContainBlockTransactionHashInCache(transactionHash, blockHeight, cancellation))
                return true;

            bool result = false;

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            result = await _cacheIoSystem.CheckTransactionHashExistOnIoBlockCached(transactionHash, blockHeight, cancellation);
                        }
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Add or update a list of block objects on the cache.
        /// </summary>
        /// <param name="listBlockObjects"></param>
        /// <param name="keepAliveData">Keep alive or not the data saved.</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> AddOrUpdateListBlockObjectOnMemoryDataCache(List<ClassBlockObject> listBlockObjects, bool keepAliveData, CancellationTokenSource cancellation)
        {
            bool result = false;

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {

                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            result = await _cacheIoSystem.PushOrUpdateListIoBlockObject(listBlockObjects, keepAliveData, cancellation);

                            if (result)
                            {
                                foreach (ClassBlockObject blockObject in listBlockObjects)
                                {
                                    

                                    if (_dictionaryBlockObjectMemory[blockObject.BlockHeight].Content != null)
                                        _dictionaryBlockObjectMemory[blockObject.BlockHeight].Content = blockObject;
                                    else
                                        AddOrUpdateBlockMirrorObject(blockObject);

                                    await UpdateListBlockTransactionCache(blockObject.BlockTransactions.Values.ToList(), cancellation, true);
                                }
                            }
                        }
                        break;
                }
            }
            else
            {
                foreach (ClassBlockObject blockObject in listBlockObjects)
                {
                    

                    if (_dictionaryBlockObjectMemory[blockObject.BlockHeight].Content != null)
                        _dictionaryBlockObjectMemory[blockObject.BlockHeight].Content = blockObject;

                    await UpdateListBlockTransactionCache(blockObject.BlockTransactions.Values.ToList(), cancellation, true);
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieve back a transaction by his hash from memory cache or from the active memory.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="useBlockTransactionCache"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<ClassBlockTransaction> GetBlockTransactionByTransactionHashFromMemoryDataCache(string transactionHash, long blockHeight, bool useBlockTransactionCache, bool keepAlive, CancellationTokenSource cancellation)
        {
            ClassBlockTransaction resultBlockTransaction = null;


            if (useBlockTransactionCache)
            {
                resultBlockTransaction = await GetBlockTransactionCached(transactionHash, blockHeight, cancellation);

                if (resultBlockTransaction != null)
                    return resultBlockTransaction;
            }

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            resultBlockTransaction = await _cacheIoSystem.GetBlockTransactionFromTransactionHashOnIoBlockCached(transactionHash, blockHeight, keepAlive, cancellation);

                            if (resultBlockTransaction != null && useBlockTransactionCache)
                                await UpdateBlockTransactionCache(resultBlockTransaction.Clone(), cancellation);
                        }
                        break;
                }

                if (resultBlockTransaction == null)
                {
                    if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                    {
                        if (_dictionaryBlockObjectMemory[blockHeight].Content.BlockTransactions.ContainsKey(transactionHash))
                            return _dictionaryBlockObjectMemory[blockHeight].Content.BlockTransactions[transactionHash].Clone();
                    }
                }
            }
            else
            {
                if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                {
                    if (_dictionaryBlockObjectMemory[blockHeight].Content.BlockTransactions.ContainsKey(transactionHash))
                        return _dictionaryBlockObjectMemory[blockHeight].Content.BlockTransactions[transactionHash].Clone();
                }
            }
            return resultBlockTransaction;
        }

        /// <summary>
        /// Retrieve back a list of transaction by a list of transaction hash and a block height target, from the active memory or the cache.
        /// </summary>
        /// <param name="listTransactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="useBlockTransactionCache"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<DisposableList<ClassBlockTransaction>> GetListBlockTransactionByListTransactionHashFromMemoryDataCache(List<string> listTransactionHash, long blockHeight, bool useBlockTransactionCache, bool keepAlive, CancellationTokenSource cancellation)
        {
            DisposableList<ClassBlockTransaction> listBlockTransaction = new DisposableList<ClassBlockTransaction>();

            if (useBlockTransactionCache)
            {
                listBlockTransaction = await GetListBlockTransactionCache(listTransactionHash, blockHeight, cancellation);

                if (listBlockTransaction.Count == listTransactionHash.Count)
                    return listBlockTransaction;
                else
                    listBlockTransaction.Clear();
            }

            if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
            {
                try
                {
                    foreach (string transactionHash in listTransactionHash)
                    {
                        

                        if (_dictionaryBlockObjectMemory[blockHeight].Content.BlockTransactions.ContainsKey(transactionHash))
                            listBlockTransaction.Add(_dictionaryBlockObjectMemory[blockHeight].Content.BlockTransactions[transactionHash].Clone());
                    }

                    return listBlockTransaction;
                }
                catch
                {
                    listBlockTransaction.Clear();
                }
            }

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            listBlockTransaction.GetList = await _cacheIoSystem.GetListBlockTransactionFromListTransactionHashAndBlockHeightTarget(listTransactionHash, blockHeight, keepAlive, cancellation);
                            if (listBlockTransaction.Count == listTransactionHash.Count && useBlockTransactionCache)
                                await UpdateListBlockTransactionCache(listBlockTransaction.GetList.ToList(), cancellation, false);
                        }
                        break;
                }
            }

            return listBlockTransaction;
        }

        /// <summary>
        /// Retrieve back every block transactions by a block height target from the memory cache or the active memory.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<DisposableSortedList<string, ClassBlockTransaction>> GetTransactionListFromBlockHeightTargetFromMemoryDataCache(long blockHeight, bool keepAlive, CancellationTokenSource cancellation)
        {
            DisposableSortedList<string, ClassBlockTransaction> listBlockTransaction = new DisposableSortedList<string, ClassBlockTransaction>();

            #region Check the active memory.

            if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
            {
                if (_dictionaryBlockObjectMemory[blockHeight]?.Content != null)
                {
                    try
                    {
                        foreach (var blockTransaction in _dictionaryBlockObjectMemory[blockHeight].Content.BlockTransactions)
                        {
                            if (cancellation.Token.IsCancellationRequested)
                                break;

                            listBlockTransaction.Add(blockTransaction.Key, blockTransaction.Value.Clone());
                        }

                        if (listBlockTransaction.Count > 0)
                            return listBlockTransaction;
                    }
                    // Use the cache if an exception appear.
                    catch
                    {
                        listBlockTransaction.Clear();
                    }
                }
            }

            #endregion

            #region Use the block transaction cache if possible.

            ClassBlockObject blockInformationObject = await GetBlockMirrorObject(blockHeight, cancellation);

            if (blockInformationObject != null)
            {
                if (blockInformationObject.TotalTransaction > 0)
                {

                    // Retrieve cache transaction if they exist.

                    var getListBlockTransaction = await GetEachBlockTransactionFromBlockHeightCached(blockHeight, cancellation);

                    if (getListBlockTransaction != null)
                    {
                        if (getListBlockTransaction.Count == blockInformationObject.TotalTransaction)
                        {
                            foreach (ClassCacheIoBlockTransactionObject blockTransaction in getListBlockTransaction)
                            {
                                

                                if (blockTransaction.BlockTransaction != null)
                                {
                                    if (!listBlockTransaction.ContainsKey(blockTransaction.BlockTransaction.TransactionObject.TransactionHash))
                                        listBlockTransaction.Add(blockTransaction.BlockTransaction.TransactionObject.TransactionHash, blockTransaction.BlockTransaction);
                                }
                                else
                                {
                                    listBlockTransaction.Clear();
                                    break;
                                }
                            }

                            if (listBlockTransaction.Count == blockInformationObject.TotalTransaction)
                                return listBlockTransaction;
                            else
                                listBlockTransaction.Clear();
                        }
                    }
                }
            }


            #endregion

            #region Then ask the cache system.

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            foreach (var blockTransaction in await _cacheIoSystem.GetBlockTransactionListFromBlockHeightTarget(blockHeight, keepAlive, cancellation))
                            {
                                

                                if (blockTransaction.Value != null)
                                {
                                    if (!listBlockTransaction.ContainsKey(blockTransaction.Key))
                                        listBlockTransaction.Add(blockTransaction.Key, blockTransaction.Value);
                                }
                            }

                            if (listBlockTransaction.Count > 0)
                                await UpdateListBlockTransactionCache(listBlockTransaction.GetList.Values.ToList(), cancellation, false);
                        }
                        break;
                }
            }

            #endregion

            return listBlockTransaction;
        }

        /// <summary>
        /// Retrieve back a block list by a block height range target from the cache or the active memory if possible.
        /// </summary>
        /// <param name="blockHeightStart"></param>
        /// <param name="blockHeightEnd"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<DisposableSortedList<long, ClassBlockObject>> GetBlockListFromBlockHeightRangeTargetFromMemoryDataCache(long blockHeightStart, long blockHeightEnd, bool keepAlive, bool clone, CancellationTokenSource cancellation)
        {
            DisposableSortedList<long, ClassBlockObject> listBlockObjects = new DisposableSortedList<long, ClassBlockObject>();

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {

                if (GetLastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                {
                    #region Generate at first the list of block object in the active memory and change range if necessary.
                    // Do not allow invalid range index lower than the genesis block height.
                    if (blockHeightStart < BlockchainSetting.GenesisBlockHeight)
                        blockHeightStart = BlockchainSetting.GenesisBlockHeight;

                    // Do not allow invalid range index above the maximum of blocks indexed.
                    if (blockHeightEnd > Count)
                        blockHeightEnd = Count;

                    List<long> listBlockHeightTargetCached = new List<long>();

                    HashSet<long> blockListAlreadyRetrieved = new HashSet<long>();

                    // Check if some data are in the active memory first.
                    for (long i = blockHeightStart - 1; i < blockHeightEnd; i++)
                    {
                        

                        if (i <= blockHeightEnd)
                        {
                            long blockHeight = i + 1;

                            if (blockHeight >= blockHeightStart && blockHeight <= blockHeightEnd)
                            {
                                if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                                {
                                    blockListAlreadyRetrieved.Add(blockHeight);
                                    listBlockObjects.Add(blockHeight, clone ? _dictionaryBlockObjectMemory[blockHeight].Content.DirectCloneBlockObject() : _dictionaryBlockObjectMemory[blockHeight].Content);
                                }
                                else
                                    listBlockHeightTargetCached.Add(blockHeight);
                            }
                        }
                    }
                    #endregion

                    switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                    {
                        case CacheEnumName.IO_CACHE:
                            {
                                if (listBlockHeightTargetCached.Count > 0)
                                {
                                    try
                                    {
                                        // Calculated again the new range to retrieve.
                                        long minBlockHeight = listBlockHeightTargetCached[0];
                                        long maxBlockHeight = listBlockHeightTargetCached[listBlockHeightTargetCached.Count - 1];

                                        listBlockObjects.GetList = await _cacheIoSystem.GetBlockObjectListFromBlockHeightRange(minBlockHeight, maxBlockHeight, blockListAlreadyRetrieved, listBlockObjects.GetList, keepAlive, clone, cancellation);
                                    }
                                    catch (Exception error)
                                    {
                                        ClassLog.WriteLine("Error on trying to retrieve a list of blocks cached. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
#if DEBUG
                                        Debug.WriteLine("Error on trying to retrieve a list of blocks cached. Exception: " + error.Message);
#endif
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            else
            {
                #region Generate the list of block object in the active memory and change range if necessary.
                // Do not allow invalid range index lower than the genesis block height.
                if (blockHeightStart < BlockchainSetting.GenesisBlockHeight)
                    blockHeightStart = BlockchainSetting.GenesisBlockHeight;

                // Do not allow invalid range index above the maximum of blocks indexed.
                if (blockHeightEnd > Count)
                    blockHeightEnd = Count;

                // Check if some data are in the active memory first.
                for (long i = blockHeightStart - 1; i < blockHeightEnd; i++)
                {
                    

                    if (i <= blockHeightEnd)
                    {
                        long blockHeight = i + 1;

                        if (blockHeight >= blockHeightStart && blockHeight <= blockHeightEnd)
                            if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                                listBlockObjects.Add(blockHeight, clone ? _dictionaryBlockObjectMemory[blockHeight].Content.DirectCloneBlockObject() : _dictionaryBlockObjectMemory[blockHeight].Content);
                    }
                }

                #endregion
            }
            return listBlockObjects;
        }

        /// <summary>
        /// Retrieve back a block information list by a list of block height target from the cache or the active memory if possible.
        /// </summary>
        /// <param name="listBlockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<DisposableSortedList<long, ClassBlockObject>> GetBlockInformationListByBlockHeightListTargetFromMemoryDataCache(DisposableList<long> listBlockHeight, CancellationTokenSource cancellation)
        {
            DisposableSortedList<long, ClassBlockObject> listBlockInformation = new DisposableSortedList<long, ClassBlockObject>();

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                if (listBlockHeight.Count > 0)
                {
                    switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                    {
                        case CacheEnumName.IO_CACHE:
                            {
                                listBlockInformation.GetList = await _cacheIoSystem.GetIoListBlockInformationObject(listBlockHeight, cancellation);

                                foreach (var blockPair in listBlockInformation.GetList)
                                {
                                    
                                    AddOrUpdateBlockMirrorObject(blockPair.Value);
                                }
                            }
                            break;
                    }
                }
            }

            return listBlockInformation;
        }

        /// <summary>
        /// Retrieve back block objects from the cache to the active memory by a range of block height provided.
        /// </summary>
        /// <param name="blockHeightStart"></param>
        /// <param name="blockHeightEnd"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task RetrieveBlockFromBlockHeightRangeTargetFromMemoryDataCacheToActiveMemory(long blockHeightStart, long blockHeightEnd, CancellationTokenSource cancellation)
        {
            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
            {
                switch (_blockchainDatabaseSetting.BlockchainCacheSetting.CacheName)
                {
                    case CacheEnumName.IO_CACHE:
                        {
                            using (DisposableSortedList<long, ClassBlockObject> listBlockObject = await GetBlockListFromBlockHeightRangeTargetFromMemoryDataCache(blockHeightStart, blockHeightEnd, false, false, cancellation))
                            {
                                foreach (var block in listBlockObject.GetList)
                                {
                                    

                                    if (block.Value != null)
                                    {

                                        if (_dictionaryBlockObjectMemory.ContainsKey(block.Key))
                                        {
                                            if (_dictionaryBlockObjectMemory[block.Key].Content == null)
                                            {
                                                _dictionaryBlockObjectMemory[block.Key].Content = block.Value;
                                                _dictionaryBlockObjectMemory[block.Key].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;
                                            }
                                            else
                                            {
                                                // If the block height data is on the active memory, compare the last block change timestamp with the one of the block data cached.
                                                if (_dictionaryBlockObjectMemory[block.Key].Content.BlockLastChangeTimestamp < block.Value.BlockLastChangeTimestamp)
                                                {
                                                    _dictionaryBlockObjectMemory[block.Key].Content = block.Value;
                                                    _dictionaryBlockObjectMemory[block.Key].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;
                                                }
                                            }
                                        }

                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Get an object from get.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<ClassBlockObject> GetObjectByKeyFromMemoryOrCacheAsync(long blockHeight, CancellationTokenSource cancellation)
        {

            switch (_dictionaryBlockObjectMemory[blockHeight].ObjectCacheType)
            {
                // Retrieve it from the active memory if not empty, otherwise, retrieved it from the persistent cache.
                case CacheBlockMemoryEnumState.IN_CACHE:
                case CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY:
                    {
                        if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                        {
                            // In this case, we retrieve the element of the active memory.
                            if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                                return _dictionaryBlockObjectMemory[blockHeight].Content;


                            _dictionaryBlockObjectMemory[blockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_PERSISTENT_CACHE;
                            return await GetObjectByKeyFromMemoryOrCacheAsync(blockHeight, cancellation);
                        }
                    }
                    break;
                // Retrieved it from the persistent cache, otherwise pending to retrieve the data of the persistent cache if the active memory is not empty return the active memory instead.
                case CacheBlockMemoryEnumState.IN_PERSISTENT_CACHE:
                    {
                        // In this case, we retrieve the element of the active memory.
                        if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                        {
                            if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                            {
                                _dictionaryBlockObjectMemory[blockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;
                                return _dictionaryBlockObjectMemory[blockHeight].Content;
                            }


                            ClassBlockObject getBlockObject = null;
                            bool semaphoreUsed = false;

                            try
                            {
                                try
                                {
                                    await _semaphoreSlimMemoryAccess.WaitAsync(cancellation.Token);
                                    semaphoreUsed = true;
                                }
                                catch
                                {
                                    // The task has been cancelled.
                                }

                                if (semaphoreUsed)
                                {
                                    getBlockObject = await GetBlockMemoryDataFromCacheByKey(blockHeight, false, false, cancellation);
                                    if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                                    {
                                        // In this case, we retrieve the element of the active memory.
                                        if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                                        {
                                            _dictionaryBlockObjectMemory[blockHeight].ObjectCacheType = CacheBlockMemoryEnumState.IN_ACTIVE_MEMORY;

                                            getBlockObject = _dictionaryBlockObjectMemory[blockHeight].Content;
                                        }
                                    }
                                    else
                                    {
                                        if (getBlockObject != null)
                                            await Add(getBlockObject.BlockHeight, null, CacheBlockMemoryInsertEnumType.INSERT_IN_PERSISTENT_CACHE_OBJECT, cancellation);
                                    }
                                }
                            }
                            finally
                            {
                                if (semaphoreUsed)
                                    _semaphoreSlimMemoryAccess.Release();
                            }
                            return getBlockObject;
                        }
                    }
                    break;
            }

            return null;
        }


        /// <summary>
        /// Retrieve back the amount of active memory used by the cache.
        /// </summary>
        /// <returns></returns>
        public long GetActiveMemoryUsageFromCache()
        {
            long totalMemoryUsageFromCache = 0;

            if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
                totalMemoryUsageFromCache = _cacheIoSystem.GetIoCacheSystemMemoryConsumption(null, out _);

            return totalMemoryUsageFromCache;
        }

        #endregion


        #region Functions to manage mirror memory.

        /// <summary>
        /// Check if the block height possess his mirror block content.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public bool ContainBlockHeightMirror(long blockHeight)
        {
            if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
            {
                if (_dictionaryBlockObjectMemory[blockHeight].ContentMirror != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieve back the block mirror content of a block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<ClassBlockObject> GetBlockMirrorObject(long blockHeight, CancellationTokenSource cancellation)
        {
            if (ContainsKey(blockHeight))
            {
                if (_dictionaryBlockObjectMemory[blockHeight].Content != null)
                {
                    _dictionaryBlockObjectMemory[blockHeight].Content.DeepCloneBlockObject(false, out ClassBlockObject blockObject);

                    if (blockObject != null)
                        return blockObject;
                }

                if (_dictionaryBlockObjectMemory[blockHeight].ContentMirror != null)
                    return _dictionaryBlockObjectMemory[blockHeight].ContentMirror;

                return await GetBlockInformationMemoryDataFromCacheByKey(blockHeight, cancellation);
            }

            return null;
        }

        /// <summary>
        /// Get the transaction count stored on a block mirror object.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<int> GetBlockMirrorTransactionCount(long blockHeight, CancellationTokenSource cancellation)
        {
            ClassBlockObject blockMirrorObject = await GetBlockMirrorObject(blockHeight, cancellation);

            return blockMirrorObject != null ? blockMirrorObject.TotalTransaction : 0;
        }

        /// <summary>
        /// Insert or a update a block mirror content object.
        /// </summary>
        /// <param name="blockObject"></param>
        private void AddOrUpdateBlockMirrorObject(ClassBlockObject blockObject)
        {
            if (blockObject != null)
            {
                if (blockObject.IsChecked)
                {
                    if (ContainsKey(blockObject.BlockHeight))
                        _dictionaryBlockObjectMemory[blockObject.BlockHeight].ContentMirror = blockObject;
                    else
                    {
                        _dictionaryBlockObjectMemory.Add(blockObject.BlockHeight, new BlockchainMemoryObject()
                        {
                            ContentMirror = blockObject,
                            ObjectCacheType = CacheBlockMemoryEnumState.IN_PERSISTENT_CACHE,
                            CacheUpdated = true,
                            ObjectIndexed = true,
                        });
                    }
                }
            }
        }

        #endregion


        #region Manage the block transaction cache in front of IO Cache files/network.

        /// <summary>
        /// Insert a wallet address to keep reserved for the block transaction cache.
        /// </summary>
        /// <param name="walletAddress">Wallet address linked to transaction to keep on the cache</param>
        public void InsertWalletAddressReservedToBlockTransactionCache(string walletAddress)
        {
            if (!_listWalletAddressReservedForBlockTransactionCache.Contains(walletAddress))
                _listWalletAddressReservedForBlockTransactionCache.Add(walletAddress);
        }

        /// <summary>
        /// Remove a wallet address to not keep it reserved for the block transaction cache.
        /// </summary>
        /// <param name="walletAddress">Wallet address linked to transaction to keep on the cache</param>
        public void RemoveWalletAddressReservedToBlockTransactionCache(string walletAddress)
        {
            if (!_listWalletAddressReservedForBlockTransactionCache.Contains(walletAddress))
                _listWalletAddressReservedForBlockTransactionCache.Remove(walletAddress);
        }

        /// <summary>
        /// Update the block transaction cache task.
        /// </summary>
        private async Task<long> UpdateBlockTransactionCacheTask(CancellationTokenSource cancellation)
        {
            long totalTransactionRemoved = 0;
            bool semaphoreUsed = false;
            try
            {
                try
                {
                    await _semaphoreSlimCacheBlockTransactionAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (Count > 0)
                    {

                        long totalTransaction = 0;
                        long totalTransactionKeepAlive = 0;
                        long currentTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                        foreach (long blockHeight in _dictionaryBlockObjectMemory.Keys.ToArray())
                        {
                            

                            if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.Count > 0)
                            {
                                int totalRemovedFromBlock = 0;

                                foreach (string transactionHash in _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.Keys.ToArray())
                                {
                                    

                                    totalTransaction++;

                                    if (_listWalletAddressReservedForBlockTransactionCache.Contains(_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[transactionHash].BlockTransaction.TransactionObject.WalletAddressSender) ||
                                        _listWalletAddressReservedForBlockTransactionCache.Contains(_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[transactionHash].BlockTransaction.TransactionObject.WalletAddressReceiver))
                                        totalTransactionKeepAlive++;
                                    else
                                    {
                                        if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[transactionHash].LastUpdateTimestamp + _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxDelayKeepAliveBlockTransactionCached < currentTimestamp)
                                        {
                                            long blockTransactionMemorySize = _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[transactionHash].BlockTransactionMemorySize;

                                            if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.TryRemove(transactionHash, out _))
                                            {
                                                totalTransactionRemoved++;
                                                totalRemovedFromBlock++;
                                                if (_totalBlockTransactionCacheCount > 0)
                                                    _totalBlockTransactionCacheCount--;

                                                _totalBlockTransactionMemorySize -= blockTransactionMemorySize;
                                                if (_totalBlockTransactionMemorySize < 0)
                                                    _totalBlockTransactionMemorySize = 0;
                                            }
                                        }
                                        else
                                            totalTransactionKeepAlive++;
                                    }
                                }
                            }
                        }

                        double percentRemoved = ((double)totalTransactionRemoved / totalTransaction) / 100d;

                        if (percentRemoved >= _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalPercentDeleteBlockTransactionCachedPurgeMemory)
                            ClassUtility.CleanGc();

#if DEBUG
                        Debug.WriteLine("Block transaction cache - Clean up block transaction cache. Total Removed: " + totalTransactionRemoved + " | Total Keep Alive: " + totalTransactionKeepAlive + " on total transaction: " + totalTransaction);
                        Debug.WriteLine("Block transaction cache - Total active memory spend by the block transaction cache: " + ClassUtility.ConvertBytesToMegabytes(GetBlockTransactionCachedMemorySize()));
#endif

                        ClassLog.WriteLine("Block transaction cache - Clean up block transaction cache. Total Removed: " + totalTransactionRemoved + " | Total Keep Alive: " + totalTransactionKeepAlive + " on total transaction: " + totalTransaction, ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassLog.WriteLine("Block transaction cache - Total active memory spend by the block transaction cache: " + ClassUtility.ConvertBytesToMegabytes(GetBlockTransactionCachedMemorySize()), ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                    }
                }
                catch
                {
                    // Ignored.
                }

            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreSlimCacheBlockTransactionAccess.Release();
            }

            return totalTransactionRemoved;
        }

        /// <summary>
        /// Update the block transaction cache.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <param name="cancellation"></param>
        public async Task UpdateBlockTransactionCache(ClassBlockTransaction blockTransaction, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;
            try
            {
                try
                {
                    await _semaphoreSlimCacheBlockTransactionAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
                    {
                        if (blockTransaction?.TransactionObject != null)
                        {
                            if (blockTransaction.TransactionBlockHeightInsert > BlockchainSetting.GenesisBlockHeight)
                            {
                                if (blockTransaction.IsConfirmed)
                                {
                                    long blockHeight = blockTransaction.TransactionBlockHeightInsert;
                                    long blockTransactionMemorySize = blockTransaction.TransactionSize;

                                    if (blockTransactionMemorySize <= 0)
                                        blockTransactionMemorySize = ClassTransactionUtility.GetBlockTransactionMemorySize(blockTransaction);

                                    // Insert or update block transactions of wallet address reserved.
                                    if (_listWalletAddressReservedForBlockTransactionCache.Contains(blockTransaction.TransactionObject.WalletAddressSender) ||
                                        _listWalletAddressReservedForBlockTransactionCache.Contains(blockTransaction.TransactionObject.WalletAddressReceiver))
                                    {
                                        if (!_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                                        {

                                            if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.TryAdd(blockTransaction.TransactionObject.TransactionHash, new ClassCacheIoBlockTransactionObject()
                                            {
                                                BlockTransaction = blockTransaction.Clone(),
                                                LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
                                                BlockTransactionMemorySize = blockTransactionMemorySize
                                            }))
                                            {

                                                _totalBlockTransactionCacheCount++;

                                                // Increment size.
                                                _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                            }
                                        }
                                        else
                                        {
                                            _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransaction = blockTransaction.Clone();
                                            _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                                            // Remove previous size.
                                            _totalBlockTransactionMemorySize -= _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize;
                                            if (_totalBlockTransactionMemorySize < 0)
                                                _totalBlockTransactionMemorySize = 0;

                                            // Increment new size.
                                            _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize = blockTransactionMemorySize;
                                            _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                        }
                                    }
                                    else
                                    {
                                        if (_totalBlockTransactionMemorySize + blockTransactionMemorySize < _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalCacheMaxBlockTransactionKeepAliveMemorySize)
                                        {
                                            if (!_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                                            {

                                                if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.TryAdd(blockTransaction.TransactionObject.TransactionHash, new ClassCacheIoBlockTransactionObject()
                                                {
                                                    BlockTransaction = blockTransaction.Clone(),
                                                    LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
                                                    BlockTransactionMemorySize = blockTransactionMemorySize
                                                }))
                                                {

                                                    _totalBlockTransactionCacheCount++;

                                                    // Increment size.
                                                    _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                                }
                                            }
                                            else
                                            {
                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransaction = blockTransaction.Clone();
                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                                                // Remove previous size.
                                                _totalBlockTransactionMemorySize -= _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize;
                                                if (_totalBlockTransactionMemorySize < 0)
                                                    _totalBlockTransactionMemorySize = 0;

                                                // Increment new size.
                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize = blockTransactionMemorySize;
                                                _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                            }
                                        }
                                        else
                                        {
                                            // If the tx already stored, but was updated, we simply replace the previous one by the new one.
                                            if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                                            {
                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransaction = blockTransaction.Clone();
                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                                                // Remove previous size.
                                                _totalBlockTransactionMemorySize -= _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize;
                                                if (_totalBlockTransactionMemorySize < 0)
                                                    _totalBlockTransactionMemorySize = 0;

                                                // Increment new size.
                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize = blockTransactionMemorySize;
                                                _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                            }
                                            // If not, and if the amount of memory allocated for the cache is reach, try to remove some old tx cached to retrieve back some memory.
                                            else
                                            {
                                                long totalMemoryRetrieved = 0;
                                                bool memoryRetrived = false;
                                                long lastBlockHeight = GetLastBlockHeight;

                                                for (long i = 0; i < lastBlockHeight; i++)
                                                {
                                                    long blockHeightIndex = i + 1;
                                                    if (blockHeightIndex <= lastBlockHeight)
                                                    {
                                                        if (_dictionaryBlockObjectMemory[blockHeightIndex].BlockTransactionCache.Count > 0)
                                                        {
                                                            using (DisposableList<string> listTxHashToRemove = new DisposableList<string>())
                                                            {
                                                                using (DisposableList<string> listBlockTransactionHash = new DisposableList<string>(false, 0, _dictionaryBlockObjectMemory[blockHeightIndex].BlockTransactionCache.Keys.ToArray()))
                                                                {
                                                                    foreach (var txHash in listBlockTransactionHash.GetList)
                                                                    {
                                                                        

                                                                        if (_dictionaryBlockObjectMemory[blockHeightIndex].BlockTransactionCache.TryGetValue(txHash, out var cacheIoBlockTransactionObject))
                                                                        {
                                                                            if (cacheIoBlockTransactionObject != null)
                                                                            {
                                                                                // Remove previous size.
                                                                                long previousSize = cacheIoBlockTransactionObject.BlockTransactionMemorySize;

                                                                                listTxHashToRemove.Add(txHash);

                                                                                _totalBlockTransactionMemorySize -= previousSize;
                                                                                _totalBlockTransactionCacheCount--;


                                                                                if (totalMemoryRetrieved >= blockTransactionMemorySize)
                                                                                {
                                                                                    memoryRetrived = true;
                                                                                    break;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }


                                                                if (listTxHashToRemove.Count > 0)
                                                                {
                                                                    foreach (string txHash in listTxHashToRemove.GetList)
                                                                    {
                                                                        
                                                                        _dictionaryBlockObjectMemory[blockHeightIndex].BlockTransactionCache.TryRemove(txHash, out _);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }

                                                    if (memoryRetrived)
                                                        break;
                                                }

                                                if (memoryRetrived)
                                                {
                                                    if (!_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                                                    {
                                                        if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.TryAdd(blockTransaction.TransactionObject.TransactionHash, new ClassCacheIoBlockTransactionObject()
                                                        {
                                                            BlockTransaction = blockTransaction.Clone(),
                                                            LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
                                                            BlockTransactionMemorySize = blockTransactionMemorySize
                                                        }))
                                                        {

                                                            _totalBlockTransactionCacheCount++;

                                                            // Increment size.
                                                            _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransaction = blockTransaction.Clone();
                                                        _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                                                        // Remove previous size.
                                                        _totalBlockTransactionMemorySize -= _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize;

                                                        if (_totalBlockTransactionMemorySize < 0)
                                                            _totalBlockTransactionMemorySize = 0;

                                                        // Increment new size.
                                                        _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize = blockTransactionMemorySize;
                                                        _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
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
                if (semaphoreUsed)
                    _semaphoreSlimCacheBlockTransactionAccess.Release();
            }
        }

        /// <summary>
        /// Update the block transaction cache.
        /// </summary>
        /// <param name="blockTransactionList"></param>
        /// <param name="cancellation"></param>
        public async Task UpdateListBlockTransactionCache(List<ClassBlockTransaction> blockTransactionList, CancellationTokenSource cancellation, bool onlyIfExist)
        {
            bool semaphoreUsed = false;
            try
            {
                try
                {
                    await _semaphoreSlimCacheBlockTransactionAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_blockchainDatabaseSetting.BlockchainCacheSetting.EnableCacheDatabase)
                    {
                        foreach (var blockTransaction in blockTransactionList)
                        {
                            cancellation?.Token.ThrowIfCancellationRequested();

                            if (blockTransaction?.TransactionObject != null)
                            {
                                if (blockTransaction.TransactionBlockHeightInsert > BlockchainSetting.GenesisBlockHeight)
                                {
                                    if (blockTransaction.IsConfirmed)
                                    {
                                        long blockHeight = blockTransaction.TransactionBlockHeightInsert;
                                        long blockTransactionMemorySize = blockTransaction.TransactionSize;

                                        if (blockTransactionMemorySize <= 0)
                                            blockTransactionMemorySize = ClassTransactionUtility.GetBlockTransactionMemorySize(blockTransaction);

                                        // Insert or update block transactions of wallet address reserved.
                                        if (_listWalletAddressReservedForBlockTransactionCache.Contains(blockTransaction.TransactionObject.WalletAddressSender) ||
                                            _listWalletAddressReservedForBlockTransactionCache.Contains(blockTransaction.TransactionObject.WalletAddressReceiver))
                                        {
                                            if (!_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                                            {
                                                if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.TryAdd(blockTransaction.TransactionObject.TransactionHash, new ClassCacheIoBlockTransactionObject()
                                                {
                                                    BlockTransaction = blockTransaction.Clone(),
                                                    LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
                                                    BlockTransactionMemorySize = blockTransactionMemorySize
                                                }))
                                                {

                                                    _totalBlockTransactionCacheCount++;

                                                    // Increment size.
                                                    _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                                }
                                            }
                                            else
                                            {
                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransaction = blockTransaction.Clone();
                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                                                // Remove previous size.
                                                _totalBlockTransactionMemorySize -= _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize;

                                                if (_totalBlockTransactionMemorySize < 0)
                                                    _totalBlockTransactionMemorySize = 0;

                                                // Increment new size.
                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize = blockTransactionMemorySize;
                                                _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                            }
                                        }
                                        else
                                        {
                                            if (_totalBlockTransactionMemorySize + blockTransactionMemorySize < _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalCacheMaxBlockTransactionKeepAliveMemorySize)
                                            {
                                                if (!_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                                                {
                                                    if (!onlyIfExist)
                                                    {
                                                        if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.TryAdd(blockTransaction.TransactionObject.TransactionHash, new ClassCacheIoBlockTransactionObject()
                                                        {
                                                            BlockTransaction = blockTransaction.Clone(),
                                                            LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
                                                            BlockTransactionMemorySize = blockTransactionMemorySize
                                                        }))
                                                        {

                                                            _totalBlockTransactionCacheCount++;

                                                            // Increment size.
                                                            _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransaction = blockTransaction.Clone();
                                                    _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                                                    // Remove previous size.
                                                    _totalBlockTransactionMemorySize -= _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize;

                                                    if (_totalBlockTransactionMemorySize < 0)
                                                        _totalBlockTransactionMemorySize = 0;

                                                    // Increment new size.
                                                    _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize = blockTransactionMemorySize;
                                                    _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                                }
                                            }
                                            else
                                            {
                                                // If the tx already stored, but was updated, we simply replace the previous one by the new one.
                                                if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                                                {
                                                    _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransaction = blockTransaction.Clone();
                                                    _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                                                    // Remove previous size.
                                                    _totalBlockTransactionMemorySize -= _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize;

                                                    if (_totalBlockTransactionMemorySize < 0)
                                                        _totalBlockTransactionMemorySize = 0;

                                                    // Increment new size.
                                                    _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize = blockTransactionMemorySize;
                                                    _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                                }
                                                // If not, and if the amount of memory allocated for the cache is reach, try to remove some old tx cached to retrieve back some memory.
                                                else
                                                {
                                                    if (!onlyIfExist)
                                                    {
                                                        long totalMemoryRetrieved = 0;
                                                        bool memoryRetrived = false;
                                                        long lastBlockHeight = GetLastBlockHeight;

                                                        for (long i = 0; i < lastBlockHeight; i++)
                                                        {
                                                            long blockHeightIndex = i + 1;
                                                            if (blockHeightIndex <= lastBlockHeight)
                                                            {
                                                                if (_dictionaryBlockObjectMemory[blockHeightIndex].BlockTransactionCache.Count > 0)
                                                                {
                                                                    using (DisposableList<string> listTxHashToRemove = new DisposableList<string>())
                                                                    {
                                                                        using (DisposableList<string> listBlockTransactionHash = new DisposableList<string>(false, 0, _dictionaryBlockObjectMemory[blockHeightIndex].BlockTransactionCache.Keys.ToArray()))
                                                                        {
                                                                            foreach (var txHash in listBlockTransactionHash.GetList)
                                                                            {
                                                                                

                                                                                if (_dictionaryBlockObjectMemory[blockHeightIndex].BlockTransactionCache.TryGetValue(txHash, out var cacheIoBlockTransactionObject))
                                                                                {
                                                                                    if (cacheIoBlockTransactionObject != null)
                                                                                    {
                                                                                        // Remove previous size.
                                                                                        long previousSize = cacheIoBlockTransactionObject.BlockTransactionMemorySize;

                                                                                        listTxHashToRemove.Add(txHash);

                                                                                        _totalBlockTransactionMemorySize -= previousSize;
                                                                                        _totalBlockTransactionCacheCount--;

                                                                                        if (totalMemoryRetrieved >= blockTransactionMemorySize)
                                                                                        {
                                                                                            memoryRetrived = true;
                                                                                            break;
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }


                                                                        if (listTxHashToRemove.Count > 0)
                                                                        {
                                                                            foreach (string txHash in listTxHashToRemove.GetList)
                                                                            {
                                                                                
                                                                                _dictionaryBlockObjectMemory[blockHeightIndex].BlockTransactionCache.TryRemove(txHash, out _);
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }

                                                            if (memoryRetrived)
                                                                break;
                                                        }

                                                        if (memoryRetrived)
                                                        {
                                                            if (!_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                                                            {
                                                                if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.TryAdd(blockTransaction.TransactionObject.TransactionHash, new ClassCacheIoBlockTransactionObject()
                                                                {
                                                                    BlockTransaction = blockTransaction.Clone(),
                                                                    LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
                                                                    BlockTransactionMemorySize = blockTransactionMemorySize
                                                                }))
                                                                {

                                                                    _totalBlockTransactionCacheCount++;

                                                                    // Increment size.
                                                                    _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransaction = blockTransaction.Clone();
                                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].LastUpdateTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                                                                // Remove previous size.
                                                                _totalBlockTransactionMemorySize -= _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize;

                                                                if (_totalBlockTransactionMemorySize < 0)
                                                                    _totalBlockTransactionMemorySize = 0;

                                                                // Increment new size.
                                                                _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[blockTransaction.TransactionObject.TransactionHash].BlockTransactionMemorySize = blockTransactionMemorySize;
                                                                _totalBlockTransactionMemorySize += blockTransactionMemorySize;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
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
                if (semaphoreUsed)
                    _semaphoreSlimCacheBlockTransactionAccess.Release();
            }
        }

        /// <summary>
        /// Check if the cache contain the transaction hash in the cache.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> ContainBlockTransactionHashInCache(string transactionHash, long blockHeight, CancellationTokenSource cancellation)
        {
            bool result = false;
            bool semaphoreUsed = false;
            try
            {
                try
                {
                    await _semaphoreSlimCacheBlockTransactionAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                        result = _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(transactionHash);
                }
                catch
                {
                    // Ignored.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreSlimCacheBlockTransactionAccess.Release();
            }

            return result;
        }

        /// <summary>
        /// Retrieve back a block transaction cached.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<ClassBlockTransaction> GetBlockTransactionCached(string transactionHash, long blockHeight, CancellationTokenSource cancellation)
        {
            ClassBlockTransaction blockTransaction = null;

            bool semaphoreUsed = false;
            try
            {
                try
                {
                    await _semaphoreSlimCacheBlockTransactionAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                    {

                        if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(transactionHash))
                        {
                            long currentTimestamp = ClassUtility.GetCurrentTimestampInSecond();
                            if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[transactionHash].LastUpdateTimestamp + _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxDelayKeepAliveBlockTransactionCached >= currentTimestamp)
                                blockTransaction = _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[transactionHash].BlockTransaction.Clone();
                            else
                            {
                                long totalMemoryTransactionSize = _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[transactionHash].BlockTransactionMemorySize;
                                if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.TryRemove(transactionHash, out _))
                                {
                                    _totalBlockTransactionMemorySize -= totalMemoryTransactionSize;
                                    _totalBlockTransactionCacheCount--;

                                    if (_totalBlockTransactionCacheCount < 0)
                                        _totalBlockTransactionCacheCount = 0;

                                    if (_totalBlockTransactionMemorySize < 0)
                                        _totalBlockTransactionMemorySize = 0;
                                }
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
                if (semaphoreUsed)
                    _semaphoreSlimCacheBlockTransactionAccess.Release();
            }

            return blockTransaction;
        }

        /// <summary>
        /// Retrieve back a list of block transaction cached.
        /// </summary>
        /// <param name="listTransactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<DisposableList<ClassBlockTransaction>> GetListBlockTransactionCache(List<string> listTransactionHash, long blockHeight, CancellationTokenSource cancellation)
        {
            DisposableList<ClassBlockTransaction> listBlockTransactions = new DisposableList<ClassBlockTransaction>();

            bool semaphoreUsed = false;
            try
            {
                try
                {
                    await _semaphoreSlimCacheBlockTransactionAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                    {

                        long currentTimestamp = ClassUtility.GetCurrentTimestampInSecond();
                        foreach (string transactionHash in listTransactionHash)
                        {
                            

                            if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.ContainsKey(transactionHash))
                            {
                                if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[transactionHash].LastUpdateTimestamp + _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxDelayKeepAliveBlockTransactionCached >= currentTimestamp)
                                    listBlockTransactions.Add(_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[transactionHash].BlockTransaction.Clone());
                                else
                                {
                                    long totalMemoryTransactionSize = _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache[transactionHash].BlockTransactionMemorySize;
                                    if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.TryRemove(transactionHash, out _))
                                    {
                                        _totalBlockTransactionMemorySize -= totalMemoryTransactionSize;
                                        _totalBlockTransactionCacheCount--;

                                        if (_totalBlockTransactionCacheCount < 0)
                                        {
                                            _totalBlockTransactionCacheCount = 0;
                                            _totalBlockTransactionMemorySize = 0;
                                        }

                                        if (_totalBlockTransactionMemorySize < 0)
                                            _totalBlockTransactionMemorySize = 0;
                                    }
                                }
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
                if (semaphoreUsed)
                    _semaphoreSlimCacheBlockTransactionAccess.Release();
            }

            return listBlockTransactions;
        }

        /// <summary>
        /// Get each block transaction cached of a block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<IList<ClassCacheIoBlockTransactionObject>> GetEachBlockTransactionFromBlockHeightCached(long blockHeight, CancellationTokenSource cancellation)
        {
            List<ClassCacheIoBlockTransactionObject> result = null;
            bool semaphoreUsed = false;
            try
            {
                try
                {
                    await _semaphoreSlimCacheBlockTransactionAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;
                }
                catch
                {
                    // The task has been cancelled.
                }

                if (semaphoreUsed)
                {
                    if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeight <= GetLastBlockHeight)
                    {
                        long currentTimestamp = ClassUtility.GetCurrentTimestampInSecond();

                        if (_dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.Count > 0)
                            result = _dictionaryBlockObjectMemory[blockHeight].BlockTransactionCache.Values.TakeWhile(x => (x.LastUpdateTimestamp + _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxDelayKeepAliveBlockTransactionCached >= currentTimestamp)).ToList();
                    }
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreSlimCacheBlockTransactionAccess.Release();
            }

            return result == null ? new List<ClassCacheIoBlockTransactionObject>() : result;
        }

        /// <summary>
        /// Return the amount of block transaction cached.
        /// </summary>
        /// <returns></returns>
        public long GetBlockTransactionCachedCount()
        {
            return _totalBlockTransactionCacheCount;
        }

        /// <summary>
        /// Return the amount of memory used by block transactions cached.
        /// </summary>
        /// <returns></returns>
        public long GetBlockTransactionCachedMemorySize()
        {
            return _totalBlockTransactionMemorySize + (GetBlockTransactionCachedCount() * (BlockchainSetting.TransactionHashSize * sizeof(char)));
        }

        #endregion


        #region Other functions.

        /// <summary>
        /// Build a list of block height by range.
        /// </summary>
        /// <param name="lastBlockHeightUnlocked"></param>
        /// <returns></returns>
        private List<List<long>> BuildListBlockHeightByRange(long lastBlockHeightUnlocked, HashSet<long> listBlockHeightToExcept, CancellationTokenSource cancellation)
        {
            List<List<long>> listOfBlockRange = new List<List<long>>
            {
                // Default list.
                new List<long>()
            };

            for (long i = 0; i < lastBlockHeightUnlocked; i++)
            {
                

                long blockHeight = i + 1;

                if (!listBlockHeightToExcept.Contains(blockHeight))
                {
                    if (blockHeight <= lastBlockHeightUnlocked)
                    {
                        int countList = listOfBlockRange.Count - 1;
                        if (listOfBlockRange[countList].Count < _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxRangeReadBlockDataFromCache)
                            listOfBlockRange[countList].Add(blockHeight);
                        else
                        {
                            listOfBlockRange.Add(new List<long>());
                            countList = listOfBlockRange.Count - 1;
                            listOfBlockRange[countList].Add(blockHeight);
                        }
                    }
                    else
                        break;
                }
            }

            return listOfBlockRange;
        }

        #endregion
    }
}
