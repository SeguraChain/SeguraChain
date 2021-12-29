using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Utility;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object.Request;
using SeguraChain_RPC_Wallet.API.Service.Packet.Object.Response;
using SeguraChain_RPC_Wallet.Config;
using SeguraChain_RPC_Wallet.Database.Object;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_RPC_Wallet.Database.Wallet
{
    public class ClassWalletData
    {
        /// <summary>
        /// General informations.
        /// </summary>
        public string WalletAddress;
        public string WalletPublicKey;
        public string WalletPrivateKey;

        /// <summary>
        /// Wallet balances.
        /// </summary>
        public BigInteger WalletBalance;
        public BigInteger WalletPendingBalance;
        public long WalletBlockHeight;
        public ConcurrentDictionary<string, ClassBlockTransaction> WalletTransactionList;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassWalletData()
        {
            WalletTransactionList = new ConcurrentDictionary<string, ClassBlockTransaction>();
        }

        /// <summary>
        /// Get the wallet send transaction fee cost calculation with the list of transaction to spend.
        /// </summary>
        /// <param name="amountTarget"></param>
        /// <param name="feeTarget"></param>
        /// <param name="rpcConfig"></param>
        /// <param name="lastBlockHeightUnlocked"></param>
        /// <param name="blockHeightConfirmationStart"></param>
        /// <param name="blockHeightConfirmationTarget"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async Task<ClassSendTransactionFeeCostCalculationObject> GetWalletSendTransactionFeeCostCalculationObject(BigInteger amountTarget, BigInteger feeTarget, ClassRpcConfig rpcConfig, long lastBlockHeightUnlocked, long blockHeightConfirmationStart, long blockHeightConfirmationTarget, CancellationTokenSource cancellation)
        {
            ClassSendTransactionFeeCostCalculationObject sendTransactionFeeCostCalculation = new ClassSendTransactionFeeCostCalculationObject();

            BigInteger amountCalculated = 0;
            BigInteger feeCalculated = 0;

            if (WalletTransactionList.Count == 0)
                return sendTransactionFeeCostCalculation;

            using (DisposableList<string> listBlockTransactionHash = new DisposableList<string>(false, 0, WalletTransactionList.Keys.ToArray()))
            {
                foreach (string blockTransactionHash in listBlockTransactionHash.GetList)
                {
                    // Ignore sent transaction.
                    if (WalletTransactionList[blockTransactionHash].TransactionObject.WalletAddressSender == WalletAddress)
                        continue;

                    if (!WalletTransactionList[blockTransactionHash].IsConfirmed ||
                        !WalletTransactionList[blockTransactionHash].TransactionStatus ||
                         WalletTransactionList[blockTransactionHash].Spent ||
                         WalletTransactionList[blockTransactionHash].NeedUpdateAmountTransactionSource)
                        continue;

                    if (WalletTransactionList[blockTransactionHash].TotalSpend >=
                        WalletTransactionList[blockTransactionHash].TransactionObject.Amount +
                        WalletTransactionList[blockTransactionHash].TransactionObject.Fee)
                        continue;

                    BigInteger difference = WalletTransactionList[blockTransactionHash].TransactionObject.Amount - WalletTransactionList[blockTransactionHash].TotalSpend;

                    if (difference > 0)
                    {
                        if (amountCalculated + difference <= amountTarget)
                            amountCalculated += difference;
                        else
                            amountCalculated += (amountTarget - amountCalculated);

                        sendTransactionFeeCostCalculation.ListTransactionHashToSpend.Add(blockTransactionHash, new ClassTransactionHashSourceObject()
                        {
                            Amount = amountCalculated + difference <= amountTarget ? difference : (amountTarget - amountCalculated)
                        });
                    }

                    if (amountCalculated == amountTarget + feeTarget)
                        break;
                }
            }

            if (amountCalculated == amountTarget + feeTarget)
            {
                Tuple<BigInteger, bool> calculationFeeCostConfirmation = await ClassApiClientUtility.GetFeeCostTransactionFromExternalSyncMode(
                    rpcConfig.RpcNodeApiSetting.RpcNodeApiIp,
                    rpcConfig.RpcNodeApiSetting.RpcNodeApiPort,
                    rpcConfig.RpcNodeApiSetting.RpcNodeApiMaxDelay,
                    lastBlockHeightUnlocked,
                    blockHeightConfirmationStart,
                    blockHeightConfirmationTarget,
                    cancellation);

                if (calculationFeeCostConfirmation.Item2)
                    feeCalculated = calculationFeeCostConfirmation.Item1;
            }

            if (feeCalculated <= feeTarget && amountCalculated == amountTarget + feeTarget)
            {
                sendTransactionFeeCostCalculation.CalculationStatus = true;
                sendTransactionFeeCostCalculation.AmountCalculed = amountCalculated;
                sendTransactionFeeCostCalculation.FeeCalculated = feeCalculated;
            }

            return sendTransactionFeeCostCalculation;
        }

        /// <summary>
        /// Get wallet transaction object.
        /// </summary>
        /// <param name="rpcApiGetWalletTransaction"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public ClassRpcApiSendWalletTransaction GetWalletSendTransactionObject(ClassRpcApiGetWalletTransaction rpcApiGetWalletTransaction, CancellationTokenSource cancellation)
        {
            if (rpcApiGetWalletTransaction == null)
                return null;

            if (rpcApiGetWalletTransaction.by_transaction_by_index)
            {
                if (rpcApiGetWalletTransaction.transaction_start_index < 0 || rpcApiGetWalletTransaction.transaction_end_index < 0 ||
                    rpcApiGetWalletTransaction.transaction_start_index > WalletTransactionList.Count || rpcApiGetWalletTransaction.transaction_end_index > WalletTransactionList.Count)
                    return null;

                ClassRpcApiSendWalletTransaction rpcApiSendWalletTransaction = new ClassRpcApiSendWalletTransaction();

                using (DisposableList<string> transactionHashList = new DisposableList<string>(false, 0, WalletTransactionList.Keys.ToList()))
                {
                    for(int i = rpcApiGetWalletTransaction.transaction_start_index; i < rpcApiGetWalletTransaction.transaction_end_index; i++)
                    {
                        if (cancellation.IsCancellationRequested)
                            break;

                        rpcApiSendWalletTransaction.block_transaction_object.Add(WalletTransactionList[transactionHashList[i]]);
                    }
                }

                rpcApiSendWalletTransaction.packet_timestamp = ClassUtility.GetCurrentTimestampInSecond();

                return rpcApiSendWalletTransaction;
            }

            if (rpcApiGetWalletTransaction.by_transaction_by_hash)
            {
                if (WalletTransactionList.ContainsKey(rpcApiGetWalletTransaction.transaction_hash))
                {
                    return new ClassRpcApiSendWalletTransaction()
                    {
                        block_transaction_object = new List<ClassBlockTransaction>()
                        {
                            WalletTransactionList[rpcApiGetWalletTransaction.transaction_hash]
                        },
                        packet_timestamp = ClassUtility.GetCurrentTimestampInSecond()
                    };
                }
            }

            return null;
        }
    }
}
