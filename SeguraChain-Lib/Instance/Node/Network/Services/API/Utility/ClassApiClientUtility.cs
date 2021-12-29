using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Mining.Enum;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Blockchain.Stats.Object;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Instance.Node.Network.Enum.API.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Client.Enum;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Request;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Response;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Utility
{
    /// <summary>
    /// Client API Utility, usefull for RPC Wallet Tool, Desktop Wallet tool, Solo mining tool, and other kind of tools who need to communicate with an API of a Node.
    /// </summary>
    public class ClassApiClientUtility
    {

        /// <summary>
        /// Check if the API is alive.
        /// </summary>
        /// <param name="peerApiIp"></param>
        /// <param name="peerApiPort"></param>
        /// <param name="peerApiMaxConnectionDelay"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> GetAliveFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, CancellationTokenSource cancellation)
        {
            string content = await SendGetRequest(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassPeerApiEnumGetRequest.GetAlive, cancellation);

            if (ClassUtility.TryDeserialize(content, out ClassApiPeerPacketObjetReceive apiPeerPacketObjetReceive))
                return apiPeerPacketObjetReceive.PacketType == ClassPeerApiPacketResponseEnum.OK;

            return false;
        }

        /// <summary>
        /// Retrieve the current blocktemplate from the external sync mode.
        /// </summary>
        /// <param name="peerApiIp"></param>
        /// <param name="peerApiPort"></param>
        /// <param name="peerApiMaxConnectionDelay"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<ClassApiPeerPacketSendBlockTemplate> GetBlockTemplateFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendBlockTemplate peerPacketSendBlockTemplate = await SendGetRequestToExternalSyncNode<ClassApiPeerPacketSendBlockTemplate>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassPeerApiEnumGetRequest.GetBlockTemplate, cancellation);

            return peerPacketSendBlockTemplate != null ? peerPacketSendBlockTemplate : null;
        }


        /// <summary>
        /// Submit a solo mining share across a node API target and retrieve his result.
        /// </summary>
        /// <param name="miningPoWaCShareObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<ClassMiningPoWaCEnumStatus> SubmitSoloMiningShareFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, ClassMiningPoWaCShareObject miningPoWaCShareObject, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendMiningShareResponse peerPacketSendMiningShareResponse = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendMiningShareResponse>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketSendMiningShare
            {
                MiningPowShareObject = miningPoWaCShareObject,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.PUSH_MINING_SHARE, cancellation);

            return peerPacketSendMiningShareResponse != null ? peerPacketSendMiningShareResponse.MiningPoWShareStatus : ClassMiningPoWaCEnumStatus.SUBMIT_NETWORK_ERROR;
        }

        /// <summary>
        /// Retrieve the blockchain network stats object from the external sync mode.
        /// </summary>
        /// <returns></returns>
        public static async Task<ClassBlockchainNetworkStatsObject> GetBlockchainNetworkStatsFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendNetworkStats peerPacketSendNetworkStats = await SendGetRequestToExternalSyncNode<ClassApiPeerPacketSendNetworkStats>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassPeerApiEnumGetRequest.GetNetworkStats, cancellation);

            return peerPacketSendNetworkStats != null ? peerPacketSendNetworkStats.BlockchainNetworkStatsObject : null;
        }

        /// <summary>
        /// Retrieve a block transaction from a transaction hash a block height from the external sync mode.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<ClassBlockTransaction> GetBlockTransactionByTransactionHashAndBlockHeightFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, string transactionHash, long blockHeight, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendBlockTransaction peerPacketSendBlockTransaction = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendBlockTransaction>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketAskBlockTransaction()
            {
                BlockHeight = blockHeight,
                TransactionHash = transactionHash,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.ASK_BLOCK_TRANSACTION, cancellation);

            return peerPacketSendBlockTransaction != null ? peerPacketSendBlockTransaction.BlockTransaction : null;
        }

        /// <summary>
        /// Retrieve a block object informations by a block height target from the external sync mode.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<ClassBlockObject> GetBlockInformationFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, long blockHeight, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendBlockInformation peerPacketSendBlockInformation = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendBlockInformation>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketAskBlockInformation()
            {
                BlockHeight = blockHeight,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.ASK_BLOCK_INFORMATION, cancellation);

            return peerPacketSendBlockInformation != null ? peerPacketSendBlockInformation.BlockObject : null;
        }

        /// <summary>
        /// Retrieve a block transaction from a transaction hash a block height from the external sync mode.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<long> GetBlockTimestampCreateFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, long blockHeight, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendBlockInformation peerPacketSendBlockInformation = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendBlockInformation>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketAskBlockInformation()
            {
                BlockHeight = blockHeight,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.ASK_BLOCK_INFORMATION, cancellation);

            return peerPacketSendBlockInformation != null ? peerPacketSendBlockInformation.BlockObject.TimestampCreate : 0;
        }

        /// <summary>
        /// Get a mempool transaction by transaction hash from the external sync mode.
        /// </summary>
        /// <param name="transactionHash"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<ClassTransactionObject> GetWalletMemPoolTransactionFromTransactionHashFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, string transactionHash, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendMemPoolTransaction peerPacketSendMemPoolTransaction = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendMemPoolTransaction>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketAskMemPoolTransaction()
            {
                BlockHeight = 0,
                TransactionHash = transactionHash,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.ASK_MEMPOOL_TRANSACTION, cancellation);

            return peerPacketSendMemPoolTransaction != null ? peerPacketSendMemPoolTransaction.TransactionObject : null;
        }

        /// <summary>
        /// Get a fee cost transaction calculation from the external sync mode.
        /// </summary>
        /// <param name="lastBlockHeightUnlocked"></param>
        /// <param name="blockHeightConfirmationStart"></param>
        /// <param name="blockHeightConfirmationTarget"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<Tuple<BigInteger, bool>> GetFeeCostTransactionFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, long lastBlockHeightUnlocked, long blockHeightConfirmationStart, long blockHeightConfirmationTarget, CancellationTokenSource cancellation)
        {

            ClassApiPeerPacketSendFeeCostConfirmation peerPacketSendFeeCostConfirmation = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendFeeCostConfirmation>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketAskFeeCostTransaction()
            {
                LastBlockHeightUnlocked = lastBlockHeightUnlocked,
                BlockHeightConfirmationStart = blockHeightConfirmationStart,
                BlockHeightConfirmationTarget = blockHeightConfirmationTarget,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.ASK_FEE_COST_TRANSACTION, cancellation);


            return peerPacketSendFeeCostConfirmation != null ? new Tuple<BigInteger, bool>(peerPacketSendFeeCostConfirmation.FeeCost, peerPacketSendFeeCostConfirmation.Status) : new Tuple<BigInteger, bool>(0, false);
        }

        /// <summary>
        /// Get the last block height unlocked from the external sync mode.
        /// </summary>
        /// <returns></returns>
        public static async Task<long> GetLastBlockHeightUnlockedFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendBlockHeight peerPacketSendBlockHeight = await SendGetRequestToExternalSyncNode<ClassApiPeerPacketSendBlockHeight>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassPeerApiEnumGetRequest.GetLastBlockHeightUnlocked, cancellation);

            return peerPacketSendBlockHeight != null ? peerPacketSendBlockHeight.BlockHeight : 0;
        }

        /// <summary>
        /// Get the last block height from the external sync mode.
        /// </summary>
        /// <returns></returns>
        public static async Task<long> GetLastBlockHeightFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendBlockHeight peerPacketSendBlockHeight = await SendGetRequestToExternalSyncNode<ClassApiPeerPacketSendBlockHeight>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassPeerApiEnumGetRequest.GetLastBlockHeight, cancellation);

            return peerPacketSendBlockHeight != null ? peerPacketSendBlockHeight.BlockHeight : 0;
        }


        /// <summary>
        /// Get the last block height transaction confirmation done from the external sync mode.
        /// </summary>
        /// <returns></returns>
        public static async Task<long> GetLastBlockHeightTransactionConfirmationDoneFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendBlockHeight peerPacketSendBlockHeight = await SendGetRequestToExternalSyncNode<ClassApiPeerPacketSendBlockHeight>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassPeerApiEnumGetRequest.GetLastBlockHeightTransactionConfirmation, cancellation);

            return peerPacketSendBlockHeight != null ? peerPacketSendBlockHeight.BlockHeight : 0;
        }

        /// <summary>
        /// Get a gerenated block height start transaction confirmation from the external sync mode.
        /// </summary>
        /// <param name="lastBlockHeightUnlocked"></param>
        /// <param name="lastBlockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<long> GetGenerateBlockHeightStartTransactionConfirmationFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, long lastBlockHeightUnlocked, long lastBlockHeight, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendGenerateBlockHeightStartTransactionConfirmation peerPacketSendGenerateBlockHeightStartTransactionConfirmation = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendGenerateBlockHeightStartTransactionConfirmation>(
                peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketAskGenerateBlockHeightStartTransactionConfirmation()
            {
                LastBlockHeightUnlocked = lastBlockHeightUnlocked,
                LastBlockHeight = lastBlockHeight,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.ASK_GENERATE_BLOCK_HEIGHT_START_TRANSACTION_CONFIRMATION, cancellation);

            return peerPacketSendGenerateBlockHeightStartTransactionConfirmation != null ? peerPacketSendGenerateBlockHeightStartTransactionConfirmation.BlockHeight : 0;
        }


        /// <summary>
        /// Get the transaction count of MemPool by block height from the external sync mode.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<int> GetMemPoolTransactionCountByBlockHeightFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, long blockHeight, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendMemPoolTransactionCount peerPacketSendMemPoolTransactionCount = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendMemPoolTransactionCount>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketAskMemPoolTxCountByBlockHeight()
            {
                BlockHeight = blockHeight,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.ASK_MEMPOOL_TRANSACTION_COUNT_BY_BLOCK_HEIGHT, cancellation);

            return (int)(peerPacketSendMemPoolTransactionCount != null ? peerPacketSendMemPoolTransactionCount.TransactionCount : 0);
        }


        /// <summary>
        /// Get the total amount of transaction on MemPool from the external sync mode.
        /// </summary>
        /// <returns></returns>
        public static async Task<long> GetMemPoolTotalTransactionCountFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendMemPoolTransactionCount peerPacketSendMemPoolTransactionCount = await SendGetRequestToExternalSyncNode<ClassApiPeerPacketSendMemPoolTransactionCount>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassPeerApiEnumGetRequest.GetMemPoolTransactionCount, cancellation);

            return peerPacketSendMemPoolTransactionCount != null ? peerPacketSendMemPoolTransactionCount.TransactionCount : 0;
        }

        /// <summary>
        /// Get the total amount of transaction on MemPool from the external sync mode.
        /// </summary>
        /// <returns></returns>
        public static async Task<DisposableList<long>> GetMemPoolListBlockHeights(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendMemPoolBlockHeights peerPacketSendMemPoolBlockHeights = await SendGetRequestToExternalSyncNode<ClassApiPeerPacketSendMemPoolBlockHeights>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassPeerApiEnumGetRequest.GetMemPoolBlockHeights, cancellation);

            return peerPacketSendMemPoolBlockHeights?.ListBlockHeights != null ? new DisposableList<long>(false, 0, peerPacketSendMemPoolBlockHeights.ListBlockHeights) : new DisposableList<long>();
        }


        /// <summary>
        /// Get a list of transaction of MemPool by range range from the external sync mode.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<DisposableList<ClassTransactionObject>> GetMemPoolTransactionByRangeFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, long blockHeight, int start, int end, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendMemPoolTransactionByRange peerPacketSendMemPoolTransactionByRange = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendMemPoolTransactionByRange>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketAskMemPoolTransactionByRange
            {
                BlockHeight = blockHeight,
                Start = start,
                End = end,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.ASK_MEMPOOL_TRANSACTION_BY_RANGE, cancellation);

            return peerPacketSendMemPoolTransactionByRange?.ListTransaction != null ? new DisposableList<ClassTransactionObject>(false, 0, peerPacketSendMemPoolTransactionByRange.ListTransaction) : new DisposableList<ClassTransactionObject>();
        }

        /// <summary>
        /// Get a list of transaction of Blockchain by range range from the external sync mode.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<DisposableList<ClassBlockTransaction>> GetBlockTransactionByRangeFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, long blockHeight, int start, int end, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendListBlockTransaction peerPacketSendListBlockTransaction = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendListBlockTransaction>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketAskBlockTransactionByRange()
            {
                BlockHeight = blockHeight,
                Start = start,
                End = end,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.ASK_BLOCK_TRANSACTION_BY_RANGE, cancellation);

            return peerPacketSendListBlockTransaction?.ListBlockTransaction != null ? new DisposableList<ClassBlockTransaction>(false, 0, peerPacketSendListBlockTransaction.ListBlockTransaction) : new DisposableList<ClassBlockTransaction>();
        }

        /// <summary>
        /// Get a list of transaction by hash list from the external sync mode.
        /// </summary>
        /// <param name="listTransactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<DisposableList<ClassBlockTransaction>> GetBlockTransactionByHashFromExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, List<string> listTransactionHash, long blockHeight, CancellationTokenSource cancellation)
        {

            ClassApiPeerPacketSendListBlockTransaction peerPacketSendListBlockTransaction = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendListBlockTransaction>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketAskBlockTransactionByHashList()
            {
                BlockHeight = blockHeight,
                ListTransactionHash = listTransactionHash,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.ASK_BLOCK_TRANSACTION_BY_HASH_LIST, cancellation);


            return peerPacketSendListBlockTransaction?.ListBlockTransaction != null ? new DisposableList<ClassBlockTransaction>(false, 0, peerPacketSendListBlockTransaction.ListBlockTransaction) : new DisposableList<ClassBlockTransaction>();
        }

        /// <summary>
        /// Send a transaction by using the external sync mode.
        /// </summary>
        /// <param name="transactionObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> SendTransactionByExternalSyncMode(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, ClassTransactionObject transactionObject, CancellationTokenSource cancellation)
        {
            ClassApiPeerPacketSendPushWalletTransactionResponse peerPacketSendPushWalletTransactionResponse = await SendPostRequestToExternalSyncNode<ClassApiPeerPacketSendPushWalletTransactionResponse>(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, ClassUtility.SerializeData(new ClassApiPeerPacketPushWalletTransaction()
            {
                TransactionObject = transactionObject,
                PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
            }), ClassPeerApiPostPacketSendEnum.PUSH_WALLET_TRANSACTION, cancellation);

            return peerPacketSendPushWalletTransactionResponse != null ? 
                peerPacketSendPushWalletTransactionResponse.BlockTransactionInsertStatus == ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INSERTED : false;
        }


        /// <summary>
        /// Send a get request to the peer selected in external mode.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="getRequest"></param>
        /// <returns></returns>
        private static async Task<T> SendGetRequestToExternalSyncNode<T>(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, string getRequest, CancellationTokenSource cancellation)
        {
            string content = await SendGetRequest(peerApiIp, peerApiPort, peerApiMaxConnectionDelay, getRequest, cancellation);

            if (ClassUtility.TryDeserialize(content, out ClassApiPeerPacketObjetReceive apiPeerPacketObjetReceive))
            {
                // Clean up.
                content.Clear();

                if (!apiPeerPacketObjetReceive.PacketObjectSerialized.IsNullOrEmpty(out _))
                {
                    if (ClassUtility.TryDeserialize(apiPeerPacketObjetReceive.PacketObjectSerialized, out T apiPeerPacketContent))
                        return apiPeerPacketContent;
                }

            }

            return default;
        }

        /// <summary>
        /// Send a get request.
        /// </summary>
        /// <param name="peerApiIp"></param>
        /// <param name="peerApiPort"></param>
        /// <param name="peerApiMaxConnectionDelay"></param>
        /// <param name="getRequest"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private static async Task<string> SendGetRequest(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, string getRequest, CancellationTokenSource cancellation)
        {
            string content = null;

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = new TimeSpan(0, 0, peerApiMaxConnectionDelay);

#if NET5_0_OR_GREATER
                    content = await httpClient.GetStringAsync("http://" + peerApiIp + ":" + peerApiPort + "/" + getRequest, cancellation.Token);
#else
                    content = await httpClient.GetStringAsync("http://" + peerApiIp + ":" + peerApiPort + "/" + getRequest);
#endif
                }
            }
            catch
            {
                // Ignored catch the exception once the task is cancelled.
            }

            return content;
        }

        /// <summary>
        /// Send a post request to the peer selected in external mode.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packetContent"></param>
        /// <param name="packetType"></param>
        /// <returns></returns>
        private static async Task<T> SendPostRequestToExternalSyncNode<T>(string peerApiIp, int peerApiPort, int peerApiMaxConnectionDelay, string packetContent, ClassPeerApiPostPacketSendEnum packetType, CancellationTokenSource cancellation)
        {
            string content = null;

            try
            {
                using (CancellationTokenSource cancellationTokenTimeout = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(peerApiMaxConnectionDelay * 1000).Token, cancellation.Token))
                {
                    if (packetType != ClassPeerApiPostPacketSendEnum.ASK_FEE_COST_TRANSACTION)
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            httpClient.Timeout = new TimeSpan(0, 0, peerApiMaxConnectionDelay);

                            var response = await httpClient.PostAsync("http://" + peerApiIp + ":" + peerApiPort, new StringContent(ClassUtility.SerializeData(new ClassApiPeerPacketObjectSend()
                            {
                                PacketType = packetType,
                                PacketContentObjectSerialized = packetContent
                            })), cancellation.Token);

                            if (response.Content.Headers.ContentLength > 0)
                            {
#if NET5_0_OR_GREATER
                                using (StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationTokenTimeout.Token)))
#else
                                using (StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
#endif
                                {
                                    string line;

                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        content += line;
                                        if (content.Length >= response.Content.Headers.ContentLength)
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {

                        WebRequest request = WebRequest.Create("http://" + peerApiIp + ":" + peerApiPort);
                        request.Method = "POST";
                        request.Proxy = null;
                        request.Timeout = peerApiMaxConnectionDelay;
                        request.ContentType = "text/json";

                        using (DisposableList<byte[]> disposableData = new DisposableList<byte[]>(false))
                        {
                            disposableData.Add(ClassUtility.GetByteArrayFromStringAscii(ClassUtility.SerializeData(new ClassApiPeerPacketObjectSend()
                            {
                                PacketType = packetType,
                                PacketContentObjectSerialized = packetContent
                            })));

                            request.ContentLength = disposableData[0].Length;


                            using (Stream dataStream = request.GetRequestStream())
                                dataStream.Write(disposableData[0], 0, disposableData[0].Length);

                            using (WebResponse response = request.GetResponse())
                            {
                                if (response.ContentLength > 0)
                                {
                                    using (Stream stream = response.GetResponseStream())
                                    {
                                        if (stream != null)
                                        {
                                            using (StreamReader reader = new StreamReader(stream))
                                            {
                                                string line;

                                                while ((line = reader.ReadLine()) != null)
                                                {
                                                    content += line;
                                                    if (content.Length >= response.ContentLength)
                                                        break;
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
                // Ignored catch the exception once the task is cancelled.
            }


            if (!content.IsNullOrEmpty(out _))
            {
                if (ClassUtility.TryDeserialize(content, out ClassApiPeerPacketObjetReceive apiPeerPacketObjetReceive))
                {
                    // Clean up.
                    content.Clear();

                    if (apiPeerPacketObjetReceive != null)
                    {
                        if (ClassUtility.TryDeserialize(apiPeerPacketObjetReceive.PacketObjectSerialized, out T apiPeerPacketContent))
                            return apiPeerPacketContent;
                    }
                }
                else
                    // Clean up.
                    content.Clear();
            }


            return default;
        }
    }
}
