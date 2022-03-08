using System.Collections.Generic;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Response
{
    public class ClassApiPeerPacketSendMemPoolBlockHeights
    {
        public List<long> ListBlockHeights;
        public long PacketTimestamp;

        public ClassApiPeerPacketSendMemPoolBlockHeights()
        {
            ListBlockHeights = new List<long>();
        }
    }
}
