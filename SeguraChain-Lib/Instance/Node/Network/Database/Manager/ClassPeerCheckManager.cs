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
        public static void CheckWholePeerStatus(ClassPeerDatabase peerDatabase, CancellationTokenSource cancellation, ClassPeerNetworkSettingObject peerNetworkSetting, ClassPeerFirewallSettingObject peerFirewallSetting)
        {
            if (peerDatabase.Count > 0)
            {
                long currentTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;
                foreach (string peerIp in peerDatabase.Keys.ToArray())
                {
                    if (cancellation.IsCancellationRequested)
                        break;

                    if (peerDatabase[peerIp, cancellation].Count > 0)
                    {
                        foreach (string peerUniqueId in peerDatabase[peerIp, cancellation].Keys.ToArray())
                        {
                            if (cancellation.IsCancellationRequested)
                                break;

                            if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerIsPublic)
                            {
                                if (!CheckPeerClientStatus(peerDatabase, peerIp, peerUniqueId, false, peerNetworkSetting, peerFirewallSetting, cancellation))
                                {
                                    if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastPacketReceivedTimestamp + peerNetworkSetting.PeerDelayDeleteDeadPeer <= currentTimestamp &&
                                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastValidPacket + peerNetworkSetting.PeerMaxDelayKeepAliveStats <= currentTimestamp &&
                                        (peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket >= peerNetworkSetting.PeerMaxInvalidPacket ||
                                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalAttemptConnection >= peerNetworkSetting.PeerMaxAttemptConnection))
                                    {
                                        long deadTime = currentTimestamp - peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastPacketReceivedTimestamp;

                                        if (peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
                                        {
                                            SetPeerDeadState(peerDatabase, peerIp, peerUniqueId, peerNetworkSetting, peerFirewallSetting, cancellation);
                                            if (peerDatabase[peerIp, cancellation].TryRemove(peerUniqueId, out _))
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
        public static bool CheckPeerClientStatus(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, bool isIncomingConnection, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation)
        {
            try
            {
                if (peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
                {
                    if (CheckPeerClientInitializationStatus(peerDatabase, peerIp, peerUniqueId, cancellation))
                    {
                        if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerStatus == ClassPeerEnumStatus.PEER_BANNED || peerDatabase[peerIp, peerUniqueId, cancellation].PeerStatus == ClassPeerEnumStatus.PEER_DEAD)
                        {

                            if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerStatus == ClassPeerEnumStatus.PEER_BANNED)
                            {
                                if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerBanDate + peerNetworkSettingObject.PeerBanDelay <= TaskManager.TaskManager.CurrentTimestampSecond)
                                {
                                    CleanPeerState(peerDatabase, peerIp, peerUniqueId, true, cancellation);
                                    return true;
                                }
                            }
                            else if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerStatus == ClassPeerEnumStatus.PEER_DEAD)
                            {
                                if (!isIncomingConnection)
                                {
                                    if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastDeadTimestamp + peerNetworkSettingObject.PeerDeadDelay <= TaskManager.TaskManager.CurrentTimestampSecond)
                                    {
                                        CleanPeerState(peerDatabase, peerIp, peerUniqueId, true, cancellation);
                                        return true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            /*
                            if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastValidPacket + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats <= TaskManager.TaskManager.CurrentTimestampSecond)
                                CleanPeerState(peerIp, peerUniqueId, false);
                            else*/ if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalAttemptConnection >= peerNetworkSettingObject.PeerMaxAttemptConnection ||
                                     peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalNoPacketConnectionAttempt >= peerNetworkSettingObject.PeerMaxNoPacketPerConnectionOpened)
                            {
                                SetPeerDeadState(peerDatabase, peerIp, peerUniqueId, peerNetworkSettingObject, peerFirewallSettingObject, cancellation);
                                return false;
                            }
                            else if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket >= peerNetworkSettingObject.PeerMaxInvalidPacket)
                            {
                                SetPeerBanState(peerDatabase, peerIp, peerUniqueId, peerNetworkSettingObject, peerFirewallSettingObject, cancellation);
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
        public static bool CheckPeerClientWhitelistStatus(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, CancellationTokenSource cancellation)
        {
            if (peerUniqueId.IsNullOrEmpty(false, out _))
                return false;

            if (!peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
                return false;

            if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastValidPacket + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats < TaskManager.TaskManager.CurrentTimestampSecond)
            {
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientLastTimestampPeerPacketSignatureWhitelist = 0;
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalPassedPeerPacketSignature = 0;
                return false;
            }

            if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientLastTimestampPeerPacketSignatureWhitelist >= TaskManager.TaskManager.CurrentTimestampSecond)
                return true;

            if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalValidPacket >= peerNetworkSettingObject.PeerMinValidPacket)
            {
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalPassedPeerPacketSignature++;
                if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalPassedPeerPacketSignature >= peerNetworkSettingObject.PeerMaxWhiteListPacket)
                {
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientLastTimestampPeerPacketSignatureWhitelist = TaskManager.TaskManager.CurrentTimestampSecond + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalPassedPeerPacketSignature = 0;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalValidPacket = 0;
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
        public static bool CheckPeerClientInitializationStatus(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, CancellationTokenSource cancellation)
        {
            if (!peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
                return false;

            if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerIp.IsNullOrEmpty(false, out _) ||
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerUniqueId.IsNullOrEmpty(false, out _) ||
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerPort <= 0 ||
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKey == null ||
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKeyIv == null ||
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPublicKey.IsNullOrEmpty(false, out _) ||
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey.IsNullOrEmpty(false, out _) ||
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPacketEncryptionKey == null ||
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPacketEncryptionKeyIv == null ||
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPublicKey.IsNullOrEmpty(false, out _))
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
        public static bool ComparePeerPacketPublicKey(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, string peerPublicKeyReceived, CancellationTokenSource cancellation)
        {
            if (!peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation) ||
                peerPublicKeyReceived.IsNullOrEmpty(false, out _))
                return false;

            return peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPublicKey == peerPublicKeyReceived;
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
        public static void InputPeerClientInvalidPacket(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation)
        {
            long currentTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;

            if (!peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
            {
                if (peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastBadStatePacket + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats <= currentTimestamp)
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket = 0;

                UpdatePeerLastBadStatePacket(peerDatabase, peerIp, peerUniqueId, peerFirewallSettingObject, cancellation);
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket++;

                if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket >= peerNetworkSettingObject.PeerMaxInvalidPacket)
                    SetPeerBanState(peerDatabase, peerIp, peerUniqueId, peerNetworkSettingObject, peerFirewallSettingObject, cancellation);

            }
        }

        /// <summary>
        /// Input the amount of no packet provided per connection opened.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public static void InputPeerClientNoPacketConnectionOpened(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation)
        {

            if (!peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
            {
                if (peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastBadStatePacket + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats <= TaskManager.TaskManager.CurrentTimestampSecond)
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalNoPacketConnectionAttempt = 0;

                UpdatePeerLastBadStatePacket(peerDatabase, peerIp, peerUniqueId, peerFirewallSettingObject, cancellation);

                peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalNoPacketConnectionAttempt++;

                if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalNoPacketConnectionAttempt >= peerNetworkSettingObject.PeerMaxNoPacketPerConnectionOpened)
                    SetPeerDeadState(peerDatabase, peerIp, peerUniqueId, peerNetworkSettingObject, peerFirewallSettingObject, cancellation);
            }
        }

        /// <summary>
        /// Increment attempt to connect to a peer.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public static void InputPeerClientAttemptConnect(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation)
        {
            if (!peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
            {
                if (peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastBadStatePacket + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats <= TaskManager.TaskManager.CurrentTimestampSecond)
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalAttemptConnection = 0;

                UpdatePeerLastBadStatePacket(peerDatabase, peerIp, peerUniqueId, peerFirewallSettingObject, cancellation);

                peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalAttemptConnection++;
                if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalAttemptConnection >= peerNetworkSettingObject.PeerMaxAttemptConnection)
                    SetPeerDeadState(peerDatabase, peerIp, peerUniqueId, peerNetworkSettingObject, peerFirewallSettingObject, cancellation);
            }
        }

        /// <summary>
        /// Increment valid packet counter to a peer.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        public static void InputPeerClientValidPacket(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, CancellationTokenSource cancellation)
        {
            if (peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
            {
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastValidPacket = TaskManager.TaskManager.CurrentTimestampSecond + peerNetworkSettingObject.PeerMaxDelayKeepAliveStats;
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalValidPacket++;
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalNoPacketConnectionAttempt = 0;

                if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalValidPacket >= peerNetworkSettingObject.PeerMaxInvalidPacket)
                {
                    if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket > 0)
                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket--;
                    else if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket < 0)
                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket = 0;
                }
            }
        }

        /// <summary>
        /// Update the peer last packet received
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        public static void UpdatePeerClientLastPacketReceived(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, long peerLastTimestampSignatureWhitelist, CancellationTokenSource cancellation)
        {
            if (peerIp.IsNullOrEmpty(false, out _) || 
                peerUniqueId.IsNullOrEmpty(false, out _) ||
                cancellation == null)
                return;

            if (peerDatabase[peerIp, peerUniqueId, cancellation] == null)
                return;

            try
            {
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastPacketReceivedTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalNoPacketConnectionAttempt = 0;
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientLastTimestampPeerPacketSignatureWhitelist = peerLastTimestampSignatureWhitelist;
            }
            catch
            {

            }
        }

        #endregion

        #region Manage peer states.

        /// <summary>
        /// CloseDiskCache peer state.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="unBanOrUnDead"></param>
        public static void CleanPeerState(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, bool unBanOrUnDead, CancellationTokenSource cancellation)
        {
            if (peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
            {
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerStatus = ClassPeerEnumStatus.PEER_ALIVE;
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalAttemptConnection = 0;
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket = 0;
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastBadStatePacket = 0;
                if (unBanOrUnDead)
                {
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerBanDate = 0;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastDeadTimestamp = 0;
                }
                if (!unBanOrUnDead)
                {
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientLastTimestampPeerPacketSignatureWhitelist = 0;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalPassedPeerPacketSignature = 0;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalNoPacketConnectionAttempt = 0;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalAttemptConnection = 0;
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
        public static void SetPeerBanState(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation)
        {

            peerDatabase[peerIp, peerUniqueId, cancellation].PeerStatus = ClassPeerEnumStatus.PEER_BANNED;
            peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalInvalidPacket = peerNetworkSettingObject.PeerMaxInvalidPacket;
            peerDatabase[peerIp, peerUniqueId, cancellation].PeerBanDate = TaskManager.TaskManager.CurrentTimestampSecond + peerNetworkSettingObject.PeerBanDelay;
            peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalValidPacket = 0;
            peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalPassedPeerPacketSignature = 0;
            peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientLastTimestampPeerPacketSignatureWhitelist = 0;
            ClassLog.WriteLine("Peer: " + peerIp + " | Unique ID: " + peerUniqueId + " state has been set to banned temporaly.", ClassEnumLogLevelType.LOG_LEVEL_PEER_MANAGER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);

        }

        /// <summary>
        /// Set a peer dead.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public static void SetPeerDeadState(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSettingObject, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation)
        {
            if (!peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
            {
                if (!peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerStatus != ClassPeerEnumStatus.PEER_DEAD)
                {
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerStatus = ClassPeerEnumStatus.PEER_DEAD;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalAttemptConnection = 0;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerTotalNoPacketConnectionAttempt = 0;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastDeadTimestamp = TaskManager.TaskManager.CurrentTimestampSecond + peerNetworkSettingObject.PeerDeadDelay;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientLastTimestampPeerPacketSignatureWhitelist = 0;

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
        public static bool PeerHasSeedRank(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, CancellationTokenSource cancellation, out string numericPublicKeyOut, out long timestampRankDelay)
        {
            timestampRankDelay = 0;
            numericPublicKeyOut = string.Empty;

            if (!peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
                return false;

            numericPublicKeyOut = peerDatabase[peerIp, peerUniqueId, cancellation].PeerNumericPublicKey;

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
        public static void UpdatePeerLastBadStatePacket(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, ClassPeerFirewallSettingObject peerFirewallSettingObject, CancellationTokenSource cancellation)
        {
            if (!peerDatabase.ContainsPeerUniqueId(peerIp, peerUniqueId, cancellation))
            {
                if (peerFirewallSettingObject.PeerEnableFirewallLink)
                    ClassPeerFirewallManager.InsertInvalidPacket(peerIp);
            }
            else
            {
                peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastBadStatePacket = TaskManager.TaskManager.CurrentTimestampSecond;
                //peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalValidPacket = 0;
                //peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientTotalPassedPeerPacketSignature = 0;
                //peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientLastTimestampPeerPacketSignatureWhitelist = 0;
            }

        }

        #endregion
    }
}
