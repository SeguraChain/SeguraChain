using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.MemPool.Database;
using SeguraChain_Lib.Blockchain.Mining.Enum;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Sovereign.Database;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Instance.Node.Network.Database.Manager;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Manager;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.Model;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Request;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response.Enum;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ServerSync.Client.Enum;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ServerSync.Client
{
    /// <summary>
    /// Object dedicated to peer client tcp received on listening.
    /// </summary>
    public class ClassPeerNetworkClientServerObject : IDisposable
    {
        private Socket _clientSocket;
        public CancellationTokenSource CancellationTokenHandlePeerConnection;
        private CancellationTokenSource _cancellationTokenClientCheckConnectionPeer;
        private CancellationTokenSource _cancellationTokenAccessData;
        private CancellationTokenSource _cancellationTokenListenPeerPacket;

        private ClassPeerNetworkSettingObject _peerNetworkSettingObject;
        private ClassPeerFirewallSettingObject _peerFirewallSettingObject;
        private string _peerClientIp;
        private string _peerServerOpenNatIp;
        private string _peerUniqueId;


        /// <summary>
        /// Network status and data.
        /// </summary>
        public bool ClientPeerConnectionStatus;
        public long ClientPeerLastPacketReceived;
        private bool _clientPeerPacketReceivedStatus;
        private bool _clientAskDisconnection;
        private bool _onSendingPacketResponse;

        /// <summary>
        /// About MemPool broadcast mode.
        /// </summary>
        private bool _enableMemPoolBroadcastClientMode;
        private bool _onSendingMemPoolTransaction;
        private bool _onWaitingMemPoolTransactionConfirmationReceived;
        private Dictionary<long, int> _listMemPoolBroadcastBlockHeight;
        private DisposableList<ClassReadPacketSplitted> listPacketReceived;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="clientSocket">The tcp client object.</param>
        /// <param name="cancellationTokenHandlePeerConnection">The cancellation token who permit to cancel the handling of the incoming connection.</param>
        /// <param name="peerClientIp">The peer client IP.</param>
        /// <param name="peerServerOpenNatIp">The public ip of the server.</param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public ClassPeerNetworkClientServerObject(Socket clientSocket, CancellationTokenSource cancellationTokenHandlePeerConnection, string peerClientIp, string peerServerOpenNatIp, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            ClientPeerConnectionStatus = true;
            ClientPeerLastPacketReceived = TaskManager.TaskManager.CurrentTimestampSecond;
            CancellationTokenHandlePeerConnection = cancellationTokenHandlePeerConnection;
            _clientSocket = clientSocket;
            _peerNetworkSettingObject = peerNetworkSettingObject;
            _peerFirewallSettingObject = peerFirewallSettingObject;
            _peerClientIp = peerClientIp;
            _peerServerOpenNatIp = peerServerOpenNatIp;
            _clientPeerPacketReceivedStatus = false;
            _listMemPoolBroadcastBlockHeight = new Dictionary<long, int>();
            _cancellationTokenClientCheckConnectionPeer = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenHandlePeerConnection.Token);
            _cancellationTokenListenPeerPacket = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenHandlePeerConnection.Token);
            _cancellationTokenAccessData = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenListenPeerPacket.Token, _cancellationTokenClientCheckConnectionPeer.Token);
        }

        #region Dispose functions

        private bool _disposed;

        ~ClassPeerNetworkClientServerObject()
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
            if (_disposed && !ClientPeerConnectionStatus)
                return;

            if (disposing)
                ClosePeerClient(false);

            _disposed = true;
        }
        #endregion

        #region Manage Peer Client connection.


        /// <summary>
        /// Check peer client connection.
        /// </summary>
        private async Task CheckPeerClientAsync()
        {
            while (ClientPeerConnectionStatus)
            {
                try
                {

                    if (!ClientPeerConnectionStatus)
                        break;

                    if (!_onSendingPacketResponse && !(_enableMemPoolBroadcastClientMode || _onSendingMemPoolTransaction))
                    {
                        // If any packet are received after the delay, the function close the peer client connection to listen.
                        if (ClientPeerLastPacketReceived + _peerNetworkSettingObject.PeerMaxDelayConnection < TaskManager.TaskManager.CurrentTimestampSecond)
                        {
                            // On this case, insert invalid attempt of connection.
                            if (!_clientPeerPacketReceivedStatus)
                                ClassPeerCheckManager.InputPeerClientNoPacketConnectionOpened(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);

                            break;
                        }
                    }

                    if (!ClassUtility.SocketIsConnected(_clientSocket))
                        break;

                    if (_peerFirewallSettingObject.PeerEnableFirewallLink)
                    {
                        if (!ClassPeerFirewallManager.CheckClientIpStatus(_peerClientIp))
                            break;
                    }

                    await Task.Delay(1000);


                }
                catch
                {
                    break;
                }
            }

            ClosePeerClient(true);
        }

        /// <summary>
        /// Close peer client connection received.
        /// </summary>
        public void ClosePeerClient(bool fromCheckConnection)
        {
            ClientPeerConnectionStatus = false;

            // Clean up.
            _listMemPoolBroadcastBlockHeight?.Clear();
            listPacketReceived?.Clear();

            try
            {
                if (_cancellationTokenListenPeerPacket != null)
                {
                    if (!_cancellationTokenListenPeerPacket.IsCancellationRequested)
                        _cancellationTokenListenPeerPacket.Cancel();
                }
            }
            catch
            {
                // Ignored.
            }

            if (!fromCheckConnection)
            {
                try
                {
                    if (_cancellationTokenClientCheckConnectionPeer != null)
                    {
                        if (!_cancellationTokenClientCheckConnectionPeer.IsCancellationRequested)
                            _cancellationTokenClientCheckConnectionPeer.Cancel();
                    }
                }
                catch
                {
                    // Ignored.
                }
            }

            ClassUtility.CloseSocket(_clientSocket);

        }

        #endregion

        #region Listen peer client packets received.

        /// <summary>
        /// Listen packet received from peer.
        /// </summary>
        /// <returns></returns>
        public void HandlePeerClient()
        {
            // Launch a task to handle packets received.
            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                using (listPacketReceived = new DisposableList<ClassReadPacketSplitted>())
                {
                    listPacketReceived.Add(new ClassReadPacketSplitted());

                    byte[] packetBufferOnReceive = new byte[_peerNetworkSettingObject.PeerMaxPacketBufferSize];

                    try
                    {
                        using (NetworkStream networkStream = new NetworkStream(_clientSocket))
                        {

                            int packetLength = 0;

                            while (ClientPeerConnectionStatus && !_clientAskDisconnection && (packetLength = await networkStream.ReadAsync(packetBufferOnReceive, 0, packetBufferOnReceive.Length, _cancellationTokenListenPeerPacket.Token)) > 0)
                            {

                                try
                                {


                                    ClientPeerLastPacketReceived = TaskManager.TaskManager.CurrentTimestampSecond;

                                    #region Handle packet content received, split the data.

                                    string packetData = packetBufferOnReceive.GetStringFromByteArrayUtf8().Replace("\0", "");

                                    if (packetData.Contains(ClassPeerPacketSetting.PacketPeerSplitSeperator))
                                    {
                                        int countSeperator = packetData.Count(x => x == ClassPeerPacketSetting.PacketPeerSplitSeperator);

                                        string[] splitPacketData = packetData.Split(new[] { ClassPeerPacketSetting.PacketPeerSplitSeperator }, StringSplitOptions.None);

                                        int completed = 0;
                                        foreach (string data in splitPacketData)
                                        {
                                            string dataFormatted = data.Replace(ClassPeerPacketSetting.PacketPeerSplitSeperator.ToString(), "");

                                            if (dataFormatted.IsNullOrEmpty(false, out _) || dataFormatted.Length == 0 || !ClassUtility.CheckBase64String(dataFormatted))
                                                continue;

                                            listPacketReceived[listPacketReceived.Count - 1].Packet += dataFormatted;

                                            if (completed < countSeperator)
                                            {
                                                listPacketReceived[listPacketReceived.Count - 1].Complete = true;
                                                listPacketReceived.Add(new ClassReadPacketSplitted());
                                            }

                                            completed++;
                                        }
                                    }
                                    else
                                    {
                                        string data = packetData.Replace(ClassPeerPacketSetting.PacketPeerSplitSeperator.ToString(), "");

                                        if (data.IsNullOrEmpty(false, out _) || data.Length == 0 || !ClassUtility.CheckBase64String(data))
                                            continue;

                                        listPacketReceived[listPacketReceived.Count - 1].Packet += data;
                                    }

                                    #endregion


                                    if (listPacketReceived.GetList.Count(x => x.Complete) == 0)
                                        continue;

                                    for (int i = 0; i < listPacketReceived.Count; i++)
                                    {
                                        if (!listPacketReceived[i].Complete || listPacketReceived[i].Packet.Length == 0)
                                            continue;

                                        bool failed = false;
                                        byte[] base64Packet = null;

                                        if (ClassUtility.CheckBase64String(listPacketReceived[i].Packet))
                                        {
                                            try
                                            {
                                                base64Packet = Convert.FromBase64String(listPacketReceived[i].Packet);
                                                if (base64Packet == null || base64Packet.Length == 0)
                                                    failed = true;
                                            }
                                            catch
                                            {
                                                failed = true;
                                            }
                                        }
                                        else failed = true;

                                        listPacketReceived[i].Packet.Clear();

                                        if (failed || base64Packet == null)
                                            continue;

                                        TaskManager.TaskManager.InsertTask(new Action(async () =>
                                        {
                                            _onSendingPacketResponse = true;

                                            switch (await HandlePacket(base64Packet))
                                            {
                                                case ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_TYPE_PACKET:
                                                case ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET:
                                                    {
                                                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                        ClientPeerConnectionStatus = false;
                                                    }
                                                    break;
                                                case ClassPeerNetworkClientServerHandlePacketEnumStatus.EXCEPTION_PACKET:
                                                case ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET:
                                                    {
                                                        ClassPeerCheckManager.InputPeerClientAttemptConnect(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                        ClientPeerConnectionStatus = false;
                                                    }
                                                    break;
                                                case ClassPeerNetworkClientServerHandlePacketEnumStatus.VALID_PACKET:
                                                    {
                                                        ClassPeerCheckManager.InputPeerClientValidPacket(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject);
                                                        if (_clientAskDisconnection)
                                                            ClientPeerConnectionStatus = _clientAskDisconnection;
                                                    }
                                                    break;
                                            }

                                            _onSendingPacketResponse = false;

                                        }), 0, _cancellationTokenListenPeerPacket, null);
                                    }

                                    listPacketReceived.GetList.RemoveAll(x => x.Complete);


                                    if (_clientAskDisconnection || !ClientPeerConnectionStatus)
                                    {
                                        ClientPeerConnectionStatus = false;
                                        break;
                                    }
                                }
                                catch
                                {
                                    ClientPeerConnectionStatus = false;
                                    break;
                                }
                            }

                        }
                    }
                    catch
                    {
                        // Ignored socket exception.
                    }

                }

                ClosePeerClient(false);

            }), 0, _cancellationTokenListenPeerPacket, _clientSocket);


            // Launch a task for check the peer connection.
            TaskManager.TaskManager.InsertTask(new Action(async () => await CheckPeerClientAsync()), 0, _cancellationTokenClientCheckConnectionPeer, null);

        }

        #endregion

        #region Handle peer packet received.

        /// <summary>
        /// Handle decrypted packet. (Usually used for send auth keys for register a new peer).
        /// </summary>
        /// <param name="packet">Packet received to handle.</param>
        /// <returns>Return the status of the handle of the packet.</returns>
        private async Task<ClassPeerNetworkClientServerHandlePacketEnumStatus> HandlePacket(byte[] packet)
        {

            try
            {
                ClassPeerPacketSendObject packetSendObject = new ClassPeerPacketSendObject(packet, out bool status);

                if (!status)
                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_TYPE_PACKET;

                _clientPeerPacketReceivedStatus = true;

                #region Update peer activity.

                _peerUniqueId = packetSendObject.PacketPeerUniqueId;

                if (_peerUniqueId.IsNullOrEmpty(false, out _))
                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                if (ClassPeerDatabase.ContainsPeer(_peerClientIp, _peerUniqueId))
                    ClassPeerCheckManager.UpdatePeerClientLastPacketReceived(_peerClientIp, _peerUniqueId, packetSendObject.PeerLastTimestampSignatureWhitelist);
                // Not allow other packets until to get the node initialized.
                else
                {
                    if (packetSendObject.PacketOrder != ClassPeerEnumPacketSend.ASK_PEER_AUTH_KEYS)
                    {
                        await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, string.Empty, 0)
                        {
                            PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET,
                            PacketContent = string.Empty,
                        }, false);

                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                    }
                }


                #endregion

                #region Check packet signature if necessary.

                bool peerIgnorePacketSignature = packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_PEER_AUTH_KEYS ||
                                                 packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_KEEP_ALIVE ||
                                                 packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_MEM_POOL_BROADCAST_MODE;

                if (!peerIgnorePacketSignature)
                {
                    if (!ClassPeerCheckManager.CheckPeerClientWhitelistStatus(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject))
                        ClassPeerCheckManager.InputPeerClientValidPacket(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject);
                    else
                    {
                        if (!CheckContentPacketSignaturePeer(packetSendObject))
                        {
                            await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE,
                                PacketContent = string.Empty,
                            }, false);

                            return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                        }
                        else
                            ClassPeerCheckManager.InputPeerClientValidPacket(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject);
                    }
                    
                }

                #endregion

                #region Packet type if the broadcast client mode is enabled.

                if (_enableMemPoolBroadcastClientMode)
                {
                    bool invalidPacketBroadcast = true;

                    if (packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_KEEP_ALIVE ||
                        packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_DISCONNECT_REQUEST ||
                        packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE ||
                        packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE ||
                        packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_VOTE ||
                        packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BROADCAST_CONFIRMATION_RECEIVED)
                    {
                        invalidPacketBroadcast = false;
                    }

                    if (invalidPacketBroadcast)
                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_TYPE_PACKET;
                }
                else
                {
                    if (packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE ||
                        packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE ||
                        packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BROADCAST_CONFIRMATION_RECEIVED)
                    {
                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_TYPE_PACKET;
                    }
                }

                #endregion

                switch (packetSendObject.PacketOrder)
                {
                    case ClassPeerEnumPacketSend.ASK_PEER_AUTH_KEYS: // ! This packet type is not encrypted because we exchange unique encryption keys, public key, numeric public key to the node who ask them. !
                        {
                            ClassPeerPacketSendAskPeerAuthKeys packetSendPeerAuthKeysObject = JsonConvert.DeserializeObject<ClassPeerPacketSendAskPeerAuthKeys>(packetSendObject.PacketContent);

                            if (!ClassUtility.CheckPacketTimestamp(packetSendPeerAuthKeysObject.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (!ClassPeerKeysManager.UpdatePeerInternalKeys(_peerClientIp, packetSendPeerAuthKeysObject.PeerPort, _peerUniqueId, _cancellationTokenAccessData, _peerNetworkSettingObject, true))
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE,
                                    PacketContent = string.Empty,
                                }, false);

                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                            }

                            if (!ClassPeerKeysManager.UpdatePeerKeysReceivedNetworkServer(_peerClientIp, _peerUniqueId, packetSendPeerAuthKeysObject, _cancellationTokenAccessData))
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE,
                                    PacketContent = string.Empty,
                                }, false);

                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                            }

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_PEER_AUTH_KEYS,
                                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendPeerAuthKeys()
                                {
                                    AesEncryptionIv = ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPacketEncryptionKeyIv,
                                    AesEncryptionKey = ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPacketEncryptionKey,
                                    PublicKey = ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey,
                                    NumericPublicKey = _peerNetworkSettingObject.PeerNumericPublicKey,
                                    PeerPort = _peerNetworkSettingObject.ListenPort,
                                    PeerApiPort = _peerNetworkSettingObject.ListenApiPort,
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                })
                            }, false))
                            {
                                ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_PEER_LIST:
                        {
                            ClassPeerPacketSendAskPeerList packetSendAskPeerList = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskPeerList>(packetSendObject.PacketContent);

                            if (packetSendAskPeerList == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskPeerList.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            Dictionary<string, Tuple<int, string>> listPeerInfo = ClassPeerDatabase.GetPeerListInfo(_peerClientIp);

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_PEER_LIST,
                                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendPeerList()
                                {
                                    PeerIpList = new List<string>(listPeerInfo.Keys),
                                    PeerPortList = new List<int>(listPeerInfo.Values.Select(x => x.Item1)),
                                    PeerUniqueIdList = new List<string>(listPeerInfo.Values.Select(x => x.Item2)),
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                })
                            }, true))
                            {
                                ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_LIST_SOVEREIGN_UPDATE:
                        {
                            ClassPeerPacketSendAskListSovereignUpdate packetSendAskListSovereignUpdate = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskListSovereignUpdate>(packetSendObject.PacketContent);

                            if (packetSendAskListSovereignUpdate == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskListSovereignUpdate.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            ClassPeerPacketSendListSovereignUpdate packetContent = new ClassPeerPacketSendListSovereignUpdate()
                            {
                                SovereignUpdateHashList = ClassSovereignUpdateDatabase.GetSovereignUpdateListHash().GetList,
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            };

                            SignPacketWithNumericPrivateKey(packetContent, out string numericHash, out string numericSignature);
                            packetContent.PacketNumericHash = numericHash;
                            packetContent.PacketNumericSignature = numericSignature;

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_LIST_SOVEREIGN_UPDATE,
                                PacketContent = ClassUtility.SerializeData(packetContent)
                            }, true))
                            {
                                ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_SOVEREIGN_UPDATE_FROM_HASH:
                        {
                            ClassPeerPacketSendAskSovereignUpdateFromHash packetSendAskSovereignUpdateFromHash = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskSovereignUpdateFromHash>(packetSendObject.PacketContent);

                            if (packetSendAskSovereignUpdateFromHash == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskSovereignUpdateFromHash.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (packetSendAskSovereignUpdateFromHash.SovereignUpdateHash.IsNullOrEmpty(false, out _))
                            {
                                ClassLog.WriteLine("Sovereign Update Hash received from peer: " + _peerClientIp + " is empty.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                            }

                            if (!ClassSovereignUpdateDatabase.DictionarySovereignUpdateObject.ContainsKey(packetSendAskSovereignUpdateFromHash.SovereignUpdateHash))
                            {
                                ClassLog.WriteLine("Sovereign Update Hash received from peer: " + _peerClientIp + " not exist on registered updates. Hash received: " + packetSendAskSovereignUpdateFromHash.SovereignUpdateHash, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                            }


                            // Build numeric signature.
                            SignPacketWithNumericPrivateKey(ClassSovereignUpdateDatabase.DictionarySovereignUpdateObject[packetSendAskSovereignUpdateFromHash.SovereignUpdateHash], out string hashNumeric, out string signatureNumeric);

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_SOVEREIGN_UPDATE_FROM_HASH,
                                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendSovereignUpdateFromHash()
                                {
                                    SovereignUpdateObject = ClassSovereignUpdateDatabase.DictionarySovereignUpdateObject[packetSendAskSovereignUpdateFromHash.SovereignUpdateHash],
                                    PacketNumericHash = hashNumeric,
                                    PacketNumericSignature = signatureNumeric,
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                })
                            }, true))
                            {
                                ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_NETWORK_INFORMATION:
                        {
                            ClassPeerPacketSendAskNetworkInformation packetSendAskNetworkInformation = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskNetworkInformation>(packetSendObject.PacketContent);

                            if (packetSendAskNetworkInformation == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskNetworkInformation.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;


                            long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();

                            if (lastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                            {

                                ClassBlockObject blockObject = await ClassBlockchainStats.GetBlockInformationData(lastBlockHeight, _cancellationTokenAccessData);

                                if (blockObject != null)
                                {
                                    ClassPeerPacketSendNetworkInformation packetSendNetworkInformation = new ClassPeerPacketSendNetworkInformation()
                                    {
                                        CurrentBlockHeight = lastBlockHeight,
                                        LastBlockHeightUnlocked = blockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED ? lastBlockHeight : lastBlockHeight == BlockchainSetting.GenesisBlockHeight ? lastBlockHeight : lastBlockHeight - 1,
                                        CurrentBlockDifficulty = blockObject.BlockDifficulty,
                                        CurrentBlockHash = blockObject.BlockHash,
                                        TimestampBlockCreate = blockObject.TimestampCreate,
                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                    };

                                    SignPacketWithNumericPrivateKey(packetSendNetworkInformation, out string hashNumeric, out string signatureNumeric);

                                    packetSendNetworkInformation.PacketNumericHash = hashNumeric;
                                    packetSendNetworkInformation.PacketNumericSignature = signatureNumeric;

                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_NETWORK_INFORMATION,
                                        PacketContent = ClassUtility.SerializeData(packetSendNetworkInformation)
                                    }, true))
                                    {
                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                    }
                                }
                            }
                            // No block synced on the node.
                            else
                            {
                                ClassPeerPacketSendNetworkInformation packetSendNetworkInformation = new ClassPeerPacketSendNetworkInformation()
                                {
                                    CurrentBlockHeight = lastBlockHeight,
                                    LastBlockHeightUnlocked = 0,
                                    CurrentBlockDifficulty = 0,
                                    CurrentBlockHash = string.Empty,
                                    TimestampBlockCreate = 0,
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                };

                                SignPacketWithNumericPrivateKey(packetSendNetworkInformation, out string hashNumeric, out string signatureNumeric);

                                packetSendNetworkInformation.PacketNumericHash = hashNumeric;
                                packetSendNetworkInformation.PacketNumericSignature = signatureNumeric;

                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_NETWORK_INFORMATION,
                                    PacketContent = ClassUtility.SerializeData(packetSendNetworkInformation)
                                }, true))
                                {
                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                }
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_BLOCK_DATA:
                        {
                            ClassPeerPacketSendAskBlockData packetSendAskBlockData = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskBlockData>(packetSendObject.PacketContent);

                            if (packetSendAskBlockData == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskBlockData.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (ClassBlockchainStats.ContainsBlockHeight(packetSendAskBlockData.BlockHeight))
                            {
                                ClassBlockObject blockObject = await ClassBlockchainStats.GetBlockInformationData(packetSendAskBlockData.BlockHeight, _cancellationTokenAccessData);

                                if (blockObject != null)
                                {
                                    blockObject.DeepCloneBlockObject(false, out ClassBlockObject blockObjectCopy);
                                    blockObjectCopy.BlockTransactionFullyConfirmed = false;
                                    blockObjectCopy.BlockUnlockValid = false;
                                    blockObjectCopy.BlockNetworkAmountConfirmations = 0;
                                    blockObjectCopy.BlockSlowNetworkAmountConfirmations = 0;
                                    blockObjectCopy.BlockLastHeightTransactionConfirmationDone = 0;
                                    blockObjectCopy.BlockTotalTaskTransactionConfirmationDone = 0;
                                    blockObjectCopy.BlockTransactionConfirmationCheckTaskDone = false;
                                    blockObjectCopy.BlockTotalTaskTransactionConfirmationDone = 0;
                                    blockObjectCopy.BlockTransactionCountInSync = blockObject.TotalTransaction;
                                    blockObjectCopy.TotalCoinConfirmed = 0;
                                    blockObjectCopy.TotalCoinPending = 0;
                                    blockObjectCopy.TotalFee = 0;
                                    blockObjectCopy.TotalTransactionConfirmed = 0;
                                    blockObjectCopy.TotalTransaction = blockObject.TotalTransaction;

                                    ClassPeerPacketSendBlockData packetSendBlockData = new ClassPeerPacketSendBlockData()
                                    {
                                        BlockData = blockObjectCopy,
                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                    };

                                    if (blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight && blockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                        packetSendBlockData.BlockData.TimestampFound = blockObject.BlockMiningPowShareUnlockObject.Timestamp;


                                    SignPacketWithNumericPrivateKey(packetSendBlockData, out string hashNumeric, out string signatureNumeric);

                                    packetSendBlockData.PacketNumericHash = hashNumeric;
                                    packetSendBlockData.PacketNumericSignature = signatureNumeric;

                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_BLOCK_DATA,
                                        PacketContent = ClassUtility.SerializeData(packetSendBlockData)
                                    }, true))
                                    {
                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                    }
                                }
                                else
                                {
                                    await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                        PacketContent = string.Empty,
                                    }, false);
                                }
                            }
                            else
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                    PacketContent = string.Empty,
                                }, false);
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_BLOCK_HEIGHT_INFORMATION:
                        {
                            ClassPeerPacketSendAskBlockHeightInformation packetSendAskBlockHeightInformation = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskBlockHeightInformation>(packetSendObject.PacketContent);

                            if (packetSendAskBlockHeightInformation == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskBlockHeightInformation.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (ClassBlockchainStats.ContainsBlockHeight(packetSendAskBlockHeightInformation.BlockHeight))
                            {


                                ClassBlockObject blockObject = await ClassBlockchainStats.GetBlockInformationData(packetSendAskBlockHeightInformation.BlockHeight, _cancellationTokenAccessData);

                                if (blockObject != null)
                                {

                                    ClassPeerPacketSendBlockHeightInformation packetSendBlockHeightInformation = new ClassPeerPacketSendBlockHeightInformation()
                                    {
                                        BlockHeight = packetSendAskBlockHeightInformation.BlockHeight,
                                        BlockFinalTransactionHash = blockObject.BlockFinalHashTransaction,
                                        BlockHash = blockObject.BlockHash,
                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                    };


                                    SignPacketWithNumericPrivateKey(packetSendBlockHeightInformation, out string hashNumeric, out string signatureNumeric);

                                    packetSendBlockHeightInformation.PacketNumericHash = hashNumeric;
                                    packetSendBlockHeightInformation.PacketNumericSignature = signatureNumeric;

                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_BLOCK_HEIGHT_INFORMATION,
                                        PacketContent = ClassUtility.SerializeData(packetSendBlockHeightInformation)
                                    }, true))
                                    {
                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                    }
                                }
                                else
                                {
                                    await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                        PacketContent = string.Empty,
                                    }, false);
                                }
                            }
                            else
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                    PacketContent = string.Empty,
                                }, false);
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_BLOCK_TRANSACTION_DATA:
                        {
                            ClassPeerPacketSendAskBlockTransactionData packetSendAskBlockTransactionData = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskBlockTransactionData>(packetSendObject.PacketContent);

                            if (packetSendAskBlockTransactionData == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskBlockTransactionData.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (ClassBlockchainStats.ContainsBlockHeight(packetSendAskBlockTransactionData.BlockHeight))
                            {
                                int blockTransactionCount = await ClassBlockchainStats.GetBlockTransactionCount(packetSendAskBlockTransactionData.BlockHeight, _cancellationTokenAccessData);

                                if (blockTransactionCount > packetSendAskBlockTransactionData.TransactionId)
                                {
                                    using (DisposableSortedList<string, ClassBlockTransaction> transactionList = await ClassBlockchainStats.GetTransactionListFromBlockHeightTarget(packetSendAskBlockTransactionData.BlockHeight, true, _cancellationTokenAccessData))
                                    {

                                        if (transactionList.Count > packetSendAskBlockTransactionData.TransactionId)
                                        {

                                            ClassPeerPacketSendBlockTransactionData packetSendBlockTransactionData = new ClassPeerPacketSendBlockTransactionData()
                                            {
                                                BlockHeight = packetSendAskBlockTransactionData.BlockHeight,
                                                TransactionObject = transactionList.GetList.ElementAt(packetSendAskBlockTransactionData.TransactionId).Value.TransactionObject,
                                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                            };

                                            SignPacketWithNumericPrivateKey(packetSendBlockTransactionData, out string hashNumeric, out string signatureNumeric);

                                            packetSendBlockTransactionData.PacketNumericHash = hashNumeric;
                                            packetSendBlockTransactionData.PacketNumericSignature = signatureNumeric;

                                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                            {
                                                PacketOrder = ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA,
                                                PacketContent = ClassUtility.SerializeData(packetSendBlockTransactionData)
                                            }, true))
                                            {
                                                ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET,
                                        PacketContent = string.Empty,
                                    }, false);
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                                }
                            }
                            else
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                    PacketContent = string.Empty,
                                }, false);
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_BLOCK_TRANSACTION_DATA_BY_RANGE:
                        {
                            ClassPeerPacketSendAskBlockTransactionDataByRange packetSendAskBlockTransactionDataByRange = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskBlockTransactionDataByRange>(packetSendObject.PacketContent);

                            if (packetSendAskBlockTransactionDataByRange == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskBlockTransactionDataByRange.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (ClassBlockchainStats.ContainsBlockHeight(packetSendAskBlockTransactionDataByRange.BlockHeight))
                            {
                                if (packetSendAskBlockTransactionDataByRange.TransactionIdStartRange >= 0 &&
                                    packetSendAskBlockTransactionDataByRange.TransactionIdEndRange >= 0 &&
                                    packetSendAskBlockTransactionDataByRange.TransactionIdStartRange < packetSendAskBlockTransactionDataByRange.TransactionIdEndRange)
                                {
                                    int blockTransactionCount = await ClassBlockchainStats.GetBlockTransactionCount(packetSendAskBlockTransactionDataByRange.BlockHeight, _cancellationTokenAccessData);

                                    if (blockTransactionCount > packetSendAskBlockTransactionDataByRange.TransactionIdStartRange &&
                                        blockTransactionCount >= packetSendAskBlockTransactionDataByRange.TransactionIdEndRange)
                                    {
                                        TaskManager.TaskManager.InsertTask(async () =>
                                        {

                                            using (DisposableSortedList<string, ClassBlockTransaction> transactionList = await ClassBlockchainStats.GetTransactionListFromBlockHeightTarget(packetSendAskBlockTransactionDataByRange.BlockHeight, true, _cancellationTokenAccessData))
                                            {

                                                if (transactionList.Count > packetSendAskBlockTransactionDataByRange.TransactionIdStartRange &&
                                                    transactionList.Count >= packetSendAskBlockTransactionDataByRange.TransactionIdEndRange)
                                                {
                                                    #region Generate the list of transaction asked by range.

                                                    SortedDictionary<string, ClassTransactionObject> transactionListRangeToSend = new SortedDictionary<string, ClassTransactionObject>();

                                                    foreach (var transactionPair in transactionList.GetList.Skip(packetSendAskBlockTransactionDataByRange.TransactionIdStartRange).Take(packetSendAskBlockTransactionDataByRange.TransactionIdEndRange - packetSendAskBlockTransactionDataByRange.TransactionIdStartRange))
                                                        transactionListRangeToSend.Add(transactionPair.Key, transactionPair.Value.TransactionObject);

                                                    #endregion

                                                    ClassPeerPacketSendBlockTransactionDataByRange packetSendBlockTransactionData = new ClassPeerPacketSendBlockTransactionDataByRange()
                                                    {
                                                        BlockHeight = packetSendAskBlockTransactionDataByRange.BlockHeight,
                                                        ListTransactionObject = transactionListRangeToSend,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                    };


                                                    SignPacketWithNumericPrivateKey(packetSendBlockTransactionData, out string hashNumeric, out string signatureNumeric);

                                                    packetSendBlockTransactionData.PacketNumericHash = hashNumeric;
                                                    packetSendBlockTransactionData.PacketNumericSignature = signatureNumeric;

                                                    bool sendError = false;

                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                    {
                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA_BY_RANGE,
                                                        PacketContent = ClassUtility.SerializeData(packetSendBlockTransactionData)
                                                    }, true))
                                                    {
                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                        sendError = true;
                                                    }


                                                    // Clean up.
                                                    transactionList.Clear();
                                                    transactionListRangeToSend.Clear();

                                                    if (sendError)
                                                        ClassPeerCheckManager.InputPeerClientAttemptConnect(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                }

                                                // Clean up.
                                                transactionList.Clear();
                                            }

                                        }, 0, _cancellationTokenAccessData, _clientSocket);
                                    }
                                    else
                                    {
                                        await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                        {
                                            PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET,
                                            PacketContent = string.Empty,
                                        }, false);
                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                                    }
                                }
                                else
                                {
                                    await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET,
                                        PacketContent = string.Empty,
                                    }, false);
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                                }
                            }
                            else
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                    PacketContent = string.Empty,
                                }, false);
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MINING_SHARE_VOTE:
                        {
                            if (ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                            {
                                ClassPeerPacketSendAskMiningShareVote packetSendAskMemPoolMiningShareVote = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskMiningShareVote>(packetSendObject.PacketContent);

                                if (packetSendAskMemPoolMiningShareVote == null)
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;


                                if (!ClassUtility.CheckPacketTimestamp(packetSendAskMemPoolMiningShareVote.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay) && ClassUtility.CheckPacketTimestamp(packetSendAskMemPoolMiningShareVote.MiningPowShareObject.Timestamp, BlockchainSetting.BlockMiningUnlockShareTimestampMaxDelay, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;


                                if (packetSendAskMemPoolMiningShareVote.MiningPowShareObject == null)
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                                // Do not allow to target genesis block and invalid height.
                                if (packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight <= BlockchainSetting.GenesisBlockHeight)
                                {
                                    // Just in case we increment the amount of invalid packet.
                                    ClassPeerPacketSendMiningShareVote packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
                                    {
                                        BlockHeight = packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight,
                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.REFUSED,
                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                    };

                                    SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
                                    packetSendMiningShareVote.PacketNumericHash = hashNumeric;
                                    packetSendMiningShareVote.PacketNumericSignature = numericSignature;

                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                    }, true))
                                    {
                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                    }
                                }
                                else
                                {
                                    long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();

                                    if (packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight > lastBlockHeight)
                                    {
                                        if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                        {
                                            PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                            PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendMiningShareVote()
                                            {
                                                BlockHeight = lastBlockHeight,
                                                VoteStatus = ClassPeerPacketMiningShareVoteEnum.NOT_SYNCED,
                                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                            })
                                        }, true))
                                        {
                                            ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                            return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;

                                        }
                                    }
                                    else
                                    {
                                        ClassBlockObject previousBlockObjectInformation = await ClassBlockchainStats.GetBlockInformationData(packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight - 1, _cancellationTokenAccessData);
                                        int previousBlockTransactionCount = previousBlockObjectInformation.TotalTransaction;

                                        if (packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight == lastBlockHeight)
                                        {
                                            ClassBlockObject blockObjectInformation = await ClassBlockchainStats.GetBlockInformationData(packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight, _cancellationTokenAccessData);

                                            if (blockObjectInformation.BlockStatus == ClassBlockEnumStatus.LOCKED)
                                            {
                                                // Check the share at first.
                                                ClassMiningPoWaCEnumStatus miningShareCheckStatus = ClassMiningPoWaCUtility.CheckPoWaCShare(BlockchainSetting.CurrentMiningPoWaCSettingObject(packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight),
                                                                                         packetSendAskMemPoolMiningShareVote.MiningPowShareObject,
                                                                                         lastBlockHeight,
                                                                                         blockObjectInformation.BlockHash,
                                                blockObjectInformation.BlockDifficulty,
                                                previousBlockTransactionCount,
                                                previousBlockObjectInformation.BlockFinalHashTransaction, out BigInteger jobDifficulty, out _);

                                                // Ensure equality and validity.
                                                bool shareIsValid = miningShareCheckStatus == ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE && packetSendAskMemPoolMiningShareVote.MiningPowShareObject.PoWaCShareDifficulty == jobDifficulty;

                                                if (shareIsValid)
                                                {
                                                    switch (await ClassBlockchainDatabase.UnlockCurrentBlockAsync(packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight, packetSendAskMemPoolMiningShareVote.MiningPowShareObject, false, _peerNetworkSettingObject.ListenIp, _peerServerOpenNatIp, false, false, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenAccessData))
                                                    {
                                                        case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED:
                                                            {
                                                                ClassPeerNetworkBroadcastFunction.BroadcastMiningShareAsync(_peerNetworkSettingObject.ListenIp, _peerServerOpenNatIp, string.Empty, packetSendAskMemPoolMiningShareVote.MiningPowShareObject, _peerNetworkSettingObject, _peerFirewallSettingObject);

                                                                ClassPeerPacketSendMiningShareVote packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
                                                                {
                                                                    BlockHeight = packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight,
                                                                    VoteStatus = ClassPeerPacketMiningShareVoteEnum.ACCEPTED,
                                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                                };

                                                                SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
                                                                packetSendMiningShareVote.PacketNumericHash = hashNumeric;
                                                                packetSendMiningShareVote.PacketNumericSignature = numericSignature;


                                                                bool resultSend = true;
                                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                {
                                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                    PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                                }, true))
                                                                {
                                                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                                    resultSend = false;
                                                                }



                                                                return resultSend ? ClassPeerNetworkClientServerHandlePacketEnumStatus.VALID_PACKET : ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                            }
                                                        case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND:
                                                            {
                                                                if (ClassMiningPoWaCUtility.ComparePoWaCShare(blockObjectInformation.BlockMiningPowShareUnlockObject, packetSendAskMemPoolMiningShareVote.MiningPowShareObject))
                                                                {

                                                                    ClassPeerPacketSendMiningShareVote packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
                                                                    {
                                                                        BlockHeight = packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight,
                                                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.ACCEPTED,
                                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                                    };

                                                                    SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
                                                                    packetSendMiningShareVote.PacketNumericHash = hashNumeric;
                                                                    packetSendMiningShareVote.PacketNumericSignature = numericSignature;


                                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                    {
                                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                                    }, true))
                                                                    {
                                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                                    }
                                                                }
                                                                else
                                                                {

                                                                    ClassPeerPacketSendMiningShareVote packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
                                                                    {
                                                                        BlockHeight = packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight,
                                                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.REFUSED,
                                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                                    };

                                                                    SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
                                                                    packetSendMiningShareVote.PacketNumericHash = hashNumeric;
                                                                    packetSendMiningShareVote.PacketNumericSignature = numericSignature;

                                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                    {
                                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                                    }, true))
                                                                    {
                                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                        case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOCONSENSUS:
                                                            {
                                                                if (ClassMiningPoWaCUtility.ComparePoWaCShare(blockObjectInformation.BlockMiningPowShareUnlockObject, packetSendAskMemPoolMiningShareVote.MiningPowShareObject))
                                                                {

                                                                    ClassPeerPacketSendMiningShareVote packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
                                                                    {
                                                                        BlockHeight = packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight,
                                                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.ACCEPTED,
                                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                                    };

                                                                    SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
                                                                    packetSendMiningShareVote.PacketNumericHash = hashNumeric;
                                                                    packetSendMiningShareVote.PacketNumericSignature = numericSignature;


                                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                    {
                                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                                    }, true))
                                                                    {
                                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                    {
                                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                        PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendMiningShareVote()
                                                                        {
                                                                            BlockHeight = lastBlockHeight,
                                                                            VoteStatus = ClassPeerPacketMiningShareVoteEnum.REFUSED,
                                                                            PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                                        })
                                                                    }, true))
                                                                    {
                                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                        case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOT_SYNCED:
                                                            {
                                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                {
                                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendMiningShareVote()
                                                                    {
                                                                        BlockHeight = lastBlockHeight,
                                                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.NOT_SYNCED,
                                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                                    })
                                                                }, true))
                                                                {
                                                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                                }
                                                            }
                                                            break;
                                                        case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_INVALID_TIMESTAMP:
                                                        case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED:
                                                            {
                                                                // By default assume the share is invalid or found by someone else.
                                                                ClassPeerPacketMiningShareVoteEnum voteStatus = ClassPeerPacketMiningShareVoteEnum.REFUSED;

                                                                // This is the same winner, probably a returned broadcasted packet of the same share from another peer who have accept the share.
                                                                if (blockObjectInformation.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                                                {
                                                                    if (ClassMiningPoWaCUtility.ComparePoWaCShare(blockObjectInformation.BlockMiningPowShareUnlockObject, packetSendAskMemPoolMiningShareVote.MiningPowShareObject))
                                                                        voteStatus = ClassPeerPacketMiningShareVoteEnum.ACCEPTED;

                                                                }

                                                                ClassPeerPacketSendMiningShareVote packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
                                                                {
                                                                    BlockHeight = packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight,
                                                                    VoteStatus = voteStatus,
                                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                                };

                                                                SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
                                                                packetSendMiningShareVote.PacketNumericHash = hashNumeric;
                                                                packetSendMiningShareVote.PacketNumericSignature = numericSignature;


                                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                {
                                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                    PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                                }, true))
                                                                {
                                                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;

                                                                }
                                                            }
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    ClassPeerPacketSendMiningShareVote packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
                                                    {
                                                        BlockHeight = packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight,
                                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.REFUSED,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                    };

                                                    SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
                                                    packetSendMiningShareVote.PacketNumericHash = hashNumeric;
                                                    packetSendMiningShareVote.PacketNumericSignature = numericSignature;

                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                    {
                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                    }, true))
                                                    {
                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // By default, assume the share is invalid or found by someone else.
                                                ClassPeerPacketMiningShareVoteEnum voteStatus = ClassPeerPacketMiningShareVoteEnum.REFUSED;

                                                // This is the same winner, probably a returned broadcasted packet of the same share from another peer who have accept the share.
                                                if (ClassMiningPoWaCUtility.ComparePoWaCShare(blockObjectInformation.BlockMiningPowShareUnlockObject, packetSendAskMemPoolMiningShareVote.MiningPowShareObject))
                                                {
                                                    voteStatus = ClassPeerPacketMiningShareVoteEnum.ACCEPTED;
                                                    ClassPeerNetworkBroadcastFunction.BroadcastMiningShareAsync(_peerNetworkSettingObject.ListenIp, _peerServerOpenNatIp, string.Empty, packetSendAskMemPoolMiningShareVote.MiningPowShareObject, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                }

                                                ClassPeerPacketSendMiningShareVote packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
                                                {
                                                    BlockHeight = packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight,
                                                    VoteStatus = voteStatus,
                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                };

                                                SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
                                                packetSendMiningShareVote.PacketNumericHash = hashNumeric;
                                                packetSendMiningShareVote.PacketNumericSignature = numericSignature;


                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                {
                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                    PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                }, true))
                                                {
                                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                }
                                            }
                                        }
                                        // If behind.
                                        else
                                        {
                                            if (ClassBlockchainStats.ContainsBlockHeight(packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight))
                                            {
                                                ClassBlockObject blockObjectInformation = await ClassBlockchainStats.GetBlockInformationData(packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight, _cancellationTokenAccessData);

                                                // If the share is the same of the block height target by the share, return the same reponse.
                                                if (ClassMiningPoWaCUtility.ComparePoWaCShare(blockObjectInformation.BlockMiningPowShareUnlockObject, packetSendAskMemPoolMiningShareVote.MiningPowShareObject))
                                                {

                                                    ClassPeerPacketSendMiningShareVote packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
                                                    {
                                                        BlockHeight = packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight,
                                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.ACCEPTED,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                    };

                                                    SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
                                                    packetSendMiningShareVote.PacketNumericHash = hashNumeric;
                                                    packetSendMiningShareVote.PacketNumericSignature = numericSignature;

                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                    {
                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                    }, true))
                                                    {
                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                    }
                                                }
                                                // If not even if the block is already found, return false.
                                                else
                                                {
                                                    ClassPeerPacketSendMiningShareVote packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
                                                    {
                                                        BlockHeight = packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight,
                                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.REFUSED,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                    };

                                                    SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
                                                    packetSendMiningShareVote.PacketNumericHash = hashNumeric;
                                                    packetSendMiningShareVote.PacketNumericSignature = numericSignature;

                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                    {
                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                    }, true))
                                                    {
                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                {
                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendMiningShareVote()
                                                    {
                                                        BlockHeight = lastBlockHeight,
                                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.NOT_SYNCED,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                    })
                                                }, true))
                                                {
                                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                }
                                            }
                                        }
                                    }
                                }

                            }
                            else
                            {
                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendMiningShareVote()
                                    {
                                        BlockHeight = 0,
                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.NOT_SYNCED,
                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                    })
                                }, true))
                                {
                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                }
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_VOTE:
                        {
                            ClassPeerPacketSendAskMemPoolTransactionVote packetSendAskMemPoolTransactionVote = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskMemPoolTransactionVote>(packetSendObject.PacketContent);

                            if (packetSendAskMemPoolTransactionVote == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskMemPoolTransactionVote.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (packetSendAskMemPoolTransactionVote.ListTransactionObject == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            using (DisposableList<ClassTransactionObject> listTransactionToBroadcast = new DisposableList<ClassTransactionObject>())
                            {
                                using (DisposableDictionary<string, ClassTransactionEnumStatus> listTransactionResult = new DisposableDictionary<string, ClassTransactionEnumStatus>())
                                {
                                    foreach (ClassTransactionObject transactionObject in packetSendAskMemPoolTransactionVote.ListTransactionObject)
                                    {
                                        ClassTransactionEnumStatus transactionStatus = ClassTransactionEnumStatus.EMPTY_TRANSACTION; // Default.

                                        long blockHeightSend = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetCloserBlockHeightFromTimestamp(transactionObject.TimestampBlockHeightCreateSend, _cancellationTokenAccessData);

                                        if (blockHeightSend >= BlockchainSetting.GenesisBlockHeight && blockHeightSend <= ClassBlockchainStats.GetLastBlockHeight())
                                        {

                                            if (transactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION && transactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                                            {
                                                bool alreadyExist = await ClassMemPoolDatabase.CheckTxHashExist(transactionObject.TransactionHash, _cancellationTokenAccessData);
                                                if (!alreadyExist)
                                                {
                                                    transactionStatus = await ClassTransactionUtility.CheckTransactionWithBlockchainData(transactionObject, true, false, _enableMemPoolBroadcastClientMode, null, 0, null, false, _cancellationTokenAccessData);

                                                    // The node can be late or in advance.
                                                    if (transactionStatus != ClassTransactionEnumStatus.VALID_TRANSACTION && transactionStatus != ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT)
                                                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                }
                                                else
                                                {
                                                    ClassTransactionObject memPoolTransactionObject = await ClassMemPoolDatabase.GetMemPoolTxFromTransactionHash(transactionObject.TransactionHash, 0, _cancellationTokenAccessData);

                                                    if (memPoolTransactionObject != null)
                                                    {
                                                        alreadyExist = true;
                                                        if (!ClassTransactionUtility.CompareTransactionObject(memPoolTransactionObject, transactionObject))
                                                        {
                                                            transactionStatus = ClassTransactionEnumStatus.DUPLICATE_TRANSACTION_HASH;
                                                            ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                        }
                                                        else
                                                            transactionStatus = ClassTransactionEnumStatus.VALID_TRANSACTION;
                                                    }
                                                    else
                                                    {
                                                        transactionStatus = ClassTransactionEnumStatus.EMPTY_TRANSACTION;
                                                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                    }
                                                }

                                                if (transactionStatus == ClassTransactionEnumStatus.VALID_TRANSACTION)
                                                {
                                                    if (!alreadyExist)
                                                        ClassMemPoolDatabase.InsertTxToMemPool(transactionObject);

                                                    if (transactionObject.BlockHeightTransactionConfirmationTarget <= ClassBlockchainStats.GetLastBlockHeight())
                                                        listTransactionToBroadcast.Add(transactionObject);
                                                }
                                            }
                                        }
                                        else
                                            transactionStatus = ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;

                                        listTransactionResult.Add(transactionObject.TransactionHash, transactionStatus);
                                    }

                                    if (listTransactionToBroadcast.Count > 0)
                                        TaskManager.TaskManager.InsertTask(new Action(async () => await ClassPeerNetworkBroadcastFunction.AskMemPoolTxVoteToPeerListsAsync(_peerServerOpenNatIp, _peerServerOpenNatIp, _peerClientIp, listTransactionToBroadcast.GetList, _peerNetworkSettingObject, _peerFirewallSettingObject, new CancellationTokenSource(), false)), 0, null, null);

                                    ClassPeerPacketSendMemPoolTransactionVote packetSendMemPoolTransactionVote = new ClassPeerPacketSendMemPoolTransactionVote()
                                    {
                                        ListTransactionHashResult = listTransactionResult.GetList,
                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                    };

                                    SignPacketWithNumericPrivateKey(packetSendMemPoolTransactionVote, out string hashNumeric, out string numericSignature);
                                    packetSendMemPoolTransactionVote.PacketNumericHash = hashNumeric;
                                    packetSendMemPoolTransactionVote.PacketNumericSignature = numericSignature;

                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_VOTE,
                                        PacketContent = ClassUtility.SerializeData(packetSendMemPoolTransactionVote),
                                    }, true))
                                    {
                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                    }
                                }
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_DISCONNECT_REQUEST: // Ask to disconnect propertly from the peer.
                        {
                            _clientAskDisconnection = true;

                            if (packetSendObject.PacketContent != _peerNetworkSettingObject.ListenIp && packetSendObject.PacketContent != _peerServerOpenNatIp)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_TYPE_PACKET;

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_DISCONNECT_CONFIRMATION,
                                PacketContent = packetSendObject.PacketContent,
                            }, false))
                            {
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_KEEP_ALIVE:
                        {
                            if (!ClassUtility.TryDeserialize(packetSendObject.PacketContent, out ClassPeerPacketAskKeepAlive packetAskKeepAlive, ObjectCreationHandling.Reuse))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (packetAskKeepAlive == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetAskKeepAlive.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MEM_POOL_BROADCAST_MODE: // Ask broadcast mode. No packet data required.
                        {

                            _enableMemPoolBroadcastClientMode = _enableMemPoolBroadcastClientMode ? false : true;

                            ClassPeerPacketSendBroadcastMemPoolResponse packetSendBroadcastMemPoolResponse = new ClassPeerPacketSendBroadcastMemPoolResponse()
                            {
                                Status = _enableMemPoolBroadcastClientMode,
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            };

                            // Do not encrypt packet.
                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_BROADCAST_RESPONSE,
                                PacketContent = ClassUtility.SerializeData(packetSendBroadcastMemPoolResponse),
                            }, false))
                            {
                                _enableMemPoolBroadcastClientMode = false;
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE:
                        {
                            ClassPeerPacketSendAskMemPoolBlockHeightList packetMemPoolAskBlockHeightList = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskMemPoolBlockHeightList>(packetSendObject.PacketContent);

                            if (packetMemPoolAskBlockHeightList == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetMemPoolAskBlockHeightList.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;


                            SortedList<long, int> listMemPoolBlockHeightAndCount = new SortedList<long, int>(); // Block height | Tx count.

                            using (DisposableList<long> listMemPoolBlockHeights = await ClassMemPoolDatabase.GetMemPoolListBlockHeight(_cancellationTokenAccessData))
                            {
                                foreach (long blockHeight in listMemPoolBlockHeights.GetList)
                                {
                                    int txCount = await ClassMemPoolDatabase.GetCountMemPoolTxFromBlockHeight(blockHeight, true, _cancellationTokenAccessData);

                                    if (txCount > 0)
                                        listMemPoolBlockHeightAndCount.Add(blockHeight, txCount);
                                }
                            }

                            ClassPeerPacketSendMemPoolBlockHeightList packetSendMemPoolBlockHeightList = new ClassPeerPacketSendMemPoolBlockHeightList()
                            {
                                MemPoolBlockHeightListAndCount = listMemPoolBlockHeightAndCount,
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            };

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE,
                                PacketContent = ClassUtility.SerializeData(packetSendMemPoolBlockHeightList),
                            }, true))
                            {
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BROADCAST_CONFIRMATION_RECEIVED:
                        {

                            if (!ClassUtility.TryDeserialize(packetSendObject.PacketContent, out ClassPeerPacketAskMemPoolTransactionBroadcastConfirmationReceived packetAskMemPoolBroadcastTransactionConfirmationReceived, ObjectCreationHandling.Reuse))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (packetAskMemPoolBroadcastTransactionConfirmationReceived == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetAskMemPoolBroadcastTransactionConfirmationReceived.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            _onWaitingMemPoolTransactionConfirmationReceived = false;

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE:
                        {
                            ClassPeerPacketSendAskMemPoolTransactionList packetMemPoolAskMemPoolTransactionList = DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskMemPoolTransactionList>(packetSendObject.PacketContent);

                            if (packetMemPoolAskMemPoolTransactionList == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetMemPoolAskMemPoolTransactionList.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            bool doBroadcast = false;

                            if (!_onSendingMemPoolTransaction)
                            {
                                int countMemPoolTx = await ClassMemPoolDatabase.GetCountMemPoolTxFromBlockHeight(packetMemPoolAskMemPoolTransactionList.BlockHeight, true, _cancellationTokenAccessData);

                                if (countMemPoolTx > 0 && countMemPoolTx > packetMemPoolAskMemPoolTransactionList.TotalTransactionProgress)
                                {

                                    _onSendingMemPoolTransaction = true;
                                    doBroadcast = true;

                                    try
                                    {

                                        // Enable a task of broadcasting transactions from the MemPool, await after each sending a confirmation. 
                                        TaskManager.TaskManager.InsertTask(new Action(async () =>
                                        {
                                            int countMemPoolTxSent = 0;
                                            bool exceptionOnSending = false;

                                            using (DisposableList<ClassTransactionObject> listTransaction = await ClassMemPoolDatabase.GetMemPoolTxObjectFromBlockHeight(packetMemPoolAskMemPoolTransactionList.BlockHeight, true, _cancellationTokenAccessData))
                                            {
                                                using (DisposableList<ClassTransactionObject> listToSend = new DisposableList<ClassTransactionObject>())
                                                {

                                                    int currentProgress = 0;

                                                    if (_listMemPoolBroadcastBlockHeight.ContainsKey(packetMemPoolAskMemPoolTransactionList.BlockHeight))
                                                        currentProgress = _listMemPoolBroadcastBlockHeight[packetMemPoolAskMemPoolTransactionList.BlockHeight];
                                                    else
                                                        _listMemPoolBroadcastBlockHeight.Add(packetMemPoolAskMemPoolTransactionList.BlockHeight, 0);

                                                    if (currentProgress < packetMemPoolAskMemPoolTransactionList.TotalTransactionProgress)
                                                        currentProgress = packetMemPoolAskMemPoolTransactionList.TotalTransactionProgress;

                                                    foreach (ClassTransactionObject transactionObject in listTransaction.GetAll.Skip(currentProgress))
                                                    {
                                                        if (transactionObject != null)
                                                        {
                                                            if (transactionObject.TransactionType == ClassTransactionEnumType.NORMAL_TRANSACTION || transactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
                                                            {
                                                                listToSend.Add(transactionObject);

                                                                if (listToSend.Count >= _peerNetworkSettingObject.PeerMaxRangeTransactionToSyncPerRequest)
                                                                {
                                                                    ClassPeerPacketSendMemPoolTransaction packetSendMemPoolTransaction = new ClassPeerPacketSendMemPoolTransaction()
                                                                    {
                                                                        ListTransactionObject = listToSend.GetList,
                                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                                    };

                                                                    _onWaitingMemPoolTransactionConfirmationReceived = true;

                                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                    {
                                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE,
                                                                        PacketContent = ClassUtility.SerializeData(packetSendMemPoolTransaction),
                                                                    }, true))
                                                                    {
                                                                        exceptionOnSending = true;
                                                                        ClientPeerConnectionStatus = false;
                                                                        break;
                                                                    }

                                                                    if (!exceptionOnSending)
                                                                    {
                                                                        long timestampStartWaitingResponse = TaskManager.TaskManager.CurrentTimestampMillisecond;
                                                                        while (_onWaitingMemPoolTransactionConfirmationReceived)
                                                                        {
                                                                            if (timestampStartWaitingResponse + (_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000) < TaskManager.TaskManager.CurrentTimestampMillisecond)
                                                                                break;

                                                                            try
                                                                            {
                                                                                await Task.Delay(100, _cancellationTokenAccessData.Token);
                                                                            }
                                                                            catch
                                                                            {
                                                                                break;
                                                                            }
                                                                        }

                                                                        if (!_onWaitingMemPoolTransactionConfirmationReceived)
                                                                            countMemPoolTxSent += listToSend.Count;
                                                                        else
                                                                        {
                                                                            exceptionOnSending = true;
                                                                            break;
                                                                        }
                                                                    }

                                                                    listToSend.Clear();
                                                                }
                                                            }
                                                        }
                                                    }

                                                    if (!exceptionOnSending)
                                                    {
                                                        if (listToSend.Count > 0)
                                                        {
                                                            ClassPeerPacketSendMemPoolTransaction packetSendMemPoolTransaction = new ClassPeerPacketSendMemPoolTransaction()
                                                            {
                                                                ListTransactionObject = listToSend.GetList,
                                                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                            };

                                                            _onWaitingMemPoolTransactionConfirmationReceived = true;

                                                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                            {
                                                                PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE,
                                                                PacketContent = ClassUtility.SerializeData(packetSendMemPoolTransaction),
                                                            }, true))
                                                            {
                                                                exceptionOnSending = true;
                                                                ClientPeerConnectionStatus = false;
                                                            }

                                                            if (!exceptionOnSending)
                                                            {
                                                                long timestampStartWaitingResponse = TaskManager.TaskManager.CurrentTimestampMillisecond;
                                                                while (_onWaitingMemPoolTransactionConfirmationReceived)
                                                                {
                                                                    if (timestampStartWaitingResponse + (_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000) < TaskManager.TaskManager.CurrentTimestampMillisecond)
                                                                        break;

                                                                    try
                                                                    {
                                                                        await Task.Delay(100, _cancellationTokenAccessData.Token);
                                                                    }
                                                                    catch
                                                                    {
                                                                        break;
                                                                    }
                                                                }

                                                                if (!_onWaitingMemPoolTransactionConfirmationReceived)
                                                                    countMemPoolTxSent += listToSend.Count;
                                                                else
                                                                    exceptionOnSending = true;
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            if (exceptionOnSending)
                                                ClientPeerConnectionStatus = false;
                                            else
                                            {
                                                // End broadcast transaction. Packet not encrypted, no content.
                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                {
                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_END_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE,
                                                    PacketContent = string.Empty,
                                                }, false))
                                                {
                                                    ClientPeerConnectionStatus = false;
                                                }
                                                else
                                                    _listMemPoolBroadcastBlockHeight[packetMemPoolAskMemPoolTransactionList.BlockHeight] += countMemPoolTxSent;

                                            }

                                            _onSendingMemPoolTransaction = false;

                                        }), 0, _cancellationTokenAccessData, _clientSocket);

                                    }
                                    catch
                                    {
                                        // Ignored, catch the exception once broadcast task is cancelled.
                                        _onSendingMemPoolTransaction = false;
                                    }

                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.VALID_PACKET;

                                }
                            }

                            if (!doBroadcast && !_onSendingMemPoolTransaction)
                            {
                                // End broadcast transaction. Packet not encrypted, no content.
                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_END_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE,
                                    PacketContent = string.Empty,
                                }, false))
                                {
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                }
                            }
                        }
                        break;
                    default:
                        ClassLog.WriteLine("Invalid packet type received from: " + _peerClientIp + " | Content: " + packet.GetStringFromByteArrayUtf8(), ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_TYPE_PACKET;
                }
            }
            catch
            {
                ClassLog.WriteLine("Invalid packet received from: " + _peerClientIp + " | Content: " + packet.GetStringFromByteArrayUtf8(), ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                return ClassPeerNetworkClientServerHandlePacketEnumStatus.EXCEPTION_PACKET;
            }

            return ClassPeerNetworkClientServerHandlePacketEnumStatus.VALID_PACKET;
        }

        #endregion

        #region Send peer packet response.

        /// <summary>
        /// Send a packet to a peer.
        /// </summary>
        /// <param name="packetSendObject">The packet object to send.</param>
        /// <param name="encrypted">Indicate if the packet require encryption.</param>
        /// <returns>Return the status of the sending of the packet.</returns>
        private async Task<bool> SendPacketToPeer(ClassPeerPacketRecvObject packetSendObject, bool encrypted)
        {

            try
            {
                byte[] packetContentEncrypted = null;

                if (encrypted)
                {
                    if (ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].GetClientCryptoStreamObject != null)
                        packetContentEncrypted = ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].GetClientCryptoStreamObject.EncryptDataProcess(ClassUtility.GetByteArrayFromStringUtf8(packetSendObject.PacketContent));
                    else
                    {
                        if (!ClassAes.EncryptionProcess(ClassUtility.GetByteArrayFromStringUtf8(packetSendObject.PacketContent), ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientPacketEncryptionKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientPacketEncryptionKeyIv, out packetContentEncrypted))
                        {
                            _onSendingPacketResponse = false;
                            return false;
                        }
                    }
                }

                if (packetContentEncrypted == null && encrypted)
                {
                    _onSendingPacketResponse = false;
                    return false;
                }

                if (encrypted)
                    packetSendObject.PacketContent = Convert.ToBase64String(packetContentEncrypted);

                packetSendObject.PacketHash = ClassUtility.GenerateSha256FromString(packetSendObject.PacketContent);

                if (ClassPeerCheckManager.CheckPeerClientWhitelistStatus(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject))
                {
                    if (ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].GetClientCryptoStreamObject != null)
                        packetSendObject.PacketSignature = ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].GetClientCryptoStreamObject.DoSignatureProcess(packetSendObject.PacketHash, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerInternPrivateKey);
                }

                using (NetworkStream networkStream = new NetworkStream(_clientSocket))
                {
                    if (await networkStream.TrySendSplittedPacket(ClassUtility.GetByteArrayFromStringUtf8(Convert.ToBase64String(packetSendObject.GetPacketData()) + ClassPeerPacketSetting.PacketPeerSplitSeperator), _cancellationTokenAccessData, _peerNetworkSettingObject.PeerMaxPacketSplitedSendSize))
                    {
                        _onSendingPacketResponse = false;
                        return true;
                    }
                }
            }
            catch
            {
                // Ignored.
            }

            _onSendingPacketResponse = false;
            return false;
        }


        #endregion

        #region Sign packet with the private key signature of the peer.

        /// <summary>
        /// Sign packet with the numeric private key of the peer.
        /// </summary>
        /// <typeparam name="T">Type of the content to handle.</typeparam>
        /// <param name="content">The content to serialise and to hash.</param>
        /// <param name="numericHash">The hash generated returned.</param>
        /// <param name="numericSignature">The signature generated returned.</param>
        private void SignPacketWithNumericPrivateKey<T>(T content, out string numericHash, out string numericSignature)
        {
            numericHash = ClassUtility.GenerateSha256FromString(ClassUtility.SerializeData(content));
            numericSignature = ClassWalletUtility.WalletGenerateSignature(_peerNetworkSettingObject.PeerNumericPrivateKey, numericHash);
        }

        #endregion

        #region Check/Decrypt packet from peer.

        /// <summary>
        /// Decrypt and deserialize packet content received from a peer.
        /// </summary>
        /// <typeparam name="T">Type of the content to return.</typeparam>
        /// <param name="packetContentCrypted">The packet content to decrypt.</param>
        /// <returns></returns>
        private T DecryptDeserializePacketContentPeer<T>(string packetContentCrypted)
        {
            if (ClassUtility.TryDeserialize(DecryptContentPacketPeer(packetContentCrypted)?.GetStringFromByteArrayUtf8(), out T result))
                return result;

            return default;
        }

        /// <summary>
        /// Check peer packet content signature and hash.
        /// </summary>
        /// <param name="packetSendObject">The packet object received to check.</param>
        /// <returns>Return if the signature provided is valid.</returns>
        private bool CheckContentPacketSignaturePeer(ClassPeerPacketSendObject packetSendObject)
        {
            if (!ClassPeerDatabase.ContainsPeer(_peerClientIp, _peerUniqueId))
                return ClassUtility.GenerateSha256FromString(packetSendObject.PacketContent + packetSendObject.PacketOrder) == packetSendObject.PacketHash;

            if (ClassPeerCheckManager.CheckPeerClientWhitelistStatus(_peerClientIp, _peerUniqueId, _peerNetworkSettingObject))
                return true;

            if (ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientPublicKey.IsNullOrEmpty(false, out _) || 
                packetSendObject.PacketContent.IsNullOrEmpty(false, out _) || 
                packetSendObject.PacketHash.IsNullOrEmpty(false, out _))
            {
                ClassLog.WriteLine("Packet received to check from peer " + _peerClientIp + " is invalid. The public key of the peer, or the date sent are empty.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                return false;
            }

            if (ClassUtility.GenerateSha256FromString(packetSendObject.PacketContent + packetSendObject.PacketOrder) != packetSendObject.PacketHash)
                return false;

            if (ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].GetClientCryptoStreamObject.CheckSignatureProcess(packetSendObject.PacketHash, packetSendObject.PacketSignature, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientPublicKey))
            {
                ClassLog.WriteLine("Signature of the packet received from peer " + _peerClientIp + " is valid. Hash: " + packetSendObject.PacketHash + " Signature: " + packetSendObject.PacketSignature, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                return true;
            }

            ClassLog.WriteLine("Signature of packet received from peer " + _peerClientIp + " is invalid. Public Key of the peer: " + ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientPublicKey, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

            return false;
        }

        /// <summary>
        /// Decrypt encrypted packet content received from a peer.
        /// </summary>
        /// <param name="content">The content encrypted.</param>
        /// <returns>Indicate if the decryption has been done successfully.</returns>
        private byte[] DecryptContentPacketPeer(string content)
        {
            if (!ClassPeerDatabase.ContainsPeer(_peerClientIp, _peerUniqueId))
                return null;

            if (ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].GetClientCryptoStreamObject != null)
            {
                var contentDecryptedTuple = ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].GetClientCryptoStreamObject.DecryptDataProcess(Convert.FromBase64String(content));

                if (contentDecryptedTuple != null)
                {
                    if (contentDecryptedTuple.Item2 && contentDecryptedTuple.Item1 != null)
                        return contentDecryptedTuple.Item1;
                }
            }
            else
            {
                if (ClassAes.DecryptionProcess(Convert.FromBase64String(content), ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientPacketEncryptionKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerClientIp][_peerUniqueId].PeerClientPacketEncryptionKeyIv, out byte[] contentDecrypted))
                    return contentDecrypted;
            }

            return null;
        }

        #endregion
    }
}