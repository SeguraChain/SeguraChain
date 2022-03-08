using SeguraChain_Lib.Blockchain.Mining.Object;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Request
{
    public class ClassApiPeerPacketSendMiningShare
    {
        public ClassMiningPoWaCShareObject MiningPowShareObject;
        public long PacketTimestamp;
    }
}
