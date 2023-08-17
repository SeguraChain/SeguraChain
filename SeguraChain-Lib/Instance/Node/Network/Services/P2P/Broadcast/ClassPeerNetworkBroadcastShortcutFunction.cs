using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Instance.Node.Network.Database.Manager;
using SeguraChain_Lib.Instance.Node.Network.Database.Object;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.ClientConnect.Object;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BigInteger = Org.BouncyCastle.Math.BigInteger;


namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast
{
    public class ClassPeerNetworkBroadcastShortcutFunction
    {
        /// <summary>
        /// Build and send a packet, await the expected packet response to receive.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="peerNetworkClientSyncObject"></param>
        /// <param name="packetType"></param>
        /// <param name="packetToSend"></param>
        /// <param name="peerIpTarget"></param>
        /// <param name="peerUniqueIdTarget"></param>
        /// <param name="peerNetworkSetting"></param>
        /// <param name="packetTypeExpected"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<R> SendBroadcastPacket<T, R>(ClassPeerNetworkClientSyncObject peerNetworkClientSyncObject, ClassPeerEnumPacketSend packetType, T packetToSend, string peerIpTarget, string peerUniqueIdTarget, ClassPeerNetworkSettingObject peerNetworkSetting, ClassPeerEnumPacketResponse packetTypeExpected, CancellationTokenSource cancellation)
        {

            ClassPeerObject peerObject = ClassPeerDatabase.GetPeerObject(peerIpTarget, peerUniqueIdTarget);

            if (peerObject == null)
                return default(R);

            ClassPeerPacketSendObject packetSendObject = new ClassPeerPacketSendObject(peerNetworkSetting.PeerUniqueId,
            peerObject.PeerInternPublicKey,
            peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
            {
                PacketOrder = packetType,
                PacketContent = ClassUtility.SerializeData(packetToSend)
            };
            packetSendObject.PacketHash = ClassUtility.GenerateSha256FromString(packetSendObject.PacketContent + packetSendObject.PacketOrder);
            packetSendObject.PacketSignature = ClassWalletUtility.WalletGenerateSignature(peerObject.PeerInternPrivateKey, packetSendObject.PacketHash);


            if (packetSendObject == null)
                return default(R);

            if (!await peerNetworkClientSyncObject.TrySendPacketToPeerTarget(packetSendObject, true, peerObject.PeerPort, peerUniqueIdTarget, cancellation, packetTypeExpected, false, false))
                return default(R);

            if (peerNetworkClientSyncObject.PeerPacketReceived == null)
                return default(R);

            if (peerNetworkClientSyncObject.PeerPacketReceived.PacketOrder != packetTypeExpected)
                return default(R);

            bool peerPacketSignatureValid = ClassPeerCheckManager.CheckPeerClientWhitelistStatus(peerIpTarget, peerUniqueIdTarget, peerNetworkSetting) ? true : ClassWalletUtility.WalletCheckSignature(peerNetworkClientSyncObject.PeerPacketReceived.PacketHash, peerNetworkClientSyncObject.PeerPacketReceived.PacketSignature, ClassPeerDatabase.DictionaryPeerDataObject[peerIpTarget][peerUniqueIdTarget].PeerClientPublicKey);

            if (!peerPacketSignatureValid)
                return default(R);


            byte[] packetTupleDecrypted = await peerObject.GetInternCryptoStreamObject.DecryptDataProcess(ClassUtility.GetByteArrayFromHexString(peerNetworkClientSyncObject.PeerPacketReceived.PacketContent), cancellation);

            if (packetTupleDecrypted == null)
            {
#if DEBUG
                Debug.WriteLine("Failed to decrypt packet data from " + peerIpTarget);
#endif
                return default(R);
            }


            if (packetTupleDecrypted == null)
                return default(R);


            if (!ClassUtility.TryDeserialize(packetTupleDecrypted.GetStringFromByteArrayUtf8(), out R peerPacketReceived))
            {
#if DEBUG
                Debug.WriteLine("Failed to deserialize packet content: " + packetTupleDecrypted.GetStringFromByteArrayUtf8());
#endif
                return default(R);
            }

            if (EqualityComparer<R>.Default.Equals(peerPacketReceived, default))
                return default(R);


            return peerPacketReceived;
        }

        #region Static peer packet signing/encryption function.

        /// <summary>
        /// Build the packet content encrypted with peer auth keys and the internal private key assigned to the peer target for sign the packet.
        /// </summary>
        /// <param name="sendObject"></param>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<ClassPeerPacketSendObject> BuildSignedPeerSendPacketObject(ClassPeerPacketSendObject sendObject, string peerIp, string peerUniqueId, bool forceSignature, ClassPeerNetworkSettingObject peerNetworkSettingObject, CancellationTokenSource cancellation)
        {
            if (ClassPeerDatabase.ContainsPeer(peerIp, peerUniqueId))
            {
                byte[] packetContentEncrypted;

                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetInternCryptoStreamObject != null)
                {
                    if (cancellation == null)
                    {
                        if (!ClassAes.EncryptionProcess(sendObject.PacketContent.GetByteArray(), ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKeyIv, out packetContentEncrypted))
                            return null;
                    }
                    else
                    {
                        packetContentEncrypted = await ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetInternCryptoStreamObject.EncryptDataProcess(sendObject.PacketContent.GetByteArray(), cancellation);

                        if (packetContentEncrypted == null)
                        {
                            if (!ClassAes.EncryptionProcess(sendObject.PacketContent.GetByteArray(), ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKeyIv, out packetContentEncrypted))
                                return null;
                        }
                    }
                }
                else
                {
                    if (!ClassAes.EncryptionProcess(sendObject.GetPacketData(), ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKeyIv, out packetContentEncrypted))
                        return null;
                }


                sendObject.PacketContent = ClassUtility.GetHexStringFromByteArray(packetContentEncrypted);
                sendObject.PacketHash = ClassUtility.GenerateSha256FromString(sendObject.PacketContent + sendObject.PacketOrder);

                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId))
                {
                    if (ClassPeerCheckManager.CheckPeerClientWhitelistStatus(peerIp, peerUniqueId, peerNetworkSettingObject) || forceSignature)
                    {
                        if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetClientCryptoStreamObject != null && cancellation != null)
                            sendObject.PacketSignature = await ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetClientCryptoStreamObject.DoSignatureProcess(sendObject.PacketHash, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, cancellation);
                        else
                        {
                            var signer = SignerUtilities.GetSigner(BlockchainSetting.SignerNameNetwork);

                            signer.Init(true, new ECPrivateKeyParameters(new BigInteger(ClassBase58.DecodeWithCheckSum(ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, true)), ClassWalletUtility.ECDomain));

                            signer.BlockUpdate(ClassUtility.GetByteArrayFromHexString(sendObject.PacketHash), 0, sendObject.PacketHash.Length / 2);

                            sendObject.PacketSignature = Convert.ToBase64String(signer.GenerateSignature());

                            // Reset.
                            signer.Reset();
                        }
                    }
                }
            }
            return sendObject;
        }

        #endregion

        #region Check Peer numeric keys signatures on packets

        /// <summary>
        /// Check the peer seed numeric packet signature, compare with the list of peer listed by sovereign updates.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="objectData"></param>
        /// <param name="packetNumericHash"></param>
        /// <param name="packetNumericSignature"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="cancellation"></param>
        /// <param name="numericPublicKeyOut"></param>
        /// <returns></returns>
        public static bool CheckPeerSeedNumericPacketSignature<T>(string peerIp, string peerUniqueId, T objectData, string packetNumericHash, string packetNumericSignature, ClassPeerNetworkSettingObject peerNetworkSettingObject, CancellationTokenSource cancellation, out string numericPublicKeyOut)
        {
            // Default value.
            numericPublicKeyOut = string.Empty;

            if (!peerNetworkSettingObject.PeerEnableSovereignPeerVote || packetNumericHash.IsNullOrEmpty(false, out _) || packetNumericSignature.IsNullOrEmpty(false, out _) || !ClassPeerCheckManager.PeerHasSeedRank(peerIp, peerUniqueId, out numericPublicKeyOut, out _))
                return false;

            return ClassPeerCheckManager.CheckPeerSeedNumericPacketSignature(ClassUtility.SerializeData(objectData), packetNumericHash, packetNumericSignature, numericPublicKeyOut, cancellation);
        }

        #endregion
    }
}
