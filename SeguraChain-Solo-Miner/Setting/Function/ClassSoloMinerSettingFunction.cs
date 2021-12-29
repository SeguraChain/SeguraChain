using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;
using SeguraChain_Solo_Miner.Setting.Enum;
using SeguraChain_Solo_Miner.Setting.Object;

namespace SeguraChain_Solo_Miner.Setting.Function
{
    public class ClassSoloMinerSettingFunction
    {
        private const string DefaultSoloMinerSettingFilename = "solo-miner-setting.json";
        private static readonly string DefaultSoloMinerSettingFilePath = ClassUtility.ConvertPath(AppContext.BaseDirectory + "\\" + DefaultSoloMinerSettingFilename);

        /// <summary>
        /// Load the solo miner setting file.
        /// </summary>
        /// <param name="selectPath"></param>
        /// <param name="soloMinerSettingObject"></param>
        /// <returns></returns>
        public static ClassSoloMinerSettingLoadEnum LoadSoloMinerSettingFile(string selectPath, out ClassSoloMinerSettingObject soloMinerSettingObject)
        {
            if (selectPath.IsNullOrEmpty(out _))
                selectPath = DefaultSoloMinerSettingFilePath;

            if (File.Exists(selectPath))
            {
                string dataRead;

                using (StreamReader reader = new StreamReader(selectPath))
                    dataRead = reader.ReadToEnd();

                if (!dataRead.IsNullOrEmpty(out _))
                {
                    if (ClassUtility.TryDeserialize(dataRead, out soloMinerSettingObject))
                    {
                        if (soloMinerSettingObject?.SoloMinerWalletSetting != null &&
                            soloMinerSettingObject?.SoloMinerThreadSetting != null &&
                            soloMinerSettingObject?.SoloMinerNetworkSetting != null)
                        {
                            if (CheckWalletAddressFormat(soloMinerSettingObject.SoloMinerWalletSetting.wallet_address) &&
                                CheckMaxThread(soloMinerSettingObject.SoloMinerThreadSetting.max_thread, true) &&
                                CheckThreadPriority(soloMinerSettingObject.SoloMinerThreadSetting.thread_priority, true) &&
                                CheckPeerIpTarget(soloMinerSettingObject.SoloMinerNetworkSetting.peer_ip_target, true) &&
                                CheckPeerApiPortTarget(soloMinerSettingObject.SoloMinerNetworkSetting.peer_api_port_target, true))
                                return ClassSoloMinerSettingLoadEnum.FILE_LOAD_SUCCESS;
                        }
                    }
                }

                soloMinerSettingObject = null;
                return ClassSoloMinerSettingLoadEnum.FILE_ERROR;
            }

            soloMinerSettingObject = null;
            return ClassSoloMinerSettingLoadEnum.FILE_NOT_FOUND;
        }

        /// <summary>
        /// Initialize the solo miner setting file.
        /// </summary>
        public static void InitializeSoloMinerSettingFile(out ClassSoloMinerSettingObject soloMinerSettingObject)
        {
            ClassLog.SimpleWriteLine("Start initialization of the solo miner setting file.", ConsoleColor.Magenta);

            ClassLog.SimpleWriteLine("Write your wallet address: ", ConsoleColor.Yellow);

            string walletAddress = Console.ReadLine() ?? string.Empty;

            while (!CheckWalletAddressFormat(walletAddress))
            {
                ClassLog.SimpleWriteLine("Write your wallet address: ", ConsoleColor.Yellow);
                walletAddress = Console.ReadLine() ?? string.Empty;
            }

            ClassLog.SimpleWriteLine("Write your amount of threads [" + Environment.ProcessorCount + " thread(s) detected]: ", ConsoleColor.Yellow);

            int maxThread = 0;
            bool firstCheckDone = false;

            while (!CheckMaxThread(maxThread, firstCheckDone))
            {
                while (!int.TryParse(Console.ReadLine() ?? string.Empty, out maxThread))
                {
                    ClassLog.SimpleWriteLine("The input thread is invalid.", ConsoleColor.Red);
                    ClassLog.SimpleWriteLine("Write your amount of threads [" + Environment.ProcessorCount + " thread(s) detected]: ", ConsoleColor.Yellow);
                    firstCheckDone = true;
                }
            }

            ClassLog.SimpleWriteLine("Write the thread priority [Min: " + (int)ClassSoloMinerSettingThreadPriorityEnum.LOWEST + " | Max: " + (int)ClassSoloMinerSettingThreadPriorityEnum.HIGHEST + " ]: ", ConsoleColor.Yellow);
            int threadPriority = -1;
            firstCheckDone = false;

            while (!CheckThreadPriority(threadPriority, firstCheckDone))
            {
                while (!int.TryParse(Console.ReadLine() ?? string.Empty, out threadPriority))
                {
                    ClassLog.SimpleWriteLine("The input thread priority is invalid.", ConsoleColor.Red);
                    ClassLog.SimpleWriteLine("Write the thread priority [Min: " + ClassSoloMinerSettingThreadPriorityEnum.LOWEST + " | Max: " + ClassSoloMinerSettingThreadPriorityEnum.HIGHEST + "]: ", ConsoleColor.Yellow);
                    firstCheckDone = true;
                }
            }

            ClassLog.SimpleWriteLine("Write the ip of the peer target: ", ConsoleColor.Yellow);
            string peerIpTarget = Console.ReadLine();
            int peerApiPortTarget = 0;

            while (!CheckPeerIpTarget(peerIpTarget, true))
            {
                ClassLog.SimpleWriteLine("The input peer ip is invalid.", ConsoleColor.Red);
                peerIpTarget = Console.ReadLine();
            }

            ClassLog.SimpleWriteLine("Write the api peer port target: ", ConsoleColor.Yellow);

            firstCheckDone = false;
            while (!CheckPeerApiPortTarget(peerApiPortTarget, firstCheckDone))
            {
                while (!int.TryParse(Console.ReadLine() ?? string.Empty, out peerApiPortTarget))
                {
                    ClassLog.SimpleWriteLine("The input api port is invalid.", ConsoleColor.Red);
                    ClassLog.SimpleWriteLine("Write the api peer port target: ", ConsoleColor.Yellow);
                    firstCheckDone = true;
                }
            }


            soloMinerSettingObject = new ClassSoloMinerSettingObject(walletAddress, maxThread, threadPriority, peerIpTarget, peerApiPortTarget);

            using (StreamWriter writer = new StreamWriter(DefaultSoloMinerSettingFilePath))
                writer.Write(ClassUtility.SerializeData(soloMinerSettingObject, Formatting.Indented));

            ClassLog.SimpleWriteLine(DefaultSoloMinerSettingFilename + " saved successfully.", ConsoleColor.Magenta);
        }

