
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_RPC_Wallet.Command.Enum;
using SeguraChain_RPC_Wallet.Config;
using SeguraChain_RPC_Wallet.Database;
using SeguraChain_RPC_Wallet.Database.Wallet;
using System;
using System.Threading;

namespace SeguraChain_RPC_Wallet.Command
{
    public class ClassRpcCommand
    {
        private ClassRpcConfig _rpcConfig;
        private ClassWalletDatabase _rpcWalletDatabase;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rpcConfig"></param>
        public ClassRpcCommand(ClassRpcConfig rpcConfig, ClassWalletDatabase rpcWalletDatabase)
        {
            _rpcConfig = rpcConfig;
            _rpcWalletDatabase = rpcWalletDatabase;
        }

        /// <summary>
        /// Activation des lignes de commandes.
        /// </summary>
        public void EnableRpcCommandLineSystem()
        {
            new Thread(() =>
            {
                while (_rpcConfig.RpcWalletEnabled)
                {
                    string commandLine = Console.ReadLine().ToLower();

                    switch (commandLine)
                    {
                        case ClassRpcEnumCommand.Help:
                            {
                                Console.WriteLine(ClassRpcEnumCommand.ListWallet + " - List every wallets");
                                Console.WriteLine(ClassRpcEnumCommand.Exit + " - Exit the wallet");
                            }
                            break;
                        case ClassRpcEnumCommand.ListWallet:
                            {
                                using (var walletList = _rpcWalletDatabase.GetListWalletAddress)
                                {
                                    foreach (string walletAddress in walletList.GetList)
                                    {
                                        ClassWalletData walletData = _rpcWalletDatabase.GetWalletDataFromWalletAddress(walletAddress);

                                        if (walletData != null)
                                        {
                                            Console.WriteLine(walletData.WalletAddress + " | Wallet Balance: " + (walletData.WalletBalance / BlockchainSetting.CoinDecimal) + " | Wallet Pending Balance: " + (walletData.WalletPendingBalance / BlockchainSetting.CoinDecimal));
                                            Console.WriteLine(walletData.WalletTransactionList.Count + " tx's count.");
                                        }

                                    }
                                }
                            }
                            break;
                        case ClassRpcEnumCommand.Save:
                            {
                                Console.WriteLine("Please write the password to encrypt the database: ");

                                _rpcWalletDatabase.SaveWalletDatabase(
                                _rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath,
                                 _rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseFilename, Console.ReadLine());
                            }
                            break;
                        case ClassRpcEnumCommand.Exit:
                            {

                                Console.WriteLine("Please write the password to encrypt the database: ");

                                _rpcWalletDatabase.SaveWalletDatabase(
                                    _rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath,
                                    _rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseFilename,
                                    Console.ReadLine());

                                _rpcConfig.RpcWalletEnabled = false;
                            }
                            break;
                        default:
                            Console.WriteLine("Command line not exist.");
                            break;
                    }
                }

            }).Start();

        }
    }
}
