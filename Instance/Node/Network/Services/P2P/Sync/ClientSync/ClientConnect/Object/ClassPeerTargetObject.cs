namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.ClientConnect.Object
{
    public class ClassPeerTargetObject
    {
        public ClassPeerNetworkClientSyncObject PeerNetworkClientSyncObject;

        public string PeerIpTarget => PeerNetworkClientSyncObject != null ? PeerNetworkClientSyncObject.PeerIpTarget : string.Empty;

        public int PeerPortTarget => PeerNetworkClientSyncObject != null ? PeerNetworkClientSyncObject.PeerPortTarget : 0;

        public string PeerUniqueIdTarget => PeerNetworkClientSyncObject != null ? PeerNetworkClientSyncObject.PeerUniqueIdTarget : string.Empty;
    }
}
