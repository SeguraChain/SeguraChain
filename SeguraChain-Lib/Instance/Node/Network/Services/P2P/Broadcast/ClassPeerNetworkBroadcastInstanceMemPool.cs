using Newtonsoft.Json;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.MemPool.Database;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.Function;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Request;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Instance.Node.Network.Database.Manager;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.Model;
using SeguraChain_Lib.Other.Object.List;
using System.Diagnostics;
using SeguraChain_Lib.Instance.Node.Network.Database.Object;
using System.Collections.Concurrent;
using SeguraChain_Lib.Other.Object.Network;
using static SeguraChain_Lib.Other.Object.Network.ClassCustomSocket;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast
{
    public class ClassPeerNetworkBroadcastInstanceMemPool
    {
        private Dictionary<string, Dictionary<string, ClassPeerNetworkClientBroadcastMemPool>> _listPeerNetworkClientBroadcastMemPoolReceiver; // Peer IP | [Peer Unique ID | Client broadcast object]
        private Dictionary<string, Dictionary<string, ClassPeerNetworkClientBroadcastMemPool>> _listPeerNetworkClientBroadcastMemPoolSender; // Peer IP | [Peer Unique ID | Client broadcast object]
        private ClassPeerNetworkSettingObject _peerNetworkSettingObject;
        private ClassPeerFirewallSettingObject _peerFirewallSettingObject;
        private CancellationTokenSource _cancellation;
        private string _peerOpenNatIp;
        private bool _peerNetworkBroadcastInstanceMemPoolStatus;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassPeerNetworkBroadcastInstanceMemPool(ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject, string peerOpenNatIp)
        {
            _listPeerNetworkClientBroadcastMemPoolReceiver = new Dictionary<string, Dictionary<string, ClassPeerNetworkClientBroadcastMemPool>>();
            _listPeerNetworkClientBroadcastMemPoolSender = new Dictionary<string, Dictionary<string, ClassPeerNetworkClientBroadcastMemPool>>();
            _peerNetworkSettingObject = peerNetworkSettingObject;
            _peerFirewallSettingObject = peerFirewallSettingObject;
            _peerOpenNatIp = peerOpenNatIp;
        }

        /// <summary>
        /// Run the network broadcast mempool instance.
        /// </summary>
        public void RunNetworkBroadcastMemPoolInstanceTask()
        {
            _cancellation = new CancellationTokenSource();
            _peerNetworkBroadcastInstanceMemPoolStatus = true;


            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {

                while (_peerNetworkBroadcastInstanceMemPoolStatus && !_cancellation.IsCancellationRequested)
                {
                    #region Generate sender/receiver broadcast instances by targetting public peers.

                    if (ClassPeerDatabase.DictionaryPeerDataObject.Count > 0)
                    {
                        foreach (string peerIpTarget in ClassPeerDatabase.DictionaryPeerDataObject.Keys.ToArray())
                        {

                            if (peerIpTarget.IsNullOrEmpty(false, out _))
                                continue;


                            ConcurrentDictionary<string, ClassPeerObject> peerObject = ClassPeerDatabase.GetPeerCollectionObject(peerIpTarget);

                            if (peerObject == null)
                                continue;

                            if (peerObject.Count == 0)
                                continue;

                            foreach (string peerUniqueIdTarget in peerObject.Keys.ToArray())
                            {

                                bool success = false;

                                try
                                {
                                    if (!peerUniqueIdTarget.IsNullOrEmpty(false, out _))
                                    {
                                        if (ClassPeerCheckManager.CheckPeerClientStatus(peerIpTarget, peerUniqueIdTarget, false, _peerNetworkSettingObject, _peerFirewallSettingObject))
                                        {
                                            if (peerObject[peerUniqueIdTarget].PeerIsPublic)
                                            {
                                                int peerPortTarget = peerObject[peerUniqueIdTarget].PeerPort;


                                                // Build receiver instance.
                                                if (!_listPeerNetworkClientBroadcastMemPoolReceiver.ContainsKey(peerIpTarget))
                                                    _listPeerNetworkClientBroadcastMemPoolReceiver.Add(peerIpTarget, new Dictionary<string, ClassPeerNetworkClientBroadcastMemPool>());

                                                if (!_listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget].ContainsKey(peerUniqueIdTarget))
                                                {
                                                    _listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget].Add(peerUniqueIdTarget, new ClassPeerNetworkClientBroadcastMemPool(peerIpTarget,
                                                                                                                                           peerUniqueIdTarget,
                                                                                                                                           peerPortTarget,
                                                                                                                                           false,
                                                                                                                                           _peerNetworkSettingObject,
                                                                                                                                           _peerFirewallSettingObject));
                                                    if (!await RunPeerNetworkClientBroadcastMemPool(peerIpTarget, peerUniqueIdTarget, false))
                                                    {
                                                        if (!_listPeerNetworkClientBroadcastMemPoolReceiver.ContainsKey(peerIpTarget) ||
                                                            !_listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget].ContainsKey(peerUniqueIdTarget))
                                                            continue;

                                                        _listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget][peerUniqueIdTarget].StopTaskAndDisconnect();
                                                        _listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget].Remove(peerUniqueIdTarget);

                                                    }
                                                    else
                                                        success = true;
                                                }

                                                // Build sender instance if the node is in public node.
                                                if (success && _peerNetworkSettingObject.PublicPeer)
                                                {
                                                    if (!_listPeerNetworkClientBroadcastMemPoolSender.ContainsKey(peerIpTarget))
                                                        _listPeerNetworkClientBroadcastMemPoolSender.Add(peerIpTarget, new Dictionary<string, ClassPeerNetworkClientBroadcastMemPool>());

                                                    if (!_listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget].ContainsKey(peerUniqueIdTarget))
                                                    {
                                                        _listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget].Add(peerUniqueIdTarget, new ClassPeerNetworkClientBroadcastMemPool(peerIpTarget,
                                                                                                                                               peerUniqueIdTarget,
                                                                                                                                               peerPortTarget,
                                                                                                                                               true,
                                                                                                                                               _peerNetworkSettingObject,
                                                                                                                                               _peerFirewallSettingObject));

                                                        if (!await RunPeerNetworkClientBroadcastMemPool(peerIpTarget, peerUniqueIdTarget, true))
                                                        {
                                                            _listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget][peerUniqueIdTarget].StopTaskAndDisconnect();
                                                            _listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget].Remove(peerUniqueIdTarget);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    break;
                                }
                            }
                        }
                    }

                    #endregion

                    #region Check Client MemPool broadcast launched.

                    // Check receiver mode instances.
                    if (_listPeerNetworkClientBroadcastMemPoolReceiver.Count > 0)
                    {
                        foreach (string peerIp in _listPeerNetworkClientBroadcastMemPoolReceiver.Keys.ToArray())
                        {
                            try
                            {
                                if (_listPeerNetworkClientBroadcastMemPoolReceiver[peerIp].Count > 0)
                                {
                                    foreach (string peerUniqueId in _listPeerNetworkClientBroadcastMemPoolReceiver[peerIp].Keys.ToArray())
                                    {
                                        if (!_listPeerNetworkClientBroadcastMemPoolReceiver[peerIp][peerUniqueId].IsAlive)
                                        {
                                            _listPeerNetworkClientBroadcastMemPoolReceiver[peerIp][peerUniqueId].StopTaskAndDisconnect();

                                            if (!await RunPeerNetworkClientBroadcastMemPool(peerIp, peerUniqueId, false))
                                            {
                                                _listPeerNetworkClientBroadcastMemPoolReceiver[peerIp][peerUniqueId].Dispose();
                                                _listPeerNetworkClientBroadcastMemPoolReceiver[peerIp].Remove(peerUniqueId);
                                            }
                                        }
                                    }
                                }

                                if (_listPeerNetworkClientBroadcastMemPoolReceiver[peerIp].Count == 0)
                                    _listPeerNetworkClientBroadcastMemPoolReceiver.Remove(peerIp);
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }

                    // Check sender mode instances if the node is in public mode.
                    if (_peerNetworkSettingObject.PublicPeer)
                    {
                        if (_listPeerNetworkClientBroadcastMemPoolSender.Count > 0)
                        {
                            foreach (string peerIp in _listPeerNetworkClientBroadcastMemPoolSender.Keys.ToArray())
                            {
                                try
                                {
                                    if (_listPeerNetworkClientBroadcastMemPoolSender[peerIp].Count > 0)
                                    {
                                        foreach (string peerUniqueId in _listPeerNetworkClientBroadcastMemPoolSender[peerIp].Keys.ToArray())
                                        {
                                            try
                                            {
                                                if (!_listPeerNetworkClientBroadcastMemPoolSender[peerIp][peerUniqueId].IsAlive)
                                                {
                                                    _listPeerNetworkClientBroadcastMemPoolSender[peerIp][peerUniqueId].StopTaskAndDisconnect();

                                                    if (!await RunPeerNetworkClientBroadcastMemPool(peerIp, peerUniqueId, true))
                                                    {
                                                        _listPeerNetworkClientBroadcastMemPoolSender[peerIp][peerUniqueId].Dispose();
                                                        _listPeerNetworkClientBroadcastMemPoolSender[peerIp].Remove(peerUniqueId);
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                                break;
                                            }
                                        }
                                    }

                                    if (_listPeerNetworkClientBroadcastMemPoolSender[peerIp].Count == 0)
                                        _listPeerNetworkClientBroadcastMemPoolSender.Remove(peerIp);
                                }
                                catch
                                {
                                    break;
                                }
                            }
                        }
                    }

                    #endregion

                    await Task.Delay(1000);
                }

            }), 0, _cancellation, null).Wait();

        }

        /// <summary>
        /// Run a peer network client broadcast mempool.
        /// </summary>
        /// <param name="peerIpTarget"></param>
        /// <param name="peerUniqueIdTarget"></param>
        /// <returns></returns>
        private async Task<bool> RunPeerNetworkClientBroadcastMemPool(string peerIpTarget, string peerUniqueIdTarget, bool onSendingMode)
        {
            try
            {
                if (!onSendingMode)
                {
                    if (await _listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget][peerUniqueIdTarget].TryConnectAsync(_cancellation))
                    {
                        if (await _listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget][peerUniqueIdTarget].TryAskBroadcastMode())
                        {
                            _listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget][peerUniqueIdTarget].RunBroadcastTransactionTask();
                            return true;
                        }
                    }
                }
                else
                {
                    if (await _listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget][peerUniqueIdTarget].TryConnectAsync(_cancellation))
                    {
                        if (await _listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget][peerUniqueIdTarget].TryAskBroadcastMode())
                        {
                            _listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget][peerUniqueIdTarget].RunBroadcastTransactionTask();
                            return true;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Stop the network broadcast mempool instance.
        /// </summary>
        public void StopNetworkBroadcastMemPoolInstance()
        {
            _peerNetworkBroadcastInstanceMemPoolStatus = false;

            if (!_cancellation.IsCancellationRequested)
                _cancellation.Cancel();

            #region Clean client broadcast instances.

            // Clean receiver mode instances.
            if (_listPeerNetworkClientBroadcastMemPoolReceiver.Count > 0)
            {
                foreach (string peerIp in _listPeerNetworkClientBroadcastMemPoolReceiver.Keys.ToArray())
                {
                    try
                    {
                        if (_listPeerNetworkClientBroadcastMemPoolReceiver[peerIp].Count == 0)
                            continue;

                        foreach (string peerUniqueId in _listPeerNetworkClientBroadcastMemPoolReceiver[peerIp].Keys.ToArray())
                        {
                            try
                            {
                                _listPeerNetworkClientBroadcastMemPoolReceiver[peerIp][peerUniqueId].StopTaskAndDisconnect();
                            }
                            catch
                            {
                                continue;
                            }
                        }

                        _listPeerNetworkClientBroadcastMemPoolReceiver[peerIp].Clear();
                    }
                    catch
                    {
                        continue;
                    }
                }

                _listPeerNetworkClientBroadcastMemPoolReceiver.Clear();
            }

            // Clean sender mode instances.
            if (_listPeerNetworkClientBroadcastMemPoolSender.Count > 0)
            {
                foreach (string peerIp in _listPeerNetworkClientBroadcastMemPoolSender.Keys.ToArray())
                {
                    try
                    {
                        if (_listPeerNetworkClientBroadcastMemPoolSender[peerIp].Count > 0)
                        {
                            foreach (string peerUniqueId in _listPeerNetworkClientBroadcastMemPoolSender[peerIp].Keys.ToArray())
                            {
                                try
                                {
                                    _listPeerNetworkClientBroadcastMemPoolSender[peerIp][peerUniqueId].StopTaskAndDisconnect();
                                }
                                catch
                                {
                                    // Ignored.
                                }
                            }

                            try
                            {
                                _listPeerNetworkClientBroadcastMemPoolSender[peerIp].Clear();
                            }
                            catch
                            {
                                // Ignored.
                            }
                        }
                    }
                    catch
                    {
                        // Ignored.
                    }
                }

                _listPeerNetworkClientBroadcastMemPoolSender.Clear();
            }

            #endregion
        }

        /// <summary>
        /// Peer broadcast client mempool transaction object.
        /// </summary>
        internal class ClassPeerNetworkClientBroadcastMemPool : ClassPeerSyncFunction, IDisposable
        {
            public bool IsAlive;
            private ClassCustomSocket _peerSocketClient;
            private string _peerIpTarget;
            private string _peerUniqueIdTarget;
            private int _peerPortTarget;
            private ClassPeerNetworkSettingObject _peerNetworkSettingObject;
            private ClassPeerFirewallSettingObject _peerFirewallSettingObject;
            private CancellationTokenSource _peerCancellationToken;
            private CancellationTokenSource _peerCheckConnectionCancellationToken;
            private CancellationTokenSource _peerDoConnectionCancellationToken;
            private Dictionary<long, HashSet<string>> _memPoolListBlockHeightTransactionReceived;
            private Dictionary<long, HashSet<string>> _memPoolListBlockHeightTransactionSend;
            private bool _onSendBroadcastMode;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="peerIpTarget"></param>
            /// <param name="peerUniqueIdTarget"></param>
            /// <param name="peerPortTarget"></param>
            /// <param name="peerNetworkSettingObject"></param>
            /// <param name="peerFirewallSettingObject"></param>
            public ClassPeerNetworkClientBroadcastMemPool(string peerIpTarget, string peerUniqueIdTarget, int peerPortTarget, bool onSendBroadcastMode, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
            {
                _peerIpTarget = peerIpTarget;
                _peerUniqueIdTarget = peerUniqueIdTarget;
                _peerPortTarget = peerPortTarget;
                _onSendBroadcastMode = onSendBroadcastMode;
                _peerNetworkSettingObject = peerNetworkSettingObject;
                _peerFirewallSettingObject = peerFirewallSettingObject;
                _memPoolListBlockHeightTransactionReceived = new Dictionary<long, HashSet<string>>();
                _memPoolListBlockHeightTransactionSend = new Dictionary<long, HashSet<string>>();
            }

            #region Dispose functions

            private bool _disposed;

            ~ClassPeerNetworkClientBroadcastMemPool()
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
                if (_disposed && !IsAlive)
                    return;

                if (disposing)
                    StopTaskAndDisconnect();

                _disposed = true;
            }
            #endregion

            #region Initialize/Stop broadcast client.

            /// <summary>
            /// Try to connect to the peer target.
            /// </summary>
            /// <returns></returns>
            public async Task<bool> TryConnectAsync(CancellationTokenSource mainCancellation)
            {
                if (_peerCheckConnectionCancellationToken != null)
                {
                    if (!_peerCheckConnectionCancellationToken.IsCancellationRequested)
                    {
                        _peerCheckConnectionCancellationToken.Cancel();
                        _peerCheckConnectionCancellationToken.Dispose();
                    }
                }

                if (_peerDoConnectionCancellationToken != null)
                {
                    if (!_peerDoConnectionCancellationToken.IsCancellationRequested)
                    {
                        _peerDoConnectionCancellationToken.Cancel();
                        _peerDoConnectionCancellationToken.Dispose();
                    }
                }

                StopTaskAndDisconnect();

                _peerDoConnectionCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(mainCancellation.Token);

                bool successConnect = false;

                long timestampEnd = TaskManager.TaskManager.CurrentTimestampMillisecond + (_peerNetworkSettingObject.PeerMaxDelayToConnectToTarget * 1000);

                await TaskManager.TaskManager.InsertTask(new Action(async () =>
                {
                    while (!successConnect)
                    {
                        try
                        {
                            _peerSocketClient?.Kill(SocketShutdown.Both);
                            _peerSocketClient = new ClassCustomSocket(new Socket(ClassUtility.GetAddressFamily(_peerIpTarget), SocketType.Stream, ProtocolType.Tcp), false);

                            if (_peerSocketClient.Connect(_peerIpTarget, _peerPortTarget, _peerNetworkSettingObject.PeerMaxDelayToConnectToTarget))
                            {
                                successConnect = true;
                                break;
                            }
                        }
                        catch
                        {
                            // Ignored, catch the exception once the attempt to connect to a peer failed.
                        }
                        await Task.Delay(10);
                    }

                }), 0, _peerDoConnectionCancellationToken, null);



                while (!successConnect)
                {
                    if (timestampEnd < TaskManager.TaskManager.CurrentTimestampMillisecond ||
                        _peerDoConnectionCancellationToken.Token.IsCancellationRequested)
                        break;

                    await Task.Delay(10);
                }


                if (!successConnect)
                {
                    IsAlive = false;
                    return false;
                }


                IsAlive = true;
                _peerCheckConnectionCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(mainCancellation.Token);
                _peerCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(mainCancellation.Token);


                return successConnect;
            }

            /// <summary>
            /// Try to ask the broadcast mode.
            /// </summary>
            /// <returns></returns>
            public async Task<bool> TryAskBroadcastMode()
            {

                ClassPeerPacketSendObject packetSendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId,
                    ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerInternPublicKey,
                    ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                {
                    PacketOrder = ClassPeerEnumPacketSend.ASK_MEM_POOL_BROADCAST_MODE,
                };

                if (!await TrySendPacketToPeer(packetSendObject.GetPacketData(), _peerCancellationToken))
                {
                    IsAlive = false;
                    return false;
                }

                bool broadcastResponsePacketStatus = false;
                bool failed = false;

                CancellationTokenSource cancellationReceiveBroadcastResponsePacket = CancellationTokenSource.CreateLinkedTokenSource(_peerCancellationToken.Token);


                await TaskManager.TaskManager.InsertTask(new Action(async () =>
                {

                    try
                    {
                        using (DisposableList<ClassReadPacketSplitted> listPacketReceived = new DisposableList<ClassReadPacketSplitted>(false, 0, new List<ClassReadPacketSplitted>() { new ClassReadPacketSplitted() }))
                        {

                            while (!broadcastResponsePacketStatus)
                            {
                                using (ReadPacketData readPacketData = await _peerSocketClient.TryReadPacketData(_peerNetworkSettingObject.PeerMaxPacketBufferSize, _peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000, false, cancellationReceiveBroadcastResponsePacket))
                                {
                                    if (!readPacketData.Status)
                                        break;

                                    listPacketReceived.GetList = ClassUtility.GetEachPacketSplitted(readPacketData.Data, listPacketReceived, cancellationReceiveBroadcastResponsePacket).GetList;

                                    if (listPacketReceived.Count > 0)
                                    {
                                        for (int i = 0; i < listPacketReceived.Count; i++)
                                        {
                                            if (!listPacketReceived[i].Complete || listPacketReceived[i].Used)
                                                continue;

                                            listPacketReceived[i].Used = true;

                                            ClassPeerPacketRecvObject peerPacketRecvObject = new ClassPeerPacketRecvObject(Convert.FromBase64String(listPacketReceived[i].Packet), out bool status);

                                            listPacketReceived[i].Packet.Clear();

                                            if (!status)
                                            {
                                                failed = true;
                                                break;
                                            }

                                            if (peerPacketRecvObject.PacketContent.IsNullOrEmpty(false, out _))
                                            {
                                                failed = true;
                                                break;
                                            }

                                            if (peerPacketRecvObject.PacketOrder != ClassPeerEnumPacketResponse.SEND_MEM_POOL_BROADCAST_RESPONSE)
                                            {
                                                failed = true;
                                                break;
                                            }

                                            if (!ClassUtility.TryDeserialize(peerPacketRecvObject.PacketContent, out ClassPeerPacketSendBroadcastMemPoolResponse packetSendBroadcastMemPoolResponse, ObjectCreationHandling.Reuse))
                                            {
                                                failed = true;
                                                break;
                                            }

                                            if (packetSendBroadcastMemPoolResponse == null)
                                            {
                                                failed = true;
                                                break;
                                            }

                                            if (!packetSendBroadcastMemPoolResponse.Status)
                                            {
                                                failed = true;
                                                break;
                                            }

                                            ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerTimestampSignatureWhitelist = peerPacketRecvObject.PeerLastTimestampSignatureWhitelist;

                                            broadcastResponsePacketStatus = true;

                                            listPacketReceived.GetList.RemoveAll(x => x.Complete);
                                            break;
                                        }
                                    }
                                }
                            }

                        }
                    }
                    catch (Exception error)
                    {
#if DEBUG
                        Debug.WriteLine("Exception on asking broadcast mode. " + error.Message);
#endif
                    }

                }), 0, cancellationReceiveBroadcastResponsePacket, null);



                while (!broadcastResponsePacketStatus)
                {

                    if (cancellationReceiveBroadcastResponsePacket.IsCancellationRequested)
                        break;

                    if (failed)
                        break;


                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }

                cancellationReceiveBroadcastResponsePacket.Cancel();


                if (!broadcastResponsePacketStatus)
                    IsAlive = false;

                return broadcastResponsePacketStatus;
            }

            /// <summary>
            /// Stop tasks and disconnect.
            /// </summary>
            public void StopTaskAndDisconnect()
            {
                IsAlive = false;

                // Stop tasks.
                if (_peerCancellationToken != null)
                {
                    if (!_peerCancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            _peerCancellationToken.Cancel();
                            _peerCancellationToken.Dispose();
                        }
                        catch
                        {
                            // Ignored.
                        }
                    }
                }

                _peerSocketClient?.Kill(SocketShutdown.Both);

                // Clean up past heights received/sent.
                _memPoolListBlockHeightTransactionReceived?.Clear();
                _memPoolListBlockHeightTransactionSend?.Clear();
            }

            #endregion

            #region Broadcast tasks.

            /// <summary>
            /// Run the broadcast mempool client transaction tasks.
            /// </summary>
            public void RunBroadcastTransactionTask()
            {
                IsAlive = true;

                EnableCheckConnect();

                try
                {
                    EnableKeepAliveTask();
                    if (!_onSendBroadcastMode)
                        EnableReceiveBroadcastTransactionTask();
                    else
                        EnableSendBroadcastTransactionTask();
                }
                catch
                {
                    // Ignored, catch the exception once the task is cancelled.
                }
            }

            /// <summary>
            /// Enable the keep alive task.
            /// </summary>
            private void EnableKeepAliveTask()
            {
                TaskManager.TaskManager.InsertTask(new Action(async () =>
                {
                    while (IsAlive)
                    {
                        try
                        {
                            ClassPeerPacketSendObject packetSendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId,
                                ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerInternPublicKey,
                                ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                            {
                                PacketOrder = ClassPeerEnumPacketSend.ASK_KEEP_ALIVE,
                                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketAskKeepAlive()
                                {
                                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                }),
                            };

                            if (!await TrySendPacketToPeer(packetSendObject.GetPacketData(), _peerCancellationToken))
                            {
                                IsAlive = false;
                                break;
                            }

                            await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                        }
                        catch
                        {
                            IsAlive = false;
                            break;
                        }
                    }

                }), 0, _peerCancellationToken, _peerSocketClient).Wait();
            }

            /// <summary>
            /// Enable the received broadcast transaction task.
            /// </summary>
            private void EnableReceiveBroadcastTransactionTask()
            {
                TaskManager.TaskManager.InsertTask(new Action(async () =>
                {

                    while (IsAlive)
                    {

                        long lastBlockHeightUnlocked = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeightUnlocked(_peerCancellationToken);

                        // Clean passed blocks mined.
                        foreach (long blockHeight in _memPoolListBlockHeightTransactionReceived.Keys.ToArray())
                        {
                            if (blockHeight <= lastBlockHeightUnlocked)
                            {
                                _memPoolListBlockHeightTransactionReceived[blockHeight].Clear();
                                _memPoolListBlockHeightTransactionReceived.Remove(blockHeight);
                            }
                        }


                        #region First ask the mem pool block height list and their transaction counts.

                        ClassPeerPacketSendObject packetSendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId,
                            ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerInternPublicKey,
                            ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                        {
                            PacketOrder = ClassPeerEnumPacketSend.ASK_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE,
                            PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskMemPoolBlockHeightList()
                            {
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            }),
                        };

                        packetSendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(packetSendObject, _peerIpTarget, _peerUniqueIdTarget, true, _peerNetworkSettingObject, _peerCancellationToken);

                        if (!await TrySendPacketToPeer(packetSendObject.GetPacketData(), _peerCancellationToken))
                        {
                            IsAlive = false;
                            break;
                        }

                        ClassPeerPacketRecvObject peerPacketRecvMemPoolBlockHeightList = await TryReceiveMemPoolBlockHeightListPacket();

                        ClassTranslatePacket<ClassPeerPacketSendMemPoolBlockHeightList> peerPacketMemPoolBlockHeightListTranslated = await TranslatePacketReceived<ClassPeerPacketSendMemPoolBlockHeightList>(peerPacketRecvMemPoolBlockHeightList, ClassPeerEnumPacketResponse.SEND_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE, _peerCancellationToken);

                        if (!peerPacketMemPoolBlockHeightListTranslated.Status)
                        {
                            IsAlive = false;
                            break;
                        }


                        #endregion

                        #region Check the packet of mempool block height lists.

                        if (peerPacketMemPoolBlockHeightListTranslated.PacketTranslated.MemPoolBlockHeightListAndCount == null)
                        {
                            IsAlive = false;
                            break;
                        }

                        #endregion

                        #region Then sync transaction by height.

                        if (peerPacketMemPoolBlockHeightListTranslated.PacketTranslated.MemPoolBlockHeightListAndCount.Count > 0)
                        {
                            bool failed = false;

                            foreach (long blockHeight in peerPacketMemPoolBlockHeightListTranslated.PacketTranslated.MemPoolBlockHeightListAndCount.Keys.OrderBy(x => x))
                            {
                                if (_peerCancellationToken.IsCancellationRequested)
                                    break;

                                if (ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeight < blockHeight)
                                {
                                    int countAlreadySynced = 0;

                                    if (_memPoolListBlockHeightTransactionReceived.ContainsKey(blockHeight))
                                        countAlreadySynced = _memPoolListBlockHeightTransactionReceived[blockHeight].Count;
                                    else
                                        _memPoolListBlockHeightTransactionReceived.Add(blockHeight, new HashSet<string>());

                                    try
                                    {
                                        if (peerPacketMemPoolBlockHeightListTranslated.PacketTranslated.MemPoolBlockHeightListAndCount[blockHeight] > 0)
                                        {
                                            long lastBlockHeightUnlockedConfirmed = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeightTransactionConfirmationDone(_peerCancellationToken);
                                            long lastBlockHeight = ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeight;

                                            if (lastBlockHeightUnlockedConfirmed == blockHeight)
                                                continue;

                                            int countToSync = peerPacketMemPoolBlockHeightListTranslated.PacketTranslated.MemPoolBlockHeightListAndCount[blockHeight];

                                            if (countToSync != countAlreadySynced)
                                            {
                                                // Ensure to be compatible with most recent transactions sent.

                                                packetSendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId,
                                                    ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerInternPublicKey,
                                                    ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                {
                                                    PacketOrder = ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE,
                                                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskMemPoolTransactionList()
                                                    {
                                                        BlockHeight = blockHeight,
                                                        TotalTransactionProgress = 0,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                    }),
                                                };

                                                packetSendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(packetSendObject, _peerIpTarget, _peerUniqueIdTarget, true, _peerNetworkSettingObject, _peerCancellationToken);

                                                if (!await TrySendPacketToPeer(packetSendObject.GetPacketData(), _peerCancellationToken))
                                                {
                                                    IsAlive = false;
                                                    failed = true;
                                                    break;
                                                }

                                                if (!await TryReceiveMemPoolTransactionPacket(blockHeight, countToSync))
                                                {
                                                    IsAlive = false;
                                                    failed = true;
                                                    break;
                                                }
                                            }

                                        }
                                    }
                                    catch
                                    {
                                        IsAlive = false;
                                        break;
                                    }
                                }

                                if (failed)
                                {
                                    IsAlive = false;
                                    break;
                                }
                            }
                        }

                        #endregion
                        await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                    }


                }), 0, _peerCancellationToken, _peerSocketClient).Wait();
            }

            /// <summary>
            /// Enable the send broadcast transaction task.
            /// </summary>
            private void EnableSendBroadcastTransactionTask()
            {
                TaskManager.TaskManager.InsertTask(new Action(async () =>
                {

                    while (IsAlive)
                    {
                        bool cancelSendingBroadcast = false;
                        try
                        {
                            using (DisposableList<long> listMemPoolBlockHeight = await ClassMemPoolDatabase.GetMemPoolListBlockHeight(_peerCancellationToken))
                            {

                                if (listMemPoolBlockHeight.Count > 0)
                                {
                                    foreach (long blockHeight in listMemPoolBlockHeight.GetList)
                                    {
                                        if (_peerCancellationToken.IsCancellationRequested)
                                            break;

                                        int countMemPoolTransaction = await ClassMemPoolDatabase.GetCountMemPoolTxFromBlockHeight(blockHeight, true, _peerCancellationToken);

                                        if (countMemPoolTransaction > 0)
                                        {
                                            if (!_memPoolListBlockHeightTransactionSend.ContainsKey(blockHeight))
                                                _memPoolListBlockHeightTransactionSend.Add(blockHeight, new HashSet<string>());

                                            using (DisposableList<ClassTransactionObject> listTransactionObjectToSend = await ClassMemPoolDatabase.GetMemPoolTxObjectFromBlockHeight(blockHeight, true, _peerCancellationToken))
                                            {
                                                ClassPeerPacketSendObject packetSendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                {
                                                    PacketOrder = ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_VOTE,
                                                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskMemPoolTransactionVote()
                                                    {
                                                        ListTransactionObject = listTransactionObjectToSend.GetList,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                                                    })
                                                };

                                                packetSendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(packetSendObject, _peerIpTarget, _peerUniqueIdTarget, true, _peerNetworkSettingObject, _peerCancellationToken);

                                                if (!await TrySendPacketToPeer(packetSendObject.GetPacketData(), _peerCancellationToken))
                                                {
                                                    IsAlive = false;
                                                    break;
                                                }

                                                using (var listTransactionResult = await TryReceiveMemPoolTransactionVotePacket())
                                                {
                                                    if (listTransactionResult.Count > 0)
                                                    {
                                                        foreach (ClassTransactionObject transactionObject in listTransactionObjectToSend.GetList)
                                                        {
                                                            if (listTransactionResult.ContainsKey(transactionObject.TransactionHash))
                                                            {
                                                                if (listTransactionResult[transactionObject.TransactionHash] == ClassTransactionEnumStatus.VALID_TRANSACTION)
                                                                    _memPoolListBlockHeightTransactionSend[blockHeight].Add(transactionObject.TransactionHash);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        _memPoolListBlockHeightTransactionSend[blockHeight].Clear();
                                                        cancelSendingBroadcast = true;
                                                        IsAlive = false;
                                                        break;
                                                    }
                                                }
                                            }

                                        }

                                        if (cancelSendingBroadcast)
                                            break;
                                    }
                                }

                            }
                        }
                        catch
                        {
                            IsAlive = false;
                            break;
                        }


                        await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                    }


                }), 0, _peerCancellationToken, _peerSocketClient).Wait();
            }

            /// <summary>
            /// Enable the check connect task.
            /// </summary>
            private void EnableCheckConnect()
            {

                TaskManager.TaskManager.InsertTask(new Action(async () =>
                {

                    while (IsAlive)
                    {
                        try
                        {
                            if (!_peerSocketClient.IsConnected())
                            {
                                StopTaskAndDisconnect();
                                IsAlive = false;
                                break;
                            }

                            await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                        }
                        catch
                        {
                            StopTaskAndDisconnect();
                            IsAlive = false;
                            break;
                        }
                    }

                }), 0, _peerCheckConnectionCancellationToken, _peerSocketClient).Wait();

            }

            #endregion

            #region Handle packet response

            /// <summary>
            /// Try receive packet.
            /// </summary>
            /// <param name="networkStream"></param>
            /// <returns></returns>
            private async Task<ClassPeerPacketRecvObject> TryReceiveMemPoolBlockHeightListPacket()
            {
                bool failed = false;
                bool packetReceived = false;
                bool taskComplete = false;
                ClassPeerPacketRecvObject peerPacketRecvObject = null;


                CancellationTokenSource cancellationReceiveBlockListPacket = CancellationTokenSource.CreateLinkedTokenSource(_peerCancellationToken.Token);


                await TaskManager.TaskManager.InsertTask(new Action(async () =>
                {

                    try
                    {
                        using (DisposableList<ClassReadPacketSplitted> listPacketReceived = new DisposableList<ClassReadPacketSplitted>())
                        {
                            listPacketReceived.Add(new ClassReadPacketSplitted());

                            while (!packetReceived && IsAlive && !taskComplete)
                            {
                                using (ReadPacketData readPacketData = await _peerSocketClient.TryReadPacketData(_peerNetworkSettingObject.PeerMaxPacketBufferSize, _peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000, false, cancellationReceiveBlockListPacket))
                                {
                                    if (!readPacketData.Status)
                                        break;

                                    listPacketReceived.GetList = ClassUtility.GetEachPacketSplitted(readPacketData.Data, listPacketReceived, cancellationReceiveBlockListPacket).GetList;

                                    if (listPacketReceived.GetList.Count(x => x.Complete) == 0)
                                        continue;

                                    for (int i = 0; i < listPacketReceived.Count; i++)
                                    {

                                        if (listPacketReceived[i].Complete && listPacketReceived[i].Packet.Length > 0)
                                        {
                                            packetReceived = true;

                                            if (!listPacketReceived[i].Complete || listPacketReceived[i].Used)
                                                continue;

                                            listPacketReceived[i].Used = true;

                                            byte[] base64Packet = null;

                                            try
                                            {
                                                base64Packet = Convert.FromBase64String(listPacketReceived[i].Packet);
                                            }
                                            catch
                                            {
                                                // Ignored.
                                            }


                                            peerPacketRecvObject = new ClassPeerPacketRecvObject(base64Packet, out bool status);

                                            if (!status)
                                                failed = true;

                                            if (peerPacketRecvObject.PacketContent.IsNullOrEmpty(false, out _))
                                                failed = true;

                                            if (peerPacketRecvObject.PacketOrder != ClassPeerEnumPacketResponse.SEND_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE)
                                                failed = true;

                                            ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerTimestampSignatureWhitelist = peerPacketRecvObject.PeerLastTimestampSignatureWhitelist;


                                            taskComplete = true;
                                            break;
                                        }
                                    }

                                }
                            }

                        }
                    }
                    catch
                    {
                        // Ignored, the socket can be closed.
                    }

                    taskComplete = true;

                }), 0, cancellationReceiveBlockListPacket, null);



                while (!taskComplete)
                {

                    if (!IsAlive)
                        break;


                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }

                cancellationReceiveBlockListPacket.Cancel();


                if (failed)
                    return null;

                return peerPacketRecvObject;
            }

            /// <summary>
            /// Try to receive every transactions sent by the peer target.
            /// </summary>
            /// <param name="networkStream"></param>
            /// <returns></returns>
            private async Task<bool> TryReceiveMemPoolTransactionPacket(long blockHeight, int countTransactionToSync)
            {
                bool receiveStatus = true;
                bool endBroadcast = false;
                int txCountReceived = 0;

                using (DisposableDictionary<string, string> listWalletAddressAndPublicKeyCache = new DisposableDictionary<string, string>())
                {

                    using (DisposableList<ClassTransactionObject> listTransactionObject = new DisposableList<ClassTransactionObject>())
                    {

                        CancellationTokenSource cancellationReceiveTransactionPacket = CancellationTokenSource.CreateLinkedTokenSource(_peerCancellationToken.Token);


                        await TaskManager.TaskManager.InsertTask(new Action(async () =>
                        {


                            using (DisposableList<ClassReadPacketSplitted> listPacketReceived = new DisposableList<ClassReadPacketSplitted>())
                            {
                                listPacketReceived.Add(new ClassReadPacketSplitted());


                                while (!endBroadcast && receiveStatus && IsAlive)
                                {

                                    using (ReadPacketData readPacketData = await _peerSocketClient.TryReadPacketData(_peerNetworkSettingObject.PeerMaxPacketBufferSize, _peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000, false, cancellationReceiveTransactionPacket))
                                    {
                                        if (!readPacketData.Status)
                                            break;

                                        listPacketReceived.GetList = ClassUtility.GetEachPacketSplitted(readPacketData.Data, listPacketReceived, cancellationReceiveTransactionPacket).GetList;

                                        if (listPacketReceived.GetList.Count(x => x.Complete) == 0)
                                            continue;

                                        for (int i = 0; i < listPacketReceived.Count; i++)
                                        {
                                            try
                                            {
                                                if (listPacketReceived[i].Complete && listPacketReceived[i].Packet.Length > 0 && !listPacketReceived[i].Used)
                                                {
                                                    listPacketReceived[i].Used = true;

                                                    byte[] base64Data = null;

                                                    try
                                                    {
                                                        base64Data = Convert.FromBase64String(listPacketReceived[i].Packet);
                                                    }
                                                    catch
                                                    {
                                                    }

                                                    listPacketReceived[i].Packet.Clear();

                                                    ClassPeerPacketRecvObject peerPacketRecvObject = new ClassPeerPacketRecvObject(base64Data, out bool status);

                                                    if (!status)
                                                    {
                                                        receiveStatus = false;
                                                        break;
                                                    }

                                                    if (peerPacketRecvObject.PacketOrder == ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE ||
                                                    peerPacketRecvObject.PacketOrder == ClassPeerEnumPacketResponse.SEND_MEM_POOL_END_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE)
                                                    {
                                                        ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerTimestampSignatureWhitelist = peerPacketRecvObject.PeerLastTimestampSignatureWhitelist;

                                                        // Receive transaction.
                                                        if (peerPacketRecvObject.PacketOrder == ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE)
                                                        {

                                                            ClassTranslatePacket<ClassPeerPacketSendMemPoolTransaction> packetTranslated = await TranslatePacketReceived<ClassPeerPacketSendMemPoolTransaction>(peerPacketRecvObject, ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE, cancellationReceiveTransactionPacket);

                                                            if (packetTranslated.Status && packetTranslated.PacketTranslated.ListTransactionObject.Count > 0)
                                                            {

                                                                foreach (ClassTransactionObject transactionObject in packetTranslated.PacketTranslated.ListTransactionObject)
                                                                {
                                                                    if (transactionObject != null)
                                                                    {
                                                                        if (transactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION &&
                                                                        transactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                                                                        {

                                                                            txCountReceived++;
                                                                            if (!_memPoolListBlockHeightTransactionReceived[blockHeight].Contains(transactionObject.TransactionHash))
                                                                            {
                                                                                bool canInsert = false;

                                                                                if (transactionObject.BlockHeightTransaction > ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeight)
                                                                                {
                                                                                    ClassTransactionEnumStatus checkTxResult = await ClassTransactionUtility.CheckTransactionWithBlockchainData(transactionObject, true, false, true, null, 0, listWalletAddressAndPublicKeyCache, true, _peerCancellationToken);

                                                                                    if (checkTxResult == ClassTransactionEnumStatus.VALID_TRANSACTION || checkTxResult == ClassTransactionEnumStatus.DUPLICATE_TRANSACTION_HASH)
                                                                                        canInsert = true;
                                                                                }

                                                                                if (canInsert)
                                                                                {
                                                                                    if (!listWalletAddressAndPublicKeyCache.ContainsKey(transactionObject.WalletAddressSender))
                                                                                        listWalletAddressAndPublicKeyCache.Add(transactionObject.WalletAddressSender, transactionObject.WalletPublicKeySender);

                                                                                    if (transactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION)
                                                                                        listWalletAddressAndPublicKeyCache.Add(transactionObject.WalletAddressReceiver, transactionObject.WalletPublicKeyReceiver);

                                                                                    listTransactionObject.Add(transactionObject);
                                                                                }
                                                                            }

                                                                            if (listTransactionObject.Count >= countTransactionToSync)
                                                                            {
                                                                                endBroadcast = true;
                                                                                break;
                                                                            }
                                                                        }
                                                                    }
                                                                }

                                                                // Send the confirmation of receive.
                                                                if (!await TrySendPacketToPeer(new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId,
                                                                ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerInternPublicKey,
                                                                ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                                {
                                                                    PacketOrder = ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BROADCAST_CONFIRMATION_RECEIVED,
                                                                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketAskMemPoolTransactionBroadcastConfirmationReceived()
                                                                    {
                                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                                    })
                                                                }.GetPacketData(), cancellationReceiveTransactionPacket))
                                                                {
                                                                    IsAlive = false;
                                                                    endBroadcast = true;
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                        // End broadcast transaction.
                                                        else if (peerPacketRecvObject.PacketOrder == ClassPeerEnumPacketResponse.SEND_MEM_POOL_END_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE)
                                                        {
                                                            endBroadcast = true;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        receiveStatus = false;
                                                        break;
                                                    }


                                                }
                                            }
                                            catch
                                            {
                                                endBroadcast = true;
                                                break;
                                            }
                                        }

                                        // Clean up.
                                        listPacketReceived.GetList.RemoveAll(x => x.Complete);



                                        if (endBroadcast)
                                            break;
                                    }
                                }

                            }

                        }), 0, cancellationReceiveTransactionPacket, _peerSocketClient);


                        while (!endBroadcast)
                        {

                            if (!IsAlive)
                                break;

                            if (endBroadcast)
                                break;

                            await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                        }

                        cancellationReceiveTransactionPacket.Cancel();


                        if (listTransactionObject.Count > 0)
                        {
                            if (!_memPoolListBlockHeightTransactionReceived.ContainsKey(blockHeight))
                                _memPoolListBlockHeightTransactionReceived.Add(blockHeight, new HashSet<string>());

                            foreach (ClassTransactionObject transactionObject in listTransactionObject.GetAll)
                            {

                                if (!_memPoolListBlockHeightTransactionReceived[blockHeight].Contains(transactionObject.TransactionHash))
                                {

                                    if (ClassMemPoolDatabase.InsertTxToMemPool(transactionObject))
                                        ClassLog.WriteLine("[Client Broadcast] - TX Hash " + transactionObject.TransactionHash + " received from peer: " + _peerIpTarget, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                    _memPoolListBlockHeightTransactionReceived[blockHeight].Add(transactionObject.TransactionHash);
                                }
                            }
                        }
                    }
                }

                return txCountReceived > 0;
            }

            /// <summary>
            /// Try to receive a transaction vote packet received from the peer target.
            /// </summary>
            /// <returns></returns>
            private async Task<DisposableDictionary<string, ClassTransactionEnumStatus>> TryReceiveMemPoolTransactionVotePacket()
            {
                bool voteStatus = false;
                bool taskDone = false;
                DisposableDictionary<string, ClassTransactionEnumStatus> listTransactionStatus = new DisposableDictionary<string, ClassTransactionEnumStatus>();

                CancellationTokenSource cancellationReceiveMemPoolTransactionVote = CancellationTokenSource.CreateLinkedTokenSource(_peerCancellationToken.Token);


                await TaskManager.TaskManager.InsertTask(new Action(async () =>
                {
                    try
                    {
                        using (DisposableList<ClassReadPacketSplitted> listPacketReceived = new DisposableList<ClassReadPacketSplitted>())
                        {
                            listPacketReceived.Add(new ClassReadPacketSplitted());


                            while (IsAlive && !taskDone && !voteStatus)
                            {

                                using (ReadPacketData readPacketData = await _peerSocketClient.TryReadPacketData(_peerNetworkSettingObject.PeerMaxPacketBufferSize, _peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000, false, cancellationReceiveMemPoolTransactionVote))
                                {
                                    if (!readPacketData.Status)
                                        break;


                                    listPacketReceived.GetList = ClassUtility.GetEachPacketSplitted(readPacketData.Data, listPacketReceived, cancellationReceiveMemPoolTransactionVote).GetList;

                                    if (listPacketReceived.GetList.Count(x => x.Complete) == 0)
                                        continue;

                                    for (int i = 0; i < listPacketReceived.Count; i++)
                                    {
                                        if (!listPacketReceived[i].Complete || listPacketReceived[i].Used)
                                            continue;

                                        listPacketReceived[i].Used = true;

                                        bool exceptionBase64 = false;
                                        byte[] base64Data = null;

                                        try
                                        {
                                            base64Data = Convert.FromBase64String(listPacketReceived[0].Packet);
                                        }
                                        catch
                                        {
                                            exceptionBase64 = true;
                                        }

                                        listPacketReceived[i].Packet.Clear();

                                        if (!exceptionBase64)
                                        {
                                            try
                                            {
                                                ClassPeerPacketRecvObject peerPacketRecvObject = new ClassPeerPacketRecvObject(base64Data, out bool status);

                                                if (status)
                                                {
                                                    if (peerPacketRecvObject.PacketOrder == ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_VOTE)
                                                    {

                                                        ClassTranslatePacket<ClassPeerPacketSendMemPoolTransactionVote> packetTranslated = await TranslatePacketReceived<ClassPeerPacketSendMemPoolTransactionVote>(peerPacketRecvObject, ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_VOTE, cancellationReceiveMemPoolTransactionVote);

                                                        if (packetTranslated.Status)
                                                        {
                                                            ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerTimestampSignatureWhitelist = peerPacketRecvObject.PeerLastTimestampSignatureWhitelist;

                                                            listTransactionStatus.GetList = packetTranslated.PacketTranslated.ListTransactionHashResult;

                                                            voteStatus = true;
                                                        }

                                                        taskDone = true;
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        IsAlive = false;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    IsAlive = false;
                                                    break;
                                                }

                                            }
                                            catch
                                            {
                                                // Ignored.
                                            }
                                        }
                                    }

                                }
                            }

                        }
                    }
                    catch
                    {
                        // Ignored.
                    }

                    taskDone = true;
                }), 0, cancellationReceiveMemPoolTransactionVote, null);


                while (!taskDone)
                {
                    if (voteStatus)
                        break;

                    if (!IsAlive)
                        break;

                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }

                cancellationReceiveMemPoolTransactionVote.Cancel();


                return listTransactionStatus;
            }

            #endregion

            #region Packet management.

            /// <summary>
            /// Try to send a packet to the peer target.
            /// </summary>
            /// <param name="packetData"></param>
            /// <returns></returns>
            private async Task<bool> TrySendPacketToPeer(byte[] packetData, CancellationTokenSource cancellation)
            {
                return await _peerSocketClient.TrySendSplittedPacket((Convert.ToBase64String(packetData) + ClassPeerPacketSetting.PacketPeerSplitSeperator).GetByteArray(), cancellation, _peerNetworkSettingObject.PeerMaxPacketSplitedSendSize, false);
            }



            /// <summary>
            /// Translate the packet received.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="peerPacketRecvObject"></param>
            /// <param name="packetResponseExpected"></param>
            /// <returns></returns>
            private async Task<ClassTranslatePacket<T>> TranslatePacketReceived<T>(ClassPeerPacketRecvObject peerPacketRecvObject, ClassPeerEnumPacketResponse packetResponseExpected, CancellationTokenSource cancellation)
            {
                ClassTranslatePacket<T> translatedPacket = new ClassTranslatePacket<T>();

                if (peerPacketRecvObject != null)
                {
                    if (!peerPacketRecvObject.PacketContent.IsNullOrEmpty(false, out _))
                    {
                        bool peerIgnorePacketSignature = ClassPeerCheckManager.CheckPeerClientWhitelistStatus(_peerIpTarget, _peerUniqueIdTarget, _peerNetworkSettingObject);

                        if (!peerIgnorePacketSignature)
                            peerIgnorePacketSignature = ClassPeerDatabase.DictionaryPeerDataObject[_peerIpTarget][_peerUniqueIdTarget].PeerTimestampSignatureWhitelist >= TaskManager.TaskManager.CurrentTimestampSecond;

                        bool packetSignatureStatus = true;

                        if (!peerIgnorePacketSignature)
                        {
                            packetSignatureStatus = await CheckPacketSignature(_peerIpTarget,
                                                            _peerUniqueIdTarget,
                                                            _peerNetworkSettingObject,
                                                            peerPacketRecvObject.PacketContent,
                                                            packetResponseExpected,
                                                            peerPacketRecvObject.PacketHash,
                                                            peerPacketRecvObject.PacketSignature,
                                                            _peerCancellationToken);
                        }

                        if (packetSignatureStatus)
                        {
                            if (TryDecryptPacketPeerContent(_peerIpTarget, _peerUniqueIdTarget, peerPacketRecvObject.PacketContent, _peerCheckConnectionCancellationToken, out byte[] packetDecrypted))
                            {
                                if (packetDecrypted != null)
                                {
                                    if (packetDecrypted.Length > 0)
                                    {
                                        if (DeserializePacketContent(packetDecrypted.GetStringFromByteArrayUtf8(), out translatedPacket.PacketTranslated))
                                        {
                                            if (!translatedPacket.PacketTranslated.Equals(default(T)))
                                                translatedPacket.Status = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return translatedPacket;
            }

            #endregion

            #region Internal objects.

            /// <summary>
            /// Store the packet data translated after every checks passed.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            internal class ClassTranslatePacket<T>
            {
                public T PacketTranslated;
                public bool Status;
            }

            #endregion
        }
    }
}