using System.Collections.Generic;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendPeerList
    {
        public List<string> PeerIpList;
        public List<int> PeerPortList;
        public List<string> PeerUniqueIdList;
        public long PacketTimestamp;
    }
}