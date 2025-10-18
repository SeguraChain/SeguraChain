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
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Instance.Node.Network.Database.Manager;
using SeguraChain_Lib.Other.Object.List;
using System.Diagnostics;
using SeguraChain_Lib.Instance.Node.Network.Database.Object;
using System.Collections.Concurrent;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.ClientConnect.Object;
using SeguraChain_Lib.Blockchain.Wallet.Function;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast
{
    public class ClassPeerNetworkBroadcastInstanceMemPool
    {
        private ClassPeerDatabase _peerDatabase;
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
        public ClassPeerNetworkBroadcastInstanceMemPool(ClassPeerDatabase peerDatabase, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject, string peerOpenNatIp)
        {
            _peerDatabase = peerDatabase;
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
                    try
                    {
                        #region Generate sender/receiver broadcast instances by targetting public peers.

                        if (_peerDatabase.Count > 0)
                        {
                            foreach (string peerIpTarget in _peerDatabase.Keys.ToArray())
                            {

                                if (peerIpTarget.IsNullOrEmpty(false, out _))
                                    continue;


                                ConcurrentDictionary<string, ClassPeerObject> peerListObject = _peerDatabase[peerIpTarget, _cancellation];

                                if (peerListObject == null)
                                    continue;

                                foreach (string peerUniqueIdTarget in peerListObject.Keys.ToArray())
                                {

                                    if (peerUniqueIdTarget.IsNullOrEmpty(false, out _))
                                        continue;

                                    bool success = false;

                                    ClassPeerObject peerObject = _peerDatabase[peerIpTarget, peerUniqueIdTarget, _cancellation];

                                    if (peerObject.PeerIsPublic)
                                    {


                                        int peerPortTarget = peerObject.PeerPort;


                                        // Build receiver instance.
                                        if (!_listPeerNetworkClientBroadcastMemPoolReceiver.ContainsKey(peerIpTarget))
                                            _listPeerNetworkClientBroadcastMemPoolReceiver.Add(peerIpTarget, new Dictionary<string, ClassPeerNetworkClientBroadcastMemPool>());

                                        if (!_listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget].ContainsKey(peerUniqueIdTarget))
                                        {
                                            _listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget].Add(peerUniqueIdTarget, new ClassPeerNetworkClientBroadcastMemPool(
                                                                                                                                   _peerDatabase,
                                                                                                                                   _cancellation,
                                                                                                                                   peerIpTarget,
                                                                                                                                   peerUniqueIdTarget,
                                                                                                                                   false,
                                                                                                                                   _peerNetworkSettingObject,
                                                                                                                                   _peerFirewallSettingObject));
                                            if (!RunPeerNetworkClientBroadcastMemPool(peerIpTarget, peerUniqueIdTarget, false))
                                            {
                                                if (!_listPeerNetworkClientBroadcastMemPoolReceiver.ContainsKey(peerIpTarget) ||
                                                    !_listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget].ContainsKey(peerUniqueIdTarget))
                                                    continue;

                                                _listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget][peerUniqueIdTarget].StopTaskAndDisconnect();
                                                _listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget].Remove(peerUniqueIdTarget);

                                            }
                                            else
                                                success = true;


#if DEBUG
                                        Debug.WriteLine("Run Sync MemPool Receive Mode transaction from Peer: " + peerIpTarget + ":" + peerUniqueIdTarget + " | Status: " + (success ? "success" : "failed"));
#endif
                                            ClassLog.WriteLine("Run Sync MemPool Receive Mode transaction from Peer: " + peerIpTarget + ":" + peerUniqueIdTarget + " | Status: " + (success ? "success" : "failed") + ".", ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, success ? ConsoleColor.Green : ConsoleColor.Red);
                                        }

                                        if (!success)
                                            continue;

                                        // Build sender instance if the node is in public node.

                                        if (!_listPeerNetworkClientBroadcastMemPoolSender.ContainsKey(peerIpTarget))
                                            _listPeerNetworkClientBroadcastMemPoolSender.Add(peerIpTarget, new Dictionary<string, ClassPeerNetworkClientBroadcastMemPool>());

                                        if (!_listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget].ContainsKey(peerUniqueIdTarget))
                                        {
                                            _listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget].Add(peerUniqueIdTarget, new ClassPeerNetworkClientBroadcastMemPool(
                                                                                                                                   _peerDatabase,
                                                                                                                                   _cancellation,
                                                                                                                                   peerIpTarget,
                                                                                                                                   peerUniqueIdTarget,
                                                                                                                                   true,
                                                                                                                                   _peerNetworkSettingObject,
                                                                                                                                   _peerFirewallSettingObject));

                                            bool successRunClientBroadcast = true;

                                            if (!RunPeerNetworkClientBroadcastMemPool(peerIpTarget, peerUniqueIdTarget, true))
                                            {
                                                _listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget][peerUniqueIdTarget].StopTaskAndDisconnect();
                                                _listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget].Remove(peerUniqueIdTarget);
                                                successRunClientBroadcast = false;
                                            }

