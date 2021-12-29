using SeguraChain_Lib.Blockchain.Setting;
using System;
using System.Collections.Generic;

namespace SeguraChain_RPC_Wallet.Config
{
    public class ClassConfig
    {
        public ClassRpcApiSetting RpcApiSetting;
        public ClassRpcNodeApiSetting RpcNodeApiSetting;
        public ClassRpcWalletDatabaseSetting RpcWalletDatabaseSetting;
    }

    public class ClassRpcApiSetting
    {
        public string RpcApiIp;
        public int RpcApiPort;
        public bool RpcApiEnableSecretKey;
        public string RpcApiSecretKey;
        public bool RpcApiEnableWhitelist;
        public HashSet<string> RpcApiWhitelist;

        /// <summary>
        /// Constructor. Default settings.
        /// </summary>
        public ClassRpcApiSetting()
        {
            RpcApiIp = "127.0.0.1";
            RpcApiPort = 3080;
            RpcApiEnableSecretKey = false;
            RpcApiSecretKey = string.Empty;
            RpcApiEnableWhitelist = true; // Don't use whitelist.
            RpcApiWhitelist = new HashSet<string>() { "127.0.0.1" }; // Default whitelist.
        }
    }

    public class ClassRpcNodeApiSetting
    {
        public string RpcNodeApiIp;
        public int RpcNodeApiPort;

        /// <summary>
        /// Constructor. Default settings.
        /// </summary>
        public ClassRpcNodeApiSetting()
        {
            RpcNodeApiIp = BlockchainSetting.PeerDefaultApiIp;
            RpcNodeApiPort = BlockchainSetting.PeerDefaultApiPort;
        }
    }

    public class ClassRpcWalletDatabaseSetting
    {
        public string RpcWalletDatabasePath;
        public bool RpcWalletDatabaseEnableEncryption;
        public bool RpcWalletDatabaseEnableCompression;
        public bool RpcWalletDatabaseEnableJsonFormat;

        /// <summary>
        /// Constructor. Default settings.
        /// </summary>
        public ClassRpcWalletDatabaseSetting()
        {
            RpcWalletDatabasePath = AppContext.BaseDirectory + "\\Database\\";
            RpcWalletDatabaseEnableEncryption = false;
            RpcWalletDatabaseEnableCompression = false;
            RpcWalletDatabaseEnableJsonFormat = true;
        }
    }
}
