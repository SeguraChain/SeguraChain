using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Instance.Node.Network.Database.Manager;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Status;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.ClientConnect.Object;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Request;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response.Enum;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast
{
    public class ClassPeerNetworkBroadcastFunction
    {
        #region Generate a random list of peers to target.

        /// <summary>
        /// Generate a list of random alive peers excepting the peer server and another peer.
        /// </summary>
        /// <param name="peerServerIp"></param>
        /// <param name="peerOpenNatServerIp"></param>
        /// <param name="peerToExcept"></param>
        /// <param name="previousListPeerSelected"></param>
        /// <param name="peerNetworkSetting"></param>
        /// <param name="peerFirewallSettingObject"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static Dictionary<int, ClassPeerTargetObject> GetRandomListPeerTargetAlive(string peerServerIp, string peerOpenNatServerIp, string peerToExcept, Dictionary<int, ClassPeerTargetObject> previousListPeerSelected, ClassPeerNetworkSettingObject peerNetworkSetting, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation)
        {
            if (previousListPeerSelected == null)
                previousListPeerSelected = new Dictionary<int, ClassPeerTargetObject>();

            Dictionary<int, ClassPeerTargetObject> newListSelected = new Dictionary<int, ClassPeerTargetObject>();

            if (previousListPeerSelected.Count > 0)
            {
                foreach (int peerIndex in previousListPeerSelected.Keys.ToArray())
                {
                    cancellation?.Token.ThrowIfCancellationRequested();
                    bool removePeerConnection = false;
                    string peerIp = previousListPeerSelected[peerIndex].PeerIpTarget;
                    if (peerIp != peerToExcept && peerIp != peerServerIp && peerIp != peerOpenNatServerIp)
                    {
                        string peerUniqueId = previousListPeerSelected[peerIndex].PeerUniqueIdTarget;

                        if (!ClassPeerCheckManager.CheckPeerClientStatus(peerIp, peerUniqueId, false, peerNetworkSetting, peerFirewallSettingObject))
                            removePeerConnection = true;
                    }
                    else
                        removePeerConnection = true;

                    if (removePeerConnection)
                    {
                        try
                        {
                            if (previousListPeerSelected[peerIndex].PeerNetworkClientSyncObject != null)
                            {
                                previousListPeerSelected[peerIndex].PeerNetworkClientSyncObject.DisconnectFromTarget();
                                previousListPeerSelected[peerIndex].PeerNetworkClientSyncObject.Dispose();
                            }
                        }
                        catch
                        {
                            // Ignored.
                        }
                        previousListPeerSelected.Remove(peerIndex);
                    }
                    else
                    {
                        try
                        {
                            if (previousListPeerSelected[peerIndex].PeerNetworkClientSyncObject != null)
                            {
                                if (!previousListPeerSelected[peerIndex].PeerNetworkClientSyncObject.PeerConnectStatus ||
                                    !previousListPeerSelected[peerIndex].PeerNetworkClientSyncObject.PeerPacketReceivedStatus)
                                {
                                    try
                                    {
                                        previousListPeerSelected[peerIndex].PeerNetworkClientSyncObject.DisconnectFromTarget();
                                        previousListPeerSelected[peerIndex].PeerNetworkClientSyncObject.Dispose();
                                    }
                                    catch
                                    {
                                        // Ignored.
                                    }
                                    previousListPeerSelected.Remove(peerIndex);
                                }

                            }
                        }
                        catch
                        {
                            // Ignored.
                        }
                    }

                }
            }

            if (ClassPeerDatabase.DictionaryPeerDataObject.Count > 0)
            {
                Dictionary<string, string> listPublicPeer = new Dictionary<string, string>(); // Peer ip | Peer unique id.

                foreach (string peerIp in ClassPeerDatabase.DictionaryPeerDataObject.Keys.ToArray())
                {
                    cancellation?.Token.ThrowIfCancellationRequested();
                    if (peerIp != peerToExcept && peerIp != peerServerIp && peerIp != peerOpenNatServerIp)
                    {
                        if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].Count > 0 && peerIp != peerToExcept)
                        {
                            foreach (string peerUniqueId in ClassPeerDatabase.DictionaryPeerDataObject[peerIp].Keys.ToArray())
                            {
                                cancellation?.Token.ThrowIfCancellationRequested();
                                if (!peerUniqueId.IsNullOrEmpty(false, out _))
                                {
                                    if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIsPublic && !ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].OnUpdateAuthKeys)
                                    {
                                        if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus == ClassPeerEnumStatus.PEER_ALIVE)
                                        {
                                            if (ClassPeerCheckManager.CheckPeerClientStatus(peerIp, peerUniqueId, false, peerNetworkSetting, peerFirewallSettingObject))
                                            {
                                                if (!listPublicPeer.ContainsKey(peerIp))
                                                {
                                                    if (previousListPeerSelected.Count(x => x.Value.PeerUniqueIdTarget == peerUniqueId) == 0)
                                                        listPublicPeer.Add(peerIp, peerUniqueId);
                                                }
                                                else
                                                {
                                                    if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastValidPacket > ClassPeerDatabase.DictionaryPeerDataObject[peerIp][listPublicPeer[peerIp]].PeerLastValidPacket)
                                                        listPublicPeer[peerIp] = peerUniqueId;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (listPublicPeer.Count >= BlockchainSetting.MaxPeerPerSyncTask)
                        break;
                }


                int indexPeer = 0;
                if (previousListPeerSelected.Count > 0)
                {
                    foreach (var peerIndex in previousListPeerSelected.Keys)
                    {
                        cancellation?.Token.ThrowIfCancellationRequested();
                        while (newListSelected.ContainsKey(indexPeer))
                            indexPeer++;

                        newListSelected.Add(indexPeer, previousListPeerSelected[peerIndex]);
                        indexPeer++;
                    }
                }

                if (listPublicPeer.Count > 0)
                {

                    foreach (var peer in listPublicPeer)
                    {
                        cancellation?.Token.ThrowIfCancellationRequested();

                        while (newListSelected.ContainsKey(indexPeer))
                            indexPeer++;

                        newListSelected.Add(indexPeer, new ClassPeerTargetObject()
                        {
                            PeerNetworkClientSyncObject = new ClassPeerNetworkClientSyncObject(peer.Key, ClassPeerDatabase.DictionaryPeerDataObject[peer.Key][peer.Value].PeerPort, peer.Value, cancellation, peerNetworkSetting, peerFirewallSettingObject)
                        });

                        indexPeer++;
                    }
                }
            }

            return newListSelected;
        }

        #endregion

        #region Mining Broadcast.

        /// <summary>
        /// Broadcast mining share accepted to other peers.
        /// </summary>
        /// <param name="peerOpenNatServerIp"></param>
        /// <param name="peerToExcept"></param>
        /// <param name="miningPowShareObject"></param>
        /// <param name="peerServerIp"></param>
        /// <param name="peerNetworkSetting"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public static void BroadcastMiningShareAsync(string peerServerIp, string peerOpenNatServerIp, string peerToExcept, ClassMiningPoWaCShareObject miningPowShareObject, ClassPeerNetworkSettingObject peerNetworkSetting, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            TaskManager.TaskManager.InsertTask(new Action(() =>
            {

                foreach (var peerValuePair in GetRandomListPeerTargetAlive(peerServerIp, peerOpenNatServerIp, peerToExcept, null, peerNetworkSetting, peerFirewallSettingObject, new CancellationTokenSource()))
                {

                    TaskManager.TaskManager.InsertTask(new Action(async () =>
                    {
                        try
                        {
                            string peerIpTarget = peerValuePair.Value.PeerIpTarget;
                            string peerUniqueIdTarget = peerValuePair.Value.PeerUniqueIdTarget;


                            ClassPeerPacketSendMiningShareVote peerPacketSendMiningShareVote = null;

                            while (peerPacketSendMiningShareVote == null)
                            {
                                if (!ClassPeerCheckManager.CheckPeerClientStatus(peerIpTarget, peerUniqueIdTarget, false, peerNetworkSetting, peerFirewallSettingObject))
                                    break;

                                peerPacketSendMiningShareVote = await ClassPeerNetworkBroadcastShortcutFunction.SendBroadcastPacket<ClassPeerPacketSendAskMiningShareVote, ClassPeerPacketSendMiningShareVote>(
                                                                                                        peerValuePair.Value.PeerNetworkClientSyncObject,
                                                                                                        ClassPeerEnumPacketSend.ASK_MINING_SHARE_VOTE,
                                                                                                        new ClassPeerPacketSendAskMiningShareVote()
                                                                                                        {
                                                                                                            BlockHeight = miningPowShareObject.BlockHeight,
                                                                                                            MiningPowShareObject = miningPowShareObject,
                                                                                                            PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
                                                                                                        }, peerIpTarget, peerUniqueIdTarget, peerNetworkSetting, ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE, new CancellationTokenSource());
                                if (peerPacketSendMiningShareVote != null)
                                {
                                    peerValuePair.Value.PeerNetworkClientSyncObject.Dispose();
                                    break;
                                }

                                peerValuePair.Value.PeerNetworkClientSyncObject.DisconnectFromTarget();
                            }

                        }
                        catch
                        {
                            // Ignored.
                        }
                    }), 0, null);
                }
            }), 0, null);
        }

        /// <summary>
        /// Ask to other peers validation of the mining pow share.
        /// </summary>
        /// <param name="peerOpenNatServerIp"></param>
        /// <param name="peerToExcept"></param>
        /// <param name="blockHeight"></param>
        /// <param name="miningPowShareObject"></param>
        /// <param name="peerNetworkSetting"></param>
        /// <param name="peerFirewallSettingObject"></param>
        /// <param name="cancellation"></param>
        /// <param name="peerServerIp"></param>
        /// <param name="onlyOneAgree"></param>
        /// <returns></returns>
        public static async Task<Tuple<ClassBlockEnumMiningShareVoteStatus, bool>> AskBlockMiningShareVoteToPeerListsAsync(string peerServerIp, string peerOpenNatServerIp, string peerToExcept, long blockHeight, ClassMiningPoWaCShareObject miningPowShareObject, ClassPeerNetworkSettingObject peerNetworkSetting, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation, bool onlyOneAgree)
        {
            ClassBlockEnumMiningShareVoteStatus voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOCONSENSUS;

            #region If the block is already unlocked before to start broadcasting and ask votes.

            if (ClassBlockchainDatabase.BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus == ClassBlockEnumStatus.UNLOCKED)
            {
                // That's can happen sometimes when the broadcast of the share to other nodes is very fast and return back the data of the block unlocked to the synced data before to retrieve back every votes done.
                if (ClassMiningPoWaCUtility.ComparePoWaCShare(ClassBlockchainDatabase.BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject, miningPowShareObject))
                    voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                else
                    voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;

                return new Tuple<ClassBlockEnumMiningShareVoteStatus, bool>(voteResultStatus, true);
            }

            #endregion

            using (DisposableDictionary<int, ClassPeerTargetObject> peerListTarget = new DisposableDictionary<int, ClassPeerTargetObject>(0, GetRandomListPeerTargetAlive(peerServerIp, peerOpenNatServerIp, peerToExcept, null, peerNetworkSetting, peerFirewallSettingObject, cancellation)))
            {

                int totalTaskDone = 0;
                int totalResponseOk = 0;
                int totalAgree = 0;

                using (DisposableDictionary<bool, float> dictionaryMiningShareVoteNormPeer = new DisposableDictionary<bool, float>(0, new Dictionary<bool, float>() { { false, 0 }, { true, 0 } }))
                {
                    using (DisposableDictionary<bool, float> dictionaryMiningShareVoteSeedPeer = new DisposableDictionary<bool, float>(0, new Dictionary<bool, float>() { { false, 0 }, { true, 0 } }))
                    {
                        using (DisposableHashset<string> listOfRankedPeerPublicKeySaved = new DisposableHashset<string>())
                        {
                            using (CancellationTokenSource cancellationTokenSourceMiningShareVote = cancellation != null ? CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token) : new CancellationTokenSource())
                            {

                                long taskTimestampStart = TaskManager.TaskManager.CurrentTimestampMillisecond;


                                #region Ask mining share votes check result to peers.

                                foreach (var peerKey in peerListTarget.GetList.Keys)
                                {
                                    try
                                    {

                                        TaskManager.TaskManager.InsertTask(new Action(async () =>
                                        {
                                            bool invalidPacket = false;
                                            string peerIpTarget = peerListTarget[peerKey].PeerIpTarget;
                                            string peerUniqueIdTarget = peerListTarget[peerKey].PeerUniqueIdTarget;

                                            try
                                            {

                                                ClassPeerPacketSendMiningShareVote peerPacketSendMiningShareVote = await ClassPeerNetworkBroadcastShortcutFunction.SendBroadcastPacket<ClassPeerPacketSendAskMiningShareVote, ClassPeerPacketSendMiningShareVote>(
                                                    peerListTarget[peerKey].PeerNetworkClientSyncObject,
                                                    ClassPeerEnumPacketSend.ASK_MINING_SHARE_VOTE,
                                                    new ClassPeerPacketSendAskMiningShareVote()
                                                    {
                                                        BlockHeight = miningPowShareObject.BlockHeight,
                                                        MiningPowShareObject = miningPowShareObject,
                                                        PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
                                                    }, peerIpTarget, peerUniqueIdTarget, peerNetworkSetting, ClassPeerEnumPacketResponse.SEND_MINING_SHARE_VOTE, cancellationTokenSourceMiningShareVote);


                                                if (peerPacketSendMiningShareVote == null)
                                                    ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIpTarget, peerUniqueIdTarget, peerNetworkSetting, peerFirewallSettingObject);
                                                else
                                                {
                                                    if (ClassUtility.CheckPacketTimestamp(peerPacketSendMiningShareVote.PacketTimestamp, peerNetworkSetting.PeerMaxTimestampDelayPacket, peerNetworkSetting.PeerMaxEarlierPacketDelay))
                                                    {
                                                        ClassPeerCheckManager.InputPeerClientValidPacket(peerIpTarget, peerUniqueIdTarget, peerNetworkSetting);

                                                        if (peerPacketSendMiningShareVote.BlockHeight == miningPowShareObject.BlockHeight)
                                                        {
                                                            bool ignoreVote = false;
                                                            bool voteStatus = false;
                                                            switch (peerPacketSendMiningShareVote.VoteStatus)
                                                            {
                                                                case ClassPeerPacketMiningShareVoteEnum.ACCEPTED:
                                                                    voteStatus = true;
                                                                    totalAgree++;
                                                                    break;
                                                                // Already set to false.
                                                                case ClassPeerPacketMiningShareVoteEnum.REFUSED:
                                                                    break;
                                                                case ClassPeerPacketMiningShareVoteEnum.NOT_SYNCED:
                                                                    ignoreVote = true;
                                                                    break;
                                                                default:
                                                                    ignoreVote = true;
                                                                    break;
                                                            }
                                                            if (!ignoreVote)
                                                            {
                                                                bool peerRanked = false;

                                                                if (peerNetworkSetting.PeerEnableSovereignPeerVote)
                                                                {
                                                                    if (ClassPeerCheckManager.PeerHasSeedRank(peerIpTarget, peerUniqueIdTarget, out string numericPublicKeyOut, out _))
                                                                    {
                                                                        if (!listOfRankedPeerPublicKeySaved.Contains(numericPublicKeyOut))
                                                                        {
                                                                            if (ClassPeerCheckManager.CheckPeerSeedNumericPacketSignature(ClassUtility.SerializeData(new ClassPeerPacketSendMiningShareVote()
                                                                            {
                                                                                BlockHeight = peerPacketSendMiningShareVote.BlockHeight,
                                                                                VoteStatus = peerPacketSendMiningShareVote.VoteStatus,
                                                                                PacketTimestamp = peerPacketSendMiningShareVote.PacketTimestamp
                                                                            }),
                                                                            peerPacketSendMiningShareVote.PacketNumericHash,
                                                                            peerPacketSendMiningShareVote.PacketNumericSignature,
                                                                            numericPublicKeyOut,
                                                                            cancellationTokenSourceMiningShareVote))
                                                                            {
                                                                                // Do not allow multiple seed votes from the same numeric public key.
                                                                                if (!listOfRankedPeerPublicKeySaved.Contains(numericPublicKeyOut))
                                                                                {
                                                                                    if (listOfRankedPeerPublicKeySaved.Add(numericPublicKeyOut))
                                                                                        peerRanked = true;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }


                                                                switch (voteStatus)
                                                                {
                                                                    case true:
                                                                        if (peerRanked)
                                                                            dictionaryMiningShareVoteSeedPeer[true]++;
                                                                        else
                                                                            dictionaryMiningShareVoteNormPeer[true]++;
                                                                        break;
                                                                    case false:
                                                                        if (peerRanked)
                                                                            dictionaryMiningShareVoteSeedPeer[false]++;
                                                                        else
                                                                            dictionaryMiningShareVoteNormPeer[false]++;
                                                                        break;
                                                                }

                                                            }
                                                            totalResponseOk++;
                                                        }
                                                    }
                                                }

                                                peerListTarget[peerKey].PeerNetworkClientSyncObject.DisconnectFromTarget();
                                                peerListTarget[peerKey].PeerNetworkClientSyncObject.Dispose();
                                            }
                                            catch
                                            {
                                                // Ignored.
                                            }

                                            totalTaskDone++;

                                            if (invalidPacket)
                                                ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIpTarget, peerUniqueIdTarget, peerNetworkSetting, peerFirewallSettingObject);

                                        }), 0, cancellationTokenSourceMiningShareVote);

                                    }
                                    catch
                                    {
                                        // Ignored catch the exception once the taks is cancelled.
                                    }
                                }

                                #endregion

                                #region Wait every tasks of votes are done or cancelled.


                                while (totalTaskDone < peerListTarget.Count)
                                {
                                    // If the block is already unlocked pending to wait votes from other peers.
                                    if (ClassBlockchainDatabase.BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                    {
                                        // That's can happen sometimes when the broadcast of the share to other nodes is very fast and return back the data of the block unlocked to the synced data before to retrieve back every votes done.
                                        if (ClassMiningPoWaCUtility.ComparePoWaCShare(ClassBlockchainDatabase.BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject, miningPowShareObject))
                                        {
                                            ClassLog.WriteLine("Votes from peers ignored, the block seems to be found by the share provided and already available on sync.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                                            voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                        }
                                        else
                                        {
                                            ClassLog.WriteLine("Votes from peers ignored, the block seems to be found by another share or another miner and already available on sync.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                            voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;
                                        }

                                        cancellationTokenSourceMiningShareVote.Cancel();

                                        return new Tuple<ClassBlockEnumMiningShareVoteStatus, bool>(voteResultStatus, true);
                                    }


                                    if (totalResponseOk == peerListTarget.Count || totalTaskDone == peerListTarget.Count)
                                        break;

                                    if (onlyOneAgree && totalAgree > 0)
                                        break;

                                    // Max delay of waiting.
                                    if (taskTimestampStart + (peerNetworkSetting.PeerMaxDelayConnection * 1000) < TaskManager.TaskManager.CurrentTimestampMillisecond)
                                        break;

                                    await Task.Delay(100);
                                }

                                #endregion

                                cancellationTokenSourceMiningShareVote.Cancel();

                                if (!onlyOneAgree)
                                {
                                    try
                                    {
                                        if (!cancellationTokenSourceMiningShareVote.IsCancellationRequested)
                                            cancellationTokenSourceMiningShareVote.Cancel();
                                    }
                                    catch
                                    {
                                        // Ignored.  
                                    }

                                    // Clean up.
                                    listOfRankedPeerPublicKeySaved.Clear();

                                    foreach (int peerKey in peerListTarget.GetList.Keys)
                                    {
                                        try
                                        {
                                            peerListTarget[peerKey].PeerNetworkClientSyncObject.DisconnectFromTarget();
                                            peerListTarget[peerKey].PeerNetworkClientSyncObject.Dispose();
                                        }
                                        catch
                                        {
                                            // Ignored.
                                        }
                                    }

                                }


                                // If the block is already unlocked pending to wait votes from other peers.
                                if (ClassBlockchainDatabase.BlockchainMemoryManagement[blockHeight, cancellation].BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                                {
                                    // That's can happen sometimes when the broadcast of the share to other nodes is very fast and return back the data of the block unlocked to the synced data before to retrieve back every votes done.
                                    if (ClassMiningPoWaCUtility.ComparePoWaCShare(ClassBlockchainDatabase.BlockchainMemoryManagement[blockHeight, cancellation].BlockMiningPowShareUnlockObject, miningPowShareObject))
                                    {
                                        ClassLog.WriteLine("Votes from peers ignored, the block seems to be found by the share provided and already available on sync.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                                        voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                    }
                                    else
                                    {
                                        ClassLog.WriteLine("Votes from peers ignored, the block seems to be found by another share or another miner and already available on sync.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                        voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ALREADY_FOUND;
                                    }

                                    return new Tuple<ClassBlockEnumMiningShareVoteStatus, bool>(voteResultStatus, true);
                                }


                                if (!onlyOneAgree)
                                {
                                    #region Check the amount of responses received.

                                    if (peerNetworkSetting.PeerMinAvailablePeerSync > BlockchainSetting.PeerMinAvailablePeerSync)
                                    {
                                        if (totalResponseOk < peerNetworkSetting.PeerMinAvailablePeerSync)
                                        {
                                            ClassLog.WriteLine("Error on calculating peer(s) vote(s). Not enough responses received from peer, cancel vote and return no consensus.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                            return new Tuple<ClassBlockEnumMiningShareVoteStatus, bool>(ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOCONSENSUS, true);
                                        }
                                    }
                                    else
                                    {
                                        if (totalResponseOk < BlockchainSetting.PeerMinAvailablePeerSync)
                                        {
                                            ClassLog.WriteLine("Error on calculating peer(s) vote(s). Not enough responses received from peer, cancel vote and return no consensus.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                            return new Tuple<ClassBlockEnumMiningShareVoteStatus, bool>(ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOCONSENSUS, true);
                                        }
                                    }

                                    #endregion

                                    #region Calculate votes.

                                    try
                                    {

                                        float totalSeedVotes = dictionaryMiningShareVoteSeedPeer[false] + dictionaryMiningShareVoteSeedPeer[true];
                                        float totalNormVotes = dictionaryMiningShareVoteNormPeer[false] + dictionaryMiningShareVoteNormPeer[true];

                                        #region Check the amount of votes received.

                                        if (totalSeedVotes + totalNormVotes < peerNetworkSetting.PeerMinAvailablePeerSync)
                                        {
                                            ClassLog.WriteLine("Error on calculating peer(s) vote(s). Not enough responses received from peer, cancel vote and return no consensus.", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                            return new Tuple<ClassBlockEnumMiningShareVoteStatus, bool>(ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_NOCONSENSUS, true);
                                        }

                                        #endregion

                                        float percentSeedAgree = 0;
                                        float percentSeedDenied = 0;
                                        float seedCountAgree = 0;
                                        float seedCountDenied = 0;
                                        bool seedVoteResult = false;

                                        float percentNormAgree = 0;
                                        float percentNormDenied = 0;
                                        float normCountAgree = 0;
                                        float normCountDenied = 0;
                                        bool normVoteResult = false;

                                        if (totalSeedVotes > 0)
                                        {
                                            seedCountAgree = dictionaryMiningShareVoteSeedPeer[true];
                                            seedCountDenied = dictionaryMiningShareVoteSeedPeer[false];

                                            if (dictionaryMiningShareVoteSeedPeer[true] > 0)
                                                percentSeedAgree = (dictionaryMiningShareVoteSeedPeer[true] / totalSeedVotes) * 100f;
                                            if (dictionaryMiningShareVoteSeedPeer[false] > 0)
                                                percentSeedDenied = (dictionaryMiningShareVoteSeedPeer[false] / totalSeedVotes) * 100f;

                                            seedVoteResult = percentSeedAgree > percentSeedDenied;
                                        }

                                        if (totalNormVotes > 0)
                                        {
                                            normCountAgree = dictionaryMiningShareVoteNormPeer[true];
                                            normCountDenied = dictionaryMiningShareVoteNormPeer[false];

                                            if (dictionaryMiningShareVoteNormPeer[true] > 0)
                                                percentNormAgree = (dictionaryMiningShareVoteNormPeer[true] / totalNormVotes) * 100f;

                                            if (dictionaryMiningShareVoteNormPeer[false] > 0)
                                                percentNormDenied = (dictionaryMiningShareVoteNormPeer[false] / totalNormVotes) * 100f;

                                            normVoteResult = percentNormAgree > percentNormDenied;
                                        }

                                        switch (seedVoteResult)
                                        {
                                            case true:
                                                switch (normVoteResult)
                                                {
                                                    // Both types agreed together.
                                                    case true:
                                                        ClassLog.WriteLine("Mining Share on block height: " + blockHeight + " accepted by seeds and peers. " +
                                                                           "Seed Peer Accept: " + percentSeedAgree + "% (A: " + seedCountAgree + "/ D: " + seedCountDenied + ") | " +
                                                                           "Normal Peer Accept: " + percentNormAgree + "% (A: " + normCountAgree + "/ D: " + normCountDenied + ")", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);

                                                        voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                        break;
                                                    // Compare percent of agreements of seed peers vs percent of denied of normal peers.
                                                    case false:
                                                        if (percentSeedAgree > percentNormDenied)
                                                        {
                                                            ClassLog.WriteLine("Mining Share on block height: " + blockHeight + " accepted by seeds in majority. " +
                                                                               "Seed Peer Accept: " + percentSeedAgree + "% (A: " + seedCountAgree + "/ D: " + seedCountDenied + ") | " +
                                                                               "Normal Peer Denied: " + percentNormDenied + "% (A: " + normCountAgree + "/ D: " + normCountDenied + ")", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);
                                                            voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                        }
                                                        else if (percentSeedAgree < percentNormDenied)
                                                        {
                                                            ClassLog.WriteLine("Mining Share on block height: " + blockHeight + " refused by peers in majority. " +
                                                                               "Seed Peer Accept: " + percentSeedAgree + "% (A: " + seedCountAgree + "/ D: " + seedCountDenied + ") | " +
                                                                               "Normal Peer Denied: " + percentNormDenied + "% (A: " + normCountAgree + "/ D: " + normCountDenied + ")", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);
                                                            voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED;
                                                        }
                                                        break;
                                                }
                                                break;
                                            case false:
                                                switch (normVoteResult)
                                                {
                                                    // Compare percent of agreements of normal peers vs percent of denied of seeds.
                                                    case true:
                                                        if (percentNormAgree > percentSeedDenied)
                                                        {
                                                            ClassLog.WriteLine("Mining Share on block height: " + blockHeight + "  accepted by peers in majority. " +
                                                                               "Seed Peer Denied: " + percentSeedDenied + "% (A: " + seedCountAgree + "/ D: " + seedCountDenied + ") | " +
                                                                               "Normal Peer Accept: " + percentNormAgree + "% (A: " + normCountAgree + "/ D: " + normCountDenied + ")", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);

                                                            voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED;
                                                        }
                                                        else if (percentNormAgree < percentSeedDenied)
                                                        {
                                                            ClassLog.WriteLine("Mining Share on block height: " + blockHeight + "  refused by seed in majority. " +
                                                                               "Seed Peer Denied: " + percentSeedDenied + "% (A: " + seedCountAgree + "/ D: " + seedCountDenied + ") | " +
                                                                               "Normal Peer Accept: " + percentNormAgree + "% (A: " + normCountAgree + "/ D: " + normCountDenied + ")", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);

                                                            voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED;
                                                        }
                                                        break;
                                                    // Both types denied together.
                                                    case false:
                                                        ClassLog.WriteLine("Mining Share on block height: " + blockHeight + " refused by seeds and peers. " +
                                                                           "Seed Peer Denied: " + percentSeedDenied + "% (A: " + seedCountAgree + "/ D: " + seedCountDenied + ") | " +
                                                                           "Normal Peer Denied: " + percentNormDenied + "% (A: " + normCountAgree + "/ D: " + normCountDenied + ")", ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Cyan);

                                                        voteResultStatus = ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_REFUSED;
                                                        break;
                                                }
                                                break;
                                        }


                                    }
                                    catch (Exception error)
                                    {
                                        ClassLog.WriteLine("Error on calculating peer(s) vote(s). Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                    }

                                    #endregion
                                }
                                else
                                {
                                    if (totalAgree > 0)
                                        return new Tuple<ClassBlockEnumMiningShareVoteStatus, bool>(ClassBlockEnumMiningShareVoteStatus.MINING_SHARE_VOTE_ACCEPTED, false);
                                }

                                // Exception or no consensus found or ignored vote result.
                                return new Tuple<ClassBlockEnumMiningShareVoteStatus, bool>(voteResultStatus, false);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region MemPool tx's broadcast.

        /// <summary>
        /// Ask other peers if the tx's to push into mem pool is valid.
        /// </summary>
        /// <param name="peerToExcept"></param>
        /// <param name="listTransactionObject"></param>
        /// <param name="peerNetworkSetting"></param>
        /// <param name="peerFirewallSettingObject"></param>
        /// <param name="cancellation"></param>
        /// <param name="peerServerIp"></param>
        /// <param name="peerOpenNatServerIp"></param>
        /// <returns></returns>
        public static async Task<DisposableDictionary<string, ClassTransactionEnumStatus>> AskMemPoolTxVoteToPeerListsAsync(string peerServerIp, string peerOpenNatServerIp, string peerToExcept, List<ClassTransactionObject> listTransactionObject, ClassPeerNetworkSettingObject peerNetworkSetting, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation, bool onlyOneAgree)
        {

            DisposableDictionary<string, ClassTransactionEnumStatus> dictionaryTransactionCheckStatus = new DisposableDictionary<string, ClassTransactionEnumStatus>();
            var peerListTarget = GetRandomListPeerTargetAlive(peerServerIp, peerOpenNatServerIp, peerToExcept, null, peerNetworkSetting, peerFirewallSettingObject, cancellation);

            using (DisposableDictionary<string, Dictionary<bool, float>> dictionaryMemPoolTxVoteNormPeer = new DisposableDictionary<string, Dictionary<bool, float>>())
            {
                using (DisposableDictionary<string, Dictionary<bool, float>> dictionaryMemPoolTxVoteSeedPeer = new DisposableDictionary<string, Dictionary<bool, float>>())
                {
                    using (DisposableHashset<string> listOfRankedPeerPublicKeySaved = new DisposableHashset<string>())
                    {
                        long timestampEnd = TaskManager.TaskManager.CurrentTimestampMillisecond + (peerNetworkSetting.PeerMaxDelayAwaitResponse * 1000);
                        int totalTaskDone = 0;
                        int totalResponseOk = 0;

                        using (CancellationTokenSource cancellationTokenSourceMemPoolTxVote = new CancellationTokenSource())
                        {


                            foreach (var peerKey in peerListTarget.Keys)
                            {
                                try
                                {
                                    TaskManager.TaskManager.InsertTask(new Action(async () =>
                                    {
                                        bool invalidPacket = false;

                                        string peerIpTarget = peerListTarget[peerKey].PeerIpTarget;
                                        string peerUniqueIdTarget = peerListTarget[peerKey].PeerUniqueIdTarget;

                                        try
                                        {

                                            ClassPeerPacketSendMemPoolTransactionVote packetSendMemPoolTransactionVote = await ClassPeerNetworkBroadcastShortcutFunction.SendBroadcastPacket<ClassPeerPacketSendAskMemPoolTransactionVote, ClassPeerPacketSendMemPoolTransactionVote>(
                                                                                                                     peerListTarget[peerKey].PeerNetworkClientSyncObject,
                                                                                                                     ClassPeerEnumPacketSend.ASK_MEM_POOL_TRANSACTION_VOTE,
                                                                                                                    new ClassPeerPacketSendAskMemPoolTransactionVote()
                                                                                                                    {
                                                                                                                        ListTransactionObject = listTransactionObject,
                                                                                                                        PacketTimestamp = ClassUtility.GetCurrentTimestampInSecond(),
                                                                                                                    }, peerIpTarget, peerUniqueIdTarget, peerNetworkSetting, ClassPeerEnumPacketResponse.SEND_MEM_POOL_TRANSACTION_VOTE, cancellationTokenSourceMemPoolTxVote);

                                            if (packetSendMemPoolTransactionVote == null)
                                                invalidPacket = true;
                                            else
                                            {
                                                if (ClassUtility.CheckPacketTimestamp(packetSendMemPoolTransactionVote.PacketTimestamp, peerNetworkSetting.PeerMaxTimestampDelayPacket, peerNetworkSetting.PeerMaxEarlierPacketDelay))
                                                {
                                                    ClassPeerCheckManager.InputPeerClientValidPacket(peerIpTarget, peerUniqueIdTarget, peerNetworkSetting);

                                                    bool peerRanked = false;

                                                    if (peerNetworkSetting.PeerEnableSovereignPeerVote)
                                                    {
                                                        if (ClassPeerCheckManager.PeerHasSeedRank(peerIpTarget, peerUniqueIdTarget, out string numericPublicKeyOut, out _))
                                                        {
                                                            if (!listOfRankedPeerPublicKeySaved.Contains(numericPublicKeyOut))
                                                            {
                                                                if (ClassPeerCheckManager.CheckPeerSeedNumericPacketSignature(ClassUtility.SerializeData(new ClassPeerPacketSendMemPoolTransactionVote()
                                                                {
                                                                    ListTransactionHashResult = packetSendMemPoolTransactionVote.ListTransactionHashResult,
                                                                    PacketTimestamp = packetSendMemPoolTransactionVote.PacketTimestamp
                                                                }),
                                                                packetSendMemPoolTransactionVote.PacketNumericHash,
                                                                packetSendMemPoolTransactionVote.PacketNumericSignature,
                                                                numericPublicKeyOut,
                                                                cancellationTokenSourceMemPoolTxVote))
                                                                {
                                                                // Do not allow multiple seed votes from the same numeric public key.
                                                                if (!listOfRankedPeerPublicKeySaved.Contains(numericPublicKeyOut))
                                                                    {
                                                                        if (listOfRankedPeerPublicKeySaved.Add(numericPublicKeyOut))
                                                                            peerRanked = true;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }

                                                    foreach (var transaction in packetSendMemPoolTransactionVote.ListTransactionHashResult)
                                                    {
                                                        switch (peerRanked)
                                                        {
                                                            case true:
                                                                {
                                                                    if (!dictionaryMemPoolTxVoteSeedPeer.ContainsKey(transaction.Key))
                                                                    {
                                                                        dictionaryMemPoolTxVoteSeedPeer.Add(transaction.Key, new Dictionary<bool, float>());
                                                                        dictionaryMemPoolTxVoteSeedPeer[transaction.Key].Add(false, 0);
                                                                        dictionaryMemPoolTxVoteSeedPeer[transaction.Key].Add(true, 0);
                                                                    }
                                                                    if (dictionaryMemPoolTxVoteSeedPeer.ContainsKey(transaction.Key))
                                                                    {
                                                                        if (transaction.Value == ClassTransactionEnumStatus.VALID_TRANSACTION)
                                                                            dictionaryMemPoolTxVoteSeedPeer[transaction.Key][true]++;
                                                                        else
                                                                            dictionaryMemPoolTxVoteSeedPeer[transaction.Key][false]++;
                                                                    }
                                                                    totalResponseOk++;
                                                                }
                                                                break;
                                                            case false:
                                                                {
                                                                    if (!dictionaryMemPoolTxVoteNormPeer.ContainsKey(transaction.Key))
                                                                    {
                                                                        dictionaryMemPoolTxVoteNormPeer.Add(transaction.Key, new Dictionary<bool, float>());
                                                                        dictionaryMemPoolTxVoteNormPeer[transaction.Key].Add(false, 0);
                                                                        dictionaryMemPoolTxVoteNormPeer[transaction.Key].Add(true, 0);
                                                                    }

                                                                    if (dictionaryMemPoolTxVoteNormPeer.ContainsKey(transaction.Key))
                                                                    {
                                                                        if (transaction.Value == ClassTransactionEnumStatus.VALID_TRANSACTION)
                                                                            dictionaryMemPoolTxVoteNormPeer[transaction.Key][true]++;
                                                                        else
                                                                            dictionaryMemPoolTxVoteNormPeer[transaction.Key][false]++;
                                                                    }
                                                                    totalResponseOk++;
                                                                }
                                                                break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                        // Ignored.
                                    }

                                        if (invalidPacket)
                                            ClassPeerCheckManager.InputPeerClientInvalidPacket(peerIpTarget, peerUniqueIdTarget, peerNetworkSetting, peerFirewallSettingObject);


                                        totalTaskDone++;

                                    }), timestampEnd, cancellationTokenSourceMemPoolTxVote);
                                }
                                catch
                                {
                                    // Ignored, catch the exception once the task is cancelled.
                                }
                            }

                            while (totalTaskDone < peerListTarget.Count)
                            {
                                if (totalResponseOk >= peerListTarget.Count)
                                    break;

                                // Timeout reach.
                                if (timestampEnd < TaskManager.TaskManager.CurrentTimestampMillisecond)
                                    break;

                                if (onlyOneAgree)
                                {

                                    int totalAgree = 0;
                                    foreach (var transaction in listTransactionObject)
                                    {
                                        bool agree = false;
                                        if (dictionaryMemPoolTxVoteNormPeer.GetList.ContainsKey(transaction.TransactionHash))
                                        {
                                            if (dictionaryMemPoolTxVoteNormPeer[transaction.TransactionHash].ContainsKey(true))
                                            {
                                                if (dictionaryMemPoolTxVoteNormPeer[transaction.TransactionHash][true] > 0)
                                                {
                                                    totalAgree++;
                                                    agree = true;
                                                }
                                            }
                                        }

                                        if (!agree)
                                        {
                                            if (dictionaryMemPoolTxVoteSeedPeer.GetList.ContainsKey(transaction.TransactionHash))
                                            {
                                                if (dictionaryMemPoolTxVoteSeedPeer[transaction.TransactionHash].ContainsKey(true))
                                                {
                                                    if (dictionaryMemPoolTxVoteSeedPeer[transaction.TransactionHash][true] > 0)
                                                        totalAgree++;
                                                }
                                            }
                                        }
                                    }

                                    if (totalAgree >= listTransactionObject.Count)
                                        break;
                                }

                                try
                                {
                                    await Task.Delay(100, cancellationTokenSourceMemPoolTxVote.Token);
                                }
                                catch
                                {
                                    break;
                                }
                            }

                            cancellationTokenSourceMemPoolTxVote.Cancel();




                            #region Clean up contact peers.

                            foreach (int peerKey in peerListTarget.Keys)
                            {
                                try
                                {
                                    peerListTarget[peerKey].PeerNetworkClientSyncObject.Dispose();
                                }
                                catch
                                {
                                    // Ignored.
                                }
                            }


                            #endregion

                            #region Calculate votes.


                            foreach (var transaction in listTransactionObject)
                            {
                                float totalSeedVotes = 0;
                                float percentSeedAgree = 0;
                                float percentSeedDenied = 0;
                                bool seedVoteResult = false;

                                float totalNormVotes = 0;
                                float percentNormAgree = 0;
                                float percentNormDenied = 0;
                                bool normVoteResult = false;

                                if (dictionaryMemPoolTxVoteSeedPeer.ContainsKey(transaction.TransactionHash))
                                    totalSeedVotes = dictionaryMemPoolTxVoteSeedPeer[transaction.TransactionHash][false] + dictionaryMemPoolTxVoteSeedPeer[transaction.TransactionHash][true];

                                if (dictionaryMemPoolTxVoteNormPeer.ContainsKey(transaction.TransactionHash))
                                    totalNormVotes = dictionaryMemPoolTxVoteNormPeer[transaction.TransactionHash][false] + dictionaryMemPoolTxVoteNormPeer[transaction.TransactionHash][true];


                                if (totalSeedVotes > 0)
                                {
                                    if (dictionaryMemPoolTxVoteSeedPeer[transaction.TransactionHash].ContainsKey(true))
                                    {
                                        if (dictionaryMemPoolTxVoteSeedPeer[transaction.TransactionHash][true] > 0)
                                            percentSeedAgree = (dictionaryMemPoolTxVoteSeedPeer[transaction.TransactionHash][true] / totalSeedVotes) * 100f;
                                    }

                                    if (dictionaryMemPoolTxVoteSeedPeer[transaction.TransactionHash].ContainsKey(false))
                                    {
                                        if (dictionaryMemPoolTxVoteSeedPeer[transaction.TransactionHash][false] > 0)
                                            percentSeedDenied = (dictionaryMemPoolTxVoteSeedPeer[transaction.TransactionHash][false] / totalSeedVotes) * 100f;
                                    }

                                    seedVoteResult = percentSeedAgree > percentSeedDenied;
                                }

                                if (totalNormVotes > 0)
                                {
                                    if (dictionaryMemPoolTxVoteNormPeer[transaction.TransactionHash].ContainsKey(true))
                                    {
                                        if (dictionaryMemPoolTxVoteNormPeer[transaction.TransactionHash][true] > 0)
                                            percentNormAgree = (dictionaryMemPoolTxVoteNormPeer[transaction.TransactionHash][true] / totalNormVotes) * 100f;
                                    }

                                    if (dictionaryMemPoolTxVoteNormPeer[transaction.TransactionHash].ContainsKey(false))
                                    {
                                        if (dictionaryMemPoolTxVoteNormPeer[transaction.TransactionHash][false] > 0)
                                            percentNormDenied = (dictionaryMemPoolTxVoteNormPeer[transaction.TransactionHash][false] / totalNormVotes) * 100f;
                                    }

                                    normVoteResult = percentNormAgree > percentNormDenied;
                                }


                                ClassTransactionEnumStatus result = ClassTransactionEnumStatus.INVALID_TRANSACTION_FROM_VOTE;

                                if (!onlyOneAgree)
                                {
                                    switch (seedVoteResult)
                                    {
                                        case true:
                                            {
                                                // Total equality.
                                                if (normVoteResult) result = ClassTransactionEnumStatus.VALID_TRANSACTION;
                                                else if (percentSeedAgree > percentNormDenied) result = ClassTransactionEnumStatus.VALID_TRANSACTION;
                                            }
                                            break;
                                        case false:
                                            {
                                                // Total equality.
                                                if (!normVoteResult) result = ClassTransactionEnumStatus.INVALID_TRANSACTION_FROM_VOTE;
                                                else if (percentNormAgree > percentSeedDenied) result = ClassTransactionEnumStatus.VALID_TRANSACTION;
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    if (percentNormAgree > 0)
                                        result = ClassTransactionEnumStatus.VALID_TRANSACTION;
                                }

                                if (!dictionaryTransactionCheckStatus.ContainsKey(transaction.TransactionHash))
                                    dictionaryTransactionCheckStatus.Add(transaction.TransactionHash, result);
                            }

                            listOfRankedPeerPublicKeySaved.Clear();

                            #endregion

                        }
                    }
                }
            }

            return dictionaryTransactionCheckStatus;
        }

        #endregion
    }
}
