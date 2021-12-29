using System.Numerics;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendNetworkInformation
    {
        public long CurrentBlockHeight;
        public long LastBlockHeightUnlocked;
        public BigInteger CurrentBlockDifficulty;
        public string CurrentBlockHash;
        public long TimestampBlockCreate;
        public long PacketTimestamp;
        public string PacketNumericHash;
        public string PacketNumericSignature;
    }
}