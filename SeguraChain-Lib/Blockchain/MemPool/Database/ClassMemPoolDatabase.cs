using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LZ4;
using Newtonsoft.Json;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Blockchain.MemPool.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;

using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.MemPool.Database
{
    // This class is dedicated to TX and Mining PoW Share to valid with other Peers before to apply them.
    public class ClassMemPoolDatabase
    {

        /// <summary>
        /// Dictionary of data.
        /// </summary>
        private static ConcurrentDictionary<long, ConcurrentDictionary<string, ClassMemPoolTransactionObject>> _dictionaryMemPoolTransactionObjects; // Tx Hash |Mem pool transactions object.
        private static SemaphoreSlim _semaphoreMemPoolAccess = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Database settings.
        /// </summary>
        private static byte[] _encryptionKey;
        private static byte[] _encryptionKeyIv;

        #region Manage MemPool Save/Load data.

        /// <summary>
        /// Load Mem Pool Data saved who need to be checked with others peers.
        /// </summary>
        /// <returns></returns>
        public static bool LoadMemPoolDatabase(string encryptionKey, ClassBlockchainDatabaseSetting blockchainDatabaseSetting)
        {
            #region Initialize encryption database properties keys.

            if (blockchainDatabaseSetting.DataSetting.EnableEncryptionDatabase)
            {
                if (encryptionKey.IsNullOrEmpty(false, out _))
                {
                    ClassLog.WriteLine("The input encryption key is empty, can't decrypt MemPool database.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return false;
                }

                if (!ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringAscii(encryptionKey), true, out _encryptionKey))
                {
                    ClassLog.WriteLine("Can't generate encryption key with custom key for decrypt MemPool database content.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return false;
                }
                _encryptionKeyIv = ClassAes.GenerateIv(_encryptionKey);

            }

            #endregion


            _dictionaryMemPoolTransactionObjects = new ConcurrentDictionary<long, ConcurrentDictionary<string, ClassMemPoolTransactionObject>>();


            if (!Directory.Exists(blockchainDatabaseSetting.MemPoolSetting.MemPoolDirectoryPath))
            {
                Directory.CreateDirectory(blockchainDatabaseSetting.MemPoolSetting.MemPoolDirectoryPath);
                return true;
            }

            #region Load Mem Pool Tx Data.


            if (File.Exists(blockchainDatabaseSetting.GetMemPoolTransactionDatabaseFilePath))
            {
                try
                {
                    using (FileStream fileStream = new FileStream(blockchainDatabaseSetting.GetMemPoolTransactionDatabaseFilePath, FileMode.Open))
                    {
                        using (StreamReader reader = blockchainDatabaseSetting.DataSetting.EnableCompressDatabase ? new StreamReader(new LZ4Stream(fileStream, LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression)) : new StreamReader(fileStream))
                        {
                            string line;

                            while ((line = reader.ReadLine()) != null)
                            {
                                if (blockchainDatabaseSetting.DataSetting.EnableEncryptionDatabase)
                                {
                                    if (ClassAes.DecryptionProcess(Convert.FromBase64String(line), _encryptionKey, _encryptionKeyIv, out byte[] decryptedLine))
                                        line = decryptedLine.GetStringFromByteArrayAscii();
                                    else
                                    {
                                        ClassLog.WriteLine("[ERROR] The encryption key selected can't decrypt the data saved on the mem pool database of tx's.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                        return false;
                                    }
                                }

                                if (!line.IsNullOrEmpty(false, out _))
                                {

                                    if (ClassUtility.TryDeserialize(line, out ClassMemPoolTransactionObject transactionObject, ObjectCreationHandling.Reuse))
                                    {
                                        long blockHeightTransaction = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionObject.TransactionObject.TransactionHash);

                                        if (!_dictionaryMemPoolTransactionObjects.ContainsKey(blockHeightTransaction))
                                            _dictionaryMemPoolTransactionObjects.TryAdd(blockHeightTransaction, new ConcurrentDictionary<string, ClassMemPoolTransactionObject>());

                                        if (!_dictionaryMemPoolTransactionObjects[blockHeightTransaction].ContainsKey(transactionObject.TransactionObject.TransactionHash))
                                            _dictionaryMemPoolTransactionObjects[blockHeightTransaction].TryAdd(transactionObject.TransactionObject.TransactionHash, transactionObject);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception error)
                {
                    ClassLog.WriteLine("Can't load mem pool tx objects from database file. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                }
            }

            #endregion


            return true;
        }

        /// <summary>
        /// Save Mem Pool Data from memory to a file.
        /// </summary>
        /// <returns></returns>
        public static bool SaveMemPoolDatabase(ClassBlockchainDatabaseSetting blockchainDatabaseSetting)
        {

            if (!Directory.Exists(blockchainDatabaseSetting.MemPoolSetting.MemPoolDirectoryPath))
                Directory.CreateDirectory(blockchainDatabaseSetting.MemPoolSetting.MemPoolDirectoryPath);

            #region Save Mem Pool Tx Data.


            if (!File.Exists(blockchainDatabaseSetting.GetMemPoolTransactionDatabaseFilePath))
                File.Create(blockchainDatabaseSetting.GetMemPoolTransactionDatabaseFilePath).Close();

            try
            {
                using (StreamWriter writer = blockchainDatabaseSetting.DataSetting.EnableCompressDatabase ?
                    new StreamWriter(new LZ4Stream(new FileStream(blockchainDatabaseSetting.GetMemPoolTransactionDatabaseFilePath, FileMode.Truncate), LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression, ClassBlockchainDatabaseDefaultSetting.Lz4CompressionBlockSize)) :
                    new StreamWriter(new FileStream(blockchainDatabaseSetting.GetMemPoolTransactionDatabaseFilePath, FileMode.Truncate)))
                {
                    if (_dictionaryMemPoolTransactionObjects.Count > 0)
                    {
                        foreach (long blockHeight in _dictionaryMemPoolTransactionObjects.Keys)
                        {
                            foreach (var txObjects in _dictionaryMemPoolTransactionObjects[blockHeight])
                            {

                                if (blockchainDatabaseSetting.DataSetting.EnableEncryptionDatabase)
                                {
                                    if (ClassAes.EncryptionProcess(ClassUtility.GetByteArrayFromStringAscii(ClassUtility.SerializeData(txObjects.Value)), _encryptionKey, _encryptionKeyIv, out byte[] encryptedResult))
                                        writer.WriteLine(Convert.ToBase64String(encryptedResult));
                                }
                                else
                                    writer.WriteLine(ClassUtility.SerializeData(txObjects.Value));
                            }
                        }
                    }
                }
            }
            catch (Exception error)
            {
                ClassLog.WriteLine("Can't save mem pool tx objects to database file. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }


            #endregion

            return true;
        }

        #endregion

        #region Manage Transaction object of MemPool.

        /// <summary>
        /// Reset the whole mem pool database.
        /// </summary>
        public static void ResetMemPool()
        {
            _dictionaryMemPoolTransactionObjects.Clear();
        }

        /// <summary>
        /// Return a transaction from his hash.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<ClassTransactionObject> GetMemPoolTxFromTransactionHash(string transactionHash, long blockHeightTransaction, CancellationTokenSource cancellation)
        {
            ClassTransactionObject transactionObject = null;

            if (blockHeightTransaction < BlockchainSetting.GenesisBlockHeight)
               blockHeightTransaction = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

            if (blockHeightTransaction > BlockchainSetting.GenesisBlockHeight)
            {
                bool semaphoreUsed = false;
                try
                {
                    try
                    {
                        await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                        semaphoreUsed = true;


                        if (_dictionaryMemPoolTransactionObjects.ContainsKey(blockHeightTransaction))
                        {
                            if (_dictionaryMemPoolTransactionObjects[blockHeightTransaction].ContainsKey(transactionHash))
                                transactionObject = _dictionaryMemPoolTransactionObjects[blockHeightTransaction][transactionHash].TransactionObject;
                        }
                    }
                    catch
                    {
                        // Ignored, catch the exception once the cancellation token has been cancelled.
                    }
                }
                finally
                {
                    if (semaphoreUsed)
                        _semaphoreMemPoolAccess.Release();
                }
            }

            return transactionObject;
        }

        /// <summary>
        /// Check if the Mem Pool contains a tx of the block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<Tuple<bool, int>> MemPoolContainsBlockHeight(long blockHeight, CancellationTokenSource cancellation)
        {
            int countTx = 0;

            if (blockHeight > BlockchainSetting.GenesisBlockHeight)
            {
                bool semaphoreUsed = false;

                try
                {
                    try
                    {
                        await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                        semaphoreUsed = true;

                        if (_dictionaryMemPoolTransactionObjects.Count > 0 && _dictionaryMemPoolTransactionObjects.ContainsKey(blockHeight))
                            countTx = _dictionaryMemPoolTransactionObjects[blockHeight].Count;
                    }
                    catch
                    {
                        // Ignored, catch the exception once the cancellation token has been cancelled.
                    }
                }
                finally
                {
                    if (semaphoreUsed)
                        _semaphoreMemPoolAccess.Release();
                }

            }

            return new Tuple<bool, int>(countTx > 0, countTx);
        }

        /// <summary>
        /// Get every block heights listed.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<DisposableList<long>> GetMemPoolListBlockHeight(CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;
            DisposableList<long> listBlockHeight = new DisposableList<long>();

            try
            {
                try {
                    await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_dictionaryMemPoolTransactionObjects.Count > 0)
                        listBlockHeight.GetList = _dictionaryMemPoolTransactionObjects.Keys.ToList();
                }
                catch
                {
                    // Ignored, catch the exception once the cancellation token has been cancelled.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreMemPoolAccess.Release();
            }

            return listBlockHeight;
        }

        /// <summary>
        /// Get tx's object(s) inside mem pool who target a specific block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<DisposableList<ClassTransactionObject>> GetMemPoolTxObjectFromBlockHeight(long blockHeight, bool exceptBlockReward, CancellationTokenSource cancellation)
        {
            DisposableList<ClassTransactionObject> listMemPoolTxObjects = new DisposableList<ClassTransactionObject>();

            bool semaphoreUsed = false;

            try
            {
                try
                {
                    await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_dictionaryMemPoolTransactionObjects.Count > 0 && _dictionaryMemPoolTransactionObjects.ContainsKey(blockHeight))
                    {
                        if (_dictionaryMemPoolTransactionObjects[blockHeight].Count > 0)
                        {
                            foreach (var memPoolTxObject in _dictionaryMemPoolTransactionObjects[blockHeight].OrderBy(x => x.Key))
                            {
                                if (cancellation.IsCancellationRequested)
                                    break;

                                if (exceptBlockReward)
                                {
                                    if (memPoolTxObject.Value.TransactionObject.TransactionType == ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION ||
                                        memPoolTxObject.Value.TransactionObject.TransactionType == ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                                        continue;
                                }

                                if (memPoolTxObject.Value.TransactionObject.BlockHeightTransaction == blockHeight)
                                    listMemPoolTxObjects.Add(memPoolTxObject.Value.TransactionObject);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignored, catch the exception once the cancellation token has been cancelled.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreMemPoolAccess.Release();
            }

            return listMemPoolTxObjects;
        }

        /// <summary>
        /// Get all tx's object(s) inside mem pool.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<DisposableList<ClassTransactionObject>> GetMemPoolAllTxObject(CancellationTokenSource cancellation)
        {
            DisposableList<ClassTransactionObject> listMemPoolTxObjects = new DisposableList<ClassTransactionObject>();

            bool semaphoreUsed = false;
            try
            {
                try
                {
                    await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_dictionaryMemPoolTransactionObjects.Count > 0)
                    {
                        foreach (var blockHeight in _dictionaryMemPoolTransactionObjects.Keys)
                        {
                            cancellation.Token.ThrowIfCancellationRequested();

                            if (_dictionaryMemPoolTransactionObjects[blockHeight].Count > 0)
                            {
                                foreach (var memPoolTxObject in _dictionaryMemPoolTransactionObjects[blockHeight])
                                {
                                    cancellation.Token.ThrowIfCancellationRequested();

                                    listMemPoolTxObjects.Add(memPoolTxObject.Value.TransactionObject);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignored, catch the exception once the cancellation token has been cancelled.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreMemPoolAccess.Release();
            }

            return listMemPoolTxObjects;
        }

        /// <summary>
        /// Get all tx's object(s) inside the MemPool linked to a specific wallet address target.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="maxBlockHeightTarget"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<DisposableList<ClassTransactionObject>> GetMemPoolTxFromWalletAddressTargetAsync(string walletAddress, long maxBlockHeightTarget, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;
            DisposableList<ClassTransactionObject> listMemPoolTransaction = new DisposableList<ClassTransactionObject>();

            try
            {
                try
                {
                    await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_dictionaryMemPoolTransactionObjects.Count > 0)
                    {
                        foreach (var blockHeight in _dictionaryMemPoolTransactionObjects.Keys.Where(x => x <= maxBlockHeightTarget))
                        {
                            cancellation.Token.ThrowIfCancellationRequested();

                            if (_dictionaryMemPoolTransactionObjects[blockHeight].Count > 0)
                            {
                                foreach (var memPoolTxObject in _dictionaryMemPoolTransactionObjects[blockHeight].Where(x => x.Value.TransactionObject.WalletAddressReceiver == walletAddress || x.Value.TransactionObject.WalletAddressSender == walletAddress))
                                {
                                    cancellation.Token.ThrowIfCancellationRequested();
                                    listMemPoolTransaction.Add(memPoolTxObject.Value.TransactionObject);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignored, catch the exception once the cancellation token has been cancelled.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreMemPoolAccess.Release();
            }

            return listMemPoolTransaction;
        }

        /// <summary>
        /// Get all tx's object(s) inside the MemPool linked to a specific wallet address target.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<DisposableList<ClassTransactionObject>> GetMemPoolAllTxFromWalletAddressTargetAsync(string walletAddress, CancellationTokenSource cancellation)
        {
            bool semaphoreUsed = false;

            DisposableList<ClassTransactionObject> disposableListTransaction = new DisposableList<ClassTransactionObject>();

            try
            {
                try
                {
                    await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_dictionaryMemPoolTransactionObjects.Count > 0)
                    {
                        foreach (var blockHeight in _dictionaryMemPoolTransactionObjects.Keys)
                        {
                            cancellation.Token.ThrowIfCancellationRequested();

                            if (_dictionaryMemPoolTransactionObjects[blockHeight].Count > 0)
                            {
                                foreach (var memPoolTxObject in _dictionaryMemPoolTransactionObjects[blockHeight].Where(x => x.Value.TransactionObject.WalletAddressReceiver == walletAddress || x.Value.TransactionObject.WalletAddressSender == walletAddress))
                                {
                                    cancellation.Token.ThrowIfCancellationRequested();

                                    if (memPoolTxObject.Value != null && memPoolTxObject.Value?.TransactionObject != null)
                                        disposableListTransaction.Add(memPoolTxObject.Value.TransactionObject);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignored, catch the exception once the cancellation token has been cancelled.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreMemPoolAccess.Release();
            }

            return disposableListTransaction;
        }

        /// <summary>
        /// Remove a tx object from mempool.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="cancellation"></param>
        public static async Task<bool> RemoveMemPoolTxObject(string transactionHash, CancellationTokenSource cancellation)
        {
            bool result = false;
            bool semaphoreUsed = false;

            try
            {
                try
                {
                    await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_dictionaryMemPoolTransactionObjects.Count > 0)
                    {
                        long blockHeightTransaction = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

                        if (blockHeightTransaction > BlockchainSetting.GenesisBlockHeight && _dictionaryMemPoolTransactionObjects.ContainsKey(blockHeightTransaction))
                        {
                            if (_dictionaryMemPoolTransactionObjects[blockHeightTransaction].Count > 0 && _dictionaryMemPoolTransactionObjects[blockHeightTransaction].ContainsKey(transactionHash))
                                result = _dictionaryMemPoolTransactionObjects[blockHeightTransaction].TryRemove(transactionHash, out _);
                        }
                    }
                }
                catch
                {
                    // Ignored, catch the exception once the cancellation token has been cancelled.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreMemPoolAccess.Release();
            }

            return result;
        }

        /// <summary>
        /// Remove all tx from a block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        public static async Task RemoveMemPoolAllTxFromBlockHeightTarget(long blockHeight, CancellationTokenSource cancellation)
        {
            if (blockHeight > BlockchainSetting.GenesisBlockHeight)
            {
                bool semaphoreUsed = false;
                try
                {
                    try
                    {
                        await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                        semaphoreUsed = true;

                        if (_dictionaryMemPoolTransactionObjects.Count > 0 && _dictionaryMemPoolTransactionObjects.ContainsKey(blockHeight))
                        {
                            _dictionaryMemPoolTransactionObjects[blockHeight].Clear();
                            _dictionaryMemPoolTransactionObjects.TryRemove(blockHeight, out _);
                        }
                    }
                    catch
                    {
                        // Ignored, catch the exception once the cancellation token has been cancelled.
                    }
                }
                finally
                {
                    if (semaphoreUsed)
                        _semaphoreMemPoolAccess.Release();
                }
            }
        }

        /// <summary>
        /// Check if the tx hash is already stored.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> CheckTxHashExist(string transactionHash, CancellationTokenSource cancellation)
        {
            bool result = false;
            bool semaphoreUsed = false;

            try
            {
                try
                {
                    await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_dictionaryMemPoolTransactionObjects.Count > 0)
                    {
                        long blockHeightTransaction = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionHash);

                        if (blockHeightTransaction > BlockchainSetting.GenesisBlockHeight)
                        {
                            if (_dictionaryMemPoolTransactionObjects.ContainsKey(blockHeightTransaction))
                                result = _dictionaryMemPoolTransactionObjects[blockHeightTransaction].ContainsKey(transactionHash);
                        }
                    }
                }
                catch
                {
                    // Ignored, catch the exception once the cancellation token has been cancelled.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreMemPoolAccess.Release();
            }

            return result;
        }

        /// <summary>
        /// Insert a transaction into the mem pool.
        /// </summary>
        /// <param name="transactionObject"></param>
        /// 
        /// <returns></returns>
        public static bool InsertTxToMemPool(ClassTransactionObject transactionObject)
        {
            bool result = false;
            long blockHeightTransaction = ClassTransactionUtility.GetBlockHeightFromTransactionHash(transactionObject.TransactionHash);

            if (blockHeightTransaction > BlockchainSetting.GenesisBlockHeight)
            {
                try
                {
                    bool existList = true;

                    if (!_dictionaryMemPoolTransactionObjects.ContainsKey(blockHeightTransaction))
                    {
                        existList = _dictionaryMemPoolTransactionObjects.TryAdd(blockHeightTransaction, new ConcurrentDictionary<string, ClassMemPoolTransactionObject>());

                        if (!existList)
                            existList = _dictionaryMemPoolTransactionObjects.ContainsKey(blockHeightTransaction);
                    }

                    if (existList)
                    {
                        if (!_dictionaryMemPoolTransactionObjects[blockHeightTransaction].ContainsKey(transactionObject.TransactionHash))
                        {
                            result = _dictionaryMemPoolTransactionObjects[blockHeightTransaction].TryAdd(transactionObject.TransactionHash, new ClassMemPoolTransactionObject()
                            {
                                TransactionObject = transactionObject,
                                TotalNoConsensusCount = 0
                            });
                        }
                    }
                }
                catch
                {
                    // Ignored.
                }
            }

            return result;
        }

        /// <summary>
        /// Return the amount of tx stored.
        /// </summary>
        public static long GetCountMemPoolTx
        {
            get
            {
                long countMemPoolTx = 0;

                if (_dictionaryMemPoolTransactionObjects.Count > 0)
                {
                    foreach(long blockHeight in _dictionaryMemPoolTransactionObjects.Keys.ToArray())
                    {
                        try
                        {
                            countMemPoolTx += _dictionaryMemPoolTransactionObjects[blockHeight].Count;
                        }
                        catch
                        {
                            // Ignored.
                        }
                    }
                }

                return countMemPoolTx;
            }
        }

            

        /// <summary>
        /// Get tx's object(s) inside mem pool who target a specific block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<int> GetCountMemPoolTxFromBlockHeight(long blockHeight, bool exceptBlockReward, CancellationTokenSource cancellation)
        {
            int result = 0;
            bool semaphoreUsed = false;
            try
            {
                try
                {
                    await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_dictionaryMemPoolTransactionObjects.Count > 0 && _dictionaryMemPoolTransactionObjects.ContainsKey(blockHeight))
                    {
                        if (exceptBlockReward)
                            result = _dictionaryMemPoolTransactionObjects[blockHeight].Count(x => x.Value.TransactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION && x.Value.TransactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION);
                        else
                            result = _dictionaryMemPoolTransactionObjects[blockHeight].Count;
                    }
                }
                catch
                {
                    // Ignored, catch the exception once the cancellation token has been cancelled.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreMemPoolAccess.Release();
            }

            return result;
        }

        /// <summary>
        /// Get the list of block height with their tx count.
        /// </summary>
        /// <param name="exceptBlockReward"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<DisposableSortedList<long, int>> GetListCountMemPoolTx(bool exceptBlockReward, CancellationTokenSource cancellation)
        {
            DisposableSortedList<long, int> listTxCount = new DisposableSortedList<long, int>();

            bool semaphoreUsed = false;

            try
            {
                try
                {
                    await _semaphoreMemPoolAccess.WaitAsync(cancellation.Token);
                    semaphoreUsed = true;

                    if (_dictionaryMemPoolTransactionObjects.Count > 0)
                    {
                        using (DisposableList<long> listBlockHeight = new DisposableList<long>(false, 0, _dictionaryMemPoolTransactionObjects.Keys.ToList()))
                        {
                            foreach (long blockHeight in listBlockHeight.GetList)
                            {
                                int count = 0;

                                if (exceptBlockReward)
                                    count = _dictionaryMemPoolTransactionObjects[blockHeight].Count(x => x.Value.TransactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION && x.Value.TransactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION);
                                else
                                    count = _dictionaryMemPoolTransactionObjects[blockHeight].Count;

                                listTxCount.Add(blockHeight, count);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignored, catch the exception once the cancellation token has been cancelled.
                }
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreMemPoolAccess.Release();
            }

            return listTxCount;
        }

        #endregion


    }
}
