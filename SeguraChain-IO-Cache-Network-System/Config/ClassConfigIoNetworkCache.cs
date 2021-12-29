using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;

namespace SeguraChain_IO_Cache_Network_System.Config
{
    public class ClassConfigIoNetworkDefaultSetting
    {
        public const string ConfigIoNetworkConfigFile = "io-network-config.json";
    }

    public class ClassConfigIoNetworkCache
    {
        public ClassBlockchainDatabaseSetting BlockchainDatabaseSetting;
        public ClassConfigIoNetworkCacheServer BlockchainConfigIoNetworkCacheServer;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassConfigIoNetworkCache()
        {
            BlockchainDatabaseSetting = new ClassBlockchainDatabaseSetting();
            BlockchainConfigIoNetworkCacheServer = new ClassConfigIoNetworkCacheServer();
        }
    }

    public class ClassConfigIoNetworkCacheServer
    {
        public string ip;
        public int port;
        public bool enable_encryption;
        public string server_encryption_key;
        public int server_packet_size = 8192;
    }
}
