using System;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Log;
using SeguraChain_Solo_Miner.Argument.Enum;
using SeguraChain_Solo_Miner.Setting.Function;
using SeguraChain_Solo_Miner.Setting.Object;

namespace SeguraChain_Solo_Miner.Argument.Function
{
    public class ClassHandleArgument
    {
        /// <summary>
        /// Handle arguments on startup.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="soloMinerSettingObject"></param>
        /// <returns></returns>
        public static ClassEnumLoadArgument HandleArgument(string[] args, out ClassSoloMinerSettingObject soloMinerSettingObject)
        {
            ClassEnumLoadArgument loadArgumentResult = ClassEnumLoadArgument.NO_VALID_AMOUNT_ARGUMENTS;
            soloMinerSettingObject = null;

            if (args.Length == ClassEnumIndexArgument.MinArgumentCount)
            {
                bool error = true;

                if (ClassSoloMinerSettingFunction.CheckWalletAddressFormat(args[ClassEnumIndexArgument.IndexArgumentWalletAddress]))
                {
                    if (int.TryParse(args[ClassEnumIndexArgument.IndexArgumentMaxThread], out int maxThread) &&
                        ClassSoloMinerSettingFunction.CheckMaxThread(maxThread, true) &&
                        int.TryParse(args[ClassEnumIndexArgument.IndexArgumentThreadPriority], out int threadPriority) &&
                        ClassSoloMinerSettingFunction.CheckThreadPriority(threadPriority, true) &&
                        ClassSoloMinerSettingFunction.CheckPeerIpTarget(args[ClassEnumIndexArgument.IndexArgumentPeerIpTarget], true) &&
                        int.TryParse(args[ClassEnumIndexArgument.IndexArgumentPeerApiPortTarget], out int peerApiPortTarget) &&
                        ClassSoloMinerSettingFunction.CheckPeerApiPortTarget(peerApiPortTarget, true))
                    {
                        error = false;
                        loadArgumentResult = ClassEnumLoadArgument.VALID_ARGUMENTS;
                        soloMinerSettingObject = new ClassSoloMinerSettingObject(args[ClassEnumIndexArgument.IndexArgumentWalletAddress], maxThread, threadPriority, args[ClassEnumIndexArgument.IndexArgumentPeerIpTarget], peerApiPortTarget);
                    }
                }

                // One of arguments provided present an error.
                if (error)
                    loadArgumentResult = ClassEnumLoadArgument.INVALID_ARGUMENTS;
            }

            return loadArgumentResult;
        }

        /// <summary>
        /// Show example of startup arguments.
        /// </summary>
        public static void ShowStartupArgumentExamples()
        {
            ClassLog.SimpleWriteLine("First possibility: " + AppDomain.CurrentDomain.FriendlyName + " mining_config_file_path", ConsoleColor.Cyan);
            ClassLog.SimpleWriteLine("Example: " + AppDomain.CurrentDomain.FriendlyName + " C:\\my-config-file.json", ConsoleColor.Magenta);

            ClassLog.SimpleWriteLine("Second possibility: " + AppDomain.CurrentDomain.FriendlyName + " wallet_address thread_count thread_priority peer_ip_target peer_api_port_target", ConsoleColor.Cyan);
            ClassLog.SimpleWriteLine("Example: " + AppDomain.CurrentDomain.FriendlyName + " " + BlockchainSetting.DefaultWalletAddressDev + " 4 2 127.0.0.1 " + BlockchainSetting.PeerDefaultApiPort, ConsoleColor.Magenta);
        }
    }
}
