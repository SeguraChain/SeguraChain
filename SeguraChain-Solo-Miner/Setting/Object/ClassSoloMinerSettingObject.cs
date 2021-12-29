using SeguraChain_Lib.Blockchain.Setting;

namespace SeguraChain_Solo_Miner.Setting.Object
{
    public class ClassSoloMinerSettingObject
    {
        public ClassSoloMinerWalletSettingObject SoloMinerWalletSetting;
        public ClassSoloMinerThreadSettingObject SoloMinerThreadSetting;
        public ClassSoloMinerNetworkSettingObject SoloMinerNetworkSetting;
        public ClassSoloMinerMiscSettingObject SoloMinerMiscSetting;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <param name="maxThread"></param>
        /// <param name="threadPriority"></param>
        /// <param name="peerIpTarget"></param>
        /// <param name="peerApiPortTarget"></param>
        public ClassSoloMinerSettingObject(string walletAddress, int maxThread, int threadPriority, string peerIpTarget, int peerApiPortTarget)
        {
            SoloMinerWalletSetting = new ClassSoloMinerWalletSettingObject(walletAddress);
            SoloMinerThreadSetting = new ClassSoloMinerThreadSettingObject(maxThread, threadPriority);
            SoloMinerNetworkSetting = new ClassSoloMinerNetworkSettingObject(peerIpTarget, peerApiPortTarget);
            SoloMinerMiscSetting = new ClassSoloMinerMiscSettingObject(true);
        }

        #region Settings class object.

        /// <summary>
        /// Wallet setting object.
        /// </summary>
        public class ClassSoloMinerWalletSettingObject
        {
            public string wallet_address;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="walletAddress"></param>
            public ClassSoloMinerWalletSettingObject(string walletAddress)
            {
                wallet_address = walletAddress;
            }
        }

        /// <summary>
        /// Thread setting object.
        /// </summary>
        public class ClassSoloMinerThreadSettingObject
        {
            public int max_thread;
            public int thread_priority;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="maxThread"></param>
            /// <param name="threadPriority"></param>
            public ClassSoloMinerThreadSettingObject(int maxThread, int threadPriority)
            {
                max_thread = maxThread;
                thread_priority = threadPriority;
            }
        }

        /// <summary>
        /// Network setting object.
        /// </summary>
        public class ClassSoloMinerNetworkSettingObject
        {
            public string peer_ip_target;
            public int peer_api_port_target;
            public int peer_api_max_connection_delay;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="peerIpTarget"></param>
            /// <param name="peerApiPortTarget"></param>
            public ClassSoloMinerNetworkSettingObject(string peerIpTarget, int peerApiPortTarget)
            {
                peer_ip_target = peerIpTarget;
                peer_api_port_target = peerApiPortTarget;
                peer_api_max_connection_delay = BlockchainSetting.PeerApiMaxConnectionDelay;
            }
        }

        /// <summary>
        /// Other settings.
        /// </summary>
        public class ClassSoloMinerMiscSettingObject
        {
            public bool enable_log;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="enableLog"></param>
            public ClassSoloMinerMiscSettingObject(bool enableLog)
            {
                enable_log = enableLog;
            }

        }

        #endregion
    }
}
