using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Object;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Utility;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_RPC_Wallet.Config;
using SeguraChain_RPC_Wallet.Database;
using SeguraChain_RPC_Wallet.Database.Wallet;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_RPC_Wallet.RpcTask
{

    /// <summary>
    /// Can proceed task registered for later.
    /// </summary>
    public class ClassRpcTaskSystem
    {
        private const int RpcConfigUpdateWalletInterval = 10 * 1000;
        private const int RpcConfigAutoSaveWalletInterval = 30 * 1000;
        private ClassWalletDatabase _walletDatabase;
        private CancellationTokenSource _cancellation;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="walletDatabase"></param>
        public ClassRpcTaskSystem(ClassWalletDatabase walletDatabase)
        {
            _walletDatabase = walletDatabase;
            _cancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// Enable the auto update of wallet data.
        /// </summary>
        /// <param name="rpcConfig"></param>
        public void EnableUpdateWallet(ClassRpcConfig rpcConfig)
        {
            Task taskUpdateWallet = new Task(async () =>
            {
                while (rpcConfig.RpcWalletEnabled)
                {
                    try
                    {
                        #region Insert new block transaction to wallets.

                        ClassBlockchainNetworkStatsObject resultBlockchainNetworkStats = await ClassApiClientUtility.
                             GetBlockchainNetworkStatsFromExternalSyncMode(
                             rpcConfig.RpcNodeApiSetting.RpcNodeApiIp,
                             rpcConfig.RpcNodeApiSetting.RpcNodeApiPort,
                             rpcConfig.RpcNodeApiSetting.RpcNodeApiMaxDelay,
                             _cancellation);

                        for (int i = BlockchainSetting.GenesisBlockHeight; i < resultBlockchainNetworkStats.LastBlockHeightUnlocked; i++)
                        {
                            ClassBlockObject blockInformation = await ClassApiClientUtility.GetBlockInformationFromExternalSyncMode(
                               rpcConfig.RpcNodeApiSetting.RpcNodeApiIp,
                               rpcConfig.RpcNodeApiSetting.RpcNodeApiPort,
                               rpcConfig.RpcNodeApiSetting.RpcNodeApiMaxDelay * 1000,
                               i,
                               _cancellation);

                            if (blockInformation != null)
                            {
                                if (blockInformation.BlockStatus == ClassBlockEnumStatus.LOCKED)
                                    continue;

                                DisposableList<ClassBlockTransaction> listBlockTransaction = await ClassApiClientUtility.GetBlockTransactionByRangeFromExternalSyncMode(
                                     rpcConfig.RpcNodeApiSetting.RpcNodeApiIp,
                                     rpcConfig.RpcNodeApiSetting.RpcNodeApiPort,
                                     rpcConfig.RpcNodeApiSetting.RpcNodeApiMaxDelay * 1000,
                                     i,
                                     0,
                                     blockInformation.TotalTransaction,
                                     _cancellation);

                                while (listBlockTransaction.Count == 0) // Callback them.
                                {
                                    listBlockTransaction = await ClassApiClientUtility.GetBlockTransactionByRangeFromExternalSyncMode(
                                     rpcConfig.RpcNodeApiSetting.RpcNodeApiIp,
                                     rpcConfig.RpcNodeApiSetting.RpcNodeApiPort,
                                     rpcConfig.RpcNodeApiSetting.RpcNodeApiMaxDelay * 1000,
                                     i,
                                     0,
                                     blockInformation.TotalTransaction,
                                     _cancellation);

#if DEBUG
                                    Debug.WriteLine("Callback block transaction from height: " + blockInformation.BlockHeight + " | Count: " + blockInformation.TotalTransaction);
#endif
                                }

                                foreach (ClassBlockTransaction blockTransaction in listBlockTransaction.GetList.ToList())
                                {
                                    if (_walletDatabase.GetListWalletAddress.GetList.Contains(blockTransaction.TransactionObject.WalletAddressSender))
                                    {
                                        ClassWalletData walletData = _walletDatabase.GetWalletDataFromWalletAddress(blockTransaction.TransactionObject.WalletAddressSender);
#if DEBUG
                                        Debug.WriteLine("Insert Wallet transaction: " + blockTransaction.TransactionObject.TransactionHash + " | " + walletData.WalletAddress);
#endif
                                        walletData.WalletTransactionList.TryAdd(blockTransaction.TransactionObject.TransactionHash, blockTransaction);
                                    }

                                    if (_walletDatabase.GetListWalletAddress.GetList.Contains(blockTransaction.TransactionObject.WalletAddressReceiver))
                                    {
                                        ClassWalletData walletData = _walletDatabase.GetWalletDataFromWalletAddress(blockTransaction.TransactionObject.WalletAddressReceiver);
#if DEBUG
                                        Debug.WriteLine("Insert Wallet transaction: " + blockTransaction.TransactionObject.TransactionHash + " | " + walletData.WalletAddress);
#endif
                                        walletData.WalletTransactionList.TryAdd(blockTransaction.TransactionObject.TransactionHash, blockTransaction);
                                    }

                                    listBlockTransaction.Clear();
                                }
                            }
                        }

                        #endregion

                        #region Update block transaction stored. 

                        foreach (string walletAddress in _walletDatabase.GetListWalletAddress.GetList)
                        {
                            ClassWalletData walletData = _walletDatabase.GetWalletDataFromWalletAddress(walletAddress);
                            
                            // Initialize back.
                            walletData.WalletBalance = 0;
                            walletData.WalletPendingBalance = 0;

                            // Update block transactions synced according to block transaction confirmations from the network.
                            foreach (ClassBlockTransaction blockTransaction in walletData.WalletTransactionList.Values)
                            {
                                ClassBlockTransaction blockTransactionUpdated = await ClassApiClientUtility.GetBlockTransactionByTransactionHashAndBlockHeightFromExternalSyncMode(
                                rpcConfig.RpcNodeApiSetting.RpcNodeApiIp,
                                rpcConfig.RpcNodeApiSetting.RpcNodeApiPort,
                                rpcConfig.RpcNodeApiSetting.RpcNodeApiMaxDelay,
                                blockTransaction.TransactionObject.TransactionHash,
                                blockTransaction.TransactionObject.BlockHeightTransaction,
                                _cancellation);

                                if (blockTransactionUpdated == null)
                                    continue;

                                // Update transaction stored.
                                walletData.WalletTransactionList[blockTransaction.TransactionObject.TransactionHash] = blockTransactionUpdated;

#if DEBUG
                                Debug.WriteLine("Update block transaction: " + blockTransaction.TransactionObject.TransactionHash + " | Wallet: " + walletData.WalletAddress);
#endif
                                if (blockTransaction.TransactionObject.WalletAddressSender == walletData.WalletAddress)
                                    walletData.WalletBalance -= (blockTransaction.TransactionObject.Amount + blockTransaction.TransactionObject.Fee);
                                else if (blockTransaction.TransactionObject.WalletAddressReceiver == walletData.WalletAddress)
                                {
                                    if (blockTransaction.TransactionTotalConfirmation >= BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations)
                                        walletData.WalletBalance += blockTransaction.TransactionObject.Amount;
                                    else
                                        walletData.WalletPendingBalance += blockTransaction.TransactionObject.Amount;
                                }
                            }
                        }

                        #endregion
                    }
                    catch(Exception error)
                    {
#if DEBUG
                        Debug.WriteLine("Error on update wallet database. Exception: " + error.Message);
#endif
                    }
                    await Task.Delay(RpcConfigUpdateWalletInterval);
                }
            }); taskUpdateWallet.Start();

        }

        /// <summary>
        /// Enable the auto save wallet data.
        /// </summary>
        /// <param name="rpcConfig"></param>
        public void EnableAutoSaveWallet(ClassRpcConfig rpcConfig, string walletPassword)
        {
            Task taskSaveWallet = new Task(async () =>
            {
                while(rpcConfig.RpcWalletEnabled)
                {
                    if (!_walletDatabase.SaveWalletDatabase(
                        rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath,
                        rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseFilename,
                        walletPassword))
                        Console.WriteLine("Failed to save wallet database.");

                    await Task.Delay(RpcConfigAutoSaveWalletInterval);
                }
            });

            taskSaveWallet.Start();
        }
    }
}
