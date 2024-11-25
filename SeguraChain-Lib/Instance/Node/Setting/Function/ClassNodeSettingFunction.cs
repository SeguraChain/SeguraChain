using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Instance.Node.Network.Database.Manager;
using SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Enum;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node.Setting.Function
{
    public class ClassPeerNodeSettingFunction
    {
        private const string PeerSettingFile = "peer-setting.json";
        private static readonly string PeerSettingFilePath = ClassUtility.ConvertPath(AppContext.BaseDirectory + PeerSettingFile);

        /// <summary>
        /// Load peer setting file.
        /// </summary>
        /// <param name="peerSettingObject"></param>
        /// <returns></returns>
        public static bool LoadPeerSetting(out ClassNodeSettingObject peerSettingObject)
        {
            if (File.Exists(PeerSettingFilePath))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(PeerSettingFilePath))
                    {
                        if (!ClassUtility.TryDeserialize(reader.ReadToEnd(), out peerSettingObject, ObjectCreationHandling.Reuse))
                            return false;
                    }

                    return CheckPeerSetting(peerSettingObject);
                }
                catch (Exception error)
                {
                    ClassLog.WriteLine("Can't load peer setting file. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    peerSettingObject = null;
                    return false;
                }
            }

            // Initialize settings if the file not exist.
            return InitializePeerSetting(out peerSettingObject);
        }

        /// <summary>
        /// Initialize peer setting.
        /// </summary>
        /// <param name="peerSettingObject"></param>
        /// <returns></returns>
        public static bool InitializePeerSetting(out ClassNodeSettingObject peerSettingObject)
        {
            Console.WriteLine("Write your peer server IP: ");

            string peerIp = Console.ReadLine();

            while(!IPAddress.TryParse(peerIp, out _))
            {
                Console.WriteLine("The input ip is invalid, please try again: ");
                peerIp = Console.ReadLine();
            }

            Console.WriteLine("Write your peer port (Example: " + BlockchainSetting.PeerDefaultPort + "): ");

            int peerPort = 0;

            while (peerPort < BlockchainSetting.PeerMinPort || peerPort > BlockchainSetting.PeerMaxPort)
            {
                while (!int.TryParse(Console.ReadLine(), out peerPort))
                    Console.WriteLine("Invalid input port, please try again:");

                if (peerPort < BlockchainSetting.PeerMinPort || peerPort > BlockchainSetting.PeerMaxPort)
                    Console.WriteLine("Invalid input port, this one is not inside of the range [" + BlockchainSetting.PeerMinPort + ":" + BlockchainSetting.PeerMaxPort + "], please try again.");
            }

            Console.WriteLine("Write your peer API port, it's usually for Wallets and web API client (exchanges and more): ");


            int peerApiPort = 0;

            while (peerApiPort < BlockchainSetting.PeerMinPort || peerApiPort > BlockchainSetting.PeerMaxPort)
            {
                while (!int.TryParse(Console.ReadLine(), out peerApiPort))
                    Console.WriteLine("Invalid input API port, please try again:");

                if (peerApiPort < BlockchainSetting.PeerMinPort || peerApiPort > BlockchainSetting.PeerMaxPort)
                    Console.WriteLine("Invalid input API port, this one is not inside of the range [" + BlockchainSetting.PeerMinPort + ":" + BlockchainSetting.PeerMaxPort + "], please try again.");

                if (peerApiPort == peerPort)
                {
                    Console.WriteLine("Invalid input API port, the port is the same of the peer port.");
                    peerApiPort = 0;
                }
            }

            Console.WriteLine("[Advanced Settings]");
            Console.WriteLine("Do you want to enable the Firewall Link System? This system permit to ban/unban IP's to your firewall of your system: [Y/N]");

            bool enableFirewallLink = false;
            string firewallName = string.Empty;
            string firewallChainName = string.Empty;

            string choose = Console.ReadLine() ?? string.Empty;

            if (choose.ToLower() == "y")
            {
                enableFirewallLink = true;
                Console.WriteLine("Select the firewall name of your system: ");
                Console.WriteLine("1. " + ClassApiFirewallName.Iptables);
                Console.WriteLine("2. " + ClassApiFirewallName.PacketFilter);
                Console.WriteLine("3. " + ClassApiFirewallName.Windows);

                int firewallId = 0;

                while (firewallId < 1 || firewallId > 3)
                {

                    while (!int.TryParse(Console.ReadLine(), out firewallId))
                    {
                        Console.Clear();
                        Console.WriteLine("Input invalid.");
                        Console.WriteLine("Select the firewall name of your system: ");
                        Console.WriteLine("1. " + ClassApiFirewallName.Iptables);
                        Console.WriteLine("2. " + ClassApiFirewallName.PacketFilter);
                        Console.WriteLine("3. " + ClassApiFirewallName.Windows);
                    }
                    if (firewallId < 1 || firewallId > 3)
                    {
                        Console.Clear();
                        Console.WriteLine("Firewall ID invalid.");
                        Console.WriteLine("Select the firewall name of your system: ");
                        Console.WriteLine("1. " + ClassApiFirewallName.Iptables);
                        Console.WriteLine("2. " + ClassApiFirewallName.PacketFilter);
                        Console.WriteLine("3. " + ClassApiFirewallName.Windows);
                    }
                }

                switch (firewallId)
                {
                    case 1:
                        firewallName = ClassApiFirewallName.Iptables;
                        break;
                    case 2:
                        firewallName = ClassApiFirewallName.PacketFilter;
                        break;
                    case 3:
                        firewallName = ClassApiFirewallName.Windows;
                        break;
                }

                Console.WriteLine("Write the name of the chain/table name of your firewall:");

                firewallChainName = Console.ReadLine() ?? string.Empty;

                while (firewallChainName.IsNullOrEmpty(false, out _))
                {
                    Console.Clear();

                    Console.WriteLine("Invalid input, the chain/table name can't be empty.");
                    Console.WriteLine("Write the name of the chain/table name of your firewall:");
                    firewallChainName = Console.ReadLine() ?? string.Empty;
                }
            }

            Console.WriteLine("Do you want to put your Peer has a public Peer? [Y/N]");
            Console.ForegroundColor = ConsoleColor.Red;

#if NET5_0_OR_GREATER
            Console.WriteLine("Notice: OpenNAT doesn't work with NET5, if your host is not a dedicated server or a VPS. You need open the P2P port and target the host IP to your router.");
#else
            Console.WriteLine("Notice: OpenNAT will be used to open the Peer port to the public and require the UPnP protocol available and actived to your router.");
#endif
            Console.ForegroundColor = ConsoleColor.White;
            choose = Console.ReadLine() ?? string.Empty;

            string choose2 = string.Empty;
            if (choose.ToLower() == "y")
            {
                Console.WriteLine("Do you use a dedicated server? In this case OpenNAT will not be used for open the Peer port to the public: [Y/N]");

                choose2 = Console.ReadLine() ?? string.Empty;
            }

            string numericPrivateKey = ClassPeerKeysManager.GeneratePeerPrivateKey();

            peerSettingObject = new ClassNodeSettingObject
            {
                PeerNetworkSettingObject =
                {
                    ListenIp = peerIp,
                    ListenPort = peerPort,
                    ListenApiIp = peerIp,
                    ListenApiPort = peerApiPort,
                    PublicPeer = choose.ToLower() == "y",
                    IsDedicatedServer = choose2.ToLower() == "y",
                    PeerUniqueId = ClassPeerKeysManager.GeneratePeerUniqueId(),
                    PeerNumericPrivateKey = numericPrivateKey,
                    PeerNumericPublicKey = ClassPeerKeysManager.GeneratePeerPublicKeyFromPrivateKey(numericPrivateKey),
                    PeerMaxRangeBlockToSyncPerRequest = Environment.ProcessorCount * 2
                },
                PeerLogSettingObject =
                {
                    LogWriteLevel = (int) ClassLog.CurrentWriteLogLevel,
                    LogLevel = (int) ClassLog.CurrentLogLevelType
                },
                PeerFirewallSettingObject =
                {
                    PeerEnableFirewallLink = enableFirewallLink,
                    PeerFirewallName = firewallName,
                    PeerFirewallChainName = firewallChainName
                }
            };


            try
            {
                using (StreamWriter writer = new StreamWriter(PeerSettingFilePath))
                    writer.WriteLine(ClassUtility.SerializeData(peerSettingObject, Formatting.Indented));
            }
            catch (Exception error)
            {
                ClassLog.WriteLine("Can't save peer setting initialized. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }

            ClassLog.WriteLine("Peer setting successfully initialized.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            return true;
        }

        /// <summary>
        /// Check the peer setting.
        /// </summary>
        /// <param name="peerSettingObject"></param>
        /// <returns></returns>
        private static bool CheckPeerSetting(ClassNodeSettingObject peerSettingObject)
        {
            if (peerSettingObject == null)
            {
                ClassLog.WriteLine("Error, the peer setting file is null or empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            #region Check log settings.

            if (!ClassLog.ChangeLogLevel(true, peerSettingObject.PeerLogSettingObject.LogLevel))
            {
                ClassLog.WriteLine("Error, the peer log level is invalid.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            if (!ClassLog.ChangeLogWriteLevel(peerSettingObject.PeerLogSettingObject.LogWriteLevel))
            {
                ClassLog.WriteLine("Error, the peer log write level is invalid.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            #endregion

            #region Check Peer Network settings.

            if (!IPAddress.TryParse(peerSettingObject.PeerNetworkSettingObject.ListenIp, out _))
            {
                ClassLog.WriteLine("Error, the peer listen IP is invalid.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }

            if (peerSettingObject.PeerNetworkSettingObject.ListenPort < BlockchainSetting.PeerMinPort || peerSettingObject.PeerNetworkSettingObject.ListenPort > BlockchainSetting.PeerMaxPort)
            {
                ClassLog.WriteLine("Error, the peer P2P port is not the range allowed. Range allow: " + BlockchainSetting.PeerMinPort + ":" + BlockchainSetting.PeerMaxPort, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }

            if (peerSettingObject.PeerNetworkSettingObject.ListenApiPort < BlockchainSetting.PeerMinPort || peerSettingObject.PeerNetworkSettingObject.ListenApiPort > BlockchainSetting.PeerMaxPort)
            {
                ClassLog.WriteLine("Error, the peer API port is not the range allowed. Range allow: " + BlockchainSetting.PeerMinPort + ":" + BlockchainSetting.PeerMaxPort, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }

            if (peerSettingObject.PeerNetworkSettingObject.ListenApiPort == peerSettingObject.PeerNetworkSettingObject.ListenPort)
            {
                ClassLog.WriteLine("Error, the peer API port is the same of the peer port. Peer Port: " + peerSettingObject.PeerNetworkSettingObject.ListenPort + " | Api Port: " + peerSettingObject.PeerNetworkSettingObject.ListenApiPort, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }

            if (peerSettingObject.PeerNetworkSettingObject.PeerNumericPrivateKey.IsNullOrEmpty(false, out _) ||
                peerSettingObject.PeerNetworkSettingObject.PeerNumericPublicKey.IsNullOrEmpty(false, out _))
            {
                ClassLog.WriteLine("Error, empty private/public numeric keys.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }

            if (ClassBase58.DecodeWithCheckSum(peerSettingObject.PeerNetworkSettingObject.PeerNumericPrivateKey, true) == null ||
                ClassBase58.DecodeWithCheckSum(peerSettingObject.PeerNetworkSettingObject.PeerNumericPublicKey, false) == null)
            {
                ClassLog.WriteLine("Error, invalid private/public numeric keys format.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }

            if (peerSettingObject.PeerNetworkSettingObject.PeerUniqueId.IsNullOrEmpty(false, out _))
            {
                ClassLog.WriteLine("Error, the peer unique id is empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                return false;
            }

            if (peerSettingObject.PeerNetworkSettingObject.PeerUniqueId.Length != BlockchainSetting.PeerUniqueIdHashLength || !ClassUtility.CheckHexStringFormat(peerSettingObject.PeerNetworkSettingObject.PeerUniqueId))
            {
                ClassLog.WriteLine("Error, the peer unique id have an invalid size/format.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            if (peerSettingObject.PeerNetworkSettingObject.PeerMaxNodeConnectionPerIp <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMaxApiConnectionPerIp <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMaxDelayAwaitResponse <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMaxDelayConnection <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMaxTimestampDelayPacket <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMaxEarlierPacketDelay <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMaxDelayToConnectToTarget <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerTaskSyncDelay <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMinAvailablePeerSync <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMaxAuthKeysExpire <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMaxPacketBufferSize == 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMinPort <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMaxPort <= 0 ||
                peerSettingObject.PeerNetworkSettingObject.PeerMaxPacketSplitedSendSize <= 0 |
                peerSettingObject.PeerNetworkSettingObject.PeerMaxSemaphoreConnectAwaitDelay <= 0)
            {
                ClassLog.WriteLine("Error, something is invalid on the peer network setting part, Ensure to not have any delay lower than 1.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            #endregion

            #region Check Blockchain database settings.

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject == null)
            {
                ClassLog.WriteLine("Error, the peer blockchain setting is null or empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainSetting == null)
            {
                ClassLog.WriteLine("Error, the peer blockchain database setting part is null or empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject.DataSetting == null)
            {
                ClassLog.WriteLine("Error, the peer blockchain data setting part is null or empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting == null)
            {
                ClassLog.WriteLine("Error, the peer blockchain cache setting part is null or empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject.MemPoolSetting == null)
            {
                ClassLog.WriteLine("Error, the peer blockchain MemPool setting part is null or empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            #region Database setting part.

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainSetting.BlockchainBlockDatabaseFilename.IsNullOrEmpty(false, out _))
            {
                ClassLog.WriteLine("Error, the block database filename is empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainSetting.BlockchainCheckpointDatabaseFilename.IsNullOrEmpty(false, out _))
            {
                ClassLog.WriteLine("Error, the checkpoint database filename is empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainSetting.BlockchainDirectoryPath.IsNullOrEmpty(false, out _))
            {
                ClassLog.WriteLine("Error, the blockchain directory path is empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            #endregion

            #region Cache setting part.

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting == null)
            {
                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.CacheDirectoryPath.IsNullOrEmpty(false, out _))
                {
                    ClassLog.WriteLine("Error, the peer blockchain cache directory path is empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalMaxActiveMemoryAllocationFromCache <= 0)
                {
                    ClassLog.WriteLine("Error, the global max active memory allocation cannot be lower or equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalMaxActiveMemoryAllocationFromCache < ClassBlockchainDatabaseDefaultSetting.DefaultGlobalMaxActiveMemoryAllocationFromCache)
                {
                    ClassLog.WriteLine("Warning, the global max active memory allocation is lower than the minimum set by default.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalMaxBlockCountToKeepInMemory <= 0)
                {
                    ClassLog.WriteLine("Warning, the global max block count to keep in memory is equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalMaxRangeReadBlockDataFromCache <= 0)
                {
                    ClassLog.WriteLine("Error, the global max range read block data from cache cannot be lower or equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalObjectCacheUpdateLimitTime <= 0)
                {
                    ClassLog.WriteLine("Error, the global cache update limit time cannot be lower or equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalObjectLimitSimpleGetObjectFromCache <= 0)
                {
                    ClassLog.WriteLine("Error, the global limit simple get object from cache cannot be lower or equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalBlockActiveMemoryKeepAlive <= 0)
                {
                    ClassLog.WriteLine("Error, the global expiration memory object cached cannot be lower or equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalTaskManageMemoryInterval <= 0)
                {
                    ClassLog.WriteLine("Error, the global task manage memory interval cannot be lower or equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.IoCacheDiskParallelTaskWaitDelay <= 0)
                {
                    ClassLog.WriteLine("Error, the io cache disk the parallel task wait delay cannot be lower or equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.IoCacheDiskMaxKeepAliveDataInMemoryTimeLimit <= 0)
                {
                    ClassLog.WriteLine("Error, the io cache disk the get call back data to memory time limit cannot be lower or equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.IoCacheDiskMaxBlockPerFile <= 0)
                {
                    ClassLog.WriteLine("Error, the io cache disk the max block per file cannot be lower or equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.IoCacheDiskReadStreamBufferSize < 1024)
                {
                    ClassLog.WriteLine("Error, the io cache disk the read stream buffer size cannot be lower than 1024.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.IoCacheDiskWriteStreamBufferSize < 1024)
                {
                    ClassLog.WriteLine("Error, the io cache disk the write stream buffer size cannot be lower than 1024.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalCacheMaxBlockTransactionKeepAliveMemorySize < 0)
                {
                    ClassLog.WriteLine("Error, the io cache disk the max memory allocated to the block transaction cache to keep alive them cannot be lower than 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }

                if (peerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalMaxDelayKeepAliveBlockTransactionCached <= 0)
                {
                    ClassLog.WriteLine("Error, the the max delay to keep alive a block transaction cached cannot be lower or equal of 0.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                    return false;
                }
            }

            #endregion

            #region MemPool setting part.

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject.MemPoolSetting.MemPoolDirectoryPath.IsNullOrEmpty(false, out _))
            {
                ClassLog.WriteLine("Error, the MemPool directory path cannot be null or empty", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                return false;
            }

            if (peerSettingObject.PeerBlockchainDatabaseSettingObject.MemPoolSetting.MemPoolTransactionDatabaseFilename.IsNullOrEmpty(false, out _))
            {
                ClassLog.WriteLine("Error, the MemPool transaction database filename cannot be null or empty", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                return false;
            }

            #endregion

            #endregion

            #region Check Peer Firewall settings.

            if (peerSettingObject.PeerFirewallSettingObject == null)
            {
                ClassLog.WriteLine("Error, the peer firewall setting part is null or empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                return false;
            }

            if (peerSettingObject.PeerFirewallSettingObject.PeerEnableFirewallLink)
            {
                if (peerSettingObject.PeerFirewallSettingObject.PeerFirewallName.IsNullOrEmpty(false, out _) || peerSettingObject.PeerFirewallSettingObject.PeerFirewallChainName.IsNullOrEmpty(false, out _))
                {
                    ClassLog.WriteLine("Error, the peer firewall link is enabled, but the firewall name or the chain/table firewall name are empty.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                    return false;
                }

                if (peerSettingObject.PeerFirewallSettingObject.PeerFirewallName != ClassApiFirewallName.Iptables && peerSettingObject.PeerFirewallSettingObject.PeerFirewallName != ClassApiFirewallName.PacketFilter && peerSettingObject.PeerFirewallSettingObject.PeerFirewallName != ClassApiFirewallName.Windows)
                {
                    ClassLog.WriteLine("Error, the peer firewall name: " + peerSettingObject.PeerFirewallSettingObject.PeerFirewallName + " not supported or the name is invalid.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                    return false;
                }
            }

            #endregion

            return true;
        }
    }
}