#if DEBUG
                                        Debug.WriteLine("Run Sync Sending Mode MemPool transaction from Peer: " + peerIpTarget + " | Status: " + (successRunClientBroadcast ? "success" : "failed") + ".");
#endif
                                            ClassLog.WriteLine("Run Sync Sending Mode MemPool transaction from Peer: " + peerIpTarget + " | Status: " + (successRunClientBroadcast ? "success" : "failed") + ".", ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, successRunClientBroadcast ? ConsoleColor.Green : ConsoleColor.Red);

                                        }

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
                                                _listPeerNetworkClientBroadcastMemPoolReceiver[peerIp].Remove(peerUniqueId);
#if DEBUG
                                                Debug.WriteLine("MemPool Broadcast instance receiver of peer " + peerIp + ":" + peerUniqueId + " is dead.");
#endif
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
#if DEBUG
                                                    Debug.WriteLine("MemPool Broadcast instance sender of peer "+peerIp+":"+peerUniqueId+" is dead.");
#endif
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
                    }
                    catch(Exception error)
                    {
                        ClassLog.WriteLine("Error on generating MemPool instance. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                    }
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
        private bool RunPeerNetworkClientBroadcastMemPool(string peerIpTarget, string peerUniqueIdTarget, bool onSendingMode)
        {
            try
            {
                if (!onSendingMode)
                    _listPeerNetworkClientBroadcastMemPoolReceiver[peerIpTarget][peerUniqueIdTarget].RunBroadcastTransactionTask();
                else
                    _listPeerNetworkClientBroadcastMemPoolSender[peerIpTarget][peerUniqueIdTarget].RunBroadcastTransactionTask();
            }
            catch
            {
                return false;
            }

            return true;
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
            private ClassPeerDatabase _peerDatabase;
            public bool IsAlive;
            private string _peerIpTarget;
            private string _peerUniqueIdTarget;
            private ClassPeerNetworkSettingObject _peerNetworkSettingObject;
            private ClassPeerFirewallSettingObject _peerFirewallSettingObject;
            private CancellationTokenSource _peerCancellationToken;
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
            public ClassPeerNetworkClientBroadcastMemPool(ClassPeerDatabase peerDatabase, CancellationTokenSource peerCancellation, string peerIpTarget, string peerUniqueIdTarget, bool onSendBroadcastMode, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
            {
                _peerDatabase = peerDatabase;
                _peerCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(peerCancellation.Token);
                _peerIpTarget = peerIpTarget;
                _peerUniqueIdTarget = peerUniqueIdTarget;
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

                if (!_onSendBroadcastMode)
                    EnableReceiveBroadcastTransactionTask();
                else
                    EnableSendBroadcastTransactionTask();
            }


            /// <summary>
            /// Enable the received broadcast transaction task.
            /// </summary>
            private void EnableReceiveBroadcastTransactionTask()
            {


                TaskManager.TaskManager.InsertTask(new Action(async () =>
                {
                    using (DisposableDictionary<string, string> listWalletAddressAndPublicKeyCache = new DisposableDictionary<string, string>())
                    {
                        while (IsAlive)
                        {
                            try
                            {
                                ClassPeerObject peerObject = _peerDatabase[_peerIpTarget, _peerUniqueIdTarget, _peerCancellationToken];

                                if (peerObject == null)
                                {
                                    IsAlive = false;
                                    break;
                                }

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
                                    peerObject.PeerInternPublicKey,
                                    peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                {
                                    PacketOrder = ClassPeerEnumPacketSend.ASK_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE,
                                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskMemPoolBlockHeightList()
                                    {
                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                    }),
                                };


                                packetSendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(_peerDatabase, packetSendObject, _peerIpTarget, _peerUniqueIdTarget, false, _peerNetworkSettingObject, _peerCancellationToken);

                                if (packetSendObject == null)
                                {
                                    IsAlive = false;
                                    break;
                                }

                                ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject = new ClassPeerNetworkClientSyncObject(_peerDatabase, _peerIpTarget, _peerDatabase[_peerIpTarget, _peerUniqueIdTarget, _peerCancellationToken].PeerPort, _peerUniqueIdTarget, _peerNetworkSettingObject, _peerFirewallSettingObject);


#if DEBUG
                            Debug.WriteLine("Try to send ask MemPool block height list: " + _peerIpTarget + " request.");
#endif
                                ClassLog.WriteLine("Try to send ask MemPool block height list: " + _peerIpTarget + " request.", ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);

                                if (!await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(packetSendObject, true, peerObject.PeerPort, _peerUniqueIdTarget, _peerCancellationToken, ClassPeerEnumPacketResponse.SEND_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE, false, false))
                                {
#if DEBUG
                                Debug.WriteLine("Try to send ask MemPool block height list: " + _peerIpTarget + " failed.");
#endif
                                    ClassLog.WriteLine("Try to send ask MemPool block height list: " + _peerIpTarget + " failed.", ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);

                                    IsAlive = false;
                                    break;
                                }

                                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                                {
                                    IsAlive = false;
                                    break;
                                }
#if DEBUG
                            Debug.WriteLine("Request to ask MemPool block height list: " + _peerIpTarget + " done.");
#endif

                                ClassTranslatePacket<ClassPeerPacketSendMemPoolBlockHeightList> peerPacketMemPoolBlockHeightListTranslated = await TranslatePacketReceived<ClassPeerPacketSendMemPoolBlockHeightList>(peerNetworkClientSyncObject.PeerPacketReceived, ClassPeerEnumPacketResponse.SEND_MEM_POOL_BLOCK_HEIGHT_LIST_BROADCAST_MODE, _peerCancellationToken);

                                if (peerPacketMemPoolBlockHeightListTranslated.PacketTranslated == null)
                                {
                                    IsAlive = false;
                                    break;
                                }

                                ClassLog.WriteLine("Request to ask MemPool block height list: " + _peerIpTarget + " done. Result: " + JsonConvert.SerializeObject(peerPacketMemPoolBlockHeightListTranslated.PacketTranslated.MemPoolBlockHeightListAndCount), ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);

#if DEBUG
                            Debug.WriteLine("MemPool block height list received from: " + _peerIpTarget + " | " + JsonConvert.SerializeObject(peerPacketMemPoolBlockHeightListTranslated.PacketTranslated.MemPoolBlockHeightListAndCount));
#endif

                                if (peerPacketMemPoolBlockHeightListTranslated == null || !peerPacketMemPoolBlockHeightListTranslated.Status)
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
                                        if (_peerCancellationToken.IsCancellationRequested || failed)
                                            break;

                                        if (ClassBlockchainDatabase.BlockchainMemoryManagement.GetLastBlockHeight < blockHeight)
                                        {
                                            int countAlreadySynced = 0;

                                            if (_memPoolListBlockHeightTransactionReceived.ContainsKey(blockHeight))
                                                countAlreadySynced = _memPoolListBlockHeightTransactionReceived[blockHeight].Count;
                                            else
                                                _memPoolListBlockHeightTransactionReceived.Add(blockHeight, new HashSet<string>());


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
                                                        peerObject.PeerInternPublicKey,
                                                        peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                    {
                                                        PacketOrder = ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE,
                                                        PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskMemPoolTransactionList()
                                                        {
                                                            BlockHeight = blockHeight,
                                                            TotalTransactionProgress = 0,
                                                            PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                                                        }),
                                                    };

                                                    packetSendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(_peerDatabase, packetSendObject, _peerIpTarget, _peerUniqueIdTarget, false, _peerNetworkSettingObject, _peerCancellationToken);

                                                    if (!await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(packetSendObject, true, peerObject.PeerPort, _peerUniqueIdTarget, _peerCancellationToken, ClassPeerEnumPacketResponse.SEND_MEM_POOL_END_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE, false, false))
                                                    {
                                                        IsAlive = false;
                                                        failed = true;
                                                        break;
                                                    }


                                                    ClassTranslatePacket<ClassPeerPacketSendMemPoolTransaction> packetTranslated = await TranslatePacketReceived<ClassPeerPacketSendMemPoolTransaction>(peerNetworkClientSyncObject.PeerPacketReceived, ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_BY_BLOCK_HEIGHT_BROADCAST_MODE, _peerCancellationToken);

                                                    if (packetTranslated.Status && packetTranslated.PacketTranslated.ListTransactionObject.Count > 0)
                                                    {

                                                        foreach (ClassTransactionObject transactionObject in packetTranslated.PacketTranslated.ListTransactionObject)
                                                        {
                                                            if (transactionObject != null)
                                                            {

#if DEBUG
                                                            Debug.WriteLine("Transaction object received from peer: " + _peerIpTarget + " | Data: " + JsonConvert.SerializeObject(transactionObject));
#endif
                                                                ClassLog.WriteLine("Transaction object received from peer: " + _peerIpTarget + " | Data: " + JsonConvert.SerializeObject(transactionObject), ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);

                                                                if (transactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION &&
                                                                transactionObject.TransactionType != ClassTransactionEnumType.DEV_FEE_TRANSACTION)
                                                                {

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
                                                                            ClassMemPoolDatabase.InsertTxToMemPool(transactionObject);
                                                                            _memPoolListBlockHeightTransactionReceived[blockHeight].Add(transactionObject.TransactionHash);
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
                                }

                                #endregion
                            }
                            catch(Exception error)
                            {
                                ClassLog.WriteLine("Task to receive MemPool transaction failed from: " + _peerIpTarget + " | Exception. Result: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_MEMPOOL_BROADCAST, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                            }
                            await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                        }
                    }
                }), 0, _peerCancellationToken).Wait();

#if DEBUG
                Debug.WriteLine("Enable Receive Broadcast transaction MemPool from: " + _peerIpTarget + " done. " + IsAlive);
#endif
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
                                                ClassPeerPacketSendObject packetSendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, _peerDatabase[_peerIpTarget, _peerUniqueIdTarget, _peerCancellationToken].PeerInternPublicKey, _peerDatabase[_peerIpTarget, _peerUniqueIdTarget, _peerCancellationToken].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                                                {
                                                    PacketOrder = ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_VOTE,
                                                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskMemPoolTransactionVote()
                                                    {
                                                        ListTransactionObject = listTransactionObjectToSend.GetList,
                                                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                                                    })
                                                };

                                                packetSendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(_peerDatabase, packetSendObject, _peerIpTarget, _peerUniqueIdTarget, false, _peerNetworkSettingObject, _peerCancellationToken);

                                                ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject = new ClassPeerNetworkClientSyncObject(_peerDatabase, _peerIpTarget, _peerDatabase[_peerIpTarget, _peerUniqueIdTarget, _peerCancellationToken].PeerPort, _peerUniqueIdTarget, _peerNetworkSettingObject, _peerFirewallSettingObject);

                                                if (!await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(packetSendObject, true, _peerDatabase[_peerIpTarget, _peerUniqueIdTarget, _peerCancellationToken].PeerPort, _peerUniqueIdTarget, _peerCancellationToken, ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_VOTE, true, false))
                                                {
                                                    IsAlive = false;
                                                    break;
                                                }

                                                ClassTranslatePacket<ClassPeerPacketSendMemPoolTransactionVote> packetTranslated = await TranslatePacketReceived<ClassPeerPacketSendMemPoolTransactionVote>(peerNetworkClientSyncObject.PeerPacketReceived, ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_VOTE, _peerCancellationToken);

                                                if (packetTranslated.Status)
                                                {
                                                    foreach (ClassTransactionObject transactionObject in listTransactionObjectToSend.GetList)
                                                        _memPoolListBlockHeightTransactionSend[blockHeight].Add(transactionObject.TransactionHash);

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


                }), 0, _peerCancellationToken).Wait();

#if DEBUG
                Debug.WriteLine("Enable Send Broadcast transaction MemPool from: " + _peerIpTarget + " done.");
#endif
            }

            #endregion

            #region Packet management.



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
                        try
                        {
                            bool peerIgnorePacketSignature = ClassPeerCheckManager.CheckPeerClientWhitelistStatus(_peerDatabase, _peerIpTarget, _peerUniqueIdTarget, _peerNetworkSettingObject, cancellation);

                            if (!peerIgnorePacketSignature)
                                peerIgnorePacketSignature = _peerDatabase[_peerIpTarget, _peerUniqueIdTarget, _peerCancellationToken].PeerTimestampSignatureWhitelist >= TaskManager.TaskManager.CurrentTimestampSecond;

                            bool packetSignatureStatus = true;

                            if (!peerIgnorePacketSignature)
                            {
                                packetSignatureStatus = await CheckPacketSignature(
                                                                _peerDatabase,
                                                                _peerIpTarget,
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
                                if (TryDecryptPacketPeerContent(_peerDatabase, _peerIpTarget, _peerUniqueIdTarget, peerPacketRecvObject.PacketContent, _peerCancellationToken, out byte[] packetDecrypted))
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
                        catch
                        {
                            translatedPacket.Status = false;
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