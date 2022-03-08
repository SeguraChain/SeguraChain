using System.Collections.Generic;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendMemPoolBlockHeightList
    {
        public SortedList<long, int> MemPoolBlockHeightListAndCount;
        public long PacketTimestamp;
    }
}
