using Newtonsoft.Json;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Status;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Database.Object
{
    public class ClassPeerObject
    {
        #region Peer intern side.

        public byte[] PeerInternPacketEncryptionKey;
        public byte[] PeerInternPacketEncryptionKeyIv;
        public string PeerInternPrivateKey; // Used for sign encrypted packets sent to a peer.
        public string PeerInternPublicKey; // Used for check signature of encrypted packets sent to another peer.
        public long PeerInternTimestampKeyGenerated;

        #endregion

        #region Peer client side.

        public long PeerTimestampInsert;
        public string PeerIp;
        public int PeerPort;
        public string PeerUniqueId;
        public ClassPeerEnumStatus PeerStatus;
        public byte[] PeerClientPacketEncryptionKey;
        public byte[] PeerClientPacketEncryptionKeyIv;
        public string PeerClientPublicKey; // Used for sign encrypted packets.
        public bool PeerIsPublic;
        public string PeerNumericPublicKey; // Used by peer with the seed node rank.
        public long PeerTimestampSignatureWhitelist; // Used to know when it's necessary to sign a packet.
        public long PeerClientLastBlockHeight;

        #endregion

        #region Peer stats.

        public long PeerLastUpdateOfKeysTimestamp;
        public int PeerClientTotalValidPacket;
        public long PeerClientLastTimestampPeerPacketSignatureWhitelist;
        public int PeerClientTotalPassedPeerPacketSignature;
        public int PeerTotalInvalidPacket;
        public int PeerTotalAttemptConnection;
        public int PeerTotalNoPacketConnectionAttempt;
        public long PeerLastValidPacket;
        public long PeerLastBadStatePacket;
        public long PeerLastPacketReceivedTimestamp;
        public long PeerBanDate;
        public long PeerLastDeadTimestamp;

        #endregion


        [JsonIgnore]
        public bool OnUpdateAuthKeys;

        [JsonIgnore]
        public ClassPeerCryptoStreamObject GetClientCryptoStreamObject { get; set; }

        [JsonIgnore]
        public ClassPeerCryptoStreamObject GetInternCryptoStreamObject { get; set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassPeerObject()
        {
            PeerTimestampInsert = TaskManager.TaskManager.CurrentTimestampSecond;
            PeerLastPacketReceivedTimestamp = TaskManager.TaskManager.CurrentTimestampSecond;
        }

    }
}
