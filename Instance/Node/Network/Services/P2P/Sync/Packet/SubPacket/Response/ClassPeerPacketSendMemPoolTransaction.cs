using SeguraChain_Lib.Blockchain.Transaction.Object;
using System.Collections.Generic;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendMemPoolTransaction
    {
        public List<ClassTransactionObject> ListTransactionObject;
        public long PacketTimestamp;
    }
}
