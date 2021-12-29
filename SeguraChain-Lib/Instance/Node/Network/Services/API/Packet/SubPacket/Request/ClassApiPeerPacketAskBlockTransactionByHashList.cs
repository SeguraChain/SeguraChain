using System.Collections.Generic;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Request
{
    public class ClassApiPeerPacketAskBlockTransactionByHashList
    {
        public List<string> ListTransactionHash;
        public long BlockHeight;
        public long PacketTimestamp;
    }
}
