using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Function;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Enum;
using SeguraChain_Lib.Blockchain.MemPool.Database;
using SeguraChain_Lib.Blockchain.Mining.Enum;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Sovereign.Database;
using SeguraChain_Lib.Blockchain.Sovereign.Object;
using SeguraChain_Lib.Blockchain.Stats.Function;
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

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.Service
{
    public class ClassPeerNetworkSyncServiceObject : ClassPeerSyncFunction, IDisposable
    {
        /// <summary>
        /// Settings.
        /// </summary>
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
        private ConcurrentDictionary<string, Dictionary<string, ClassPeerPacketSendNetworkInformation>> _listPeerNetworkInformationStats;



        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="peerOpenNatServerIp"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public ClassPeerNetworkSyncServiceObject(string peerOpenNatServerIp, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            PeerOpenNatServerIp = peerOpenNatServerIp;
            _peerNetworkSettingObject = peerNetworkSettingObject;
            _peerFirewallSettingObject = peerFirewallSettingObject;
            _listPeerNetworkInformationStats = new ConcurrentDictionary<string, Dictionary<string, ClassPeerPacketSendNetworkInformation>>();
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

            // Check the last block height with other peers to see if this one has been mined.
            StartTaskSyncCheckLastBlock();

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
            if (ClassPeerDatabase.DictionaryPeerDataObject.Count == 0)
                ClassLog.WriteLine("The peer don't have any public peer listed. Contact default peer list to get a new peer list..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

            foreach (string peerIp in BlockchainSetting.BlockchainStaticPeerList.Keys)
            {
                if (peerIp != _peerNetworkSettingObject.ListenIp && peerIp != PeerOpenNatServerIp)
                {
                    foreach (string peerUniqueId in BlockchainSetting.BlockchainStaticPeerList[peerIp].Keys)
                    {
                        int peerPort = BlockchainSetting.BlockchainStaticPeerList[peerIp][peerUniqueId];

                        if (!await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(peerIp, peerPort, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject), _cancellationTokenServiceSync, true))
                            ClassLog.WriteLine("Can't send auth keys to default peer: " + peerIp + ":" + peerPort + " | Peer Unique ID: " + peerUniqueId, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                        else
                        {
                            if (ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(peerIp))
                            {
                                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId))
                                {
                                    if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIsPublic)
                                        ClassPeerCheckManager.InputPeerClientValidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject);
                                }
                            }
                        }
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

            using (DisposableList<string> peerList = new DisposableList<string>(false, 0, ClassPeerDatabase.DictionaryPeerDataObject.Keys.ToList()))
            {
                using (DisposableList<Tuple<string, string>> peerListToInitialize = new DisposableList<Tuple<string, string>>()) // Peer IP | Peer unique id.
                {

                    foreach (var peerIp in peerList.GetList)
                    {


                        foreach (string peerUniqueId in ClassPeerDatabase.DictionaryPeerDataObject[peerIp].Keys.ToArray())
                        {
                            if (!ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIsPublic)
                                continue;

                            if (!ClassPeerCheckManager.CheckPeerClientStatus(peerIp, peerUniqueId, false, _peerNetworkSettingObject, _peerFirewallSettingObject))
                            {
                                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus == ClassPeerEnumStatus.PEER_BANNED && ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerBanDate + _peerNetworkSettingObject.PeerBanDelay < TaskManager.TaskManager.CurrentTimestampSecond)
                                    peerListToInitialize.Add(new Tuple<string, string>(peerIp, peerUniqueId));
                                else if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus == ClassPeerEnumStatus.PEER_DEAD && ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerBanDate + _peerNetworkSettingObject.PeerDeadDelay < TaskManager.TaskManager.CurrentTimestampSecond)
                                    peerListToInitialize.Add(new Tuple<string, string>(peerIp, peerUniqueId));
                            }
                            else if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(peerIp, peerUniqueId))
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

                                TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {
                                    try
                                    {

                                        int peerPort = ClassPeerDatabase.GetPeerPort(copyPeer.Item1, copyPeer.Item2);

                                        if (await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(copyPeer.Item1, peerPort, copyPeer.Item2, _peerNetworkSettingObject, _peerFirewallSettingObject), CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource(_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000).Token), true))
                                        {
                                            totalInitializedSuccessfully++;
                                            ClassPeerCheckManager.CleanPeerState(copyPeer.Item1, copyPeer.Item2, true);
                                            ClassPeerCheckManager.InputPeerClientValidPacket(copyPeer.Item1, copyPeer.Item2, _peerNetworkSettingObject);
                                        }
                                        else
                                        {
                                            ClassLog.WriteLine("Peer to initialize " + copyPeer.Item1 + " is completly dead after asking auth keys, remove it from peer list registered.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                                            if (ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(copyPeer.Item1))
                                            {
                                                if (ClassPeerDatabase.DictionaryPeerDataObject[copyPeer.Item1].ContainsKey(copyPeer.Item2))
                                                {
                                                    if (ClassPeerDatabase.DictionaryPeerDataObject[copyPeer.Item1].TryRemove(copyPeer.Item2, out _))
                                                    {
                                                        totalPeerRemoved++;
                                                        if (ClassPeerDatabase.DictionaryPeerDataObject[copyPeer.Item1].Count == 0)
                                                            ClassPeerDatabase.DictionaryPeerDataObject.Remove(copyPeer.Item1);
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
                foreach (var peer in ClassPeerDatabase.DictionaryPeerDataObject.Keys.ToArray())
                {
                    if (ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(peer))
                    {
                        if (ClassPeerDatabase.DictionaryPeerDataObject[peer].Count > 0)
                        {
                            foreach (string peerUniqueId in ClassPeerDatabase.DictionaryPeerDataObject[peer].Keys.ToArray())
                            {
                                if (ClassPeerDatabase.DictionaryPeerDataObject[peer][peerUniqueId].PeerIsPublic)
                                {
                                    if (ClassPeerDatabase.DictionaryPeerDataObject[peer][peerUniqueId].PeerStatus == ClassPeerEnumStatus.PEER_DEAD)
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
                            TaskManager.TaskManager.InsertTask(new Action(async () =>
                            {

                                try
                                {
                                    string peerIp = peerListToCheck[i1].Item1;
                                    string peerUniqueId = peerListToCheck[i1].Item2;
                                    int peerPort = ClassPeerDatabase.GetPeerPort(peerIp, peerUniqueId);

                                    if (await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(peerIp, peerPort, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject), CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token), true))
                                    {
                                        totalCheckSuccessfullyDone++;
                                        ClassPeerCheckManager.CleanPeerState(peerIp, peerUniqueId, true);
                                        ClassPeerCheckManager.InputPeerClientValidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject);
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
                    try
                    {
                        await StartContactDefaultPeerList();
                        await StartCheckHealthPeers();

                        if (ClassPeerDatabase.DictionaryPeerDataObject.Count > 0)
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

                            ClearPeerTargetList(peerTargetList);
                        }




                    }
                    catch (Exception error)
                    {
                        ClassLog.WriteLine("[WARNING] Error pending to sync current network informations. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                    }


                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }

            }), 0, _cancellationTokenServiceSync, null);

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
                    try
                    {
                        if (ClassPeerDatabase.DictionaryPeerDataObject.Count > 0)
                        {

                            peerTargetList = GenerateOrUpdatePeerTargetList(peerTargetList);

                            // If true, run every peer check tasks functions.
                            if (peerTargetList.Count > 0)
                                ClassLog.WriteLine("Total sovereign update(s) received: " + await StartAskSovereignUpdateListFromListPeerTarget(peerTargetList), ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);


                            ClearPeerTargetList(peerTargetList);
                        }
                    }
                    catch (Exception error)
                    {
                        ClassLog.WriteLine("[WARNING] Error pending to sync sovereign update(s). Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                    }

                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }

            }), 0, _cancellationTokenServiceSync, null);

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
                    try
                    {

                        if (ClassPeerDatabase.DictionaryPeerDataObject.Count > 0)
                        {

                            peerTargetList = GenerateOrUpdatePeerTargetList(peerTargetList);

                            // If true, run every peer check tasks functions.
                            if (peerTargetList.Count > 0)
                            {
                                long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();

                                if (_packetNetworkInformation != null)
                                {
                                    var currentPacketNetworkInformation = _packetNetworkInformation;


                                    #region Sync block objects and transaction(s).

                                    long lastBlockHeightUnlocked = await ClassBlockchainStats.GetLastBlockHeightUnlocked(_cancellationTokenServiceSync);
                                    long lastBlockHeightUnlockedChecked = await ClassBlockchainStats.GetLastBlockHeightNetworkConfirmationChecked(_cancellationTokenServiceSync);
                                    long lastBlockHeightNetworkUnlocked = GetHighestBlockHeightUnlockedFromPeerList(_listPeerNetworkInformationStats);


                                    using (DisposableList<long> blockListToSync = ClassBlockchainStats.GetListBlockMissing(lastBlockHeightNetworkUnlocked, true, false, _cancellationTokenServiceSync, _peerNetworkSettingObject.PeerMaxRangeBlockToSyncPerRequest))
                                    {
                                        if (blockListToSync.Count > 0)
                                        {
                                            ClassLog.WriteLine("Their is: " + blockListToSync.Count + " block(s) missing to sync. Current Height: " + ClassBlockchainStats.GetLastBlockHeight() + "/" + currentPacketNetworkInformation.LastBlockHeightUnlocked, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

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
                                                            if (blockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                                            {
                                                                using (DisposableDictionary<string, string> listWalletAndPublicKeys = new DisposableDictionary<string, string>())
                                                                {
                                                                    if (!await SyncBlockDataTransaction(lastBlockHeightNetworkUnlocked, blockObject, peerTargetList, listWalletAndPublicKeys, _cancellationTokenServiceSync))
                                                                    {
                                                                        ClassLog.WriteLine("Failed to sync block transaction(s) from the block height: " + blockObject.BlockHeight, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                        break;
                                                                    }
                                                                    else
                                                                    {
                                                                        if (index == syncBlockResult.Count - 1)
                                                                            await ClassBlockchainDatabase.GenerateNewMiningBlockObject(blockObject.BlockHeight, blockObject.BlockHeight + 1, blockObject.TimestampFound, blockObject.BlockWalletAddressWinner, false, false, _cancellationTokenServiceSync);
                                                                    }
                                                                }
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
                                                    ClassLog.WriteLine("Can't sync " + blockListToSync.Count + " block(s), retry again later.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                                    #region Resync the last block height if necessary, to unlock the sync progress from the node majority.

                                                    ClassBlockObject blockObjectInformationsToCheck = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockInformationDataStrategy(lastBlockHeightUnlocked, _cancellationTokenServiceSync);

                                                    if (blockObjectInformationsToCheck != null)
                                                    {
                                                        switch (await StartCheckBlockDataUnlockedFromListPeerTarget(peerTargetList, blockObjectInformationsToCheck.BlockHeight, blockObjectInformationsToCheck))
                                                        {
                                                            case ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.INVALID_BLOCK:
                                                            case ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.NO_CONSENSUS_FOUND:
                                                                {
                                                                    using (DisposableList<long> blockListToCorrect = new DisposableList<long>(false, 0, new List<long>() { blockObjectInformationsToCheck.BlockHeight }))
                                                                    {
                                                                        using (var syncBlockFixResult = await StartAskBlockObjectFromListPeerTarget(peerTargetList, blockListToCorrect, true))
                                                                        {

                                                                            if (syncBlockFixResult.Count > 0)
                                                                            {
                                                                                ClassLog.WriteLine("The block height: " + lastBlockHeight + " seems to be retrieve from peers, sync transactions..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                                                                                if (syncBlockFixResult[blockObjectInformationsToCheck.BlockHeight]?.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                                                                {
                                                                                    using (DisposableDictionary<string, string> listWalletAndPublicKeys = new DisposableDictionary<string, string>())
                                                                                    {
                                                                                        if (!await SyncBlockDataTransaction(lastBlockHeightUnlocked, syncBlockFixResult[blockObjectInformationsToCheck.BlockHeight], peerTargetList, listWalletAndPublicKeys, _cancellationTokenServiceSync))
                                                                                        {
                                                                                            ClassLog.WriteLine("Sync of transaction(s) from the block height: " + syncBlockFixResult[blockObjectInformationsToCheck.BlockHeight].BlockHeight + " failed, the amount of tx's to sync from a unlocked block cannot be equal of 0. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                                            break;
                                                                                        }
                                                                                        else
                                                                                            await FixMiningBlockLocked(lastBlockHeight, lastBlockHeightUnlocked, lastBlockHeight, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenServiceSync);
                                                                                    }

                                                                                    ClassLog.WriteLine("The block height: " + lastBlockHeight + " retrieved from peers, is fixed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                                                                }
                                                                                else
                                                                                {
                                                                                    ClassLog.WriteLine("Sync of transaction(s) from the block height: " + syncBlockFixResult[0].BlockHeight + " failed. The block is not unlocked. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                ClassLog.WriteLine("Can't sync again transactions for the block height: " + lastBlockHeight + " cancel the task of checking blocks.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                break;
                                                        }
                                                    }

                                                    #endregion
                                                }
                                            }
                                        }
                                    }


                                    #endregion

                                }
                            }

                            ClearPeerTargetList(peerTargetList);
                        }

                    }
                    catch (Exception error)
                    {
                        ClassLog.WriteLine("[WARNING] Error pending to sync blocks and tx's. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                    }

                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);

                }

            }), 0, _cancellationTokenServiceSync, null);

        }

        /// <summary>
        /// Start the task who correct blocks and tx's who are wrong from other peers.
        /// </summary>
        private void StartTaskSyncCheckBlockAndTx()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                using (DisposableDictionary<int, ClassPeerTargetObject> peerTargetList = new DisposableDictionary<int, ClassPeerTargetObject>())
                {

                    while (_peerSyncStatus)
                    {
                        try
                        {
                            if (ClassPeerDatabase.DictionaryPeerDataObject.Count > 0 && ClassBlockchainStats.BlockCount > 0 && ClassBlockchainStats.GetCountBlockLocked() <= 1)
                            {
                                peerTargetList.GetList = GenerateOrUpdatePeerTargetList(peerTargetList.GetList);

                                // If true, run every peer check tasks functions.
                                if (peerTargetList.Count > 0)
                                {

                                    if (_packetNetworkInformation != null)
                                    {
                                        var currentPacketNetworkInformation = _packetNetworkInformation;

                                        long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();
                                        long lastBlockHeightUnlocked = await ClassBlockchainStats.GetLastBlockHeightUnlocked(_cancellationTokenServiceSync);

                                        #region Check block's and tx's synced with other peers and increment network confirmations.

                                        int totalBlockChecked = 0;

                                        using (DisposableList<long> listBlockMissed = ClassBlockchainStats.GetListBlockMissing(lastBlockHeight, false, true, _cancellationTokenServiceSync, _peerNetworkSettingObject.PeerMaxRangeBlockToSyncPerRequest))
                                        {
                                            if (listBlockMissed.Count == 0)
                                            {
                                                using (DisposableList<long> listBlockNetworkUnconfirmed = await ClassBlockchainStats.GetListBlockNetworkUnconfirmed(_cancellationTokenServiceSync))
                                                {
                                                    if (listBlockNetworkUnconfirmed.Count > 0)
                                                    {
                                                        ClassLog.WriteLine("Increment block check network confirmations..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);

                                                        bool cancelCheck = false;

                                                        foreach (long blockHeightToCheck in listBlockNetworkUnconfirmed.GetAll)
                                                        {

                                                            ClassBlockObject blockObjectInformationsToCheck = await ClassBlockchainStats.GetBlockInformationData(blockHeightToCheck, CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource(_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000).Token));

                                                            if (blockObjectInformationsToCheck == null)
                                                            {
                                                                ClassLog.WriteLine("Can't check the block height: " + blockHeightToCheck + ", this one can't be retrieved successfully.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                                                break;
                                                            }

                                                            ClassLog.WriteLine("Start to check the block height: " + blockHeightToCheck + " with other peers..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);

                                                            switch (await StartCheckBlockDataUnlockedFromListPeerTarget(peerTargetList.GetList, blockHeightToCheck, blockObjectInformationsToCheck))
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
                                                                            using (var result = await StartAskBlockObjectFromListPeerTarget(peerTargetList.GetList, blockListToCorrect, true))
                                                                            {

                                                                                if (result.Count > 0)
                                                                                {
                                                                                    ClassLog.WriteLine("The block height: " + blockHeightToCheck + " seems to be retrieve from peers, sync transactions..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);

                                                                                    if (result[blockHeightToCheck]?.BlockStatus == ClassBlockEnumStatus.UNLOCKED && result[blockHeightToCheck]?.BlockHeight == blockHeightToCheck)
                                                                                    {
                                                                                        using (DisposableDictionary<string, string> listWalletAndPublicKeys = new DisposableDictionary<string, string>())
                                                                                        {
                                                                                            if (!await SyncBlockDataTransaction(lastBlockHeightUnlocked, result[blockHeightToCheck], peerTargetList.GetList, listWalletAndPublicKeys, _cancellationTokenServiceSync))
                                                                                            {
                                                                                                ClassLog.WriteLine("Sync of transaction(s) from the block height: " + result[blockHeightToCheck].BlockHeight + " failed, the amount of tx's to sync from a unlocked block cannot be equal of 0. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                                                break;
                                                                                            }
                                                                                            else
                                                                                                await FixMiningBlockLocked(blockHeightToCheck, lastBlockHeightUnlocked, lastBlockHeight, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenServiceSync);
                                                                                        }

                                                                                        ClassLog.WriteLine("The block height: " + blockHeightToCheck + " retrieved from peers, is fixed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        ClassLog.WriteLine("Sync of transaction(s) from the block height: " + result[0].BlockHeight + " failed. The block is not unlocked. Cancel sync and retry again.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                                        cancelCheck = true;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    ClassLog.WriteLine("Can't sync again transactions for the block height: " + blockHeightToCheck + " cancel the task of checking blocks.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                                                                    cancelCheck = true;
                                                                                }
                                                                            }
                                                                        }

                                                                        #endregion
                                                                    }
                                                                    break;
                                                                case ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.VALID_BLOCK:
                                                                    {
                                                                        ClassBlockObject blockObjectToCheck = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockDataStrategy(blockHeightToCheck, false, true, _cancellationTokenServiceSync);

                                                                        if (blockObjectToCheck == null)
                                                                            cancelCheck = true;
                                                                        else
                                                                        {
                                                                            if (blockObjectToCheck.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                                                            {
                                                                                blockObjectToCheck.BlockLastChangeTimestamp = TaskManager.TaskManager.CurrentTimestampMillisecond;

                                                                                ClassLog.WriteLine("The block height: " + blockHeightToCheck + " seems to be valid for other peers. Amount of confirmations: " + blockObjectToCheck.BlockNetworkAmountConfirmations + "/" + BlockchainSetting.BlockAmountNetworkConfirmations, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                                                                                // Faster network confirmations.
                                                                                if (blockHeightToCheck + blockObjectToCheck.BlockNetworkAmountConfirmations < lastBlockHeight)
                                                                                {
                                                                                    blockObjectToCheck.BlockNetworkAmountConfirmations++;

                                                                                    if (blockObjectToCheck.BlockNetworkAmountConfirmations >= BlockchainSetting.BlockAmountNetworkConfirmations)
                                                                                    {
                                                                                        ClassLog.WriteLine("The block height: " + blockHeightToCheck + " is totally valid. The node can start to confirm tx's of this block.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkCyan);
                                                                                        blockObjectToCheck.BlockUnlockValid = true;
                                                                                    }
                                                                                    if (!await ClassBlockchainDatabase.BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObjectToCheck, true, _cancellationTokenServiceSync))
                                                                                        ClassLog.WriteLine("The block height: " + blockHeightToCheck + " seems to be valid for other peers. But can't push updated data into the database.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);

                                                                                }
                                                                                // Increment slowly network confirmations.
                                                                                else
                                                                                {
                                                                                    if (blockObjectToCheck.BlockSlowNetworkAmountConfirmations >= BlockchainSetting.BlockAmountSlowNetworkConfirmations)
                                                                                    {
                                                                                        blockObjectToCheck.BlockNetworkAmountConfirmations++;
                                                                                        blockObjectToCheck.BlockSlowNetworkAmountConfirmations = 0;
                                                                                    }
                                                                                    else
                                                                                        blockObjectToCheck.BlockSlowNetworkAmountConfirmations++;


                                                                                    if (blockObjectToCheck.BlockNetworkAmountConfirmations >= BlockchainSetting.BlockAmountNetworkConfirmations)
                                                                                    {
                                                                                        ClassLog.WriteLine("The block height: " + blockHeightToCheck + " is totally valid. The node can start to confirm tx's of this block.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkCyan);
                                                                                        blockObjectToCheck.BlockUnlockValid = true;
                                                                                    }

                                                                                    if (!await ClassBlockchainDatabase.BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObjectToCheck, true, _cancellationTokenServiceSync))
                                                                                        ClassLog.WriteLine("The block height: " + blockHeightToCheck + " seems to be valid for other peers. But can't push updated data into the database.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                    break;
                                                            }

                                                            if (cancelCheck)
                                                                break;

                                                            totalBlockChecked++;

                                                            if (totalBlockChecked >= _peerNetworkSettingObject.PeerMaxRangeBlockToSyncPerRequest)
                                                                break;
                                                        }

                                                        ClassLog.WriteLine("Increment block check network confirmations done..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);
                                                    }
                                                }
                                            }
                                            else
                                                ClassLog.WriteLine("Increment block check network confirmations canceled. Their is " + listBlockMissed.Count + " block(s) missed to sync.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);
                                        }


                                        #endregion

                                    }
                                }

                                ClearPeerTargetList(peerTargetList.GetList);
                            }
                        }
                        catch (Exception error)
                        {
                            ClassLog.WriteLine("[WARNING] Error pending to check blocks and tx's synced. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                        }

                        await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                    }
                }

            }), 0, _cancellationTokenServiceSync, null);

        }

        /// <summary>
        /// Start the task who check with other peers the last block height to mine and see if this one has been mined.
        /// </summary>
        private void StartTaskSyncCheckLastBlock()
        {
            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                Dictionary<int, ClassPeerTargetObject> peerTargetList = null;

                while (_peerSyncStatus)
                {
                    try
                    {

                        if (ClassPeerDatabase.DictionaryPeerDataObject.Count > 0 && ClassBlockchainStats.BlockCount > 0)
                        {
                            peerTargetList = GenerateOrUpdatePeerTargetList(peerTargetList);

                            // If true, run every peer check tasks functions.
                            if (peerTargetList.Count > 0)
                            {
                                long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();
                                if (lastBlockHeight >= BlockchainSetting.GenesisBlockHeight)
                                {
                                    if (ClassBlockchainStats.GetCountBlockLocked() == 1)
                                    {
                                        #region Check if the last block to mine has been mined on other nodes of the network.

                                        ClassBlockObject blockObjectInformations = await ClassBlockchainStats.GetBlockInformationData(lastBlockHeight, _cancellationTokenServiceSync);

                                        if (blockObjectInformations.BlockStatus == ClassBlockEnumStatus.LOCKED)
                                        {
                                            using (DisposableList<long> blockListToSync = new DisposableList<long>(false, 0, new List<long>() { lastBlockHeight }))
                                            {
                                                peerTargetList = GenerateOrUpdatePeerTargetList(peerTargetList);

                                                using (var syncBlockResult = await StartAskBlockObjectFromListPeerTarget(peerTargetList, blockListToSync, true))
                                                {

                                                    if (syncBlockResult.Count > 0)
                                                    {
                                                        ClassBlockObject blockObject = syncBlockResult.GetList.ElementAt(0).Value;

                                                        if (blockObject != null)
                                                        {
                                                            if (blockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                                            {
                                                                if (blockObject.BlockMiningPowShareUnlockObject != null)
                                                                {
                                                                    ClassBlockObject previousBlockObjectInformations = await ClassBlockchainStats.GetBlockInformationData(lastBlockHeight - 1, _cancellationTokenServiceSync);
                                                                    ClassBlockObject currentBlockObjectInformations = await ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockInformationDataStrategy(lastBlockHeight, _cancellationTokenServiceSync);

                                                                    ClassMiningPoWaCEnumStatus miningPoWaCStatus = ClassMiningPoWaCUtility.CheckPoWaCShare(BlockchainSetting.CurrentMiningPoWaCSettingObject(lastBlockHeight),
                                                                        blockObject.BlockMiningPowShareUnlockObject, lastBlockHeight, currentBlockObjectInformations.BlockHash,
                                                                        currentBlockObjectInformations.BlockDifficulty,
                                                                        previousBlockObjectInformations.TotalTransaction,
                                                                        previousBlockObjectInformations.BlockFinalHashTransaction,
                                                                        out BigInteger _, out _);

                                                                    if (miningPoWaCStatus == ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE)
                                                                    {
                                                                        if (currentBlockObjectInformations.BlockStatus == ClassBlockEnumStatus.LOCKED)
                                                                        {
                                                                            var resultUnlockShare = await ClassBlockchainDatabase.UnlockCurrentBlockAsync(lastBlockHeight, blockObject.BlockMiningPowShareUnlockObject, false, _peerNetworkSettingObject.ListenIp, PeerOpenNatServerIp, false, true, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenServiceSync);

                                                                            if (resultUnlockShare == ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED)
                                                                                ClassLog.WriteLine("Attempt to check if the block height: " + lastBlockHeight + " has been mined successfully done.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                            else
                                                                                ClassLog.WriteLine("Attempt to check if the block height: " + lastBlockHeight + " has been mined failed. The attempt to unlock it failed: " + resultUnlockShare + ". Resync the block with other peers.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                        }
                                                                        else
                                                                            ClassLog.WriteLine("Attempt to check if the block height: " + lastBlockHeight + " is already unlocked.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                    }
                                                                    else
                                                                    {
                                                                        ClassLog.WriteLine("Attempt to check if the block height: " + lastBlockHeight + " has been mined failed. The mining share who unlock the block received is wrong: " + miningPoWaCStatus, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                                                        switch (miningPoWaCStatus)
                                                                        {
                                                                            case ClassMiningPoWaCEnumStatus.INVALID_BLOCK_HASH:
                                                                                {
                                                                                    if (ClassBlockUtility.GetBlockTemplateFromBlockHash(currentBlockObjectInformations.BlockHash, out ClassBlockTemplateObject blockTemplateObject))
                                                                                    {
                                                                                        if (previousBlockObjectInformations.BlockFinalHashTransaction != blockTemplateObject.BlockPreviousFinalTransactionHash ||
                                                                                        previousBlockObjectInformations.BlockWalletAddressWinner != blockTemplateObject.BlockPreviousWalletAddressWinner ||
                                                                                        previousBlockObjectInformations.BlockDifficulty != blockTemplateObject.BlockDifficulty ||
                                                                                        previousBlockObjectInformations.BlockHeight != blockTemplateObject.BlockHeight)
                                                                                        {
                                                                                            ClassLog.WriteLine("The block height: " + lastBlockHeight + " checked provide a blocktemplate who is wrong. Replace it by the data synced from the network.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                                                                            await ClassBlockchainDatabase.BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObject, true, _cancellationTokenServiceSync);
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        ClassLog.WriteLine("The block height: " + lastBlockHeight + " checked provide a blocktemplate fully wrong. Replace it by the data synced from the network.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                                                        await ClassBlockchainDatabase.BlockchainMemoryManagement.InsertOrUpdateBlockObjectToCache(blockObject, true, _cancellationTokenServiceSync);
                                                                                    }

                                                                                }
                                                                                break;
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                    ClassLog.WriteLine("Attempt to check if the block height: " + lastBlockHeight + " has been mined failed. The mining share who unlock the block received is empty.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                            }

                                                        }
                                                        else
                                                            ClassLog.WriteLine("Attempt to check if the block height: " + lastBlockHeight + " has been mined failed. The block received is empty.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                                    }
                                                }
                                            }

                                        }

                                        #endregion

                                    }
                                }

                            }

                            ClearPeerTargetList(peerTargetList);
                        }
                    }
                    catch (Exception error)
                    {
                        ClassLog.WriteLine("[WARNING] Error pending to check the last block synced. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                    }

                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);

                }

            }), 0, _cancellationTokenServiceSync, null);

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
                        if (ClassPeerDatabase.DictionaryPeerDataObject.Count > 0)
                        {
                            peerTargetList = GenerateOrUpdatePeerTargetList(peerTargetList);

                            // If true, run every peer check tasks functions.
                            if (peerTargetList.Count > 0)
                            {
                                ClassLog.WriteLine("Start sync to retrieve back new network informations..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                bool blockchainNetworkInformationsStatus = true;

                                Tuple<ClassPeerPacketSendNetworkInformation, float> packetNetworkInformationTmp = await StartAskNetworkInformationFromListPeerTarget(peerTargetList);

                                if (packetNetworkInformationTmp != null)
                                {
                                    if (packetNetworkInformationTmp.Item2 > 0)
                                    {

                                        // Sync block missing or not yet synced.
                                        if (blockchainNetworkInformationsStatus)
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
                                        else
                                            ClassLog.WriteLine("Current network informations received are invalid. Retry the sync later..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    }
                                }
                                else
                                    ClassLog.WriteLine("Current network informations not received. Retry the sync later..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            }

                            ClearPeerTargetList(peerTargetList);
                        }

                    }
                    catch (Exception error)
                    {
                        ClassLog.WriteLine("[WARNING] Error pending to sync current network informations. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                    }

                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                }

            }), 0, _cancellationTokenServiceSync);

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

                if (peerListTarget[i] == null)
                {
                    totalTaskComplete++;
                    continue;
                }

                if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget))
                {
                    totalTaskComplete++;
                    continue;
                }

                var i1 = i;


                TaskManager.TaskManager.InsertTask(new Action(async () =>
                {
                    try
                    {
                        string peerIpTarget = peerListTarget[i1].PeerIpTarget;
                        int peerPortTarget = peerListTarget[i1].PeerPortTarget;
                        if (await SendAskPeerList(peerListTarget[i1].PeerNetworkClientSyncObject, CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token)))
                        {
                            totalResponseOk++;
                            ClassLog.WriteLine("Peer list asked to peer target: " + peerIpTarget + ":" + peerPortTarget + " successfully received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                        }
                    }
                    catch
                    {
                        // The peer collection list can change or has been disposed/cleaned.
                    }

                    totalTaskComplete++;

                }), 0, null);

            }


            #endregion

            // Await the task is complete.
            while (totalTaskComplete < peerListTarget.Count)
                await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);


            ClassLog.WriteLine("Total Peers Task(s) done: " + totalTaskComplete + "/" + peerListTarget.Count, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);

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

                    if (peerListTarget[i] == null)
                    {
                        totalTaskComplete++;
                        continue;
                    }

                    if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget))
                    {
                        totalTaskComplete++;
                        continue;
                    }

                    TaskManager.TaskManager.InsertTask(new Action(async () =>
                    {
                        try
                        {
                            if (peerListTarget.ContainsKey(i))
                            {
                                var result = await SendAskSovereignUpdateList(peerListTarget[i].PeerNetworkClientSyncObject, CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token));

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
                        if (peerListTarget[i] == null)
                        {
                            totalTaskComplete++;
                            continue;
                        }

                        if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget))
                        {
                            totalTaskComplete++;
                            continue;
                        }


                        TaskManager.TaskManager.InsertTask(new Action(async () =>
                        {

                            foreach (var sovereignUpdateHash in hashSetSovereignUpdateHash.GetList)
                            {
                                if (peerListTarget.ContainsKey(i))
                                {
                                    var result = await SendAskSovereignUpdateData(peerListTarget[i].PeerNetworkClientSyncObject, sovereignUpdateHash, CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token));

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
                            }

                            totalTaskComplete++;
                        }), 0, null);

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
                                if (peerListTarget[i] == null)
                                {
                                    totalTaskDone++;
                                    continue;
                                }

                                if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget))
                                {
                                    totalTaskDone++;
                                    continue;
                                }


                                var i1 = i;
                                TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {

                                    if (peerListTarget.ContainsKey(i1))
                                    {
                                        try
                                        {
                                            Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>> result = await SendAskNetworkInformation(peerListTarget[i1].PeerNetworkClientSyncObject, CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token));

                                            if (result != null)
                                            {
                                                if (result.Item1 && result.Item2 != null)
                                                {
                                                    bool peerRanked = false;

                                                    if (_peerNetworkSettingObject.PeerEnableSovereignPeerVote)
                                                    {
                                                        if (CheckIfPeerIsRanked(peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, result.Item2.ObjectReturned, result.Item2.PacketNumericHash, result.Item2.PacketNumericSignature, CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token), out string numericPublicKeyOut))
                                                            peerRanked = !listOfRankedPeerPublicKeySaved.ContainsKey(numericPublicKeyOut) ? listOfRankedPeerPublicKeySaved.TryAdd(numericPublicKeyOut, 0) : false;
                                                    }

                                                    // Ignore packet timestamp now, to not make false comparing of other important data's.
                                                    if (result.Item2.ObjectReturned != null)
                                                    {
                                                        if (result.Item2.ObjectReturned.LastBlockHeightUnlocked > 0 &&
                                                        result.Item2.ObjectReturned.CurrentBlockHeight > 0)
                                                        {
                                                            if (result.Item2.ObjectReturned.CurrentBlockHeight >= ClassBlockchainStats.GetLastBlockHeight() &&
                                                                result.Item2.ObjectReturned.LastBlockHeightUnlocked <= result.Item2.ObjectReturned.CurrentBlockHeight)
                                                            {

                                                                var packetData = result.Item2.ObjectReturned;

                                                                packetData.PacketTimestamp = 0;

                                                                bool insert = false;
                                                                if (!_listPeerNetworkInformationStats.ContainsKey(peerListTarget[i1].PeerIpTarget))
                                                                {
                                                                    if (_listPeerNetworkInformationStats.TryAdd(peerListTarget[i1].PeerIpTarget, new Dictionary<string, ClassPeerPacketSendNetworkInformation>()))
                                                                    {

                                                                        if (!_listPeerNetworkInformationStats[peerListTarget[i1].PeerIpTarget].ContainsKey(peerListTarget[i1].PeerUniqueIdTarget))
                                                                        {
                                                                            _listPeerNetworkInformationStats[peerListTarget[i1].PeerIpTarget].Add(peerListTarget[i1].PeerUniqueIdTarget, packetData);
                                                                            insert = true;
                                                                        }
                                                                        else
                                                                        {
                                                                            _listPeerNetworkInformationStats[peerListTarget[i1].PeerIpTarget][peerListTarget[i1].PeerUniqueIdTarget] = packetData;
                                                                            insert = true;
                                                                        }
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    if (!_listPeerNetworkInformationStats[peerListTarget[i1].PeerIpTarget].ContainsKey(peerListTarget[i1].PeerUniqueIdTarget))
                                                                    {
                                                                        _listPeerNetworkInformationStats[peerListTarget[i1].PeerIpTarget].Add(peerListTarget[i1].PeerUniqueIdTarget, packetData);
                                                                        insert = true;
                                                                    }
                                                                    else
                                                                    {
                                                                        _listPeerNetworkInformationStats[peerListTarget[i1].PeerIpTarget][peerListTarget[i1].PeerUniqueIdTarget] = packetData;
                                                                        insert = true;
                                                                    }
                                                                }

                                                                if (insert)
                                                                {
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
                                        }
                                        catch (Exception error)
                                        {
                                            ClassLog.WriteLine("Error on asking network informations. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY, true);
                                        }
                                    }

                                    totalTaskDone++;

                                }), 0, null);
                            }


                            while (totalTaskDone < totalTaskToDo)
                                await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);


                            try
                            {
                                if (totalResponseOk < _peerNetworkSettingObject.PeerMinAvailablePeerSync)
                                {

                                    ClassLog.WriteLine("Not enough peers response for accept network informations data synced.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    return new Tuple<ClassPeerPacketSendNetworkInformation, float>(null, 0);
                                }


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

        private class ClassBlockSync {
            public long BlockHeight;
            public string BlockHash;
            public int PeerId;
            public int TotalVote;
        }

        /// <summary>
        /// Run multiple async task to ask a list of blocks object.
        /// </summary>
        private async Task<DisposableSortedList<long, ClassBlockObject>> StartAskBlockObjectFromListPeerTarget(Dictionary<int, ClassPeerTargetObject> peerListTarget, DisposableList<long> listBlockHeightTarget, bool refuseLockedBlock)
        {
            DisposableSortedList<long, ClassBlockObject> blockListSynced = new DisposableSortedList<long, ClassBlockObject>();

            int totalTaskToDo = peerListTarget.Count;
            int totalTaskDone = 0;


            using (DisposableConcurrentDictionary<long, Dictionary<int, ClassBlockObject>> listBlockObjectsReceived = new DisposableConcurrentDictionary<long, Dictionary<int, ClassBlockObject>>())
            {
                foreach (var blockHeight in listBlockHeightTarget.GetAll)
                    listBlockObjectsReceived.TryAdd(blockHeight, new Dictionary<int, ClassBlockObject>());

                foreach (int i in peerListTarget.Keys)
                {
                    if (peerListTarget[i] == null)
                    {
                        totalTaskDone++;
                        continue;
                    }

                    if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget))
                    {
                        totalTaskDone++;
                        continue;
                    }

                    if (!_listPeerNetworkInformationStats.ContainsKey(peerListTarget[i].PeerIpTarget))
                    {
                        totalTaskDone++;
                        continue;
                    }

                    int i1 = i;
                    TaskManager.TaskManager.InsertTask(new Action(async () =>
                    {
                        if (peerListTarget.ContainsKey(i1))
                        {

                            foreach (var blockHeight in listBlockHeightTarget.GetAll)
                            {

                                if (!_listPeerNetworkInformationStats.ContainsKey(peerListTarget[i].PeerIpTarget))
                                    break;

                                if (!_listPeerNetworkInformationStats[peerListTarget[i].PeerIpTarget].ContainsKey(peerListTarget[i].PeerUniqueIdTarget))
                                    break;


                                if (_listPeerNetworkInformationStats[peerListTarget[i].PeerIpTarget][peerListTarget[i].PeerUniqueIdTarget].LastBlockHeightUnlocked < blockHeight)
                                    break;

                                CancellationTokenSource cancellationTokenSourceTaskSync = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token);

                                Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>> result = await SendAskBlockData(peerListTarget[i1].PeerNetworkClientSyncObject, blockHeight, refuseLockedBlock, cancellationTokenSourceTaskSync);

                                if (result == null || !result.Item1 || result.Item2 == null)
                                    continue;

                                if (result.Item2.ObjectReturned.BlockData.BlockHeight != blockHeight)
                                    ClassPeerCheckManager.InputPeerClientInvalidPacket(peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                else
                                {
                                    ClassBlockObject blockObject = result.Item2.ObjectReturned.BlockData;

                                    #region Ensure to clean the block object received.

                                    blockObject.BlockTransactionFullyConfirmed = false;
                                    blockObject.BlockUnlockValid = false;
                                    blockObject.BlockNetworkAmountConfirmations = 0;
                                    blockObject.BlockSlowNetworkAmountConfirmations = 0;
                                    blockObject.BlockLastHeightTransactionConfirmationDone = 0;
                                    blockObject.BlockTotalTaskTransactionConfirmationDone = 0;
                                    blockObject.BlockTransactionConfirmationCheckTaskDone = false;
                                    blockObject?.BlockTransactions.Clear();

                                    listBlockObjectsReceived[blockHeight].Add(i1, blockObject);

                                    #endregion
                                }
                            }
                        }

                        totalTaskDone++;

                    }), 0, null);
                }

                while (totalTaskDone < totalTaskToDo)
                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);

                if (listBlockObjectsReceived.GetList.Values.Count >= _peerNetworkSettingObject.PeerMinAvailablePeerSync)
                {
                    foreach (int blockHeight in listBlockObjectsReceived.GetList.Keys)
                    {
                        using (DisposableDictionary<string, ClassBlockSync> listBlockSynced = new DisposableDictionary<string, ClassBlockSync>())
                        {
                            foreach (int peerId in listBlockObjectsReceived[blockHeight].Keys)
                            {
                                string blockObjectHash = ClassUtility.GenerateSha256FromString(string.Concat("", ClassBlockUtility.BlockObjectToStringBlockData(listBlockObjectsReceived[blockHeight][peerId], false).ToList()));

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
                }
            }



            return blockListSynced;
                /*
                foreach (var blockHeight in listBlockHeightTarget.GetAll)
                {
                    int totalTaskToDo = peerListTarget.Count;
                    int totalTaskDone = 0;
                    int totalResponseOk = 0;

                    using (DisposableConcurrentDictionary<string, int> listOfRankedPeerPublicKeySaved = new DisposableConcurrentDictionary<string, int>())
                    {
                        using (DisposableConcurrentDictionary<string, ClassBlockObject> listBlockObjectsReceived = new DisposableConcurrentDictionary<string, ClassBlockObject>())
                        {
                            using (DisposableConcurrentDictionary<string, ConcurrentDictionary<bool, float>> listBlockObjectsReceivedVotes = new DisposableConcurrentDictionary<string, ConcurrentDictionary<bool, float>>())
                            { 


                                foreach (int i in peerListTarget.Keys)
                                {
                                    if (peerListTarget[i] == null)
                                    {
                                        totalTaskDone++;
                                        continue;
                                    }

                                    if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget))
                                    {
                                        totalTaskDone++;
                                        continue;
                                    }

                                    if (!_listPeerNetworkInformationStats.ContainsKey(peerListTarget[i].PeerIpTarget))
                                    {
                                        totalTaskDone++;
                                        continue;
                                    }


                                    if (!_listPeerNetworkInformationStats[peerListTarget[i].PeerIpTarget].ContainsKey(peerListTarget[i].PeerUniqueIdTarget))
                                    {
                                        totalTaskDone++;
                                        continue;
                                    }

                                    if (_listPeerNetworkInformationStats[peerListTarget[i].PeerIpTarget][peerListTarget[i].PeerUniqueIdTarget].LastBlockHeightUnlocked < blockHeight)
                                    {
                                        totalTaskDone++;
                                        continue;
                                    }

                                    int i1 = i;
                                    TaskManager.TaskManager.InsertTask(new Action(async () =>
                                    {
                                        if (peerListTarget.ContainsKey(i1))
                                        {
                                            CancellationTokenSource cancellationTokenSourceTaskSync = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token);

                                            Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>> result = await SendAskBlockData(peerListTarget[i1].PeerNetworkClientSyncObject, blockHeight, refuseLockedBlock, cancellationTokenSourceTaskSync);

                                            if (result != null)
                                            {
                                                if (result.Item1 && result.Item2 != null)
                                                {
                                                    if (result.Item2.ObjectReturned.BlockData.BlockHeight != blockHeight)
                                                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                    else
                                                    {
                                                        bool peerRanked = false;

                                                        if (_peerNetworkSettingObject.PeerEnableSovereignPeerVote)
                                                        {
                                                            if (CheckIfPeerIsRanked(peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, result.Item2.ObjectReturned, result.Item2.PacketNumericHash, result.Item2.PacketNumericSignature, cancellationTokenSourceTaskSync, out string numericPublicKeyOut))
                                                                peerRanked = !listOfRankedPeerPublicKeySaved.ContainsKey(numericPublicKeyOut) ? listOfRankedPeerPublicKeySaved.TryAdd(numericPublicKeyOut, 0) : false;
                                                        }

                                                        ClassBlockObject blockObject = result.Item2.ObjectReturned.BlockData;

                                                        #region Ensure to clean the block object received.

                                                        blockObject.BlockTransactionFullyConfirmed = false;
                                                        blockObject.BlockUnlockValid = false;
                                                        blockObject.BlockNetworkAmountConfirmations = 0;
                                                        blockObject.BlockSlowNetworkAmountConfirmations = 0;
                                                        blockObject.BlockLastHeightTransactionConfirmationDone = 0;
                                                        blockObject.BlockTotalTaskTransactionConfirmationDone = 0;
                                                        blockObject.BlockTransactionConfirmationCheckTaskDone = false;
                                                        blockObject?.BlockTransactions.Clear();

                                                        #endregion

                                                        string blockObjectHash = ClassUtility.GenerateSha256FromString(string.Concat("", ClassBlockUtility.BlockObjectToStringBlockData(blockObject, false).ToList()));

                                                        bool insertStatus = false;

                                                        if (!listBlockObjectsReceived.ContainsKey(blockObjectHash))
                                                            insertStatus = listBlockObjectsReceived.TryAdd(blockObjectHash, blockObject);
                                                        else
                                                            insertStatus = true;

                                                        if (insertStatus)
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
                                                        }
                                                    }

                                                }
                                            }

                                        }

                                        totalTaskDone++;

                                    }), 0, null);

                                }

                                while (totalTaskDone < totalTaskToDo)
                                    await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);


                                try
                                {
                                    if (totalResponseOk < _peerNetworkSettingObject.PeerMinAvailablePeerSync)
                                        return blockListSynced;

                                    ClassLog.WriteLine(listBlockObjectsReceived.Count + " different block(s) data received for sync the block height: " + blockHeight + ". Calculate votes..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                    using (DisposableDictionary<string, float> majorityVoteBlockSeed = new DisposableDictionary<string, float>())
                                    {
                                        using (DisposableDictionary<string, float> majorityVoteBlockNormal = new DisposableDictionary<string, float>())
                                        {

                                            float totalSeedVotes = 0;
                                            float totalNormalVotes = 0;

                                            #region Sorting votes done.

                                            foreach (var dictionaryVote in listBlockObjectsReceivedVotes.GetList)
                                            {
                                                if (!majorityVoteBlockSeed.ContainsKey(dictionaryVote.Key))
                                                    majorityVoteBlockSeed.Add(dictionaryVote.Key, 0);

                                                if (!majorityVoteBlockNormal.ContainsKey(dictionaryVote.Key))
                                                    majorityVoteBlockNormal.Add(dictionaryVote.Key, 0);

                                                if (dictionaryVote.Value.Count > 0)
                                                {
                                                    if (dictionaryVote.Value.ContainsKey(true))
                                                    {
                                                        majorityVoteBlockSeed[dictionaryVote.Key] += dictionaryVote.Value[true];
                                                        totalSeedVotes += dictionaryVote.Value[true];
                                                    }
                                                    if (dictionaryVote.Value.ContainsKey(false))
                                                    {
                                                        majorityVoteBlockNormal[dictionaryVote.Key] += dictionaryVote.Value[false];
                                                        totalNormalVotes += dictionaryVote.Value[false];
                                                    }
                                                }
                                            }

                                            string blockHashSeedSelected = majorityVoteBlockSeed.GetList.OrderByDescending(x => x.Value).First().Key;
                                            string blockHashNormSelected = majorityVoteBlockNormal.GetList.OrderByDescending(x => x.Value).First().Key;
                                            float blockHashSeedSelectedCountVote = majorityVoteBlockSeed.GetList.OrderByDescending(x => x.Value).First().Value;
                                            float blockHashNormSelectedCountVote = majorityVoteBlockNormal.GetList.OrderByDescending(x => x.Value).First().Value;

                                            // Calculate percent.
                                            if (totalSeedVotes < blockHashSeedSelectedCountVote)
                                                blockHashSeedSelectedCountVote = (blockHashSeedSelectedCountVote / totalSeedVotes) * 100f;
                                            else
                                            {
                                                // All seed agree together.
                                                blockHashSeedSelectedCountVote = 100f;
                                            }

                                            // Calculate percent.
                                            if (totalNormalVotes < blockHashNormSelectedCountVote)
                                                blockHashNormSelectedCountVote = (blockHashNormSelectedCountVote / totalNormalVotes) * 100f;
                                            else
                                            {
                                                // All noral peer agree together.
                                                blockHashNormSelectedCountVote = 100f;
                                            }

                                            #endregion

                                            #region Select the hash most voted.

                                            // Perfect equality.
                                            if (blockHashNormSelected == blockHashSeedSelected)
                                                blockListSynced.Add(listBlockObjectsReceived[blockHashSeedSelected].BlockHeight, listBlockObjectsReceived[blockHashSeedSelected]);
                                            else
                                            {
                                                // Seed select.
                                                if (blockHashSeedSelectedCountVote > blockHashNormSelectedCountVote)
                                                    blockListSynced.Add(listBlockObjectsReceived[blockHashSeedSelected].BlockHeight, listBlockObjectsReceived[blockHashSeedSelected]);
                                                // Normal select.
                                                else
                                                    blockListSynced.Add(listBlockObjectsReceived[blockHashNormSelected].BlockHeight, listBlockObjectsReceived[blockHashNormSelected]);
                                            }

                                            #endregion

                                            ClassLog.WriteLine("Block height: " + blockHeight + " data retrieved from peers successfully, continue the sync..", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                        }
                                    }
                                }
                                catch (Exception error)
                                {
                                    ClassLog.WriteLine("Error on trying to sync a block metadata from peers. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
    #if DEBUG
                                    Debug.WriteLine("Error on trying to sync a block metadata from peers. Exception: " + error.Message);
    #endif
                                }
                            }
                        }
                    }
                }

                return blockListSynced;*/
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
                                if (peerListTarget[i] == null)
                                {
                                    totalResponseOk++;
                                    continue;
                                }

                                if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget))
                                {
                                    totalResponseOk++;
                                    continue;
                                }

                                if (!_listPeerNetworkInformationStats.ContainsKey(peerListTarget[i].PeerIpTarget))
                                {
                                    totalResponseOk++;
                                    continue;
                                }

                                if (!_listPeerNetworkInformationStats[peerListTarget[i].PeerIpTarget].ContainsKey(peerListTarget[i].PeerUniqueIdTarget))
                                {
                                    totalResponseOk++;
                                    continue;
                                }

                                if (_listPeerNetworkInformationStats[peerListTarget[i].PeerIpTarget][peerListTarget[i].PeerUniqueIdTarget].LastBlockHeightUnlocked < blockHeightTarget)
                                {
                                    totalResponseOk++;
                                    continue;
                                }


                                var i1 = i;

                                TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {
                                    if (peerListTarget.ContainsKey(i1))
                                    {
                                        try
                                        {
                                            CancellationTokenSource cancellationTokenSourceTaskSync = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token);

                                            var result = await SendAskBlockTransactionData(peerListTarget[i1].PeerNetworkClientSyncObject, blockHeightTarget, transactionId, cancellationTokenSourceTaskSync);

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
                                                                    if (CheckIfPeerIsRanked(peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, result.Item2.ObjectReturned, result.Item2.PacketNumericHash, result.Item2.PacketNumericSignature, cancellationTokenSourceTaskSync, out string numericPublicKeyOut))
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
                                    }

                                    totalTaskDone++;

                                }), 0, null);
                            }


                            while (totalTaskDone < totalTaskToDo)
                                await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);


                            try
                            {
                                if (totalResponseOk < _peerNetworkSettingObject.PeerMinAvailablePeerSync)
                                {
                                    ClassLog.WriteLine("Not enough peers response for accept block transaction data synced.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    return null;
                                }

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
        private async Task<SortedDictionary<string, ClassTransactionObject>> StartAskBlockTransactionObjectByRangeFromListPeerTarget(Dictionary<int, ClassPeerTargetObject> peerListTarget, long blockHeightTarget, int transactionIdStart, int transactionIdEnd, DisposableDictionary<string, string> listWalletAndPublicKeys)
        {
            using (DisposableConcurrentDictionary<string, SortedDictionary<string, ClassTransactionObject>> listTransactionObjects = new DisposableConcurrentDictionary<string, SortedDictionary<string, ClassTransactionObject>>())
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
                                if (peerListTarget[i] == null)
                                {
                                    totalTaskDone++;
                                    continue;
                                }

                                if (!_listPeerNetworkInformationStats.ContainsKey(peerListTarget[i].PeerIpTarget))
                                {
                                    totalTaskDone++;
                                    continue;
                                }


                                if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget))
                                {
                                    totalTaskDone++;
                                    continue;
                                }

                                if (!_listPeerNetworkInformationStats[peerListTarget[i].PeerIpTarget].ContainsKey(peerListTarget[i].PeerUniqueIdTarget))
                                {
                                    totalTaskDone++;
                                    continue;
                                }

                                if (_listPeerNetworkInformationStats[peerListTarget[i].PeerIpTarget][peerListTarget[i].PeerUniqueIdTarget].LastBlockHeightUnlocked < blockHeightTarget)
                                {
                                    totalTaskDone++;
                                    continue;
                                }

                                TaskManager.TaskManager.InsertTask(new Action(async () =>
                                {


                                    if (peerListTarget.ContainsKey(i))
                                    {
                                        try
                                        {
                                            CancellationTokenSource cancellationTokenSourceTaskSync = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token);

                                            var result = await SendAskBlockTransactionDataByRange(peerListTarget[i].PeerNetworkClientSyncObject, blockHeightTarget, transactionIdStart, transactionIdEnd, listWalletAndPublicKeys, cancellationTokenSourceTaskSync);

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
                                                                    if (CheckIfPeerIsRanked(peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget, result.Item2.ObjectReturned, result.Item2.PacketNumericHash, result.Item2.PacketNumericSignature, cancellationTokenSourceTaskSync, out string numericPublicKeyOut))
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
                                    }
                                    totalTaskDone++;

                                }), 0, null);

                            }

                            while (totalTaskDone < totalTaskToDo)
                                await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);

                            try
                            {
                                if (totalResponseOk < _peerNetworkSettingObject.PeerMinAvailablePeerSync)
                                {
                                    ClassLog.WriteLine("Not enough peers response for accept block transaction data synced.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    return null;
                                }

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
                            if (peerListTarget[i] == null)
                            {
                                totalTaskDone++;
                                continue;
                            }

                            if (!ClassPeerCheckManager.CheckPeerClientInitializationStatus(peerListTarget[i].PeerIpTarget, peerListTarget[i].PeerUniqueIdTarget))
                            {
                                totalTaskDone++;
                                continue;
                            }

                            if (!_listPeerNetworkInformationStats.ContainsKey(peerListTarget[i].PeerIpTarget))
                            {
                                totalTaskDone++;
                                continue;
                            }

                            if (!_listPeerNetworkInformationStats[peerListTarget[i].PeerIpTarget].ContainsKey(peerListTarget[i].PeerUniqueIdTarget))
                            {
                                totalTaskDone++;
                                continue;
                            }

                            if (_listPeerNetworkInformationStats[peerListTarget[i].PeerIpTarget][peerListTarget[i].PeerUniqueIdTarget].LastBlockHeightUnlocked < blockHeightTarget)
                            {
                                totalTaskDone++;
                                continue;
                            }

                            var i1 = i;
                            TaskManager.TaskManager.InsertTask(new Action(async () =>
                            {
                                try
                                {
                                    CancellationTokenSource cancellationTokenSourceTaskSync = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token);

                                    var result = await SendAskBlockData(peerListTarget[i1].PeerNetworkClientSyncObject, blockHeightTarget, true, cancellationTokenSourceTaskSync);

                                    if (result != null)
                                    {
                                        if (result.Item1)
                                        {
                                            if (result.Item2 != null)
                                            {
                                                if (result.Item2.ObjectReturned.BlockData.BlockHeight != blockHeightTarget)
                                                    ClassPeerCheckManager.InputPeerClientInvalidPacket(peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                                else
                                                {
                                                    bool peerRanked = false;
                                                    if (_peerNetworkSettingObject.PeerEnableSovereignPeerVote)
                                                    {
                                                        if (CheckIfPeerIsRanked(peerListTarget[i1].PeerIpTarget, peerListTarget[i1].PeerUniqueIdTarget, result.Item2.ObjectReturned, result.Item2.PacketNumericHash, result.Item2.PacketNumericSignature, cancellationTokenSourceTaskSync, out string numericPublicKeyOut))
                                                            peerRanked = !listOfRankedPeerPublicKeySaved.ContainsKey(numericPublicKeyOut) ? listOfRankedPeerPublicKeySaved.TryAdd(numericPublicKeyOut, 0) : false;
                                                    }

                                                    bool comparedShares = false;

                                                    if (blockObject.BlockHeight == BlockchainSetting.GenesisBlockHeight)
                                                    {
                                                        if (result.Item2.ObjectReturned.BlockData.BlockMiningPowShareUnlockObject == null && blockObject.BlockMiningPowShareUnlockObject == null)
                                                            comparedShares = true;
                                                    }
                                                    else
                                                        comparedShares = ClassMiningPoWaCUtility.ComparePoWaCShare(result.Item2.ObjectReturned.BlockData.BlockMiningPowShareUnlockObject, blockObject.BlockMiningPowShareUnlockObject);

                                                    if (!comparedShares)
                                                    {
                                                        if (result.Item2.ObjectReturned.BlockData.BlockStatus == ClassBlockEnumStatus.LOCKED && blockObject.BlockStatus == ClassBlockEnumStatus.LOCKED
                                                            && result.Item2.ObjectReturned.BlockData.BlockMiningPowShareUnlockObject == null && blockObject.BlockMiningPowShareUnlockObject == null)
                                                            comparedShares = true;
                                                    }

                                                    bool isEqual = false;
                                                    if (result.Item2.ObjectReturned.BlockData.BlockHeight == blockObject.BlockHeight &&
                                                        result.Item2.ObjectReturned.BlockData.BlockHash == blockObject.BlockHash &&
                                                        result.Item2.ObjectReturned.BlockData.TimestampFound == blockObject.TimestampFound &&
                                                        result.Item2.ObjectReturned.BlockData.TimestampCreate == blockObject.TimestampCreate &&
                                                        result.Item2.ObjectReturned.BlockData.BlockStatus == blockObject.BlockStatus &&
                                                        result.Item2.ObjectReturned.BlockData.BlockDifficulty == blockObject.BlockDifficulty &&
                                                        result.Item2.ObjectReturned.BlockData.BlockFinalHashTransaction == blockObject.BlockFinalHashTransaction &&
                                                        comparedShares &&
                                                        result.Item2.ObjectReturned.BlockData.BlockWalletAddressWinner == blockObject.BlockWalletAddressWinner)
                                                    {
                                                        isEqual = true;
                                                    }
#if DEBUG
                                                    else
                                                    {
                                                        Debug.WriteLine("Block height: " + blockObject.BlockHeight + " is invalid for peer: " + peerListTarget[i1].PeerIpTarget);
                                                        Debug.WriteLine("External: Height: " + result.Item2.ObjectReturned.BlockData.BlockHeight +
                                                                        Environment.NewLine + "Hash: " + result.Item2.ObjectReturned.BlockData.BlockHash +
                                                                        Environment.NewLine + "Timestamp create: " + result.Item2.ObjectReturned.BlockData.TimestampCreate +
                                                                        Environment.NewLine + "Timestamp found: " + result.Item2.ObjectReturned.BlockData.TimestampFound +
                                                                        Environment.NewLine + "Block status: " + result.Item2.ObjectReturned.BlockData.BlockStatus +
                                                                        Environment.NewLine + "Block Difficulty: " + result.Item2.ObjectReturned.BlockData.BlockDifficulty +
                                                                        Environment.NewLine + "Block final transaction hash: " + result.Item2.ObjectReturned.BlockData.BlockFinalHashTransaction +
                                                                        Environment.NewLine + "Block Mining pow share: " + ClassUtility.SerializeData(result.Item2.ObjectReturned.BlockData.BlockMiningPowShareUnlockObject) +
                                                                        Environment.NewLine + "Block wallet address winner: " + result.Item2.ObjectReturned.BlockData.BlockWalletAddressWinner);

                                                        Debug.WriteLine("Internal: Height: " + blockObject.BlockHeight +
                                                                        Environment.NewLine + "Hash: " + blockObject.BlockHash +
                                                                        Environment.NewLine + "Timestamp create: " + blockObject.TimestampCreate +
                                                                        Environment.NewLine + "Timestamp found: " + blockObject.TimestampFound +
                                                                        Environment.NewLine + "Block status: " + blockObject.BlockStatus +
                                                                        Environment.NewLine + "Block Difficulty: " + blockObject.BlockDifficulty +
                                                                        Environment.NewLine + "Block final transaction hash: " + blockObject.BlockFinalHashTransaction +
                                                                        Environment.NewLine + "Block Mining pow share: " + ClassUtility.SerializeData(blockObject.BlockMiningPowShareUnlockObject) +
                                                                        Environment.NewLine + "Block wallet address winner: " + blockObject.BlockWalletAddressWinner);
                                                    }
#endif

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

                                                    totalResponseOk++;
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

                            }), 0, null);

                        }

                        while (totalTaskDone < totalTaskToDo)
                            await Task.Delay(_peerNetworkSettingObject.PeerTaskSyncDelay);
                        


                        try
                        {
                            if (totalResponseOk < _peerNetworkSettingObject.PeerMinAvailablePeerSync)
                                return ClassPeerNetworkSyncServiceEnumCheckBlockDataUnlockedResult.NO_CONSENSUS_FOUND;

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
                        catch (Exception error)
                        {
                            ClassLog.WriteLine("Error on trying to check a block with other peers. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
#if DEBUG
                            Debug.WriteLine("Error on trying to check a block with other peers. Exception: " + error.Message);
#endif
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
            #region Initialize peer target informations.

            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            bool targetExist = false;
            if (!ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(peerIp))
                ClassPeerDatabase.DictionaryPeerDataObject.Add(peerIp, new ConcurrentDictionary<string, ClassPeerObject>());
            else
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId))
                    targetExist = true;
            }

            ClassPeerObject peerObject = null;

            if (targetExist)
            {
                if (await ClassPeerKeysManager.UpdatePeerInternalKeys(peerIp, peerPort, peerUniqueId, cancellation, _peerNetworkSettingObject, forceUpdate))
                    peerObject = ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId];
            }
            else
            {
                peerObject = ClassPeerKeysManager.GeneratePeerObject(peerIp, peerPort, peerUniqueId, cancellation);
                peerObject.PeerLastPacketReceivedTimestamp = TaskManager.TaskManager.CurrentTimestampSecond + _peerNetworkSettingObject.PeerMaxDelayKeepAliveStats;
                if (!ClassPeerDatabase.DictionaryPeerDataObject[peerIp].TryAdd(peerUniqueId, peerObject))
                    return false;

                targetExist = true;
            }

            if (peerObject == null)
                return false;

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

            var packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject.GetPacketData(), peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_PEER_AUTH_KEYS, true, false);

            if (!packetSendStatus)
            {
                ClassLog.WriteLine(peerIp + ":" + peerPort + " try to retrieve auth keys failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                return false;
            }


            #region Handle packet received.

            if (peerNetworkClientSyncObject.PeerPacketReceived == null)
            {
                ClassLog.WriteLine(peerIp + ":" + peerPort + " packet is empty.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
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
                            ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);

                        return false;
                    }

                    peerUniqueId = peerNetworkClientSyncObject.PeerPacketReceived.PacketPeerUniqueId;

                    targetExist = ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId);

                    if (!targetExist)
                    {
                        peerObject.PeerUniqueId = peerUniqueId;

                        if (!ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(peerIp))
                            ClassPeerDatabase.DictionaryPeerDataObject.Add(peerIp, new ConcurrentDictionary<string, ClassPeerObject>());

                        if (!ClassPeerDatabase.DictionaryPeerDataObject[peerIp].TryAdd(peerUniqueId, peerObject))
                            return ClassPeerDatabase.DictionaryPeerDataObject[peerIp].TryAdd(peerUniqueId, peerObject);

                        targetExist = true;
                    }

                    if (await ClassPeerKeysManager.UpdatePeerKeysReceiveTaskSync(peerIp, peerNetworkClientSyncObject.PeerPacketReceived.PacketPeerUniqueId, peerPacketSendPeerAuthKeys, cancellation, _peerNetworkSettingObject))
                    {

                        ClassPeerCheckManager.CleanPeerState(peerIp, peerUniqueId, true);
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIsPublic = true;
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus = ClassPeerEnumStatus.PEER_ALIVE;

                        ClassLog.WriteLine(peerIp + ":" + peerPort + " send propertly auth keys.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientValidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject);
                        return true;
                    }
                    else
                    {
                        ClassPeerCheckManager.SetPeerDeadState(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " failed to update auth keys.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        return false;
                    }
                }
                catch (Exception error)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " exception from packet received: " + error.Message + ", on receiving auth keys, increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                    if (targetExist)
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);

                    return false;
                }

            }

            #endregion

            ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
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
            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_PEER_LIST,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskPeerList()
                {
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };

            sendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(sendObject, peerIp, peerUniqueId, false, _peerNetworkSettingObject, cancellation);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject.GetPacketData(), peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_PEER_LIST, true, false);

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

                if (!TryGetPacketPeerList(peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, cancellation, out ClassPeerPacketSendPeerList packetPeerList))
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " can't handle peer packet received. Increment invalid packets", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
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
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                        continue;
                    }

                    if (packetPeerList.PeerPortList[i] < _peerNetworkSettingObject.PeerMinPort || packetPeerList.PeerPortList[i] > _peerNetworkSettingObject.PeerMaxPort)
                    {
                        ClassLog.WriteLine("Can't register peer: " + packetPeerList.PeerIpList[i] + ":" + packetPeerList.PeerPortList[i] + " because the P2P port is not valid.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                        continue;
                    }

                    if (packetPeerList.PeerUniqueIdList[i].IsNullOrEmpty(false, out _) ||
                        !ClassUtility.CheckHexStringFormat(packetPeerList.PeerUniqueIdList[i]) ||
                        packetPeerList.PeerUniqueIdList[i].Length != BlockchainSetting.PeerUniqueIdHashLength)
                    {
                        ClassLog.WriteLine("Can't register peer: " + packetPeerList.PeerIpList[i] + " because the unique id: \n" + packetPeerList.PeerUniqueIdList[i] + " is not valid.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                        continue;
                    }


                    if (ClassPeerCheckManager.CheckPeerClientStatus(packetPeerList.PeerIpList[i], packetPeerList.PeerUniqueIdList[i], false, _peerNetworkSettingObject, _peerFirewallSettingObject))
                        continue;

                    if (await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(packetPeerList.PeerIpList[i], packetPeerList.PeerPortList[i], packetPeerList.PeerUniqueIdList[i], _peerNetworkSettingObject, _peerFirewallSettingObject), CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenServiceSync.Token, new CancellationTokenSource((_peerNetworkSettingObject.PeerMaxDelayAwaitResponse * 1000)).Token), true))
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
            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_LIST_SOVEREIGN_UPDATE,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskListSovereignUpdate()
                {
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };

            sendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(sendObject, peerIp, peerUniqueId, true, _peerNetworkSettingObject, cancellation);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject.GetPacketData(), peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_LIST_SOVEREIGN_UPDATE, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " retrieve sovereign update list failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);

                    return new Tuple<bool, List<string>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, List<string>>(false, null);

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_LIST_SOVEREIGN_UPDATE)
                {
                    if (!TryGetPacketSovereignUpdateList(peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, cancellation, out ClassPeerPacketSendListSovereignUpdate packetPeerSovereignUpdateList))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " invalid sovereign update list packet received. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                        return new Tuple<bool, List<string>>(false, null);
                    }

                    ClassLog.WriteLine(peerIp + ":" + peerPort + " packet return " + packetPeerSovereignUpdateList.SovereignUpdateHashList.Count + " sovereign update hash.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
                    return new Tuple<bool, List<string>>(true, packetPeerSovereignUpdateList.SovereignUpdateHashList);
                }

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE ||
                peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_ENCRYPTION)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, List<string>>(false, null);
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
            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_SOVEREIGN_UPDATE_FROM_HASH,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskSovereignUpdateFromHash()
                {
                    SovereignUpdateHash = sovereignHash,
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };

            sendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(sendObject, peerIp, peerUniqueId, false, _peerNetworkSettingObject, cancellation);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject.GetPacketData(), peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_SOVEREIGN_UPDATE_FROM_HASH, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask sovereign update data failed. Hash: " + sovereignHash, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(false, null);

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_SOVEREIGN_UPDATE_FROM_HASH)
                {

                    if (!TryGetPacketSovereignUpdateData(peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, cancellation, out ClassPeerPacketSendSovereignUpdateFromHash packetSovereignUpdateData))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " a packet sovereign update data received is invalid. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject);

                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(true, new ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>()
                    {
                        ObjectReturned = packetSovereignUpdateData.SovereignUpdateObject,
                        PacketNumericHash = packetSovereignUpdateData.PacketNumericHash,
                        PacketNumericSignature = packetSovereignUpdateData.PacketNumericSignature
                    });
                }

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE ||
                peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_ENCRYPTION)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassSovereignUpdateObject>>(false, null);
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
            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;


            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_NETWORK_INFORMATION,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskPeerList()
                {
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };

            sendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(sendObject, peerIp, peerUniqueId, false, _peerNetworkSettingObject, cancellation);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject.GetPacketData(), peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_NETWORK_INFORMATION, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask network information failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);

                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_NETWORK_INFORMATION)
                {

                    if (!TryGetPacketNetworkInformation(peerNetworkClientSyncObject, peerIp, peerPort, _peerNetworkSettingObject, cancellation, out ClassPeerPacketSendNetworkInformation peerPacketNetworkInformation))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + "  can't retrieve packet network information from packet received. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject);

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

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE ||
                    peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_ENCRYPTION)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enoguth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);
                }

                ClassLog.WriteLine("Packet received type not expected: " + peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder + " received.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(await HandleUnexpectedPacketOrder(peerIp, peerPort, peerUniqueId, peerNetworkClientSyncObject.PeerPacketReceived, cancellation), null);
            }

            ClassLog.WriteLine("Packet build to send is empty and cannot be sent.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY);
            return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendNetworkInformation>>(false, null);
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
            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject;
            try
            {
                sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
                {
                    PacketOrder = ClassPeerEnumPacketSend.ASK_BLOCK_DATA,
                    PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskBlockData()
                    {
                        BlockHeight = blockHeightTarget,
                        PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                    })
                };
            }
            catch
            {
                return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);
            }


            sendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(sendObject, peerIp, peerUniqueId, false, _peerNetworkSettingObject, cancellation);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject.GetPacketData(), peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_BLOCK_DATA, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask block data " + blockHeightTarget + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_BLOCK_DATA)
                {
                    if (!TryGetPacketBlockData(peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, blockHeightTarget, refuseLockedBlock, cancellation, out ClassPeerPacketSendBlockData packetSendBlockData))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " invalid block data received. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject);

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

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE ||
                    peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_ENCRYPTION)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enoguth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockData>>(false, null);
                }

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
            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = ClassPeerEnumPacketSend.ASK_BLOCK_TRANSACTION_DATA,
                PacketContent = ClassUtility.SerializeData(new ClassPeerPacketSendAskBlockTransactionData()
                {
                    BlockHeight = blockHeightTarget,
                    TransactionId = transactionId,
                    PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                })
            };


            sendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(sendObject, peerIp, peerUniqueId, false, _peerNetworkSettingObject, cancellation);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject.GetPacketData(), peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask block transaction from block height: " + blockHeightTarget + " id: " + transactionId + " failed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA)
                {

                    if (!TryGetPacketBlockTransactionData(peerNetworkClientSyncObject, peerIp, _peerNetworkSettingObject, blockHeightTarget, cancellation, out ClassPeerPacketSendBlockTransactionData packetSendBlockTransactionData))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " send an invalid block transaction data. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject);

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

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE ||
                 peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_ENCRYPTION)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enoguth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionData>>(false, null);
                }

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
            string peerIp = peerNetworkClientSyncObject.PeerIpTarget;
            int peerPort = peerNetworkClientSyncObject.PeerPortTarget;
            string peerUniqueId = peerNetworkClientSyncObject.PeerUniqueIdTarget;

            ClassPeerPacketSendObject sendObject = new ClassPeerPacketSendObject(_peerNetworkSettingObject.PeerUniqueId, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist)
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


            sendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(sendObject, peerIp, peerUniqueId, false, _peerNetworkSettingObject, cancellation);

            if (sendObject != null)
            {
                bool packetSendStatus = await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(sendObject.GetPacketData(), peerPort, peerUniqueId, cancellation, ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA_BY_RANGE, true, false);

                if (!packetSendStatus)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " ask block transaction from height: " + blockHeightTarget + " range: " + transactionIdRangeStart + "/" + transactionIdRangeEnd + ".", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_BLOCK_TRANSACTION_DATA_BY_RANGE)
                {

                    if (!TryGetPacketBlockTransactionDataByRange(peerNetworkClientSyncObject, peerIp, listWalletAndPublicKeys, _peerNetworkSettingObject, blockHeightTarget, cancellation, out ClassPeerPacketSendBlockTransactionDataByRange packetSendBlockTransactionDataByRange))
                    {
                        ClassLog.WriteLine(peerIp + ":" + peerPort + " send an invalid block transaction data. Increment invalid packets.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                        return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);
                    }

                    ClassPeerCheckManager.InputPeerClientValidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject);

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

                if (peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_SIGNATURE ||
                    peerNetworkClientSyncObject.PeerPacketTypeReceived == ClassPeerEnumPacketResponse.INVALID_PEER_PACKET_ENCRYPTION)
                {
                    await SendAskAuthPeerKeys(peerNetworkClientSyncObject, cancellation, true);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);
                }

                if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.NOT_YET_SYNCED)
                {
                    ClassLog.WriteLine(peerIp + ":" + peerPort + " is not enoguth synced yet.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return new Tuple<bool, ClassPeerSyncPacketObjectReturned<ClassPeerPacketSendBlockTransactionDataByRange>>(false, null);
                }

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

                            SortedDictionary<string, ClassTransactionObject> transactionObjectByRange = await StartAskBlockTransactionObjectByRangeFromListPeerTarget(peerTargetList, blockObject.BlockHeight, startRange, endRange, listWalletAndPublicKeys);


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
                                    break;

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

                        SortedDictionary<string, ClassTransactionObject> transactionObjectByRange = await StartAskBlockTransactionObjectByRangeFromListPeerTarget(peerTargetList, blockObject.BlockHeight, startRange, endRange, listWalletAndPublicKeys);


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
                                break;

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
                            break;

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
                                        break;

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
                bool exist = ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId);


                if (exist)
                {
                    if (!ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].OnUpdateAuthKeys)
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
                                        if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus != ClassPeerEnumStatus.PEER_ALIVE)
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
                                        if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus != ClassPeerEnumStatus.PEER_ALIVE)
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
                                ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIp, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject);
                                break;
                        }
                    }
                }

                if (doPeerKeysUpdate)
                {
                    if (exist)
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].OnUpdateAuthKeys = true;

                    if (await SendAskAuthPeerKeys(new ClassPeerNetworkClientSyncObject(peerIp, peerPort, peerUniqueId, _peerNetworkSettingObject, _peerFirewallSettingObject), cancellation, forceUpdate))
                    {
                        ClassLog.WriteLine("Auth keys generated successfully sent, peer target auth keys successfully received and updated.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);
                        if (ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
                        {
                            ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastUpdateOfKeysTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;
                            result = true;
                        }
                    }
                    else
                        ClassLog.WriteLine("Auth keys generated can't be sent to the peer target.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_HIGH_PRIORITY);

                    if (exist)
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].OnUpdateAuthKeys = false;
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
            return ClassPeerNetworkBroadcastFunction.GetRandomListPeerTargetAlive(_peerNetworkSettingObject.ListenIp, PeerOpenNatServerIp, string.Empty, peerTargetList, _peerNetworkSettingObject, _peerFirewallSettingObject, _cancellationTokenServiceSync);
        }

        /// <summary>
        /// Clear the peer list target propertly.
        /// </summary>
        /// <param name="peerTargetList"></param>
        /// <returns></returns>
        private void ClearPeerTargetList(Dictionary<int, ClassPeerTargetObject> peerTargetList)
        {
            foreach (int peerKey in peerTargetList.Keys.ToArray())
            {
                try
                {
                    if (!peerTargetList[peerKey].PeerNetworkClientSyncObject.PeerConnectStatus)
                    {
                        peerTargetList[peerKey].PeerNetworkClientSyncObject.DisconnectFromTarget();

                        if (!ClassPeerCheckManager.CheckPeerClientStatus(peerTargetList[peerKey].PeerIpTarget, peerTargetList[peerKey].PeerUniqueIdTarget, false, _peerNetworkSettingObject, _peerFirewallSettingObject))
                        {
                            peerTargetList[peerKey].PeerNetworkClientSyncObject.Dispose();
                            peerTargetList.Remove(peerKey);
                        }
                    }
                }
                catch
                {
                    // Ignored.
                }
            }
        }

        #endregion

    }
}