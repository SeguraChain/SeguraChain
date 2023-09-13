using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Instance.Node.Network.Services.Firewall.Manager;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Instance.Node.Tasks
{
    public class ClassPeerUpdateTask
    {
        /// <summary>
        /// The node instance object.
        /// </summary>
        private readonly ClassNodeInstance _nodeInstance;

        /// <summary>
        /// Cancellation of each parallel update tasks of the node.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSourceUpdateTask;

        /// <summary>
        /// Intervals of tasks.
        /// </summary>
#if !NET5_0_OR_GREATER
        private const int CheckOpenNatPublicIpInterval = 5 * 1000;
#endif
        private const int ManageApiFirewallInterval = 2 * 1000;
        private const int CleanUpApiDeadConnectionInterval = 10 * 1000;
        private const int CleanUpPeerDeadConnectionInterval = 60 * 1000;
        private const int UpdateBlockTransactionConfirmationInterval = 1 * 1000;
        private const int UpdateNodeInternalStats = 1 * 1000;
        private const int UpdateBlockchainStatsInterval = 1 * 1000;
        private const int CleanUpGcInterval = 60 * 1000;

        /// <summary>
        /// Indicate to the task of block transactions confirmations to stop once a task is complete instead to force to stop the process in pending.
        /// </summary>
        private bool _enableCallStopTaskUpdateBlockTransaction;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeInstance"></param>
        public ClassPeerUpdateTask(ClassNodeInstance nodeInstance)
        {
            _nodeInstance = nodeInstance;
            _enableCallStopTaskUpdateBlockTransaction = false;
        }

        #region Manage automatic tasks.

        /// <summary>
        /// Start every Peer Update Tasks.
        /// </summary>
        public void StartPeerUpdateTasks()
        {
            _cancellationTokenSourceUpdateTask = new CancellationTokenSource();

#if !NET5_0_OR_GREATER

            if (_nodeInstance.PeerSettingObject.PeerNetworkSettingObject.PublicPeer)
            {
                if (!_nodeInstance.PeerSettingObject.PeerNetworkSettingObject.IsDedicatedServer)
                {
                    if (_nodeInstance.PeerOpenNatInitializationStatus)
                        StartTaskCheckOpenNatPublicIp();
                    else
                        ClassLog.WriteLine("Can't start the task who manage the OpenNAT because the initialization of the port by OpenNAT have failed.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                }
            }
#endif

            StartTaskConfirmBlockTransaction();
            StartTaskCleanUpClosedApiClientConnection();
            StartTaskCleanUpClosedPeerClientConnection();

            if (_nodeInstance.PeerSettingObject.PeerFirewallSettingObject.PeerEnableFirewallLink)
                StartTaskManageApiFirewall();

            StartTaskUpdateBlockchainNetworkStats();
            StartTaskUpdateNodeInternalStats();
            StartTaskUpdateGarbageCollection();
        }

        /// <summary>
        /// Call to stop propertly after the last task of block transaction confirmation done.
        /// </summary>
        /// <returns></returns>
        public async Task StopAutomaticBlockTransactionConfirmation()
        {
            try
            {
                ClassLog.WriteLine("Waiting the last block transaction confirmation task has been done..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Yellow);
                _enableCallStopTaskUpdateBlockTransaction = true;

                // Await to complete the last task of block transaction confirmations.
                while (_enableCallStopTaskUpdateBlockTransaction)
                    await Task.Delay(100);

                ClassLog.WriteLine("The last block transaction confirmation task has been done, continue to closing of the node.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
            }
#if !DEBUG
            catch
            { 
#else
            catch (Exception error)
            {
                Debug.WriteLine("Error on waiting the last block transaction confirmation: " + error.Message);
#endif
            }
        }

        /// <summary>
        /// Stop every automatic tasks.
        /// </summary>
        public void StopAutomaticUpdateTask()
        {
            try
            {
                if (_cancellationTokenSourceUpdateTask != null)
                {
                    if (!_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                        _cancellationTokenSourceUpdateTask.Cancel();
                }
            }
            catch
            {
                // Ignored.
            }
        }

        #endregion

        #region Tasks functions

#if !NET5_0_OR_GREATER

        /// <summary>
        /// Start a task for change public IP get from OpenNAT.
        /// </summary>
        private void StartTaskCheckOpenNatPublicIp()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    while (_nodeInstance.PeerToolStatus)
                    {
                        if (_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                            break;

                        try
                        {
                            var device = await _nodeInstance.NatDiscovererObject.DiscoverDeviceAsync();

                            string publicIpOpenNat = device.GetExternalIPAsync().Result.ToString();
                            if (_nodeInstance.PeerOpenNatPublicIp != publicIpOpenNat)
                            {
                                _nodeInstance.PeerOpenNatPublicIp = publicIpOpenNat;
                                ClassLog.WriteLine("WARNING - Your public IP has been changed, update every services..", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);

                                _nodeInstance.PeerOpenNatPublicIp = publicIpOpenNat;

                                _nodeInstance.PeerNetworkServerObject.PeerIpOpenNatServer = publicIpOpenNat;
                                _nodeInstance.PeerNetworkClientSyncObject.PeerOpenNatServerIp = publicIpOpenNat;

                            }
                        }
                        catch(Exception error)
                        {
                            ClassLog.WriteLine("Error on the task who manage OpenNAT. Exception: "+error.Message, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        }
                        await Task.Delay(CheckOpenNatPublicIpInterval, _cancellationTokenSourceUpdateTask.Token);

                    }
                }, _cancellationTokenSourceUpdateTask.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

#endif
        /// <summary>
        /// Start a task who manage the API Firewall.
        /// </summary>
        private void StartTaskManageApiFirewall()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                while (_nodeInstance.PeerToolStatus)
                {

                    try
                    {
                        if (_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                            break;

                        ClassPeerFirewallManager.ManageFirewallLink(_nodeInstance.PeerSettingObject.PeerFirewallSettingObject.PeerFirewallName, _nodeInstance.PeerSettingObject.PeerFirewallSettingObject.PeerFirewallChainName);
                    }
                    catch (Exception error)
                    {
                        ClassLog.WriteLine("Error on the task who manage the Firewall of the API. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_FIREWALL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                    }
                    await Task.Delay(ManageApiFirewallInterval);

                }
            }), 0, _cancellationTokenSourceUpdateTask).Wait();

        }

        /// <summary>
        /// Start a task who clean up all closed api client connection.
        /// </summary>
        private void StartTaskCleanUpClosedApiClientConnection()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                while (_nodeInstance.PeerToolStatus)
                {

                    try
                    {
                        if (_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                            break;

                        long totalClosed = _nodeInstance.PeerApiServerObject.CleanUpAllIncomingClosedConnection(out int totalIp);

                        if (totalClosed > 0)
                            ClassLog.WriteLine("Total incoming dead api connection cleaned: " + totalClosed + " | Total IP: " + totalIp, ClassEnumLogLevelType.LOG_LEVEL_API_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                    }
                    catch
                    {
                        // Ignored.
                    }
                    await Task.Delay(CleanUpApiDeadConnectionInterval);
                }
            }), 0, _cancellationTokenSourceUpdateTask).Wait();

        }

        /// <summary>
        /// Start a task whop clean up all closed peer client connection.
        /// </summary>
        private void StartTaskCleanUpClosedPeerClientConnection()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                while (_nodeInstance.PeerToolStatus)
                {

                    try
                    {
                        if (_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                            break;

                        long totalClosed = _nodeInstance.PeerNetworkServerObject.CleanUpAllIncomingClosedConnection(out int totalIp);

                        if (totalClosed > 0)
                            ClassLog.WriteLine("Total incoming dead peer connection cleaned: " + totalClosed + " | Total IP: " + totalIp, ClassEnumLogLevelType.LOG_LEVEL_PEER_SERVER, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                    }
                    catch
                    {
                        // Ignored.
                    }
                    await Task.Delay(CleanUpPeerDeadConnectionInterval);
                }

            }), 0, _cancellationTokenSourceUpdateTask).Wait();

        }

        /// <summary>
        /// Start a task who automatically update blockchain network stats from data synced.
        /// </summary>
        private void StartTaskUpdateBlockchainNetworkStats()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                while (_nodeInstance.PeerToolStatus)
                {
                    try
                    {
                        if (_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                            break;
                        await ClassBlockchainStats.UpdateBlockchainNetworkStats(true, _cancellationTokenSourceUpdateTask);
                    }
                    catch
                    {
                        // Ignored.
                    }
                    await Task.Delay(UpdateBlockchainStatsInterval);
                }
            }), 0, _cancellationTokenSourceUpdateTask).Wait();

        }

        /// <summary>
        /// Start a task who automatically confirm transactions.
        /// </summary>
        private void StartTaskConfirmBlockTransaction()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                long previousBlockHeightUnlockedChecked = 0;
                while (_nodeInstance.PeerToolStatus)
                {
                    try
                    {
                        if (_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                            break;

                        if (ClassBlockchainStats.BlockCount > 0)
                        {
                            bool blockMissing = false;

                            using (DisposableList<long> listBlockMissing = await ClassBlockchainStats.GetListBlockMissing(ClassBlockchainStats.GetLastBlockHeight(), false, true, _cancellationTokenSourceUpdateTask, 0))
                            {
                                if (listBlockMissing.Count > 0)
                                {
                                    blockMissing = true;
                                    ClassLog.WriteLine("Can't start to confirm block transaction(s) synced at the moment, their is " + listBlockMissing.Count + " block(s) missed to sync.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                                }
                            }

                            #region Travel blocks and select blocks unlocked where the task of confirmations are not done.

                            if (!blockMissing)
                            {
                                long lastBlockHeightUnlockedChecked = await ClassBlockchainStats.GetLastBlockHeightNetworkConfirmationChecked(_cancellationTokenSourceUpdateTask);

                                if (lastBlockHeightUnlockedChecked >= BlockchainSetting.GenesisBlockHeight)
                                {
                                    if (previousBlockHeightUnlockedChecked < lastBlockHeightUnlockedChecked)
                                    {
                                        ClassBlockchainBlockConfirmationResultObject blockConfirmationResultObject = await ClassBlockchainDatabase.BlockchainMemoryManagement.UpdateBlockDataTransactionConfirmations(lastBlockHeightUnlockedChecked, _cancellationTokenSourceUpdateTask);
                                        if (blockConfirmationResultObject.Status)
                                        {
                                            if (blockConfirmationResultObject.LastBlockHeightConfirmationDone > 0)
                                            {
                                                if (blockConfirmationResultObject.ListBlockHeightConfirmed.Count > 0)
                                                {
                                                    while (!await ClassBlockchainDatabase.BlockchainMemoryManagement.IncrementBlockTransactionConfirmationOnBlockFullyConfirmed(blockConfirmationResultObject.ListBlockHeightConfirmed.GetList, lastBlockHeightUnlockedChecked, _cancellationTokenSourceUpdateTask))
                                                    {
                                                        if (_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                                                            break;


                                                        ClassLog.WriteLine("Failed to confirm back every block heights transactions fully confirmed.", ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);

#if DEBUG
                                                        Debug.WriteLine("Failed to confirm back every block heights transactions fully confirmed.");
#endif
                                                    }

                                                    ClassLog.WriteLine("New block height transaction confirmation reach: " + previousBlockHeightUnlockedChecked + " Total block fully confirmed: " + blockConfirmationResultObject.ListBlockHeightConfirmed.Count, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
#if DEBUG
                                                    Debug.WriteLine("New block height transaction confirmation reach: " + previousBlockHeightUnlockedChecked + " Total block fully confirmed: " + blockConfirmationResultObject.ListBlockHeightConfirmed.Count);
#endif

                                                    blockConfirmationResultObject.ListBlockHeightConfirmed.Clear();
                                                }

                                                previousBlockHeightUnlockedChecked = blockConfirmationResultObject.LastBlockHeightConfirmationDone;
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion
                        }

                        if (_enableCallStopTaskUpdateBlockTransaction)
                        {
                            _enableCallStopTaskUpdateBlockTransaction = false;
#if DEBUG
                            Debug.WriteLine("Stop task of block transaction confirmation asked.");
#endif
                            break;
                        }


                        await Task.Delay(UpdateBlockTransactionConfirmationInterval);
                    }
                    catch (Exception error)
                    {
                        if (!_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                            ClassLog.WriteLine("Error on the task who confirm automatically blockchain transactions. Exception: " + error.Message, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_TRANSACTION_CONFIRMATION, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, true);
                        else
                            break;
                    }
                }

            }), 0, _cancellationTokenSourceUpdateTask).Wait();

        }

        /// <summary>
        /// Start a task who automatically update the node internal stats.
        /// </summary>
        private void StartTaskUpdateNodeInternalStats()
        {
            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                while (_nodeInstance.PeerToolStatus)
                {

                    try
                    {
                        if (_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                            break;

                        _nodeInstance.NodeInternalStatsReportObject.NodeCacheMemoryUsage = ClassBlockchainDatabase.BlockchainMemoryManagement.GetActiveMemoryUsageFromCache();
                        _nodeInstance.NodeInternalStatsReportObject.NodeCacheMaxMemoryAllocation = _nodeInstance.PeerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalMaxActiveMemoryAllocationFromCache;
                        _nodeInstance.NodeInternalStatsReportObject.NodeCacheTransactionMemoryUsage = ClassBlockchainDatabase.BlockchainMemoryManagement.GetBlockTransactionCachedMemorySize();
                        _nodeInstance.NodeInternalStatsReportObject.NodeCacheTransactionMaxMemoryAllocation = _nodeInstance.PeerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalCacheMaxBlockTransactionKeepAliveMemorySize;
                        _nodeInstance.NodeInternalStatsReportObject.NodeCacheWalletIndexMemoryUsage = ClassBlockchainDatabase.BlockchainMemoryManagement.BlockchainWalletIndexMemoryCacheObject.LastMemoryUsage;
                        _nodeInstance.NodeInternalStatsReportObject.NodeCacheWalletIndexMaxMemoryAllocation = _nodeInstance.PeerSettingObject.PeerBlockchainDatabaseSettingObject.BlockchainCacheSetting.GlobalCacheMaxWalletIndexKeepMemorySize;
                        _nodeInstance.NodeInternalStatsReportObject.NodeTotalMemoryUsage = ClassUtility.GetMemoryAllocationFromProcess();
                    }
                    catch
                    {
                        // Ignored, can happen depending the state of the node, for example if this one is in pending to close for example.
                    }


                    await Task.Delay(UpdateNodeInternalStats);
                }

            }), 0, _cancellationTokenSourceUpdateTask).Wait();
        }

        /// <summary>
        /// Start a task to clear the garbage collection.
        /// </summary>
        private void StartTaskUpdateGarbageCollection()
        {

            TaskManager.TaskManager.InsertTask(new Action(async () =>
            {
                while (_nodeInstance.PeerToolStatus)
                {
                    try
                    {
                        if (_cancellationTokenSourceUpdateTask.IsCancellationRequested)
                            break;

                        ClassUtility.CleanGc();
                    }
                    catch
                    {
                        // Ignored.
                    }

                    await Task.Delay(CleanUpGcInterval);
                }
            }), 0, _cancellationTokenSourceUpdateTask).Wait();

        }

        #endregion

    }
}
