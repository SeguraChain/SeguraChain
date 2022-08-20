using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.MemPool.Database;
using SeguraChain_Lib.Blockchain.Mining.Enum;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Instance.Node.Network.Enum.API.Packet;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Client.Enum;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.Explorer;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Request;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Response;
using SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Manager;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.Utility;
using static SeguraChain_Lib.Other.Object.Network.ClassCustomSocket;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Client
{
    public class ClassPeerApiClientObject : IDisposable
    {
        private readonly ClassPeerNetworkSettingObject _peerNetworkSettingObject;
        private readonly ClassPeerFirewallSettingObject _peerFirewallSettingObject;
        private readonly string _apiServerOpenNatIp;
        private ClassCustomSocket _clientSocket;
        private readonly string _clientIp;
        public CancellationTokenSource _cancellationTokenApiClient;
        private CancellationTokenSource _cancellationTokenApiClientCheck;
        public bool ClientConnectionStatus;
        public long ClientConnectTimestamp;
        public bool PacketResponseSent;
        private bool _validPostRequest;


        public bool OnHandlePacket;

        #region Dispose functions

        private bool _disposed;

        ~ClassPeerApiClientObject()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                CloseApiClientConnection(false);

            _disposed = true;
        }

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="clientIp"></param>
        /// <param name="peerFirewallSettingObject"></param>
        /// <param name="apiServerOpenNatIp"></param>
        /// <param name="cancellationTokenApiServer"></param>
        /// <param name="peerNetworkSettingObject"></param>
        public ClassPeerApiClientObject(ClassCustomSocket clientSocket, string clientIp, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject, string apiServerOpenNatIp, CancellationTokenSource cancellationTokenApiServer)
        {
            _apiServerOpenNatIp = apiServerOpenNatIp;
            _clientSocket = clientSocket;
            _peerNetworkSettingObject = peerNetworkSettingObject;
            _peerFirewallSettingObject = peerFirewallSettingObject;
            _clientIp = clientIp;
            _cancellationTokenApiClient = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenApiServer.Token);
            _cancellationTokenApiClientCheck = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenApiServer.Token);
            ClientConnectionStatus = true;
            ClientConnectTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;
        }

        #region Manage Client API connection

        /// <summary>
        /// Handle a new incoming connection from a client to the API.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> HandleApiClientConnection()
        {


            TaskManager.TaskManager.InsertTask(new Action(async () => await CheckApiClientConnection()), 0, _cancellationTokenApiClientCheck, _clientSocket);
       

            try
            {
                long packetSizeCount = 0;

                using (DisposableList<byte[]> listPacket = new DisposableList<byte[]>())
                {
                    if (ClientConnectionStatus)
                    {
                        try
                        {
                            bool continueReading = true;
                            bool isPostRequest = false;

                            string packetReceived = string.Empty;


                            while (continueReading && ClientConnectionStatus)
                            {
                                using (ReadPacketData readPacketData = await _clientSocket.TryReadPacketData(_peerNetworkSettingObject.PeerMaxPacketBufferSize, _cancellationTokenApiClient))
                                {

                                    if (readPacketData.Status)
                                        listPacket.Add(readPacketData.Data);
                                    else break;

                                    packetSizeCount += _peerNetworkSettingObject.PeerMaxPacketBufferSize;

                                    ClientConnectTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;

                                    if (listPacket.Count > 0)
                                    {
                                        // If above the max data to receive.
                                        if (packetSizeCount / 1024 >= ClassPeerPacketSetting.PacketMaxLengthReceive)
                                            listPacket.Clear();

                                        foreach (byte dataByte in listPacket.GetList.SelectMany(x => x).ToArray())
                                        {
                                            char character = (char)dataByte;

                                            if (character != '\0')
                                                packetReceived += character;
                                        }

                                        // Control the post request content length, break the reading if the content length is reach.
                                        if (packetReceived.Contains(ClassPeerApiEnumHttpPostRequestSyntax.HttpPostRequestType) && packetReceived.Contains(ClassPeerApiEnumHttpPostRequestSyntax.PostDataPosition1))
                                        {
                                            isPostRequest = true;

                                            int indexPacket = packetReceived.IndexOf(ClassPeerApiEnumHttpPostRequestSyntax.PostDataPosition1, 0, StringComparison.Ordinal);

                                            string[] packetInfoSplitted = packetReceived.Substring(indexPacket).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                                            if (packetInfoSplitted.Length == 2)
                                            {
                                                int packetContentLength = 0;

                                                string contentLength = packetInfoSplitted[0].Replace(ClassPeerApiEnumHttpPostRequestSyntax.PostDataPosition1 + " ", "");

                                                // Compare Content-length with content.
                                                if (int.TryParse(contentLength, out packetContentLength))
                                                {
                                                    if (packetContentLength == packetInfoSplitted[1].Length)
                                                        continueReading = false;
                                                }
                                            }
                                        }
                                        else if (packetReceived.Contains("GET /"))
                                            continueReading = false;

                                        if (continueReading)
                                            packetReceived.Clear();
                                    }
                                }
                            }
                            

                            if (listPacket.Count > 0 && ClientConnectionStatus)
                            {
                                #region Take in count the common POST HTTP request syntax of data.

                                if (isPostRequest)
                                {
                                    _validPostRequest = true;

                                    int indexPacket = packetReceived.IndexOf(ClassPeerApiEnumHttpPostRequestSyntax.PostDataTargetIndexOf, 0, StringComparison.Ordinal);

                                    packetReceived = packetReceived.Substring(indexPacket);

                                    OnHandlePacket = true;

                                    if (!await HandleApiClientPostPacket(packetReceived))
                                    {
                                        if (_peerFirewallSettingObject.PeerEnableFirewallLink)
                                            ClassPeerFirewallManager.InsertInvalidPacket(_clientIp);
                                    }

                                    OnHandlePacket = false;
                                }

                                #endregion

                                #region Take in count the common GET HTTP request.

                                if (!_validPostRequest)
                                {
                                    if (packetReceived.Contains("GET"))
                                    {
                                        packetReceived = packetReceived.GetStringBetweenTwoStrings("GET /", "HTTP");
                                        packetReceived = packetReceived.Replace("%7C", "|"); // Translate special character | 
                                        packetReceived = packetReceived.Replace(" ", ""); // Remove empty,space characters

                                        OnHandlePacket = true;

                                        if (!await HandleApiClientGetPacket(packetReceived))
                                        {
                                            if (_peerFirewallSettingObject.PeerEnableFirewallLink)
                                                ClassPeerFirewallManager.InsertInvalidPacket(_clientIp);
                                        }

                                        OnHandlePacket = false;
                                    }
                                }

                                #endregion
                            }

                            // Close the connection after to have receive the packet of the incoming connection.
                            ClientConnectionStatus = false;

                        }
                        catch
                        {
                            ClientConnectionStatus = false;
                        }
                    }
                }
            }
            catch
            {
                // Ignored.
            }


            return PacketResponseSent;
        }

        /// <summary>
        /// Check the API Client Connection
        /// </summary>
        /// <returns></returns>
        private async Task CheckApiClientConnection()
        {
            
            while (ClientConnectionStatus)
            {
                try
                {
                    // Disconnected or the task has been stopped.
                    if (!ClientConnectionStatus)
                        break;

                    // If the API Firewall Link is enabled.
                    if (_peerFirewallSettingObject.PeerEnableFirewallLink)
                    {
                        // Banned.
                        if (!ClassPeerFirewallManager.CheckClientIpStatus(_clientIp))
                            break;
                    }

                    // Timeout.
                    if (!OnHandlePacket)
                    {
                        if (ClientConnectTimestamp + _peerNetworkSettingObject.PeerApiMaxConnectionDelay < TaskManager.TaskManager.CurrentTimestampSecond)
                            break;
                    }

                    if (_clientSocket == null)
                        break;

                    await Task.Delay(1000);
                }
                catch
                {
                    break;
                }

            }

            CloseApiClientConnection(true);
        }

        /// <summary>
        /// Close the API client connection.
        /// </summary>
        public void CloseApiClientConnection(bool fromChecker)
        {
            ClientConnectionStatus = false;

            if (fromChecker)
            {
                try
                {
                    if (_cancellationTokenApiClient != null)
                    {
                        if (!_cancellationTokenApiClient.IsCancellationRequested)
                        {
                            _cancellationTokenApiClient.Cancel();
                            _cancellationTokenApiClient.Dispose();
                        }
                    }
                }
                catch
                {
                    // Ignored.
                }
            }

            if (!fromChecker)
            {
                if (_cancellationTokenApiClientCheck != null)
                {
                    if (!_cancellationTokenApiClientCheck.IsCancellationRequested)
                    {
                        _cancellationTokenApiClientCheck.Cancel();
                        _cancellationTokenApiClientCheck.Dispose();
                    }
                }
            }

            _clientSocket?.Kill(SocketShutdown.Both);
        }

        #endregion

        #region Manage Client API Packets

        /// <summary>
        /// Handle API POST Packet sent by a client.
        /// </summary>
        /// <param name="packetReceived"></param>
        private async Task<bool> HandleApiClientPostPacket(string packetReceived)
        {
            try
            {
                ClassPeerApiPacketResponseEnum typeResponse = ClassPeerApiPacketResponseEnum.OK;

                ClassApiPeerPacketObjectSend apiPeerPacketObjectSend = TryDeserializedPacketContent<ClassApiPeerPacketObjectSend>(packetReceived);

                if (apiPeerPacketObjectSend == null)
                    typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                else
                {
                    if (apiPeerPacketObjectSend.PacketContentObjectSerialized.IsNullOrEmpty(false, out _))
                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                    else
                    {

                        switch (apiPeerPacketObjectSend.PacketType)
                        {
                            case ClassPeerApiPostPacketSendEnum.ASK_BLOCK_INFORMATION:
                                {

                                    ClassApiPeerPacketAskBlockInformation apiPeerPacketAskBlockinformation = TryDeserializedPacketContent<ClassApiPeerPacketAskBlockInformation>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    if (apiPeerPacketAskBlockinformation != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketAskBlockinformation.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            if (ClassBlockchainStats.ContainsBlockHeight(apiPeerPacketAskBlockinformation.BlockHeight))
                                            {
                                                if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendBlockInformation()
                                                {
                                                    BlockObject = await ClassBlockchainStats.GetBlockInformationData(apiPeerPacketAskBlockinformation.BlockHeight, _cancellationTokenApiClient),
                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond

                                                }, ClassPeerApiPacketResponseEnum.SEND_BLOCK_INFORMATION)))
                                                {
                                                    // Can't send packet.
                                                    return false;
                                                }
                                            }
                                            else
                                                typeResponse = ClassPeerApiPacketResponseEnum.INVALID_BLOCK_HEIGHT;
                                        }
                                        else // Invalid Packet Timestamp.
                                            typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET_TIMESTAMP;
                                    }
                                    else // Invalid Packet.
                                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;

                                }
                                break;
                            case ClassPeerApiPostPacketSendEnum.ASK_BLOCK_TRANSACTION:
                                {
                                    ClassApiPeerPacketAskBlockTransaction apiPeerPacketAskBlockTransaction = TryDeserializedPacketContent<ClassApiPeerPacketAskBlockTransaction>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    if (apiPeerPacketAskBlockTransaction != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketAskBlockTransaction.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            if (ClassBlockchainStats.ContainsBlockHeight(apiPeerPacketAskBlockTransaction.BlockHeight))
                                            {
                                                if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendBlockTransaction()
                                                {
                                                    BlockTransaction = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockTransactionFromSpecificTransactionHashAndHeight(apiPeerPacketAskBlockTransaction.TransactionHash, apiPeerPacketAskBlockTransaction.BlockHeight, true, true, _cancellationTokenApiClient),
                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond

                                                }, ClassPeerApiPacketResponseEnum.SEND_BLOCK_TRANSACTION)))
                                                {
                                                    // Can't send packet.
                                                    return false;
                                                }
                                            }
                                            else
                                                typeResponse = ClassPeerApiPacketResponseEnum.INVALID_BLOCK_HEIGHT;
                                        }
                                        else // Invalid Packet Timestamp.
                                            typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET_TIMESTAMP;
                                    }
                                    else // Invalid Packet.
                                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                                }
                                break;
                            case ClassPeerApiPostPacketSendEnum.ASK_GENERATE_BLOCK_HEIGHT_START_TRANSACTION_CONFIRMATION:
                                {
                                    ClassApiPeerPacketAskGenerateBlockHeightStartTransactionConfirmation apiPeerPacketAskGenerateBlockHeightStartTransactionConfirmation = TryDeserializedPacketContent<ClassApiPeerPacketAskGenerateBlockHeightStartTransactionConfirmation>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    if (apiPeerPacketAskGenerateBlockHeightStartTransactionConfirmation != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketAskGenerateBlockHeightStartTransactionConfirmation.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            if (ClassBlockchainStats.ContainsBlockHeight(apiPeerPacketAskGenerateBlockHeightStartTransactionConfirmation.LastBlockHeight) &&
                                                ClassBlockchainStats.ContainsBlockHeight(apiPeerPacketAskGenerateBlockHeightStartTransactionConfirmation.LastBlockHeightUnlocked))
                                            {
                                                if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendGenerateBlockHeightStartTransactionConfirmation()
                                                {
                                                    BlockHeight = await ClassTransactionUtility.GenerateBlockHeightStartTransactionConfirmation(apiPeerPacketAskGenerateBlockHeightStartTransactionConfirmation.LastBlockHeightUnlocked,
                                                    apiPeerPacketAskGenerateBlockHeightStartTransactionConfirmation.LastBlockHeight, _cancellationTokenApiClient),
                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond

                                                }, ClassPeerApiPacketResponseEnum.SEND_GENERATE_BLOCK_HEIGHT_START_TRANSACTION_CONFIRMATION)))
                                                {
                                                    // Can't send packet.
                                                    return false;
                                                }
                                            }
                                            else
                                                typeResponse = ClassPeerApiPacketResponseEnum.INVALID_BLOCK_HEIGHT;
                                        }
                                        else // Invalid Packet Timestamp.
                                            typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET_TIMESTAMP;
                                    }
                                    else // Invalid Packet.
                                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                                }
                                break;
                            case ClassPeerApiPostPacketSendEnum.ASK_MEMPOOL_TRANSACTION:
                                {
                                    ClassApiPeerPacketAskMemPoolTransaction apiPeerPacketAskMemPoolTransaction = TryDeserializedPacketContent<ClassApiPeerPacketAskMemPoolTransaction>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    if (apiPeerPacketAskMemPoolTransaction != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketAskMemPoolTransaction.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            if (ClassBlockchainStats.ContainsBlockHeight(apiPeerPacketAskMemPoolTransaction.BlockHeight))
                                            {
                                                if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendMemPoolTransaction()
                                                {
                                                    TransactionObject = await ClassMemPoolDatabase.GetMemPoolTxFromTransactionHash(apiPeerPacketAskMemPoolTransaction.TransactionHash, apiPeerPacketAskMemPoolTransaction.BlockHeight, _cancellationTokenApiClient),
                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond

                                                }, ClassPeerApiPacketResponseEnum.SEND_MEMPOOL_TRANSACTION)))
                                                {
                                                    // Can't send packet.
                                                    return false;
                                                }
                                            }
                                            else
                                                typeResponse = ClassPeerApiPacketResponseEnum.INVALID_BLOCK_HEIGHT;
                                        }
                                        else // Invalid Packet Timestamp.
                                            typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET_TIMESTAMP;
                                    }
                                    else // Invalid Packet.
                                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                                }
                                break;
                            case ClassPeerApiPostPacketSendEnum.ASK_FEE_COST_TRANSACTION:
                                {

                                    ClassApiPeerPacketAskFeeCostTransaction apiPeerPacketAskFeeCostTransaction = TryDeserializedPacketContent<ClassApiPeerPacketAskFeeCostTransaction>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    if (apiPeerPacketAskFeeCostTransaction != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketAskFeeCostTransaction.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            if (ClassBlockchainStats.ContainsBlockHeight(apiPeerPacketAskFeeCostTransaction.LastBlockHeightUnlocked))
                                            {
                                                var feeCostCalculation = await ClassTransactionUtility.GetFeeCostFromWholeBlockchainTransactionActivity(apiPeerPacketAskFeeCostTransaction.LastBlockHeightUnlocked,
                                                    apiPeerPacketAskFeeCostTransaction.BlockHeightConfirmationStart,
                                                    apiPeerPacketAskFeeCostTransaction.BlockHeightConfirmationTarget,
                                                    _cancellationTokenApiClient);

                                                if (feeCostCalculation.Item2)
                                                {
                                                    if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendFeeCostConfirmation()
                                                    {
                                                        FeeCost = feeCostCalculation.Item1,
                                                        Status = feeCostCalculation.Item2,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond

                                                    }, ClassPeerApiPacketResponseEnum.SEND_FEE_COST_TRANSACTION)))
                                                    {
                                                        // Can't send packet.
                                                        return false;
                                                    }
                                                }
                                                else
                                                    typeResponse = ClassPeerApiPacketResponseEnum.INVALID_BLOCK_HEIGHT;
                                            }
                                            else
                                                typeResponse = ClassPeerApiPacketResponseEnum.INVALID_BLOCK_HEIGHT;
                                        }
                                        else // Invalid Packet Timestamp.
                                            typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET_TIMESTAMP;
                                    }
                                    else // Invalid Packet.
                                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                                }
                                break;
                            case ClassPeerApiPostPacketSendEnum.ASK_MEMPOOL_TRANSACTION_COUNT_BY_BLOCK_HEIGHT:
                                {
                                    ClassApiPeerPacketAskMemPoolTxCountByBlockHeight apiPeerPacketAskMemPoolTxCountByBlockHeight = TryDeserializedPacketContent<ClassApiPeerPacketAskMemPoolTxCountByBlockHeight>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    if (apiPeerPacketAskMemPoolTxCountByBlockHeight != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketAskMemPoolTxCountByBlockHeight.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendMemPoolTransactionCount()
                                            {
                                                TransactionCount = await ClassMemPoolDatabase.GetCountMemPoolTxFromBlockHeight(apiPeerPacketAskMemPoolTxCountByBlockHeight.BlockHeight, true, _cancellationTokenApiClient),
                                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond

                                            }, ClassPeerApiPacketResponseEnum.SEND_MEMPOOL_TRANSACTION_COUNT_BY_BLOCK_HEIGHT)))
                                            {
                                                // Can't send packet.
                                                return false;
                                            }
                                        }
                                        else // Invalid Packet Timestamp.
                                            typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET_TIMESTAMP;
                                    }
                                    else // Invalid Packet.
                                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                                }
                                break;
                            case ClassPeerApiPostPacketSendEnum.ASK_MEMPOOL_TRANSACTION_BY_RANGE:
                                {
                                    ClassApiPeerPacketAskMemPoolTransactionByRange apiPeerPacketAsMemPoolTransactionByRange = TryDeserializedPacketContent<ClassApiPeerPacketAskMemPoolTransactionByRange>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    if (apiPeerPacketAsMemPoolTransactionByRange != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketAsMemPoolTransactionByRange.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            using (DisposableList<ClassTransactionObject> listMemPoolTransaction = await ClassMemPoolDatabase.GetMemPoolTxObjectFromBlockHeight(apiPeerPacketAsMemPoolTransactionByRange.BlockHeight, true, _cancellationTokenApiClient))
                                            {
                                                if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendMemPoolTransactionByRange()
                                                {
                                                    ListTransaction = listMemPoolTransaction.GetList.Skip(apiPeerPacketAsMemPoolTransactionByRange.Start).Take(apiPeerPacketAsMemPoolTransactionByRange.End).ToList(),
                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond

                                                }, ClassPeerApiPacketResponseEnum.SEND_MEMPOOL_TRANSACTION_COUNT_BY_BLOCK_HEIGHT)))
                                                {
                                                    // Can't send packet.
                                                    return false;
                                                }
                                            }
                                        }
                                        else // Invalid Packet Timestamp.
                                            typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET_TIMESTAMP;
                                    }
                                    else // Invalid Packet.
                                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                                }
                                break;
                            case ClassPeerApiPostPacketSendEnum.ASK_BLOCK_TRANSACTION_BY_RANGE:
                                {
                                    ClassApiPeerPacketAskBlockTransactionByRange apiPeerPacketAskBlockTransactionByRange = TryDeserializedPacketContent<ClassApiPeerPacketAskBlockTransactionByRange>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    if (apiPeerPacketAskBlockTransactionByRange != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketAskBlockTransactionByRange.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            if (ClassBlockchainStats.ContainsBlockHeight(apiPeerPacketAskBlockTransactionByRange.BlockHeight))
                                            {
                                                int blockTransactionCount = await ClassBlockchainStats.GetBlockTransactionCount(apiPeerPacketAskBlockTransactionByRange.BlockHeight, _cancellationTokenApiClient);

                                                if (blockTransactionCount > apiPeerPacketAskBlockTransactionByRange.Start &&
                                                    blockTransactionCount >= apiPeerPacketAskBlockTransactionByRange.End)
                                                {
                                                    using (var disposableBlockTransactionList = await ClassBlockchainStats.GetTransactionListFromBlockHeightTarget(apiPeerPacketAskBlockTransactionByRange.BlockHeight, true, _cancellationTokenApiClient))
                                                    {

                                                        if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendListBlockTransaction()
                                                        {
                                                            ListBlockTransaction = disposableBlockTransactionList.GetList.Values.Skip(apiPeerPacketAskBlockTransactionByRange.Start).Take(apiPeerPacketAskBlockTransactionByRange.End).ToList(),
                                                            PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond

                                                        }, ClassPeerApiPacketResponseEnum.SEND_BLOCK_TRANSACTION_BY_RANGE)))
                                                        {
                                                            // Can't send packet.
                                                            return false;
                                                        }
                                                    }
                                                }
                                                else
                                                    typeResponse = ClassPeerApiPacketResponseEnum.MAX_BLOCK_TRANSACTION_REACH;
                                            }
                                            else
                                                typeResponse = ClassPeerApiPacketResponseEnum.INVALID_BLOCK_HEIGHT;
                                        }
                                        else // Invalid Packet Timestamp.
                                            typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET_TIMESTAMP;
                                    }
                                    else // Invalid Packet.
                                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                                }
                                break;
                            case ClassPeerApiPostPacketSendEnum.ASK_BLOCK_TRANSACTION_BY_HASH_LIST:
                                {
                                    ClassApiPeerPacketAskBlockTransactionByHashList apiPeerPacketAskBlockTransactionByHashList = TryDeserializedPacketContent<ClassApiPeerPacketAskBlockTransactionByHashList>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    if (apiPeerPacketAskBlockTransactionByHashList != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketAskBlockTransactionByHashList.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            if (ClassBlockchainStats.ContainsBlockHeight(apiPeerPacketAskBlockTransactionByHashList.BlockHeight))
                                            {

                                                if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendListBlockTransaction()
                                                {
                                                    ListBlockTransaction = (await ClassBlockchainDatabase.BlockchainMemoryManagement.GetListBlockTransactionFromListTransactionHashAndHeight(apiPeerPacketAskBlockTransactionByHashList.ListTransactionHash,
                                                    apiPeerPacketAskBlockTransactionByHashList.BlockHeight, true, true, _cancellationTokenApiClient)).GetList,
                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond

                                                }, ClassPeerApiPacketResponseEnum.SEND_BLOCK_TRANSACTION_BY_HASH_LIST)))
                                                {
                                                    // Can't send packet.
                                                    return false;
                                                }

                                            }
                                        }
                                        else // Invalid Packet Timestamp.
                                            typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET_TIMESTAMP;
                                    }
                                    else // Invalid Packet.
                                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                                }
                                break;
                            case ClassPeerApiPostPacketSendEnum.PUSH_WALLET_TRANSACTION:
                                {
                                    ClassApiPeerPacketPushWalletTransaction apiPeerPacketPushWalletTransaction = TryDeserializedPacketContent<ClassApiPeerPacketPushWalletTransaction>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    bool responseSent = false;

                                    if (apiPeerPacketPushWalletTransaction != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketPushWalletTransaction.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            if (apiPeerPacketPushWalletTransaction.TransactionObject != null)
                                            {
                                                if (await ClassBlockchainStats.CheckTransaction(apiPeerPacketPushWalletTransaction.TransactionObject, null, false, null, _cancellationTokenApiClient, true) == ClassTransactionEnumStatus.VALID_TRANSACTION)
                                                {

                                                    using (DisposableDictionary<string, ClassTransactionEnumStatus> transactionStatus = await ClassPeerNetworkBroadcastFunction.AskMemPoolTxVoteToPeerListsAsync(_peerNetworkSettingObject.ListenApiIp, _apiServerOpenNatIp, _clientIp, new List<ClassTransactionObject>() { apiPeerPacketPushWalletTransaction.TransactionObject }, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenApiClient, true))
                                                    {

                                                        bool broadcastComplete = transactionStatus.ContainsKey(apiPeerPacketPushWalletTransaction.TransactionObject.TransactionHash) ?
                                                            transactionStatus[apiPeerPacketPushWalletTransaction.TransactionObject.TransactionHash] == ClassTransactionEnumStatus.VALID_TRANSACTION : false;


                                                        if (broadcastComplete)
                                                        {
                                                            ClassBlockTransactionInsertEnumStatus blockTransactionInsertStatus = ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INVALID;

                                                            if (!await ClassMemPoolDatabase.CheckTxHashExist(apiPeerPacketPushWalletTransaction.TransactionObject.TransactionHash, _cancellationTokenApiClient))
                                                                blockTransactionInsertStatus = await ClassBlockchainDatabase.InsertTransactionToMemPool(apiPeerPacketPushWalletTransaction.TransactionObject, true, false, true, _cancellationTokenApiClient);
                                                            else
                                                                blockTransactionInsertStatus = ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_HASH_ALREADY_EXIST;

                                                            responseSent = true;

                                                            if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendPushWalletTransactionResponse()
                                                            {
                                                                BlockTransactionInsertStatus = blockTransactionInsertStatus,
                                                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                            }, ClassPeerApiPacketResponseEnum.SEND_REPLY_WALLET_TRANSACTION_PUSHED)))
                                                            {
                                                                // Can't send packet.
                                                                return false;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (!responseSent)
                                    {
                                        if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendPushWalletTransactionResponse()
                                        {
                                            BlockTransactionInsertStatus = ClassBlockTransactionInsertEnumStatus.BLOCK_TRANSACTION_INVALID,
                                            PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                        }, ClassPeerApiPacketResponseEnum.SEND_REPLY_WALLET_TRANSACTION_PUSHED)))
                                        {
                                            // Can't send packet.
                                            return false;
                                        }
                                    }
                                }
                                break;
                            case ClassPeerApiPostPacketSendEnum.PUSH_MINING_SHARE:
                                {
                                    ClassApiPeerPacketSendMiningShare apiPeerPacketSendMiningShare = TryDeserializedPacketContent<ClassApiPeerPacketSendMiningShare>(apiPeerPacketObjectSend.PacketContentObjectSerialized);

                                    if (apiPeerPacketSendMiningShare != null)
                                    {
                                        if (ClassUtility.CheckPacketTimestamp(apiPeerPacketSendMiningShare.PacketTimestamp, _peerNetworkSettingObject.PeerApiMaxPacketDelay, _peerNetworkSettingObject.PeerApiMaxEarlierPacketDelay))
                                        {
                                            long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();

                                            if (lastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                                            {
                                                long previousBlockHeight = lastBlockHeight - 1;

                                                ClassBlockObject previousBlockObjectInformations = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockInformationDataStrategy(previousBlockHeight, _cancellationTokenApiClient);

                                                if (ClassBlockchainDatabase.BlockchainMemoryManagement[lastBlockHeight, _cancellationTokenApiClient].BlockStatus == ClassBlockEnumStatus.LOCKED)
                                                {
                                                    ClassMiningPoWaCEnumStatus miningPowShareStatus = ClassMiningPoWaCUtility.CheckPoWaCShare(BlockchainSetting.CurrentMiningPoWaCSettingObject(lastBlockHeight), apiPeerPacketSendMiningShare.MiningPowShareObject, ClassBlockchainDatabase.BlockchainMemoryManagement[lastBlockHeight, _cancellationTokenApiClient].BlockHeight, ClassBlockchainDatabase.BlockchainMemoryManagement[lastBlockHeight, _cancellationTokenApiClient].BlockHash, ClassBlockchainDatabase.BlockchainMemoryManagement[lastBlockHeight, _cancellationTokenApiClient].BlockDifficulty, previousBlockObjectInformations.TotalTransaction, previousBlockObjectInformations.BlockFinalHashTransaction, out _, out _);

                                                    switch (miningPowShareStatus)
                                                    {
                                                        // Unlock the current block if everything is okay with other peers.
                                                        case ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE:
                                                            {
                                                                ClassBlockEnumMiningShareVoteStatus miningShareVoteStatus = await ClassBlockchainDatabase.UnlockCurrentBlockAsync(lastBlockHeight, apiPeerPacketSendMiningShare.MiningPowShareObject, false, _peerNetworkSettingObject.ListenIp, _apiServerOpenNatIp, false, false, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenApiClient);

                                                                switch (miningShareVoteStatus)
                                                                {
                                                                    case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED:
                                                                        miningPowShareStatus = ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE;
                                                                        ClassPeerNetworkBroadcastFunction.BroadcastMiningShareAsync(_peerNetworkSettingObject.ListenIp, _apiServerOpenNatIp, _clientIp, apiPeerPacketSendMiningShare.MiningPowShareObject, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                                        break;
                                                                    case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND:
                                                                        // That's can happen sometimes when the broadcast of the share to other nodes is very fast and return back the data of the block unlocked to the synced data before to retrieve back every votes done.
                                                                        if (ClassMiningPoWaCUtility.ComparePoWaCShare(ClassBlockchainDatabase.BlockchainMemoryManagement[lastBlockHeight, _cancellationTokenApiClient].BlockMiningPowShareUnlockObject, apiPeerPacketSendMiningShare.MiningPowShareObject))
                                                                            miningPowShareStatus = ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE;
                                                                        else
                                                                            miningPowShareStatus = ClassMiningPoWaCEnumStatus.BLOCK_ALREADY_FOUND;
                                                                        break;
                                                                    case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOCONSENSUS:
                                                                        // That's can happen sometimes when the broadcast of the share to other nodes is very fast and return back the data of the block unlocked to the synced data before to retrieve back every votes done.
                                                                        if (ClassMiningPoWaCUtility.ComparePoWaCShare(ClassBlockchainDatabase.BlockchainMemoryManagement[lastBlockHeight, _cancellationTokenApiClient].BlockMiningPowShareUnlockObject, apiPeerPacketSendMiningShare.MiningPowShareObject))
                                                                            miningPowShareStatus = ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE;
                                                                        else
                                                                            miningPowShareStatus = ClassMiningPoWaCEnumStatus.BLOCK_ALREADY_FOUND;
                                                                        break;
                                                                    case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_INVALID_TIMESTAMP:
                                                                    case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED:
                                                                        miningPowShareStatus = ClassMiningPoWaCEnumStatus.INVALID_SHARE_DATA;
                                                                        break;
                                                                }
                                                            }
                                                            break;
                                                        case ClassMiningPoWaCEnumStatus.VALID_SHARE:
                                                            miningPowShareStatus = ClassMiningPoWaCEnumStatus.VALID_SHARE;
                                                            break;
                                                        default:
                                                            typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PUSH_MINING_SHARE;
                                                            break;
                                                    }

                                                    if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendMiningShareResponse()
                                                    {
                                                        MiningPoWShareStatus = miningPowShareStatus,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                    }, ClassPeerApiPacketResponseEnum.SEND_MINING_SHARE_RESPONSE)))
                                                    {
                                                        // Can't send packet.
                                                        return false;
                                                    }
                                                }
                                                else
                                                {
                                                    if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendMiningShareResponse()
                                                    {
                                                        MiningPoWShareStatus = ClassMiningPoWaCEnumStatus.BLOCK_ALREADY_FOUND,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                    }, ClassPeerApiPacketResponseEnum.SEND_MINING_SHARE_RESPONSE)))
                                                    {
                                                        // Can't send packet.
                                                        return false;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendMiningShareResponse()
                                                {
                                                    MiningPoWShareStatus = ClassMiningPoWaCEnumStatus.INVALID_BLOCK_HEIGHT,
                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                }, ClassPeerApiPacketResponseEnum.SEND_MINING_SHARE_RESPONSE)))
                                                {
                                                    // Can't send packet.
                                                    return false;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            // Invalid Packet.
                            default:
                                {
                                    ClassLog.WriteLine("Unknown packet type received: " + apiPeerPacketObjectSend.PacketType, ClassEnumLogLevelType.LOG_LEVEL_API_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                    typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                                }
                                break;
                        }
                    }
                }

                return await SendResponseType(typeResponse);
            }
            catch (Exception error)
            {
                ClassLog.WriteLine("Error to handle API Packet received. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_API_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                return false;
            }

        }

        /// <summary>
        /// Handle Get request packets sent by a client.
        /// </summary>
        /// <param name="packetReceived"></param>
        private async Task<bool> HandleApiClientGetPacket(string packetReceived)
        {
            try
            {
                ClassPeerApiPacketResponseEnum typeResponse = ClassPeerApiPacketResponseEnum.OK;

                switch (packetReceived)
                {
                    case ClassPeerApiEnumGetRequest.GetAlive:
                        {
                            if (!await SendResponseType(typeResponse))
                                return false;
                        }
                        break;
                    case ClassPeerApiEnumGetRequest.GetBlockTemplate:
                        {
                            long currentBlockHeight = ClassBlockchainStats.GetLastBlockHeight();
                            if (currentBlockHeight > 0)
                            {
                                if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendBlockTemplate()
                                {
                                    CurrentBlockHeight = currentBlockHeight,
                                    CurrentBlockDifficulty = ClassBlockchainDatabase.BlockchainMemoryManagement[currentBlockHeight, _cancellationTokenApiClient].BlockDifficulty,
                                    CurrentBlockHash = ClassBlockchainDatabase.BlockchainMemoryManagement[currentBlockHeight, _cancellationTokenApiClient].BlockHash,
                                    LastTimestampBlockFound = ClassBlockchainDatabase.BlockchainMemoryManagement[currentBlockHeight, _cancellationTokenApiClient].TimestampCreate,
                                    CurrentMiningPoWaCSetting = BlockchainSetting.CurrentMiningPoWaCSettingObject(currentBlockHeight),
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                }, ClassPeerApiPacketResponseEnum.SEND_BLOCK_TEMPLATE)))
                                    return false;
                            }
                            else
                                typeResponse = ClassPeerApiPacketResponseEnum.OK;
                        }
                        break;
                    case ClassPeerApiEnumGetRequest.GetNetworkStats:
                        {
                            if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendNetworkStats()
                            {
                                BlockchainNetworkStatsObject = ClassBlockchainStats.BlockchainNetworkStatsObject,
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            }, ClassPeerApiPacketResponseEnum.SEND_NETWORK_STATS)))
                                return false;
                        }
                        break;
                    case ClassPeerApiEnumGetRequest.GetLastBlockHeightUnlocked:
                        {
                            if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendBlockHeight()
                            {
                                BlockHeight = await ClassBlockchainStats.GetLastBlockHeightUnlocked(_cancellationTokenApiClient),
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            }, ClassPeerApiPacketResponseEnum.SEND_LAST_BLOCK_HEIGHT_UNLOCKED)))
                                return false;
                        }
                        break;
                    case ClassPeerApiEnumGetRequest.GetLastBlockHeight:
                        {
                            if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendBlockHeight()
                            {
                                BlockHeight = ClassBlockchainStats.GetLastBlockHeight(),
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            }, ClassPeerApiPacketResponseEnum.SEND_LAST_BLOCK_HEIGHT)))
                                return false;
                        }
                        break;
                    case ClassPeerApiEnumGetRequest.GetLastBlockHeightTransactionConfirmation:
                        {
                            if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendBlockHeight()
                            {
                                BlockHeight = await ClassBlockchainStats.GetLastBlockHeightTransactionConfirmationDone(_cancellationTokenApiClient),
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            }, ClassPeerApiPacketResponseEnum.SEND_LAST_BLOCK_HEIGHT_TRANSACTION_CONFIRMATION)))
                                return false;
                        }
                        break;
                    case ClassPeerApiEnumGetRequest.GetMemPoolTransactionCount:
                        {
                            if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendMemPoolTransactionCount()
                            {
                                TransactionCount = ClassMemPoolDatabase.GetCountMemPoolTx,
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            }, ClassPeerApiPacketResponseEnum.SEND_MEMPOOL_TRANSACTION_COUNT)))
                                return false;
                        }
                        break;
                    case ClassPeerApiEnumGetRequest.GetMemPoolBlockHeights:
                        {
                            using (DisposableList<long> listMemPoolBlockHeights = await ClassMemPoolDatabase.GetMemPoolListBlockHeight(_cancellationTokenApiClient))
                            {
                                if (!await SendApiResponse(BuildPacketResponse(new ClassApiPeerPacketSendMemPoolBlockHeights()
                                {
                                    ListBlockHeights = listMemPoolBlockHeights.GetList,
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                }, ClassPeerApiPacketResponseEnum.SEND_MEMPOOL_TRANSACTION_COUNT)))
                                    return false;
                            }
                        }
                        break;
                    case ClassPeerApiEnumGetRequest.GetBlockchainExplorer:
                        {
                            if (!await SendApiResponse(GetBlockchainExplorerContent(), "text/html; charset=utf-8"))
                                return false;
                        }
                        break;
                    default:
                        typeResponse = ClassPeerApiPacketResponseEnum.INVALID_PACKET;
                        break;
                }

                return await SendResponseType(typeResponse);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Send a response type.
        /// </summary>
        /// <param name="typeResponse"></param>
        /// <returns></returns>
        private async Task<bool> SendResponseType(ClassPeerApiPacketResponseEnum typeResponse)
        {
            if (!await SendApiResponse(BuildPacketResponseStatus(typeResponse)))
                return false;

            if (typeResponse != ClassPeerApiPacketResponseEnum.OK)
                return false;

            return true;
        }

        /// <summary>
        /// Send an API Response to the client.
        /// </summary>
        /// <param name="packetToSend"></param>
        /// <param name="htmlContentType">Html packet content, default json.</param>
        /// <returns></returns>
        private async Task<bool> SendApiResponse(string packetToSend, string htmlContentType = "text/json")
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(@"HTTP/1.1 200 OK");
            builder.AppendLine(@"Content-Type: " + htmlContentType);
            builder.AppendLine(@"Content-Length: " + packetToSend.Length);
            builder.AppendLine(@"Access-Control-Allow-Origin: *");
            builder.AppendLine(@"");
            builder.AppendLine(@"" + packetToSend);

            bool sendResult;

            try
            {
                sendResult = await _clientSocket.TrySendSplittedPacket(ClassUtility.GetByteArrayFromStringUtf8(builder.ToString()), _cancellationTokenApiClient, _peerNetworkSettingObject.PeerMaxPacketSplitedSendSize);
            }
            catch
            {
                sendResult = false;
            }

            PacketResponseSent = sendResult;

            // Clean up.
            builder.Clear();

            return sendResult;
        }

        #endregion

        #region Other functions used for packets received to handle.

        /// <summary>
        /// Try to deserialize the packet content object received.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packetContentObject"></param>
        /// <returns></returns>
        private T TryDeserializedPacketContent<T>(string packetContentObject)
        {
            if (ClassUtility.TryDeserialize(packetContentObject, out T apiPeerPacketDeserialized))
                return apiPeerPacketDeserialized;

            return default;
        }

        /// <summary>
        /// Serialise packet response to send.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packetResponse"></param>
        /// <param name="packetResponseType"></param>
        /// <returns></returns>
        private string BuildPacketResponse<T>(T packetResponse, ClassPeerApiPacketResponseEnum packetResponseType)
        {
            return ClassUtility.SerializeData(new ClassApiPeerPacketObjetReceive()
            {
                PacketType = packetResponseType,
                PacketObjectSerialized = ClassUtility.SerializeData(packetResponse)
            });
        }

        /// <summary>
        /// Serialise packet response type to send.
        /// </summary>
        /// <param name="packetResponseType"></param>
        /// <returns></returns>
        private string BuildPacketResponseStatus(ClassPeerApiPacketResponseEnum packetResponseType)
        {
            return ClassUtility.SerializeData(new ClassApiPeerPacketObjetReceive()
            {
                PacketType = packetResponseType,
                PacketObjectSerialized = null
            });
        }

        /// <summary>
        /// Return the blockchain explorer content string.
        /// </summary>
        /// <returns></returns>
        private string GetBlockchainExplorerContent()
        {
            return ClassApiBlockchainExplorerHtmlContent.Content.Replace(ClassApiBlockchainExplorerHtmlContent.ContentCoinName, BlockchainSetting.CoinName)
                .Replace(ClassApiBlockchainExplorerHtmlContent.ContentApiHost, _peerNetworkSettingObject.ListenApiIp)
                .Replace(ClassApiBlockchainExplorerHtmlContent.ContentApiPort, _peerNetworkSettingObject.ListenApiPort.ToString());
        }

        #endregion
    }
}
