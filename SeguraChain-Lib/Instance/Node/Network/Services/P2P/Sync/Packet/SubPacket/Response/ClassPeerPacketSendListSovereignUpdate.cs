using System.Collections.Generic;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendListSovereignUpdate
    {
        public List<string> SovereignUpdateHashList;
        public long PacketTimestamp;
        public string PacketNumericHash;
        public string PacketNumericSignature;
    }
}