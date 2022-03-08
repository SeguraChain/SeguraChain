namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendBlockHeightInformation
    {
        public long BlockHeight;
        public string BlockHash;
        public string BlockFinalTransactionHash;
        public long PacketTimestamp;
        public string PacketNumericHash;
        public string PacketNumericSignature;
    }
}
