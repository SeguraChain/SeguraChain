namespace SeguraChain_Solo_Miner.Network.Object
{
    public class ClassMiningNetworkPeerObject
    {
        public int PeerApiPort;
        public int PeerTotalConnectFailed;
        public int PeerTotalInvalidPacket;
        public long PeerLastFailed;
        public long PeerLastInvalidPacket;
        public bool PeerStatus;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="peerApiPort"></param>
        public ClassMiningNetworkPeerObject(int peerApiPort)
        {
            PeerApiPort = peerApiPort;
            PeerStatus = true;
        }
    }
}
