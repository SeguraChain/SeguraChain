using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Enum;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Database.DatabaseSetting
{
    public class ClassBlockchainDatabaseDefaultSetting
    {
        /// <summary>
        /// Default paths and settings of the blockchain database.
        /// </summary>
        public static readonly string DefaultBlockchainDirectoryPath = ClassUtility.ConvertPath(AppContext.BaseDirectory + "\\Blockchain\\").Replace("\\\\", "\\");
        public static readonly string DefaultBlockchainDirectoryBlockPath = ClassUtility.ConvertPath(AppContext.BaseDirectory + "\\Blockchain\\Block\\").Replace("\\\\", "\\");
        public const string BlockDatabaseDirectory = "Block\\";
        public const string BlockDatabaseFileName = "block-";
        public const string BlockDatabaseFileExtension = ".dat";
        public const string CheckpointDatabaseFileName = "checkpoint.dat";
        public static readonly string BlockchainCacheDirectoryPath = ClassUtility.ConvertPath(DefaultBlockchainDirectoryPath + "\\Cache\\").Replace("\\\\", "\\");
        public static readonly string WalletIndexCacheDirectoryPath = ClassUtility.ConvertPath(DefaultBlockchainDirectoryPath + "\\WalletIndexCache\\").Replace("\\\\", "\\");
        public const int MaxBlockTaskSave = 10;

        /// <summary>
        /// Default paths of the MemPool database.
        /// </summary>
        public static readonly string DefaultMemPoolDirectoryPath = ClassUtility.ConvertPath(AppContext.BaseDirectory + "\\MemPool\\").Replace("\\\\", "\\");
        public const string DefaultMemPoolTransactionDatabaseFileName = "mempool-tx.dat";

        /// <summary>
        /// LZ4 Compression settings.
        /// </summary>
        public const int Lz4CompressionBlockSize = 8192 * 1024;

        /// <summary>
        /// Default data settings.
        /// </summary>
        public const bool DefaultEnableEncryptionDatabase = false;
        public const bool DefaultEnableCompressingDatabase = BlockchainSetting.BlockchainDefaultEnableCompressingDatabase;
        public const bool DefaultEnableCachingDatabase = true;
        public const bool DefaultDataFormatIsJson = BlockchainSetting.BlockchainDefaultDataFormatIsJson;
        public const ClassBlockchainDatabaseCacheTypeEnum DefaultCacheType = ClassBlockchainDatabaseCacheTypeEnum.CACHE_DISK;
        public const CacheEnumName DefaultCacheName = CacheEnumName.IO_CACHE;

        /// <summary>
        /// IO Disk default cache settings.
        /// </summary>
        public const long DefaultIoDiskCacheMaxBlockPerFile = 10; // Maximum of blocks per io cache file.
        public const long DefaultIoCacheDiskTransactionSizePerLine = 1_000_000_000;
        public const int DefaultIoCacheDiskMaxTransactionPerLineOnBlockStringToWrite = 1000; // A maximum of 1000 transactions per line per block. Example, 100 000 tx's are stored on a block, the result return 100 lines who contains 1000 transactions on each.
        public const int DefaultIoCacheDiskMaxKeepAliveDataInMemoryTimeLimit = 600 * 1000; // Maximum of time to keep data in the active memory.
        public const double DefaultIoCacheDiskFullPurgeEnablePercentWrite = 50d; // If the amount of blocks to write on a io cache index file is above this percent, a full purge is done on the file. If not, new lines are written on the cache file.
        public const int DefaultIoCacheDiskWriteStreamBufferSize = 65535;
        public const int DefaultIoCacheDiskReadStreamBufferSize = 8192;
        public const int DefaultIoCacheDiskMinReadByBlockSize = 1024;
        public const int DefaultIoCacheDiskMinWriteByBlockSize = 1024;
        public const int DefaultIoCacheDiskMinPercentReadFromBlockDataSize = 5;
        public const int DefaultIoCacheDiskMinPercentWriteFromBlockDataSize = 5;
        public const int DefaultIoCacheDiskParallelTaskWaitDelay = 1000;  // The delay of waiting in milliseconds per while pending parallel tasks are running on the IO Cache system.
        public const bool DefaultIoCacheDiskEnableCompressBlockData = false;
        public const bool DefaultIoCacheDiskEnableMultiTask = true;

        /// <summary>
        /// Global default memory transaction cache settings.
        /// </summary>
        public const int DefaultGlobalCacheMaxBlockTransactionKeepAliveMemorySize = 128 * 1024 * 1024; // Max amount of memory allowed to the block transactions cache to keep alive from of io cache files. (Around 131MB of ram used by 100 000 transaction(s)).
        public const int DefaultGlobalMaxDelayKeepAliveBlockTransactionCached = 60; // Keep alive a transaction cached pending 60 seconds.
        public const int DefaultGlobalPercentDeleteBlockTransactionCachedPurgeMemory = 30; // Call GC Collector if 30% of the block transaction cache are deleted.


        /// <summary>
        /// Global default memory wallet index cache settings.
        /// </summary>
        public const long DefaultGlobalCacheMaxWalletIndexKeepMemorySize = 128 * 1024 * 1024; // Max amount of memory allowed to the wallet index cache to keep alive from the cache files.
        public const long DefaultGlobalMaxDelayKeepAliveWalletIndexCached = 60 * 1000; // Keep alive a wallet index pending 60000 milliseconds.
        public const long DefaultGlobalCacheMaxWalletIndexPerFile = 10000;
        public const long DefaultGlobalCacheMaxWalletIndexCheckpointsPerLine = 10000;

        /// <summary>
        /// Global default memory cache management settings.
        /// </summary>
        public const long DefaultGlobalMaxActiveMemoryAllocationFromCache = 512 * 1024 * 1024; // Allow a maximum of 512MB of ram allocated.
        public const long DefaultGlobalMaxBlockCountToKeepInMemory = 2; // The maxmimum of blocks to keep alive in the active memory, it's always latests blocks who are keep alive.
        public const long DefaultGlobalMaxRangeReadBlockDataFromCache = 2; // The maximum of blocks to retrieve back from the cache by range.
        public const int DefaultGlobalTaskManageMemoryInterval = 60 * 1000;  // Task interval who manage the active memory.
        public const int DefaultGlobalObjectCacheUpdateLimitTime = 60; // Max time of update interval of an element. Once the average of time is reach, the element is inserted/updated from the active memory to the Cache.
        public const int DefaultGlobalBlockActiveMemoryKeepAlive = 120; // Delete an element permently of the active memory after 2 minutes of inactivity, insert/update the element removed from the active memory to the Cache.
        public const int DefaultGlobalObjectLimitSimpleGetObjectFromCache = 5; // Once this limit is reach on a block height object target, this one is retrieved back to the active memory and call to do the same on the IO system to retrieve it faster later, until a purge is done.
    }


    public class ClassBlockchainDatabaseSetting
    {
        public Blockchain BlockchainSetting;
        public MemPool MemPoolSetting;
        public DatabaseSetting DataSetting;


        /// <summary>
        /// Cache settings.
        /// </summary>
        public ClassBlockchainCacheSetting BlockchainCacheSetting;

        /// <summary>
        /// Constructor with default values.
        /// </summary>
        public ClassBlockchainDatabaseSetting()
        {
            BlockchainSetting = new Blockchain
            {
                BlockchainDirectoryPath = ClassBlockchainDatabaseDefaultSetting.DefaultBlockchainDirectoryPath,
                BlockchainDirectoryBlockPath = ClassBlockchainDatabaseDefaultSetting.DefaultBlockchainDirectoryBlockPath,
                BlockchainBlockDatabaseFilename = ClassBlockchainDatabaseDefaultSetting.BlockDatabaseFileName,
                BlockchainCheckpointDatabaseFilename = ClassBlockchainDatabaseDefaultSetting.CheckpointDatabaseFileName
            };

            MemPoolSetting = new MemPool
            {
                MemPoolDirectoryPath = ClassBlockchainDatabaseDefaultSetting.DefaultMemPoolDirectoryPath,
                MemPoolTransactionDatabaseFilename = ClassBlockchainDatabaseDefaultSetting.DefaultMemPoolTransactionDatabaseFileName
            };

            DataSetting = new DatabaseSetting
            {
                EnableEncryptionDatabase = ClassBlockchainDatabaseDefaultSetting.DefaultEnableEncryptionDatabase,
                EnableCompressDatabase = ClassBlockchainDatabaseDefaultSetting.DefaultEnableCompressingDatabase,
                DataFormatIsJson = ClassBlockchainDatabaseDefaultSetting.DefaultDataFormatIsJson,
            };


            ulong totalMemoryHost = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalMaxActiveMemoryAllocationFromCache*2;

            if (totalMemoryHost < ClassBlockchainDatabaseDefaultSetting.DefaultGlobalMaxActiveMemoryAllocationFromCache +
                ClassBlockchainDatabaseDefaultSetting.DefaultGlobalCacheMaxBlockTransactionKeepAliveMemorySize +
                ClassBlockchainDatabaseDefaultSetting.DefaultGlobalCacheMaxWalletIndexKeepMemorySize)
            {
                ClassLog.WriteLine("[WARNING] - The host memory is lower than the default setting.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
            }


            BlockchainCacheSetting = new ClassBlockchainCacheSetting()
            {
                CacheDirectoryPath = ClassBlockchainDatabaseDefaultSetting.BlockchainCacheDirectoryPath,
                WalletIndexCacheDirectoryPath = ClassBlockchainDatabaseDefaultSetting.WalletIndexCacheDirectoryPath,
                CacheType = ClassBlockchainDatabaseDefaultSetting.DefaultCacheType,
                CacheName = ClassBlockchainDatabaseDefaultSetting.DefaultCacheName,
                EnableCacheDatabase = ClassBlockchainDatabaseDefaultSetting.DefaultEnableCachingDatabase,

                // IO cache disk settings.
                IoCacheDiskMaxBlockPerFile = ClassBlockchainDatabaseDefaultSetting.DefaultIoDiskCacheMaxBlockPerFile,
                IoCacheDiskMaxTransactionSizePerLine = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskTransactionSizePerLine,
                IoCacheDiskMaxTransactionPerLineOnBlockStringToWrite = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskMaxTransactionPerLineOnBlockStringToWrite,
                IoCacheDiskMaxKeepAliveDataInMemoryTimeLimit = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskMaxKeepAliveDataInMemoryTimeLimit,
                IoCacheDiskFullPurgeEnablePercentWrite = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskFullPurgeEnablePercentWrite,
                IoCacheDiskWriteStreamBufferSize = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskWriteStreamBufferSize,
                IoCacheDiskReadStreamBufferSize = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskReadStreamBufferSize,
                IoCacheDiskMinReadByBlockSize = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskMinReadByBlockSize,
                IoCacheDiskMinWriteByBlockSize = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskMinWriteByBlockSize,
                IoCacheDiskMinPercentReadFromBlockDataSize = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskMinPercentReadFromBlockDataSize,
                IoCacheDiskMinPercentWriteFromBlockDataSize = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskMinPercentWriteFromBlockDataSize,
                IoCacheDiskParallelTaskWaitDelay = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskParallelTaskWaitDelay,
                IoCacheDiskEnableCompressBlockData = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskEnableCompressBlockData,
                IoCacheDiskEnableMultiTask = ClassBlockchainDatabaseDefaultSetting.DefaultIoCacheDiskEnableMultiTask,

                // Global wallet index cache memory setting.
                GlobalCacheMaxWalletIndexKeepMemorySize = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalCacheMaxWalletIndexKeepMemorySize,
                GlobalMaxDelayKeepAliveWalletIndexCached = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalMaxDelayKeepAliveWalletIndexCached,
                GlobalCacheMaxWalletIndexPerFile = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalCacheMaxWalletIndexPerFile,
                GlobalCacheMaxWalletIndexCheckpointsPerLine = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalCacheMaxWalletIndexCheckpointsPerLine,

                // Global block transaction cache memory setting.
                GlobalCacheMaxBlockTransactionKeepAliveMemorySize = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalCacheMaxBlockTransactionKeepAliveMemorySize,
                GlobalMaxDelayKeepAliveBlockTransactionCached = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalMaxDelayKeepAliveBlockTransactionCached,
                GlobalPercentDeleteBlockTransactionCachedPurgeMemory = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalPercentDeleteBlockTransactionCachedPurgeMemory,

                // Global memory cache setting.
                GlobalMaxActiveMemoryAllocationFromCache = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalMaxActiveMemoryAllocationFromCache,
                GlobalMaxBlockCountToKeepInMemory = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalMaxBlockCountToKeepInMemory,
                GlobalMaxRangeReadBlockDataFromCache = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalMaxRangeReadBlockDataFromCache,
                GlobalTaskManageMemoryInterval = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalTaskManageMemoryInterval,
                GlobalObjectCacheUpdateLimitTime = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalObjectCacheUpdateLimitTime,
                GlobalBlockActiveMemoryKeepAlive = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalBlockActiveMemoryKeepAlive,
                GlobalObjectLimitSimpleGetObjectFromCache = ClassBlockchainDatabaseDefaultSetting.DefaultGlobalObjectLimitSimpleGetObjectFromCache
            };

        }

        /// <summary>
        /// Return the database file path.
        /// </summary>
        [JsonIgnore]
        public string GetBlockDatabaseFilePath => ClassUtility.ConvertPath(BlockchainSetting.BlockchainDirectoryPath + "\\"+ BlockchainSetting.BlockchainBlockDatabaseFilename).Replace("\\\\", "\\");

        /// <summary>
        /// Return the checkpoint database file path.
        /// </summary>
        [JsonIgnore]
        public string GetCheckpointDatabaseFilePath => ClassUtility.ConvertPath(BlockchainSetting.BlockchainDirectoryPath + "\\" + BlockchainSetting.BlockchainCheckpointDatabaseFilename).Replace("\\\\", "\\");


        /// <summary>
        /// Return the cache directory path.
        /// </summary>
        [JsonIgnore]
        public string GetBlockchainCacheDirectoryPath
        {
            get
            {
                switch (BlockchainCacheSetting.CacheType)
                {
                    case ClassBlockchainDatabaseCacheTypeEnum.CACHE_DISK:
                        return ClassUtility.ConvertPath(BlockchainCacheSetting.CacheDirectoryPath);
                    case ClassBlockchainDatabaseCacheTypeEnum.CACHE_NETWORK:
                        switch (BlockchainCacheSetting.CacheName)
                        {
                            case CacheEnumName.IO_CACHE:
                                return ClassUtility.ConvertPath(BlockchainCacheSetting.CacheDirectoryPath);
                        }
                        break;

                }
                return null;
            }
        }

        [JsonIgnore]
        public string GetBlockchainWalletIndexCacheDirectoryPath => ClassUtility.ConvertPath(BlockchainCacheSetting.WalletIndexCacheDirectoryPath);

        /// <summary>
        /// Return the MemPool transaction database.
        /// </summary>
        [JsonIgnore]
        public string GetMemPoolTransactionDatabaseFilePath => ClassUtility.ConvertPath(MemPoolSetting.MemPoolDirectoryPath + "\\" + MemPoolSetting.MemPoolTransactionDatabaseFilename).Replace("\\\\", "\\");

        /// <summary>
        /// Cache database settings.
        /// </summary>
        public class ClassBlockchainCacheSetting
        {
            public bool EnableCacheDatabase;
            public string CacheDirectoryPath;
            public string WalletIndexCacheDirectoryPath;

            public string Hostname;
            public int Port;
            public string Password;

            [JsonConverter(typeof(StringEnumConverter))]
            public ClassBlockchainDatabaseCacheTypeEnum CacheType;

            [JsonConverter(typeof(StringEnumConverter))]
            public CacheEnumName CacheName;

            /// <summary>
            /// IO Cache settings.
            /// </summary>
            public long IoCacheDiskMaxBlockPerFile;
            public long IoCacheDiskMaxTransactionSizePerLine;
            public int IoCacheDiskMaxTransactionPerLineOnBlockStringToWrite;
            public int IoCacheDiskMaxKeepAliveDataInMemoryTimeLimit;
            public double IoCacheDiskFullPurgeEnablePercentWrite;
            public int IoCacheDiskWriteStreamBufferSize;
            public int IoCacheDiskReadStreamBufferSize;
            public int IoCacheDiskMinReadByBlockSize;
            public int IoCacheDiskMinWriteByBlockSize;
            public int IoCacheDiskMinPercentReadFromBlockDataSize;
            public int IoCacheDiskMinPercentWriteFromBlockDataSize;
            public int IoCacheDiskParallelTaskWaitDelay;
            public bool IoCacheDiskEnableCompressBlockData;
            public bool IoCacheDiskEnableMultiTask;

            /// <summary>
            /// Global cache settings.
            /// </summary>
            public long GlobalCacheMaxWalletIndexKeepMemorySize;
            public long GlobalMaxDelayKeepAliveWalletIndexCached;
            public long GlobalCacheMaxWalletIndexPerFile;
            public long GlobalCacheMaxWalletIndexCheckpointsPerLine;
            public long GlobalCacheMaxBlockTransactionKeepAliveMemorySize;
            public int GlobalMaxDelayKeepAliveBlockTransactionCached;
            public int GlobalPercentDeleteBlockTransactionCachedPurgeMemory;
            public long GlobalMaxActiveMemoryAllocationFromCache;
            public long GlobalMaxBlockCountToKeepInMemory;
            public long GlobalMaxRangeReadBlockDataFromCache;
            public int GlobalTaskManageMemoryInterval;
            public int GlobalObjectCacheUpdateLimitTime;
            public int GlobalBlockActiveMemoryKeepAlive;
            public int GlobalObjectLimitSimpleGetObjectFromCache;
        }

        /// <summary>
        /// Blockchain databases settings.
        /// </summary>
        public class Blockchain
        {
            public string BlockchainDirectoryPath;
            public string BlockchainDirectoryBlockPath;
            public string BlockchainBlockDatabaseFilename;
            public string BlockchainCheckpointDatabaseFilename;
        }

        /// <summary>
        /// MemPool databases settings.
        /// </summary>
        public class MemPool
        {
            public string MemPoolDirectoryPath;
            public string MemPoolTransactionDatabaseFilename;
        }

        /// <summary>
        /// Data settings.
        /// </summary>
        public class DatabaseSetting
        {
            public bool EnableEncryptionDatabase;
            public bool EnableCompressDatabase;
            public bool DataFormatIsJson;
        }
    }
}
