using Newtonsoft.Json;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Desktop_Wallet.Sync.Object;
using SeguraChain_Desktop_Wallet.Wallet.Function;
using SeguraChain_Desktop_Wallet.Wallet.Object;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace SeguraChain_Desktop_Wallet.Wallet.Database
{
    public class ClassWalletDatabase
    {
        /// <summary>
        /// Global data.
        /// </summary>
        public ConcurrentDictionary<string, ClassWalletDataObject> DictionaryWalletData = new ConcurrentDictionary<string, ClassWalletDataObject>(); // Wallet filename | Wallet data.
        public HashSet<string> ListWalletFile = new HashSet<string>();

        /// <summary>
        /// Internal data of the class.
        /// </summary>
        private SemaphoreSlim _semaphoreLoadWalletFile;
        private SemaphoreSlim _semaphoreSaveWalletFile;
        private SemaphoreSlim _semaphoreGetWalletFileData;
        private CancellationTokenSource _cancellationTokenTaskWallet;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassWalletDatabase()
        {
            _semaphoreLoadWalletFile = new SemaphoreSlim(1, 1);
            _semaphoreSaveWalletFile = new SemaphoreSlim(1, 1);
            _semaphoreGetWalletFileData = new SemaphoreSlim(1, 1);
            _cancellationTokenTaskWallet = new CancellationTokenSource();
            if (!Directory.Exists(ClassDesktopWalletCommonData.WalletSettingObject.WalletDirectoryPath))
                Directory.CreateDirectory(ClassDesktopWalletCommonData.WalletSettingObject.WalletDirectoryPath);
        }

        #region Manage wallet database.

        /// <summary>
        /// Load a wallet file data.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// <returns></returns>
        public async Task<ClassWalletLoadFileEnumResult> LoadWalletFileAsync(string walletFileName, CancellationTokenSource cancellation)
        {
            if (DictionaryWalletData.ContainsKey(walletFileName))
                return ClassWalletLoadFileEnumResult.WALLET_LOAD_ALREADY_LOADED_ERROR;

            await _semaphoreLoadWalletFile.WaitAsync();

            ClassWalletLoadFileEnumResult resultLoad;

            string walletFilePath = ClassUtility.ConvertPath(ClassDesktopWalletCommonData.WalletSettingObject.WalletDirectoryPath + walletFileName);

            if (File.Exists(walletFilePath))
            {
                string walletData;

                using (FileStream walletFileStream = new FileStream(walletFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader walletFileStreamReader = new StreamReader(walletFileStream))
                        walletData = walletFileStreamReader.ReadToEnd();
                }

                if (ClassUtility.TryDeserialize(walletData, out ClassWalletDataObject walletDataObjectLoaded))
                {
                    if (walletDataObjectLoaded != null)
                    {
                        resultLoad = ClassWalletDataFunction.InitializeWalletDataObjectToLoad(walletDataObjectLoaded, out walletDataObjectLoaded);

                        if (resultLoad == ClassWalletLoadFileEnumResult.WALLET_LOAD_SUCCESS)
                        {
                            if (DictionaryWalletData.ContainsKey(walletFileName))
                                resultLoad = ClassWalletLoadFileEnumResult.WALLET_LOAD_ALREADY_LOADED_ERROR;
                            else
                            {
                                if (!GetWalletFileNameFromWalletAddress(walletDataObjectLoaded.WalletAddress).IsNullOrEmpty(false, out _))
                                    resultLoad = ClassWalletLoadFileEnumResult.WALLET_LOAD_ALREADY_LOADED_ERROR;
                                else
                                {
                                    if (!DictionaryWalletData.TryAdd(walletFileName, walletDataObjectLoaded))
                                        resultLoad = ClassWalletLoadFileEnumResult.WALLET_LOAD_ALREADY_LOADED_ERROR;
                                    else
                                    {
                                        bool semaphoreUsed = false;

                                        try
                                        {
                                            await _semaphoreGetWalletFileData.WaitAsync();

                                            semaphoreUsed = true;

                                            StopUpdateTaskWallet();

                                            DictionaryWalletData[walletFileName].WalletFileName = walletFileName;

                                            // Lock the wallet file.
                                            DictionaryWalletData[walletFileName].WalletFileStream = new FileStream(walletFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                                            long lastBlockHeightSynced = await ClassDesktopWalletCommonData.WalletSyncSystem.GetLastBlockHeightSynced(cancellation, true);

                                            if (lastBlockHeightSynced > 0 && DictionaryWalletData[walletFileName].WalletLastBlockHeightSynced > lastBlockHeightSynced)
                                            {
                                                DictionaryWalletData[walletFileName].WalletEnableRescan = true;
                                                ResetWalletSync(walletFileName, cancellation);
                                            }
                                            else
                                            {
                                                if (!ClassDesktopWalletCommonData.WalletSyncSystem.DatabaseSyncCache.ContainsKey(DictionaryWalletData[walletFileName].WalletAddress))
                                                    ClassDesktopWalletCommonData.WalletSyncSystem.DatabaseSyncCache.TryAdd(DictionaryWalletData[walletFileName].WalletAddress, new ClassSyncCacheObject());

                                                if (!DictionaryWalletData[walletFileName].WalletEnableRescan)
                                                {

                                                    if (DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Count > 0)
                                                        DictionaryWalletData[walletFileName].WalletTotalMemPoolTransaction = DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Count;

                                                    if (DictionaryWalletData[walletFileName].WalletTransactionList.Count > 0)
                                                    {
                                                        foreach (long blockHeight in DictionaryWalletData[walletFileName].WalletTransactionList.Keys)
                                                            DictionaryWalletData[walletFileName].WalletTotalTransaction += DictionaryWalletData[walletFileName].WalletTransactionList[blockHeight].Count;
                                                    }

                                                    long totalTransactions = DictionaryWalletData[walletFileName].WalletTotalTransaction + DictionaryWalletData[walletFileName].WalletTotalMemPoolTransaction;

                                                    if (ClassDesktopWalletCommonData.WalletSyncSystem.DatabaseSyncCache[DictionaryWalletData[walletFileName].WalletAddress].TotalTransactions < totalTransactions)
                                                    {
                                                        DictionaryWalletData[walletFileName].WalletEnableRescan = true;
                                                        ResetWalletSync(walletFileName, cancellation);
                                                    }
                                                }
                                            }

                                            _cancellationTokenTaskWallet = new CancellationTokenSource();
                                            EnableTaskUpdateWalletSync();
                                            EnableTaskUpdateWalletFileList();
                                        }
                                        finally
                                        {
                                            if (semaphoreUsed)
                                                _semaphoreGetWalletFileData.Release();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                        resultLoad = ClassWalletLoadFileEnumResult.WALLET_LOAD_EMPTY_FILE_DATA_ERROR;
                }
                else
                    resultLoad = ClassWalletLoadFileEnumResult.WALLET_LOAD_BAD_FILE_FORMAT_ERROR;
            }
            else
                resultLoad = ClassWalletLoadFileEnumResult.WALLET_LOAD_FILE_NOT_FOUND_ERROR;

            _semaphoreLoadWalletFile.Release();

            return resultLoad;
        }

        /// <summary>
        /// Save a wallet data to his file target.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// <returns></returns>
        public async Task<bool> SaveWalletFileAsync(string walletFileName)
        {
            bool useSemaphore = false;
            bool result = false;

            try
            {
                await _semaphoreSaveWalletFile.WaitAsync();
                useSemaphore = true;

                try
                {
                    if (DictionaryWalletData.ContainsKey(walletFileName))
                    {
                        string walletFilePath = ClassUtility.ConvertPath(ClassDesktopWalletCommonData.WalletSettingObject.WalletDirectoryPath + walletFileName);

                        // Close the lock stream.
                        if (DictionaryWalletData[walletFileName].WalletFileStream != null)
                            DictionaryWalletData[walletFileName].WalletFileStream.Close();

                        FileMode fileMode = FileMode.Truncate;

                        if (!File.Exists(walletFilePath))
                            fileMode = FileMode.Create;

                        using (FileStream walletFileStream = new FileStream(walletFilePath, fileMode, FileAccess.Write, FileShare.Read))
                        {
                            using (StreamWriter walletFileStreamReader = new StreamWriter(walletFileStream) { AutoFlush = true })
                                walletFileStreamReader.Write(ClassUtility.SerializeData(DictionaryWalletData[walletFileName], Formatting.Indented));
                        }

                        // Lock again the wallet file.
                        DictionaryWalletData[walletFileName].WalletFileStream = new FileStream(walletFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);


                        result = true;
                    }
                }
                catch (Exception error)
                {
#if DEBUG
                    Debug.WriteLine("Error on saving the wallet file " + walletFileName + ". Exception: " + error.Message);
#endif
                    ClassLog.WriteLine("Error on saving the wallet file " + walletFileName + ". Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                }

            }
            finally
            {
                if (useSemaphore)
                    _semaphoreSaveWalletFile.Release();
            }
            return result;

        }

        /// <summary>
        /// Close and save wallet file.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// <returns></returns>
        public async Task<bool> CloseAndSaveWalletFileAsync(string walletFileName)
        {
            bool result = false;

            bool semaphoreUsed = false;

            try
            {
                StopUpdateTaskWallet();

                await _semaphoreGetWalletFileData.WaitAsync();

                semaphoreUsed = true;


                try
                {
                    if (DictionaryWalletData.ContainsKey(walletFileName))
                    {
                        if (await SaveWalletFileAsync(walletFileName))
                        {

                            if (DictionaryWalletData[walletFileName].WalletFileStream != null)
                                DictionaryWalletData[walletFileName].WalletFileStream.Close();

                            if (ClassDesktopWalletCommonData.WalletSettingObject.WalletSyncMode == ClassWalletSettingEnumSyncMode.INTERNAL_PEER_SYNC_MODE)
                                ClassBlockchainDatabase.BlockchainMemoryManagement.RemoveWalletAddressReservedToBlockTransactionCache(DictionaryWalletData[walletFileName].WalletAddress);

                            if (DictionaryWalletData.TryRemove(walletFileName, out _))
                                result = true;
                        }
                    }
                }
                catch (Exception error)
                {
#if DEBUG
                    Debug.WriteLine("Error on closing the wallet file " + walletFileName + ". Exception: " + error.Message);
#endif
                    ClassLog.WriteLine("Error on closing the wallet file " + walletFileName + ". Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                }


                _semaphoreGetWalletFileData.Release();
                semaphoreUsed = false;

                _cancellationTokenTaskWallet = new CancellationTokenSource();
                EnableTaskUpdateWalletSync();
                EnableTaskUpdateWalletFileList();
            }
            finally
            {
                if (semaphoreUsed)
                    _semaphoreGetWalletFileData.Release();
            }


            return result;
        }

        /// <summary>
        /// Get the amount of wallet file.
        /// </summary>
        public int GetCountWalletFile => Directory.GetFiles(ClassDesktopWalletCommonData.WalletSettingObject.WalletDirectoryPath, ClassWalletDefaultSetting.WalletFileFormat).Length;

        /// <summary>
        /// Get the wallet file list.
        /// </summary>
        public string[] GetWalletFileList
        {
            get
            {
                string[] walletFileList = Directory.GetFiles(ClassDesktopWalletCommonData.WalletSettingObject.WalletDirectoryPath, ClassWalletDefaultSetting.WalletFileFormat);
                for (int i = 0; i < walletFileList.Length; i++)
                    walletFileList[i] = Path.GetFileName(walletFileList[i]);

                return walletFileList;
            }
        }

        /// <summary>
        /// Get a wallet file opened data.
        /// </summary>
        /// <param name="walletFileOpened"></param>
        /// <returns></returns>
        public ClassWalletDataObject GetWalletFileOpenedData(string walletFileOpened)
        {
            if (walletFileOpened.IsNullOrEmpty(false, out _))
                return null;

            return DictionaryWalletData.ContainsKey(walletFileOpened) ? DictionaryWalletData[walletFileOpened] : null;
        }

        /// <summary>
        /// Get the wallet filename linked to a wallet address.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <returns></returns>
        public string GetWalletFileNameFromWalletAddress(string walletAddress)
        {
            if (DictionaryWalletData.Count > 0)
            {
                using (DisposableList<string> walletAddresses = new DisposableList<string>(false, 0, DictionaryWalletData.Keys.ToArray()))
                {
                    foreach (string walletFileName in walletAddresses.GetList)
                    {
                        try
                        {
                            if (DictionaryWalletData[walletFileName].WalletAddress == walletAddress)
                                return walletFileName;
                        }
                        catch
                        {
                            // Ignored.
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get the wallet filename linked to a wallet address.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <returns></returns>
        public string GetWalletAddressFromWalletFileName(string walletFileName)
        {
            return DictionaryWalletData.ContainsKey(walletFileName) ? DictionaryWalletData[walletFileName].WalletAddress : string.Empty;
        }

        /// <summary>
        /// Return the wallet rescan status.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// <returns></returns>
        public bool GetWalletRescanStatus(string walletFileName)
        {
            return DictionaryWalletData.ContainsKey(walletFileName) ? DictionaryWalletData[walletFileName].WalletEnableRescan : false;
        }

        /// <summary>
        /// Check if one of wallets file store is on rescan.
        /// </summary>
        /// <returns></returns>
        public bool WalletOnResync(string walletFileName)
        {
            if (!walletFileName.IsNullOrEmpty(false, out _))
            {
                if (DictionaryWalletData.ContainsKey(walletFileName))
                    return DictionaryWalletData[walletFileName].WalletEnableRescan;
            }

            return false;
        }

        /// <summary>
        /// Reset the wallet sync.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// <param name="cancellation"></param>
        private void ResetWalletSync(string walletFileName, CancellationTokenSource cancellation)
        {

            DictionaryWalletData[walletFileName].WalletTransactionList.Clear();
            DictionaryWalletData[walletFileName].WalletMemPoolTransactionList.Clear();
            DictionaryWalletData[walletFileName].WalletTotalMemPoolTransaction = 0;
            DictionaryWalletData[walletFileName].WalletTotalTransaction = 0;
            DictionaryWalletData[walletFileName].WalletLastBlockHeightSynced = 0;
            ClassDesktopWalletCommonData.WalletSyncSystem.CleanSyncCacheOfWalletAddressTarget(DictionaryWalletData[walletFileName].WalletAddress, cancellation);
            DictionaryWalletData[walletFileName].WalletBalanceCalculated = false;

            if (DictionaryWalletData[walletFileName].WalletBalanceObject != null)
            {
                DictionaryWalletData[walletFileName].WalletBalanceObject.WalletAvailableBalance = ClassTransactionUtility.GetFormattedAmountFromBigInteger(0);
                DictionaryWalletData[walletFileName].WalletBalanceObject.WalletPendingBalance = ClassTransactionUtility.GetFormattedAmountFromBigInteger(0);
                DictionaryWalletData[walletFileName].WalletBalanceObject.WalletTotalBalance = ClassTransactionUtility.GetFormattedAmountFromBigInteger(0);
            }

        }
        
        #endregion

        #region Parallel Tasks.

        /// <summary>
        /// Enable a task who update the list of wallet file.
        /// </summary>
        public void EnableTaskUpdateWalletFileList()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (ClassDesktopWalletCommonData.DesktopWalletStarted)
                    {
                        string[] walletFileList = GetWalletFileList;

                        if (walletFileList.Length > 0)
                        {
                            for (int i = 0; i < walletFileList.Length; i++)
                            {
                                string walletFileName = walletFileList[i];

                                if (!ListWalletFile.Contains(walletFileName))
                                    ListWalletFile.Add(walletFileName);

                                walletFileList[i] = walletFileName;
                            }

                            foreach (var walletFileName in ListWalletFile.ToArray())
                            {
                                bool containFileName = false;

                                foreach (var walletFile in walletFileList)
                                {
                                    if (ListWalletFile.Contains(walletFile))
                                        containFileName = true;
                                }

                                if (!containFileName)
                                    ListWalletFile.Remove(walletFileName);
                            }
                        }

                        await Task.Delay(1000);
                    }
                }, _cancellationTokenTaskWallet.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Enable a task who update each wallet sync data.
        /// </summary>
        public void EnableTaskUpdateWalletSync()
        {
            bool useSemaphore = false;

            try
            {
                Task.Factory.StartNew(async () =>
                {

                    while (ClassDesktopWalletCommonData.DesktopWalletStarted)
                    {

                        if (_cancellationTokenTaskWallet.IsCancellationRequested)
                            break;
                        try
                        {
                            try
                            {


                                await _semaphoreGetWalletFileData.WaitAsync(_cancellationTokenTaskWallet.Token);
                                useSemaphore = true;

                                string[] walletFileOpened = DictionaryWalletData.Keys.ToArray();

                                if (walletFileOpened.Length > 0)
                                {

                                    int countWalletTaskToDo = walletFileOpened.Length;
                                    int countWalletTaskDone = 0;


                                    using (CancellationTokenSource cancellationLinked = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenTaskWallet.Token))
                                    {
                                        foreach (var walletFileName in walletFileOpened)
                                        {
                                            DictionaryWalletData[walletFileName].WalletOnSync = false;

                                            try
                                            {
                                                await Task.Factory.StartNew(async () =>
                                                {

                                                    bool requireSave = await UpdateWalletSyncTarget(walletFileName, cancellationLinked);

                                                    // If changes are done.
                                                    if (requireSave)
                                                    {
                                                        if (await SaveWalletFileAsync(walletFileName))
                                                            ClassLog.WriteLine(walletFileName + " wallet file updated from sync.", ClassEnumLogLevelType.LOG_LEVEL_WALLET, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                                                    }

                                                    countWalletTaskDone++;
                                                }, cancellationLinked.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
                                            }
                                            catch
                                            {
                                                // Catch the exception once the task is cancelled.
                                            }
                                        }

                                        while (countWalletTaskDone < countWalletTaskToDo)
                                        {
                                            if (walletFileOpened.Length != DictionaryWalletData.Count)
                                                break;

                                            if (_cancellationTokenTaskWallet.IsCancellationRequested)
                                                break;

                                            try
                                            {
                                                await Task.Delay(100, _cancellationTokenTaskWallet.Token);
                                            }
                                            catch
                                            {
                                                break;
                                            }
                                        }

                                        foreach (var walletFileName in walletFileOpened)
                                            DictionaryWalletData[walletFileName].WalletOnSync = false;

                                        cancellationLinked.Cancel();
                                    }

                                    // Clean up.
                                    Array.Clear(walletFileOpened, 0, walletFileOpened.Length);

                                }
                            }
                            catch
                            {
                                // Ignored, catch the exception once the task is cancelled.
                            }
                        }
                        finally
                        {
                            if (useSemaphore)
                                _semaphoreGetWalletFileData.Release();

                        }


                        await Task.Delay(ClassWalletDefaultSetting.DefaultWalletUpdateSyncInterval);
                    }
                }, _cancellationTokenTaskWallet.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                if (useSemaphore)
                    _semaphoreGetWalletFileData.Release();

            }
        }

        /// <summary>
        /// Update the wallet sync of a wallet file target.
        /// </summary>
        /// <param name="walletFileName"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> UpdateWalletSyncTarget(string walletFileName, CancellationTokenSource cancellation)
        {
            bool requireSave;


            if (DictionaryWalletData[walletFileName].WalletEnableRescan)
            {
                ResetWalletSync(walletFileName, cancellation);
                await ClassDesktopWalletCommonData.WalletSyncSystem.UpdateWalletSync(walletFileName, _cancellationTokenTaskWallet);

                DictionaryWalletData[walletFileName].WalletEnableRescan = false;
                requireSave = true;
            }
            else
                requireSave = await ClassDesktopWalletCommonData.WalletSyncSystem.UpdateWalletSync(walletFileName, _cancellationTokenTaskWallet);

            if (DictionaryWalletData[walletFileName].WalletEnableRescan)
            {
                ResetWalletSync(walletFileName, cancellation);
                DictionaryWalletData[walletFileName].WalletEnableRescan = false;
                requireSave = true;
            }

            if (requireSave || !DictionaryWalletData[walletFileName].WalletBalanceCalculated)
            {
                ClassWalletBalanceObject walletBalanceObject = ClassDesktopWalletCommonData.WalletSyncSystem.GetWalletBalanceFromSyncedData(walletFileName);

                if (DictionaryWalletData[walletFileName].WalletBalanceObject == null)
                {
                    requireSave = true;
                    DictionaryWalletData[walletFileName].WalletBalanceObject = walletBalanceObject;
                }
                else
                {
                    if (DictionaryWalletData[walletFileName].WalletBalanceObject.WalletAvailableBalance != walletBalanceObject.WalletAvailableBalance ||
                        DictionaryWalletData[walletFileName].WalletBalanceObject.WalletPendingBalance != walletBalanceObject.WalletPendingBalance ||
                        DictionaryWalletData[walletFileName].WalletBalanceObject.WalletTotalBalance != walletBalanceObject.WalletTotalBalance)
                    {
                        DictionaryWalletData[walletFileName].WalletBalanceObject = walletBalanceObject;
                        requireSave = true;
                    }
                }

                DictionaryWalletData[walletFileName].WalletBalanceCalculated = true;
            }

            return requireSave;
        }

        /// <summary>
        /// Stop update task(s) of wallet.
        /// </summary>
        public void StopUpdateTaskWallet()
        {
            try
            {
                if (_cancellationTokenTaskWallet != null)
                {
                    if (!_cancellationTokenTaskWallet.IsCancellationRequested)
                        _cancellationTokenTaskWallet.Cancel();
                }
            }
            catch
            {
                // Ignored.
            }
        }

        #endregion

    }
}
