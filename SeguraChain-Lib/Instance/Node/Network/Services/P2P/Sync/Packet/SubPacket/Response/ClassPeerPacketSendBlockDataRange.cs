
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using System.Collections.Generic;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Response
{
    public class ClassPeerPacketSendBlockDataRange
    {
        public List<ClassBlockObject> ListBlockObject;
        public long PacketTimestamp;
        public string PacketNumericHash;
        public string PacketNumericSignature;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassPeerPacketSendBlockDataRange()
        {
            ListBlockObject = new List<ClassBlockObject>();
        }
    }
}
