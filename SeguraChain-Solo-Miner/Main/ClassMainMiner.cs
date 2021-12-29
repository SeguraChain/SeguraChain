using System;
using System.Threading;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;
using SeguraChain_Solo_Miner.Command.Enum;
using SeguraChain_Solo_Miner.Mining;
using SeguraChain_Solo_Miner.Network.Function;
using SeguraChain_Solo_Miner.Network.Object;
using SeguraChain_Solo_Miner.Setting.Object;

namespace SeguraChain_Solo_Miner.Main
{
    public class ClassMainMiner
    {
        private ClassMiningNetworkStatsObject _miningNetworkStatsObject;
        private ClassMiningNetworkFunction _miningNetworkFunction;
        private ClassSoloMinerSettingObject _soloMinerSettingObject;
        private ClassSoloMiningInstance _soloMiningInstance;
        private Thread _threadCommandLines;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="soloMinerSettingObject"></param>
        public ClassMainMiner(ClassSoloMinerSettingObject soloMinerSettingObject)
        {
            _soloMinerSettingObject = soloMinerSettingObject;
            _miningNetworkStatsObject = new ClassMiningNetworkStatsObject();
            _miningNetworkFunction = new ClassMiningNetworkFunction(_soloMinerSettingObject, _miningNetworkStatsObject);
            _soloMiningInstance = new ClassSoloMiningInstance(_soloMinerSettingObject, _miningNetworkStatsObject, _miningNetworkFunction);
        }

        /// <summary>
        /// Start everything.
        /// </summary>
        public void StartAll()
        {
            if (_soloMinerSettingObject.SoloMinerMiscSetting.enable_log)
            {
                if (ClassLog.InitializeWriteLog())
                {
                    ClassLog.EnableWriteLogTask();
                    ClassLog.ChangeLogLevel((int)ClassEnumLogLevelType.LOG_LEVEL_GENERAL);
                    ClassLog.WriteLine("Log system enabled successfully.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);
                }
            }

            ClassLog.ChangeLogLevel((int)ClassEnumLogLevelType.LOG_LEVEL_MINING);


            ClassLog.WriteLine("[Mining setting]" +
                                     "\n - Wallet Address: " + _soloMinerSettingObject.SoloMinerWalletSetting.wallet_address +
                                     "\n - Thread(s): " + _soloMinerSettingObject.SoloMinerThreadSetting.max_thread +
                                     "\n - Thread Priority: " + _soloMinerSettingObject.SoloMinerThreadSetting.thread_priority +
                                     "\n - Peer IP Target: " + _soloMinerSettingObject.SoloMinerNetworkSetting.peer_ip_target +
                                     "\n - Peer API Port Target: " + _soloMinerSettingObject.SoloMinerNetworkSetting.peer_api_port_target, ClassEnumLogLevelType.LOG_LEVEL_MINING, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);

            _miningNetworkFunction.StartMiningNetworkTask();
            _soloMiningInstance.StartMining();
            StartCommandLines();
        }

        /// <summary>
        /// Start the command lines system.
        /// </summary>
        private void StartCommandLines()
        {
            _threadCommandLines = new Thread(delegate () 
            {
                HelpCommand();
                while (true)
                {
                    try
                    {

                        switch (Console.ReadKey(true).KeyChar)
                        {
                            case ClassEnumCommandLines.Help:
                                HelpCommand();
                                break;
                            case ClassEnumCommandLines.Stats:
                                StatsCommand();
                                break;
                            case ClassEnumCommandLines.Pause:
                                PauseCommand();
                                break;
                        }
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            });
            _threadCommandLines.Start();
        }

        #region Command lines functions.

        /// <summary>
        /// Help command line.
        /// </summary>
        private void HelpCommand()
        {
            ClassLog.SimpleWriteLine("List of available commands: ", ConsoleColor.Magenta);
            ClassLog.SimpleWriteLine(ClassEnumCommandLines.Help + " - Show every command lines.");
            ClassLog.SimpleWriteLine(ClassEnumCommandLines.Stats + " - Show mining stats.");
            ClassLog.SimpleWriteLine(ClassEnumCommandLines.Pause + " - Enable/Disable mining pause.");
        }

        /// <summary>
        /// Stats command line.
        /// </summary>
        private void StatsCommand()
        {
            ClassLog.SimpleWriteLine("[Mining Stats]", ConsoleColor.Magenta);

            if (_miningNetworkStatsObject.GetBlockTemplateObject != null)
            {
                switch (_soloMiningInstance.GetMiningStatus)
                {
                    case true:
                        if (_soloMiningInstance.GetSetMiningPauseStatus)
                            ClassLog.SimpleWriteLine("Mining status: Pause.", ConsoleColor.Cyan);
                        else
                        {
                            ClassLog.SimpleWriteLine("Mining status: Active.", ConsoleColor.Green);
                            ClassLog.SimpleWriteLine("Current Block Height: " + _miningNetworkStatsObject.GetBlockTemplateObject.BlockHeight);
                            ClassLog.SimpleWriteLine("Current Block Difficulty: " + _miningNetworkStatsObject.GetBlockTemplateObject.BlockDifficulty);
                            ClassLog.SimpleWriteLine("Current Block Hash: " + _miningNetworkStatsObject.GetBlockTemplateObject.BlockHash);
                        }
                        break;
                    case false:
                        ClassLog.SimpleWriteLine("Mining status: totally stopped.");
                        break;
                }
            }
            else
                ClassLog.SimpleWriteLine("Mining status: Waiting blocktemplate.", ConsoleColor.Red);

            ClassLog.SimpleWriteLine("Total Hashrate: " + ClassUtility.GetFormattedHashrate(_soloMiningInstance.GetTotalHashrate));
            ClassLog.SimpleWriteLine("Total hashes: " + _soloMiningInstance.GetTotalHashes);
            ClassLog.SimpleWriteLine("Total share(s) produced: " + _soloMiningInstance.GetTotalShare);
            ClassLog.SimpleWriteLine("Total potential unlocked block(s): " + _miningNetworkStatsObject.GetTotalUnlockedBlock);
            ClassLog.SimpleWriteLine("Total orphaned block(s): " + _miningNetworkStatsObject.GetTotalOrphanedBlock);
            ClassLog.SimpleWriteLine("Total refused share(s): " + _miningNetworkStatsObject.GetTotalRefusedShare);
            ClassLog.SimpleWriteLine("Total invalid share(s): " + _miningNetworkStatsObject.GetTotalInvalidShare);
        }

        /// <summary>
        /// Päuse command line.
        /// </summary>
        private void PauseCommand()
        {
            if (_soloMiningInstance.GetSetMiningPauseStatus)
            {
                _soloMiningInstance.GetSetMiningPauseStatus = false;
                ClassLog.SimpleWriteLine("Mining pause disabled.", ConsoleColor.Green);
            }
            else
            {
                _soloMiningInstance.GetSetMiningPauseStatus = true;
                ClassLog.SimpleWriteLine("Mining pause enabled.", ConsoleColor.Yellow);
            }
        }

        #endregion
    }
}
