using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SeguraChain_Lib.Blockchain.MemPool.Database;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Sovereign.Database;
using SeguraChain_Lib.Blockchain.Sovereign.Enum;
using SeguraChain_Lib.Blockchain.Sovereign.Object;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Blockchain.Stats.Object;
using SeguraChain_Lib.Blockchain.Wallet.Object.Blockchain;
using SeguraChain_Lib.Instance.Node;
using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Instance.Node.Network.Database.Manager;
using SeguraChain_Lib.Instance.Node.Network.Enum.Manage;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using SeguraChain_Peer.Mining.Instance;

namespace SeguraChain_Peer.CommandLine
{
    public class ClassConsoleCommandLineEnumeration
    {
        public const string HelpCommand = "help";
        public const string ExitCommand = "exit";
        public const string StatusCommand = "status";
        public const string BuildSovereignUpdateCommand = "build-sovereign-update";
        public const string ShowPeerListSeedRankCommand = "show-seed-list";
        public const string ShowPeerListCommand = "show-peer-list";
        public const string ShowLogLevelCommand = "show-log-level";
        public const string SetLogLevelCommand = "set-log-level";
        public const string ShowLogWriteLevelCommand = "show-log-write-level";
        public const string SetLogWriteLevelCommand = "set-log-write-level";
        public const string RegisterPeerCommand = "register-peer-command";
        public const string CloseActivePeerConnection = "close-active-peer-connection";
        public const string CloseActiveApiConnection = "close-active-api-connection";
        public const string GetWalletBalance = "get-wallet-balance";
        public const string StartSoloMining = "start-solo-mining";
        public const string StopSoloMining = "stop-solo-mining";
        public const string ShowMiningStats = "show-mining-stats";
        public const string GetNodeInternalStats = "get-node-internal-stats";
        public const string ClearConsoleCommand = "clear";
    }

    public class ClassConsoleCommandLine
    {
        private Thread _threadConsoleCommandLine;
        private ClassSoloMiningInstance _soloMiningInstance;
        private ClassNodeInstance _nodeInstance;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeInstance"></param>
        public ClassConsoleCommandLine(ClassNodeInstance nodeInstance)
        {
            _nodeInstance = nodeInstance;
        }

        /// <summary>
        /// Enable the console command line.
        /// </summary>
        public void EnableConsoleCommandLine()
        {
            ClassLog.SimpleWriteLine("Command lines system enabled: ");

            _threadConsoleCommandLine = new Thread(delegate ()
            {
                HandleCommandLineAsync(ClassConsoleCommandLineEnumeration.HelpCommand).Wait();

                while (_nodeInstance.PeerToolStatus)
                {
                   if(!HandleCommandLineAsync(Console.ReadLine()).Result)
                   {
                        break;
                   }
                }

            });
            _threadConsoleCommandLine.Start();
        }

