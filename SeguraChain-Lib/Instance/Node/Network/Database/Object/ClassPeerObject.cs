using Newtonsoft.Json;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Status;

namespace SeguraChain_Lib.Instance.Node.Network.Database.Object
{
    public class ClassPeerObject
    {
        #region Peer intern side.

        public byte[] PeerInternPacketEncryptionKey;
        public byte[] PeerInternPacketEncryptionKeyIv;
        public string PeerInternPrivateKey; // Used for sign encrypted packets sent to a peer.
        public string PeerInternPublicKey; // Used for check signature of encrypted packets sent to another peer.
        public byte[] _peerInternPacketBegin;
        public byte[] _peerInternPacketEnd;

        [JsonIgnore]
        public byte[] PeerInternPacketBegin
        {
            set { _peerInternPacketBegin = value; }
            get
            {
                if (_peerInternPacketBegin == null)
                    return ClassPeerPacketSetting.PacketSeperatorBegin;
                else return _peerInternPacketBegin;
            }
        }

        [JsonIgnore]
        public byte[] PeerInternPacketEnd
        {
            set { _peerInternPacketEnd = value; }
            get
            {
                if (_peerInternPacketEnd == null)
                    return ClassPeerPacketSetting.PacketSeperatorEnd;
                else return _peerInternPacketEnd;
            }
        }

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

        public byte[] _peerClientPacketBegin;
        public byte[] _peerClientPacketEnd;

        [JsonIgnore]
        public byte[] PeerClientPacketBegin
        {
            set { _peerClientPacketBegin = value; }
            get
            {
                if (_peerClientPacketBegin == null)
                    return ClassPeerPacketSetting.PacketSeperatorBegin;
                else return _peerClientPacketBegin;
            }
        }

        [JsonIgnore]
        public byte[] PeerClientPacketEnd
        {
            set { _peerClientPacketEnd = value; }
            get
            {
                if (_peerClientPacketEnd == null)
                    return ClassPeerPacketSetting.PacketSeperatorEnd;
                else return _peerClientPacketEnd;
            }
        }

        public long PeerTimestampSignatureWhitelist; // Used to know when it's necessary to sign a packet.

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
