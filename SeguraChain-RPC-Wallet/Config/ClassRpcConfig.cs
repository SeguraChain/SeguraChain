using Newtonsoft.Json;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Utility;
using System;
using System.Collections.Generic;

namespace SeguraChain_RPC_Wallet.Config
{
    public class ClassRpcConfigPath
    {
        public const string RpcConfigPath = "config.json";
    }

    public class ClassRpcConfig
    {
        [JsonIgnore]
        public bool RpcWalletEnabled;

        public ClassRpcApiSetting RpcApiSetting;
        public ClassRpcNodeApiSetting RpcNodeApiSetting;
        public ClassRpcWalletDatabaseSetting RpcWalletDatabaseSetting;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassRpcConfig()
        {
            RpcWalletEnabled = true;
            RpcApiSetting = new ClassRpcApiSetting();
            RpcNodeApiSetting = new ClassRpcNodeApiSetting();
            RpcWalletDatabaseSetting = new ClassRpcWalletDatabaseSetting();
        }
    }

    public class ClassRpcApiSetting
    {
        public string RpcApiIp;
        public int RpcApiPort;
        public bool RpcApiEnableSecretKey;
        public string RpcApiSecretKey;
        public bool RpcApiEnableWhitelist;
        public HashSet<string> RpcApiWhitelist;
        public int RpcApiSemaphoreTimeout;
        public int RpcApiMaxConnectDelay;

        [JsonIgnore]
        public byte[] RpcApiSecretKeyArray;
        public byte[] RpcApiSecretIvArray;

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
            RpcApiSemaphoreTimeout = 5000;
            RpcApiMaxConnectDelay = 30;

            // Compute the API Secret Key if this one is initialized.
            if (!RpcApiSecretKey.IsNullOrEmpty(out _))
            {
                ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringUtf8(RpcApiSecretKey), true, out RpcApiSecretKeyArray);
                RpcApiSecretIvArray = ClassAes.GenerateIv(RpcApiSecretKeyArray);
            }
        }
    }

    public class ClassRpcNodeApiSetting
    {
        public string RpcNodeApiIp;
        public int RpcNodeApiPort;
        public int RpcNodeApiMaxDelay;

        /// <summary>
        /// Constructor. Default settings.
        /// </summary>
        public ClassRpcNodeApiSetting()
        {
            RpcNodeApiIp = BlockchainSetting.PeerDefaultApiIp;
            RpcNodeApiPort = BlockchainSetting.PeerDefaultApiPort;
            RpcNodeApiMaxDelay = BlockchainSetting.PeerApiMaxPacketDelay;
        }
    }

    public class ClassRpcWalletDatabaseSetting
    {
        public string RpcWalletDatabasePath;
        public string RpcWalletDatabaseFilename;
        public bool RpcWalletDatabaseEnableEncryption;
        public bool RpcWalletDatabaseEnableCompression;
        public bool RpcWalletDatabaseEnableJsonFormat;

        /// <summary>
        /// Constructor. Default settings.
        /// </summary>
        public ClassRpcWalletDatabaseSetting()
        {
            RpcWalletDatabasePath = AppContext.BaseDirectory + "\\WalletDatabase\\";
            RpcWalletDatabaseFilename = "wallet.dat";
            RpcWalletDatabaseEnableEncryption = false;
            RpcWalletDatabaseEnableCompression = false;
            RpcWalletDatabaseEnableJsonFormat = true;
        }
    }
}
