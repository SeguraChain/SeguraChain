namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Request
{
    public class ClassPeerPacketSendAskBlockTransactionDataByRange
    {
        public long BlockHeight;
        public int TransactionIdStartRange;
        public int TransactionIdEndRange;
        public long PacketTimestamp;
    }
}
