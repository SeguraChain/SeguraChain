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
                                            Console.WriteLine(walletData.WalletAddress + " | Wallet Balance: " + walletData.WalletBalance + " | Wallet Pending Balance: " + walletData.WalletPendingBalance);
                                            Console.WriteLine(walletData.WalletTransactionList.Count + " tx's count.");
                                        }

                                    }
                                }
                            }
                            break;
                        default:

                            break;
                    }
                }

            }).Start();

        }
    }
}
