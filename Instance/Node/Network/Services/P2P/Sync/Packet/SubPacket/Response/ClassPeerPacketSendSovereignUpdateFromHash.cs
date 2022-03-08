using SeguraChain_Lib.Blockchain.Sovereign.Object;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendSovereignUpdateFromHash
    {
        public ClassSovereignUpdateObject SovereignUpdateObject;
        public string PacketNumericHash;
        public string PacketNumericSignature;
        public long PacketTimestamp;
    }
}