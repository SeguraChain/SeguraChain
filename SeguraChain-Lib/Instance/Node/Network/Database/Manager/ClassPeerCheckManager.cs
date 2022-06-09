using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SeguraChain_Lib.Blockchain.Sovereign.Database;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Status;
using SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Manager;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.ClientConnect.Object;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Database.Manager
{
    /// <summary>
    /// A static class for check peer tcp connection server or client.
    /// </summary>
    public class ClassPeerCheckManager
    {
        #region Check peer state.


        /// <summary>
        /// Check whole peer status, remove dead peers.
        /// </summary>
        /// <param name="cancellation"></param>
        /// <param name="peerNetworkSetting"></param>
        /// <returns></returns>
        public static void CheckWholePeerStatus(CancellationTokenSource cancellation, ClassPeerNetworkSettingObject peerNetworkSetting, ClassPeerFirewallSettingObject peerFirewallSetting)
        {
            if (ClassPeerDatabase.DictionaryPeerDataObject.Count > 0)
            {
                long currentTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;
                foreach (string peerIp in ClassPeerDatabase.DictionaryPeerDataObject.Keys.ToArray())
                {
                    if (cancellation.IsCancellationRequested)
                        break;

                    if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].Count > 0)
                    {
                        foreach (string peerUniqueId in ClassPeerDatabase.DictionaryPeerDataObject[peerIp].Keys.ToArray())
                        {
                            if (cancellation.IsCancellationRequested)
                                break;

                            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIsPublic)
                            {
                                if (!CheckPeerClientStatus(peerIp, peerUniqueId, false, peerNetworkSetting, peerFirewallSetting))
                                {
                                    if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastPacketReceivedTimestamp + peerNetworkSetting.PeerDelayDeleteDeadPeer <= currentTimestamp &&
                                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastValidPacket + peerNetworkSetting.PeerMaxDelayKeepAliveStats <= currentTimestamp &&
                                        (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket >= peerNetworkSetting.PeerMaxInvalidPacket ||
                                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalAttemptConnection >= peerNetworkSetting.PeerMaxAttemptConnection))
                                    {
                                        long deadTime = currentTimestamp - ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastPacketReceivedTimestamp;

                                        if (ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
                                        {
                                            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].TryRemove(peerUniqueId, out _))
                                            {
#if DEBUG
                                                Debug.WriteLine("Peer IP: " + peerIp + " | Peer Unique ID: " + peerUniqueId + " removed from the listing of peers, this one is dead since " + deadTime + " second(s).");
#endif
                                                ClassLog.WriteLine("Peer IP: " + peerIp + " | Peer Unique ID: " + peerUniqueId + " removed from the listing of peers, this one is dead since " + deadTime + " second(s).", ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
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

        /// <summary>
        /// Check if the peer client status is good. (Invalid packets amount, ban delay and more).
        /// </summary>
        /// <returns></returns>
        public static bool CheckPeerClientStatus(string peerIp, string peerUniqueId, bool isIncomingConnection, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            try
            {
                if (ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
                {
                    if (CheckPeerClientInitializationStatus(peerIp, peerUniqueId))
                    {
                        if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus == ClassPeerEnumStatus.PEER_BANNED || ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus == ClassPeerEnumStatus.PEER_DEAD)
                        {

                            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus == ClassPeerEnumStatus.PEER_BANNED)
                            {
                                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerBanDate + peerNetworkSettingObject.PeerBanDelay <= TaskManager.TaskManager.CurrentTimestampSecond)
                                {
                                    CleanPeerState(peerIp, peerUniqueId, true);
                                    return true;
                                }
                            }
                            else if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus == ClassPeerEnumStatus.PEER_DEAD)
                            {
                                if (!isIncomingConnection)
                                {
                                    if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastDeadTimestamp + peerNetworkSettingObject.PeerDeadDelay <= TaskManager.TaskManager.CurrentTimestampSecond)
                                    {
                                        CleanPeerState(peerIp, peerUniqueId, true);
                                        return true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastValidPacket + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats <= TaskManager.TaskManager.CurrentTimestampSecond)
                                CleanPeerState(peerIp, peerUniqueId, false);
                            else if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalAttemptConnection >= peerNetworkSettingObject.PeerMaxAttemptConnection ||
                                     ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalNoPacketConnectionAttempt >= peerNetworkSettingObject.PeerMaxNoPacketPerConnectionOpened)
                            {
                                SetPeerDeadState(peerIp, peerUniqueId, peerNetworkSettingObject, peerFirewallSettingObject);
                                return false;
                            }
                            else if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket >= peerNetworkSettingObject.PeerMaxInvalidPacket)
                            {
                                SetPeerBanState(peerIp, peerUniqueId, peerNetworkSettingObject, peerFirewallSettingObject);
                                return false;
                            }
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignored.
            }


            return false;
        }

        /// <summary>
        /// Check if the peer client status is enough fine for not check packet signature.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <returns></returns>
        public static bool CheckPeerClientWhitelistStatus(string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject)
        {
            if (peerUniqueId.IsNullOrEmpty(false, out _))
                return false;

            if (!ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
                return false;

            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastValidPacket + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats < TaskManager.TaskManager.CurrentTimestampSecond)
            {
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist = 0;
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalPassedPeerPacketSignature = 0;
                return false;
            }

            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist >= TaskManager.TaskManager.CurrentTimestampSecond)
                return true;

            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalValidPacket >= peerNetworkSettingObject.PeerMinValidPacket)
            {
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalPassedPeerPacketSignature++;
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalPassedPeerPacketSignature >= peerNetworkSettingObject.PeerMaxWhiteListPacket)
                {
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist = TaskManager.TaskManager.CurrentTimestampSecond + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalPassedPeerPacketSignature = 0;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalValidPacket = 0;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the peer client has been initialized.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <returns></returns>
        public static bool CheckPeerClientInitializationStatus(string peerIp, string peerUniqueId)
        {
            if (!ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
                return false;

            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIp.IsNullOrEmpty(false, out _) ||
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerUniqueId.IsNullOrEmpty(false, out _) ||
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerPort <= 0 ||
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey == null ||
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKeyIv == null ||
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey.IsNullOrEmpty(false, out _) ||
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey.IsNullOrEmpty(false, out _) ||
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPacketEncryptionKey == null ||
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPacketEncryptionKeyIv == null ||
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPublicKey.IsNullOrEmpty(false, out _))
                return false;

            return true;
        }

        /// <summary>
        /// Compare the public key received by a peer with the public key registered.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerPublicKeyReceived"></param>
        /// <returns></returns>
        public static bool ComparePeerPacketPublicKey(string peerIp, string peerUniqueId, string peerPublicKeyReceived)
        {
            if (!ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId) ||
                peerPublicKeyReceived.IsNullOrEmpty(false, out _))
                return false;

            return ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPublicKey == peerPublicKeyReceived;
        }

        /// <summary>
        /// Return the amount of peers alive from a list.
        /// </summary>
        /// <param name="peerListTarget"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        /// <returns></returns>
        public static int GetCountPeerAliveFromList(Dictionary<int, ClassPeerTargetObject> peerListTarget, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            int count = 0;

            foreach(var peer in peerListTarget)
            {
                if (CheckPeerClientStatus(peer.Value.PeerIpTarget, peer.Value.PeerUniqueIdTarget, false, peerNetworkSettingObject, peerFirewallSettingObject))
                    count++;
            }

            return count;
        }

        #endregion

        #region Update peer stats.

        /// <summary>
        /// Input invalid packet to a peer.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public static void InputPeerClientInvalidPacket(string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            long currentTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;

            if (!ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
            {
                if (peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastBadStatePacket + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats <= currentTimestamp)
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket = 0;

                UpdatePeerLastBadStatePacket(peerIp, peerUniqueId, peerFirewallSettingObject);
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket++;

                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket >= peerNetworkSettingObject.PeerMaxInvalidPacket)
                    SetPeerBanState(peerIp, peerUniqueId, peerNetworkSettingObject, peerFirewallSettingObject);

            }
        }

        /// <summary>
        /// Input the amount of no packet provided per connection opened.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public static void InputPeerClientNoPacketConnectionOpened(string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {

            if (!ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
            {
                if (peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastBadStatePacket + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats <= TaskManager.TaskManager.CurrentTimestampSecond)
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalNoPacketConnectionAttempt = 0;

                UpdatePeerLastBadStatePacket(peerIp, peerUniqueId, peerFirewallSettingObject);

                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalNoPacketConnectionAttempt++;

                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalNoPacketConnectionAttempt >= peerNetworkSettingObject.PeerMaxNoPacketPerConnectionOpened)
                    SetPeerDeadState(peerIp, peerUniqueId, peerNetworkSettingObject,  peerFirewallSettingObject);
            }
        }

        /// <summary>
        /// Increment attempt to connect to a peer.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public static void InputPeerClientAttemptConnect(string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            if (!ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
            {
                if (peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastBadStatePacket + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats <= TaskManager.TaskManager.CurrentTimestampSecond)
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalAttemptConnection = 0;

                UpdatePeerLastBadStatePacket(peerIp, peerUniqueId, peerFirewallSettingObject);

                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalAttemptConnection++;
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalAttemptConnection >= peerNetworkSettingObject.PeerMaxAttemptConnection)
                    SetPeerDeadState(peerIp, peerUniqueId, peerNetworkSettingObject, peerFirewallSettingObject);
            }
        }

        /// <summary>
        /// Increment valid packet counter to a peer.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        public static void InputPeerClientValidPacket(string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject)
        {
            if (ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
            {
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastValidPacket = TaskManager.TaskManager.CurrentTimestampSecond;
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalValidPacket++;
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalNoPacketConnectionAttempt = 0;

                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalValidPacket >= peerNetworkSettingObject.PeerMaxInvalidPacket)
                {
                    if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket > 0)
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket--;
                    else if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket < 0)
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket = 0;
                }
            }
        }

        /// <summary>
        /// Update the peer last packet received
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        public static void UpdatePeerClientLastPacketReceived(string peerIp, string peerUniqueId, long peerLastTimestampSignatureWhitelist)
        {
            ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastPacketReceivedTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;
            ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalNoPacketConnectionAttempt = 0;
            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist > 0)
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist = peerLastTimestampSignatureWhitelist;
        }

        #endregion

        #region Manage peer states.

        /// <summary>
        /// CloseDiskCache peer state.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="unBanOrUnDead"></param>
        public static void CleanPeerState(string peerIp, string peerUniqueId, bool unBanOrUnDead)
        {
            if (ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
            {
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus = ClassPeerEnumStatus.PEER_ALIVE;
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalAttemptConnection = 0;
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket = 0;
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastBadStatePacket = 0;
                if (unBanOrUnDead)
                {
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerBanDate = 0;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastDeadTimestamp = 0;
                }
                if (!unBanOrUnDead)
                {
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist = 0;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalPassedPeerPacketSignature = 0;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalNoPacketConnectionAttempt = 0;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalAttemptConnection = 0;
                }
                ClassLog.WriteLine("Peer: " + peerIp + " | Unique ID: " + peerUniqueId + " state has been cleaned.", ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MEDIUM_PRIORITY, false, ConsoleColor.Magenta);
            }
        }

        /// <summary>
        /// Ban a peer.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public static void SetPeerBanState(string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            if (!ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
            {
                if (peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus != ClassPeerEnumStatus.PEER_BANNED)
                {
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus = ClassPeerEnumStatus.PEER_BANNED;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalInvalidPacket = peerNetworkSettingObject.PeerMaxInvalidPacket;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerBanDate = TaskManager.TaskManager.CurrentTimestampSecond;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalValidPacket = 0;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalPassedPeerPacketSignature = 0;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist = 0;
                    ClassLog.WriteLine("Peer: " + peerIp + " | Unique ID: " + peerUniqueId + " state has been set to banned temporaly.", ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                }
            }

        }

        /// <summary>
        /// Set a peer dead.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public static void SetPeerDeadState(string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            if (!ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
            {
                if (!peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus != ClassPeerEnumStatus.PEER_DEAD)
                {
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus = ClassPeerEnumStatus.PEER_DEAD;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalAttemptConnection = 0;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerTotalNoPacketConnectionAttempt = 0;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastDeadTimestamp = TaskManager.TaskManager.CurrentTimestampSecond + peerNetworkSettingObject.PeerDeadDelay;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist = 0;

                    ClassLog.WriteLine("Peer: " + peerIp + " | Unique ID: " + peerUniqueId + " state has been set to dead temporaly.", ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                }
            }
        }

        /// <summary>
        /// Check if the peer have the Seed Rank.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="numericPublicKeyOut"></param>
        /// <param name="timestampRankDelay"></param>
        /// <returns></returns>
        public static bool PeerHasSeedRank(string peerIp, string peerUniqueId, out string numericPublicKeyOut, out long timestampRankDelay)
        {
            timestampRankDelay = 0;

            if (!ClassPeerDatabase.GetPeerNumericPublicKey(peerIp, peerUniqueId, out numericPublicKeyOut))
                return false;

            if (numericPublicKeyOut.IsNullOrEmpty(false, out _))
                return false;

            return ClassSovereignUpdateDatabase.CheckIfNumericPublicKeyPeerIsRanked(numericPublicKeyOut, out timestampRankDelay);
        }

        /// <summary>
        /// Check the peer packet numeric signature sent from a peer with the seed rank.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="packetNumericHash"></param>
        /// <param name="packetNumericSignature"></param>
        /// <param name="peerNumericPublicKey"></param>
        /// <returns></returns>
        public static bool CheckPeerSeedNumericPacketSignature(string data, string packetNumericHash, string packetNumericSignature, string peerNumericPublicKey, CancellationTokenSource cancellation)
        {
            if (packetNumericHash.IsNullOrEmpty(false, out _) || packetNumericSignature.IsNullOrEmpty(false, out _) || peerNumericPublicKey.IsNullOrEmpty(false, out _))
                return false;

            if (ClassUtility.GenerateSha256FromString(data) != packetNumericHash)
                return false;

            return ClassWalletUtility.WalletCheckSignature(packetNumericHash, packetNumericSignature, peerNumericPublicKey);
        }

        #endregion

        #region Update peer states timestamp.

        /// <summary>
        /// Update last timestamp of packet received.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerFirewallSettingObject"></param>
        /// 
        public static void UpdatePeerLastBadStatePacket(string peerIp, string peerUniqueId, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            if (!ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
            {
                if (peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastBadStatePacket = TaskManager.TaskManager.CurrentTimestampSecond;
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalValidPacket = 0;
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientTotalPassedPeerPacketSignature = 0;
                ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientLastTimestampPeerPacketSignatureWhitelist = 0;
            }

        }

        #endregion
    }
}
