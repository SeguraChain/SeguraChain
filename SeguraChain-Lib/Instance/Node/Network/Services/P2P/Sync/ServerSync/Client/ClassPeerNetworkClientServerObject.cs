﻿using System;
﻿﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using SeguraChain_Lib.Instance.Node.Network.Database.Object;
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
using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.TaskManager;
using SeguraChain_Lib.Utility;
using static SeguraChain_Lib.Other.Object.Network.ClassCustomSocket;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ServerSync.Client
{
    /// <summary>
    /// Object dedicated to peer client tcp received on listening.
    /// </summary>
    public class ClassPeerNetworkClientServerObject : IDisposable
    {
        private ClassPeerDatabase _peerDatabase;
        private ClassCustomSocket _clientSocket;
        public CancellationTokenSource CancellationTokenHandlePeerConnection;
        private CancellationTokenSource _cancellationTokenClientCheckConnectionPeer;
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
        private bool _clientAskDisconnection;
        private bool _onSendingPacketResponse;
        private bool _onHandlePacketResponse;
        private bool _clientResponseSendSuccessfully;

        /// <summary>
        /// About MemPool broadcast mode.
        /// </summary>
        private bool _enableMemPoolBroadcastClientMode;
        private bool _onSendingMemPoolTransaction;
        private bool _onWaitingMemPoolTransactionConfirmationReceived;
        private Dictionary<long, int> _listMemPoolBroadcastBlockHeight;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="clientSocket">The tcp client object.</param>
        /// <param name="cancellationTokenHandlePeerConnection">The cancellation token who permit to cancel the handling of the incoming connection.</param>
        /// <param name="peerClientIp">The peer client IP.</param>
        /// <param name="peerServerOpenNatIp">The public ip of the server.</param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public ClassPeerNetworkClientServerObject(ClassPeerDatabase peerDatabase, ClassCustomSocket clientSocket, CancellationTokenSource cancellationTokenHandlePeerConnection, string peerClientIp, string peerServerOpenNatIp, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            _peerDatabase = peerDatabase;
           ClientPeerConnectionStatus = true;
            CancellationTokenHandlePeerConnection = cancellationTokenHandlePeerConnection;
            _clientSocket = clientSocket;
            _peerNetworkSettingObject = peerNetworkSettingObject;
            _peerFirewallSettingObject = peerFirewallSettingObject;
            _peerClientIp = peerClientIp;
            _peerServerOpenNatIp = peerServerOpenNatIp;
            _listMemPoolBroadcastBlockHeight = new Dictionary<long, int>();
            _cancellationTokenClientCheckConnectionPeer = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenHandlePeerConnection.Token);
            _cancellationTokenListenPeerPacket = CancellationTokenSource.CreateLinkedTokenSource(CancellationTokenHandlePeerConnection.Token);
        }

        #region Dispose functions

        public bool _disposed;

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
        private async Task CheckPeerClientTask()
        {
            while (ClientPeerConnectionStatus)
            {


                if (!ClientPeerConnectionStatus || _clientAskDisconnection || !_clientSocket.IsConnected())
                    break;

                if (_peerFirewallSettingObject.PeerEnableFirewallLink)
                {
                    if (!ClassPeerFirewallManager.CheckClientIpStatus(_peerClientIp))
                        break;
                }

                if (!(_onHandlePacketResponse || _onSendingPacketResponse || (_enableMemPoolBroadcastClientMode && _onSendingMemPoolTransaction)))
                {
                    // If any packet are received after the delay, the function close the peer client connection to listen.
                    if (ClientPeerLastPacketReceived + _peerNetworkSettingObject.PeerServerPacketDelay < TaskManager.TaskManager.CurrentTimestampMillisecond)
                    {
                        // On this case, insert invalid attempt of connection.
                       if (!_clientResponseSendSuccessfully && _peerUniqueId != null)
                            ClassPeerCheckManager.InputPeerClientNoPacketConnectionOpened(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenClientCheckConnectionPeer);
                        break;
                    }
                }

                await Task.Delay(1000);
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

            _clientSocket?.Kill(SocketShutdown.Both);
        }

        #endregion

        #region Listen peer client packets received.

        /// <summary>
        /// Listen packet received from peer.
        /// </summary>
        /// <returns></returns>
        public async Task HandlePeerClient()
        {

            ClientPeerLastPacketReceived = TaskManager.TaskManager.CurrentTimestampMillisecond + _peerNetworkSettingObject.PeerServerPacketDelay;
            // Launch a task for check the peer connection.
            await TaskManager.TaskManager.InsertTask(new Action(async () => await CheckPeerClientTask()), 0, _cancellationTokenClientCheckConnectionPeer);

            // Launch a task to handle packets received.
            await TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                using (var listPacketReceived = new DisposableList<ClassReadPacketSplitted>(false, 0, new List<ClassReadPacketSplitted> { new ClassReadPacketSplitted() }))
                {
                    while (ClientPeerConnectionStatus && !_clientAskDisconnection)
                    {
                        if (!_clientSocket.IsConnected())
                            break;

                        if (_peerFirewallSettingObject.PeerEnableFirewallLink)
                        {
                            if (!ClassPeerFirewallManager.CheckClientIpStatus(_peerClientIp))
                                break;
                        }

                        using (ReadPacketData readPacketData = await _clientSocket.TryReadPacketData(_peerNetworkSettingObject.PeerMaxPacketBufferSize, _peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000, false, _cancellationTokenListenPeerPacket))
                        {
                            if (!readPacketData.Status)
                            {
                                // If no packet is received after the delay, close the connection.
                                // This replaces the CheckPeerClientTask functionality.
                                if (!_onHandlePacketResponse && !_onSendingPacketResponse && !(_enableMemPoolBroadcastClientMode && _onSendingMemPoolTransaction))
                                {
                                    if (!_clientResponseSendSuccessfully && _peerUniqueId != null)
                                        ClassPeerCheckManager.InputPeerClientNoPacketConnectionOpened(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenListenPeerPacket);
                                }
                                break;
                            }

                            ClientPeerLastPacketReceived = TaskManager.TaskManager.CurrentTimestampMillisecond + _peerNetworkSettingObject.PeerServerPacketDelay;

                            #region Handle packet content received, split the data.

                            listPacketReceived.GetList = ClassUtility.GetEachPacketSplitted(
                                readPacketData.Data,
                                listPacketReceived, _cancellationTokenListenPeerPacket).GetList;

                            if (listPacketReceived.GetList.Sum(x => x.Packet.Length) >= ClassPeerPacketSetting.PacketMaxLengthReceive)
                            {
#if DEBUG
                                Debug.WriteLine("Too huge packet data length from peer " + _peerClientIp + " | " + listPacketReceived.GetList.Sum(x => x.Packet.Length) + "/" + ClassPeerPacketSetting.PacketMaxLengthReceive);
#endif
                                break;
                            }
                            #endregion
                            for (int index = 0; index < listPacketReceived.GetList.Count; index++)
                            {

                                if (!ClientPeerConnectionStatus)
                                    break;

                                if (listPacketReceived[index] == null || !listPacketReceived[index].Complete || listPacketReceived[index].Used)
                                       continue;

                                byte[] base64Packet = null;

                                bool failed = false;

                                try {
                                    base64Packet = Convert.FromBase64String(listPacketReceived[index].Packet);
                                    failed = base64Packet == null || base64Packet.Length == 0;
                                }
                                catch
                                {
                                    failed = true;
                                }

                                listPacketReceived[index].Used = true;
                                listPacketReceived[index].Packet.Clear();

                                if (failed)
                                    continue;

                               _onHandlePacketResponse = true;

                                try
                                {
                                    ClassPeerPacketSendObject packetSendObject = new ClassPeerPacketSendObject(base64Packet, out bool status);
                                    if (!status)
                                    {
                                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenListenPeerPacket);
                                        ClientPeerConnectionStatus = false;
                                    }
                                    else
                                    {
                                        switch (await HandlePacket(packetSendObject)) {
                                            case ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_TYPE_PACKET:
                                            case ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET:
                                                {
#if DEBUG
                                                    Debug.WriteLine("Invalid packet data from " + _peerClientIp + " | Order: " + packetSendObject.PacketOrder);
#endif
                                                    ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenListenPeerPacket);
                                                }
                                                break;
                                            case ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED:
                                                {
                                                    ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenListenPeerPacket);
                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, string.Empty, packetSendObject.PeerLastTimestampSignatureWhitelist)
                                                    {
                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS,
                                                        PacketContent = string.Empty,
                                                    }, null, false))
                                                    {
                                                        ClientPeerConnectionStatus = false;
                                                    }
                                                }
                                                break;
                                            case ClassPeerNetworkClientServerHandlePacketEnumStatus.EXCEPTION_PACKET:
                                            case ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET:
                                                {
                                                    ClassPeerCheckManager.InputPeerClientAttemptConnect(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenListenPeerPacket);
                                                    ClientPeerConnectionStatus = false;
                                                }
                                                break;
                                            case ClassPeerNetworkClientServerHandlePacketEnumStatus.VALID_PACKET:
                                                {
                                                    _clientResponseSendSuccessfully = true;

                                                    ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _cancellationTokenListenPeerPacket);
                                                }
                                                break;
                                        }
                                    }
                                }
                                catch (Exception error)
                                {
                                    Debug.WriteLine("Error to handle packet received from Peer: " + _peerClientIp + " | Exception: " + error.Message);
                                }

                                _onHandlePacketResponse = false;

                            }

                            listPacketReceived.GetList.RemoveAll(x => x.Used); // Clean up used packets.
                        }
                   }

                    ClosePeerClient(false);
                }
            }), 0, _cancellationTokenListenPeerPacket);

        }

