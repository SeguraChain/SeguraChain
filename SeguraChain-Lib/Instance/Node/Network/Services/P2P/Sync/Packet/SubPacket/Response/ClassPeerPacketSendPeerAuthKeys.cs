namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendPeerAuthKeys
    {
        public byte[] AesEncryptionKey;
        public byte[] AesEncryptionIv;
        public string PublicKey; // Used for check packet signed.
        public string NumericPublicKey; // Used for check packet signed by a peer who have the seed rank.
        public int PeerPort;
        public int PeerApiPort;
        public long PacketTimestamp;
        public byte[] PeerPacketBegin;
        public byte[] PeerPacketEnd;
    }
}