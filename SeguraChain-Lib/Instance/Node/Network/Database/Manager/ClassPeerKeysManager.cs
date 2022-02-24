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
        public static bool UpdatePeerInternalKeys(string peerIp, int peerPort, string peerUniqueId, CancellationTokenSource cancellation, ClassPeerNetworkSettingObject peerNetworkSettingObject, bool forceUpdate)
        {
            bool result = false;

            long currentTimestamp = ClassUtility.GetCurrentTimestampInSecond();

            if (!ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(peerIp))
                ClassPeerDatabase.DictionaryPeerDataObject.Add(peerIp, new ConcurrentDictionary<string, ClassPeerObject>());

            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId))
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternTimestampKeyGenerated + peerNetworkSettingObject.PeerMaxAuthKeysExpire < currentTimestamp || forceUpdate)
                {
                    if (ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringAscii(ClassUtility.GetRandomWord(RandomWordKeySize)), true, out ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey))
                    {
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKeyIv = ClassAes.GenerateIv(ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey, BlockchainSetting.PeerIvIterationCount);
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey = GeneratePeerPrivateKey();
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey = GeneratePeerPublicKeyFromPrivateKey(ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey);
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerPort = peerPort;
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternTimestampKeyGenerated = currentTimestamp;

                        if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetInternCryptoStreamObject == null)
                            ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetInternCryptoStreamObject = new ClassPeerCryptoStreamObject(ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKeyIv, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, cancellation);
                        else
                            ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetInternCryptoStreamObject.UpdateEncryptionStream(ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKeyIv, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, cancellation);
                    }
                }
                result = true;
            }
            else
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].TryAdd(peerUniqueId, new ClassPeerObject()
                {
                    PeerPort = peerPort,
                    PeerIp = peerIp,
                    PeerUniqueId = peerUniqueId
                }))
                {
                    if (ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringAscii(ClassUtility.GetRandomWord(RandomWordKeySize)), true, out ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey))
                    {

                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKeyIv = ClassAes.GenerateIv(ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey, BlockchainSetting.PeerIvIterationCount);
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey = GeneratePeerPrivateKey();
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey = GeneratePeerPublicKeyFromPrivateKey(ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey);
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternTimestampKeyGenerated = currentTimestamp;

                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetInternCryptoStreamObject = new ClassPeerCryptoStreamObject(ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPacketEncryptionKeyIv, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, cancellation);

                        result = true;
                    }
                }
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

            if (ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringAscii(ClassUtility.GetRandomWord(RandomWordKeySize)), true, out peerObject.PeerInternPacketEncryptionKey))
            {
                peerObject.PeerInternPacketEncryptionKeyIv = ClassAes.GenerateIv(peerObject.PeerInternPacketEncryptionKey, BlockchainSetting.PeerIvIterationCount);
                peerObject.PeerInternPrivateKey = GeneratePeerPrivateKey();
                peerObject.PeerInternPublicKey = GeneratePeerPublicKeyFromPrivateKey(peerObject.PeerInternPrivateKey);
                peerObject.PeerInternTimestampKeyGenerated = ClassUtility.GetCurrentTimestampInSecond();
                peerObject.GetInternCryptoStreamObject = new ClassPeerCryptoStreamObject(peerObject.PeerInternPacketEncryptionKey, peerObject.PeerInternPacketEncryptionKeyIv, peerObject.PeerInternPublicKey, peerObject.PeerInternPrivateKey, cancellation);
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
        public static bool UpdatePeerKeysReceivedNetworkServer(string peerIp, string peerUniqueId, ClassPeerPacketSendAskPeerAuthKeys sendAskPeerAuthKeysObject, CancellationTokenSource cancellation)
        {
            bool peerUniqueIdExist = false;


            if (ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(peerIp))
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId))
                {
                    peerUniqueIdExist = true;


                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPacketEncryptionKey = sendAskPeerAuthKeysObject.AesEncryptionKey;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPacketEncryptionKeyIv = sendAskPeerAuthKeysObject.AesEncryptionIv;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPublicKey = sendAskPeerAuthKeysObject.PublicKey;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerPort = sendAskPeerAuthKeysObject.PeerPort;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIsPublic = sendAskPeerAuthKeysObject.PeerIsPublic;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerUniqueId = peerUniqueId;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerNumericPublicKey = sendAskPeerAuthKeysObject.NumericPublicKey;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastValidPacket = ClassUtility.GetCurrentTimestampInSecond();

                    if (sendAskPeerAuthKeysObject.AesEncryptionKey != null && sendAskPeerAuthKeysObject.AesEncryptionIv != null)
                    {
                        if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetClientCryptoStreamObject == null)
                            ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetClientCryptoStreamObject = new ClassPeerCryptoStreamObject(sendAskPeerAuthKeysObject.AesEncryptionKey, sendAskPeerAuthKeysObject.AesEncryptionIv, sendAskPeerAuthKeysObject.PublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, cancellation);
                        else
                            ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetClientCryptoStreamObject.UpdateEncryptionStream(sendAskPeerAuthKeysObject.AesEncryptionKey, sendAskPeerAuthKeysObject.AesEncryptionIv, sendAskPeerAuthKeysObject.PublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, cancellation);
                    }
                }
            }
            else
                ClassPeerDatabase.DictionaryPeerDataObject.Add(peerIp, new ConcurrentDictionary<string, ClassPeerObject>());

            if (!peerUniqueIdExist)
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].TryAdd(peerUniqueId, new ClassPeerObject()
                {
                    PeerUniqueId = peerUniqueId,
                    PeerClientPacketEncryptionKey = sendAskPeerAuthKeysObject.AesEncryptionKey,
                    PeerClientPacketEncryptionKeyIv = sendAskPeerAuthKeysObject.AesEncryptionIv,
                    PeerClientPublicKey = sendAskPeerAuthKeysObject.PublicKey,
                    PeerPort = sendAskPeerAuthKeysObject.PeerPort,
                    PeerIp = peerIp,
                    PeerNumericPublicKey = sendAskPeerAuthKeysObject.NumericPublicKey,
                    PeerIsPublic = sendAskPeerAuthKeysObject.PeerIsPublic,
                    PeerLastValidPacket = ClassUtility.GetCurrentTimestampInSecond()
                }))
                {
                    if (sendAskPeerAuthKeysObject.AesEncryptionKey != null && sendAskPeerAuthKeysObject.AesEncryptionIv != null)
                    {
                        peerUniqueIdExist = true;

                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetClientCryptoStreamObject = new ClassPeerCryptoStreamObject(sendAskPeerAuthKeysObject.AesEncryptionKey, sendAskPeerAuthKeysObject.AesEncryptionIv, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, cancellation);
                    }
                }
            }


            return peerUniqueIdExist;
        }

        /// <summary>
        /// Update Peer KeysBlockObject received on Peer Task Sync.
        /// </summary>
        /// <param name="peerIp"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="sendPeerAuthKeysObject"></param>
        /// <param name="cancellation"></param>
        /// <param name="peerNetworkSetting"></param>
        public static void UpdatePeerKeysReceiveTaskSync(string peerIp, string peerUniqueId, ClassPeerPacketSendPeerAuthKeys sendPeerAuthKeysObject, CancellationTokenSource cancellation, ClassPeerNetworkSettingObject peerNetworkSetting)
        {
            bool peerUniqueIdExist = false;

            if (ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(peerIp))
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].ContainsKey(peerUniqueId))
                {
                    peerUniqueIdExist = true;



                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPacketEncryptionKey = sendPeerAuthKeysObject.AesEncryptionKey;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPacketEncryptionKeyIv = sendPeerAuthKeysObject.AesEncryptionIv;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPublicKey = sendPeerAuthKeysObject.PublicKey;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIp = peerIp;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerUniqueId = peerUniqueId;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerPort = sendPeerAuthKeysObject.PeerPort;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerNumericPublicKey = sendPeerAuthKeysObject.NumericPublicKey;
                    ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerLastValidPacket = ClassUtility.GetCurrentTimestampInSecond();

                    if (sendPeerAuthKeysObject.AesEncryptionKey != null && sendPeerAuthKeysObject.AesEncryptionIv != null)
                    {
                        if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetClientCryptoStreamObject == null)
                            ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetClientCryptoStreamObject = new ClassPeerCryptoStreamObject(sendPeerAuthKeysObject.AesEncryptionKey, sendPeerAuthKeysObject.AesEncryptionIv, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, cancellation);
                        else
                            ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetClientCryptoStreamObject.UpdateEncryptionStream(sendPeerAuthKeysObject.AesEncryptionKey, sendPeerAuthKeysObject.AesEncryptionIv, sendPeerAuthKeysObject.PublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, cancellation);
                    }

                }
            }
            else
                ClassPeerDatabase.DictionaryPeerDataObject.Add(peerIp, new ConcurrentDictionary<string, ClassPeerObject>());

            if (!peerUniqueIdExist)
            {
                if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].TryAdd(peerUniqueId, new ClassPeerObject
                {
                    PeerClientPacketEncryptionKey = sendPeerAuthKeysObject.AesEncryptionKey,
                    PeerClientPacketEncryptionKeyIv = sendPeerAuthKeysObject.AesEncryptionIv,
                    PeerClientPublicKey = sendPeerAuthKeysObject.PublicKey,
                    PeerPort = sendPeerAuthKeysObject.PeerPort,
                    PeerNumericPublicKey = sendPeerAuthKeysObject.NumericPublicKey,
                    PeerUniqueId = peerUniqueId,
                    PeerIp = peerIp,
                    PeerLastValidPacket = ClassUtility.GetCurrentTimestampInSecond()
                }))
                {

                    if (sendPeerAuthKeysObject.AesEncryptionKey != null && sendPeerAuthKeysObject.AesEncryptionIv != null)
                        ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].GetClientCryptoStreamObject = new ClassPeerCryptoStreamObject(sendPeerAuthKeysObject.AesEncryptionKey, sendPeerAuthKeysObject.AesEncryptionIv, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerClientPublicKey, ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerInternPrivateKey, cancellation);
                }
            }
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
                sha512.Compute(ClassUtility.GetByteArrayFromStringAscii(randomWord), out privateKeyBytes);

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
