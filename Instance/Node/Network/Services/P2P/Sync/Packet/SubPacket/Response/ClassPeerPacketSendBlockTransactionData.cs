using SeguraChain_Lib.Blockchain.Transaction.Object;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendBlockTransactionData
    {
        public long BlockHeight;
        public ClassTransactionObject TransactionObject;
        public long PacketTimestamp;
        public string PacketNumericHash;
        public string PacketNumericSignature;
    }
}