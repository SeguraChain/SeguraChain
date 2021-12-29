using System;
using System.Numerics;

namespace SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Response
{
    public class ClassApiPeerPacketSendFeeCostConfirmation
    {
        public BigInteger FeeCost;
        public bool Status;
        public long PacketTimestamp;
    }
}
