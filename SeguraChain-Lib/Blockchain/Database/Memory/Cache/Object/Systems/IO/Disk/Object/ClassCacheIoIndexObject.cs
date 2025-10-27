using SeguraChain_Lib.Blockchain.Block.Function;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Disk.Function;
using SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Main;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Cache.Object.Systems.IO.Disk.Object
{
    public class ClassCacheIoIndexObject : ClassCacheIoFunction
    {

        /// <summary>
        /// The controller object, usefull for calculate the memory usage across multiple io cache files.
        /// </summary>
        private ClassCacheIoSystem _ioCacheSystem;

        /// <summary>
        /// Contains every IO Data and their structures.
        /// </summary>
        private Dictionary<long, ClassCacheIoStructureObject> _ioStructureObjectsDictionary;

        /// <summary>
        /// IO Streams and files paths.
        /// </summary>
        private bool _ioStreamsClosed;
        private bool _ioOnWriteData;
        private FileStream _ioDataStructureFileLockStream;
        private string _ioDataStructureFilename;
        private string _ioDataStructureFilePath;
        private UTF8Encoding _ioDataUtf8Encoding;
        private long _lastIoCacheFileLength;
        private long _totalIoCacheFileSize;
        private long _lastIoCacheTotalFileSizeWritten;

        /// <summary>
        /// Lock multithreading access.
        /// </summary>
        private SemaphoreSlim _ioSemaphoreAccess;

        /// <summary>
        /// Blockchain database settings.
        /// </summary>
        private ClassBlockchainDatabaseSetting _blockchainDatabaseSetting;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ioDataStructureFilename"></param>
        /// <param name="blockchainDatabaseSetting"></param>
        /// <param name="ioCacheSystem"></param>
        public ClassCacheIoIndexObject(string ioDataStructureFilename, ClassBlockchainDatabaseSetting blockchainDatabaseSetting, ClassCacheIoSystem ioCacheSystem)
        {
            _ioCacheSystem = ioCacheSystem;
            _blockchainDatabaseSetting = blockchainDatabaseSetting;
            _ioDataStructureFilename = ioDataStructureFilename;
            _ioDataStructureFilePath = blockchainDatabaseSetting.GetBlockchainCacheDirectoryPath + ioDataStructureFilename;
            _ioStructureObjectsDictionary = new Dictionary<long, ClassCacheIoStructureObject>();
            _ioSemaphoreAccess = new SemaphoreSlim(1, 1);
            _ioDataUtf8Encoding = new UTF8Encoding(true, false);
        }

        #region Initialize Io Cache Index functions.

        /// <summary>
        /// Initialize the io cache object.
        /// </summary>
        /// <returns></returns>
        public async Task<Tuple<bool, HashSet<long>>> InitializeIoCacheObjectAsync()
        {
            try
            {
                if (!File.Exists(_ioDataStructureFilePath))
                    File.Create(_ioDataStructureFilePath).Close();

                _ioDataStructureFileLockStream = new FileStream(_ioDataStructureFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskReadStreamBufferSize, FileOptions.Asynchronous | FileOptions.RandomAccess);

                return File.Exists(_ioDataStructureFilePath) ? await RunIoDataIndexingAsync() : new Tuple<bool, HashSet<long>>(true, new HashSet<long>());
            }
            catch
            {
                return new Tuple<bool, HashSet<long>>(false, new HashSet<long>());
            }
        }

        /// <summary>
        /// Run io data indexing.
        /// </summary>
        /// <returns></returns>
        private async Task<Tuple<bool, HashSet<long>>> RunIoDataIndexingAsync()
        {
            await ResetSeekLockStreamAsync(null);

            using (StreamReader reader = new StreamReader(_ioDataStructureFileLockStream, _ioDataUtf8Encoding, false, _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskReadStreamBufferSize, true))
            {
                string ioDataLine;

                long currentStreamPosition = 0;

                while ((ioDataLine = reader.ReadLine()) != null)
                {
                    if (ioDataLine.StartsWith(IoDataBeginBlockString))
                    {
                        long blockHeight = ExtractBlockHeight(ioDataLine);

                        if (blockHeight > BlockchainSetting.GenesisBlockHeight)
                        {

                            #region Retrieve important informations from the block.

                            string ioDataMerged = ioDataLine + Environment.NewLine;

                            bool complete = false;

                            while (true)
                            {
                                ioDataLine = reader.ReadLine();

                                if (ioDataLine == null)
                                    break;

                                if (ioDataLine.StartsWith(IoDataEndBlockString))
                                {
                                    ioDataMerged += ioDataLine + Environment.NewLine;
                                    complete = true;
                                    break;
                                }

                                ioDataMerged += ioDataLine + Environment.NewLine;
                            }

                            #endregion

                            if (complete)
                            {
                                // Test the block object read.
                                if (IoStringDataLineToBlockObject(ioDataMerged, _blockchainDatabaseSetting, false, null, out ClassBlockObject blockObject))
                                {
                                    if (blockObject?.BlockHeight == blockHeight)
                                    {
                                        if (!_ioStructureObjectsDictionary.ContainsKey(blockHeight))
                                        {
                                            try
                                            {
                                                _ioStructureObjectsDictionary.Add(blockHeight, new ClassCacheIoStructureObject()
                                                {
                                                    IsWritten = true,
                                                    IoDataPosition = currentStreamPosition,
                                                    IoDataSizeOnFile = ioDataMerged.Length,
                                                });

                                                if (await ReadAndGetIoBlockDataFromBlockHeightOnIoStreamFile(blockHeight, null, false) == null)
                                                    _ioStructureObjectsDictionary.Remove(blockHeight);
                                            }
                                            catch
                                            {
                                                // Ignored.
                                            }
                                        }
                                        else
                                        {
                                            long previousPosition = _ioStructureObjectsDictionary[blockHeight].IoDataPosition;
                                            long previousSize = _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile;
                                            _ioStructureObjectsDictionary[blockHeight].IoDataPosition = currentStreamPosition;
                                            _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile = ioDataMerged.Length;

                                            if (await ReadAndGetIoBlockDataFromBlockHeightOnIoStreamFile(blockHeight, null, false) == null)
                                            {
                                                _ioStructureObjectsDictionary[blockHeight].IoDataPosition = previousPosition;
                                                _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile = previousSize;
                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }

                    reader.DiscardBufferedData();
                    reader.BaseStream.Flush();
                    currentStreamPosition = reader.BaseStream.Position;
                }
            }


            return new Tuple<bool, HashSet<long>>(true, _ioStructureObjectsDictionary.Keys.ToHashSet());
        }

        #endregion

        #region Get/Set/Insert/Delete/Contains IO Data functions.

        #region Get IO Data functions.

        /// <summary>
        /// Get a list of block data information by a list of block height from the io cache file or from the memory.
        /// </summary>
        /// <param name="listBlockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<List<ClassBlockObject>> GetIoListBlockDataInformationFromListBlockHeight(List<long> listBlockHeight, CancellationTokenSource cancellationIoCache)
        {
            List<ClassBlockObject> listBlockInformation = new List<ClassBlockObject>();

            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (!useSemaphore)
                    return listBlockInformation;

                foreach (long blockHeight in listBlockHeight)
                {
                    try
                    {
                        if (_ioStructureObjectsDictionary.ContainsKey(blockHeight))
                        {
                            if (!_ioStructureObjectsDictionary[blockHeight].IsNull)
                            {
                                _ioStructureObjectsDictionary[blockHeight].BlockObject.DeepCloneBlockObject(false, out ClassBlockObject blockObjectCopy);
                                listBlockInformation.Add(blockObjectCopy);
                            }
                            else
                            {
                                ClassBlockObject blockObject = await CallGetRetrieveDataAccess(blockHeight, false, true, false, cancellationIoCache);

                                if (blockObject != null)
                                    listBlockInformation.Add(blockObject);
                            }
                        }
                    }
                    catch
                    {
                        listBlockInformation.Clear();
                        break;
                    }
                }
            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }

            return listBlockInformation;
        }

        /// <summary>
        /// Retrieve back only a io block data information object from a block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<ClassBlockObject> GetIoBlockDataInformationFromBlockHeight(long blockHeight, CancellationTokenSource cancellationIoCache)
        {
            ClassBlockObject blockObject = null;

            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (useSemaphore)
                    blockObject = await CallGetRetrieveDataAccess(blockHeight, false, true, false, cancellationIoCache);
            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }

            return blockObject;
        }

        /// <summary>
        /// Retrieve back only the block transaction count from a block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<int> GetIoBlockTransactionCountFromBlockHeight(long blockHeight, CancellationTokenSource cancellationIoCache)
        {
            int blockTransactionCount = 0;

            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (!useSemaphore)
                    return blockTransactionCount;

                if (_ioStructureObjectsDictionary.ContainsKey(blockHeight))
                {

                    ClassBlockObject blockObject = await CallGetRetrieveDataAccess(blockHeight, false, true, false, cancellationIoCache);

                    if (blockObject != null)
                        blockTransactionCount = blockObject.TotalTransaction;
                }

            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }

            return blockTransactionCount;
        }

        /// <summary>
        /// Retrieve back a io block data object from a block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<ClassBlockObject> GetIoBlockDataFromBlockHeight(long blockHeight, bool keepAlive, bool clone, CancellationTokenSource cancellationIoCache)
        {
            ClassBlockObject blockObject = null;
            bool useSemaphore = false;

            try
            {

                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (useSemaphore)
                   return await CallGetRetrieveDataAccess(blockHeight, keepAlive, false, clone, cancellationIoCache);
            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }

            return blockObject;
        }

        /// <summary>
        /// Call a retrieve get data access.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive"></param>
        /// <param name="blockInformationsOnly"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<ClassBlockObject> CallGetRetrieveDataAccess(long blockHeight, bool keepAlive, bool blockInformationsOnly, bool clone, CancellationTokenSource cancellation)
        {
            ClassBlockObject blockObject = null;

            if (_ioStructureObjectsDictionary.ContainsKey(blockHeight))
            {
                if (!_ioStructureObjectsDictionary[blockHeight].IsDeleted)
                {
                    if (!_ioStructureObjectsDictionary[blockHeight].IsNull)
                    {
                        if (!blockInformationsOnly)
                            blockObject = clone ? _ioStructureObjectsDictionary[blockHeight].BlockObject.DirectCloneBlockObject() : _ioStructureObjectsDictionary[blockHeight].BlockObject;
                        else
                            _ioStructureObjectsDictionary[blockHeight].BlockObject.DeepCloneBlockObject(false, out blockObject);
                    }
                    else
                    {
                        blockObject = await ReadAndGetIoBlockDataFromBlockHeightOnIoStreamFile(blockHeight, cancellation, blockInformationsOnly);

                        // Update the io cache file and remove the data updated from the active memory
                        if (blockObject != null && keepAlive && !blockInformationsOnly)
                            await InsertInActiveMemory(blockObject, keepAlive, true, cancellation);
                    }
                }
            }

            return blockObject;
        }

        /// <summary>
        /// Retrieve back a list of block object from the io cache object target by a list of block height.
        /// </summary>
        /// <param name="listBlockHeight">The list of block height target.</param>
        /// <param name="keepAlive">Keep alive or not data inside of the active memory.</param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<DisposableList<ClassBlockObject>> GetIoListBlockDataFromListBlockHeight(HashSet<long> listBlockHeight, bool keepAlive, bool clone, CancellationTokenSource cancellationIoCache)
        {

            DisposableList<ClassBlockObject> listBlockObjectDisposable = new DisposableList<ClassBlockObject>();

            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (!useSemaphore)
                    return listBlockObjectDisposable;

                foreach (long blockHeight in listBlockHeight.ToArray())
                {
                    if (!listBlockHeight.Contains(blockHeight) || _ioStructureObjectsDictionary[blockHeight].IsNull)
                        continue;

                    listBlockObjectDisposable.Add(clone ? _ioStructureObjectsDictionary[blockHeight].BlockObject.DirectCloneBlockObject() : _ioStructureObjectsDictionary[blockHeight].BlockObject);
                    listBlockHeight.Remove(blockHeight);
                }


                if (listBlockHeight.Count > 0)
                {
                    foreach (ClassBlockObject blockObject in await ReadAndGetIoBlockDataFromListBlockHeightOnIoStreamFile(listBlockHeight, cancellationIoCache))
                    {
                        long blockHeight = blockObject.BlockHeight;

                        if (listBlockHeight.Contains(blockHeight))
                        {
                            if (!_ioStructureObjectsDictionary[blockHeight].IsDeleted)
                            {
                                if (_ioStructureObjectsDictionary[blockHeight].IsNull)
                                {
                                    listBlockObjectDisposable.Add(blockObject);
                                    listBlockHeight.Remove(blockHeight);

                                    // Update the io cache file and remove the data updated from the active memory
                                    if (keepAlive)
                                        await InsertInActiveMemory(blockObject, keepAlive, true, cancellationIoCache);
                                }
                                else
                                {
                                    listBlockObjectDisposable.Add(clone ? _ioStructureObjectsDictionary[blockHeight].BlockObject.DirectCloneBlockObject() : _ioStructureObjectsDictionary[blockHeight].BlockObject);
                                    listBlockHeight.Remove(blockHeight);
                                }
                            }
                        }
                    }
                }

            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }

            return listBlockObjectDisposable;
        }

        /// <summary>
        /// Retrieve back a block transaction from a block height target by a transaction index selected.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="transactionHash"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<ClassBlockTransaction> GetBlockTransactionFromIoBlockHeightByTransactionHash(long blockHeight, string transactionHash, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (!useSemaphore || !_ioStructureObjectsDictionary.ContainsKey(blockHeight))
                    return null;

                if (_ioStructureObjectsDictionary[blockHeight].IsNull)
                {
                    ClassBlockObject blockObject = await CallGetRetrieveDataAccess(blockHeight, keepAlive, false, false, cancellationIoCache);

                    if (blockObject != null)
                    {
                        if (blockObject.BlockTransactions.ContainsKey(transactionHash))
                            return blockObject.BlockTransactions[transactionHash].Clone();
                    }
                }
                else
                {
                    if (_ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions.ContainsKey(transactionHash))
                        return _ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions[transactionHash].Clone();
                }

            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }

            return null;
        }

        #endregion

        #region Set IO Data functions.

        /// <summary>
        /// Push or update a io block data to the io cache.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> PushOrUpdateIoBlockData(ClassBlockObject blockObject, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (!useSemaphore || blockObject == null)
                    return false;

                if (_ioStructureObjectsDictionary.ContainsKey(blockObject.BlockHeight))
                {
                    blockObject.BlockIsUpdated = true;
                    await InsertInActiveMemory(blockObject, keepAlive, false, cancellationIoCache);
                    _ioStructureObjectsDictionary[blockObject.BlockHeight].IsDeleted = false;
                    return true;
                }
                else
                {
                    try
                    {
                        _ioStructureObjectsDictionary.Add(blockObject.BlockHeight, new ClassCacheIoStructureObject()
                        {
                            IsWritten = false,
                            IoDataPosition = 0
                        });
                    }
                    catch
                    {
                        return false;
                    }

                    if (await WriteNewIoDataOnIoStreamFile(blockObject, cancellationIoCache))
                        return true;
                }
            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }
            return false;
        }

        /// <summary>
        /// Push or update transaction on a io block data.
        /// </summary>
        /// <param name="blockTransaction"></param>
        /// <param name="blockHeight"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> PushOrUpdateTransactionOnIoBlockData(ClassBlockTransaction blockTransaction, long blockHeight, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (!useSemaphore || !_ioStructureObjectsDictionary.ContainsKey(blockHeight))
                    return false;

                if (!_ioStructureObjectsDictionary[blockHeight].IsNull)
                {
                    if (_ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions != null)
                    {
                        if (_ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                            _ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions[blockTransaction.TransactionObject.TransactionHash] = blockTransaction;

                        else
                            _ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions.Add(blockTransaction.TransactionObject.TransactionHash, blockTransaction);

                        _ioStructureObjectsDictionary[blockHeight].BlockObject.BlockIsUpdated = true;
                        _ioStructureObjectsDictionary[blockHeight].IsDeleted = false;
                        return false;
                    }
                }
                else
                {
                    ClassBlockObject blockObject = await CallGetRetrieveDataAccess(blockHeight, false, false, false, cancellationIoCache);

                    if (blockObject?.BlockTransactions != null)
                    {

                        if (blockObject.BlockTransactions.ContainsKey(blockTransaction.TransactionObject.TransactionHash))
                            blockObject.BlockTransactions[blockTransaction.TransactionObject.TransactionHash] = blockTransaction;
                        else
                            blockObject.BlockTransactions.Add(blockTransaction.TransactionObject.TransactionHash, blockTransaction);

                        blockObject.BlockIsUpdated = true;

                        _ioStructureObjectsDictionary[blockHeight].IsDeleted = false;

                        // Update the io cache file and remove the data updated from the active memory
                        await InsertInActiveMemory(blockObject, true, keepAlive, cancellationIoCache);

                        return true;
                    }
                }

            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }

            return false;
        }

        /// <summary>
        /// Push or update list of io block data to the io cache.
        /// </summary>
        /// <param name="listBlockObject"></param>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> PushOrUpdateListIoBlockData(List<ClassBlockObject> listBlockObject, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {
            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (!useSemaphore || listBlockObject.Count == 0)
                    return false;

                int countPushed = 0;

                for (int i = 0; i < listBlockObject.Count; i++)
                {
                    if (cancellationIoCache.IsCancellationRequested)
                        break;

                    if (i < listBlockObject.Count)
                    {
                        if (_ioStructureObjectsDictionary.ContainsKey(listBlockObject[i].BlockHeight))
                        {
                            await InsertInActiveMemory(listBlockObject[i], keepAlive, false, cancellationIoCache);
                            _ioStructureObjectsDictionary[listBlockObject[i].BlockHeight].IsDeleted = false;

                            countPushed++;
                        }
                        else
                        {

                            try
                            {
                                _ioStructureObjectsDictionary.Add(listBlockObject[i].BlockHeight, new ClassCacheIoStructureObject()
                                {
                                    BlockObject = null,
                                    IsWritten = false,
                                    IoDataPosition = 0
                                });
                            }
                            catch
                            {
                                break;
                            }

                            if (await WriteNewIoDataOnIoStreamFile(listBlockObject[i], cancellationIoCache))
                                countPushed++;
                        }


                    }
                }

                return countPushed == listBlockObject.Count;
            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }
        }

        /// <summary>
        /// Push or update list of io block transaction data to the io cache.
        /// </summary>
        /// <param name="keepAlive"></param>
        /// <param name="cancellationIoCache"></param>
        /// <param name="listBlockTransaction"></param>
        /// <returns></returns>
        public async Task<bool> PushOrUpdateListIoBlockTransactionData(List<ClassBlockTransaction> listBlockTransaction, bool keepAlive, CancellationTokenSource cancellationIoCache)
        {

            bool useSemaphore = false;
            int countPushed = 0;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (!useSemaphore || listBlockTransaction.Count == 0)
                    return false;


                for (int i = 0; i < listBlockTransaction.Count; i++)
                {
                    if (cancellationIoCache.IsCancellationRequested)
                        break;

                    if (i < listBlockTransaction.Count)
                    {
                        long blockHeight = listBlockTransaction[i].TransactionObject.BlockHeightTransaction;
                        string transactionHash = listBlockTransaction[i].TransactionObject.TransactionHash;

                        if (_ioStructureObjectsDictionary.ContainsKey(blockHeight))
                        {
                            if (!_ioStructureObjectsDictionary[blockHeight].IsNull)
                            {
                                if (_ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions != null)
                                {
                                    if (_ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions.ContainsKey(transactionHash))
                                        _ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions[transactionHash] = listBlockTransaction[i];
                                    else
                                        _ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions.Add(transactionHash, listBlockTransaction[i]);

                                    _ioStructureObjectsDictionary[blockHeight].BlockObject.BlockIsUpdated = true;
                                    _ioStructureObjectsDictionary[blockHeight].BlockObject.BlockLastChangeTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;
                                    countPushed++;
                                }
                            }
                            else
                            {

                                ClassBlockObject blockObject = await CallGetRetrieveDataAccess(blockHeight, keepAlive, false, false, cancellationIoCache);

                                if (blockObject?.BlockTransactions != null)
                                {
                                    if (blockObject.BlockTransactions.ContainsKey(transactionHash))
                                        blockObject.BlockTransactions[transactionHash] = listBlockTransaction[i];
                                    else
                                        blockObject.BlockTransactions.Add(transactionHash, listBlockTransaction[i]);

                                    blockObject.BlockIsUpdated = true;
                                    blockObject.BlockLastChangeTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;

                                    // Update the io cache file and remove the data updated from the active memory
                                    await InsertInActiveMemory(blockObject, keepAlive, false, cancellationIoCache);
                                    countPushed++;
                                }
                            }
                        }
                    }
                }

                return countPushed == listBlockTransaction.Count;
            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }
        }

        #endregion

        #region Manage content functions.

        /// <summary>
        /// Delete a io block data, it's done later after a purge of data or by another set.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> TryDeleteIoBlockData(long blockHeight, CancellationTokenSource cancellationIoCache)
        {
            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (!useSemaphore || !_ioStructureObjectsDictionary.ContainsKey(blockHeight))
                    return false;

                _ioStructureObjectsDictionary[blockHeight].IsDeleted = true;
                _ioStructureObjectsDictionary[blockHeight].BlockObject = null;
                return true;
            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }
        }

        /// <summary>
        /// Check if the io cache contain a block height target.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<bool> ContainsIoBlockHeight(long blockHeight, CancellationTokenSource cancellation)
        {
            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellation);

                if (!useSemaphore)
                    return false;

                return _ioStructureObjectsDictionary.ContainsKey(blockHeight);

            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }
        }

        /// <summary>
        /// Check if io blocks contain a transaction hash.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellationIoCache"></param>
        /// <returns></returns>
        public async Task<bool> ContainIoBlockTransactionHash(string transactionHash, long blockHeight, CancellationTokenSource cancellationIoCache)
        {

            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellationIoCache);

                if (!useSemaphore || blockHeight <= BlockchainSetting.GenesisBlockHeight || !_ioStructureObjectsDictionary.ContainsKey(blockHeight))
                    return false;

                if (!_ioStructureObjectsDictionary[blockHeight].IsNull)
                {
                    if (_ioStructureObjectsDictionary[blockHeight].BlockObject.BlockTransactions.ContainsKey(transactionHash))
                        return true;
                }
                else
                {
                    ClassBlockObject blockObject = await CallGetRetrieveDataAccess(blockHeight, false, false, false, cancellationIoCache);

                    if (blockObject != null)
                    {
                        if (blockObject.BlockTransactions.ContainsKey(transactionHash))
                            return true;
                    }
                }
            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }

            return false;
        }

        /// <summary>
        /// Return the list of block height indexed.
        /// </summary>
        /// <returns></returns>
        public async Task<HashSet<long>> GetIoBlockHeightListIndexed(CancellationTokenSource cancellation)
        {
            bool useSemaphore = false;

            try
            {
                useSemaphore = await _ioSemaphoreAccess.TryWaitAsync(cancellation);

                if (!useSemaphore)
                    return new HashSet<long>();

                return new HashSet<long>(_ioStructureObjectsDictionary.Keys.ToList());

            }
            finally
            {
                if (useSemaphore)
                    _ioSemaphoreAccess.Release();
            }
        }

        #endregion

        #endregion

        #region Manage IO Data functions.

        /// <summary>
        /// Clean io memory data in the active memory.
        /// </summary>
        /// <returns></returns>
        public async Task<long> PurgeIoBlockDataMemory(bool semaphoreUsed, CancellationTokenSource cancellation, long totalMemoryAsked, bool enableAskMemory)
        {
            bool useSemaphore = false;
            long totalAvailableMemoryRetrieved = 0;

            try
            {
                if (!semaphoreUsed)
                {
                    await _ioSemaphoreAccess.WaitAsync(cancellation.Token);
                    useSemaphore = true;
                }

                using (DisposableDictionary<long, ClassBlockObject> listIoData = new DisposableDictionary<long, ClassBlockObject>())
                {
                    HashSet<long> listIoDataDeleted = new HashSet<long>();

                    #region Sort data to cache, sort data to keep in active memory and sort data to delete forever.

                    long timestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;

                    foreach (long ioBlockHeight in _ioStructureObjectsDictionary.Keys)
                    {


                        if (!_ioStructureObjectsDictionary[ioBlockHeight].IsDeleted)
                        {
                            if (!_ioStructureObjectsDictionary[ioBlockHeight].IsNull)
                            {
                                // List updated blocks to remove of the memory allocated to the cache system.
                                if (_ioStructureObjectsDictionary[ioBlockHeight].IsUpdated)
                                {
                                    bool getOutBlock =
                                        (_ioStructureObjectsDictionary[ioBlockHeight].LastUpdateTimestamp + _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskMaxKeepAliveDataInMemoryTimeLimit < timestamp
                                        &&
                                        _ioStructureObjectsDictionary[ioBlockHeight].LastGetTimestamp + _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskMaxKeepAliveDataInMemoryTimeLimit < timestamp)
                                        || enableAskMemory;

                                    if (getOutBlock)
                                    {
                                        if (enableAskMemory)
                                            totalAvailableMemoryRetrieved += _ioStructureObjectsDictionary[ioBlockHeight].IoDataSizeOnMemory;

                                        listIoData.Add(ioBlockHeight, _ioStructureObjectsDictionary[ioBlockHeight].BlockObject);
                                    }
                                }
                                // Directly clean up block(s) not updated.
                                else
                                {
                                    if (enableAskMemory)
                                        totalAvailableMemoryRetrieved += _ioStructureObjectsDictionary[ioBlockHeight].IoDataSizeOnMemory;

                                    _ioStructureObjectsDictionary[ioBlockHeight].BlockObject = null;
                                }
                            }
                        }
                        else
                        {
                            if (!listIoDataDeleted.Contains(ioBlockHeight))
                                listIoDataDeleted.Add(ioBlockHeight);
                        }

                        if (enableAskMemory)
                        {
                            if (totalMemoryAsked <= totalAvailableMemoryRetrieved)
                                break;
                        }
                    }

                    #endregion

                    if (enableAskMemory && totalMemoryAsked > totalAvailableMemoryRetrieved)
                    {
                        if (_ioCacheSystem.GetIoCacheSystemMemoryConsumption(cancellation, out _) +
                            (totalMemoryAsked - totalAvailableMemoryRetrieved) <= _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxActiveMemoryAllocationFromCache)
                            totalAvailableMemoryRetrieved = totalMemoryAsked;
                    }

                    #region Update the io cache file.

                    long previousLastIoCacheTotalFileSizeWritten = _lastIoCacheTotalFileSizeWritten;

                    bool fullPurge = _lastIoCacheTotalFileSizeWritten > 0 ? ((double)_lastIoCacheTotalFileSizeWritten / _totalIoCacheFileSize) * 100d >= _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskFullPurgeEnablePercentWrite : false;

                    if (fullPurge || listIoData.Count > 0)
                        await ReplaceIoDataListOnIoStreamFile(listIoData.GetList, cancellation, fullPurge);

                    foreach (long blockHeight in listIoData.GetList.Keys)
                    {
                        if (!_ioStructureObjectsDictionary[blockHeight].IsNull)
                            _ioStructureObjectsDictionary[blockHeight].BlockObject = null;
                    }

                    if (fullPurge)
                    {
                        _lastIoCacheTotalFileSizeWritten = 0;
                        ClassUtility.CleanGc();
                    }

                    #endregion

                    #region Delete listed io cache data to delete forever.

                    if (listIoDataDeleted.Count > 0)
                    {
                        foreach (var ioBlockHeight in listIoDataDeleted)
                        {
                            if (_ioStructureObjectsDictionary.ContainsKey(ioBlockHeight))
                            {
                                if (!_ioStructureObjectsDictionary[ioBlockHeight].IsNull)
                                    _ioStructureObjectsDictionary[ioBlockHeight].BlockObject = null;

                                _ioStructureObjectsDictionary.Remove(ioBlockHeight);
                            }
                        }

                        listIoDataDeleted.Clear();
                    }

                    #endregion

                }
            }
            finally
            {

                if (useSemaphore)
                    _ioSemaphoreAccess.Release();

            }

            return totalAvailableMemoryRetrieved;
        }

        /// <summary>
        /// Total Memory usage.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="fromReading"></param>
        /// <param name="cancellationIoCache"></param>
        /// <param name="keepAlive"></param>
        /// <returns></returns>
        private async Task InsertInActiveMemory(ClassBlockObject blockObject, bool keepAlive, bool fromReading, CancellationTokenSource cancellationIoCache)
        {
            bool insertInMemory = false;
            bool cancel = blockObject == null;

            if (!cancel)
            {
                if (keepAlive)
                {
                    if (!_ioStructureObjectsDictionary[blockObject.BlockHeight].IsNull)
                    {
                        _ioStructureObjectsDictionary[blockObject.BlockHeight].BlockObject = blockObject;
                        insertInMemory = true;
                    }
                    else
                    {
                        long blockMemorySize = _ioStructureObjectsDictionary[blockObject.BlockHeight].IoDataSizeOnMemory == 0 ? ClassBlockUtility.GetIoBlockSizeOnMemory(blockObject) : _ioStructureObjectsDictionary[blockObject.BlockHeight].IoDataSizeOnMemory;

                        _ioStructureObjectsDictionary[blockObject.BlockHeight].IoDataSizeOnMemory = blockMemorySize;

                        if (blockMemorySize <= _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxActiveMemoryAllocationFromCache)
                        {

                            long totalMemoryConsumption = _ioCacheSystem.GetIoCacheSystemMemoryConsumption(cancellationIoCache, out _);

                            // Try to push the block updated in memory.
                            if (totalMemoryConsumption + blockMemorySize <= _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxActiveMemoryAllocationFromCache)
                            {
                                _ioStructureObjectsDictionary[blockObject.BlockHeight].BlockObject = blockObject;
                                _ioStructureObjectsDictionary[blockObject.BlockHeight].IoDataSizeOnMemory = blockMemorySize;
                                insertInMemory = true;
                            }
                            else
                            {
                                // Do an internal purge of the active memory on this io cache file.
                                long totalMemoryRetrieved = await PurgeIoBlockDataMemory(true, cancellationIoCache, blockMemorySize, true);

                                if (totalMemoryRetrieved >= blockMemorySize)
                                {
                                    _ioStructureObjectsDictionary[blockObject.BlockHeight].BlockObject = blockObject;
                                    _ioStructureObjectsDictionary[blockObject.BlockHeight].IoDataSizeOnMemory = blockMemorySize;
                                    insertInMemory = true;
                                }
                                else if (_ioCacheSystem.GetIoCacheSystemMemoryConsumption(cancellationIoCache, out _) + (blockMemorySize - totalMemoryRetrieved) <= _blockchainDatabaseSetting.BlockchainCacheSetting.GlobalMaxActiveMemoryAllocationFromCache)
                                {
                                    _ioStructureObjectsDictionary[blockObject.BlockHeight].BlockObject = blockObject;
                                    _ioStructureObjectsDictionary[blockObject.BlockHeight].IoDataSizeOnMemory = blockMemorySize;
                                    insertInMemory = true;
                                }
                                else
                                {
                                    // Do a purge on other io cache files indexed.
                                    if (await _ioCacheSystem.DoPurgeFromIoCacheIndex(_ioDataStructureFilename, blockMemorySize, cancellationIoCache))
                                    {
                                        _ioStructureObjectsDictionary[blockObject.BlockHeight].BlockObject = blockObject;
                                        _ioStructureObjectsDictionary[blockObject.BlockHeight].IoDataSizeOnMemory = blockMemorySize;
                                        insertInMemory = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (!insertInMemory && !fromReading)
                {
                    if (!_ioStructureObjectsDictionary[blockObject.BlockHeight].IsNull)
                        _ioStructureObjectsDictionary[blockObject.BlockHeight].BlockObject = blockObject;
                    else
                    {
                        if (blockObject.BlockIsUpdated)
                            await WriteNewIoDataOnIoStreamFile(blockObject, cancellationIoCache);
                    }
                }
            }
        }

        /// <summary>
        /// Return the memory usage of active memory from the io cache file index.
        /// </summary>
        /// <returns></returns>
        public long GetIoMemoryUsage(CancellationTokenSource cancellation, out int totalBlockKeepAlive)
        {
            long memoryUsage = 0;
            totalBlockKeepAlive = 0;

            foreach (long blockHeight in _ioStructureObjectsDictionary.Keys.ToArray())
            {
                if (_ioStructureObjectsDictionary[blockHeight].IsNull)
                    continue;

                memoryUsage += _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnMemory;
                totalBlockKeepAlive++;
            }

            return memoryUsage;
        }

        #endregion

        #region Read IO Data functions.

        /// <summary>
        /// Retrieve back a io block data target by a block height cached on a io stream file.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <param name="blockInformationsOnly"></param>
        /// <returns></returns>
        private async Task<ClassBlockObject> ReadAndGetIoBlockDataFromBlockHeightOnIoStreamFile(long blockHeight, CancellationTokenSource cancellation, bool blockInformationsOnly)
        {

            await WaitIoWriteDone(cancellation);

            ClassBlockObject blockObject = null;

            // If the data is indicated to be inside the io cache file.
            if (_ioStructureObjectsDictionary[blockHeight].IsWritten)
            {
                long ioDataPosition = _ioStructureObjectsDictionary[blockHeight].IoDataPosition;
                long ioDataSize = _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile;

                // Read by seek and position registered directly.
                _ioDataStructureFileLockStream.Position = ioDataPosition;

                Tuple<bool, byte[]> readResult = await ReadByBlock(ioDataSize, cancellation, _ioDataStructureFileLockStream);

                if (readResult.Item1)
                {
                    string ioDataLine = _ioDataUtf8Encoding.GetString(readResult.Item2);

                    if (BlockHeightMatchToIoDataLine(blockHeight, ioDataLine))
                        IoStringDataLineToBlockObject(ioDataLine, _blockchainDatabaseSetting, blockInformationsOnly, cancellation, out blockObject);
                }
            }

            return blockObject;
        }

        /// <summary>
        /// Retrieve back a io block data target by a block height cached on a io stream file.
        /// </summary>
        /// <param name="listBlockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<List<ClassBlockObject>> ReadAndGetIoBlockDataFromListBlockHeightOnIoStreamFile(HashSet<long> listBlockHeight, CancellationTokenSource cancellation)
        {
            await WaitIoWriteDone(cancellation);

            List<ClassBlockObject> listBlockObject = new List<ClassBlockObject>();

            HashSet<long> listBlockHeightFound = new HashSet<long>();

            // Direct research.
            foreach (long blockHeight in listBlockHeight)
            {


                if (!_ioStructureObjectsDictionary[blockHeight].IsDeleted)
                {
                    if (_ioStructureObjectsDictionary[blockHeight].IsNull)
                    {
                        // If the data is indicated to be inside the io cache file.
                        if (_ioStructureObjectsDictionary[blockHeight].IsWritten)
                        {

                            // Read by seek and position registered directly.
                            long ioDataPosition = _ioStructureObjectsDictionary[blockHeight].IoDataPosition;
                            long ioDataSize = _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile;

                            // Read by seek and position registered directly.
                            _ioDataStructureFileLockStream.Position = ioDataPosition;

                            var readResult = await ReadByBlock(ioDataSize, cancellation, _ioDataStructureFileLockStream);

                            if (readResult.Item1)
                            {
                                var ioDataLine = _ioDataUtf8Encoding.GetString(readResult.Item2);

                                if (BlockHeightMatchToIoDataLine(blockHeight, ioDataLine))
                                {
                                    if (IoStringDataLineToBlockObject(ioDataLine, _blockchainDatabaseSetting, false, cancellation, out ClassBlockObject blockObject))
                                    {
                                        listBlockHeightFound.Add(blockHeight);
                                        listBlockObject.Add(blockObject);
                                    }
                                }

                                // Clean up.
                                ioDataLine.Clear();
                            }
                            else
                                break;
                        }
                    }
                    else
                    {
                        listBlockObject.Add(_ioStructureObjectsDictionary[blockHeight].BlockObject);
                        listBlockHeightFound.Add(blockHeight);
                    }
                }
            }

            if (listBlockHeightFound.Count < listBlockHeight.Count)
            {
                int blockLeft = listBlockHeight.Count - listBlockHeightFound.Count;
                ClassLog.WriteLine("Cache IO Index Object - Retrieve back " + listBlockHeight.Count + " failed. " + blockLeft + " block(s) not retrieved.", ClassEnumLogLevelType.LOG_LEVEL_MEMORY_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
            }

            // Clean up.
            listBlockHeightFound.Clear();

            return listBlockObject;
        }

        /// <summary>
        /// Read data by block.
        /// </summary>
        /// <param name="ioFileSize"></param>
        /// <param name="cancellation"></param>
        /// <param name="ioFileStream"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, byte[]>> ReadByBlock(long ioFileSize, CancellationTokenSource cancellation, FileStream ioFileStream)
        {
            bool readStatus = true;
            byte[] data = new byte[ioFileSize];

            using (DisposableList<byte[]> listData = new DisposableList<byte[]>())
            {

                long size = 0;
                int percentRead = _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskMinPercentReadFromBlockDataSize;
                long sizeByBlockToRead = (ioFileSize * percentRead) / 100;

                if (sizeByBlockToRead <= 0)
                    sizeByBlockToRead = _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskMinReadByBlockSize;

                if (readStatus && ioFileStream.CanRead)
                {

                    while (size < data.Length)
                    {
                        try
                        {
                            if (cancellation != null)
                            {
                                if (cancellation.IsCancellationRequested)
                                {
                                    readStatus = false;
                                    break;
                                }
                            }
                            if (size + sizeByBlockToRead <= data.Length)
                            {
                                byte[] blockData = new byte[sizeByBlockToRead];

                                if (cancellation == null)
                                   await ioFileStream.ReadAsync(blockData, 0, blockData.Length);
                                else
                                   await ioFileStream.ReadAsync(blockData, 0, blockData.Length, cancellation.Token);

                                listData.Add(blockData);

                                size += sizeByBlockToRead;
                            }
                            else
                            {
                                long rest = data.Length - size;

                                if (rest > 0)
                                {
                                    byte[] blockData = new byte[rest];

                                    if (cancellation == null)
                                        await ioFileStream.ReadAsync(blockData, 0, blockData.Length);
                                    else
                                        await ioFileStream.ReadAsync(blockData, 0, blockData.Length, cancellation.Token);

                                    listData.Add(blockData);

                                    size += rest;
                                }
                            }

                            if (size == data.Length)
                                break;
                        }
                        catch
                        {
                            readStatus = false;
                            break;
                        }
                    }

                }

                return new Tuple<bool, byte[]>(readStatus, listData.GetList.SelectMany(x => x).ToArray());
            }
        }


        #endregion

        #region Write IO Data functions.

        /// <summary>
        /// Write directly a io data on the io cache file.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> WriteNewIoDataOnIoStreamFile(ClassBlockObject blockObject, CancellationTokenSource cancellation)
        {
            bool success = false;


            await WaitIoWriteDone(cancellation);

            _ioOnWriteData = true;

            try
            {
                if (blockObject != null)
                {
                    _ioDataStructureFileLockStream.Position = _lastIoCacheFileLength;

                    long position = _lastIoCacheFileLength;

                    long dataLength = 0;

                    bool cancelled = false;

                    foreach (string ioDataLine in BlockObjectToIoStringData(blockObject, _blockchainDatabaseSetting, cancellation))
                    {
                        if (cancellation != null)
                        {
                            if (cancellation.IsCancellationRequested)
                            {
                                cancelled = true;
                                break;
                            }
                        }

                        var writeResult = await WriteByBlock(_ioDataUtf8Encoding.GetBytes(ioDataLine), cancellation, _ioDataStructureFileLockStream);

                        if (writeResult.Item1)
                            dataLength += writeResult.Item2;
                        else
                        {
                            cancelled = true;
                            break;
                        }
                    }

                    if (!cancelled)
                    {
                        _ioDataStructureFileLockStream.WriteByte((byte)'\r');
                        _ioDataStructureFileLockStream.WriteByte((byte)'\n');

                        if (!_ioStructureObjectsDictionary[blockObject.BlockHeight].IsWritten)
                            _totalIoCacheFileSize += dataLength;
                        else
                        {
                            _totalIoCacheFileSize -= _ioStructureObjectsDictionary[blockObject.BlockHeight].IoDataSizeOnFile;
                            _totalIoCacheFileSize += dataLength;
                        }

                        _ioStructureObjectsDictionary[blockObject.BlockHeight].IsWritten = true;
                        _ioStructureObjectsDictionary[blockObject.BlockHeight].IoDataSizeOnFile = dataLength;
                        _ioStructureObjectsDictionary[blockObject.BlockHeight].IoDataPosition = position;
                        _lastIoCacheTotalFileSizeWritten += dataLength;

                        _lastIoCacheFileLength = position + dataLength + 2;
                        success = true;
                    }
                }

                _ioOnWriteData = false;
            }
            catch
            {
                _ioOnWriteData = false;
            }

            return success;
        }

        /// <summary>
        /// Write directly a io data on the io cache file.
        /// </summary>
        /// <param name="blockListObject"></param>
        /// <param name="cancellation"></param>
        /// <param name="fullPurge"></param>
        /// <returns></returns>
        private async Task ReplaceIoDataListOnIoStreamFile(Dictionary<long, ClassBlockObject> blockListObject, CancellationTokenSource cancellation, bool fullPurge)
        {

            await WaitIoWriteDone(cancellation);

            _ioOnWriteData = true;

            try
            {
                await ResetSeekLockStreamAsync(cancellation);

                if (fullPurge)
                {
                    string ioDataStructureFileBackupPath = _ioDataStructureFilePath + "copy";

                    bool cancelled = false;

                    using (FileStream writer = new FileStream(ioDataStructureFileBackupPath, FileMode.Create, FileAccess.Write, FileShare.Read, _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskWriteStreamBufferSize))
                    {
                        // Rewrite updated blocks from memory.
                        foreach (long blockHeight in _ioStructureObjectsDictionary.Keys.ToArray())
                        {
                            if (!blockListObject.ContainsKey(blockHeight))
                            {
                                if (cancellation != null)
                                {
                                    if (cancellation.IsCancellationRequested)
                                    {
                                        cancelled = true;
                                        break;
                                    }
                                }

                                if (!_ioStructureObjectsDictionary[blockHeight].IsDeleted)
                                {
                                    // If the data is indicated to be inside the io cache file.
                                    if (_ioStructureObjectsDictionary[blockHeight].IsWritten)
                                    {
                                        if (cancellation != null)
                                        {
                                            if (cancellation.IsCancellationRequested)
                                            {
                                                cancelled = true;
                                                break;
                                            }
                                        }

                                        // Read by seek and position registered directly.
                                        long ioDataPosition = _ioStructureObjectsDictionary[blockHeight].IoDataPosition;
                                        long ioDataSize = _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile;

                                        // Read by seek and position registered directly.
                                        _ioDataStructureFileLockStream.Position = ioDataPosition;

                                        var readResult = await ReadByBlock(ioDataSize, cancellation, _ioDataStructureFileLockStream);

                                        if (readResult.Item1)
                                        {
                                            string ioDataLine = _ioDataUtf8Encoding.GetString(readResult.Item2);

                                            if (BlockHeightMatchToIoDataLine(blockHeight, ioDataLine))
                                            {
                                                long position = writer.Position;

                                                var writeResult = await WriteByBlock(readResult.Item2, cancellation, writer);

                                                if (writeResult.Item1)
                                                {

                                                    writer.WriteByte((byte)'\r');
                                                    writer.WriteByte((byte)'\n');

                                                    if (!_ioStructureObjectsDictionary[blockHeight].IsWritten)
                                                        _totalIoCacheFileSize += writeResult.Item2;
                                                    else
                                                    {
                                                        _totalIoCacheFileSize -= _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile;
                                                        _totalIoCacheFileSize += writeResult.Item2;
                                                    }

                                                    if (!_ioStructureObjectsDictionary[blockHeight].IsNull)
                                                        _ioStructureObjectsDictionary[blockHeight].BlockObject = null;

                                                    _ioStructureObjectsDictionary[blockHeight].IoDataPosition = position;
                                                    _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile = writeResult.Item2;
                                                    _ioStructureObjectsDictionary[blockHeight].IsWritten = true;

                                                    _lastIoCacheFileLength = position + writeResult.Item2 + 2;
                                                }
                                                else
                                                {
                                                    cancelled = true;
                                                    break;
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (blockListObject.Count > 0 && !cancelled)
                        {
                            foreach (long blockHeight in blockListObject.Keys.ToArray())
                            {
                                if (cancellation != null)
                                {
                                    if (cancellation.IsCancellationRequested)
                                    {
                                        cancelled = true;
                                        break;
                                    }
                                }

                                long position = writer.Position;
                                long dataLength = 0;

                                foreach (string ioDataLine in BlockObjectToIoStringData(blockListObject[blockHeight], _blockchainDatabaseSetting, cancellation))
                                {
                                    if (cancellation != null)
                                    {
                                        if (cancellation.IsCancellationRequested)
                                        {
                                            cancelled = true;
                                            break;
                                        }
                                    }

                                    Tuple<bool, long> writeResult = await WriteByBlock(_ioDataUtf8Encoding.GetBytes(ioDataLine), cancellation, writer);

                                    if (writeResult.Item1)
                                        dataLength += writeResult.Item2;
                                    else
                                    {
                                        cancelled = true;
                                        break;
                                    }
                                }

                                if (!cancelled)
                                {
                                    writer.WriteByte((byte)'\r');
                                    writer.WriteByte((byte)'\n');

                                    if (!_ioStructureObjectsDictionary[blockHeight].IsWritten)
                                        _totalIoCacheFileSize += dataLength;
                                    else
                                    {
                                        _totalIoCacheFileSize -= _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile;
                                        _totalIoCacheFileSize += dataLength;
                                    }

                                    if (!_ioStructureObjectsDictionary[blockHeight].IsNull)
                                        _ioStructureObjectsDictionary[blockHeight].BlockObject = null;

                                    _ioStructureObjectsDictionary[blockHeight].IoDataPosition = position;
                                    _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile = dataLength;
                                    _ioStructureObjectsDictionary[blockHeight].IsWritten = true;
                                    _lastIoCacheFileLength = position + dataLength + 2;

                                    // Clean up.
                                    blockListObject.Remove(blockHeight);
                                }
                                else
                                    break;
                            }
                        }
                    }

                    if (!cancelled)
                    {
                        CloseLockStream();

                        File.Delete(_ioDataStructureFilePath);
                        File.Move(ioDataStructureFileBackupPath, _ioDataStructureFilePath);

                        OpenLockStream();
                    }
                    else
                        File.Delete(ioDataStructureFileBackupPath);
                }
                else
                {
                    if (blockListObject.Count > 0)
                    {
                        _ioDataStructureFileLockStream.Position = _lastIoCacheFileLength;

                        foreach (long blockHeight in blockListObject.Keys.ToArray())
                        {


                            long position = _ioDataStructureFileLockStream.Position;
                            long dataLength = 0;

                            bool cancelled = false;


                            foreach (string ioDataLine in BlockObjectToIoStringData(blockListObject[blockHeight], _blockchainDatabaseSetting, cancellation))
                            {
                                if (cancellation != null)
                                {
                                    if (cancellation.IsCancellationRequested)
                                    {
                                        cancelled = true;
                                        break;
                                    }
                                }

                                var writeResult = await WriteByBlock(_ioDataUtf8Encoding.GetBytes(ioDataLine), cancellation, _ioDataStructureFileLockStream);

                                if (writeResult.Item1)
                                    dataLength += writeResult.Item2;
                                else
                                {
                                    cancelled = true;
                                    break;
                                }
                            }

                            if (!cancelled)
                            {

                                _ioDataStructureFileLockStream.WriteByte((byte)'\r');
                                _ioDataStructureFileLockStream.WriteByte((byte)'\n');

                                if (!_ioStructureObjectsDictionary[blockHeight].IsWritten)
                                    _totalIoCacheFileSize += dataLength;
                                else
                                {
                                    _totalIoCacheFileSize -= _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile;
                                    _totalIoCacheFileSize += dataLength;
                                }

                                _ioStructureObjectsDictionary[blockHeight].IoDataPosition = position;
                                _ioStructureObjectsDictionary[blockHeight].IoDataSizeOnFile = dataLength;
                                _ioStructureObjectsDictionary[blockHeight].IsWritten = true;
                                _lastIoCacheTotalFileSizeWritten += dataLength;

                                _lastIoCacheFileLength = position + dataLength + 2;

                                // Clean up.
                                blockListObject.Remove(blockHeight);
                            }
                            else
                                break;
                        }

                        await _ioDataStructureFileLockStream.FlushAsync(cancellation.Token);
                    }
                }

                _ioOnWriteData = false;
            }
            catch
            {
                _ioOnWriteData = false;
                OpenLockStream();
            }

        }

        /// <summary>
        /// Write data by block.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cancellation"></param>
        /// <param name="ioFileStream"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, long>> WriteByBlock(byte[] data, CancellationTokenSource cancellation, FileStream ioFileStream)
        {
            bool writeStatus = _ioDataStructureFileLockStream.CanWrite;
            long dataSizeWritten = 0;

            int percentWrite = _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskMinPercentWriteFromBlockDataSize;
            long sizeByBlockToWrite = (data.Length * percentWrite) / 100;

            if (sizeByBlockToWrite <= 0)
                sizeByBlockToWrite = _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskMinWriteByBlockSize;

            if (writeStatus)
            {
                long tmpWriteBeforeSleep = 0;

                while (dataSizeWritten != data.Length && writeStatus)
                {
                    try
                    {
                        if (cancellation != null)
                        {
                            if (cancellation.IsCancellationRequested)
                            {
                                writeStatus = false;
                                break;
                            }
                        }

                        if (dataSizeWritten + sizeByBlockToWrite < data.Length)
                        {
                            byte[] blockData = new byte[sizeByBlockToWrite];

                            Array.Copy(data, dataSizeWritten, blockData, 0, blockData.Length);

                            await ioFileStream.WriteAsync(blockData, 0, blockData.Length, cancellation.Token);

                            dataSizeWritten += sizeByBlockToWrite;
                            tmpWriteBeforeSleep += sizeByBlockToWrite;
                        }
                        else
                        {
                            long rest = data.Length - dataSizeWritten;

                            if (rest > 0)
                            {
                                byte[] blockData = new byte[rest];

                                Array.Copy(data, dataSizeWritten, blockData, 0, blockData.Length);

                                await ioFileStream.WriteAsync(blockData, 0, blockData.Length, cancellation.Token);

                                dataSizeWritten += rest;
                                tmpWriteBeforeSleep += rest;
                            }
                        }

                        if (dataSizeWritten >= data.Length)
                            break;
                    }
                    catch
                    {
                        writeStatus = false;
                        break;
                    }
                }

            }

            return new Tuple<bool, long>(writeStatus, dataSizeWritten);
        }

        #endregion

        #region Manage stream functions.

        /// <summary>
        /// Close the lock stream of the io file cache.
        /// </summary>
        public void CloseLockStream()
        {
            if (!_ioStreamsClosed)
            {
                _ioDataStructureFileLockStream.Close();
                _ioStreamsClosed = true;
            }
        }

        /// <summary>
        /// Open a lock stream to the io file cache.
        /// </summary>
        private void OpenLockStream()
        {
            if (_ioStreamsClosed)
            {
                _ioDataStructureFileLockStream = new FileStream(_ioDataStructureFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, _blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskReadStreamBufferSize, FileOptions.Asynchronous | FileOptions.RandomAccess);
                _ioStreamsClosed = false;
            }
        }

        /// <summary>
        /// Reset the seek of the lock stream of the io file cache.
        /// </summary>
        private async Task ResetSeekLockStreamAsync(CancellationTokenSource cancellation)
        {
            if (_ioStreamsClosed)
                OpenLockStream();

            _ioDataStructureFileLockStream.Position = 0;

            if (_ioDataStructureFileLockStream.Position != 0)
            {
                while (_ioDataStructureFileLockStream.Position != 0)
                {
                    if (cancellation != null)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        try
                        {
                            await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay, cancellation.Token);
                        }
                        catch
                        {
                            break;
                        }
                    }
                    else
                        await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay);

                    if (_ioDataStructureFileLockStream.Position == 0)
                        break;

                    _ioDataStructureFileLockStream.Position = 0;
                }
            }
        }

        /// <summary>
        /// Wait a io write done.
        /// </summary>
        /// <returns></returns>
        private async Task WaitIoWriteDone(CancellationTokenSource cancellation)
        {
            if (_ioOnWriteData)
            {
                while (_ioOnWriteData)
                {
                    if (cancellation != null)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        try
                        {
                            await Task.Delay(_blockchainDatabaseSetting.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay, cancellation.Token);
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

        #endregion

    }
}
