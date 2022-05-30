using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_RPC_Wallet.API.Service.Server;
using SeguraChain_RPC_Wallet.Command;
using SeguraChain_RPC_Wallet.Config;
using SeguraChain_RPC_Wallet.Config.Enum;
using SeguraChain_RPC_Wallet.Database;
using SeguraChain_RPC_Wallet.Node.Client;
using SeguraChain_RPC_Wallet.RpcTask;
using System;

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

            _rpcWalletDatabase = new ClassWalletDatabase();


            #region Load the RPC Wallet Setting.

            switch (ClassRpcConfigFunction.CheckRpcWalletConfig(AppContext.BaseDirectory + "\\" + ClassRpcConfigPath.RpcConfigPath, out _rpcConfig))
            {
                case ClassRpcEnumConfig.DATABASE_DIRECTORY_NOT_EXIST:
                case ClassRpcEnumConfig.DATABASE_FILE_NOT_EXIST:
                    {
                        if (!_rpcWalletDatabase.InitWalletDatabase(_rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath, _rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseFilename))
                        {
                            Console.WriteLine("Failed to init the wallet database.");
                            return false;
                        }
                    }
                    break;
                case ClassRpcEnumConfig.INVALID_CONFIG:
                    {
                        Console.WriteLine("Invalid RPC Wallet Config settings on the file. Please check it.");
                        return false;
                    }
                case ClassRpcEnumConfig.VALID_CONFIG:
                    {
                        Console.WriteLine("The RPC Wallet Config is valid");
                    }
                    break;
            }

            #endregion
            

            #region Load the RPC Wallet Database.

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
