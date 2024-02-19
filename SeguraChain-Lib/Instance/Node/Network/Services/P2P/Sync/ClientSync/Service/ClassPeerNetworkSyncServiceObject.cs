using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Function;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Enum;
using SeguraChain_Lib.Blockchain.MemPool.Database;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Sovereign.Database;
using SeguraChain_Lib.Blockchain.Sovereign.Object;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Instance.Node.Network.Database.Manager;
using SeguraChain_Lib.Instance.Node.Network.Database.Object;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Status;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.ClientConnect.Object;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.Enum;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.Function;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.PacketObject;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Request;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.Service
{
    public class ClassPeerNetworkSyncServiceObject : ClassPeerSyncFunction, IDisposable
    {
        /// <summary>
        /// Settings.
        /// </summary>
        private ClassPeerDatabase _peerDatabase;
        private ClassPeerNetworkSettingObject _peerNetworkSettingObject;
        private ClassPeerFirewallSettingObject _peerFirewallSettingObject;
        public string PeerOpenNatServerIp;

        /// <summary>
        /// Status and cancellation of the sync service.
        /// </summary>
        private CancellationTokenSource _cancellationTokenServiceSync;
        private bool _peerSyncStatus;
        public long PeerTotalUnexpectedPacketReceived;

        /// <summary>
        /// Network informations saved.
        /// </summary>
        private ClassPeerPacketSendNetworkInformation _packetNetworkInformation;



        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="peerOpenNatServerIp"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public ClassPeerNetworkSyncServiceObject(ClassPeerDatabase peerDatabase, string peerOpenNatServerIp, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            _peerDatabase = peerDatabase;
            PeerOpenNatServerIp = peerOpenNatServerIp;
            _peerNetworkSettingObject = peerNetworkSettingObject;
            _peerFirewallSettingObject = peerFirewallSettingObject;
        }

        #region Dispose functions

        private bool _disposed;

        ~ClassPeerNetworkSyncServiceObject()
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
                StopPeerSyncTask();

            _disposed = true;
        }
        #endregion

        #region Peer Task Sync - Manage functions.

        /// <summary>
        /// Enable peer sync task.
        /// </summary>
        public void EnablePeerSyncTask()
        {
            _cancellationTokenServiceSync = new CancellationTokenSource();
            _peerSyncStatus = true;

            // Sync peer lists from other peers.
            StartTaskSyncPeerList();

            // Sync sovereign update(s) from other peers.
            StartTaskSyncSovereignUpdate();

            // Sync blocks and tx's from other peers.
            StartTaskSyncBlockAndTx();

            // Resync blocks and tx's who need to be corrected from other peers.
            StartTaskSyncCheckBlockAndTx();

            // Sync last network informations from other peers.
            StartTaskSyncNetworkInformations();
        }

        /// <summary>
        /// Stop peer tasks.
        /// </summary>
        public void StopPeerSyncTask()
        {
            if (_peerSyncStatus)
            {
                _peerSyncStatus = false;
                try
                {
                    if (_cancellationTokenServiceSync != null)
                    {
                        if (!_cancellationTokenServiceSync.IsCancellationRequested)
                            _cancellationTokenServiceSync.Cancel();
                    }
                }
                catch
                {
                    // Ignored.
                }
            }
        }

        #endregion

        #region Peer Task Sync - Manage Connectivity with peers functions.

        /// <summary>
        /// Launch emergency check tasks of peers functions.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> StartCheckHealthPeers()
        {
            ClassLog.WriteLine("Attempt to check dead public peers..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
            int totalDeadPeerRestored = await StartCheckDeadPeers();

            ClassLog.WriteLine("Attempt to initialize public peers who are not initialized propertly..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
            int totalPeerInitialized = await StartInitializePeersNotInitialized();

            if (totalDeadPeerRestored > 0 || totalPeerInitialized > 0)
                return true;

            ClassLog.WriteLine("Any peers checked retrieved back alive. Try to contact default peers.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

            return false;
        }

        /// <summary>
        /// Ask a peer list to default peer list.
        /// </summary>
        /// <returns></returns>
        private async Task StartContactDefaultPeerList()
        {
            if (_peerDatabase.Count == 0)
                ClassLog.WriteLine("The peer don't have any public peer listed. Contact default peer list to get a new peer list..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

            foreach (string peerIp in BlockchainSetting.BlockchainStaticPeerList.Keys)
            {
                if (peerIp != _peerNetworkSettingObject.ListenIp && peerIp != PeerOpenNatServerIp)
                {
                    foreach (string peerUniqueId in BlockchainSetting.BlockchainStaticPeerList[peerIp].Keys)
                    {
                        int peerPort = BlockchainSetting.BlockchainStaticPeerList[peerIp][peerUniqueId];

                        if (!await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(_peerDatabase, peerIp, peerPort, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject), _cancellationTokenServiceSync, true))
                            ClassLog.WriteLine("Can't send auth keys to default peer: " + peerIp + ":" + peerPort + " | Peer Unique ID: " + peerUniqueId, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                        else
                        {
                            if (_peerDatabase.ContainsPeerIp(peerIp, _cancellationTokenServiceSync))
                            {
                                if (_peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, _cancellationTokenServiceSync))
                                {

                                    if (_peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerIsPublic)
                                        ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _cancellationTokenServiceSync);
                                }
                            }
                        }
                    }
                }
            }

            IPAddress[] peerAddresses = null;

            bool successReadDns = true;

            try
            {
                peerAddresses = Dns.GetHostAddresses(BlockchainSetting.BlockchainStaticSeedList);
            }
            catch
            {
                successReadDns = false;
            }

            if (successReadDns)
            {
                foreach (var peerAddress in peerAddresses)
                {
                    string peerIp = peerAddress.ToString();

                    if (peerIp != _peerNetworkSettingObject.ListenIp && peerIp != PeerOpenNatServerIp)
                    {
                        int peerPort = BlockchainSetting.PeerDefaultPort;


                        if (!await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(_peerDatabase, peerIp, peerPort, string.Empty, _peerNetworkSettingObject, _peerFirewallSettingObject), _cancellationTokenServiceSync, true))
                            ClassLog.WriteLine("Can't send auth keys to default peer: " + peerIp + ":" + peerPort , ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

                    }
                }
            }
        }

        /// <summary>
        /// Run multiple async task to initialize auth keys from uninitilized peers.
        /// </summary>
        /// <returns></returns>
        private async Task<int> StartInitializePeersNotInitialized()
        {
            int totalInitializedSuccessfully = 0;

            try
            {
                using (DisposableList<string> peerList = new DisposableList<string>(false, 0, _peerDatabase.Keys.ToList()))
                {
                    using (DisposableList<Tuple<string, string>> peerListToInitialize = new DisposableList<Tuple<string, string>>()) // Peer IP | Peer unique id.
                    {

                        foreach (var peerIp in peerList.GetList)
                        {
                            foreach (string peerUniqueId in _peerDatabase[peerIp, _cancellationTokenServiceSync].Keys.ToArray())
                            {
                                ClassPeerObject peerObject = _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync];

                                if (peerObject == null || !peerObject.PeerIsPublic)
                                    continue;

                                if (!ClassPeerCheckManager.CheckPeerClientStatus(_peerDatabase, peerIp, peerUniqueId, false, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenServiceSync))
                                {
                                    if (peerObject.PeerStatus == ClassPeerEnumStatus.PEER_BANNED && _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerBanDate + _peerNetworkSettingObject.PeerBanDelay < TaskManager.TaskManager.CurrentTimestampSecond)
                                        peerListToInitialize.Add(new Tuple<string, string>(peerIp, peerUniqueId));
                                    else if (peerObject.PeerStatus == ClassPeerEnumStatus.PEER_DEAD && _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerBanDate + _peerNetworkSettingObject.PeerDeadDelay < TaskManager.TaskManager.CurrentTimestampSecond)
                                        peerListToInitialize.Add(new Tuple<string, string>(peerIp, peerUniqueId));
                                }
                                else if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(_peerDatabase, peerIp, peerUniqueId, _cancellationTokenServiceSync))
                                    peerListToInitialize.Add(new Tuple<string, string>(peerIp, peerUniqueId));

                            }
                        }

                        if (peerListToInitialize.Count > 0)
                        {
                            int totalTaskCount = peerListToInitialize.Count;
                            int totalPeerRemoved = 0;
                            int totalTaskComplete = 0;

                            for (int i = 0; i < totalTaskCount; i++)
                            {
                                if (i < totalTaskCount)
                                {

                                    var i1 = i;

                                    var copyPeer = new Tuple<string, string>(peerListToInitialize[i].Item1, peerListToInitialize[i].Item2);

                                    await TaskManager.TaskManager.InsertTask(new Action(async () =>
                                    {
                                        try
                                        {

                                            int peerPort = _peerDatabase[copyPeer.Item1, copyPeer.Item2, _cancellationTokenServiceSync].PeerPort;

                                            if (await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(_peerDatabase, copyPeer.Item1, peerPort, copyPeer.Item2, _peerNetworkSettingObject, _peerFirewallSettingObject), CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource(_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000).Token), true))
                                            {
                                                totalInitializedSuccessfully++;
                                                ClassPeerCheckManager.CleanPeerState(_peerDatabase, copyPeer.Item1, copyPeer.Item2, true, _cancellationTokenServiceSync);
                                                ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, copyPeer.Item1, copyPeer.Item2, _peerNetworkSettingObject, _cancellationTokenServiceSync);
                                            }
                                            else
                                            {
                                                ClassLog.WriteLine("Peer to initialize " + copyPeer.Item1 + " is completly dead after asking auth keys, remove it from peer list registered.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                                if (_peerDatabase.ContainsPeerIp(copyPeer.Item1, _cancellationTokenServiceSync))
                                                {
                                                    if (_peerDatabase[copyPeer.Item1, _cancellationTokenServiceSync].ContainsKey(copyPeer.Item2))
                                                    {
                                                        if (_peerDatabase[copyPeer.Item1, _cancellationTokenServiceSync].TryRemove(copyPeer.Item2, out _))
                                                        {
                                                            totalPeerRemoved++;
                                                            if (_peerDatabase[copyPeer.Item1, _cancellationTokenServiceSync].Count == 0)
                                                                _peerDatabase.RemovePeer(copyPeer.Item1, _cancellationTokenServiceSync);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            // Ignored.
                                        }

                                        totalTaskComplete++;
                                    }), 0, null);

                                }
                            }

                            while (totalTaskComplete < totalTaskCount)
                                await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);



                            ClassLog.WriteLine("Total Peer(s) initialization Task(s) complete: " + totalTaskComplete + "/" + totalTaskCount, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                            ClassLog.WriteLine("Total Peer(s) initialized successfully: " + totalInitializedSuccessfully + "/" + totalTaskComplete + " Task(s) complete.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                            ClassLog.WriteLine("Total Peer(s) to initialize removed completly: " + totalPeerRemoved + "/" + totalTaskComplete + " Task(s) complete.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);

                        }
                        else
                            ClassLog.WriteLine("No peer(s) available to initialize", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                    }
                }
            }
            catch(Exception error)
            {
                ClassLog.WriteLine("Can't initialize peers uninitialized. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red); ;
            }
            return totalInitializedSuccessfully;
        }

        /// <summary>
        /// Run multiple async task to check again dead peers.
        /// </summary>
        /// <returns></returns>
        private async Task<int> StartCheckDeadPeers()
        {
            int totalCheckSuccessfullyDone = 0;

            using (DisposableList<Tuple<string, string>> peerListToCheck = new DisposableList<Tuple<string, string>>()) // Peer IP | Peer unique id.
            {
                foreach (var peer in _peerDatabase.Keys.ToArray())
                {
                    if (_peerDatabase.ContainsPeerIp(peer, _cancellationTokenServiceSync))
                    {
                        if (_peerDatabase[peer, _cancellationTokenServiceSync].Count > 0)
                        {
                            foreach (string peerUniqueId in _peerDatabase[peer, _cancellationTokenServiceSync].Keys.ToArray())
                            {
                                if (_peerDatabase[peer, peerUniqueId, _cancellationTokenServiceSync].PeerIsPublic)
                                {
                                    if (_peerDatabase[peer, peerUniqueId, _cancellationTokenServiceSync].PeerStatus == ClassPeerEnumStatus.PEER_DEAD)
                                    {
                                        if (!peer.IsNullOrEmpty(false, out _))
                                            peerListToCheck.Add(new Tuple<string, string>(peer, peerUniqueId));
                                    }
                                }
                            }
                        }
                    }
                }

                if (peerListToCheck.Count > 0)
                {

                    int totalTaskCount = peerListToCheck.Count;
                    int totalPeerRemoved = 0;
                    int totalTaskComplete = 0;

                    for (int i = 0; i < totalTaskCount; i++)
                    {
                        if (i < totalTaskCount)
                        {

                            var i1 = i;
                            await TaskManager.TaskManager.InsertTask(new Action(async () =>
                            {

                                try
                                {
                                    string peerIp = peerListToCheck[i1].Item1;
                                    string peerUniqueId = peerListToCheck[i1].Item2;
                                    int peerPort = _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerPort;

                                    if (await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(_peerDatabase, peerIp, peerPort, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject), CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token), true))
                                    {
                                        totalCheckSuccessfullyDone++;
                                        ClassPeerCheckManager.CleanPeerState(_peerDatabase, peerIp, peerUniqueId, true, _cancellationTokenServiceSync);
                                        ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _cancellationTokenServiceSync);
                                    }
                                }
                                catch
                                {
                                    // Ignored.
                                }

                                totalTaskComplete++;
                            }), 0, null);

                        }
                    }

                    while (totalTaskComplete < totalTaskCount)
                        await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);



                    ClassLog.WriteLine("Total Peer(s) Dead checked Task(s) complete: " + totalTaskComplete + "/" + totalTaskCount, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    ClassLog.WriteLine("Total Peer(s) Dead checked recovery state successfully: " + totalCheckSuccessfullyDone + "/" + totalTaskComplete + " Task(s) complete.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    ClassLog.WriteLine("Total Peer(s) Dead checked removed completly: " + totalPeerRemoved + "/" + totalTaskComplete + " Task(s) complete.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);

                }
                else
                    ClassLog.WriteLine("No dead peer(s) available to check.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

            }


            return totalCheckSuccessfullyDone;
        }

        #endregion

        #region Peer Task Sync - Task Sync functions.

        /// <summary>
        /// Start the task who sync peer lists from other peers.
        /// </summary>
        private void StartTaskSyncPeerList()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {

                Dictionary<int, ClassPeerTargetObject> peerTargetList = null;

                while (_peerSyncStatus)
                {

                    await StartContactDefaultPeerList();
                    await StartCheckHealthPeers();

                    if (_peerDatabase.Count > 0)
                    {
                        try
                        {
                            peerTargetList = GenerateOrUpdatePeerTargetList(peerTargetList);

                            // If true, run every peer check tasks functions.
                            if (peerTargetList.Count > 0)
                            {
                                ClassLog.WriteLine(peerTargetList.Count + " Peer(s) available to use for sync.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                                ClassLog.WriteLine("Ask peer list(s) to other peers.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

                                int countPeer = await StartAskPeerListFromListPeerTarget(peerTargetList);


                                ClassLog.WriteLine(countPeer + " peer lists are received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

                            }

                            ClearPeerTargetList(peerTargetList, false);
                        }
                        catch(Exception error)
                        {
                            ClassLog.WriteLine("Error on list peer target. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                        }
                    }


                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }

            }), 0, _cancellationTokenServiceSync).Wait();

        }

        /// <summary>
        /// Start the task who sync sovereign update(s) from other peers.
        /// </summary>
        private void StartTaskSyncSovereignUpdate()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                Dictionary<int, ClassPeerTargetObject> peerTargetList = null;

                while (_peerSyncStatus)
                {

                    if (_peerDatabase.Count > 0)
                    {

                        peerTargetList = GenerateOrUpdatePeerTargetList(peerTargetList);

                        // If true, run every peer check tasks functions.
                        if (peerTargetList?.Count > 0)
                            ClassLog.WriteLine("Total sovereign update(s) received: " + await StartAskSovereignUpdateListFromListPeerTarget(peerTargetList), ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);


                        ClearPeerTargetList(peerTargetList, false);
                    }


                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }

            }), 0, _cancellationTokenServiceSync).Wait();

        }

        /// <summary>
        /// Start the task who sync blocks and tx's from other peers.
        /// </summary>
        private void StartTaskSyncBlockAndTx()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                Dictionary<int, ClassPeerTargetObject> peerTargetList = null;


                while (_peerSyncStatus)
                {


                    if (_peerDatabase.Count > 0)
                    {
                        long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();

                        peerTargetList = ClassPeerNetworkBroadcastFunction.GetLastPeerTargetSynced(_peerDatabase, peerTargetList, lastBlockHeight, _peerNetworkSettingObject.ListenIp, PeerOpenNatServerIp, string.Empty, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenServiceSync);
                        bool forceDisconnect = false;

                        // If true, run every peer check tasks functions.
                        if (peerTargetList.Count > 0)
                        {

                            #region Sync block objects and transaction(s).

                            long lastBlockHeightUnlocked = await ClassBlockchainStats.GetLastBlockHeightUnlocked(_cancellationTokenServiceSync);
                            long lastBlockHeightUnlockedChecked = await ClassBlockchainStats.GetLastBlockHeightNetworkConfirmationChecked(_cancellationTokenServiceSync);


                            long lastBlockHeightTarget = 0;

                            foreach (var peer in peerTargetList.Values)
                            {
                                if (!_peerDatabase.ContainsPeerUniqueId(peer.PeerIpTarget, peer.PeerUniqueIdTarget, _cancellationTokenServiceSync) || lastBlockHeightTarget > _peerDatabase[peer.PeerIpTarget, peer.PeerUniqueIdTarget, _cancellationTokenServiceSync].PeerClientLastBlockHeight)
                                    continue;

                                lastBlockHeightTarget = _peerDatabase[peer.PeerIpTarget, peer.PeerUniqueIdTarget, _cancellationTokenServiceSync].PeerClientLastBlockHeight;
                            }

                            using (DisposableList<long> blockListToSync = await ClassBlockchainStats.GetListBlockMissing(lastBlockHeightTarget, true, false, _cancellationTokenServiceSync, _peerNetworkSettingObject.PeerMaxRangeBlockToSyncPerRequest))
                            {
                                if (blockListToSync.Count > 0)
                                {
                                    ClassLog.WriteLine("Their is: " + blockListToSync.Count + " block(s) missing to sync. Current Height: " + ClassBlockchainStats.GetLastBlockHeight() + "/" + lastBlockHeightTarget, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                    using (var syncBlockResult = await StartAskBlockObjectFromListPeerTarget(peerTargetList, blockListToSync, true))
                                    {
                                        if (syncBlockResult.Count > 0)
                                        {
                                            ClassLog.WriteLine(syncBlockResult.Count + " block(s) synced. Sync now block transaction(s) of them..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);


                                            int index = 0;

                                            foreach (ClassBlockObject blockObject in syncBlockResult.GetList.Values)
                                            {
                                                if (blockObject?.BlockHeight > lastBlockHeightUnlocked)
                                                {
                                                    if (blockObject.BlockStatus == ClassBlockEnumStatus.LOCKED)
                                                        break;

                                                    if (blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight)
                                                    {
                                                        ClassBlockObject blockInformation = await ClassBlockchainStats.GetBlockInformationData(blockObject.BlockHeight - 1, _cancellationTokenServiceSync);

                                                        if (blockInformation == null || blockInformation.BlockStatus == ClassBlockEnumStatus.LOCKED)
                                                        {
                                                            ClassLog.WriteLine("The block height: " + blockObject.BlockHeight + " cannot be synced, the previous one is locked. Attempt to fix the sync..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

                                                            using (DisposableList<long> listBlockToFix = new DisposableList<long>())
                                                            {
                                                                listBlockToFix.Add(blockObject.BlockHeight - 1);
                                                                using (var listBlock = await StartAskBlockObjectFromListPeerTarget(peerTargetList, listBlockToFix, true))
                                                                {
                                                                    if (listBlock.Count > 0)
                                                                    {
                                                                        if (listBlock[blockObject.BlockHeight - 1]?.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                                                        {
                                                                            if (await ClassBlockchainDatabase.BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(listBlock[blockObject.BlockHeight - 1], true, _cancellationTokenServiceSync))
                                                                            {
                                                                                await ClassMemPoolDatabase.RemoveMemPoolAllTxFromBlockHeightTarget(listBlock[blockObject.BlockHeight - 1].BlockHeight, _cancellationTokenServiceSync);

                                                                                ClassLog.WriteLine("The block height: " + listBlock[blockObject.BlockHeight - 1].BlockHeight + " data updated successfully, continue to sync.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                                                                            }
                                                                        }

                                                                        ClassLog.WriteLine("The block height: " + blockObject.BlockHeight + " cannot be synced, the previous one is locked. Fix completed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    }

                                                    if (await ClassBlockchainDatabase.BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObject, true, _cancellationTokenServiceSync))
                                                    {
                                                        await ClassMemPoolDatabase.RemoveMemPoolAllTxFromBlockHeightTarget(blockObject.BlockHeight, _cancellationTokenServiceSync);

                                                        ClassLog.WriteLine("The block height: " + blockObject.BlockHeight + " data updated successfully, continue to sync.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                                                    }

                                                }
                                                else
                                                {
                                                    ClassLog.WriteLine("A block object target synced is empty, retry again later.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                    break;
                                                }

                                                index++;
                                            }

                                        }
                                        else
                                        {
                                            forceDisconnect = true;
                                            ClassLog.WriteLine("Can't sync " + blockListToSync.Count + " block(s), retry again later.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                        }
                                    }
                                }
                            }

                            if (lastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                            {
                                ClassBlockObject lastBlockObject = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockInformationDataStrategy(ClassBlockchainStats.GetLastBlockHeight(), _cancellationTokenServiceSync);

                                if (lastBlockObject != null)
                                    await ClassBlockchainDatabase.GenerateNewMiningBlockObject(lastBlockObject.BlockHeight, lastBlockObject.BlockHeight + 1, lastBlockObject.TimestampFound, lastBlockObject.BlockWalletAddressWinner, false, false, _cancellationTokenServiceSync);
                            }

                            #endregion

                        }

                        ClearPeerTargetList(peerTargetList, forceDisconnect);
                    }


                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);

                }

            }), 0, _cancellationTokenServiceSync).Wait();

        }

        /// <summary>
        /// Start the task who correct blocks and tx's who are wrong from other peers.
        /// </summary>
        private void StartTaskSyncCheckBlockAndTx()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {

                while (_peerSyncStatus)
                {
                    try
                    {
                        if (_peerDatabase.Count > 0 && ClassBlockchainStats.BlockCount > 0)
                        {

                            long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();
                            long lastBlockHeightUnlocked = await ClassBlockchainStats.GetLastBlockHeightUnlocked(_cancellationTokenServiceSync);

                            #region Check block's and tx's synced with other peers and increment network confirmations.

                            int totalBlockChecked = 0;

                            using (DisposableList<long> listBlockMissed = await ClassBlockchainStats.GetListBlockMissing(lastBlockHeight, false, true, _cancellationTokenServiceSync, _peerNetworkSettingObject.PeerMaxRangeBlockToSyncPerRequest))
                            {
                                if (listBlockMissed.Count == 0)
                                {
                                    using (DisposableList<long> listBlockNetworkUnconfirmed = await ClassBlockchainStats.GetListBlockNetworkUnconfirmed(_cancellationTokenServiceSync, Environment.ProcessorCount))
                                    {
                                        if (listBlockNetworkUnconfirmed.Count > 0)
                                        {
                                            ClassLog.WriteLine("Increment " + listBlockNetworkUnconfirmed.Count + " block check network confirmations..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);

                                            bool cancelCheck = false;
                                            int totalTask = listBlockNetworkUnconfirmed.Count;
                                            int totalTaskDone = 0;

                                            foreach (long blockHeightToCheck in listBlockNetworkUnconfirmed.GetAll.OrderBy(x => x))
                                            {
                                                if (cancelCheck)
                                                    break;

                                                ClassBlockObject blockObject = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockDataStrategy(blockHeightToCheck, false, true, _cancellationTokenServiceSync);

                                                long blockSize = ClassBlockUtility.GetIoBlockSizeOnMemory(blockObject);

                                                await TaskManager.TaskManager.InsertTask(async () =>
                                                {
                                                    var peerTargetList = GenerateOrUpdatePeerTargetList(null);

                                                    try
                                                    {

                                                        ClassLog.WriteLine("Start to check the block height: " + blockHeightToCheck + " with other peers..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);

                                                        ClassBlockObject blockObjectToCheck = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockDataStrategy(blockHeightToCheck, true, false, _cancellationTokenServiceSync);

                                                        if (blockObjectToCheck != null)
                                                        {
                                                            if (!blockObjectToCheck.BlockUnlockValid)
                                                            {
                                                                blockObjectToCheck.DeepCloneBlockObject(true, out ClassBlockObject blockObjectToUpdate);

                                                                ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult checkBlockResult = await StartCheckBlockDataUnlockedFromListPeerTarget(peerTargetList, blockHeightToCheck, blockObjectToUpdate);


                                                                switch (checkBlockResult)
                                                                {
                                                                    case ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.NO_CONSENSUS_FOUND:
                                                                        {
                                                                            ClassLog.WriteLine("Not enough peers to check the block height: " + blockHeightToCheck, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                                                                            cancelCheck = true;
                                                                        }
                                                                        break;
                                                                    case ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.INVALID_BLOCK:
                                                                        {
                                                                            ClassLog.WriteLine("The block height: " + blockHeightToCheck + " data seems to be invalid, ask peers to retrieve back the good data.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);

                                                                            #region Resync the block data who is invalid according to peers.

                                                                            using (DisposableList<long> blockListToCorrect = new DisposableList<long>(false, 0, new List<long>() { blockHeightToCheck }))
                                                                            {
                                                                                using (var result = await StartAskBlockObjectFromListPeerTarget(peerTargetList, blockListToCorrect, true))
                                                                                {

                                                                                    if (result.Count > 0 && result.ContainsKey(blockHeightToCheck))
                                                                                    {
                                                                                        ClassLog.WriteLine("The block height: " + blockHeightToCheck + " seems to be retrieve from peers, check it..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);

                                                                                        if (result[blockHeightToCheck]?.BlockStatus == ClassBlockEnumStatus.UNLOCKED && result[blockHeightToCheck]?.BlockHeight == blockHeightToCheck)
                                                                                        {

                                                                                            if (!await ClassBlockUtility.CheckBlockDataObject(result[blockHeightToCheck], blockObjectToUpdate.BlockHeight, true, _cancellationTokenServiceSync))
                                                                                            {
                                                                                                ClassLog.WriteLine("The block height: " + blockHeightToCheck + " is invalid. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                                                cancelCheck = true;
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                if (await ClassBlockchainDatabase.BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(result[blockHeightToCheck].DirectCloneBlockObject(), true, _cancellationTokenServiceSync))
                                                                                                    ClassLog.WriteLine("The block height: " + blockHeightToCheck + " retrieved from peers, is fixed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                                                                                                else
                                                                                                {
                                                                                                    ClassLog.WriteLine("The block height: " + blockHeightToCheck + " retrieved from peers, is fixed but cannot be inserted or updated. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                                                                    cancelCheck = true;
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            ClassLog.WriteLine("The block height: " + blockHeightToCheck + " failed. The block is not unlocked. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                                            cancelCheck = true;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                        ClassLog.WriteLine("Can't sync again transactions for the block height: " + blockHeightToCheck + " cancel the task of checking blocks.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);

                                                                                    cancelCheck = true;
                                                                                }
                                                                            }

                                                                            #endregion
                                                                        }
                                                                        break;
                                                                    case ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.VALID_BLOCK:
                                                                        {

                                                                            if (blockObjectToUpdate.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                                                            {

                                                                                ClassLog.WriteLine("The block height: " + blockHeightToCheck + " seems to be valid for other peers. Amount of confirmations: " + blockObjectToUpdate.BlockNetworkAmountConfirmations + "/" + BlockchainSetting.BlockAmountNetworkConfirmations, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                                                                                // Faster network confirmations.
                                                                                if (blockHeightToCheck + blockObjectToUpdate.BlockNetworkAmountConfirmations < lastBlockHeight)
                                                                                {
                                                                                    blockObjectToUpdate.BlockNetworkAmountConfirmations++;

                                                                                    if (blockObjectToUpdate.BlockNetworkAmountConfirmations >= BlockchainSetting.BlockAmountNetworkConfirmations)
                                                                                    {
                                                                                        ClassLog.WriteLine("The block height: " + blockHeightToCheck + " is totally valid. The node can start to confirm tx's of this block.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkCyan);
                                                                                        blockObjectToUpdate.BlockUnlockValid = true;
                                                                                    }
                                                                                    if (!await ClassBlockchainDatabase.BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObjectToUpdate, true, _cancellationTokenServiceSync))
                                                                                        ClassLog.WriteLine("The block height: " + blockHeightToCheck + " seems to be valid for other peers. But can't push updated data into the database.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                                                                                }
                                                                                // Increment slowly network confirmations.
                                                                                else
                                                                                {
                                                                                    if (blockObjectToUpdate.BlockSlowNetworkAmountConfirmations >= BlockchainSetting.BlockAmountSlowNetworkConfirmations)
                                                                                    {
                                                                                        blockObjectToUpdate.BlockNetworkAmountConfirmations++;
                                                                                        blockObjectToUpdate.BlockSlowNetworkAmountConfirmations = 0;
                                                                                    }
                                                                                    else
                                                                                        blockObjectToUpdate.BlockSlowNetworkAmountConfirmations++;


                                                                                    if (blockObjectToUpdate.BlockNetworkAmountConfirmations >= BlockchainSetting.BlockAmountNetworkConfirmations)
                                                                                    {
                                                                                        ClassLog.WriteLine("The block height: " + blockHeightToCheck + " is totally valid. The node can start to confirm tx's of this block.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkCyan);
                                                                                        blockObjectToUpdate.BlockUnlockValid = true;
                                                                                    }

                                                                                    if (!await ClassBlockchainDatabase.BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObjectToUpdate, true, _cancellationTokenServiceSync))
                                                                                        ClassLog.WriteLine("The block height: " + blockHeightToCheck + " seems to be valid for other peers. But can't push updated data into the database.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                                                                                }
                                                                            }
                                                                        }
                                                                        break;
#if DEBUG
                                                                    default:
                                                                        {
                                                                            Debug.WriteLine("Unexpected result: " + System.Enum.GetName(typeof(ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult), checkBlockResult));
                                                                            cancelCheck = true;
                                                                        }
                                                                        break;
#endif
                                                                }


                                                                if (cancelCheck)
                                                                    ClassLog.WriteLine("Increment block check network confirmations cancelled..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);

                                                                totalBlockChecked++;


#if DEBUG
                                                                Debug.WriteLine("Check the block height: " + blockHeightToCheck + " with other peers done: " + System.Enum.GetName(typeof(ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult), checkBlockResult));
#endif
                                                                ClassLog.WriteLine("Check the block height: " + blockHeightToCheck + " with other peers done: " + System.Enum.GetName(typeof(ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult), checkBlockResult), ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            ClassLog.WriteLine("Can't check the block height: " + blockHeightToCheck + ", this one can't be retrieved successfully.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                            cancelCheck = true;
                                                        }


                                                    }
                                                    catch (Exception error)
                                                    {
                                                        if (error is OperationCanceledException)
                                                            totalTaskDone++;

                                                        ClassLog.WriteLine("Error to check the block height: " + blockHeightToCheck + " | Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                        cancelCheck = true;

                                                    }

                                                    totalTaskDone++;

                                                    ClearPeerTargetList(peerTargetList, true);

                                                }, (blockSize > 0 ? blockSize : _peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000) * Environment.ProcessorCount, _cancellationTokenServiceSync);
                                            }

                                            while (totalTaskDone < totalTask)
                                                await Task.Delay(1, _cancellationTokenServiceSync.Token);

                                            ClassLog.WriteLine("Increment " + listBlockNetworkUnconfirmed.Count + "  block check network confirmations done..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);
                                        }
                                    }
                                }
                                else
                                    ClassLog.WriteLine("Increment block check network confirmations canceled. Their is " + listBlockMissed.Count + " block(s) missed to sync.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);

                            }

                            #endregion
                        }

                    }
                    catch(Exception error)
                    {
                        ClassLog.WriteLine("Error on checking block data synced. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                    }

                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }


            }), 0, _cancellationTokenServiceSync).Wait();

        }

        /// <summary>
        /// Start the task who sync the last network informations provided by other peers.
        /// </summary>
        private void StartTaskSyncNetworkInformations()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                Dictionary<int, ClassPeerTargetObject> peerTargetList = null;

                while (_peerSyncStatus)
                {
                    try
                    {
                        if (_peerDatabase.Count > 0)
                        {
                            peerTargetList = GenerateOrUpdatePeerTargetList(peerTargetList);

                            // If true, run every peer check tasks functions.
                            if (peerTargetList.Count > 0)
                            {
                                ClassLog.WriteLine("Start sync to retrieve back new network informations..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);


                                Tuple<ClassPeerPacketSendNetworkInformation, float> packetNetworkInformationTmp = await StartAskNetworkInformationFromListPeerTarget(peerTargetList);

                                if (packetNetworkInformationTmp?.Item1 != null)
                                {
                                    if (packetNetworkInformationTmp.Item2 > 0)
                                    {
                                        ClassLog.WriteLine("Current network informations received successfully.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                        _packetNetworkInformation = new ClassPeerPacketSendNetworkInformation()
                                        {
                                            CurrentBlockDifficulty = packetNetworkInformationTmp.Item1.CurrentBlockDifficulty,
                                            CurrentBlockHash = packetNetworkInformationTmp.Item1.CurrentBlockHash,
                                            TimestampBlockCreate = packetNetworkInformationTmp.Item1.TimestampBlockCreate,
                                            LastBlockHeightUnlocked = packetNetworkInformationTmp.Item1.LastBlockHeightUnlocked,
                                            PacketNumericHash = packetNetworkInformationTmp.Item1.PacketNumericHash,
                                            CurrentBlockHeight = packetNetworkInformationTmp.Item1.CurrentBlockHeight,
                                            PacketTimestamp = packetNetworkInformationTmp.Item1.PacketTimestamp,
                                            PacketNumericSignature = packetNetworkInformationTmp.Item1.PacketNumericSignature,
                                        };
                                        ClassBlockchainStats.UpdateLastNetworkBlockHeight(packetNetworkInformationTmp.Item1.CurrentBlockHeight);
                                    }
                                }
                                else
                                    ClassLog.WriteLine("Current network informations not received. Retry the sync later..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            }
                            ClearPeerTargetList(peerTargetList, false);
                        }
                    }
                    catch(Exception error)
                    {
                        ClassLog.WriteLine("Error on syncing network informations. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                    }
                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }

            }), 0, _cancellationTokenServiceSync).Wait();

        }

        #endregion

        #region Peer Task Sync - Tasks Packet functions.

        /// <summary>
        /// Run multiple async task for ask a list of peers from a peer list target.
        /// </summary>
        /// <param name="peerListTarget"></param>
        private async Task<int> StartAskPeerListFromListPeerTarget(Dictionary<int, ClassPeerTargetObject> peerListTarget)
        {
            int totalTaskComplete = 0;
            int totalResponseOk = 0;

            #region Ask peer lists to every peers target.

            foreach (int i in peerListTarget.Keys)
            {

                var i1 = i;

                await TaskManager.TaskManager.InsertTask(new Action(async () =>
                {
                    try
                    {
                        if (await SendAskPeerList(peerListTarget[i1].PeerNetworkClientSyncObject, _cancellationTokenServiceSync))
                        {
                            totalResponseOk++;
                            ClassLog.WriteLine("Peer list asked to peer target: " + peerListTarget[i1].PeerIpTarget + ":" + peerListTarget[i1].PeerPortTarget + " successfully received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                        }
                    }
                    catch
                    {
                        // The peer collection list can change or has been disposed/cleaned.
                    }

                    totalTaskComplete++;

                }), 0, _cancellationTokenServiceSync);

            }


            #endregion

            // Await the task is complete.
            while (totalTaskComplete < peerListTarget.Count)
                await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);


            ClassLog.WriteLine("Total Peers Task(s) done: " + totalResponseOk + "/" + peerListTarget.Count, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

            return totalTaskComplete;

        }

        /// <summary>
        /// Run multiple async task for ask a list of peers from a peer list target.
        /// </summary>
        /// <param name="peerListTarget"></param>
        private async Task<int> StartAskSovereignUpdateListFromListPeerTarget(Dictionary<int, ClassPeerTargetObject> peerListTarget)
        {
            using (DisposableHashset<string> hashSetSovereignUpdateHash = new DisposableHashset<string>())
            {
                int totalTaskCount = peerListTarget.Count;
                int totalTaskComplete = 0;
                int totalSovereignUpdatedReceived = 0;


                #region Sync sovereign update hash list from peers.

                foreach (int i in peerListTarget.Keys)
                {

                    await TaskManager.TaskManager.InsertTask(new Action(async () =>
                    {
                        try
                        {
                            Tuple<bool, List<string>> result = await SendAskSovereignUpdateList(peerListTarget[i].PeerNetworkClientSyncObject, _cancellationTokenServiceSync);

                            if (result != null && result?.Item2 != null && result.Item1 && result.Item2?.Count > 0)
                            {
                                foreach (string sovereignHash in result.Item2)
                                {
                                    if (!ClassSovereignUpdateDatabase.CheckIfSovereignUpdateHashExist(sovereignHash))
                                    {
                                        if (!hashSetSovereignUpdateHash.Contains(sovereignHash))
                                            hashSetSovereignUpdateHash.Add(sovereignHash);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Ignored.
                        }

                        totalTaskComplete++;
                    }), 0, null);

                }

                #endregion

                // Await the task is complete.
                while (totalTaskComplete < totalTaskCount)
                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);

                if (hashSetSovereignUpdateHash.Count > 0)
                {
                    ClassLog.WriteLine(hashSetSovereignUpdateHash.Count + " sovereign update retrieved from peers, sync updates..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);

                    totalTaskComplete = 0;
                    totalSovereignUpdatedReceived = 0;

                    foreach (int i in peerListTarget.Keys)
                    {
                        await TaskManager.TaskManager.InsertTask(new Action(async () =>
                        {

                            foreach (var sovereignUpdateHash in hashSetSovereignUpdateHash.GetList)
                            {
                                try
                                {
                                    Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>> result = await SendAskSovereignUpdateData(peerListTarget[i].PeerNetworkClientSyncObject, sovereignUpdateHash, _cancellationTokenServiceSync);

                                    if (result != null &&
                                        result?.Item2 != null &&
                                        result.Item1 &&
                                        result?.Item2.ObjectReturned != null &&
                                        result?.Item2?.ObjectReturned?.SovereignUpdateHash == sovereignUpdateHash)
                                    {
                                        if (!ClassSovereignUpdateDatabase.CheckIfSovereignUpdateHashExist(result.Item2.ObjectReturned.SovereignUpdateHash))
                                        {
                                            if (ClassSovereignUpdateDatabase.RegisterSovereignUpdateObject(result.Item2.ObjectReturned))
                                                totalSovereignUpdatedReceived++;
                                        }
                                    }
                                }
                                catch
                                {
                                    // Ignored.
                                }
                            }

                            totalTaskComplete++;
                        }), 0, _cancellationTokenServiceSync);

                    }

                    // Await the task is complete.
                    while (totalTaskComplete < totalTaskCount)
                        await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }

                return totalSovereignUpdatedReceived;
            }
        }

        /// <summary>
        /// Run multiple async task for ask the current network informations.
        /// </summary>
        /// <param name="peerListTarget"></param>
        /// <returns></returns>
        private async Task<Tuple<ClassPeerPacketSendNetworkInformation, float>> StartAskNetworkInformationFromListPeerTarget(Dictionary<int, ClassPeerTargetObject> peerListTarget)
        {
            int totalTaskToDo = peerListTarget.Count;
            int totalTaskDone = 0;
            int totalResponseOk = 0;
            using (DisposableConcurrentDictionary<string, ClassPeerPacketSendNetworkInformation> listNetworkInformationsSynced = new DisposableConcurrentDictionary<string, ClassPeerPacketSendNetworkInformation>())
            {
                using (DisposableConcurrentDictionary<string, float> listNetworkInformationsNoRankPeer = new DisposableConcurrentDictionary<string, float>())
                {
                    using (DisposableConcurrentDictionary<string, float> listNetworkInformationsRankedPeer = new DisposableConcurrentDictionary<string, float>())
                    {
                        using (DisposableConcurrentDictionary<string, int> listOfRankedPeerPublicKeySaved = new DisposableConcurrentDictionary<string, int>())
                        {

                            foreach (int i in peerListTarget.Keys)
                            {
                                var i1 = i;
                                await TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {
                                    try
                                    {
                                        Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>> result = await SendAskNetworkInformation(peerListTarget[i1].PeerNetworkClientSyncObject, _cancellationTokenServiceSync);

                                        if (result != null)
                                        {
                                            if (result.Item1 && result.Item2 != null)
                                            {
                                                bool peerRanked = false;

                                                if (_peerNetworkSettingObject.PeerEnableSovereignPeerVote)
                                                {
                                                    if (CheckIfPeerIsRanked(_peerDatabase, peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, result.Item2.ObjectReturned, result.Item2.PacketNumericHash, result.Item2.PacketNumericSignature, _cancellationTokenServiceSync, out string numericPublicKeyOut))
                                                        peerRanked = !listOfRankedPeerPublicKeySaved.ContainsKey(numericPublicKeyOut) ? listOfRankedPeerPublicKeySaved.TryAdd(numericPublicKeyOut, 0) : false;
                                                }

                                                // Ignore packet timestamp now, to not make false comparing of other important data's.
                                                if (result.Item2.ObjectReturned != null)
                                                {
                                                    if (result.Item2.ObjectReturned.LastBlockHeightUnlocked > 0 &&
                                                    result.Item2.ObjectReturned.CurrentBlockHeight > 0)
                                                    {
                                                        _peerDatabase[peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, _cancellationTokenServiceSync].PeerClientLastBlockHeight = result.Item2.ObjectReturned.LastBlockHeightUnlocked;

                                                        if (result.Item2.ObjectReturned.CurrentBlockHeight >= ClassBlockchainStats.GetLastBlockHeight() &&
                                                            result.Item2.ObjectReturned.LastBlockHeightUnlocked <= result.Item2.ObjectReturned.CurrentBlockHeight)
                                                        {

                                                            var packetData = result.Item2.ObjectReturned;

                                                            packetData.PacketTimestamp = 0;


                                                            string packetDataHash = ClassUtility.GenerateSha256FromString(ClassUtility.SerializeData(packetData));

                                                            if (!listNetworkInformationsSynced.ContainsKey(packetDataHash))
                                                                listNetworkInformationsSynced.TryAdd(packetDataHash, packetData);

                                                            if (peerRanked)
                                                            {
                                                                if (!listNetworkInformationsRankedPeer.ContainsKey(packetDataHash))
                                                                {
                                                                    if (!listNetworkInformationsRankedPeer.TryAdd(packetDataHash, 1))
                                                                        listNetworkInformationsRankedPeer[packetDataHash]++;
                                                                }
                                                                else
                                                                    listNetworkInformationsRankedPeer[packetDataHash]++;
                                                            }
                                                            else
                                                            {
                                                                if (!listNetworkInformationsNoRankPeer.ContainsKey(packetDataHash))
                                                                {
                                                                    if (!listNetworkInformationsNoRankPeer.TryAdd(packetDataHash, 1))
                                                                        listNetworkInformationsNoRankPeer[packetDataHash]++;
                                                                }
                                                                else
                                                                    listNetworkInformationsNoRankPeer[packetDataHash]++;
                                                            }

                                                            totalResponseOk++;

                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception error)
                                    {
                                        ClassLog.WriteLine("Error on asking network informations. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY, true);
                                    }

                                    totalTaskDone++;

                                }), 0, _cancellationTokenServiceSync);
                            }


                            while (totalTaskDone < totalTaskToDo)
                                await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);


                            try
                            {


                                ClassLog.WriteLine("Total task done: " + totalTaskDone + "/" + totalTaskToDo + ". Total network informations data received: " + (listNetworkInformationsNoRankPeer.Count + listNetworkInformationsRankedPeer.Count), ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

                                if (listNetworkInformationsNoRankPeer.Count > 0 || listNetworkInformationsRankedPeer.Count > 0)
                                {
                                    ClassLog.WriteLine("Their is " + listNetworkInformationsRankedPeer.Count + " packet responses received from Peer(s) ranked.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                                    ClassLog.WriteLine("Their is " + listNetworkInformationsNoRankPeer.Count + " packet responses received from Peer(s) without rank.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);


                                    string mostVotedDataHashSeed = string.Empty;
                                    string mostVotedDataHashNorm = string.Empty;

                                    float totalVote;
                                    float totalSeedVote = 0;
                                    float percentAgreeVoteSeed = 0;
                                    float totalNormVote = 0;
                                    float percentAgreeVoteNorm = 0;

                                    if (listNetworkInformationsRankedPeer.Count > 0)
                                    {
                                        totalSeedVote = listNetworkInformationsRankedPeer.GetList.Values.Sum();

                                        if (totalSeedVote > 0)
                                        {
                                            var mostVotedKeyPair = listNetworkInformationsRankedPeer.GetList.OrderByDescending(x => x.Value).First();
                                            mostVotedDataHashSeed = mostVotedKeyPair.Key;
                                            float totalVoteMostVoted = mostVotedKeyPair.Value;

                                            percentAgreeVoteSeed = (totalVoteMostVoted / totalSeedVote) * 100f;
                                        }
                                    }

                                    if (listNetworkInformationsNoRankPeer.Count > 0)
                                    {
                                        totalNormVote = listNetworkInformationsNoRankPeer.GetList.Values.Sum();

                                        if (totalNormVote > 0)
                                        {
                                            var mostVotedKeyPair = listNetworkInformationsNoRankPeer.GetList.OrderByDescending(x => x.Value).First();
                                            mostVotedDataHashNorm = mostVotedKeyPair.Key;
                                            float totalVoteMostVoted = mostVotedKeyPair.Value;

                                            percentAgreeVoteNorm = (totalVoteMostVoted / totalNormVote) * 100f;
                                        }
                                    }

                                    totalVote = totalSeedVote + totalNormVote;

                                    if ((percentAgreeVoteNorm > 0 || percentAgreeVoteSeed > 0))
                                    {
                                        if (!mostVotedDataHashSeed.IsNullOrEmpty(false, out _))
                                        {
                                            if (percentAgreeVoteSeed > percentAgreeVoteNorm)
                                            {
                                                if (listNetworkInformationsSynced.ContainsKey(mostVotedDataHashSeed))
                                                    return new Tuple<ClassPeerPacketSendNetworkInformation, float>(listNetworkInformationsSynced[mostVotedDataHashSeed], totalVote);
                                            }
                                            else if (!mostVotedDataHashNorm.IsNullOrEmpty(false, out _))
                                            {
                                                if (listNetworkInformationsSynced.ContainsKey(mostVotedDataHashNorm))
                                                    return new Tuple<ClassPeerPacketSendNetworkInformation, float>(listNetworkInformationsSynced[mostVotedDataHashNorm], totalVote);
                                            }
                                        }
                                        else if (!mostVotedDataHashNorm.IsNullOrEmpty(false, out _))
                                        {
                                            if (listNetworkInformationsSynced.ContainsKey(mostVotedDataHashNorm))
                                                return new Tuple<ClassPeerPacketSendNetworkInformation, float>(listNetworkInformationsSynced[mostVotedDataHashNorm], totalVote);
                                        }
                                    }
                                }
                            }
                            catch (Exception error)
                            {
                                ClassLog.WriteLine("Error on trying to sync network informations from peers. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#if DEBUG
                                Debug.WriteLine("Error on trying to sync network informations from peers. Exception: " + error.Message);
#endif
                            }


                            return new Tuple<ClassPeerPacketSendNetworkInformation, float>(_packetNetworkInformation, 0);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Run multiple async task to ask a list of blocks object.
        /// </summary>
        private async Task<DisposableSortedList<long, ClassBlockObject>> StartAskBlockObjectFromListPeerTarget(Dictionary<int, ClassPeerTargetObject> peerListTarget, DisposableList<long> listBlockHeightTarget, bool refuseLockedBlock)
        {
            DisposableSortedList<long, ClassBlockObject> blockListSynced = new DisposableSortedList<long, ClassBlockObject>();

            int totalTaskToDo = peerListTarget.Count;
            int totalTaskDone = 0;


            DisposableConcurrentDictionary<long, Dictionary<int, ClassBlockObject>> listBlockObjectsReceived = new DisposableConcurrentDictionary<long, Dictionary<int, ClassBlockObject>>();

            foreach (var blockHeight in listBlockHeightTarget.GetAll)
                listBlockObjectsReceived.TryAdd(blockHeight, new Dictionary<int, ClassBlockObject>());


            long blockHeightStart = listBlockHeightTarget.GetList.First();
            long blockHeightEnd = listBlockHeightTarget.GetList.Last();


            foreach (int i in peerListTarget.Keys)
            {
                int i1 = i;


                await TaskManager.TaskManager.InsertTask(new Action(async () =>
                {

                    if (blockHeightEnd - blockHeightStart == 0)
                    {
                        foreach (long blockHeight in listBlockHeightTarget.GetList)
                        {
                            if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                                continue;

                            try
                            {
                                if (blockHeight > _peerDatabase[peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, _cancellationTokenServiceSync].PeerClientLastBlockHeight)
                                {
                                    ClassLog.WriteLine("Peer not enough synced. " + peerListTarget[i1].PeerIpTarget + " | " + peerListTarget[i1].PeerUniqueIdTarget, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkYellow); ;
                                    break;
                                }

                                Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>> result = await SendAskBlockData(peerListTarget[i1].PeerNetworkClientSyncObject, blockHeight, refuseLockedBlock, _cancellationTokenServiceSync);

                                if (result == null ||
                                !result.Item1 ||
                                result.Item2 == null ||
                                result.Item2.ObjectReturned.BlockData == null ||
                                result.Item2.ObjectReturned.BlockData.BlockStatus == ClassBlockEnumStatus.LOCKED ||
                                !listBlockObjectsReceived.ContainsKey(result.Item2.ObjectReturned.BlockData.BlockHeight))
                                    break;

                                #region Ensure to reset the block data received.

                                if (result.Item2.ObjectReturned.BlockData.BlockTransactions == null ||
                                    result.Item2.ObjectReturned.BlockData.BlockTransactions.Count == 0)
                                    break;

                                result.Item2.ObjectReturned.BlockData.BlockTransactionFullyConfirmed = false;
                                result.Item2.ObjectReturned.BlockData.BlockUnlockValid = false;
                                result.Item2.ObjectReturned.BlockData.BlockNetworkAmountConfirmations = 0;
                                result.Item2.ObjectReturned.BlockData.BlockSlowNetworkAmountConfirmations = 0;
                                result.Item2.ObjectReturned.BlockData.BlockLastHeightTransactionConfirmationDone = 0;
                                result.Item2.ObjectReturned.BlockData.BlockTotalTaskTransactionConfirmationDone = 0;
                                result.Item2.ObjectReturned.BlockData.BlockTransactionConfirmationCheckTaskDone = false;


                                foreach (var transactionHash in result.Item2.ObjectReturned.BlockData.BlockTransactions.Keys)
                                {
                                    result.Item2.ObjectReturned.BlockData.BlockTransactions[transactionHash].TotalSpend = 0;
                                    result.Item2.ObjectReturned.BlockData.BlockTransactions[transactionHash].TransactionTotalConfirmation = 0;
                                    result.Item2.ObjectReturned.BlockData.BlockTransactions[transactionHash].TransactionStatus = true;
                                    result.Item2.ObjectReturned.BlockData.BlockTransactions[transactionHash].TransactionInvalidStatus = ClassTransactionEnumStatus.VALID_TRANSACTION;
                                }

                                #endregion

                                listBlockObjectsReceived[result.Item2.ObjectReturned.BlockData.BlockHeight].Add(i1, result.Item2.ObjectReturned.BlockData);
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }

                    else
                    {
                        Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>> result = await SendAskBlockDataByRange(peerListTarget[i1].PeerNetworkClientSyncObject, blockHeightStart, blockHeightEnd, refuseLockedBlock, _cancellationTokenServiceSync);

                        if (result != null && result.Item1 && result.Item2 != null)
                        {
                            foreach (ClassBlockObject blockObject in result.Item2.ObjectReturned.ListBlockObject)
                            {

                                try
                                {
                                    if (blockObject == null ||
                                        blockObject.BlockStatus == ClassBlockEnumStatus.LOCKED ||
                                        !listBlockObjectsReceived.ContainsKey(blockObject.BlockHeight) ||
                                        blockObject.BlockTransactions == null ||
                                        blockObject.BlockTransactions.Count == 0)
                                        break;

                                    #region Ensure to clean the block object received.

                                    if (listBlockObjectsReceived.ContainsKey(blockObject.BlockHeight))
                                    {
                                        blockObject.BlockTransactionFullyConfirmed = false;
                                        blockObject.BlockUnlockValid = false;
                                        blockObject.BlockNetworkAmountConfirmations = 0;
                                        blockObject.BlockSlowNetworkAmountConfirmations = 0;
                                        blockObject.BlockLastHeightTransactionConfirmationDone = 0;
                                        blockObject.BlockTotalTaskTransactionConfirmationDone = 0;
                                        blockObject.BlockTransactionConfirmationCheckTaskDone = false;

                                        foreach (var transactionHash in blockObject.BlockTransactions.Keys)
                                        {
                                            blockObject.BlockTransactions[transactionHash].TotalSpend = 0;
                                            blockObject.BlockTransactions[transactionHash].TransactionTotalConfirmation = 0;
                                            blockObject.BlockTransactions[transactionHash].TransactionStatus = true;
                                            blockObject.BlockTransactions[transactionHash].TransactionInvalidStatus = ClassTransactionEnumStatus.VALID_TRANSACTION;
                                        }


                                        listBlockObjectsReceived[blockObject.BlockHeight].Add(i1, blockObject);
                                    }
                                    #endregion
                                }
                                catch
                                {
                                    break;
                                }
                            }
                        }
                    }

                    totalTaskDone++;
                }), 0, _cancellationTokenServiceSync);
            }

            while (totalTaskDone < totalTaskToDo)
                await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);

            foreach (int blockHeight in listBlockObjectsReceived.GetList.Keys)
            {
                using (DisposableDictionary<string, ClassBlockSync> listBlockSynced = new DisposableDictionary<string, ClassBlockSync>())
                {
                    foreach (int peerId in listBlockObjectsReceived[blockHeight].Keys)
                    {
                        if (!listBlockObjectsReceived.ContainsKey(blockHeight) ||
                            !listBlockObjectsReceived[blockHeight].ContainsKey(peerId) ||
                            listBlockObjectsReceived[blockHeight][peerId] == null)
                            continue;

                        listBlockObjectsReceived[blockHeight][peerId].DeepCloneBlockObject(false, out ClassBlockObject blockObjectInformation);

                        string blockObjectHash = ClassUtility.GenerateSha256FromString(string.Concat("", ClassBlockUtility.BlockObjectToStringBlockData(blockObjectInformation, false).ToList()));

                        //bool insertStatus = false;

                        if (!listBlockSynced.GetList.ContainsKey(blockObjectHash))
                            listBlockSynced.Add(blockObjectHash, new ClassBlockSync()
                            {
                                BlockHash = blockObjectHash,
                                BlockHeight = blockHeight,
                                PeerId = peerId,
                                TotalVote = 1
                            });
                        else
                            listBlockSynced[blockObjectHash].TotalVote++;

                        #region Later

                        /*if (insertStatus)
                        {
                            bool insertVoteStatus = false;
                            if (!listBlockObjectsReceivedVotes.ContainsKey(blockObjectHash))
                            {
                                if (listBlockObjectsReceivedVotes.TryAdd(blockObjectHash, new ConcurrentDictionary<bool, float>()))
                                {
                                    // Ranked.
                                    if (listBlockObjectsReceivedVotes[blockObjectHash].TryAdd(true, 0))
                                    {
                                        if (listBlockObjectsReceivedVotes[blockObjectHash].TryAdd(false, 0))
                                            insertVoteStatus = true;
                                    }
                                }
                            }
                            else
                                insertVoteStatus = true;

                            if (insertVoteStatus)
                            {
                                if (peerRanked)
                                {
                                    if (listBlockObjectsReceivedVotes[blockObjectHash].ContainsKey(true))
                                        listBlockObjectsReceivedVotes[blockObjectHash][true]++;
                                }
                                else
                                {
                                    if (listBlockObjectsReceivedVotes[blockObjectHash].ContainsKey(false))
                                        listBlockObjectsReceivedVotes[blockObjectHash][false]++;
                                }

                                totalResponseOk++;
                            }
                        }*/

                        #endregion


                    }

                    if (listBlockSynced.Count > 0)
                    {
                        var element = listBlockSynced.GetList.Values.OrderByDescending(x => x.TotalVote)?.First();

                        if (element == null || !listBlockObjectsReceived[blockHeight].ContainsKey(element.PeerId) || listBlockObjectsReceived[blockHeight][element.PeerId] == null)
                            continue;

                        blockListSynced.Add(blockHeight, listBlockObjectsReceived[blockHeight][element.PeerId]);
                    }
                }
            }


            return blockListSynced;
        }

        /// <summary>
        /// Start to ask a transaction data by a transaction id target.
        /// </summary>
        /// <param name="peerListTarget"></param>
        /// <param name="blockHeightTarget"></param>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        private async Task<ClassTransactionObject> StartAskBlockTransactionObjectFromListPeerTarget(Dictionary<int, ClassPeerTargetObject> peerListTarget, long blockHeightTarget, int transactionId)
        {
            using (DisposableConcurrentDictionary<string, ClassTransactionObject> listTransactionObjects = new DisposableConcurrentDictionary<string, ClassTransactionObject>())
            {
                using (DisposableConcurrentDictionary<string, float> listTransactionSeedVote = new DisposableConcurrentDictionary<string, float>())
                {
                    using (DisposableConcurrentDictionary<string, float> listTransactionNormVote = new DisposableConcurrentDictionary<string, float>())
                    {
                        using (DisposableConcurrentDictionary<string, int> listOfRankedPeerPublicKeySaved = new DisposableConcurrentDictionary<string, int>())
                        {
                            int totalTaskToDo = peerListTarget.Count;
                            int totalTaskDone = 0;
                            int totalResponseOk = 0;


                            foreach (int i in peerListTarget.Keys)
                            {

                                var i1 = i;

                                await TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {


                                    try
                                    {
                                        Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>> result = await SendAskBlockTransactionData(peerListTarget[i1].PeerNetworkClientSyncObject, blockHeightTarget, transactionId, _cancellationTokenServiceSync);

                                        if (result != null)
                                        {
                                            if (result.Item1)
                                            {
                                                if (result.Item2?.ObjectReturned != null)
                                                {
                                                    if (result.Item2.ObjectReturned.BlockHeight == blockHeightTarget)
                                                    {
                                                        if (result.Item2.ObjectReturned.TransactionObject != null)
                                                        {

                                                            bool peerRanked = false;

                                                            if (_peerNetworkSettingObject.PeerEnableSovereignPeerVote)
                                                            {
                                                                if (CheckIfPeerIsRanked(_peerDatabase, peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, result.Item2.ObjectReturned, result.Item2.PacketNumericHash, result.Item2.PacketNumericSignature, _cancellationTokenServiceSync, out string numericPublicKeyOut))
                                                                    peerRanked = !listOfRankedPeerPublicKeySaved.ContainsKey(numericPublicKeyOut) ? listOfRankedPeerPublicKeySaved.TryAdd(numericPublicKeyOut, 0) : false;
                                                            }

                                                            string txHashCompare = ClassUtility.GenerateSha256FromString(ClassTransactionUtility.SplitTransactionObject(result.Item2.ObjectReturned.TransactionObject));

                                                            if (!listTransactionObjects.ContainsKey(txHashCompare))
                                                                listTransactionObjects.TryAdd(txHashCompare, result.Item2.ObjectReturned.TransactionObject);

                                                            if (peerRanked)
                                                            {
                                                                if (!listTransactionSeedVote.ContainsKey(txHashCompare))
                                                                {
                                                                    if (!listTransactionSeedVote.TryAdd(txHashCompare, 1))
                                                                        listTransactionSeedVote[txHashCompare]++;
                                                                }
                                                                else
                                                                    listTransactionSeedVote[txHashCompare]++;
                                                            }
                                                            else
                                                            {
                                                                if (!listTransactionNormVote.ContainsKey(txHashCompare))
                                                                {
                                                                    if (!listTransactionNormVote.TryAdd(txHashCompare, 1))
                                                                        listTransactionNormVote[txHashCompare]++;
                                                                }
                                                                else
                                                                    listTransactionNormVote[txHashCompare]++;
                                                            }
                                                            totalResponseOk++;


                                                        }
                                                    }
                                                }
                                            }
                                        }

                                    }
                                    catch
                                    {
                                        // Ignored, collection can are disposed before the task has been completed.
                                    }


                                    totalTaskDone++;

                                }), 0, _cancellationTokenServiceSync);
                            }


                            while (totalTaskDone < totalTaskToDo)
                                await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);


                            try
                            {

                                if (listTransactionObjects.Count > 0)
                                {
                                    string seedTxHashMaxVoted = string.Empty;
                                    string normTxHashMaxVoted = string.Empty;
                                    float seedVotePercent = 0;
                                    float normVotePercent = 0;

                                    if (listTransactionSeedVote.Count > 0)
                                    {
                                        float totalSeedVote = listTransactionSeedVote.GetList.Values.Sum();

                                        seedTxHashMaxVoted = listTransactionSeedVote.GetList.OrderByDescending(x => x.Value).First().Key;
                                        float maxVoted = listTransactionSeedVote.GetList.OrderByDescending(x => x.Value).First().Value;

                                        seedVotePercent = (maxVoted / totalSeedVote) * 100f;
                                    }

                                    if (listTransactionNormVote.Count > 0)
                                    {
                                        float totalNormVote = listTransactionNormVote.GetList.Values.Sum();

                                        normTxHashMaxVoted = listTransactionNormVote.GetList.OrderByDescending(x => x.Value).First().Key;
                                        float maxVoted = listTransactionNormVote.GetList.OrderByDescending(x => x.Value).First().Value;

                                        normVotePercent = (maxVoted / totalNormVote) * 100f;
                                    }

                                    // Proceed to votes.
                                    if (!seedTxHashMaxVoted.IsNullOrEmpty(false, out _) && !normTxHashMaxVoted.IsNullOrEmpty(false, out _))
                                    {
                                        // Perfect equality
                                        if (seedTxHashMaxVoted == normTxHashMaxVoted)
                                            return listTransactionObjects[seedTxHashMaxVoted];

                                        // Seed win.
                                        if (seedVotePercent > normVotePercent)
                                            return listTransactionObjects[seedTxHashMaxVoted];

                                        // Norm win.
                                        return listTransactionObjects[normTxHashMaxVoted];
                                    }

                                    // Seed win.
                                    if (!seedTxHashMaxVoted.IsNullOrEmpty(false, out _))
                                        return listTransactionObjects[seedTxHashMaxVoted];

                                    // Norm win.
                                    if (!normTxHashMaxVoted.IsNullOrEmpty(false, out _))
                                        return listTransactionObjects[normTxHashMaxVoted];
                                }
                            }
                            catch (Exception error)
                            {
                                ClassLog.WriteLine("Error on trying to sync a block transaction from peers. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#if DEBUG
                                Debug.WriteLine("Error on trying to sync a block transaction from peers. Exception: " + error.Message);
#endif
                            }

                            return null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Start to ask transaction data by a range of transaction id target.
        /// </summary>
        /// <param name="peerListTarget"></param>
        /// <param name="blockHeightTarget"></param>
        /// <param name="transactionIdStart"></param>
        /// <param name="transactionIdEnd"></param>
        /// <param name="listWalletAndPublicKeys"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, ClassTransactionObject>> StartAskBlockTransactionObjectByRangeFromListPeerTarget(Dictionary<int, ClassPeerTargetObject> peerListTarget, long blockHeightTarget, int transactionIdStart, int transactionIdEnd, DisposableDictionary<string, string> listWalletAndPublicKeys)
        {
            using (DisposableConcurrentDictionary<string, Dictionary<string, ClassTransactionObject>> listTransactionObjects = new DisposableConcurrentDictionary<string, Dictionary<string, ClassTransactionObject>>())
            {
                using (DisposableConcurrentDictionary<string, float> listTransactionSeedVote = new DisposableConcurrentDictionary<string, float>())
                {
                    using (DisposableConcurrentDictionary<string, float> listTransactionNormVote = new DisposableConcurrentDictionary<string, float>())
                    {
                        using (DisposableConcurrentDictionary<string, int> listOfRankedPeerPublicKeySaved = new DisposableConcurrentDictionary<string, int>())
                        {

                            int totalTaskToDo = peerListTarget.Count;
                            int totalTaskDone = 0;
                            int totalResponseOk = 0;



                            foreach (int i in peerListTarget.Keys)
                            {
                                await TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {

                                    try
                                    {
                                        Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>> result = await SendAskBlockTransactionDataByRange(peerListTarget[i].PeerNetworkClientSyncObject, blockHeightTarget, transactionIdStart, transactionIdEnd, listWalletAndPublicKeys, _cancellationTokenServiceSync);

                                        if (result != null)
                                        {
                                            if (result.Item1)
                                            {
                                                if (result.Item2?.ObjectReturned != null)
                                                {
                                                    if (result.Item2.ObjectReturned.BlockHeight == blockHeightTarget)
                                                    {
                                                        if (result.Item2.ObjectReturned.ListTransactionObject != null)
                                                        {

                                                            bool peerRanked = false;

                                                            if (_peerNetworkSettingObject.PeerEnableSovereignPeerVote)
                                                            {
                                                                if (CheckIfPeerIsRanked(_peerDatabase, peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget, result.Item2.ObjectReturned, result.Item2.PacketNumericHash, result.Item2.PacketNumericSignature, _cancellationTokenServiceSync, out string numericPublicKeyOut))
                                                                    peerRanked = !listOfRankedPeerPublicKeySaved.ContainsKey(numericPublicKeyOut) ? listOfRankedPeerPublicKeySaved.TryAdd(numericPublicKeyOut, 0) : false;
                                                            }

                                                            string listTxtData = string.Empty;

                                                            foreach (ClassTransactionObject transactionObject in result.Item2.ObjectReturned.ListTransactionObject.Values)
                                                                listTxtData += ClassTransactionUtility.SplitTransactionObject(transactionObject);

                                                            string txHashCompare = ClassUtility.GenerateSha256FromString(listTxtData);

                                                            // Clean up.
                                                            listTxtData.Clear();

                                                            if (!listTransactionObjects.ContainsKey(txHashCompare))
                                                                listTransactionObjects.TryAdd(txHashCompare, result.Item2.ObjectReturned.ListTransactionObject);

                                                            if (peerRanked)
                                                            {
                                                                if (!listTransactionSeedVote.ContainsKey(txHashCompare))
                                                                {
                                                                    if (!listTransactionSeedVote.TryAdd(txHashCompare, 1))
                                                                        listTransactionSeedVote[txHashCompare]++;
                                                                }
                                                                else
                                                                    listTransactionSeedVote[txHashCompare]++;
                                                            }
                                                            else
                                                            {
                                                                if (!listTransactionNormVote.ContainsKey(txHashCompare))
                                                                {
                                                                    if (!listTransactionNormVote.TryAdd(txHashCompare, 1))
                                                                        listTransactionNormVote[txHashCompare]++;
                                                                }
                                                                else
                                                                    listTransactionNormVote[txHashCompare]++;
                                                            }
                                                            totalResponseOk++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // Ignored, collection can are disposed before the task has been completed.
                                    }

                                    totalTaskDone++;

                                }), 0, _cancellationTokenServiceSync);

                            }

                            while (totalTaskDone < totalTaskToDo)
                                await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);

                            try
                            {

                                if (listTransactionObjects.Count > 0)
                                {
                                    string seedTxHashMaxVoted = string.Empty;
                                    string normTxHashMaxVoted = string.Empty;
                                    float seedVotePercent = 0;
                                    float normVotePercent = 0;

                                    if (listTransactionSeedVote.Count > 0)
                                    {
                                        float totalSeedVote = listTransactionSeedVote.GetList.Values.Sum();

                                        seedTxHashMaxVoted = listTransactionSeedVote.GetList.OrderByDescending(x => x.Value).First().Key;
                                        float maxVoted = listTransactionSeedVote.GetList.OrderByDescending(x => x.Value).First().Value;

                                        seedVotePercent = (maxVoted / totalSeedVote) * 100f;
                                    }

                                    if (listTransactionNormVote.Count > 0)
                                    {
                                        float totalNormVote = listTransactionNormVote.GetList.Values.Sum();

                                        normTxHashMaxVoted = listTransactionNormVote.GetList.OrderByDescending(x => x.Value).First().Key;
                                        float maxVoted = listTransactionNormVote.GetList.OrderByDescending(x => x.Value).First().Value;

                                        normVotePercent = (maxVoted / totalNormVote) * 100f;
                                    }

                                    // Proceed to votes.
                                    if (!seedTxHashMaxVoted.IsNullOrEmpty(false, out _) && !normTxHashMaxVoted.IsNullOrEmpty(false, out _))
                                    {
                                        // Perfect equality
                                        if (seedTxHashMaxVoted == normTxHashMaxVoted)
                                            return listTransactionObjects[seedTxHashMaxVoted];

                                        // Seed win.
                                        if (seedVotePercent > normVotePercent)
                                            return listTransactionObjects[seedTxHashMaxVoted];

                                        // Norm win.
                                        return listTransactionObjects[normTxHashMaxVoted];
                                    }

                                    // Seed win.
                                    if (!seedTxHashMaxVoted.IsNullOrEmpty(false, out _))
                                        return listTransactionObjects[seedTxHashMaxVoted];

                                    // Norm win.
                                    if (!normTxHashMaxVoted.IsNullOrEmpty(false, out _))
                                        return listTransactionObjects[normTxHashMaxVoted];
                                }
                            }
                            catch (Exception error)
                            {
                                ClassLog.WriteLine("Error on trying to sync a block transaction from peers. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#if DEBUG
                                Debug.WriteLine("Error on trying to sync a block transaction from peers. Exception: " + error.Message);
#endif
                            }

                            return null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ask peers a block data target, compare with it and return the result.
        /// </summary>
        /// <param name="peerListTarget"></param>
        /// <param name="blockHeightTarget"></param>
        /// <param name="blockObject"></param>
        /// <returns></returns>
        private async Task<ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult> StartCheckBlockDataUnlockedFromListPeerTarget(Dictionary<int, ClassPeerTargetObject> peerListTarget, long blockHeightTarget, ClassBlockObject blockObject)
        {
            if (blockObject == null)
                return ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.INVALID_BLOCK;

            using (DisposableConcurrentDictionary<string, int> listOfRankedPeerPublicKeySaved = new DisposableConcurrentDictionary<string, int>())
            {
                using (DisposableDictionary<bool, float> listCheckBlockDataSeedVote = new DisposableDictionary<bool, float>(0, new Dictionary<bool, float>() { { false, 0 }, { true, 0 } }))
                {
                    using (DisposableDictionary<bool, float> listCheckBlockDataNormVote = new DisposableDictionary<bool, float>(0, new Dictionary<bool, float>() { { false, 0 }, { true, 0 } }))
                    {
                        int totalTaskToDo = peerListTarget.Count;
                        int totalTaskDone = 0;
                        int totalResponseOk = 0;



                        foreach (int i in peerListTarget.Keys)
                        {
                            await TaskManager.TaskManager.InsertTask(new Action(async () =>
                            {
                                var i1 = i;

                                try
                                {
                                    Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>> result = await SendAskBlockData(peerListTarget[i1].PeerNetworkClientSyncObject, blockHeightTarget, true, _cancellationTokenServiceSync);

                                    if (result != null)
                                    {
                                        if (result.Item1)
                                        {
                                            if (result.Item2?.ObjectReturned?.BlockData != null && blockObject != null)
                                            {

                                                ClassBlockObject blockDataReceived = result.Item2.ObjectReturned.BlockData;

                                                lock (blockDataReceived)
                                                {
                                                    if (blockDataReceived.BlockHeight != blockHeightTarget)
                                                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenServiceSync);
                                                    else
                                                    {
                                                        bool peerRanked = false;
                                                        if (_peerNetworkSettingObject.PeerEnableSovereignPeerVote)
                                                        {
                                                            if (CheckIfPeerIsRanked(_peerDatabase, peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, result.Item2.ObjectReturned, result.Item2.PacketNumericHash, result.Item2.PacketNumericSignature, _cancellationTokenServiceSync, out string numericPublicKeyOut))
                                                                peerRanked = !listOfRankedPeerPublicKeySaved.ContainsKey(numericPublicKeyOut) ? listOfRankedPeerPublicKeySaved.TryAdd(numericPublicKeyOut, 0) : false;
                                                        }

                                                        bool comparedShares = false;

                                                        if (blockObject.BlockHeight == BlockchainSetting.GenesisBlockHeight)
                                                        {
                                                            if (blockDataReceived?.BlockMiningPowShareUnlockObject == null && blockObject.BlockMiningPowShareUnlockObject == null)
                                                                comparedShares = true;
                                                        }
                                                        else
                                                            comparedShares = ClassMiningPoWaCUtility.ComparePoWaCShare(blockDataReceived.BlockMiningPowShareUnlockObject, blockObject.BlockMiningPowShareUnlockObject);

                                                        if (!comparedShares)
                                                        {
                                                            if (blockDataReceived.BlockStatus == ClassBlockEnumStatus.LOCKED && blockObject.BlockStatus == ClassBlockEnumStatus.LOCKED
                                                                && blockDataReceived.BlockMiningPowShareUnlockObject == null && blockObject.BlockMiningPowShareUnlockObject == null)
                                                                comparedShares = true;
                                                        }

                                                        bool isEqual = false;


                                                        if (blockDataReceived.BlockHeight == blockObject.BlockHeight &&
                                                            blockDataReceived.BlockHash == blockObject.BlockHash &&
                                                            /*blockDataReceived.TimestampFound == blockObject.TimestampFound &&
                                                            blockDataReceived.TimestampCreate == blockObject.TimestampCreate &&*/
                                                            blockDataReceived.BlockStatus == blockObject.BlockStatus &&
                                                            /*blockDataReceived.BlockDifficulty == blockObject.BlockDifficulty &&*/
                                                            blockDataReceived.BlockFinalHashTransaction == blockObject.BlockFinalHashTransaction &&
                                                            comparedShares &&
                                                            blockDataReceived.BlockWalletAddressWinner == blockObject.BlockWalletAddressWinner)
                                                        {
                                                            if (blockDataReceived.BlockTransactions.Count == blockObject.BlockTransactions.Count)
                                                            {

                                                                #region Clean up.

                                                                blockDataReceived.BlockTransactionFullyConfirmed = false;
                                                                blockDataReceived.BlockUnlockValid = false;
                                                                blockDataReceived.BlockNetworkAmountConfirmations = 0;
                                                                blockDataReceived.BlockSlowNetworkAmountConfirmations = 0;
                                                                blockDataReceived.BlockLastHeightTransactionConfirmationDone = 0;
                                                                blockDataReceived.BlockTotalTaskTransactionConfirmationDone = 0;
                                                                blockDataReceived.BlockTransactionConfirmationCheckTaskDone = false;

                                                                foreach (var transactionHash in blockObject.BlockTransactions.Keys)
                                                                {
                                                                    blockDataReceived.BlockTransactions[transactionHash].TotalSpend = 0;
                                                                    blockDataReceived.BlockTransactions[transactionHash].TransactionTotalConfirmation = 0;
                                                                    blockDataReceived.BlockTransactions[transactionHash].TransactionStatus = true;
                                                                    blockDataReceived.BlockTransactions[transactionHash].TransactionInvalidStatus = ClassTransactionEnumStatus.VALID_TRANSACTION;
                                                                }

                                                                #endregion


                                                                bool success = true;

                                                                foreach (string transactionHash in blockObject.BlockTransactions.Keys.ToArray())
                                                                {
                                                                    if (blockObject == null || blockDataReceived == null)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }

                                                                    if (blockObject.BlockTransactions[transactionHash] == null)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }

                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.Amount != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.Amount)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.BlockHash != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.BlockHash)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.BlockHeightTransaction != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.BlockHeightTransaction)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.BlockHeightTransactionConfirmationTarget != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.BlockHeightTransactionConfirmationTarget)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.Fee != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.Fee)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.PaymentId != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.PaymentId)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.TimestampBlockHeightCreateSend != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.TimestampBlockHeightCreateSend)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.TimestampSend != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.TimestampSend)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionBigSignatureReceiver != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.TransactionBigSignatureReceiver)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionBigSignatureSender != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.TransactionBigSignatureSender)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionHash != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.TransactionHash)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionHashBlockReward != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.TransactionHashBlockReward)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionSignatureReceiver != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.TransactionSignatureReceiver)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionSignatureSender != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.TransactionSignatureSender)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionType != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.TransactionType)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.TransactionVersion != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.TransactionVersion)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressReceiver != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.WalletAddressReceiver)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.WalletAddressSender != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.WalletAddressSender)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.WalletPublicKeyReceiver != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.WalletPublicKeyReceiver)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }
                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.WalletPublicKeySender != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.WalletPublicKeySender)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }

                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.AmountTransactionSource == null &&
                                                                         blockDataReceived.BlockTransactions[transactionHash].TransactionObject.AmountTransactionSource == null)
                                                                        continue;

                                                                    if (blockObject.BlockTransactions[transactionHash].TransactionObject.AmountTransactionSource.Count != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.AmountTransactionSource.Count)
                                                                    {
                                                                        success = false;
                                                                        break;
                                                                    }

                                                                    foreach (string amountKey in blockObject.BlockTransactions[transactionHash].TransactionObject.AmountTransactionSource.Keys)
                                                                    {
                                                                        if (blockObject.BlockTransactions[transactionHash].TransactionObject.AmountTransactionSource[amountKey].Amount != blockDataReceived.BlockTransactions[transactionHash].TransactionObject.AmountTransactionSource[amountKey].Amount)
                                                                        {
                                                                            success = false;
                                                                            break;
                                                                        }
                                                                    }

                                                                    if (!success)
                                                                        break;
                                                                }


                                                                if (success)
                                                                    isEqual = true;
                                                            }
                                                        }
#if DEBUG
                                                        else
                                                        {
                                                            Debug.WriteLine("Block height: " + blockObject.BlockHeight + " is invalid for peer: " + peerListTarget[i1].PeerIpTarget);
                                                            Debug.WriteLine("External: Height: " + blockDataReceived.BlockHeight +
                                                                            Environment.NewLine + "Hash: " + blockDataReceived.BlockHash +
                                                                            Environment.NewLine + "Timestamp create: " + blockDataReceived.TimestampCreate +
                                                                            Environment.NewLine + "Timestamp found: " + blockDataReceived.TimestampFound +
                                                                            Environment.NewLine + "Block status: " + blockDataReceived.BlockStatus +
                                                                            Environment.NewLine + "Block Difficulty: " + blockDataReceived.BlockDifficulty +
                                                                            Environment.NewLine + "Block final transaction hash: " + blockDataReceived.BlockFinalHashTransaction +
                                                                            Environment.NewLine + "Block Mining pow share: " + ClassUtility.SerializeData(blockDataReceived.BlockMiningPowShareUnlockObject) +
                                                                            Environment.NewLine + "Block wallet address winner: " + blockDataReceived.BlockWalletAddressWinner +
                                                                            Environment.NewLine + "Block Transaction Count: " + blockDataReceived.BlockTransactions.Count);

                                                            Debug.WriteLine("Internal: Height: " + blockObject.BlockHeight +
                                                                            Environment.NewLine + "Hash: " + blockObject.BlockHash +
                                                                            Environment.NewLine + "Timestamp create: " + blockObject.TimestampCreate +
                                                                            Environment.NewLine + "Timestamp found: " + blockObject.TimestampFound +
                                                                            Environment.NewLine + "Block status: " + blockObject.BlockStatus +
                                                                            Environment.NewLine + "Block Difficulty: " + blockObject.BlockDifficulty +
                                                                            Environment.NewLine + "Block final transaction hash: " + blockObject.BlockFinalHashTransaction +
                                                                            Environment.NewLine + "Block Mining pow share: " + ClassUtility.SerializeData(blockObject.BlockMiningPowShareUnlockObject) +
                                                                            Environment.NewLine + "Block wallet address winner: " + blockObject.BlockWalletAddressWinner +
                                                                            Environment.NewLine + "Block Transaction Count: " + blockObject.BlockTransactions.Count);
                                                        }
#endif


                                                        totalResponseOk++;


                                                        if (peerRanked)
                                                        {
                                                            if (isEqual)
                                                                listCheckBlockDataSeedVote[true]++;
                                                            else
                                                                listCheckBlockDataSeedVote[false]++;
                                                        }
                                                        else
                                                        {
                                                            if (isEqual)
                                                                listCheckBlockDataNormVote[true]++;
                                                            else
                                                                listCheckBlockDataNormVote[false]++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception error)
                                {
                                    ClassLog.WriteLine("Failed to sync block data: " + blockHeightTarget + " from peer. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                }

                                totalTaskDone++;

                            }), 0, _cancellationTokenServiceSync);
                        }

                        while (totalTaskDone < totalTaskToDo)
                            await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);

                        if (totalResponseOk >= _peerNetworkSettingObject.PeerMinAvailablePeerSync)
                        {
                            if (listCheckBlockDataSeedVote.Count > 0 || listCheckBlockDataNormVote.Count > 0)
                            {
                                float totalSeedVote = listCheckBlockDataSeedVote[true] + listCheckBlockDataSeedVote[false];
                                float totalNormVote = listCheckBlockDataNormVote[true] + listCheckBlockDataNormVote[false];
                                float totalNormVoteAgree = listCheckBlockDataNormVote[true];
                                float totalNormVoteDenied = listCheckBlockDataNormVote[false];

                                bool seedResult = false;
                                float percentSeedAgree = 0;
                                float percentSeedDenied = 0;

                                bool normResult = false;
                                float percentNormAgree = 0;
                                float percentNormDenied = 0;


                                if (totalSeedVote > 0)
                                {
                                    if (listCheckBlockDataSeedVote[true] > 0)
                                        percentSeedAgree = (listCheckBlockDataSeedVote[true] / totalSeedVote) * 100f;

                                    if (listCheckBlockDataSeedVote[false] > 0)
                                        percentSeedDenied = (listCheckBlockDataSeedVote[false] / totalSeedVote) * 100f;

                                    seedResult = percentSeedAgree > percentSeedDenied;
                                }

                                if (totalNormVote > 0)
                                {
                                    if (listCheckBlockDataNormVote[true] > 0)
                                        percentNormAgree = (listCheckBlockDataNormVote[true] / totalNormVote) * 100f;

                                    if (listCheckBlockDataNormVote[false] > 0)
                                        percentNormDenied = (listCheckBlockDataNormVote[false] / totalNormVote) * 100f;

                                    normResult = percentNormAgree > percentNormDenied;
                                }


                                switch (seedResult)
                                {
                                    case true:
                                        {
                                            if (!normResult)
                                            {
                                                if (percentNormDenied > percentSeedAgree)
                                                    return ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.INVALID_BLOCK;
                                            }
                                            return ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.VALID_BLOCK;
                                        }
                                    case false:
                                        {
                                            if (normResult)
                                            {
                                                if (percentNormAgree > percentSeedDenied)
                                                    return ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.VALID_BLOCK;
                                            }
#if DEBUG
                                            Debug.WriteLine("Agree: " + percentNormAgree + "% | Denied: " + percentNormDenied + "% | Norm result: " + normResult + " | Total agree: " + totalNormVoteAgree + " | Total denied: " + totalNormVoteDenied);
#endif
                                            return ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.INVALID_BLOCK;
                                        }
                                }

                            }
                        }

                    }
                }
            }

            return ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.NO_CONSENSUS_FOUND;
        }

        #endregion

        #region Peer Task Sync - Packet request functions.

        /// <summary>
        /// Send auth keys peers to a peer target.
        /// </summary>
        /// <param name="peerNetworkClientSyncObject"></param>
        /// <param name="cancellation"></param>
        /// <param name="forceUpdate"></param>
        /// <returns></returns>
        private async Task<bool> SendAskAuthPeerKeys(ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject, CancellationTokenSource cancellation, bool forceUpdate)
        {
            try
            {
                if (peerNetworkClientSyncObject == null)
                    return false;

                #region Initialize peer target informations.

                string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
                int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
                string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

                bool targetExist = false;
                if (!_peerDatabase.ContainsPeerIp(peerIp, cancellation))
                    _peerDatabase.TryAddPeer(peerIp, peerPort, peerUniqueId, cancellation);
                else
                {
                    if (_peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
                        targetExist = true;
                }

                ClassPeerObject peerObject = null;

                if (targetExist)
                {
                    if (await ClassPeerKeysManager.UpdatePeerInternalKeys(_peerDatabase, peerIp, peerPort, peerUniqueId, cancellation, _peerNetworkSettingObject, forceUpdate))
                        peerObject = _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync];
                }
                else
                {
                    peerObject = ClassPeerKeysManager.GeneratePeerObject(peerIp, peerPort, peerUniqueId, cancellation);
                    peerObject.PeerLastPacketReceivedTimestamp = TaskManager.TaskManager.CurrentTimestampSecond + _peerNetworkSettingObject.PeerMaxDelayKeepAliveStats;
                    if (!_peerDatabase[peerIp, cancellation].TryAdd(peerUniqueId, peerObject))
                    {
                        if (_peerDatabase.UpdatePeer(peerIp, peerUniqueId, peerObject, cancellation))
                        {
                            await ClassPeerKeysManager.UpdatePeerInternalKeys(_peerDatabase, peerIp, peerPort, peerUniqueId, cancellation, _peerNetworkSettingObject, forceUpdate);
                            targetExist = true;
                        }
                    }
                }

                if (peerObject == null)
                {
                    peerObject = _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync];

                    if (peerObject == null)
                        return false;

                    if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(_peerDatabase, peerIp, peerUniqueId, _cancellationTokenServiceSync))
                        await ClassPeerKeysManager.UpdatePeerInternalKeys(_peerDatabase, peerIp, peerPort, peerUniqueId, cancellation, _peerNetworkSettingObject, forceUpdate);
                }

                #endregion

                #region Initialize the packet to send.

                ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, string.Empty, 0)
                {
                    PacketOrder = ClassPeerEnumPacketSend.ASK_PEER_AUTH_KEYS,
                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskPeerAuthKeys()
                    {
                        AesEncryptionKey = peerObject.PeerInternPacketEncryptionKey,
                        AesEncryptionIv = peerObject.PeerInternPacketEncryptionKeyIv,
                        PublicKey = peerObject.PeerInternPublicKey,
                        NumericPublicKey = _peerNetworkSettingObject.PeerNumericPublicKey,
                        PeerPort = _peerNetworkSettingObject.ListenPort,
                        PeerIsPublic = _peerNetworkSettingObject.PublicPeer,
                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                    }),
                };
                sendObject.PacketHash = ClassUtility.GenerateSha256FromString(sendObject.PacketContent + sendObject.PacketOrder);
                sendObject.PacketSignature = ClassWalletUtility.WalletGenerateSignature(peerObject.PeerInternPrivateKey, sendObject.PacketHash);

                #endregion

                var packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject, false, peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_PEER_AUTH_KEYS, false, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " try to retrieve auth keys failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return false;
                }


                #region Handle packet received.

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " auth keys failed, the packet is empty.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return false;
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_PEER_AUTH_KEYS)
                {
                    try
                    {

                        if (!TryGetPacketPeerAuthKeys(peerNetworkClientSyncObject, _peerNetworkSettingObject, out ClassPeerPacketSendPeerAuthKeys peerPacketSendPeerAuthKeys))
                        {
                            ClassLog.WriteLine(peerIp + ":" + peerPort + " can't handle peer auth keys from the packet received. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                            if (targetExist)
                                ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenServiceSync);
                            return false;
                        }

                        peerUniqueId = peerNetworkClientSyncObject.PeerPacketReceived.PacketPeerUniqueId;

                        targetExist = _peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation);

                        if (!targetExist)
                        {
                            peerObject.PeerUniqueId = peerUniqueId;

                            if (!_peerDatabase.ContainsPeerIp(peerIp, cancellation))
                            {
                                if (_peerDatabase.TryAddPeer(peerIp, peerPort, peerUniqueId, cancellation))
                                {
                                    if (_peerDatabase.UpdatePeer(peerIp, peerUniqueId, peerObject, cancellation))
                                        targetExist = true;
                                }
                            }
                        }

                        if (await ClassPeerKeysManager.UpdatePeerKeysReceiveTaskSync(_peerDatabase, peerIp, peerNetworkClientSyncObject.PeerPacketReceived.PacketPeerUniqueId, peerPacketSendPeerAuthKeys, cancellation, _peerNetworkSettingObject))
                        {

                            ClassPeerCheckManager.CleanPeerState(_peerDatabase, peerIp, peerUniqueId, true, cancellation);
                            _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerIsPublic = true;
                            _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerStatus = ClassPeerEnumStatus.PEER_ALIVE;

                            ClassLog.WriteLine(peerIp + ":" + peerPort + " send propertly auth keys.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, cancellation);
                            return true;
                        }
                        else
                        {
                            ClassPeerCheckManager.SetPeerDeadState(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                            ClassLog.WriteLine(peerIp + ":" + peerPort + " failed to update auth keys.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            return false;
                        }
                    }
                    catch (Exception error)
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " exception from packet received: " + error.Message + ", on receiving auth keys, increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                        if (targetExist)
                        {
                            ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        }
                        return false;
                    }

                }

                #endregion

                ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            }
            catch(Exception error)
            {
                ClassLog.WriteLine("Can't ask auth keys from an error. Exception:  " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            }
            return false;
        }

        /// <summary>
        /// Send a request to ask a peer list from a peer.
        /// </summary>
        /// <param name="peerNetworkClientSyncObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> SendAskPeerList(ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject, CancellationTokenSource cancellation)
        {
            if (peerNetworkClientSyncObject == null)
                return false;

            ClassPeerObject peerObject = _peerDatabase[peerNetworkClientSyncObject.PeerIpTarget, peerNetworkClientSyncObject.PeerUniqueIdTarget, cancellation];

            if (peerObject == null)
                return false;

            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerInternPublicKey, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_PEER_LIST,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskPeerList()
                {
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };
            sendObject.PacketHash = ClassUtility.GenerateSha256FromString(sendObject.PacketContent + sendObject.PacketOrder);
            sendObject.PacketSignature = ClassWalletUtility.WalletGenerateSignature(peerObject.PeerInternPrivateKey, sendObject.PacketHash);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject, true, peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_PEER_LIST, true, false);

                if (!packetSendStatus || peerNetworkClientSyncObject.PeerPacketReceived == null)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " retrieve peer list failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return false;
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder != ClassPeerEnumPacketResponse.SEND_PEER_LIST)
                {
                    ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                    return await HandleUnexpectedPacketOrder(peerIp, peerPort, peerUniqueId, peerNetworkClientSyncObject.PeerPacketReceived, cancellation);
                }

                if (!TryGetPacketPeerList(_peerDatabase, peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, cancellation, out ClassPeerPacketSendPeerList packetPeerList))
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " can't handle peer packet received. Increment invalid packets", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                    return false;
                }

                for (int i = 0; i < packetPeerList.PeerIpList.Count; i++)
                {
                    if (i > packetPeerList.PeerIpList.Count)
                        break;

                    if (packetPeerList.PeerIpList[i] == _peerNetworkSettingObject.ListenIp ||
                        packetPeerList.PeerIpList[i] == peerIp ||
                        packetPeerList.PeerIpList[i] == PeerOpenNatServerIp)
                        continue;

                    if (packetPeerList.PeerIpList[i].IsNullOrEmpty(true, out _) || !IPAddress.TryParse(packetPeerList.PeerIpList[i], out _))
                    {
                        ClassLog.WriteLine("Can't register peer: " + packetPeerList.PeerIpList[i] + ":" + packetPeerList.PeerPortList[i] + " because the IP Address is not valid.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        continue;
                    }

                    if (packetPeerList.PeerPortList[i] < _peerNetworkSettingObject.PeerMinPort || packetPeerList.PeerPortList[i] > _peerNetworkSettingObject.PeerMaxPort)
                    {
                        ClassLog.WriteLine("Can't register peer: " + packetPeerList.PeerIpList[i] + ":" + packetPeerList.PeerPortList[i] + " because the P2P port is not valid.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        continue;
                    }

                    if (packetPeerList.PeerUniqueIdList[i].IsNullOrEmpty(false, out _) ||
                        !ClassUtility.CheckHexStringFormat(packetPeerList.PeerUniqueIdList[i]) ||
                        packetPeerList.PeerUniqueIdList[i].Length != BlockchainSetting.PeerUniqueIdHashLength)
                    {
                        ClassLog.WriteLine("Can't register peer: " + packetPeerList.PeerIpList[i] + " because the unique id: \n" + packetPeerList.PeerUniqueIdList[i] + " is not valid.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        continue;
                    }


                    if (ClassPeerCheckManager.CheckPeerClientStatus(_peerDatabase, packetPeerList.PeerIpList[i], packetPeerList.PeerUniqueIdList[i], false, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation))
                        continue;

                    if (await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(_peerDatabase, packetPeerList.PeerIpList[i], packetPeerList.PeerPortList[i], packetPeerList.PeerUniqueIdList[i], _peerNetworkSettingObject, _peerFirewallSettingObject), CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token), true))
                        ClassLog.WriteLine("New Peer: " + packetPeerList.PeerIpList[i] + ":" + packetPeerList.PeerPortList[i] + " successfully registered.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

                }

                return true;
            }

            ClassLog.WriteLine("Packet build to send is empty and cannot be sent.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            return false;

        }

        /// <summary>
        /// Send a request to ask a sovereign update list.
        /// </summary>
        /// <param name="peerNetworkClientSyncObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, List<string>>> SendAskSovereignUpdateList(ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject, CancellationTokenSource cancellation)
        {
            ClassPeerObject peerObject = _peerDatabase[peerNetworkClientSyncObject.PeerIpTarget, peerNetworkClientSyncObject.PeerUniqueIdTarget, cancellation];

            if (peerObject == null)
                return new Tuple<bool, List<string>>(false, null);

            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerInternPublicKey, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_LIST_SOVEREIGN_UPDATE,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskListSovereignUpdate()
                {
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };
            sendObject.PacketHash = ClassUtility.GenerateSha256FromString(sendObject.PacketContent + sendObject.PacketOrder);
            sendObject.PacketSignature = ClassWalletUtility.WalletGenerateSignature(peerObject.PeerInternPrivateKey, sendObject.PacketHash);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject, true, peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_LIST_SOVEREIGN_UPDATE, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " retrieve sovereign update list failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);

                    return new Tuple<bool, List<string>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, List<string>>(true, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, List<string>>(false, null);

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_LIST_SOVEREIGN_UPDATE)
                {
                    if (!TryGetPacketSovereignUpdateList(_peerDatabase, peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, cancellation, out ClassPeerPacketSendListSovereignUpdate packetPeerSovereignUpdateList))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " invalid sovereign update list packet received. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        return new Tuple<bool, List<string>>(false, null);
                    }

                    ClassLog.WriteLine(peerIp + ":" + peerPort + " packet return " + packetPeerSovereignUpdateList.SovereignUpdateHashList.Count + " sovereign update hash.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                    return new Tuple<bool, List<string>>(true, packetPeerSovereignUpdateList.SovereignUpdateHashList);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enougth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, List<string>>(false, null);
                }

                ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                return new Tuple<bool, List<string>>(await HandleUnexpectedPacketOrder(peerIp, peerPort, peerUniqueId, peerNetworkClientSyncObject.PeerPacketReceived, cancellation), null);
            }

            ClassLog.WriteLine("Packet build to send is empty and cannot be sent.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            return new Tuple<bool, List<string>>(false, null);
        }

        /// <summary>
        /// Send a request to ask a sovereign data from hash.
        /// </summary>
        /// <param name="peerNetworkClientSyncObject"></param>
        /// <param name="sovereignHash"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>> SendAskSovereignUpdateData(ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject, string sovereignHash, CancellationTokenSource cancellation)
        {
            ClassPeerObject peerObject = _peerDatabase[peerNetworkClientSyncObject.PeerIpTarget, peerNetworkClientSyncObject.PeerUniqueIdTarget, cancellation];

            if (peerObject == null)
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(false, null);

            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerInternPublicKey, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_SOVEREIGN_UPDATE_FROM_HASH,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskSovereignUpdateFromHash()
                {
                    SovereignUpdateHash = sovereignHash,
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };
            sendObject.PacketHash = ClassUtility.GenerateSha256FromString(sendObject.PacketContent + sendObject.PacketOrder);
            sendObject.PacketSignature = ClassWalletUtility.WalletGenerateSignature(peerObject.PeerInternPrivateKey, sendObject.PacketHash);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject, true, peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_SOVEREIGN_UPDATE_FROM_HASH, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask sovereign update data failed. Hash: " + sovereignHash, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(true, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(false, null);

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_SOVEREIGN_UPDATE_FROM_HASH)
                {

                    if (!TryGetPacketSovereignUpdateData(_peerDatabase, peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, cancellation, out ClassPeerPacketSendSovereignUpdateFromHash packetSovereignUpdateData))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " a packet sovereign update data received is invalid. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, cancellation);

                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(true, new ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>()
                    {
                        ObjectReturned = packetSovereignUpdateData.SovereignUpdateObject,
                        PacketNumericHash = packetSovereignUpdateData.PacketNumericHash,
                        PacketNumericSignature = packetSovereignUpdateData.PacketNumericSignature
                    });
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enoguth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(false, null);
                }

                ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(await HandleUnexpectedPacketOrder(peerIp, peerPort, peerUniqueId, peerNetworkClientSyncObject.PeerPacketReceived, cancellation), null);

            }

            ClassLog.WriteLine("Packet build to send is empty and cannot be sent.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(false, null);

        }

        /// <summary>
        /// Send a request to ask the current network informations.
        /// </summary>
        /// <param name="peerNetworkClientSyncObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>> SendAskNetworkInformation(ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject, CancellationTokenSource cancellation)
        {
            ClassPeerObject peerObject = _peerDatabase[peerNetworkClientSyncObject.PeerIpTarget, peerNetworkClientSyncObject.PeerUniqueIdTarget, cancellation];

            if (peerObject == null)
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);

            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;


            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerInternPublicKey, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_NETWORK_INFORMATION,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskPeerList()
                {
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };
            sendObject.PacketHash = ClassUtility.GenerateSha256FromString(sendObject.PacketContent + sendObject.PacketOrder);
            sendObject.PacketSignature = ClassWalletUtility.WalletGenerateSignature(peerObject.PeerInternPrivateKey, sendObject.PacketHash);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject, true, peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_NETWORK_INFORMATION, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask network information failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);

                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS)

                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(true, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_NETWORK_INFORMATION)
                {

                    if (!TryGetPacketNetworkInformation(_peerDatabase, peerNetworkClientSyncObject, peerIp, peerPort, _peerNetworkSettingObject, cancellation, out ClassPeerPacketSendNetworkInformation peerPacketNetworkInformation))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + "  can't retrieve packet network information from packet received. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, cancellation);

                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(true, new ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>()
                    {
                        ObjectReturned = new ClassPeerPacketSendNetworkInformation()
                        {
                            CurrentBlockDifficulty = peerPacketNetworkInformation.CurrentBlockDifficulty,
                            CurrentBlockHash = peerPacketNetworkInformation.CurrentBlockHash,
                            CurrentBlockHeight = peerPacketNetworkInformation.CurrentBlockHeight,
                            LastBlockHeightUnlocked = peerPacketNetworkInformation.LastBlockHeightUnlocked,
                            PacketTimestamp = peerPacketNetworkInformation.PacketTimestamp,
                            TimestampBlockCreate = peerPacketNetworkInformation.TimestampBlockCreate
                        },
                        PacketNumericHash = peerPacketNetworkInformation.PacketNumericHash,
                        PacketNumericSignature = peerPacketNetworkInformation.PacketNumericSignature
                    });

                }


                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enoguth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);
                }


                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);

                ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(await HandleUnexpectedPacketOrder(peerIp, peerPort, peerUniqueId, peerNetworkClientSyncObject.PeerPacketReceived, cancellation), null);
            }

            ClassLog.WriteLine("Packet build to send is empty and cannot be sent.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);
        }

        /// <summary>
        /// Send a request to ask a block data target by range.
        /// </summary>
        /// <param name="peerNetworkClientSyncObject"></param>
        /// <param name="blockHeightTargetStart"></param>
        /// <param name="blockHeightTargetEnd"></param>
        /// <param name="refuseLockedBlock"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>>> SendAskBlockDataByRange(ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject, long blockHeightTargetStart, long blockHeightTargetEnd, bool refuseLockedBlock, CancellationTokenSource cancellation)
        {
            ClassPeerObject peerObject = _peerDatabase[peerNetworkClientSyncObject.PeerIpTarget, peerNetworkClientSyncObject.PeerUniqueIdTarget, cancellation];

            if (peerObject == null)
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>>(false, null);

            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerInternPublicKey, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_BLOCK_DATA_BY_RANGE,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskBlockDataByRange()
                {
                    BlockHeightStart = blockHeightTargetStart,
                    BlockHeightEnd = blockHeightTargetEnd,
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };


            sendObject.PacketHash = ClassUtility.GenerateSha256FromString(sendObject.PacketContent + sendObject.PacketOrder);
            sendObject.PacketSignature = ClassWalletUtility.WalletGenerateSignature(peerObject.PeerInternPrivateKey, sendObject.PacketHash);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject, true, peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_BLOCK_DATA_BY_RANGE, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask block data " + blockHeightTargetStart + "|" + blockHeightTargetEnd + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>>(true, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_BLOCK_DATA_BY_RANGE)
                {
                    if (!TryGetPacketBlockDataByRange(_peerDatabase, peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, blockHeightTargetStart, refuseLockedBlock, cancellation, out ClassPeerPacketSendBlockDataRange packetSendBlockData))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " invalid block data by range received. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, cancellation);

                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>>(true, new ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>()
                    {
                        ObjectReturned = new ClassPeerPacketSendBlockDataRange()
                        {
                            ListBlockObject = packetSendBlockData.ListBlockObject,
                            PacketTimestamp = packetSendBlockData.PacketTimestamp
                        },
                        PacketNumericHash = packetSendBlockData.PacketNumericHash,
                        PacketNumericSignature = packetSendBlockData.PacketNumericSignature
                    });
                }


                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enougth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>>(true, null);
                }


                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>>(false, null);

                ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>>(await HandleUnexpectedPacketOrder(peerIp, peerPort, peerUniqueId, peerNetworkClientSyncObject.PeerPacketReceived, cancellation), null);
            }

            ClassLog.WriteLine("Packet build to send is empty and cannot be sent.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockDataRange>>(false, null);

        }

        /// <summary>
        /// Send a request to ask a block data target.
        /// </summary>
        /// <param name="peerNetworkClientSyncObject"></param>
        /// <param name="blockHeightTarget"></param>
        /// <param name="refuseLockedBlock"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>> SendAskBlockData(ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject, long blockHeightTarget, bool refuseLockedBlock, CancellationTokenSource cancellation)
        {
            ClassPeerObject peerObject = _peerDatabase[peerNetworkClientSyncObject.PeerIpTarget, peerNetworkClientSyncObject.PeerUniqueIdTarget, cancellation];

            if (peerObject == null)
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);

            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerInternPublicKey, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_BLOCK_DATA,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskBlockData()
                {
                    BlockHeight = blockHeightTarget,
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };


            sendObject.PacketHash = ClassUtility.GenerateSha256FromString(sendObject.PacketContent + sendObject.PacketOrder);
            sendObject.PacketSignature = ClassWalletUtility.WalletGenerateSignature(peerObject.PeerInternPrivateKey, sendObject.PacketHash);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject, true, peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_BLOCK_DATA, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask block data " + blockHeightTarget + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);
                }


                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(true, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_BLOCK_DATA)
                {
                    if (!TryGetPacketBlockData(_peerDatabase, peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, blockHeightTarget, refuseLockedBlock, cancellation, out ClassPeerPacketSendBlockData packetSendBlockData))
                    {
                        //ClassLog.WriteLine(peerIp + ":" + peerPort + " invalid block data received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, cancellation);

                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(true, new ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>()
                    {
                        ObjectReturned = new ClassPeerPacketSendBlockData()
                        {
                            BlockData = packetSendBlockData.BlockData,
                            PacketTimestamp = packetSendBlockData.PacketTimestamp
                        },
                        PacketNumericHash = packetSendBlockData.PacketNumericHash,
                        PacketNumericSignature = packetSendBlockData.PacketNumericSignature
                    });
                }


                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enougth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(true, null);
                }


                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);

                ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(await HandleUnexpectedPacketOrder(peerIp, peerPort, peerUniqueId, peerNetworkClientSyncObject.PeerPacketReceived, cancellation), null);
            }

            ClassLog.WriteLine("Packet build to send is empty and cannot be sent.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);

        }

        /// <summary>
        /// Send a request to ask a block transaction data target.
        /// </summary>
        /// <param name="peerNetworkClientSyncObject"></param>
        /// <param name="blockHeightTarget"></param>
        /// <param name="transactionId"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>> SendAskBlockTransactionData(ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject, long blockHeightTarget, int transactionId, CancellationTokenSource cancellation)
        {
            ClassPeerObject peerObject = _peerDatabase[peerNetworkClientSyncObject.PeerIpTarget, peerNetworkClientSyncObject.PeerUniqueIdTarget, cancellation];

            if (peerObject == null)
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);


            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerInternPublicKey, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_BLOCK_TRANSACTION_DATA,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskBlockTransactionData()
                {
                    BlockHeight = blockHeightTarget,
                    TransactionId = transactionId,
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };


            sendObject.PacketHash = ClassUtility.GenerateSha256FromString(sendObject.PacketContent + sendObject.PacketOrder);
            sendObject.PacketSignature = ClassWalletUtility.WalletGenerateSignature(peerObject.PeerInternPrivateKey, sendObject.PacketHash);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject, true, peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask block transaction from block height: " + blockHeightTarget + " id: " + transactionId + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(true, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA)
                {

                    if (!TryGetPacketBlockTransactionData(_peerDatabase, peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, blockHeightTarget, cancellation, out ClassPeerPacketSendBlockTransactionData packetSendBlockTransactionData))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " send an invalid block transaction data. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, cancellation);

                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(true, new ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>()
                    {
                        ObjectReturned = new ClassPeerPacketSendBlockTransactionData()
                        {
                            BlockHeight = blockHeightTarget,
                            TransactionObject = packetSendBlockTransactionData.TransactionObject,
                            PacketTimestamp = 0,
                            PacketNumericHash = string.Empty,
                            PacketNumericSignature = string.Empty
                        },
                        PacketNumericHash = packetSendBlockTransactionData.PacketNumericHash,
                        PacketNumericSignature = packetSendBlockTransactionData.PacketNumericSignature
                    });

                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enoguth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);


                ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(await HandleUnexpectedPacketOrder(peerIp, peerPort, peerUniqueId, peerNetworkClientSyncObject.PeerPacketReceived, cancellation), null);
            }

            ClassLog.WriteLine("Packet build to send is empty and cannot be sent.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);
        }

        /// <summary>
        /// Send a request to ask a block transaction data by range target.
        /// </summary>
        /// <param name="peerNetworkClientSyncObject"></param>
        /// <param name="blockHeightTarget"></param>
        /// <param name="transactionIdRangeStart"></param>
        /// <param name="transactionIdRangeEnd"></param>
        /// <param name="listWalletAndPublicKeys"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>> SendAskBlockTransactionDataByRange(ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject, long blockHeightTarget, int transactionIdRangeStart, int transactionIdRangeEnd, DisposableDictionary<string, string> listWalletAndPublicKeys, CancellationTokenSource cancellation)
        {
            ClassPeerObject peerObject = _peerDatabase[peerNetworkClientSyncObject.PeerIpTarget, peerNetworkClientSyncObject.PeerUniqueIdTarget, cancellation];

            if (peerObject == null)
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);

            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerInternPublicKey, _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_BLOCK_TRANSACTION_DATA_BY_RANGE,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskBlockTransactionDataByRange()
                {
                    BlockHeight = blockHeightTarget,
                    TransactionIdStartRange = transactionIdRangeStart,
                    TransactionIdEndRange = transactionIdRangeEnd,
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };

            sendObject.PacketHash = ClassUtility.GenerateSha256FromString(sendObject.PacketContent + sendObject.PacketOrder);
            sendObject.PacketSignature = ClassWalletUtility.WalletGenerateSignature(peerObject.PeerInternPrivateKey, sendObject.PacketHash);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject, true, peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA_BY_RANGE, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask block transaction from height: " + blockHeightTarget + " range: " + transactionIdRangeStart + "/" + transactionIdRangeEnd + ".", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(true, null);
                }


                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA_BY_RANGE)
                {

                    if (!TryGetPacketBlockTransactionDataByRange(_peerDatabase, peerNetworkClientSyncObject, peerIp, listWalletAndPublicKeys, _peerNetworkSettingObject, blockHeightTarget, cancellation, out ClassPeerPacketSendBlockTransactionDataByRange packetSendBlockTransactionDataByRange))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " send an invalid block transaction data. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket( _peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, cancellation);

                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(true, new ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>()
                    {
                        ObjectReturned = new ClassPeerPacketSendBlockTransactionDataByRange()
                        {
                            BlockHeight = blockHeightTarget,
                            ListTransactionObject = packetSendBlockTransactionDataByRange.ListTransactionObject,
                            PacketTimestamp = 0,
                            PacketNumericHash = string.Empty,
                            PacketNumericSignature = string.Empty
                        },
                        PacketNumericHash = packetSendBlockTransactionDataByRange.PacketNumericHash,
                        PacketNumericSignature = packetSendBlockTransactionDataByRange.PacketNumericSignature
                    });

                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enoguth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);

                ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(await HandleUnexpectedPacketOrder(peerIp, peerPort, peerUniqueId, peerNetworkClientSyncObject.PeerPacketReceived, cancellation), null);
            }

            ClassLog.WriteLine("Packet build to send is empty and cannot be sent.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);
        }

        #endregion

        #region Peer Task Sync - Shortcut sync functions.

        /// <summary>
        /// Sync block data transactions.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="peerTargetList"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> SyncBlockDataTransaction(long lastBlockHeightUnlocked, ClassBlockObject blockObject, Dictionary<int, ClassPeerTargetObject> peerTargetList, DisposableDictionary<string, string> listWalletAndPublicKeys, CancellationTokenSource cancellation)
        {
            if (blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight)
            {
                if (blockObject.BlockMiningPowShareUnlockObject == null)
                {
                    ClassLog.WriteLine("A block object target synced is invalid, the mining share is empty, retry again later.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return false;
                }
            }

            // Reset the work of transaction confirmations done from other peers.
            blockObject.BlockTransactionFullyConfirmed = false;
            blockObject.BlockUnlockValid = false;
            blockObject.BlockNetworkAmountConfirmations = 0;
            blockObject.BlockSlowNetworkAmountConfirmations = 0;
            blockObject.BlockLastHeightTransactionConfirmationDone = 0;
            blockObject.BlockTotalTaskTransactionConfirmationDone = 0;
            blockObject.BlockTransactionConfirmationCheckTaskDone = false;
            blockObject.BlockTransactionCountInSync = blockObject.TotalTransaction;

            if (blockObject.BlockHeight == BlockchainSetting.GenesisBlockHeight)
                blockObject.BlockTransactionCountInSync = BlockchainSetting.GenesisBlockTransactionCount;

            if (blockObject.BlockTransactionCountInSync > 0)
            {
                ClassLog.WriteLine("Attempt to sync " + blockObject.BlockTransactionCountInSync + " transaction(s) from the block height: " + blockObject.BlockHeight + "..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                int txInsertIndex = 0;

                // Start to sync all block tx's by range.
                if (_peerNetworkSettingObject.PeerEnableSyncTransactionByRange && blockObject.BlockTransactionCountInSync > 1)
                {
                    int startRange = 0;
                    int endRange = 0;
                    int countToSyncByRange = blockObject.BlockTransactionCountInSync;
                    int totalSynced = 0;
                    // The block contain more transaction than the range scheduled.
                    if (blockObject.BlockTransactionCountInSync >= _peerNetworkSettingObject.PeerMaxRangeTransactionToSyncPerRequest)
                    {
                        while (startRange < blockObject.BlockTransactionCountInSync)
                        {
                            if (cancellation.IsCancellationRequested)
                                break;

                            // Increase end range.
                            int incremented = 0;

                            while (incremented < _peerNetworkSettingObject.PeerMaxRangeTransactionToSyncPerRequest)
                            {
                                if (endRange + 1 > blockObject.BlockTransactionCountInSync)
                                    break;

                                endRange++;
                                incremented++;

                                if (incremented == _peerNetworkSettingObject.PeerMaxRangeTransactionToSyncPerRequest)
                                    break;
                            }

                            Dictionary<string, ClassTransactionObject> transactionObjectByRange = await StartAskBlockTransactionObjectByRangeFromListPeerTarget(peerTargetList, blockObject.BlockHeight, startRange, endRange, listWalletAndPublicKeys);


                            if (transactionObjectByRange == null)
                            {
                                ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, the list of tx received from peers is empty on the transaction range: " + startRange + "/" + endRange, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                return false;
                            }

                            if (transactionObjectByRange.Count == 0)
                            {
                                ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, list transaction data from tx range index: " + startRange + "/" + endRange +
                                                   " provide a different amount of tx expected " + transactionObjectByRange.Count + "/" + countToSyncByRange + ". Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                return false;
                            }

                            int indexTravel = startRange;
                            foreach (string transactionHash in transactionObjectByRange.Keys)
                            {
                                if (cancellation.IsCancellationRequested)
                                    return false;

                                if (!blockObject.BlockTransactions.ContainsKey(transactionHash))
                                {
                                    try
                                    {
                                        blockObject.BlockTransactions.Add(transactionHash, new ClassBlockTransaction(txInsertIndex, transactionObjectByRange[transactionHash]));
                                        txInsertIndex++;
                                        totalSynced++;
                                        startRange++;
                                    }
                                    catch (Exception exception)
                                    {
                                        ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, transaction data from tx hash: " + transactionObjectByRange[transactionHash].TransactionHash + " can't be inserted. Exception: " + exception.Message + " Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                        return false;
                                    }
                                }
                                else
                                {
                                    ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, transaction data from tx hash: " + transactionObjectByRange[transactionHash].TransactionHash + " can't be inserted. because this is already synced. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    return false;
                                }

                                indexTravel++;
                            }


                            // Clean up.
                            transactionObjectByRange.Clear();

                            if (totalSynced == blockObject.BlockTransactionCountInSync)
                                break;
                        }
                    }
                    // The block contain less transactions than the range scheduled.
                    else
                    {
                        endRange = blockObject.BlockTransactionCountInSync;

                        Dictionary<string, ClassTransactionObject> transactionObjectByRange = await StartAskBlockTransactionObjectByRangeFromListPeerTarget(peerTargetList, blockObject.BlockHeight, startRange, endRange, listWalletAndPublicKeys);


                        if (transactionObjectByRange == null)
                        {
                            ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, list transaction data from tx range index: " + startRange + "/" + endRange + " is empty. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            return false;
                        }

                        if (transactionObjectByRange.Count != countToSyncByRange || transactionObjectByRange.Count == 0)
                        {
                            ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, list transaction data from tx range index: " + startRange + "/" + endRange +
                                               " provide a different amount of tx expected " + transactionObjectByRange.Count + "/" + countToSyncByRange + ". Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                            transactionObjectByRange.Clear();
                            return false;
                        }

                        int indexTravel = startRange;
                        foreach (string transactionHash in transactionObjectByRange.Keys)
                        {
                            if (cancellation.IsCancellationRequested)
                                return false;

                            if (!blockObject.BlockTransactions.ContainsKey(transactionHash))
                            {
                                try
                                {
                                    blockObject.BlockTransactions.Add(transactionHash, new ClassBlockTransaction(txInsertIndex, transactionObjectByRange[transactionHash]));
                                    txInsertIndex++;
                                }
                                catch (Exception exception)
                                {
                                    ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, transaction data from tx hash: " + transactionObjectByRange[transactionHash].TransactionHash + " can't be inserted. Exception: " + exception.Message + " Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    return false;

                                }
                            }
                            else
                            {
                                ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, transaction data from tx hash: " + transactionObjectByRange[transactionHash].TransactionHash + " can't be inserted. because this is already synced. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                return false;
                            }
                            indexTravel++;
                        }

                        // Clean up.
                        transactionObjectByRange.Clear();
                    }
                }
                // Start to sync all block tx's one by one.
                else
                {
                    for (int txIndex = 0; txIndex < blockObject.BlockTransactionCountInSync; txIndex++)
                    {
                        if (cancellation.IsCancellationRequested)
                            return false;

                        ClassTransactionObject transactionObject = await StartAskBlockTransactionObjectFromListPeerTarget(peerTargetList, blockObject.BlockHeight, txIndex);

                        if (transactionObject == null)
                        {
                            ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, transaction data from tx index: " + txIndex + " is empty. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            return false;
                        }

                        if (!blockObject.BlockTransactions.ContainsKey(transactionObject.TransactionHash))
                        {
                            try
                            {
                                blockObject.BlockTransactions.Add(transactionObject.TransactionHash, new ClassBlockTransaction(txInsertIndex, transactionObject));

                                txInsertIndex++;
                            }
                            catch (Exception exception)
                            {
                                ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, transaction data from tx hash: " + transactionObject.TransactionHash + " can't be inserted. Exception: " + exception.Message + " Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                return false;
                            }
                        }
                        else
                        {
                            ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, transaction data from tx hash: " + transactionObject.TransactionHash + " can't be inserted. because this is already synced. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            return false;
                        }
                    }
                }

                // Final check.
                if (blockObject.BlockTransactions.Count == blockObject.BlockTransactionCountInSync)
                {
                    ClassBlockUtility.GetBlockTemplateFromBlockHash(blockObject.BlockHash, out ClassBlockTemplateObject blockTemplateObject);

                    if (ClassBlockUtility.CheckBlockHash(blockObject.BlockHash, blockObject.BlockHeight, blockObject.BlockDifficulty, blockTemplateObject.BlockPreviousTransactionCount, blockTemplateObject.BlockPreviousFinalTransactionHash) != ClassBlockEnumCheckStatus.VALID_BLOCK_HASH)
                        return false;

                    ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + "  successfully done. " + blockObject.BlockTransactions.Count + " tx's retrieved, insert to the blockchain database.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                    string finalTransactionHashToTest = ClassBlockUtility.GetFinalTransactionHashList(blockObject.BlockTransactions.Keys.ToList(), string.Empty);

                    if (finalTransactionHashToTest == blockObject.BlockFinalHashTransaction)
                    {
                        if (!await ClassBlockUtility.CheckBlockDataObject(blockObject, blockObject.BlockHeight, true, cancellation))
                        {
                            ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, the block utility check function report an error. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            return false;
                        }

                        if (blockObject.BlockHeight + BlockchainSetting.BlockSyncAmountNetworkConfirmationsCheckpointPassed < lastBlockHeightUnlocked)
                        {
                            blockObject.BlockNetworkAmountConfirmations = BlockchainSetting.BlockAmountNetworkConfirmations;
                            blockObject.BlockUnlockValid = true;
                        }

                        blockObject.BlockLastChangeTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;

                        if (ClassBlockchainStats.ContainsBlockHeight(blockObject.BlockHeight))
                        {
                            bool failed = true;

                            if (await ClassBlockchainDatabase.BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObject, true, cancellation))
                            {
                                failed = false;

                                await ClassMemPoolDatabase.RemoveMemPoolAllTxFromBlockHeightTarget(blockObject.BlockHeight, cancellation);

                                ClassLog.WriteLine("The block height: " + blockObject.BlockHeight + " data updated successfully, continue to sync.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                            }

                            if (failed)
                            {
                                ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, error on inserting the block data synced.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                return false;
                            }
                        }
                        else
                        {
                            if (await ClassBlockchainDatabase.BlockchainMemoryManagement.Add(blockObject.BlockHeight, blockObject, CacheBlockMemoryInsertEnumType.INSERT_IN_ACTIVE_MEMORY_OBJECT, cancellation))
                            {
                                // Insert new tx's in wallet index.
                                foreach (var tx in blockObject.BlockTransactions)
                                {
                                    if (cancellation.IsCancellationRequested)
                                        return false;

                                    ClassBlockchainDatabase.InsertWalletBlockTransactionHash(tx.Value.TransactionObject, cancellation);
                                }

                                ClassLog.WriteLine("The block height: " + blockObject.BlockHeight + " data inserted successfully, continue to sync.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                            }
                            else
                            {
                                ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, error on inserting the block data synced.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                return false;

                            }
                        }
                    }
                    else
                    {
                        ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, the final transaction hash is not the same of data of tx's synced. " + finalTransactionHashToTest + "/" + blockObject.BlockFinalHashTransaction, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        return false;
                    }
                }

            }
            else
            {
                ClassLog.WriteLine("Sync of transaction(s) from the block height: " + blockObject.BlockHeight + " failed, the amount of tx's to sync from a unlocked block cannot be equal of 0. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }


            return true;
        }

        #endregion

        #region Peer Task Sync - Other functions.

        /// <summary>
        /// Handle unexpected packet order.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerPort"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerPacketReceived"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> HandleUnexpectedPacketOrder(string peerIp, int peerPort, string peerUniqueId, ClassPeerPacketRecvObject peerPacketReceived, CancellationTokenSource cancellation)
        {

            bool result = false;

            try
            {
                PeerTotalUnexpectedPacketReceived++;

                bool doPeerKeysUpdate = false;
                bool forceUpdate = false;
                long timestamp = TaskManager.TaskManager.CurrentTimestampSecond;
                bool exist = _peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation);


                if (exist)
                {
                    if (!_peerDatabase[peerIp, peerUniqueId, cancellation].OnUpdateAuthKeys)
                    {
                        switch (peerPacketReceived.PacketOrder)
                        {
                            case ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE:
                            case ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_ENCRYPTION:
                                {
                                    forceUpdate = peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE;

                                    if (forceUpdate)
                                    {
                                        doPeerKeysUpdate = true;
                                        ClassLog.WriteLine("Invalid auth keys used on packet sent, attempt to send new auth keys to the peer target..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                    }
                                    else
                                    {
                                        if (_peerDatabase[peerIp, peerUniqueId, cancellation].PeerStatus != ClassPeerEnumStatus.PEER_ALIVE)
                                            doPeerKeysUpdate = true;
                                        else
                                            result = false;
                                    }
                                }
                                break;
                            case ClassPeerEnumPacketResponse.INVALID_PEER_PACKET:
                                {
                                    ClassLog.WriteLine("The packet sent to the peer is invalid.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                    if (peerUniqueId.IsNullOrEmpty(false, out _))
                                        result = true;
                                    else
                                    {
                                        if (_peerDatabase[peerIp, peerUniqueId, cancellation].PeerStatus != ClassPeerEnumStatus.PEER_ALIVE)
                                            doPeerKeysUpdate = true;
                                        else
                                            result = false;
                                    }
                                }
                                break;
                            case ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_TIMESTAMP:
                                {
                                    ClassLog.WriteLine("Invalid timestamp used on packet sent, will try again to send the packet to the peer target next time.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                    result = false;
                                }
                                break;
                            case ClassPeerEnumPacketResponse.NOT_YET_SYNCED:
                                {
                                    ClassLog.WriteLine("The peer: " + peerIp + ":" + peerPort + " is not enough synced.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                                    result = true;
                                }
                                break;
                            case ClassPeerEnumPacketResponse.SEND_DISCONNECT_CONFIRMATION:
                                {
                                    ClassLog.WriteLine("The peer: " + peerIp + ":" + peerPort + " send a disconnect packet confirmation.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                                    result = true;
                                }
                                break;
                            default:
                                ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject, cancellation);
                                break;
                        }
                    }
                }

                if (doPeerKeysUpdate)
                {
                    if (exist)
                        _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].OnUpdateAuthKeys = true;

                    if (await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(_peerDatabase, peerIp, peerPort, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject), cancellation, forceUpdate))
                    {
                        ClassLog.WriteLine("Auth keys generated successfully sent, peer target auth keys successfully received and updated.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                        if (_peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
                        {
                            _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].PeerLastUpdateOfKeysTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;
                            result = true;
                        }
                    }
                    else
                        ClassLog.WriteLine("Auth keys generated can't be sent to the peer target.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

                    if (exist)
                        _peerDatabase[peerIp, peerUniqueId, _cancellationTokenServiceSync].OnUpdateAuthKeys = false;
                }

            }
            catch
            {
                // Ignored.
            }

            return result;
        }

        /// <summary>
        /// Generate or Update a peer target list.
        /// </summary>
        /// <param name="peerTargetList"></param>
        /// <returns></returns>
        private Dictionary<int, ClassPeerTargetObject> GenerateOrUpdatePeerTargetList(Dictionary<int, ClassPeerTargetObject> peerTargetList)
        {
            return ClassPeerNetworkBroadcastFunction.GetRandomListPeerTargetAlive(_peerDatabase, _peerNetworkSettingObject.ListenIp, PeerOpenNatServerIp, string.Empty, peerTargetList, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenServiceSync);
        }

        /// <summary>
        /// Clear the peer list target propertly.
        /// </summary>
        /// <param name="peerTargetList"></param>
        /// <returns></returns>
        private void ClearPeerTargetList(Dictionary<int, ClassPeerTargetObject> peerTargetList, bool force)
        {
            if (peerTargetList != null)
            {
                foreach (int peerKey in peerTargetList.Keys.ToArray())
                {
                    try
                    {
                        if (!peerTargetList[peerKey].PeerNetworkClientSyncObject.PeerConnectStatus ||
                            force)
                        {
                            peerTargetList[peerKey].PeerNetworkClientSyncObject.DisconnectFromTarget();
                            peerTargetList[peerKey].PeerNetworkClientSyncObject.Dispose();
                            peerTargetList.Remove(peerKey);
                        }
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            }
        }

        #endregion

    }
}