#endregion

        #region Handle peer packet received.

        /// <summary>
        /// Handle decrypted packet. (Usually used for send auth keys for register a new peer).
        /// </summary>
        /// <param name="packet">Packet received to handle.</param>
        /// <returns>Return the status of the handle of the packet.</returns>
        private async Task<ClassPeerNetworkClientServerHandlePacketEnumStatus> HandlePacket(ClassPeerPacketSendObject packetSendObject)
        {

            try
            {
                ClassPeerObject peerObject = null;

                #region Update peer activity.

                _peerUniqueId = packetSendObject.PacketPeerUniqueId;

                if (_peerUniqueId.IsNullOrEmpty(false, out _))
                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                if (_peerDatabase.ContainsPeerUniqueId(_peerClientIp, _peerUniqueId, _cancellationTokenListenPeerPacket))
                {
                    ClassPeerCheckManager.UpdatePeerClientLastPacketReceived(_peerDatabase, _peerClientIp, _peerUniqueId, packetSendObject.PeerLastTimestampSignatureWhitelist, _cancellationTokenListenPeerPacket);
                    peerObject = _peerDatabase[_peerClientIp, _peerUniqueId, _cancellationTokenListenPeerPacket];
                }
                // Not allow other packets until to get the peer auth keys are initialized.
                else
                {
                    if (packetSendObject.PacketOrder != ClassPeerEnumPacketSend.ASK_PEER_AUTH_KEYS)
                    {
                        await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, string.Empty, packetSendObject.PeerLastTimestampSignatureWhitelist)
                        {
                            PacketOrder = ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS,
                            PacketContent = string.Empty,
                        }, peerObject, false);

                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.VALID_PACKET;
                    }
                }


                #endregion

                #region Check packet signature if necessary.

                bool peerIgnorePacketSignature = packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_PEER_AUTH_KEYS ||
                                                 packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_KEEP_ALIVE ||
                                                 packetSendObject.PacketOrder == ClassPeerEnumPacketSend.ASK_MEM_POOL_BROADCAST_MODE;

                if (!peerIgnorePacketSignature)
                {
                    if (!ClassPeerCheckManager.CheckPeerClientWhitelistStatus(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _cancellationTokenListenPeerPacket))
                        ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _cancellationTokenListenPeerPacket);
                    else
                    {
                        if (!await CheckContentPacketSignaturePeer(packetSendObject, peerObject))
                        {
                            await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE,
                                PacketContent = string.Empty,
                            }, peerObject, false);

                            return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                        }
                        else
                            ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _cancellationTokenListenPeerPacket);
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
                            return await SendAuthKeys(peerObject, packetSendObject, false);
                        }
                    case ClassPeerEnumPacketSend.ASK_PEER_LIST:
                        {
                            ClassPeerPacketSendAskPeerList packetSendAskPeerList = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskPeerList>(packetSendObject, peerObject);

                            if (packetSendAskPeerList == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskPeerList.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            Dictionary<string, Tuple<int, string>> listPeerInfo = _peerDatabase.GetPeerListInfo(_peerClientIp, _cancellationTokenListenPeerPacket);

                            var peerIpList = new List<string>(listPeerInfo.Count);
                            var peerPortList = new List<int>(listPeerInfo.Count);
                            var peerUniqueIdList = new List<string>(listPeerInfo.Count);

                            foreach (var peerInfo in listPeerInfo)
                            {
                                peerIpList.Add(peerInfo.Key);
                                peerPortList.Add(peerInfo.Value.Item1);
                                peerUniqueIdList.Add(peerInfo.Value.Item2);
                            }

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_PEER_LIST,
                                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendPeerList()
                                {
                                    PeerIpList = peerIpList,
                                    PeerPortList = peerPortList,
                                    PeerUniqueIdList = peerUniqueIdList,
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                })
                            }, peerObject, true))
                            {
                                ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_LIST_SOVEREIGN_UPDATE:
                        {
                            ClassPeerPacketSendAskListSovereignUpdate packetSendAskListSovereignUpdate = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskListSovereignUpdate>(packetSendObject, peerObject);

                            if (packetSendAskListSovereignUpdate == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

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

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_LIST_SOVEREIGN_UPDATE,
                                PacketContent = ClassUtility.SerializeData(packetContent)
                            }, peerObject, true))
                            {
                                ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_SOVEREIGN_UPDATE_FROM_HASH:
                        {
                            ClassPeerPacketSendAskSovereignUpdateFromHash packetSendAskSovereignUpdateFromHash = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskSovereignUpdateFromHash>(packetSendObject, peerObject);

                            if (packetSendAskSovereignUpdateFromHash == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

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

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_SOVEREIGN_UPDATE_FROM_HASH,
                                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendSovereignUpdateFromHash()
                                {
                                    SovereignUpdateObject = ClassSovereignUpdateDatabase.DictionarySovereignUpdateObject[packetSendAskSovereignUpdateFromHash.SovereignUpdateHash],
                                    PacketNumericHash = hashNumeric,
                                    PacketNumericSignature = signatureNumeric,
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                })
                            }, peerObject, true))
                            {
                                ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_NETWORK_INFORMATION:
                        {
                            ClassPeerPacketSendAskNetworkInformation packetSendAskNetworkInformation = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskNetworkInformation>(packetSendObject, peerObject);

                            if (packetSendAskNetworkInformation == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskNetworkInformation.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;


                            long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();

                            if (lastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                            {

                                ClassBlockObject blockObject = await ClassBlockchainStats.GetBlockInformationData(lastBlockHeight, _cancellationTokenListenPeerPacket);

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

                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_NETWORK_INFORMATION,
                                        PacketContent = ClassUtility.SerializeData(packetSendNetworkInformation)
                                    }, peerObject, true))
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

                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_NETWORK_INFORMATION,
                                    PacketContent = ClassUtility.SerializeData(packetSendNetworkInformation)
                                }, peerObject, true))
                                {
                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                }
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_BLOCK_DATA_BY_RANGE:
                        {
                            ClassPeerPacketSendAskBlockDataByRange packetSendAskBlockDataByRange = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskBlockDataByRange>(packetSendObject, peerObject);

                            if (packetSendAskBlockDataByRange == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskBlockDataByRange.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (packetSendAskBlockDataByRange.BlockHeightStart > packetSendAskBlockDataByRange.BlockHeightEnd)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            bool containRange = true;

                            for (long i = packetSendAskBlockDataByRange.BlockHeightStart; i < packetSendAskBlockDataByRange.BlockHeightEnd; i++)
                            {
                                if (!ClassBlockchainStats.ContainsBlockHeight(i))
                                {
                                    containRange = false;
                                    break;
                                }
                            }

                            if (!containRange)
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                    PacketContent = string.Empty,
                                }, peerObject, false);
                            }
                            else
                            {

                                ClassPeerPacketSendBlockDataRange packetSendBlockDataRange = new ClassPeerPacketSendBlockDataRange()
                                {
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                };

                                for (long i = packetSendAskBlockDataByRange.BlockHeightStart; i < packetSendAskBlockDataByRange.BlockHeightEnd; i++)
                                {

                                    ClassBlockObject blockObject = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockDataStrategy(i, true, false, _cancellationTokenListenPeerPacket);

                                    if (blockObject != null)
                                        packetSendBlockDataRange.ListBlockObject.Add(blockObject);
                                    else
                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                }

                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_BLOCK_DATA_BY_RANGE,
                                    PacketContent = ClassUtility.SerializeData(packetSendBlockDataRange)
                                }, peerObject, true))
                                {
                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                }
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_BLOCK_DATA:
                        {
                            ClassPeerPacketSendAskBlockData packetSendAskBlockData = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskBlockData>(packetSendObject, peerObject);

                            if (packetSendAskBlockData == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskBlockData.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (ClassBlockchainStats.ContainsBlockHeight(packetSendAskBlockData.BlockHeight))
                            {
                                ClassBlockObject blockObject = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockDataStrategy(packetSendAskBlockData.BlockHeight, true, false, _cancellationTokenListenPeerPacket);

                                if (blockObject != null)
                                {

                                    ClassPeerPacketSendBlockData packetSendBlockData = new ClassPeerPacketSendBlockData()
                                    {
                                        BlockData = blockObject,
                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                    };

                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_BLOCK_DATA,
                                        PacketContent = ClassUtility.SerializeData(packetSendBlockData)
                                    }, peerObject, true))
                                    {
                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                    }
                                }
                                else
                                {
                                    await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                        PacketContent = string.Empty,
                                    }, peerObject, false);
                                }
                            }
                            else
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                    PacketContent = string.Empty,
                                }, peerObject, false);
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_BLOCK_HEIGHT_INFORMATION:
                        {
                            ClassPeerPacketSendAskBlockHeightInformation packetSendAskBlockHeightInformation = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskBlockHeightInformation>(packetSendObject, peerObject);

                            if (packetSendAskBlockHeightInformation == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskBlockHeightInformation.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (ClassBlockchainStats.ContainsBlockHeight(packetSendAskBlockHeightInformation.BlockHeight))
                            {


                                ClassBlockObject blockObject = await ClassBlockchainStats.GetBlockInformationData(packetSendAskBlockHeightInformation.BlockHeight, _cancellationTokenListenPeerPacket);

                                if (blockObject != null)
                                {

                                    ClassPeerPacketSendBlockHeightInformation packetSendBlockHeightInformation = new ClassPeerPacketSendBlockHeightInformation()
                                    {
                                        BlockHeight = packetSendAskBlockHeightInformation.BlockHeight,
                                        BlockFinalTransactionHash = blockObject.BlockFinalHashTransaction,
                                        BlockHash = blockObject.BlockHash,
                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                    };

                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_BLOCK_HEIGHT_INFORMATION,
                                        PacketContent = ClassUtility.SerializeData(packetSendBlockHeightInformation)
                                    }, peerObject, true))
                                    {
                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                    }
                                }
                                else
                                {
                                    await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                        PacketContent = string.Empty,
                                    }, peerObject, false);
                                }
                            }
                            else
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                    PacketContent = string.Empty,
                                }, peerObject, false);
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_BLOCK_TRANSACTION_DATA:
                        {
                            ClassPeerPacketSendAskBlockTransactionData packetSendAskBlockTransactionData = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskBlockTransactionData>(packetSendObject, peerObject);

                            if (packetSendAskBlockTransactionData == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskBlockTransactionData.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (ClassBlockchainStats.ContainsBlockHeight(packetSendAskBlockTransactionData.BlockHeight))
                            {
                                int blockTransactionCount = await ClassBlockchainStats.GetBlockTransactionCount(packetSendAskBlockTransactionData.BlockHeight, _cancellationTokenListenPeerPacket);

                                if (blockTransactionCount > packetSendAskBlockTransactionData.TransactionId)
                                {
                                    using (DisposableSortedList<string, ClassBlockTransaction> transactionList = await ClassBlockchainStats.GetTransactionListFromBlockHeightTarget(packetSendAskBlockTransactionData.BlockHeight, true, _cancellationTokenListenPeerPacket))
                                    {

                                        if (transactionList.Count > packetSendAskBlockTransactionData.TransactionId)
                                        {

                                            ClassPeerPacketSendBlockTransactionData packetSendBlockTransactionData = new ClassPeerPacketSendBlockTransactionData()
                                            {
                                                BlockHeight = packetSendAskBlockTransactionData.BlockHeight,
                                                TransactionObject = transactionList.GetList.ElementAt(packetSendAskBlockTransactionData.TransactionId).Value.TransactionObject,
                                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                            };

                                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                            {
                                                PacketOrder = ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA,
                                                PacketContent = ClassUtility.SerializeData(packetSendBlockTransactionData)
                                            }, peerObject, true))
                                            {
                                                ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET,
                                        PacketContent = string.Empty,
                                    }, peerObject, false);
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                                }
                            }
                            else
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                    PacketContent = string.Empty,
                                }, peerObject, false);
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_BLOCK_TRANSACTION_DATA_BY_RANGE:
                        {
                            ClassPeerPacketSendAskBlockTransactionDataByRange packetSendAskBlockTransactionDataByRange = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskBlockTransactionDataByRange>(packetSendObject, peerObject);

                            if (packetSendAskBlockTransactionDataByRange == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskBlockTransactionDataByRange.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (ClassBlockchainStats.ContainsBlockHeight(packetSendAskBlockTransactionDataByRange.BlockHeight))
                            {
                                if (packetSendAskBlockTransactionDataByRange.TransactionIdStartRange >= 0 &&
                                    packetSendAskBlockTransactionDataByRange.TransactionIdEndRange >= 0 &&
                                    packetSendAskBlockTransactionDataByRange.TransactionIdStartRange < packetSendAskBlockTransactionDataByRange.TransactionIdEndRange)
                                {
                                    int blockTransactionCount = await ClassBlockchainStats.GetBlockTransactionCount(packetSendAskBlockTransactionDataByRange.BlockHeight, _cancellationTokenListenPeerPacket);

                                    if (blockTransactionCount > packetSendAskBlockTransactionDataByRange.TransactionIdStartRange &&
                                        blockTransactionCount >= packetSendAskBlockTransactionDataByRange.TransactionIdEndRange)
                                    {

                                        using (DisposableSortedList<string, ClassBlockTransaction> transactionList = await ClassBlockchainStats.GetTransactionListFromBlockHeightTarget(packetSendAskBlockTransactionDataByRange.BlockHeight, true, _cancellationTokenListenPeerPacket))
                                        {

                                            if (transactionList.Count > packetSendAskBlockTransactionDataByRange.TransactionIdStartRange &&
                                                transactionList.Count >= packetSendAskBlockTransactionDataByRange.TransactionIdEndRange)
                                            {
                                                #region Generate the list of transaction asked by range.

                                                Dictionary<string, ClassTransactionObject> transactionListRangeToSend = new Dictionary<string, ClassTransactionObject>();

                                                var transactionListSource = transactionList.GetList;
                                                int start = packetSendAskBlockTransactionDataByRange.TransactionIdStartRange;
                                                int end = packetSendAskBlockTransactionDataByRange.TransactionIdEndRange;

                                                for (int i = start; i < end; i++)
                                                {
                                                    transactionListRangeToSend.Add(transactionListSource.ElementAt(i).Key, transactionListSource.ElementAt(i).Value.TransactionObject);
                                                }

                                                #endregion

                                                ClassPeerPacketSendBlockTransactionDataByRange packetSendBlockTransactionData = new ClassPeerPacketSendBlockTransactionDataByRange()
                                                {
                                                    BlockHeight = packetSendAskBlockTransactionDataByRange.BlockHeight,
                                                    ListTransactionObject = transactionListRangeToSend,
                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                };


                                                bool sendError = false;

                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                {
                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA_BY_RANGE,
                                                    PacketContent = ClassUtility.SerializeData(packetSendBlockTransactionData)
                                                }, peerObject, true))
                                                {
                                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                    sendError = true;
                                                }


                                                // Clean up.
                                                transactionList.Clear();
                                                transactionListRangeToSend.Clear();

                                                if (sendError)
                                                    ClassPeerCheckManager.InputPeerClientAttemptConnect(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenListenPeerPacket);
                                            }

                                            // Clean up.
                                            transactionList.Clear();
                                        }

                                    }
                                    else
                                    {
                                        await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                        {
                                            PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET,
                                            PacketContent = string.Empty,
                                        }, peerObject, false);
                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                                    }
                                }
                                else
                                {
                                    await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.INVALID_PEER_PACKET,
                                        PacketContent = string.Empty,
                                    }, peerObject, false);
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
                                }
                            }
                            else
                            {
                                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED,
                                    PacketContent = string.Empty,
                                }, peerObject, false);
                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MINING_SHARE_VOTE:
                        {
                            if (ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                            {
                                ClassPeerPacketSendAskMiningShareVote packetSendAskMemPoolMiningShareVote = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskMiningShareVote>(packetSendObject, peerObject);

                                if (packetSendAskMemPoolMiningShareVote == null)
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;


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

                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                    {
                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                    }, peerObject, true))
                                    {
                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                    }
                                }
                                else
                                {
                                    long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();

                                    if (packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight <= lastBlockHeight)
                                    {
                                        ClassBlockObject previousBlockObjectInformation = await ClassBlockchainStats.GetBlockInformationData(packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight - 1, _cancellationTokenListenPeerPacket);

                                        if (previousBlockObjectInformation == null)

                                        {
                                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                            { PacketOrder = ClassPeerEnumPacketResponse.NOT_YET_SYNCED, PacketContent = string.Empty, }, peerObject, false))
                                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                            return ClassPeerNetworkClientServerHandlePacketEnumStatus.VALID_PACKET;
                                        }
                                        int previousBlockTransactionCount = previousBlockObjectInformation.TotalTransaction;

                                        if (packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight == lastBlockHeight)
                                        {
                                            ClassBlockObject blockObjectInformation = await ClassBlockchainStats.GetBlockInformationData(packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight, _cancellationTokenListenPeerPacket);

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
                                                    switch (await ClassBlockchainDatabase.UnlockCurrentBlockAsync(_peerDatabase, packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight, packetSendAskMemPoolMiningShareVote.MiningPowShareObject, false, _peerNetworkSettingObject.ListenIp, _peerServerOpenNatIp, false, false, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenListenPeerPacket))
                                                    {
                                                        case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED:
                                                            {
                                                                ClassPeerNetworkBroadcastFunction.BroadcastMiningShareAsync(_peerDatabase, _peerNetworkSettingObject.ListenIp, _peerServerOpenNatIp, string.Empty, packetSendAskMemPoolMiningShareVote.MiningPowShareObject, _peerNetworkSettingObject, _peerFirewallSettingObject);

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
                                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                {
                                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                    PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                                }, peerObject, true))
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


                                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                    {
                                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                                    }, peerObject, true))
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

                                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                    {
                                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                                    }, peerObject, true))
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


                                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                    {
                                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                                    }, peerObject, true))
                                                                    {
                                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                    {
                                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                        PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendMiningShareVote()
                                                                        {
                                                                            BlockHeight = lastBlockHeight,
                                                                            VoteStatus = ClassPeerPacketMiningShareVoteEnum.REFUSED,
                                                                            PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                                        })
                                                                    }, peerObject, true))
                                                                    {
                                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                        case ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOT_SYNCED:
                                                            {
                                                                if (!await SendMiningShareNotSyncedVote(peerObject, lastBlockHeight))
                                                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
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


                                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                {
                                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                                    PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                                }, peerObject, true))
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
                                                    if (!await SendMiningShareRefusedVote(peerObject, packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight))
                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
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
                                                    ClassPeerNetworkBroadcastFunction.BroadcastMiningShareAsync(_peerDatabase, _peerNetworkSettingObject.ListenIp, _peerServerOpenNatIp, string.Empty, packetSendAskMemPoolMiningShareVote.MiningPowShareObject, _peerNetworkSettingObject, _peerFirewallSettingObject);
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


                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                {
                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                    PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                }, peerObject, true))
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
                                                ClassBlockObject blockObjectInformation = await ClassBlockchainStats.GetBlockInformationData(packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight, _cancellationTokenListenPeerPacket);

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

                                                    if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                    {
                                                        PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                                        PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
                                                    }, peerObject, true))
                                                    {
                                                        ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                    }
                                                }
                                                // If not even if the block is already found, return false.
                                                else
                                                {
                                                    if (!await SendMiningShareRefusedVote(peerObject, packetSendAskMemPoolMiningShareVote.MiningPowShareObject.BlockHeight))
                                                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                                }
                                            }
                                            else
                                            {
                                                if (!await SendMiningShareNotSyncedVote(peerObject, lastBlockHeight))
                                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!await SendMiningShareNotSyncedVote(peerObject, lastBlockHeight))
                                            return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                    }
                                }

                            }
                            else
                            {
                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendMiningShareVote()
                                    {
                                        BlockHeight = 0,
                                        VoteStatus = ClassPeerPacketMiningShareVoteEnum.NOT_SYNCED,
                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                    })
                                }, peerObject, true))
                                {
                                    ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                }
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_VOTE:
                        {
                            ClassPeerPacketSendAskMemPoolTransactionVote packetSendAskMemPoolTransactionVote = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskMemPoolTransactionVote>(packetSendObject, peerObject);

                            if (packetSendAskMemPoolTransactionVote == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

                            if (!ClassUtility.CheckPacketTimestamp(packetSendAskMemPoolTransactionVote.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            if (packetSendAskMemPoolTransactionVote.ListTransactionObject == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            DisposableList<ClassTransactionObject> listTransactionToBroadcast = new DisposableList<ClassTransactionObject>();

                            using (DisposableDictionary<string, ClassTransactionEnumStatus> listTransactionResult = new DisposableDictionary<string, ClassTransactionEnumStatus>())
                            {
                                foreach (ClassTransactionObject transactionObject in packetSendAskMemPoolTransactionVote.ListTransactionObject)
                                {
                                    ClassTransactionEnumStatus transactionStatus = ClassTransactionEnumStatus.EMPTY_TRANSACTION; // Default.

                                    long blockHeightSend = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetCloserBlockHeightFromTimestamp(transactionObject.TimestampBlockHeightCreateSend, _cancellationTokenListenPeerPacket);

                                    if (blockHeightSend >= BlockchainSetting.GenesisBlockHeight && blockHeightSend <= ClassBlockchainStats.GetLastBlockHeight())
                                    {

                                        if (transactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION && transactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                                        {
                                            bool alreadyExist = await ClassMemPoolDatabase.CheckTxHashExist(transactionObject.TransactionHash, _cancellationTokenListenPeerPacket);
                                            if (!alreadyExist)
                                            {
                                                transactionStatus = await ClassTransactionUtility.CheckTransactionWithBlockchainData(transactionObject, true, false, _enableMemPoolBroadcastClientMode, null, 0, null, false, _cancellationTokenListenPeerPacket);

                                                // The node can be late or in advance.
                                                if (transactionStatus != ClassTransactionEnumStatus.VALID_TRANSACTION && transactionStatus != ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT)
                                                    ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenListenPeerPacket);
                                            }
                                            else
                                            {
                                                ClassTransactionObject memPoolTransactionObject = await ClassMemPoolDatabase.GetMemPoolTxFromTransactionHash(transactionObject.TransactionHash, 0, _cancellationTokenListenPeerPacket);

                                                if (memPoolTransactionObject != null)
                                                {
                                                    alreadyExist = true;
                                                    if (!ClassTransactionUtility.CompareTransactionObject(memPoolTransactionObject, transactionObject))
                                                    {
                                                        transactionStatus = ClassTransactionEnumStatus.DUPLICATE_TRANSACTION_HASH;
                                                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenListenPeerPacket);
                                                    }
                                                    else
                                                        transactionStatus = ClassTransactionEnumStatus.VALID_TRANSACTION;
                                                }
                                                else
                                                {
                                                    transactionStatus = ClassTransactionEnumStatus.EMPTY_TRANSACTION;
                                                    ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenListenPeerPacket);
                                                }
                                            }

                                            if (transactionStatus == ClassTransactionEnumStatus.VALID_TRANSACTION)
                                            {

                                                if (!alreadyExist)
                                                    ClassMemPoolDatabase.InsertTxToMemPool(transactionObject);

                                                if (transactionObject.BlockHeightTransactionConfirmationTarget > ClassBlockchainStats.GetLastBlockHeight())
                                                    listTransactionToBroadcast.Add(transactionObject);

                                            }
                                        }
                                    }
                                    else
                                        transactionStatus = ClassTransactionEnumStatus.INVALID_BLOCK_HEIGHT;

                                    listTransactionResult.Add(transactionObject.TransactionHash, transactionStatus);
                                }

                                if (listTransactionToBroadcast.Count > 0)
                                {
                                    var listClone = listTransactionToBroadcast.GetList.ToList();
                                    await TaskManager.TaskManager.InsertTask(new Action(async () => await ClassPeerNetworkBroadcastFunction.AskMemPoolTxVoteToPeerListsAsync(_peerDatabase, _peerServerOpenNatIp, _peerServerOpenNatIp, _peerClientIp, listClone, _peerNetworkSettingObject, _peerFirewallSettingObject, new CancellationTokenSource(), false)), 0, null, null);
                                }
                                ClassPeerPacketSendMemPoolTransactionVote packetSendMemPoolTransactionVote = new ClassPeerPacketSendMemPoolTransactionVote()
                                {
                                    ListTransactionHashResult = listTransactionResult.GetList,
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                };

                                SignPacketWithNumericPrivateKey(packetSendMemPoolTransactionVote, out string hashNumeric, out string numericSignature);
                                packetSendMemPoolTransactionVote.PacketNumericHash = hashNumeric;
                                packetSendMemPoolTransactionVote.PacketNumericSignature = numericSignature;

                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_VOTE,
                                    PacketContent = ClassUtility.SerializeData(packetSendMemPoolTransactionVote),
                                }, peerObject, true))
                                {
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                }

                            }

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_DISCONNECT_REQUEST: // Ask to disconnect propertly from the peer.
                        {
                            _clientAskDisconnection = true;

                            if (packetSendObject.PacketContent != _peerNetworkSettingObject.ListenIp && packetSendObject.PacketContent != _peerServerOpenNatIp)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_TYPE_PACKET;

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_DISCONNECT_CONFIRMATION,
                                PacketContent = packetSendObject.PacketContent,
                            }, peerObject, false))
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
                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_BROADCAST_RESPONSE,
                                PacketContent = ClassUtility.SerializeData(packetSendBroadcastMemPoolResponse),
                            }, peerObject, false))
                            {
                                _enableMemPoolBroadcastClientMode = false;
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE:
                        {
                            ClassPeerPacketSendAskMemPoolBlockHeightList packetMemPoolAskBlockHeightList = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskMemPoolBlockHeightList>(packetSendObject, peerObject);

                            if (packetMemPoolAskBlockHeightList == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;
                            /*
                            if (!ClassUtility.CheckPacketTimestamp(packetMemPoolAskBlockHeightList.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;
                            */

                            SortedList<long, int> listMemPoolBlockHeightAndCount = new SortedList<long, int>(); // Block height | Tx count.

                            using (DisposableList<long> listMemPoolBlockHeights = await ClassMemPoolDatabase.GetMemPoolListBlockHeight(_cancellationTokenListenPeerPacket))
                            {
                                foreach (long blockHeight in listMemPoolBlockHeights.GetList)
                                {
                                    int txCount = await ClassMemPoolDatabase.GetCountMemPoolTxFromBlockHeight(blockHeight, true, _cancellationTokenListenPeerPacket);

                                    if (txCount > 0)
                                        listMemPoolBlockHeightAndCount.Add(blockHeight, txCount);
                                }
                            }

                            ClassPeerPacketSendMemPoolBlockHeightList packetSendMemPoolBlockHeightList = new ClassPeerPacketSendMemPoolBlockHeightList()
                            {
                                MemPoolBlockHeightListAndCount = listMemPoolBlockHeightAndCount,
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            };

                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE,
                                PacketContent = ClassUtility.SerializeData(packetSendMemPoolBlockHeightList),
                            }, peerObject, true))
                            {
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                            }
                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BROADCAST_CONFIRMATION_RECEIVED:
                        {

                            if (!ClassUtility.TryDeserialize(packetSendObject.PacketContent, out ClassPeerPacketAskMemPoolTransactionBroadcastConfirmationReceived packetAskMemPoolBroadcastTransactionConfirmationReceived, ObjectCreationHandling.Reuse)
                                || packetAskMemPoolBroadcastTransactionConfirmationReceived == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

                            if (!ClassUtility.CheckPacketTimestamp(packetAskMemPoolBroadcastTransactionConfirmationReceived.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            _onWaitingMemPoolTransactionConfirmationReceived = false;

                        }
                        break;
                    case ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE:
                        {
                            ClassPeerPacketSendAskMemPoolTransactionList packetMemPoolAskMemPoolTransactionList = await DecryptDeserializePacketContentPeer<ClassPeerPacketSendAskMemPoolTransactionList>(packetSendObject, peerObject);

                            if (packetMemPoolAskMemPoolTransactionList == null)
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.DECRYPT_PACKET_CONTENT_FAILED;

                            if (!ClassUtility.CheckPacketTimestamp(packetMemPoolAskMemPoolTransactionList.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

                            bool doBroadcast = false;

                            if (!_onSendingMemPoolTransaction)
                            {
                                int countMemPoolTx = await ClassMemPoolDatabase.GetCountMemPoolTxFromBlockHeight(packetMemPoolAskMemPoolTransactionList.BlockHeight, true, _cancellationTokenListenPeerPacket);

                                if (countMemPoolTx > 0 && countMemPoolTx > packetMemPoolAskMemPoolTransactionList.TotalTransactionProgress)
                                {

                                    _onSendingMemPoolTransaction = true;
                                    doBroadcast = true;

                                    // Enable a task of broadcasting transactions from the MemPool, await after each sending a confirmation. 
                                    await TaskManager.TaskManager.InsertTask(new Action(async () =>
                                    {
                                        int countMemPoolTxSent = 0;
                                        bool exceptionOnSending = false;

                                        using (DisposableList<ClassTransactionObject> listTransaction = await ClassMemPoolDatabase.GetMemPoolTxObjectFromBlockHeight(packetMemPoolAskMemPoolTransactionList.BlockHeight, true, _cancellationTokenListenPeerPacket))
                                        {

                                            int currentProgress = 0;

                                            if (_listMemPoolBroadcastBlockHeight.ContainsKey(packetMemPoolAskMemPoolTransactionList.BlockHeight))
                                                currentProgress = _listMemPoolBroadcastBlockHeight[packetMemPoolAskMemPoolTransactionList.BlockHeight];
                                            else
                                                _listMemPoolBroadcastBlockHeight.Add(packetMemPoolAskMemPoolTransactionList.BlockHeight, 0);

                                            if (currentProgress < packetMemPoolAskMemPoolTransactionList.TotalTransactionProgress)
                                                currentProgress = packetMemPoolAskMemPoolTransactionList.TotalTransactionProgress;


                                            if (listTransaction.Count > 0)
                                            {
                                                ClassLog.WriteLine("Sending " + listTransaction.Count + " transaction MemPool to peer: " + _peerClientIp + "..", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);

                                                ClassPeerPacketSendMemPoolTransaction packetSendMemPoolTransaction = new ClassPeerPacketSendMemPoolTransaction()
                                                {
                                                    ListTransactionObject = listTransaction.GetList,
                                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                };

                                                _onWaitingMemPoolTransactionConfirmationReceived = true;

                                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                {
                                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE,
                                                    PacketContent = ClassUtility.SerializeData(packetSendMemPoolTransaction),
                                                }, peerObject, true))
                                                {
                                                    exceptionOnSending = true;
                                                    ClientPeerConnectionStatus = false;
                                                }
                                                else ClassLog.WriteLine("Sending " + listTransaction.Count + " transaction MemPool to peer: " + _peerClientIp + " successfully done.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                                            }

                                        }


                                        if (exceptionOnSending)
                                            ClientPeerConnectionStatus = false;
                                        else
                                        {
                                            // End broadcast transaction. Packet not encrypted, no content.
                                            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                            {
                                                PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_END_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE,
                                                PacketContent = string.Empty,
                                            }, peerObject, false))
                                            {
                                                ClientPeerConnectionStatus = false;
                                            }
                                            else
                                                _listMemPoolBroadcastBlockHeight[packetMemPoolAskMemPoolTransactionList.BlockHeight] += countMemPoolTxSent;

                                        }

                                        _onSendingMemPoolTransaction = false;

                                    }), 0, _cancellationTokenListenPeerPacket);


                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.VALID_PACKET;

                                }
                            }

                            if (!doBroadcast && !_onSendingMemPoolTransaction)
                            {
                                // End broadcast transaction. Packet not encrypted, no content.
                                if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MEM_POOL_END_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE,
                                    PacketContent = string.Empty,
                                }, peerObject, false))
                                {
                                    return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
                                }
                            }
                        }
                        break;
                    default:
                        ClassLog.WriteLine("Invalid packet type received from: " + _peerClientIp + " | Order: " + packetSendObject.PacketOrder, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                        return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_TYPE_PACKET;
                }
            }
            catch
            {
#if DEBUG
                ClassLog.WriteLine("Invalid packet received from: " + _peerClientIp, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
#endif
                return ClassPeerNetworkClientServerHandlePacketEnumStatus.EXCEPTION_PACKET;
            }

            return ClassPeerNetworkClientServerHandlePacketEnumStatus.VALID_PACKET;
        }


        #endregion

        #region Store packet content.

        /// <summary>
        /// Send auth keys packet.
        /// </summary>
        /// <param name="packetSendObject"></param>
        /// <returns></returns>
        private async Task<ClassPeerNetworkClientServerHandlePacketEnumStatus> SendAuthKeys(ClassPeerObject peerObject, ClassPeerPacketSendObject packetSendObject, bool resendKeys)
        {

            if (!ClassUtility.TryDeserialize(packetSendObject.PacketContent, out ClassPeerPacketSendAskPeerAuthKeys packetSendPeerAuthKeysObject) || packetSendPeerAuthKeysObject == null)
            {
                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
            }
            if (!ClassUtility.CheckPacketTimestamp(packetSendPeerAuthKeysObject.PacketTimestamp, _peerNetworkSettingObject.PeerMaxTimestampDelayPacket, _peerNetworkSettingObject.PeerMaxEarlierPacketDelay))
                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET_TIMESTAMP;

            if (peerObject == null)
                peerObject = ClassPeerKeysManager.GeneratePeerObject(_peerClientIp, packetSendPeerAuthKeysObject.PeerPort, packetSendPeerAuthKeysObject.PeerApiPort, _peerUniqueId, _cancellationTokenListenPeerPacket);

            if (!await ClassPeerKeysManager.UpdatePeerInternalKeys(_peerDatabase, _peerClientIp, packetSendPeerAuthKeysObject.PeerPort, packetSendPeerAuthKeysObject.PeerApiPort, _peerUniqueId, _cancellationTokenListenPeerPacket, _peerNetworkSettingObject, true))
            {
                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                {
                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS,
                    PacketContent = string.Empty,
                }, peerObject, false);

                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
            }

            if (!await ClassPeerKeysManager.UpdatePeerKeysReceivedNetworkServer(_peerDatabase, _peerClientIp, _peerUniqueId, packetSendPeerAuthKeysObject, _cancellationTokenListenPeerPacket))
            {
                await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                {
                    PacketOrder = ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS,
                    PacketContent = string.Empty,
                }, peerObject, false);

                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;
            }


            peerObject = _peerDatabase[_peerClientIp, _peerUniqueId, _cancellationTokenListenPeerPacket];

            if (peerObject == null)
                return ClassPeerNetworkClientServerHandlePacketEnumStatus.INVALID_PACKET;

            if (!await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketResponse.SEND_PEER_AUTH_KEYS,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendPeerAuthKeys()
                {
                    AesEncryptionIv = peerObject.PeerInternPacketEncryptionKeyIv,
                    AesEncryptionKey = peerObject.PeerInternPacketEncryptionKey,
                    PublicKey = peerObject.PeerInternPublicKey,
                    NumericPublicKey = _peerNetworkSettingObject.PeerNumericPublicKey,
                    PeerPort = _peerNetworkSettingObject.ListenPort,
                    PeerApiPort = _peerNetworkSettingObject.ListenApiPort,
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                })
            }, peerObject, false))
            {
                ClassLog.WriteLine("Packet response to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                return ClassPeerNetworkClientServerHandlePacketEnumStatus.SEND_EXCEPTION_PACKET;
            }

            return ClassPeerNetworkClientServerHandlePacketEnumStatus.VALID_PACKET;
        }


        /// <summary>
        /// Send a REFUSED vote for a mining share.
        /// </summary>
        private async Task<bool> SendMiningShareRefusedVote(ClassPeerObject peerObject, long blockHeight)
        {
            var packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
            {
                BlockHeight = blockHeight,
                VoteStatus = ClassPeerPacketMiningShareVoteEnum.REFUSED,
                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
            };

            SignPacketWithNumericPrivateKey(packetSendMiningShareVote, out string hashNumeric, out string numericSignature);
            packetSendMiningShareVote.PacketNumericHash = hashNumeric;
            packetSendMiningShareVote.PacketNumericSignature = numericSignature;

            bool result = await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
            }, peerObject, true);

            if (!result)
            {
                ClassLog.WriteLine("Packet response [REFUSED VOTE] to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            }

            return result;
        }

        /// <summary>
        /// Send a NOT_SYNCED vote for a mining share.
        /// </summary>
        private async Task<bool> SendMiningShareNotSyncedVote(ClassPeerObject peerObject, long lastBlockHeight)
        {
            var packetSendMiningShareVote = new ClassPeerPacketSendMiningShareVote()
            {
                BlockHeight = lastBlockHeight,
                VoteStatus = ClassPeerPacketMiningShareVoteEnum.NOT_SYNCED,
                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
            };

            bool result = await SendPacketToPeer(new ClassPeerPacketRecvObject(_peerNetworkSettingObject.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE,
                PacketContent = ClassUtility.SerializeData(packetSendMiningShareVote)
            }, peerObject, true);

            if (!result)
            {
                ClassLog.WriteLine("Packet response [NOT_SYNCED VOTE] to send to peer: " + _peerClientIp + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            }
            return result;
        }


        #endregion

        #region Send peer packet response.

        /// <summary>
        /// Send a packet to a peer.
        /// </summary>
        /// <param name="packetSendObject">The packet object to send.</param>
        /// <param name="encrypted">Indicate if the packet require encryption.</param>
        /// <returns>Return the status of the sending of the packet.</returns>
        private async Task<bool> SendPacketToPeer(ClassPeerPacketRecvObject packetSendObject, ClassPeerObject peerObject, bool encrypted)
        {
            try
            {
                if (encrypted)
                {
                    byte[] packetContentToEncrypt = packetSendObject.PacketContent.GetByteArray();
                    byte[] packetContentEncrypted;

                    if (peerObject?.GetClientCryptoStreamObject != null)
                    {
                        packetContentEncrypted = await peerObject.GetClientCryptoStreamObject.EncryptDataProcess(packetContentToEncrypt, _cancellationTokenListenPeerPacket);
                    }
                    else
                    {
                        if (!ClassAes.EncryptionProcess(packetContentToEncrypt, peerObject.PeerClientPacketEncryptionKey, peerObject.PeerClientPacketEncryptionKeyIv, out packetContentEncrypted))
                            return false;
                    }

                    if (packetContentEncrypted == null)
                        return false;

                    packetSendObject.PacketContent = ClassUtility.GetHexStringFromByteArray(packetContentEncrypted);
                }

                packetSendObject.PacketHash = ClassUtility.GenerateSha256FromString(packetSendObject.PacketContent);

                if (peerObject?.GetClientCryptoStreamObject != null)
                    packetSendObject.PacketSignature = await peerObject.GetClientCryptoStreamObject.DoSignatureProcess(packetSendObject.PacketHash, peerObject.PeerInternPrivateKey, _cancellationTokenListenPeerPacket);

                return await _clientSocket.TrySendSplittedPacket((Convert.ToBase64String(packetSendObject.GetPacketData()) + ClassPeerPacketSetting.PacketPeerSplitSeperator).GetByteArray(), _cancellationTokenListenPeerPacket, _peerNetworkSettingObject.PeerMaxPacketSplitedSendSize, false);
            }
            catch
            {
                return false;
            }
        }



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
        /// <returns></returns>
        private async Task<T> DecryptDeserializePacketContentPeer<T>(ClassPeerPacketSendObject packetSendObject, ClassPeerObject peerObject)
        {

            byte[] decryptedPacketContent = await DecryptContentPacketPeer(packetSendObject.PacketContent, peerObject);

#if !DEBUG
            // Clean up after.
            packetSendObject.ClearPacketData();
#endif
            if (decryptedPacketContent == null)
                return default;


            string jsonContent = decryptedPacketContent.GetStringFromByteArrayUtf8();

            if (!ClassUtility.TryDeserialize(jsonContent, out T result) || result == null)
                return default;

#if DEBUG
            // Clean up after.
            packetSendObject.ClearPacketData();
#endif

            return result;
        }

        /// <summary>
        /// Check peer packet content signature and hash.
        /// </summary>
        /// <param name="packetSendObject">The packet object received to check.</param>
        /// <returns>Return if the signature provided is valid.</returns>
        private async Task<bool> CheckContentPacketSignaturePeer(ClassPeerPacketSendObject packetSendObject, ClassPeerObject peerObject)
        {
            if (!_peerDatabase.ContainsPeerUniqueId(_peerClientIp, _peerUniqueId, _cancellationTokenListenPeerPacket))
                return ClassUtility.GenerateSha256FromString(packetSendObject.PacketContent + packetSendObject.PacketOrder) == packetSendObject.PacketHash;

            if (ClassPeerCheckManager.CheckPeerClientWhitelistStatus(_peerDatabase, _peerClientIp, _peerUniqueId, _peerNetworkSettingObject, _cancellationTokenListenPeerPacket))
                return true;

            if (peerObject.PeerClientPublicKey.IsNullOrEmpty(false, out _) ||
                packetSendObject.PacketContent.IsNullOrEmpty(false, out _) ||
                packetSendObject.PacketHash.IsNullOrEmpty(false, out _))
            {
                ClassLog.WriteLine("Packet received to check from peer " + _peerClientIp + " is invalid. The public key of the peer, or the date sent are empty.", ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                return false;
            }

            if (ClassUtility.GenerateSha256FromString(packetSendObject.PacketContent + packetSendObject.PacketOrder) != packetSendObject.PacketHash)
                return false;

            if (await peerObject.GetClientCryptoStreamObject.CheckSignatureProcess(packetSendObject.PacketHash, packetSendObject.PacketSignature, peerObject.PeerClientPublicKey, _cancellationTokenListenPeerPacket))
            {
                ClassLog.WriteLine("Signature of the packet received from peer " + _peerClientIp + " is valid. Hash: " + packetSendObject.PacketHash + " Signature: " + packetSendObject.PacketSignature, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                return true;
            }

            ClassLog.WriteLine("Signature of packet received from peer " + _peerClientIp + " is invalid. Public Key of the peer: " + peerObject.PeerClientPublicKey, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

            return false;
        }

        /// <summary>
        /// Decrypt encrypted packet content received from a peer.
        /// </summary>
        /// <param name="content">The content encrypted.</param>
        /// <returns>Indicate if the decryption has been done successfully.</returns>
        private async Task<byte[]> DecryptContentPacketPeer(string content, ClassPeerObject peerObject)
        {
            if (content.IsNullOrEmpty(false, out _))
                return null;


            byte[] contentData = ClassUtility.GetByteArrayFromHexString(content);

            if (peerObject?.GetClientCryptoStreamObject != null)
                return await peerObject.GetClientCryptoStreamObject.DecryptDataProcess(contentData, _cancellationTokenListenPeerPacket);
            else
            {
                if (peerObject?.PeerClientPacketEncryptionKey?.Length == 0 || peerObject?.PeerClientPacketEncryptionKeyIv?.Length == 0)
                {
#if DEBUG
                    Debug.WriteLine("Missing encryption keys from peer. cannot decrypt the content.");
#endif
                    return null;
                }

                if (ClassAes.DecryptionProcess(contentData, peerObject.PeerClientPacketEncryptionKey, peerObject.PeerClientPacketEncryptionKeyIv, out byte[] contentDecrypted))
                    return contentDecrypted;
                else
                    Debug.WriteLine("Decrypt data process failed.");
            }

            return null;
        }

        #endregion

    }

}
#endregion
