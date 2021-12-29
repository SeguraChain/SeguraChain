using SeguraChain_Lib.Blockchain.Mining.Object;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Request
{
    public class ClassPeerPacketSendAskMiningShareVote
    {
        public long BlockHeight;
        public ClassMiningPoWaCShareObject MiningPowShareObject;
        public long PacketTimestamp;
    }
}
