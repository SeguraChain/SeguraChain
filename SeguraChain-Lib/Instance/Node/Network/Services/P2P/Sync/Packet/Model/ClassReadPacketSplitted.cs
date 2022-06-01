using SeguraChain_Lib.Other.Object.List;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.Model
{
    /// <summary>
    /// Store packet data and set complete status once the packet separator is received.
    /// </summary>
    public class ClassReadPacketSplitted
    {
        public string Packet;
        public bool Complete;
        public bool Used;

        public ClassReadPacketSplitted()
        {
            Packet = string.Empty;
        }
    }
}
