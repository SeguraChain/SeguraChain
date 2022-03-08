using SeguraChain_Lib.Blockchain.Block.Object.Structure;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendBlockData
    {
        public ClassBlockObject BlockData;
        public long PacketTimestamp;
        public string PacketNumericHash;
        public string PacketNumericSignature;
    }
}