using SeguraChain_Lib.Blockchain.Transaction.Object;
using System.Collections.Generic;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Response
{
    public class ClassApiPeerPacketSendMemPoolTransactionByRange
    {
        public List<ClassTransactionObject> ListTransaction;
        public long PacketTimestamp;
    }
}
