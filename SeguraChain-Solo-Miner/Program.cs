using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Log;
using SeguraChain_Solo_Miner.Argument.Enum;
using SeguraChain_Solo_Miner.Argument.Function;
using SeguraChain_Solo_Miner.Exception;
using SeguraChain_Solo_Miner.Main;
using SeguraChain_Solo_Miner.Setting.Enum;
using SeguraChain_Solo_Miner.Setting.Function;
using SeguraChain_Solo_Miner.Setting.Object;

namespace SeguraChain_Solo_Miner
{
    class Program
    {
        /// <summary>
        /// Main miner object.
        /// </summary>
        private static ClassMainMiner _mainMiner;

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += ClassUnhandledException.UnhandledException;
            Console.CancelKeyPress += Console_CancelKeyPress;
            ClassLog.SimpleWriteLine(BlockchainSetting.CoinName + " Solo Miner " + Assembly.GetExecutingAssembly().GetName().Version, ConsoleColor.Magenta);

            bool startSoloMinerStatus = false;

            if (args.Length > 1)
            {
                ClassEnumLoadArgument loadArgument = ClassHandleArgument.HandleArgument(args, out ClassSoloMinerSettingObject soloMinerSettingObject);

                switch (loadArgument)
                {
                    case ClassEnumLoadArgument.NO_VALID_AMOUNT_ARGUMENTS:
                        {
                            if (args.Length == 1)
                            {
                                ClassLog.SimpleWriteLine("Specific mining setting file target argument selected, load setting file target..");
                                startSoloMinerStatus = LoadSoloMinerSetting(args[0]);
                            }
                            else
                            {
                                ClassLog.SimpleWriteLine("Invalid argument(s) count provided, default running way started.", ConsoleColor.DarkRed);
                                startSoloMinerStatus = LoadSoloMinerSetting(string.Empty);
                            }
                        }
                        break;
                    case ClassEnumLoadArgument.VALID_ARGUMENTS:
                        {
                            ClassLog.SimpleWriteLine("Valid argument(s) provided, start solo mining.", ConsoleColor.Cyan);
                            startSoloMinerStatus = StartSoloMiningInstance(soloMinerSettingObject);
                        }
                        break;
                    case ClassEnumLoadArgument.INVALID_ARGUMENTS:
                        {
                            ClassLog.SimpleWriteLine("Invalid argument(s) provided, here is an example of valid argument(s).", ConsoleColor.DarkRed);
                            ClassHandleArgument.ShowStartupArgumentExamples();
                            CloseSoloMiner();
                        }
                        break;
                }
            }
            else
                startSoloMinerStatus = LoadSoloMinerSetting(string.Empty);
        }

        /// <summary>
        /// Load the solo miner setting file.
        /// </summary>
        /// <param name="selectedPath"></param>
        private static bool LoadSoloMinerSetting(string selectedPath)
        {
            switch (ClassSoloMinerSettingFunction.LoadSoloMinerSettingFile(selectedPath, out ClassSoloMinerSettingObject soloMinerSettingObject))
            {
                case ClassSoloMinerSettingLoadEnum.FILE_NOT_FOUND:
                    {
                        ClassLog.SimpleWriteLine("File not found, initialize solo mining setting file.", ConsoleColor.Yellow);
                        ClassSoloMinerSettingFunction.InitializeSoloMinerSettingFile(out soloMinerSettingObject);
                        ClassLog.SimpleWriteLine("Setting file initialized, start solo mining.", ConsoleColor.Yellow);
                        return StartSoloMiningInstance(soloMinerSettingObject);
                    }
                case ClassSoloMinerSettingLoadEnum.FILE_ERROR:
                    ClassLog.SimpleWriteLine("Error on loading your mining setting file, do you want to initialize it again ? [Y/N]", ConsoleColor.DarkRed);

                    if ((Console.ReadLine() ?? string.Empty).ToLower() == "y")
                    {
                        ClassSoloMinerSettingFunction.InitializeSoloMinerSettingFile(out soloMinerSettingObject);
                        ClassLog.SimpleWriteLine("Setting file initialized, start solo mining.", ConsoleColor.Green);
                        return StartSoloMiningInstance(soloMinerSettingObject);
                    }
                    else
                    {
                        ClassLog.SimpleWriteLine("Setting file initialization ignored, close the solo miner.", ConsoleColor.Yellow);
                        CloseSoloMiner();
                    }
                    break;
                case ClassSoloMinerSettingLoadEnum.FILE_LOAD_SUCCESS:
                    {
                        ClassLog.SimpleWriteLine("Setting file loaded successfully, start solo mining.", ConsoleColor.Green);
                        return StartSoloMiningInstance(soloMinerSettingObject);
                    }

            }

            return false;
        }

        /// <summary>
        /// Start the solo mining instance.
        /// </summary>
        /// <param name="soloMinerSettingObject"></param>
        private static bool StartSoloMiningInstance(ClassSoloMinerSettingObject soloMinerSettingObject)
        {
            if (soloMinerSettingObject != null)
            {
                _mainMiner = new ClassMainMiner(soloMinerSettingObject);
                _mainMiner.StartAll();
            }
            else
            {
                ClassLog.SimpleWriteLine("[Error] The solo miner setting is empty. Closing miner..", ConsoleColor.DarkRed);
                CloseSoloMiner();
            }
            return false;
        }

        /// <summary>
        /// Close the solo miner.
        /// </summary>
        private static void CloseSoloMiner()
        {
            ClassLog.SimpleWriteLine("Press a key to exit.");
            Console.ReadLine();
            Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        ///     Force to close the process of the program by CTRL+C
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            ClassLog.SimpleWriteLine("Closing miner.", ConsoleColor.Red);
            Process.GetCurrentProcess().Kill();
        }
    }
}
