using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Object;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Utility;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object.Request;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object.Response;
using SeguraChain_RPC_Wallet.Config;
using SeguraChain_RPC_Wallet.Database;
using SeguraChain_RPC_Wallet.Database.Object;
using SeguraChain_RPC_Wallet.Database.Wallet;
using SeguraChain_RPC_Wallet.Node.Object;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_RPC_Wallet.Node.Client
{
    public class ClassNodeApiClient
    {
        private ClassWalletDatabase _walletDatabaseObject;
        private ClassRpcConfig _rpcConfigObject;
        private CancellationTokenSource _cancellationTokenAutoUpdateNodeStats;
        private bool _enableAutoUpdateNodeStatsTask;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="walletDatabaseObject"></param>
        /// <param name="rpcConfigObject"></param>
        public ClassNodeApiClient(ClassWalletDatabase walletDatabaseObject, ClassRpcConfig rpcConfigObject)
        {
            _walletDatabaseObject = walletDatabaseObject;
            _rpcConfigObject = rpcConfigObject;
            _cancellationTokenAutoUpdateNodeStats = new CancellationTokenSource();
        }

        /// <summary>
        /// Node network stats.
        /// </summary>
        private ClassBlockchainNetworkStatsObject _blockchainNetworkStatsObject;
        private long _blockchainLastBlockHeight;
        private long _blockchainLastBlockHeightUnlocked;
        private long _blockchainBlockHeightTransactionStartConfirmation;
        private long _blockchainLastBlockHeightTimestampCreate;
        private string _blockchainLastBlockHash;


        /// <summary>
        /// Enable the auto update node stats task.
        /// </summary>
        public void EnableAutoUpdateNodeStatsTask()
        {
            _enableAutoUpdateNodeStatsTask = true;

            try
            {

                new TaskFactory().StartNew(async () =>
                {
                    while(_enableAutoUpdateNodeStatsTask)
                    {
                        try
                        {
                            _blockchainNetworkStatsObject = await ClassApiClientUtility.GetBlockchainNetworkStatsFromExternalSyncMode(_rpcConfigObject.RpcNodeApiSetting.RpcNodeApiIp, _rpcConfigObject.RpcNodeApiSetting.RpcNodeApiPort, _rpcConfigObject.RpcNodeApiSetting.RpcNodeApiMaxDelay, _cancellationTokenAutoUpdateNodeStats);

                            if (_blockchainNetworkStatsObject != null)
                            {
                                _blockchainLastBlockHeight = _blockchainNetworkStatsObject.LastBlockHeight;
                                _blockchainLastBlockHeightUnlocked = _blockchainNetworkStatsObject.LastBlockHeightUnlocked;
                                _blockchainBlockHeightTransactionStartConfirmation = await ClassApiClientUtility.GetGenerateBlockHeightStartTransactionConfirmationFromExternalSyncMode(_rpcConfigObject.RpcApiSetting.RpcApiIp, _rpcConfigObject.RpcApiSetting.RpcApiPort, _rpcConfigObject.RpcApiSetting.RpcApiMaxConnectDelay, _blockchainNetworkStatsObject.LastBlockHeightUnlocked, _blockchainNetworkStatsObject.LastBlockHeight, _cancellationTokenAutoUpdateNodeStats);
                                _blockchainLastBlockHeightTimestampCreate = await ClassApiClientUtility.GetBlockTimestampCreateFromExternalSyncMode(_rpcConfigObject.RpcNodeApiSetting.RpcNodeApiIp, _rpcConfigObject.RpcNodeApiSetting.RpcNodeApiPort, _rpcConfigObject.RpcNodeApiSetting.RpcNodeApiMaxDelay, _blockchainLastBlockHeight, _cancellationTokenAutoUpdateNodeStats);
                                _blockchainLastBlockHash = _blockchainNetworkStatsObject.LastBlockHash;

#if DEBUG
                                Debug.WriteLine("Current blockchain stats. Last Block Height: " + _blockchainNetworkStatsObject.LastBlockHeight + " | Last Block Height Unlocked: " + _blockchainNetworkStatsObject.LastBlockHeightUnlocked);
                                Debug.WriteLine("Last Block Hash: " + _blockchainNetworkStatsObject.LastBlockHash + " | " + _blockchainNetworkStatsObject.NetworkHashrateEstimatedFormatted);
#endif
                            }
                        }
                        catch
                        {
                            break;
                        }

                        await System.Threading.Tasks.Task.Delay(5000, _cancellationTokenAutoUpdateNodeStats.Token);
                    }

                }, _cancellationTokenAutoUpdateNodeStats.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Send a transaction across the node API service.
        /// </summary>
        /// <param name="rpcApiPostTransactionObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<ClassReportSendTransactionObject> SendTransaction(ClassRpcApiPostTransactionObject rpcApiPostTransactionObject, CancellationTokenSource cancellation)
        {

            if (_blockchainNetworkStatsObject == null)
                return null;

            if (_blockchainNetworkStatsObject.LastBlockHeight < BlockchainSetting.GenesisBlockHeight)
                return null;

            ClassWalletData walletDataSender = _walletDatabaseObject.GetWalletDataFromWalletAddress(rpcApiPostTransactionObject.wallet_address_src);

            if (walletDataSender == null)
                return null;

            ClassWalletData walletDataReceiver = rpcApiPostTransactionObject.transfer ? _walletDatabaseObject.GetWalletDataFromWalletAddress(rpcApiPostTransactionObject.wallet_address_target) : null;

            if (walletDataReceiver == null && rpcApiPostTransactionObject.transfer)
                return null;

            using (ClassSendTransactionFeeCostCalculationObject sendTransactionFeeCostCalculationObject = await walletDataSender.GetWalletSendTransactionFeeCostCalculationObject(rpcApiPostTransactionObject.amount,
                 rpcApiPostTransactionObject.fee,
                 _rpcConfigObject,
                 _blockchainLastBlockHeightUnlocked,
                 _blockchainBlockHeightTransactionStartConfirmation,
                 _blockchainBlockHeightTransactionStartConfirmation + rpcApiPostTransactionObject.total_confirmation_target,
                 cancellation))
            {

                if (!sendTransactionFeeCostCalculationObject.CalculationStatus ||
                    sendTransactionFeeCostCalculationObject.ListTransactionHashToSpend.Count == 0 ||
                    sendTransactionFeeCostCalculationObject.AmountCalculed != rpcApiPostTransactionObject.amount ||
                    sendTransactionFeeCostCalculationObject.FeeCalculated != rpcApiPostTransactionObject.fee)
                    return null;

                ClassTransactionObject transactionObject = ClassTransactionUtility.BuildTransaction(sendTransactionFeeCostCalculationObject.BlockHeight,
                        sendTransactionFeeCostCalculationObject.BlockHeightTarget,
                        walletDataSender.WalletAddress,
                        walletDataSender.WalletPublicKey,
                        rpcApiPostTransactionObject.transfer ? walletDataReceiver.WalletPublicKey : string.Empty,
                        sendTransactionFeeCostCalculationObject.AmountCalculed,
                        sendTransactionFeeCostCalculationObject.FeeCalculated,
                        rpcApiPostTransactionObject.wallet_address_target,
                        ClassUtility.GetCurrentTimestampInSecond(),
                        rpcApiPostTransactionObject.transfer ? ClassTransactionEnumType.TRANSFER_TRANSACTION : ClassTransactionEnumType.NORMAL_TRANSACTION,
                        rpcApiPostTransactionObject.payment_id,
                        _blockchainLastBlockHash,
                        string.Empty,
                        walletDataSender.WalletPrivateKey,
                        rpcApiPostTransactionObject.transfer ? walletDataReceiver.WalletPrivateKey : string.Empty,
                        sendTransactionFeeCostCalculationObject.ListTransactionHashToSpend.GetList,
                        _blockchainLastBlockHeightTimestampCreate,
                        cancellation
                );


                if (await ClassApiClientUtility.SendTransactionByExternalSyncMode(_rpcConfigObject.RpcNodeApiSetting.RpcNodeApiIp,
                                                                        _rpcConfigObject.RpcNodeApiSetting.RpcNodeApiPort,
                                                                        _rpcConfigObject.RpcNodeApiSetting.RpcNodeApiMaxDelay, transactionObject, cancellation))
                {
                    return new ClassReportSendTransactionObject()
                    {
                        block_height = sendTransactionFeeCostCalculationObject.BlockHeight,
                        block_height_confirmation_target = sendTransactionFeeCostCalculationObject.BlockHeightTarget,
                        status = true,
                        transaction_hash = transactionObject.TransactionHash,
                        wallet_address_sender = rpcApiPostTransactionObject.wallet_address_src,
                        wallet_address_target = rpcApiPostTransactionObject.wallet_address_target
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Send a report of the rpc wallet stats.
        /// </summary>
        /// <param name="rpcApiGetWalletStats"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public ClassReportSendRpcWalletStats SendRpcWalletStats(ClassRpcApiGetWalletStats rpcApiGetWalletStats, CancellationTokenSource cancellation)
        {
            if (_blockchainNetworkStatsObject == null)
                return null;

            ClassReportSendRpcWalletStats reportSendRpcWalletStats = new ClassReportSendRpcWalletStats
            {
                total_wallet_count = _walletDatabaseObject.GetWalletCount,
                current_block_height = _blockchainNetworkStatsObject.LastBlockHeight,
                last_block_height_unlocked = _blockchainNetworkStatsObject.LastBlockHeightUnlocked,
                current_block_hash = _blockchainNetworkStatsObject.LastBlockHash,
                current_block_difficulty = _blockchainNetworkStatsObject.LastBlockDifficulty
            };

            if (rpcApiGetWalletStats.show_total_balance)
            {
                using(DisposableList<string> listWalletAddress = _walletDatabaseObject.GetListWalletAddress)
                {
                    foreach(string walletAddress in listWalletAddress.GetList)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        ClassWalletData walletData = _walletDatabaseObject.GetWalletDataFromWalletAddress(walletAddress);

                        if (walletData == null)
                            continue;

                        reportSendRpcWalletStats.total_balance += walletData.WalletBalance;
                        reportSendRpcWalletStats.total_pending_balance += walletData.WalletPendingBalance;
                    }
                }
            }

            return reportSendRpcWalletStats;
        }

        /// <summary>
        /// Send a wallet data information packet.
        /// </summary>
        /// <param name="rpcApiGetWalletInformation"></param>
        /// <returns></returns>
        public ClassRpcApiSendWalletInformation SendRpcWalletInformation(ClassRpcApiGetWalletInformation rpcApiGetWalletInformation)
        {
            if (rpcApiGetWalletInformation == null)
                return null;

            ClassWalletData walletData = _walletDatabaseObject.GetWalletDataFromWalletAddress(rpcApiGetWalletInformation.wallet_address);

            if (walletData == null)
                return null;

            return new ClassRpcApiSendWalletInformation()
            {
                wallet_address = walletData.WalletAddress,
                wallet_balance = walletData.WalletBalance,
                wallet_pending_balance = walletData.WalletPendingBalance,
                wallet_private_key = walletData.WalletPrivateKey,
                wallet_public_key = walletData.WalletPublicKey,
                wallet_transaction_count = walletData.WalletTransactionList.Count
            };
        }

        /// <summary>
        /// Send a wallet data transaction packet.
        /// </summary>
        /// <param name="rpcApiSendWalletTransaction"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public ClassRpcApiSendWalletTransaction SendRpcWalletTransactionObject(ClassRpcApiGetWalletTransaction rpcApiSendWalletTransaction, CancellationTokenSource cancellation)
        {
            if (rpcApiSendWalletTransaction == null)
                return null;

            ClassWalletData walletData = _walletDatabaseObject.GetWalletDataFromWalletAddress(rpcApiSendWalletTransaction.wallet_address);

            if (walletData == null)
                return null;

            return walletData.GetWalletSendTransactionObject(rpcApiSendWalletTransaction, cancellation);
        }
    }
}
