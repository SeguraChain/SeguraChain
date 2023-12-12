using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Instance.Node.Network.Database.Object;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Status;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Request;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Other.Object.SHA3;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Database.Manager
{

    public class ClassPeerKeysManager
    {
        private const int RandomWordKeySize = 32;

        /// <summary>
        /// Generate Peer KeysBlockObject. KeysBlockObject are different between each peers target.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerPort"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="cancellation"></param>
        /// <param name="peerNetworkSettingObject"></param>
        /// <param name="forceUpdate"></param>
        public static async Task<bool> UpdatePeerInternalKeys(ClassPeerDatabase peerDatabase, string peerIp, int peerPort, string peerUniqueId, CancellationTokenSource cancellation, ClassPeerNetworkSettingObject peerNetworkSettingObject, bool forceUpdate)
        {
            bool result = false;

            try
            {
                long currentTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;

                if (!peerDatabase.ContainsPeerIp(peerIp, cancellation))
                    peerDatabase.TryAddPeerIp(peerIp, cancellation);

                if (peerDatabase[peerIp, cancellation].ContainsKey(peerUniqueId))
                {
                    if (peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternTimestampKeyGenerated + peerNetworkSettingObject.PeerMaxAuthKeysExpire < currentTimestamp || forceUpdate)
                    {
                        if (ClassAes.GenerateKey(ClassUtility.GetRandomWord(RandomWordKeySize).GetByteArray(true), true, out peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKey))
                        {
                            peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKeyIv = ClassAes.GenerateIv(peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKey, BlockchainSetting.PeerIvIterationCount);
                            peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey = GeneratePeerPrivateKey();
                            peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPublicKey = GeneratePeerPublicKeyFromPrivateKey(peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey);
                            peerDatabase[peerIp, peerUniqueId, cancellation].PeerPort = peerPort;
                            peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternTimestampKeyGenerated = currentTimestamp;

                            if (peerDatabase[peerIp, peerUniqueId, cancellation].GetInternCryptoStreamObject == null)
                            {
                                peerDatabase[peerIp, peerUniqueId, cancellation].GetInternCryptoStreamObject = new ClassPeerCryptoStreamObject(peerIp, peerUniqueId, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKeyIv, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPublicKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey, cancellation);
                                result = true;
                            }
                            else
                                result = await peerDatabase[peerIp, peerUniqueId, cancellation].GetInternCryptoStreamObject.UpdateEncryptionStream(peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKeyIv, peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPublicKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey, cancellation);
                        }
                    }
                }
                else
                {
                    if (peerDatabase[peerIp, cancellation].TryAdd(peerUniqueId, new ClassPeerObject()
                    {
                        PeerPort = peerPort,
                        PeerIp = peerIp,
                        PeerUniqueId = peerUniqueId
                    }))
                    {
                        if (ClassAes.GenerateKey(ClassUtility.GetRandomWord(RandomWordKeySize).GetByteArray(true), true, out peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKey))
                        {

                            peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKeyIv = ClassAes.GenerateIv(peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKey, BlockchainSetting.PeerIvIterationCount);
                            peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey = GeneratePeerPrivateKey();
                            peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPublicKey = GeneratePeerPublicKeyFromPrivateKey(peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey);
                            peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternTimestampKeyGenerated = currentTimestamp;

                            peerDatabase[peerIp, peerUniqueId, cancellation].GetInternCryptoStreamObject = new ClassPeerCryptoStreamObject(peerIp, peerUniqueId, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPacketEncryptionKeyIv, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPublicKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey, cancellation); ;

                            result = true;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return result;
        }

        /// <summary>
        /// Generate a peer object.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerPort"></param>
        /// <param name="peerUniqueId"></param>
        /// 
        /// <returns></returns>
        public static ClassPeerObject GeneratePeerObject(string peerIp, int peerPort, string peerUniqueId, CancellationTokenSource cancellation)
        {
            ClassPeerObject peerObject = new ClassPeerObject { PeerIp = peerIp, PeerPort = peerPort, PeerUniqueId = peerUniqueId, PeerStatus = ClassPeerEnumStatus.PEER_ALIVE };

            if (ClassAes.GenerateKey(ClassUtility.GetRandomWord(RandomWordKeySize).GetByteArray(true), true, out peerObject.PeerInternPacketEncryptionKey))
            {
                peerObject.PeerInternPacketEncryptionKeyIv = ClassAes.GenerateIv(peerObject.PeerInternPacketEncryptionKey, BlockchainSetting.PeerIvIterationCount);
                peerObject.PeerInternPrivateKey = GeneratePeerPrivateKey();
                peerObject.PeerInternPublicKey = GeneratePeerPublicKeyFromPrivateKey(peerObject.PeerInternPrivateKey);
                peerObject.PeerInternTimestampKeyGenerated = TaskManager.TaskManager.CurrentTimestampSecond;
                peerObject.GetInternCryptoStreamObject = new ClassPeerCryptoStreamObject(peerIp, peerUniqueId, peerObject.PeerInternPacketEncryptionKey, peerObject.PeerInternPacketEncryptionKeyIv, peerObject.PeerInternPublicKey, peerObject.PeerInternPrivateKey, cancellation);
            }

            return peerObject;
        }

        /// <summary>
        /// Update Peer KeysBlockObject received on Peer Network Server.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="sendAskPeerAuthKeysObject"></param>
        /// <param name="cancellation"></param>
        public static async Task<bool> UpdatePeerKeysReceivedNetworkServer(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, ClassPeerPacketSendAskPeerAuthKeys sendAskPeerAuthKeysObject, CancellationTokenSource cancellation)
        {
            try
            {
                if (sendAskPeerAuthKeysObject.AesEncryptionIv == null || sendAskPeerAuthKeysObject.AesEncryptionKey == null ||
                    sendAskPeerAuthKeysObject.PublicKey.IsNullOrEmpty(false, out _))
                    return false;


                if (peerDatabase.ContainsPeerIp(peerIp, cancellation))
                {
                    if (peerDatabase[peerIp, cancellation].ContainsKey(peerUniqueId))
                    {

                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPacketEncryptionKey = sendAskPeerAuthKeysObject.AesEncryptionKey;
                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPacketEncryptionKeyIv = sendAskPeerAuthKeysObject.AesEncryptionIv;
                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPublicKey = sendAskPeerAuthKeysObject.PublicKey;
                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerPort = sendAskPeerAuthKeysObject.PeerPort;
                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerIsPublic = sendAskPeerAuthKeysObject.PeerIsPublic;
                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerUniqueId = peerUniqueId;
                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerNumericPublicKey = sendAskPeerAuthKeysObject.NumericPublicKey;
                        peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastValidPacket = TaskManager.TaskManager.CurrentTimestampSecond;

                        if (sendAskPeerAuthKeysObject.AesEncryptionKey != null && sendAskPeerAuthKeysObject.AesEncryptionIv != null)
                        {
                            if (peerDatabase[peerIp, peerUniqueId, cancellation].GetClientCryptoStreamObject == null)
                            {
                                peerDatabase[peerIp, peerUniqueId, cancellation].GetClientCryptoStreamObject = new ClassPeerCryptoStreamObject(peerIp, peerUniqueId, sendAskPeerAuthKeysObject.AesEncryptionKey, sendAskPeerAuthKeysObject.AesEncryptionIv, sendAskPeerAuthKeysObject.PublicKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey, cancellation);
                                return true;
                            }
                            else
                                return await peerDatabase[peerIp, peerUniqueId, cancellation].GetClientCryptoStreamObject.UpdateEncryptionStream(sendAskPeerAuthKeysObject.AesEncryptionKey, sendAskPeerAuthKeysObject.AesEncryptionIv, sendAskPeerAuthKeysObject.PublicKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey, cancellation);
                        }
                    }
                }
                else
                {
                    if (peerDatabase.TryAddPeerIp(peerIp, cancellation))
                    {
                        if (peerDatabase[peerIp, cancellation].TryAdd(peerUniqueId, new ClassPeerObject()
                        {
                            PeerUniqueId = peerUniqueId,
                            PeerClientPacketEncryptionKey = sendAskPeerAuthKeysObject.AesEncryptionKey,
                            PeerClientPacketEncryptionKeyIv = sendAskPeerAuthKeysObject.AesEncryptionIv,
                            PeerClientPublicKey = sendAskPeerAuthKeysObject.PublicKey,
                            PeerPort = sendAskPeerAuthKeysObject.PeerPort,
                            PeerIp = peerIp,
                            PeerNumericPublicKey = sendAskPeerAuthKeysObject.NumericPublicKey,
                            PeerIsPublic = sendAskPeerAuthKeysObject.PeerIsPublic,
                            PeerLastValidPacket = TaskManager.TaskManager.CurrentTimestampSecond
                        }))
                        {
                            if (sendAskPeerAuthKeysObject.AesEncryptionKey != null && sendAskPeerAuthKeysObject.AesEncryptionIv != null)
                            {
                                peerDatabase[peerIp, peerUniqueId, cancellation].GetClientCryptoStreamObject = new ClassPeerCryptoStreamObject(peerIp, peerUniqueId, sendAskPeerAuthKeysObject.AesEncryptionKey, sendAskPeerAuthKeysObject.AesEncryptionIv, peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPublicKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey, cancellation);

                                return true;
                            }
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
        /// Update Peer KeysBlockObject received on Peer Task Sync.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="sendPeerAuthKeysObject"></param>
        /// <param name="cancellation"></param>
        /// <param name="peerNetworkSetting"></param>
        public static async Task<bool> UpdatePeerKeysReceiveTaskSync(ClassPeerDatabase peerDatabase, string peerIp, string peerUniqueId, ClassPeerPacketSendPeerAuthKeys sendPeerAuthKeysObject, CancellationTokenSource cancellation, ClassPeerNetworkSettingObject peerNetworkSetting)
        {
            if (sendPeerAuthKeysObject.AesEncryptionIv == null || sendPeerAuthKeysObject.AesEncryptionKey == null ||
                sendPeerAuthKeysObject.NumericPublicKey.IsNullOrEmpty(false, out _) || sendPeerAuthKeysObject.PublicKey.IsNullOrEmpty(false, out _))
                return false;

            bool peerUniqueIdExist = false;


            bool result = true;
            if (peerDatabase.ContainsPeerIp(peerIp, cancellation))
            {
                if (peerDatabase[peerIp, cancellation].ContainsKey(peerUniqueId))
                {
                    peerUniqueIdExist = true;



                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPacketEncryptionKey = sendPeerAuthKeysObject.AesEncryptionKey;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPacketEncryptionKeyIv = sendPeerAuthKeysObject.AesEncryptionIv;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPublicKey = sendPeerAuthKeysObject.PublicKey;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerIp = peerIp;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerUniqueId = peerUniqueId;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerPort = sendPeerAuthKeysObject.PeerPort;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerNumericPublicKey = sendPeerAuthKeysObject.NumericPublicKey;
                    peerDatabase[peerIp, peerUniqueId, cancellation].PeerLastValidPacket = TaskManager.TaskManager.CurrentTimestampSecond;

                    if (sendPeerAuthKeysObject.AesEncryptionKey != null && sendPeerAuthKeysObject.AesEncryptionIv != null)
                    {
                        if (peerDatabase[peerIp, peerUniqueId, cancellation].GetClientCryptoStreamObject == null)
                            peerDatabase[peerIp, peerUniqueId, cancellation].GetClientCryptoStreamObject = new ClassPeerCryptoStreamObject(peerIp, peerUniqueId, sendPeerAuthKeysObject.AesEncryptionKey, sendPeerAuthKeysObject.AesEncryptionIv, peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPublicKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey, cancellation);
                        else
                            result = await peerDatabase[peerIp, peerUniqueId, cancellation].GetClientCryptoStreamObject.UpdateEncryptionStream(sendPeerAuthKeysObject.AesEncryptionKey, sendPeerAuthKeysObject.AesEncryptionIv, sendPeerAuthKeysObject.PublicKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey, cancellation);
                    }

                }
            }
            else
                peerDatabase.TryAddPeerIp(peerIp, cancellation);

            if (!peerUniqueIdExist)
            {
                if (peerDatabase[peerIp, cancellation].TryAdd(peerUniqueId, new ClassPeerObject
                {
                    PeerClientPacketEncryptionKey = sendPeerAuthKeysObject.AesEncryptionKey,
                    PeerClientPacketEncryptionKeyIv = sendPeerAuthKeysObject.AesEncryptionIv,
                    PeerClientPublicKey = sendPeerAuthKeysObject.PublicKey,
                    PeerPort = sendPeerAuthKeysObject.PeerPort,
                    PeerNumericPublicKey = sendPeerAuthKeysObject.NumericPublicKey,
                    PeerUniqueId = peerUniqueId,
                    PeerIp = peerIp,
                    PeerLastValidPacket = TaskManager.TaskManager.CurrentTimestampSecond
                }))
                {

                    if (sendPeerAuthKeysObject.AesEncryptionKey != null &&
                        sendPeerAuthKeysObject.AesEncryptionIv != null &&
                        peerDatabase[peerIp, peerUniqueId, cancellation].GetClientCryptoStreamObject != null)
                        peerDatabase[peerIp, peerUniqueId, cancellation].GetClientCryptoStreamObject = new ClassPeerCryptoStreamObject(peerIp, peerUniqueId, sendPeerAuthKeysObject.AesEncryptionKey, sendPeerAuthKeysObject.AesEncryptionIv, peerDatabase[peerIp, peerUniqueId, cancellation].PeerClientPublicKey, peerDatabase[peerIp, peerUniqueId, cancellation].PeerInternPrivateKey, cancellation);
                }
            }

            return result;
        }

        /// <summary>
        /// Generate a peer unique id.
        /// </summary>
        /// <returns></returns>
        public static string GeneratePeerUniqueId()
        {
            return ClassUtility.GenerateSha3512FromString(ClassUtility.GetRandomWord(RandomWordKeySize));
        }

        #region Functions for generate private/public key for a peer.

        /// <summary>
        /// Generate a simple private for peer.
        /// </summary>
        public static string GeneratePeerPrivateKey()
        {
            byte[] privateKeyBytes;
            string randomWord = ClassUtility.GetRandomWord(RandomWordKeySize);

            using (ClassSha3512DigestDisposable sha512 = new ClassSha3512DigestDisposable())
                sha512.Compute(randomWord.GetByteArray(true), out privateKeyBytes);

            ClassUtility.InsertBlockchainVersionToByteArray(privateKeyBytes, out var privateKeyHex);

            return ClassBase58.EncodeWithCheckSum(ClassUtility.GetByteArrayFromHexString(privateKeyHex));
        }

        /// <summary>
        /// Generate a public key from a private key.
        /// </summary>
        /// <param name="privateKeyWif"></param>
        /// <returns>Return a public key WIF.</returns>
        public static string GeneratePeerPublicKeyFromPrivateKey(string privateKeyWif)
        {
            if (privateKeyWif.IsNullOrEmpty(false, out _))
                return null;

            var curve = SecNamedCurves.GetByName(BlockchainSetting.CurveName);
            var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

            ECPoint publicKeyOne = curve.G.Multiply(new BigInteger(ClassBase58.DecodeWithCheckSum(privateKeyWif, true)));


            return ClassBase58.EncodeWithCheckSum(new ECPublicKeyParameters(publicKeyOne, domain).Q.GetEncoded());
        }

        #endregion
    }
}
