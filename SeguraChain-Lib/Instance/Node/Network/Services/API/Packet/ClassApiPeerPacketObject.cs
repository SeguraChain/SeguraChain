using SeguraChain_Lib.Instance.Node.Network.Enum.API.Packet;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Packet
{
    public class ClassApiPeerPacketObjectSend
    {
        public ClassPeerApiPostPacketSendEnum PacketType;
        public string PacketContentObjectSerialized;
    }

    public class ClassApiPeerPacketObjetReceive
    {
        public ClassPeerApiPacketResponseEnum PacketType;
        public string PacketObjectSerialized;
    }
}
