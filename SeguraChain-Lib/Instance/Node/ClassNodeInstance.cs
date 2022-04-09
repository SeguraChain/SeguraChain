using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Open.Nat;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.MemPool.Database;
#if !NET5_0_OR_GREATER
using SeguraChain_Lib.Blockchain.Setting;
#endif
using SeguraChain_Lib.Blockchain.Sovereign.Database;
using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Server.Service;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.Service;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ServerSync.Service;
using SeguraChain_Lib.Instance.Node.Report.Object;
using SeguraChain_Lib.Instance.Node.Setting.Function;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Instance.Node.Tasks;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Instance.Node
{
    public class ClassNodeInstance
    {
        // Setting object.
        public bool PeerToolStatus;
        public ClassNodeSettingObject PeerSettingObject;

        // Network objects.
        public ClassPeerNetworkSyncServerObject PeerNetworkServerObject;
        public ClassPeerNetworkSyncServiceObject PeerNetworkClientSyncObject;
        public ClassPeerApiServerServiceObject PeerApiServerObject;
        public ClassPeerNetworkBroadcastInstanceMemPool PeerNetworkBroadcastInstanceMemPoolObject;

        // OpenNat objects.
        public bool PeerOpenNatInitializationStatus;
        public NatDiscoverer NatDiscovererObject;
        public string PeerOpenNatPublicIp;

        // Task object.
        private ClassPeerUpdateTask _peerUpdateTask;

        /// <summary>
        /// Internal stats object.
        /// </summary>
        public ClassNodeInternalStatsReportObject NodeInternalStatsReportObject;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassNodeInstance()
        {
            _peerUpdateTask = new ClassPeerUpdateTask(this);
            NodeInternalStatsReportObject = new ClassNodeInternalStatsReportObject();
        }


        #region Peer Main Functions.

        /// <summary>
        /// Full initialization of the peer tool.
        /// </summary>
        /// <returns></returns>
        public bool NodeStart(bool fromWallet)
        {
            TaskManager.TaskManager.EnableTaskManager();
            PeerToolStatus = true;

            string encryptionKey = string.Empty;
            if (PeerSettingObject.PeerBlockchainDatabaseSettingObject.DataSetting.EnableEncryptionDatabase && !fromWallet)
            {
                Console.WriteLine("Write your encryption key, she is necessary for encrypt/decrypt data:");
                Console.WriteLine("[Notice] if your data is encrypted and the key is wrong, you can't load propertly your data saved.");

                encryptionKey = Console.ReadLine();
            }

            TuningNode();

            if (ClassSovereignUpdateDatabase.LoadSovereignUpdateData(encryptionKey, PeerSettingObject.PeerBlockchainDatabaseSettingObject.DataSetting.EnableCompressDatabase, PeerSettingObject.PeerBlockchainDatabaseSettingObject.DataSetting.EnableEncryptionDatabase))
            {
                if (ClassPeerDatabase.LoadPeerDatabase(PeerSettingObject.PeerNetworkSettingObject))
                {

                    if (ClassMemPoolDatabase.LoadMemPoolDatabase(encryptionKey, PeerSettingObject.PeerBlockchainDatabaseSettingObject))
                    {
                        if (ClassBlockchainDatabase.LoadBlockchainDatabase(PeerSettingObject.PeerBlockchainDatabaseSettingObject, encryptionKey, false, fromWallet).Result)
                        {
                            #region Enable public peer mode. (If enabled and possible to enable).

                            if (PeerSettingObject.PeerNetworkSettingObject.PublicPeer)
                            {
                                if (!PeerSettingObject.PeerNetworkSettingObject.IsDedicatedServer)
                                {
#if NET5_0_OR_GREATER
                                    ClassLog.WriteLine("Your setting indicate it's not a dedicated server.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                                    ClassLog.WriteLine("Remember to open the P2P port "+PeerSettingObject.PeerNetworkSettingObject.ListenApiPort+" and target the host IP: "+PeerSettingObject.PeerNetworkSettingObject.ListenIp+" to your router.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);

                                    PeerOpenNatPublicIp = PeerSettingObject.PeerNetworkSettingObject.ListenIp;
#else
                                    Task<bool> openNatTask = PeerOpenNatPort();
                                    openNatTask.Wait();

                                    if (openNatTask.Result)
                                    {
                                        PeerOpenNatInitializationStatus = true;
                                        ClassLog.WriteLine("Peer port and the API Port successfully opened with OpenNAT to the public network.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    }
                                    else
                                    {
                                        PeerSettingObject.PeerNetworkSettingObject.PublicPeer = false;
                                        ClassLog.WriteLine("Can't open peer port with OpenNAT to the public network. Disable public mode.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    }
#endif
                                }
                                else
                                    PeerOpenNatPublicIp = PeerSettingObject.PeerNetworkSettingObject.ListenIp;
                            }

                            #endregion

                            ClassLog.WriteLine("Start peer network server..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                            PeerNetworkServerObject = new ClassPeerNetworkSyncServerObject(PeerOpenNatPublicIp, PeerSettingObject.PeerNetworkSettingObject, PeerSettingObject.PeerFirewallSettingObject);

                            if (PeerNetworkServerObject.StartPeerServer())
                            {
                                ClassLog.WriteLine("Peer server started.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                #region Peer sync network task.

                                ClassLog.WriteLine("Enable Peer Sync Network Task(s)..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                PeerNetworkClientSyncObject = new ClassPeerNetworkSyncServiceObject(PeerOpenNatPublicIp, PeerSettingObject.PeerNetworkSettingObject, PeerSettingObject.PeerFirewallSettingObject);
                                PeerNetworkClientSyncObject.EnablePeerSyncTask();

                                ClassLog.WriteLine("Peer Sync Task(s) enabled.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                #endregion

                                ClassLog.WriteLine("Start peer API server..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                PeerApiServerObject = new ClassPeerApiServerServiceObject(PeerOpenNatPublicIp, PeerSettingObject.PeerNetworkSettingObject, PeerSettingObject.PeerFirewallSettingObject);

                                if (PeerApiServerObject.StartPeerApiServer())
                                {
                                    #region Peer Update tasks.

                                    _peerUpdateTask.StartPeerUpdateTasks();

                                    ClassLog.WriteLine("Peer Update Task(s) system started.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                    #endregion

                                    PeerNetworkBroadcastInstanceMemPoolObject = new ClassPeerNetworkBroadcastInstanceMemPool(PeerSettingObject.PeerNetworkSettingObject, PeerSettingObject.PeerFirewallSettingObject, PeerOpenNatPublicIp);
                                    PeerNetworkBroadcastInstanceMemPoolObject.RunNetworkBroadcastMemPoolInstanceTask();

                                    return true;
                                }

                                PeerApiServerObject.Dispose();
#if !NET5_0_OR_GREATER
                                if (PeerSettingObject.PeerNetworkSettingObject.PublicPeer)
                                    ClosePortOpenNat();
#endif

                                ClassLog.WriteLine("Can't start peer api server. Press a key to exit.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                if (!fromWallet)
                                    Console.ReadLine();
                            }
                            else
                            {
                                PeerNetworkServerObject.Dispose();
#if !NET5_0_OR_GREATER
                                if (PeerSettingObject.PeerNetworkSettingObject.PublicPeer)
                                    ClosePortOpenNat();
#endif

                                ClassLog.WriteLine("Can't start peer network server. Press a key to exit.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                if (!fromWallet)
                                    Console.ReadLine();
                            }
                        }
                        else
                        {
                            ClassLog.WriteLine("Can't load Blockchain database. Press a key to exit.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            if (!fromWallet)
                                Console.ReadLine();
                        }
                    }
                    else
                    {
                        ClassLog.WriteLine("Can't load Wallet MemPool database. Press a key to exit.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        if (!fromWallet)
                            Console.ReadLine();
                    }
                }
                else
                {
                    ClassLog.WriteLine("Can't load Peer Database. Press a key to exit.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    if (!fromWallet)
                        Console.ReadLine();
                }
            }
            else
            {
                ClassLog.WriteLine("Can't load and apply Sovereign Update Database. Press a key to exit.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                if (!fromWallet)
                    Console.ReadLine();
            }

            return false;
        }

        /// <summary>
        /// Apply tuning node.
        /// </summary>
        private void TuningNode()
        {
            ThreadPool.SetMinThreads(PeerSettingObject.PeerNetworkSettingObject.PeerMinThreadsPool, PeerSettingObject.PeerNetworkSettingObject.PeerMinThreadsPoolCompletionPort);
            ThreadPool.SetMaxThreads(PeerSettingObject.PeerNetworkSettingObject.PeerMaxThreadsPool, PeerSettingObject.PeerNetworkSettingObject.PeerMaxThreadsPoolCompletionPort);
        }

        /// <summary>
        /// Peer tool closed.
        /// </summary>
        public async Task NodeStop(bool forceClose = false, bool isWallet = false)
        {
            ClassLog.WriteLine("Close Peer Tool, please wait a moment..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            PeerToolStatus = false;

            TaskManager.TaskManager.StopTaskManager();

            #region Close OpenNAT port.

#if !NET5_0_OR_GREATER
            ClosePortOpenNat();
#endif

            #endregion

            #region Stop peer task client broadcast mempool.

            PeerNetworkBroadcastInstanceMemPoolObject?.StopNetworkBroadcastMemPoolInstance();

            #endregion

            #region Stop peer task sync.

            ClassLog.WriteLine("Stop Peer Sync Task Network..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            PeerNetworkClientSyncObject?.StopPeerSyncTask();
            PeerNetworkClientSyncObject?.Dispose();
            ClassLog.WriteLine("Peer Sync Task Network stopped.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            #endregion

            #region Stop peer network server.

            ClassLog.WriteLine("Stop Peer Network Server..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            PeerNetworkServerObject?.StopPeerServer();
            PeerNetworkServerObject?.Dispose();
            ClassLog.WriteLine("Peer Network Server stoped.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            #endregion

            #region Stop api network server.

            ClassLog.WriteLine("Stop Peer API Server..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            PeerApiServerObject?.StopPeerApiServer();
            PeerApiServerObject?.Dispose();
            ClassLog.WriteLine("Peer API Server stopped.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            #endregion

            #region Stop the task of blockchain transaction confirmation.

            _peerUpdateTask?.StopAutomaticBlockTransactionConfirmation();

            #endregion

            #region Stop automatic peer tasks.

            _peerUpdateTask?.StopAutomaticUpdateTask();

            #endregion

            #region Save peer list.

            ClassLog.WriteLine("Save peer list..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            if (ClassPeerDatabase.SavePeers(string.Empty, true))
                ClassLog.WriteLine("Peer list saved.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            else
                ClassLog.WriteLine("Peer list saved failed.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

            #endregion

            #region Save Sovereign update Data.

            ClassLog.WriteLine("Save Sovereign Update(s) data..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            if (ClassSovereignUpdateDatabase.SaveSovereignUpdateObjectData(out int totalSaved))
                ClassLog.WriteLine("Save " + totalSaved + " Sovereign Update(s) data successfully done.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            else
                ClassLog.WriteLine("Save Sovereign Update(s) data failed.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            #endregion

            #region Save Mem Pool Data.

            ClassLog.WriteLine("Save MemPool data..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            if (ClassMemPoolDatabase.SaveMemPoolDatabase(PeerSettingObject.PeerBlockchainDatabaseSettingObject))
                ClassLog.WriteLine("MemPool data saved.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            else
                ClassLog.WriteLine("MemPool data saved failed.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

            #endregion

            #region Save Blockchain Data.

            ClassLog.WriteLine("Save Blockchain data..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

            if (await ClassBlockchainDatabase.SaveBlockchainDatabase(PeerSettingObject.PeerBlockchainDatabaseSettingObject))
            {
                await ClassBlockchainDatabase.CloseBlockchainDatabase(PeerSettingObject.PeerBlockchainDatabaseSettingObject);
                ClassLog.WriteLine("Blockchain data saved.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
            }
            else
                ClassLog.WriteLine("Blockchain data saved failed.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

            #endregion


            if (!forceClose)
            {
                ClassLog.CloseLogStreams();
                ClassLog.WriteLine("Peer tool successfully closed. Press a key to exit.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                Console.ReadLine();
            }
            else
            {
                if (!isWallet)
                    Process.GetCurrentProcess().Kill();
            }
        }

        #endregion

        #region Peer Initialization functions.

        /// <summary>
        /// Initialize Peer Setting from peer setting file.
        /// </summary>
        /// <returns></returns>
        public bool PeerInitializationSetting()
        {

            bool loadPeerSetting = ClassPeerNodeSettingFunction.LoadPeerSetting(out PeerSettingObject);

            while (!loadPeerSetting)
            {
                ClassLog.WriteLine("Do you want to initialize again the peer setting? [Y/N]", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                string choose = Console.ReadLine() ?? string.Empty;
                if (!choose.IsNullOrEmpty(true, out string chooseTrimmed) && chooseTrimmed?.ToLower() == "y")
                {

                    loadPeerSetting = ClassPeerNodeSettingFunction.InitializePeerSetting(out PeerSettingObject);

                    if (loadPeerSetting)
                        return true;
                }
                else
                    break;
            }

            return loadPeerSetting;
        }

        #region OpenNAT.

        #if !NET5_0_OR_GREATER
        
                /// <summary>
                /// Open Peer port with NAT to get the port available to the public network.
                /// </summary>
                /// <returns></returns>
                private async Task<bool> PeerOpenNatPort()
                {
                    try
                    {
                        ClassLog.WriteLine("Attempt to open peer P2P port with OpenNAT to the public network..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                        NatDiscovererObject = new NatDiscoverer();

                        var device = await NatDiscovererObject.DiscoverDeviceAsync();

                        PeerOpenNatPublicIp = device.GetExternalIPAsync().Result.ToString();

                        ClassLog.WriteLine("External IP Address is: " + PeerOpenNatPublicIp, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                        await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, PeerSettingObject.PeerNetworkSettingObject.ListenPort, PeerSettingObject.PeerNetworkSettingObject.ListenPort, BlockchainSetting.CoinName + " Peer Tool - P2P Port"));

                    }
                    catch (Exception error)
                    {
                        ClassLog.WriteLine("Error on attempt to open the peer P2P port with OpenNAT to the public network. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        return false;
                    }

                    try
                    {
                        ClassLog.WriteLine("Attempt to open peer API port with OpenNAT to the public network..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                        var device = await NatDiscovererObject.DiscoverDeviceAsync();

                        await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, PeerSettingObject.PeerNetworkSettingObject.ListenApiPort, PeerSettingObject.PeerNetworkSettingObject.ListenApiPort, BlockchainSetting.CoinName + " Peer Tool - API Port"));

                    }
                    catch (Exception error)
                    {
                        ClassLog.WriteLine("Error on attempt to open the peer API port with OpenNAT to the public network. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        return false;
                    }
                    return true;
                }

                /// <summary>
                /// Close the OpenNAT port.
                /// </summary>
                private void ClosePortOpenNat()
                {
                    if (PeerOpenNatInitializationStatus)
                    {
                        if (NatDiscovererObject != null)
                        {
                            try
                            {
                                var device = NatDiscovererObject.DiscoverDeviceAsync().Result;
                                device.DeletePortMapAsync(new Mapping(Protocol.Tcp, PeerSettingObject.PeerNetworkSettingObject.ListenPort, PeerSettingObject.PeerNetworkSettingObject.ListenPort)).Wait();
                                ClassLog.WriteLine("OpenNAT port closed.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                            }
                            catch
                            {
                                // Ignored.
                            }
                        }
                    }

                }

        #endif

        #endregion

        #endregion
    }
}
