namespace SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Object
{
    public class ClassApiFirewallObject
    {
        public string Ip;
        public int TotalInvalidPacket;
        public long LastInvalidPacketTimestamp;
        public bool BanStatus;
        public long BanTimestamp;
        public bool BanStatusFirewallLink;
    }
}
