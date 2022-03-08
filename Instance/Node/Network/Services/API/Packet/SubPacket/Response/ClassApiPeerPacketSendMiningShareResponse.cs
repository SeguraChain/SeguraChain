using SeguraChain_Lib.Blockchain.Mining.Enum;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Response
{
    public class ClassApiPeerPacketSendMiningShareResponse
    {
        public ClassMiningPoWaCEnumStatus MiningPoWShareStatus;
        public long PacketTimestamp;
    }
}
