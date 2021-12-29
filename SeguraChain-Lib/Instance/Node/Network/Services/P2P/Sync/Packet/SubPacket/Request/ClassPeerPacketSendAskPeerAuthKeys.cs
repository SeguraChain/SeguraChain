namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Request
{
    /// <summary>
    /// Send peer auth keys generated to a peer and claim his keys.
    /// </summary>
    public class ClassPeerPacketSendAskPeerAuthKeys
    {
        public byte[] AesEncryptionKey;
        public byte[] AesEncryptionIv;
        public string PublicKey; // Used for check packet signed.
        public string NumericPublicKey; // Used for check packet signed by the numeric peer key if this one has the seed rank.
        public int PeerPort;
        public bool PeerIsPublic;
        public long PacketTimestamp;
    }
}