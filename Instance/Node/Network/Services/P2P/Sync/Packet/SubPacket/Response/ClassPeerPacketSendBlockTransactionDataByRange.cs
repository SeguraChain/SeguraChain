using System.Collections.Generic;
using SeguraChain_Lib.Blockchain.Transaction.Object;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendBlockTransactionDataByRange
    {
        public long BlockHeight;
        public SortedDictionary<string, ClassTransactionObject> ListTransactionObject;
        public long PacketTimestamp;
        public string PacketNumericHash;
        public string PacketNumericSignature;
    }
}
