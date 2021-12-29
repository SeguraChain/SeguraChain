using Newtonsoft.Json;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Utility;
using SeguraChain_RPC_Wallet.API.Service.Server;
using SeguraChain_RPC_Wallet.Command;
using SeguraChain_RPC_Wallet.Config;
using SeguraChain_RPC_Wallet.Database;
using SeguraChain_RPC_Wallet.Node.Client;
using SeguraChain_RPC_Wallet.Task;
using System;
using System.IO;

namespace SeguraChain_RPC_Wallet
{
    class Program
    {
        
        private static ClassRpcConfig _rpcConfig;
        private static ClassRpcTaskSystem _rpcTaskSystem;
        private static ClassWalletDatabase _rpcWalletDatabase;
        private static ClassNodeApiClient _rpcNodeApiClient;
        private static ClassRpcApiServer _rpcApiServer;
        private static ClassRpcCommand _rpcCommand;


        static void Main(string[] args)
        {
            Console.WriteLine(BlockchainSetting.CoinName + " - RPC-Wallet");

            Console.WriteLine("Loading the RPC Wallet config file.");

            if (!LoadRpcWallet())
            {
                Console.WriteLine("Press a key to close the program.");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("The RPC Wallet has been loaded successfully.");
                _rpcCommand = new ClassRpcCommand(_rpcConfig, _rpcWalletDatabase);
                _rpcCommand.EnableRpcCommandLineSystem();
            }
        }

        /// <summary>
        /// Load the RPC Wallet content.
        /// </summary>
        /// <returns></returns>
        private static bool LoadRpcWallet()
        {
            if (!File.Exists(AppContext.BaseDirectory + "\\" + ClassRpcConfigPath.RpcConfigPath))
            {
                Console.WriteLine("The RPC Wallet config file is not found.");

                _rpcConfig = ClassRpcConfigFunction.BuildRpcWalletConfig();

                using (StreamWriter writer = new StreamWriter(AppContext.BaseDirectory + "\\" + ClassRpcConfigPath.RpcConfigPath))
                    writer.Write(ClassUtility.SerializeData(_rpcConfig, Formatting.Indented));

                return true;
            }
            else
            {
                #region Load the RPC Wallet Setting.

                using (StreamReader reader = new StreamReader(AppContext.BaseDirectory + "\\" + ClassRpcConfigPath.RpcConfigPath))
                {
                    if (!ClassUtility.TryDeserialize(reader.ReadToEnd(), out _rpcConfig))
                    {
                        Console.WriteLine("Cannot deserialize the RPC config file.");
                        return false;
                    }

                    if (!ClassRpcConfigFunction.CheckRpcWalletConfig(_rpcConfig))
                    {
                        Console.WriteLine("Invalid RPC Wallet Config settings on the file. Please check it.");
                        return false;
                    }
                }

                #endregion
            }

            #region Load the RPC Wallet Database.

            _rpcWalletDatabase = new ClassWalletDatabase();

            string rpcWalletDatabasePassword = string.Empty;

            if (_rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseEnableEncryption)
            {
                Console.WriteLine("Please input the RPC Wallet database password: ");

                rpcWalletDatabasePassword = Console.ReadLine();
            }

            if (!_rpcWalletDatabase.LoadWalletDatabase(_rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath, _rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseFilename, rpcWalletDatabasePassword))
            {
                Console.WriteLine("Failed to read the RPC Wallet database.");
                return false;
            }

            #endregion

            #region Init the RPC Wallet Node API Client.

            _rpcNodeApiClient = new ClassNodeApiClient(_rpcWalletDatabase, _rpcConfig);

            _rpcNodeApiClient.EnableAutoUpdateNodeStatsTask();

            #endregion

            #region Launch the RPC Wallet API Server.

            _rpcApiServer = new ClassRpcApiServer(_rpcConfig, _rpcNodeApiClient);

            _rpcApiServer.StartApiServer();

            #endregion

            return true;
        }
    }
}