        /// <summary>
        /// Handle command lines.
        /// </summary>
        private async Task<bool> HandleCommandLineAsync(string commandLine)
        {
            try
            {
                if (!commandLine.IsNullOrEmpty(out _))
                {
                    string[] splitCommandLine = commandLine.Split(new[] { " " }, StringSplitOptions.None);
                    switch (splitCommandLine[0])
                    {
                        case ClassConsoleCommandLineEnumeration.HelpCommand:
                            {
                                ClassLog.SimpleWriteLine("[Commands lines list]", ConsoleColor.Red);
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.HelpCommand + " - Show every command lines.");
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.ExitCommand + " - Close the peer tool.");
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.StatusCommand + " - Show network status.");

                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.BuildSovereignUpdateCommand + " - Build a sovereign update (reserved to dev).");

                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.ShowPeerListSeedRankCommand + " - Show the list of peer(s) who currently have the Seed Node rank.");
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.ShowPeerListCommand + " - Show the list of peer(s) with their state and their rank.");

                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.ShowLogLevelCommand + " - Show log levels available.");
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.SetLogLevelCommand + " - Set a log level of your choose. Usage example: " + ClassConsoleCommandLineEnumeration.SetLogLevelCommand + " 1");

                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.ShowLogWriteLevelCommand + " - Show log write levels available.");
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.SetLogWriteLevelCommand + " - Set a log write level of your choose. Usage example: " + ClassConsoleCommandLineEnumeration.SetLogWriteLevelCommand + " 1");

                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.RegisterPeerCommand + " - Permit to register a peer. Usage example: " + ClassConsoleCommandLineEnumeration.RegisterPeerCommand + " peer_ip peer_port peer_unique_id_hash");
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.CloseActivePeerConnection + " - Close every active peer(s) incoming connection(s).");
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.CloseActiveApiConnection + " - Close every active API incoming connection(s).");

                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.GetWalletBalance + " - Get the wallet balance from a wallet address. Usage example: " + ClassConsoleCommandLineEnumeration.GetWalletBalance + " " + BlockchainSetting.WalletAddressDev(0));
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.StartSoloMining + " - Execute a solo mining instance on your node. Usage example: " + ClassConsoleCommandLineEnumeration.StartSoloMining + " thread wallet_address -> " + ClassConsoleCommandLineEnumeration.StartSoloMining + " 4 " + BlockchainSetting.WalletAddressDev(0));
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.StopSoloMining + " - Stop the solo mining of your node.");
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.ShowMiningStats + " - Show your solo mining stats.");
                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.GetNodeInternalStats + " - Show your node internal stats. Show the active memory used and stats of the cache if enabled.");

                                ClassLog.SimpleWriteLine(ClassConsoleCommandLineEnumeration.ClearConsoleCommand + " - Clear the console.");
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.ExitCommand:
                            {
                                if (_soloMiningInstance != null)
                                {
                                    if (_soloMiningInstance.GetMiningStatus)
                                    {
                                        _soloMiningInstance.StopMining();
                                        ClassLog.SimpleWriteLine("Solo mining instance stopped.", ConsoleColor.DarkYellow);
                                    }
                                }
                                await _nodeInstance.NodeStop();
                                return false;
                            }
                        case ClassConsoleCommandLineEnumeration.StatusCommand:
                            {
                                ClassLog.SimpleWriteLine("Coin name: " + BlockchainSetting.CoinName);

                                ClassLog.SimpleWriteLine("Total Peer(s) registered: " + ClassPeerDatabase.DictionaryPeerDataObject.Count);
                                ClassLog.SimpleWriteLine("Total Sovereign Update(s) synced: " + ClassSovereignUpdateDatabase.DictionarySovereignUpdateObject.Count);
                                ClassLog.SimpleWriteLine("Total Peer(s) sync client active connection(s): " + _nodeInstance.PeerNetworkServerObject.GetAllTotalActiveConnection());
                                ClassLog.SimpleWriteLine("Total Client API active connection(s): " + _nodeInstance.PeerApiServerObject.GetAllTotalActiveConnection());

                                if (ClassBlockchainStats.BlockCount > 0)
                                {
                                    long lastBlockHeight = ClassBlockchainStats.GetLastBlockHeight();
                                    ClassBlockchainNetworkStatsObject blockchainNetworkStatsObjectObject = ClassBlockchainStats.BlockchainNetworkStatsObject;
                                    ClassLog.SimpleWriteLine("Current Block Height: Sync: " + lastBlockHeight + " | Network: " + blockchainNetworkStatsObjectObject.LastNetworkBlockHeight);
                                    ClassLog.SimpleWriteLine("[INFO] All stats displayed come from your data synced.", ConsoleColor.Yellow);
                                    ClassLog.SimpleWriteLine("Current Block Difficulty: " + blockchainNetworkStatsObjectObject.LastBlockDifficulty);
                                    ClassLog.SimpleWriteLine("Current Block Hash: " + blockchainNetworkStatsObjectObject.LastBlockHash);
                                    ClassLog.SimpleWriteLine("Current Block Status: " + blockchainNetworkStatsObjectObject.LastBlockStatus);
                                    ClassLog.SimpleWriteLine("Last Blockchain Stats update done: " + blockchainNetworkStatsObjectObject.LastUpdateStatsDateTime);
                                    ClassLog.SimpleWriteLine("Last Blockchain Stats update generated into: " + blockchainNetworkStatsObjectObject.BlockchainStatsTimestampToGenerate+" ms.");
                                    ClassLog.SimpleWriteLine("Estimated Network Hashrate: " + blockchainNetworkStatsObjectObject.NetworkHashrateEstimatedFormatted);
                                    ClassLog.SimpleWriteLine("Estimated Network Mining luck status: " + blockchainNetworkStatsObjectObject.BlockchainMiningStats);
                                    ClassLog.SimpleWriteLine("Estimated Network Mining luck percent: " + blockchainNetworkStatsObjectObject.BlockMiningLuckPercent + "%");

                                    ClassLog.SimpleWriteLine("[INFO] This part is updated after each tasks of transactions confirmations done in parallel into your node.", ConsoleColor.Cyan);
                                    ClassLog.SimpleWriteLine("Total Transaction(s) in Mem Pool: " + ClassMemPoolDatabase.GetCountMemPoolTx);
                                    ClassLog.SimpleWriteLine("Total Block Transaction(s): " + blockchainNetworkStatsObjectObject.TotalTransactions);
                                    ClassLog.SimpleWriteLine("Total Block Transaction(s) confirmed: " + blockchainNetworkStatsObjectObject.TotalTransactionsConfirmed + "/" + blockchainNetworkStatsObjectObject.TotalTransactions);
                                    ClassLog.SimpleWriteLine("Amount of Blocks unlocked checked: " + blockchainNetworkStatsObjectObject.LastBlockHeightTransactionConfirmationDone + "/" + blockchainNetworkStatsObjectObject.LastBlockHeightUnlocked + " (" + blockchainNetworkStatsObjectObject.TotalTaskConfirmationsDoneProgress + "%)");
                                    ClassLog.SimpleWriteLine("Total amount of coin(s) circulating on the chain: " + blockchainNetworkStatsObjectObject.TotalCoinCirculatingFormatted + " " + BlockchainSetting.CoinMinName);
                                    ClassLog.SimpleWriteLine("Total amount of fee(s) circulating on the chain: " + blockchainNetworkStatsObjectObject.TotalCoinFeeFormatted + " " + BlockchainSetting.CoinMinName);
                                    ClassLog.SimpleWriteLine("Total amount of coin(s) in pending on the chain: " + blockchainNetworkStatsObjectObject.TotalCoinPendingFormatted + " " + BlockchainSetting.CoinMinName);
                                    ClassLog.SimpleWriteLine("Total confirmed coins spread on the blockchain: " + blockchainNetworkStatsObjectObject.TotalCoinsSpreadFormatted.ToString("N" + BlockchainSetting.CoinDecimalNumber, CultureInfo.InvariantCulture) + "/" + blockchainNetworkStatsObjectObject.TotalSupply.ToString("N" + BlockchainSetting.CoinDecimalNumber, CultureInfo.InvariantCulture) + " " + BlockchainSetting.CoinMinName);

                                    CommandShowSoloMiningStats();
                                }
                                else
                                {
                                    ClassLog.SimpleWriteLine("No data synced.", ConsoleColor.DarkRed);
                                    ClassBlockchainNetworkStatsObject blockchainNetworkStatsObjectObject = ClassBlockchainStats.BlockchainNetworkStatsObject;
                                    if (blockchainNetworkStatsObjectObject != null)
                                    {
                                        if (blockchainNetworkStatsObjectObject.LastNetworkBlockHeight > 0)
                                            ClassLog.SimpleWriteLine("Current Last Network Block Height: " + blockchainNetworkStatsObjectObject.LastNetworkBlockHeight);
                                        else
                                            ClassLog.SimpleWriteLine("In research to get some peers to start the sync of your node..", ConsoleColor.Yellow);
                                    }
                                }

                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.BuildSovereignUpdateCommand:
                            {
                                ClassSovereignUpdateObject sovereignUpdateObject = ClassSovereignUpdateDatabase.GenerateSovereignUpdate(out var statutBuild);

                                if (statutBuild)
                                {
                                    if (sovereignUpdateObject != null)
                                    {
                                        ClassLog.SimpleWriteLine("Sovereign update successfully build, checking..", ConsoleColor.Yellow);
                                        ClassSovereignEnumUpdateType sovereignEnumUpdateType;
                                        ClassSovereignEnumUpdateCheckStatus sovereignUpdateCheck = ClassSovereignUpdateDatabase.CheckSovereignUpdateObject(sovereignUpdateObject, out sovereignEnumUpdateType);
                                        if (sovereignUpdateCheck == ClassSovereignEnumUpdateCheckStatus.VALID_SOVEREIGN_UPDATE)
                                        {

                                            if (ClassSovereignUpdateDatabase.RegisterSovereignUpdateObject(sovereignUpdateObject))
                                                ClassLog.SimpleWriteLine("Sovereign Update type: " + sovereignEnumUpdateType + " is valid and has been registered.", ConsoleColor.Green);
                                            else
                                                ClassLog.SimpleWriteLine("Can't save the sovereign update: " + sovereignUpdateObject.SovereignUpdateHash + " into the database, this one is already registered.", ConsoleColor.Yellow);
                                        }
                                        else
                                            ClassLog.SimpleWriteLine("The check of the sovereign update build return an error: " + sovereignUpdateCheck, ConsoleColor.Red);
                                    }
                                    else
                                        ClassLog.SimpleWriteLine("Build sovereign update failed. The object is null.", ConsoleColor.Red);
                                }
                                else
                                    ClassLog.SimpleWriteLine("Build sovereign update failed.", ConsoleColor.Red);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.ShowPeerListSeedRankCommand:
                            {
                                if (ClassSovereignUpdateDatabase.DictionarySortedSovereignUpdateList.ContainsKey(ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_GRANT_RANK_UPDATE))
                                {
                                    ClassLog.SimpleWriteLine("Total numeric public key peer(s) with the Seed Node rank: " + ClassSovereignUpdateDatabase.DictionarySortedSovereignUpdateList[ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_GRANT_RANK_UPDATE].Count);

                                    using (DisposableList<string> peerList = new DisposableList<string>(false, 0, ClassPeerDatabase.DictionaryPeerDataObject.Keys.ToList()))
                                    {
                                        if (peerList.Count > 0)
                                        {
                                            foreach (var peerIp in peerList.GetList)
                                            {
                                                if (ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(peerIp))
                                                {
                                                    if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].Count > 0)
                                                    {
                                                        foreach (string peerUniqueId in ClassPeerDatabase.DictionaryPeerDataObject[peerIp].Keys.ToArray())
                                                        {
                                                            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIsPublic)
                                                            {
                                                                if (ClassPeerCheckManager.PeerHasSeedRank(peerIp, peerUniqueId, out var numericPublicKey, out var timestampRankDelay))
                                                                    ClassLog.SimpleWriteLine("Peer: " + peerIp + " | Unique ID: " + peerUniqueId + " | Numeric Public Key: " + numericPublicKey + " | Rank valid until: " + ClassUtility.GetDatetimeFromTimestamp(timestampRankDelay));
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                            ClassLog.SimpleWriteLine("You have any peer listed.", ConsoleColor.Yellow);
                                    }
                                }
                                else
                                    ClassLog.SimpleWriteLine("Their is any peer with the Seed Node rank.", ConsoleColor.Yellow);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.ShowPeerListCommand:
                            {
                                if (ClassPeerDatabase.DictionaryPeerDataObject.Count > 0)
                                {
                                    using (DisposableList<string> peerList = new DisposableList<string>(false, 0, ClassPeerDatabase.DictionaryPeerDataObject.Keys.ToList()))
                                    {
                                        if (peerList.Count > 0)
                                        {
                                            foreach (var peerIp in peerList.GetList)
                                            {
                                                if (ClassPeerDatabase.DictionaryPeerDataObject.ContainsKey(peerIp))
                                                {
                                                    if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp].Count > 0)
                                                    {
                                                        foreach (string peerUniqueId in ClassPeerDatabase.DictionaryPeerDataObject[peerIp].Keys.ToArray())
                                                        {
                                                            if (ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerIsPublic)
                                                            {
                                                                if (ClassPeerCheckManager.PeerHasSeedRank(peerIp, peerUniqueId, out _, out var timestampRankDelay))
                                                                    ClassLog.SimpleWriteLine("Peer: " + peerIp + ":" + ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerPort + " | Status: " + ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus + " | Rank: Seed | Rank valid until: " + ClassUtility.GetDatetimeFromTimestamp(timestampRankDelay));
                                                                else
                                                                    ClassLog.SimpleWriteLine("Peer: " + peerIp + ":" + ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerPort + " | Status: " + ClassPeerDatabase.DictionaryPeerDataObject[peerIp][peerUniqueId].PeerStatus + " | Rank: Normal.");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                            ClassLog.SimpleWriteLine("You have any peer listed.", ConsoleColor.Yellow);
                                    }
                                }
                                else
                                    ClassLog.SimpleWriteLine("Their is any peer registered.", ConsoleColor.Yellow);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.ShowLogLevelCommand:
                            {
                                ClassLog.ShowLogLevels();
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.SetLogLevelCommand:
                            {
                                if (splitCommandLine.Length >= 2)
                                {
                                    if (int.TryParse(splitCommandLine[1], out var logLevel))
                                        ClassLog.ChangeLogLevel(logLevel);
                                    else
                                        ClassLog.SimpleWriteLine("Argument invalid.", ConsoleColor.Red);
                                }
                                else
                                    ClassLog.SimpleWriteLine("Not enough argument.", ConsoleColor.Red);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.ShowLogWriteLevelCommand:
                            {
                                ClassLog.ShowLogWriteLevels();
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.SetLogWriteLevelCommand:
                            {
                                if (splitCommandLine.Length >= 2)
                                {
                                    if (int.TryParse(splitCommandLine[1], out var logWriteLevel))
                                        ClassLog.ChangeLogWriteLevel(logWriteLevel);
                                    else
                                        ClassLog.SimpleWriteLine("Argument invalid.", ConsoleColor.Red);
                                }
                                else
                                    ClassLog.SimpleWriteLine("Not enough argument.", ConsoleColor.Red);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.RegisterPeerCommand:
                            {
                                if (splitCommandLine.Length >= 4)
                                {
                                    if (!splitCommandLine[1].IsNullOrEmpty(out _))
                                    {
                                        if (!splitCommandLine[2].IsNullOrEmpty(out _))
                                        {
                                            if (!splitCommandLine[3].IsNullOrEmpty(out _))
                                            {
                                                if (int.TryParse(splitCommandLine[2], out var peerPort))
                                                {
                                                    if (splitCommandLine[3].Length == BlockchainSetting.PeerUniqueIdHashLength && ClassUtility.CheckHexStringFormat(splitCommandLine[3]))
                                                    {
                                                        ClassPeerEnumInsertStatus insertStatus = ClassPeerDatabase.InputPeer(splitCommandLine[1], peerPort, splitCommandLine[3]);

                                                        if (insertStatus == ClassPeerEnumInsertStatus.PEER_INSERT_SUCCESS)
                                                            ClassLog.SimpleWriteLine("Peer " + splitCommandLine[1] + ":" + peerPort + " successfully inserted.", ConsoleColor.Green);
                                                        else
                                                            ClassLog.SimpleWriteLine("Insert peer failed. Result: " + insertStatus, ConsoleColor.Red);
                                                    }
                                                    else
                                                        ClassLog.SimpleWriteLine("Invalid Peer Unique ID argument.", ConsoleColor.Red);
                                                }
                                                else
                                                    ClassLog.SimpleWriteLine("Invalid Peer Port argument.", ConsoleColor.Red);
                                            }
                                            else
                                                ClassLog.SimpleWriteLine("Empty Peer Unique ID argument.", ConsoleColor.Red);
                                        }
                                        else
                                            ClassLog.SimpleWriteLine("Empty Peer Port argument.", ConsoleColor.Red);
                                    }
                                    else
                                        ClassLog.SimpleWriteLine("Empty Peer IP argument.", ConsoleColor.Red);
                                }
                                else
                                    ClassLog.SimpleWriteLine("Not enough argument.", ConsoleColor.Red);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.CloseActivePeerConnection:
                            {
                                ClassLog.SimpleWriteLine("Total Peer(s) incoming connection(s) closed:" + _nodeInstance.PeerNetworkServerObject.CleanUpAllIncomingConnection(), ConsoleColor.Cyan);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.CloseActiveApiConnection:
                            {
                                ClassLog.SimpleWriteLine("Total API incoming connection(s) closed:" + _nodeInstance.PeerApiServerObject.CleanUpAllIncomingConnection(), ConsoleColor.Cyan);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.ClearConsoleCommand:
                            {
                                Console.Clear();
                                ClassLog.SimpleWriteLine("Console cleaned.", ConsoleColor.Magenta);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.GetWalletBalance:
                            {
                                if (splitCommandLine.Length >= 2)
                                {
                                    if (!splitCommandLine[1].IsNullOrEmpty(out _))
                                    {
                                        string walletAddress = splitCommandLine[1];

                                        if (ClassBase58.DecodeWithCheckSum(walletAddress, true) != null)
                                        {
                                            ClassLog.SimpleWriteLine("Calculate confirmed balance, please wait a moment..");


                                            ClassBlockchainWalletBalanceCalculatedObject resultBalance = await ClassBlockchainStats.GetWalletBalanceFromTransactionAsync(walletAddress, ClassBlockchainStats.GetLastBlockHeight(), true, false, false, true, new CancellationTokenSource());

                                            decimal walletBalanceDecimalsCalculated = 0;
                                            decimal walletPendingBalanceDecimalsCalculated = 0;
                                            if (resultBalance.WalletBalance > 0)
                                            {
                                                walletBalanceDecimalsCalculated = (decimal)resultBalance.WalletBalance / BlockchainSetting.CoinDecimal;
                                                walletPendingBalanceDecimalsCalculated = (decimal)resultBalance.WalletPendingBalance / BlockchainSetting.CoinDecimal;
                                            }

                                            ClassLog.SimpleWriteLine("Confirmed Wallet Balance of " + walletAddress + " is: " + walletBalanceDecimalsCalculated.ToString("N" + BlockchainSetting.CoinDecimalNumber, CultureInfo.InvariantCulture) + " " + BlockchainSetting.CoinMinName + ".");
                                            ClassLog.SimpleWriteLine("Pending Wallet Balance of " + walletAddress + " is: " + walletPendingBalanceDecimalsCalculated.ToString("N" + BlockchainSetting.CoinDecimalNumber, CultureInfo.InvariantCulture) + " " + BlockchainSetting.CoinMinName + ".");
                                        }
                                        else
                                            ClassLog.SimpleWriteLine(walletAddress + " is not a valid wallet address.", ConsoleColor.Red);

                                        walletAddress.Clear();
                                    }
                                    else
                                        ClassLog.SimpleWriteLine("Empty wallet address.", ConsoleColor.Red);
                                }
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.StartSoloMining:
                            {
                                if (splitCommandLine.Length >= 3)
                                {
                                    if (int.TryParse(splitCommandLine[1], out int totalThreads))
                                    {
                                        if (ClassBase58.DecodeWithCheckSum(splitCommandLine[2], true) != null)
                                        {
                                            bool instanceRunning = false;
                                            if (_soloMiningInstance != null)
                                            {
                                                if (_soloMiningInstance.GetMiningStatus)
                                                {
                                                    ClassLog.SimpleWriteLine("A solo mining instance already running.", ConsoleColor.Yellow);
                                                    instanceRunning = true;
                                                }
                                            }
                                            if (!instanceRunning)
                                            { 
                                                _soloMiningInstance = new ClassSoloMiningInstance(splitCommandLine[2], totalThreads, _nodeInstance.PeerSettingObject.PeerNetworkSettingObject.ListenIp, _nodeInstance.PeerOpenNatPublicIp, _nodeInstance.PeerSettingObject.PeerNetworkSettingObject, _nodeInstance.PeerSettingObject.PeerFirewallSettingObject);
                                                _soloMiningInstance.StartMining();
                                                ClassLog.SimpleWriteLine("Solo mining started.", ConsoleColor.Green);
                                            }
                                        }
                                        else
                                            ClassLog.SimpleWriteLine("Invalid wallet address argument.", ConsoleColor.Yellow);
                                    }
                                    else
                                        ClassLog.SimpleWriteLine("Invalid total threads argument.", ConsoleColor.Yellow);
                                }
                                else
                                    ClassLog.SimpleWriteLine("Not enough argument.", ConsoleColor.Red);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.StopSoloMining:
                            {
                                if (_soloMiningInstance != null)
                                {
                                    if (_soloMiningInstance.GetMiningStatus)
                                    {
                                        _soloMiningInstance.StopMining();
                                        ClassLog.SimpleWriteLine("Solo mining stopped.", ConsoleColor.DarkRed);
                                    }
                                    else
                                        ClassLog.SimpleWriteLine("Solo mining already stopped.", ConsoleColor.Yellow);
                                }
                                else
                                    ClassLog.SimpleWriteLine("No solo mining instance has been started.", ConsoleColor.Yellow);
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.ShowMiningStats:
                            {
                                CommandShowSoloMiningStats();
                            }
                            break;
                        case ClassConsoleCommandLineEnumeration.GetNodeInternalStats:
                            {
                                ClassLog.SimpleWriteLine("Active memory used from the cache: " + ClassUtility.ConvertBytesToMegabytes(_nodeInstance.NodeInternalStatsReportObject.NodeCacheMemoryUsage) + " on " + ClassUtility.ConvertBytesToMegabytes(_nodeInstance.NodeInternalStatsReportObject.NodeCacheMaxMemoryAllocation));
                                ClassLog.SimpleWriteLine("Active memory used from the block transaction cache: " + ClassUtility.ConvertBytesToMegabytes(_nodeInstance.NodeInternalStatsReportObject.NodeCacheTransactionMemoryUsage) + " on " + ClassUtility.ConvertBytesToMegabytes(_nodeInstance.NodeInternalStatsReportObject.NodeCacheTransactionMaxMemoryAllocation));
                                ClassLog.SimpleWriteLine("Active memory used from the wallet index cache: " + ClassUtility.ConvertBytesToMegabytes(_nodeInstance.NodeInternalStatsReportObject.NodeCacheWalletIndexMemoryUsage) + " on " + ClassUtility.ConvertBytesToMegabytes(_nodeInstance.NodeInternalStatsReportObject.NodeCacheWalletIndexMaxMemoryAllocation));
                                ClassLog.SimpleWriteLine("Whole active memory spend from the node: " + ClassUtility.ConvertBytesToMegabytes(_nodeInstance.NodeInternalStatsReportObject.NodeTotalMemoryUsage));
                            }
                            break;
                        default:
                            {
                                ClassLog.SimpleWriteLine("Command line: " + commandLine + " not exist.", ConsoleColor.Red);
                            }
                            break;

                    }
                }
            }
            catch (Exception error)
            {
                ClassLog.SimpleWriteLine("Error on the input command line: " + commandLine + " | Exception: " + error.Message, ConsoleColor.Red);
            }
            return true;
        }

        #region Command line functions.

        /// <summary>
        /// Show Solo Mining stats.
        /// </summary>
        private void CommandShowSoloMiningStats()
        {
            ClassLog.SimpleWriteLine("[Solo Mining stats]", ConsoleColor.Red);

            if (_soloMiningInstance != null)
            {
                if (_soloMiningInstance.GetMiningStatus)
                {
                    ClassLog.SimpleWriteLine("Total Hashrate: " + _soloMiningInstance.GetTotalHashrate + " H/s");
                    ClassLog.SimpleWriteLine("Total Share(s) produced: " + _soloMiningInstance.GetTotalShare);
                    ClassLog.SimpleWriteLine("Total Orphaned share(s): " + _soloMiningInstance.GetTotalAlreadyShare);
                    ClassLog.SimpleWriteLine("Total Unlock Share(s): " + _soloMiningInstance.GetTotalUnlockShare);
                    ClassLog.SimpleWriteLine("Total Refused Share(s): " + _soloMiningInstance.GetTotalRefusedShare);
                    ClassLog.SimpleWriteLine("Total Low Difficulty Share(s): " + _soloMiningInstance.GetTotalLowDifficultyShare);
                    ClassLog.SimpleWriteLine("Total Hashes: " + _soloMiningInstance.GetTotalHashes);
                }
                else
                    ClassLog.SimpleWriteLine("Solo mining is stopped.", ConsoleColor.Yellow);
            }
            else
                ClassLog.SimpleWriteLine("No solo mining instance has been started.", ConsoleColor.Yellow);
        }

        #endregion
    }
}
