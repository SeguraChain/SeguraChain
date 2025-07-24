using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Object;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Utility;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object.Request;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object.Response.POST;
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

                System.Threading.Tasks.Task.Factory.StartNew(async () =>
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
                            
                        }

                        await System.Threading.Tasks.Task.Delay(5000);
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

            ClassBlockchainNetworkStatsObject blockchainNetworkStatsObject = await ClassApiClientUtility.GetBlockchainNetworkStatsFromExternalSyncMode(_rpcConfigObject.RpcNodeApiSetting.RpcNodeApiIp, _rpcConfigObject.RpcNodeApiSetting.RpcNodeApiPort, _rpcConfigObject.RpcNodeApiSetting.RpcNodeApiMaxDelay * 1000, cancellation);

            if (blockchainNetworkStatsObject == null)
                return null;

            ClassSendTransactionFeeCostCalculationObject sendTransactionFeeCostCalculationObject = await walletDataSender.GetWalletSendTransactionFeeCostCalculationObject(rpcApiPostTransactionObject.amount,
                 rpcApiPostTransactionObject.fee,
                 _rpcConfigObject,
                 blockchainNetworkStatsObject.LastBlockHeight,
                 blockchainNetworkStatsObject.LastBlockHeight,
                  blockchainNetworkStatsObject.LastBlockHeight + rpcApiPostTransactionObject.total_confirmation_target,
                 cancellation);


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
                                                                    _rpcConfigObject.RpcNodeApiSetting.RpcNodeApiMaxDelay * 1000, transactionObject, cancellation))
            {
                return new ClassReportSendTransactionObject()
                {
                    block_height = sendTransactionFeeCostCalculationObject.BlockHeight,
                    block_height_confirmation_target = sendTransactionFeeCostCalculationObject.BlockHeightTarget,
                    status = true,
                    transaction_hash = transactionObject.TransactionHash,
                    transaction_object = transactionObject,
                    wallet_address_sender = rpcApiPostTransactionObject.wallet_address_src,
                    wallet_address_target = rpcApiPostTransactionObject.wallet_address_target
                };
            }

            return new ClassReportSendTransactionObject()
            {
                block_height = sendTransactionFeeCostCalculationObject.BlockHeight,
                block_height_confirmation_target = sendTransactionFeeCostCalculationObject.BlockHeightTarget,
                status = false,
                transaction_hash = transactionObject.TransactionHash,
                transaction_object = transactionObject,
                wallet_address_sender = rpcApiPostTransactionObject.wallet_address_src,
                wallet_address_target = rpcApiPostTransactionObject.wallet_address_target
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