        #region Check functions.

        /// <summary>
        /// Check wallet address format.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <returns></returns>
        public static bool CheckWalletAddressFormat(string walletAddress)
        {
            if (ClassBase58.DecodeWithCheckSum(walletAddress, true) == null)
            {
                ClassLog.SimpleWriteLine("The wallet address: " + walletAddress +" is invalid.", ConsoleColor.Red);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check the amount of thread(s) set.
        /// </summary>
        /// <param name="maxThread"></param>
        /// <param name="noticeError"></param>
        /// <returns></returns>
        public static bool CheckMaxThread(int maxThread, bool noticeError = false)
        {
            if (maxThread > 0)
                return true;

            if (noticeError)
                ClassLog.SimpleWriteLine("The select amount of thread(s): " + maxThread + " is invalid.", ConsoleColor.Red);

            return false;
        }

        /// <summary>
        /// Check the thread priority set.
        /// </summary>
        /// <param name="threadPriority"></param>
        /// <param name="noticeError"></param>
        /// <returns></returns>
        public static bool CheckThreadPriority(int threadPriority, bool noticeError = false)
        {
            switch (threadPriority)
            {
                case (int)ClassSoloMinerSettingThreadPriorityEnum.LOWEST:
                    return true;
                case (int)ClassSoloMinerSettingThreadPriorityEnum.LOW:
                    return true;
                case (int)ClassSoloMinerSettingThreadPriorityEnum.NORMAL:
                    return true;
                case (int)ClassSoloMinerSettingThreadPriorityEnum.ABOVE:
                    return true;
                case (int)ClassSoloMinerSettingThreadPriorityEnum.HIGHEST:
                    return true;
            }

            if (noticeError)
                ClassLog.SimpleWriteLine("The thread priority: " + threadPriority + " select is invalid.", ConsoleColor.Red);

            return false;
        }

        /// <summary>
        /// Check the peer ip target selected.
        /// </summary>
        /// <param name="peerIpTarget"></param>
        /// <param name="noticeError"></param>
        /// <returns></returns>
        public static bool CheckPeerIpTarget(string peerIpTarget, bool noticeError = false)
        {
            if (noticeError)
                ClassLog.SimpleWriteLine("The peer ip target: " + peerIpTarget + " selected is invalid.", ConsoleColor.Red);

            return IPAddress.TryParse(peerIpTarget, out _);
        }

        /// <summary>
        /// Check the peer api port target.
        /// </summary>
        /// <param name="peerApiPortTarget"></param>
        /// <param name="noticeError"></param>
        /// <returns></returns>
        public static bool CheckPeerApiPortTarget(int peerApiPortTarget, bool noticeError = false)
        {
            if (noticeError)
                ClassLog.SimpleWriteLine("The peer api port target: " + peerApiPortTarget + " selected is invalid.", ConsoleColor.Red);

            return peerApiPortTarget >= BlockchainSetting.PeerMinPort && peerApiPortTarget <= BlockchainSetting.PeerMaxPort;
        }

        #endregion
    }
}
