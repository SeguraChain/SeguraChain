using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Enum;
using SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Object;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Manager
{
    public class ClassPeerFirewallManager
    {
        private const int MaxApiInvalidPacket = 30; // Max of invalid packets to reach for ban an IP.
        private const int ApiInvalidPacketDelay = 10; // Keep alive pending 10 seconds invalid packets.
        private const int ApiBanDelay = 60; // Ban pending 60 seconds.
        private static ConcurrentDictionary<string, ClassApiFirewallObject> _dictionaryFirewallObjects = new ConcurrentDictionary<string, ClassApiFirewallObject>();

        /// <summary>
        /// Check API Client IP.
        /// </summary>
        /// <param name="clientIp"></param>
        /// <returns></returns>
        public static bool CheckClientIpStatus(string clientIp)
        {
            if (_dictionaryFirewallObjects.ContainsKey(clientIp))
            {
                if (_dictionaryFirewallObjects[clientIp].BanStatus)
                {
                    if (_dictionaryFirewallObjects[clientIp].BanTimestamp + ApiBanDelay >= ClassUtility.GetCurrentTimestampInSecond())
                        return false;

                    _dictionaryFirewallObjects[clientIp].BanStatus = false;
                }

                if (_dictionaryFirewallObjects[clientIp].LastInvalidPacketTimestamp + ApiInvalidPacketDelay <= ClassUtility.GetCurrentTimestampInSecond())
                    _dictionaryFirewallObjects[clientIp].TotalInvalidPacket = 0;
            }
            else
                InsertClient(clientIp);
            
            return true;
        }

        /// <summary>
        /// Insert a new IP of API Client.
        /// </summary>
        /// <param name="clientIp"></param>
        private static bool InsertClient(string clientIp)
        {
            try
            {
                return _dictionaryFirewallObjects.TryAdd(clientIp, new ClassApiFirewallObject()
                {
                    Ip = clientIp
                });
            }
            catch
            {
                // Ignored.
            }
            return false;
        }

        /// <summary>
        /// Insert an invalid packet to an IP of API Client.
        /// </summary>
        /// <param name="clientIp"></param>
        public static void InsertInvalidPacket(string clientIp)
        {
            if (!_dictionaryFirewallObjects.ContainsKey(clientIp))
            {
                if (InsertClient(clientIp))
                {
                    _dictionaryFirewallObjects[clientIp].LastInvalidPacketTimestamp = ClassUtility.GetCurrentTimestampInSecond();
                    _dictionaryFirewallObjects[clientIp].TotalInvalidPacket++;
                }
            }
            else
            {
                _dictionaryFirewallObjects[clientIp].LastInvalidPacketTimestamp = ClassUtility.GetCurrentTimestampInSecond();
                _dictionaryFirewallObjects[clientIp].TotalInvalidPacket++;

                if (_dictionaryFirewallObjects[clientIp].TotalInvalidPacket >= MaxApiInvalidPacket)
                {
                    _dictionaryFirewallObjects[clientIp].BanTimestamp = ClassUtility.GetCurrentTimestampInSecond();
                    _dictionaryFirewallObjects[clientIp].BanStatus = true;   
                }
            }
        }

        /// <summary>
        /// Ban/Unban a client IP on the firewall of the OS.
        /// </summary>
        /// <param name="firewallLinkName"></param>
        /// <param name="firewallChainName"></param>
        public static void ManageFirewallLink(string firewallLinkName, string firewallChainName)
        {

            foreach (var clientIp in _dictionaryFirewallObjects.Keys.ToArray())
            {
                if (_dictionaryFirewallObjects.ContainsKey(clientIp))
                {
                    if (!_dictionaryFirewallObjects[clientIp].BanStatusFirewallLink)
                    {
                        if (_dictionaryFirewallObjects[clientIp].BanStatus)
                        {
                            if (_dictionaryFirewallObjects[clientIp].BanTimestamp + ApiBanDelay >= ClassUtility.GetCurrentTimestampInSecond())
                            {
                                _dictionaryFirewallObjects[clientIp].BanStatusFirewallLink = true;

                                switch (firewallLinkName)
                                {
                                    case ClassApiFirewallName.Iptables:
                                        Process.Start("/bin/bash", "-c \"iptables -A " + firewallChainName + " -p tcp -s " + clientIp + " -j DROP\""); // AddBlock iptables rule.
                                        break;
                                    case ClassApiFirewallName.PacketFilter:
                                        Process.Start("pfctl", "-t " + firewallChainName + " -T add " + clientIp + ""); // AddBlock packet filter rule.
                                        break;
                                    case ClassApiFirewallName.Windows:
                                        try
                                        {
                                            using (Process cmd = new Process())
                                            {
                                                cmd.StartInfo.FileName = "cmd.exe";
                                                cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                                cmd.StartInfo.Arguments = "/c netsh advfirewall firewall add rule name=\"" + firewallChainName + " (" + clientIp + ")\""; // AddBlock Windows rule.
                                                cmd.Start();
                                            }
                                        }
                                        catch
                                        {
                                            // Ignored.
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_dictionaryFirewallObjects[clientIp].BanTimestamp + ApiBanDelay < ClassUtility.GetCurrentTimestampInSecond())
                        {
                            _dictionaryFirewallObjects[clientIp].BanStatusFirewallLink = false;

                            switch (firewallLinkName)
                            {
                                case ClassApiFirewallName.Iptables:
                                    Process.Start("/bin/bash", "-c \"iptables -D " + firewallChainName + " -p tcp -s " + clientIp + " -j DROP\""); // RemoveFromCache iptables rule.
                                    break;
                                case ClassApiFirewallName.PacketFilter:
                                    Process.Start("pfctl", "-t " + firewallChainName + " -T del " + clientIp + ""); // RemoveFromCache packet filter rule.
                                    break;
                                case ClassApiFirewallName.Windows:
                                    try
                                    {
                                        using (Process cmd = new Process())
                                        {
                                            cmd.StartInfo.FileName = "cmd.exe";
                                            cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                            cmd.StartInfo.Arguments = "/c netsh advfirewall firewall delete rule name=\"" + firewallChainName + " (" + clientIp + ")\""; // RemoveFromCache Windows rule.
                                            cmd.Start();
                                        }
                                    }
                                    catch
                                    {
                                        // Ignored.
                                    }
                                    break;
                            }

                        }
                    }
                }
            }
           
        }
    }
}
