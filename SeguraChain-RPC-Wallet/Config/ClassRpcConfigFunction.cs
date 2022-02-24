using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Utility;
using System;
using System.IO;
using System.Net;

namespace SeguraChain_RPC_Wallet.Config
{
    public class ClassRpcConfigFunction
    {
        /// <summary>
        /// Build the RPC Wallet configuration file.
        /// </summary>
        /// <returns></returns>
        public static ClassRpcConfig BuildRpcWalletConfig()
        {
            ClassRpcConfig rpcConfig = new ClassRpcConfig();

            Console.WriteLine("Do you want to make a custom RPC Wallet config file? [Y/N]");

            bool doCustomConfig = Console.ReadLine().ToLower() == "y";

            if (doCustomConfig)
            {
                #region API Settings part.

                #region Enable secret key part.

                Console.WriteLine("Do you want to use a secret key ? [Y/N]");

                rpcConfig.RpcApiSetting.RpcApiEnableSecretKey = Console.ReadLine().ToLower() == "y";

                if (rpcConfig.RpcApiSetting.RpcApiEnableSecretKey)
                {
                    Console.WriteLine("Write a password, this one will be used a secret key for encrypt request:");

                    bool initKey = false;

                    while (!initKey)
                    {
                        string secretKey = Console.ReadLine();

                        while (secretKey.IsNullOrEmpty(false, out _))
                        {
                            Console.WriteLine("The password is empty, please write a password:");
                            secretKey = Console.ReadLine();
                        }

                        rpcConfig.RpcApiSetting.RpcApiSecretKey = secretKey;

                        // Compute the API Secret Key if this one is initialized.
                        if (ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringUtf8(secretKey), true, out rpcConfig.RpcApiSetting.RpcApiSecretKeyArray))
                        {
                            rpcConfig.RpcApiSetting.RpcApiSecretIvArray = ClassAes.GenerateIv(rpcConfig.RpcApiSetting.RpcApiSecretKeyArray);
                            initKey = true;
                        }
                        else
                            Console.WriteLine("The API secret key build has failed, please set another password.");
                    }
                }

                #endregion

                #region Enable whitelist part.

                Console.WriteLine("Do you want to enable the Whitelist System ? [Y/N]");
                Console.WriteLine("If the Whitelist System is enabled, every others incoming IP's cannot connect to the API system.");

                rpcConfig.RpcApiSetting.RpcApiEnableWhitelist = Console.ReadLine().ToLower() == "y";

                if (rpcConfig.RpcApiSetting.RpcApiEnableWhitelist)
                {
                    while (true)
                    {
                        bool cancel = false;

                        string inputIp = Console.ReadLine();

                        while (!IPAddress.TryParse(inputIp, out _))
                        {
                            Console.WriteLine("The input IP format is invalid, please try again or write exit to cancel:");
                            inputIp = Console.ReadLine();

                            cancel = inputIp == "exit";
                        }

                        if (cancel)
                            break;

                        rpcConfig.RpcApiSetting.RpcApiWhitelist.Add(inputIp);

                        Console.WriteLine("Do you want to put another IP into the whitelist ? [Y/N]");

                        if (Console.ReadLine().ToLower() != "y")
                            break;
                    }
                }

                #endregion

                #region API setting part.

                Console.WriteLine("Input the IP who will be used as listened IP by the API of the RPC Wallet: ");
                string apiIp = Console.ReadLine();

                while (!IPAddress.TryParse(apiIp, out _))
                {
                    Console.WriteLine("The input IP is invalid, please try again: ");
                    apiIp = Console.ReadLine();
                }

                rpcConfig.RpcApiSetting.RpcApiIp = apiIp;

                Console.WriteLine("Input the port who will be used by the API of the RPC Wallet: ");
                string apiPort = Console.ReadLine();

                while (!int.TryParse(apiPort, out rpcConfig.RpcApiSetting.RpcApiPort) || rpcConfig.RpcApiSetting.RpcApiPort <= 0 || rpcConfig.RpcApiSetting.RpcApiPort > 65535)
                {
                    Console.WriteLine("The input port is invalid, please try again: ");
                    apiPort = Console.ReadLine();
                }

                Console.WriteLine("Input the semaphore delay for the API part");
                Console.WriteLine("this element permit to put by force a delay between each handles of the incoming connections (in milliseconds): ");
                string semaphoreInput = Console.ReadLine();

                while (!int.TryParse(semaphoreInput, out rpcConfig.RpcApiSetting.RpcApiSemaphoreTimeout) || rpcConfig.RpcApiSetting.RpcApiSemaphoreTimeout <= 0)
                {
                    Console.WriteLine("The input of the semaphore delay is invalid, please try again: ");
                    semaphoreInput = Console.ReadLine();
                }

                Console.WriteLine("Input a max delay to keep alive an incoming connection to the API (in seconds):");

                string maxDelayInput = Console.ReadLine();

                while (!int.TryParse(maxDelayInput, out rpcConfig.RpcApiSetting.RpcApiMaxConnectDelay) || rpcConfig.RpcApiSetting.RpcApiMaxConnectDelay <= 0)
                {
                    Console.WriteLine("The input of max delay is invalid, please try again (in seconds):");
                    maxDelayInput = Console.ReadLine();
                }

                #endregion

                #endregion

                #region API Node Settings part.

                Console.WriteLine("Please write the listened IP of the node API:");

                rpcConfig.RpcNodeApiSetting.RpcNodeApiIp = Console.ReadLine();

                while (!IPAddress.TryParse(rpcConfig.RpcNodeApiSetting.RpcNodeApiIp, out _))
                {
                    Console.WriteLine("The Listened node API IP is invalid please try again: ");
                    rpcConfig.RpcNodeApiSetting.RpcNodeApiIp = Console.ReadLine();
                }

                Console.WriteLine("Please write the listened port of the node API: ");

                string nodeApiPort = Console.ReadLine();

                while (!int.TryParse(nodeApiPort, out rpcConfig.RpcNodeApiSetting.RpcNodeApiPort) || rpcConfig.RpcNodeApiSetting.RpcNodeApiPort <= 0)
                {
                    Console.WriteLine("The input port is invalid, please try again:");
                    nodeApiPort = Console.ReadLine();
                }

                Console.WriteLine("Please write the max delay of an outgoing connection to the Node API (in seconds):");

                string nodeMaxDelay = Console.ReadLine();

                while (!int.TryParse(nodeMaxDelay, out rpcConfig.RpcNodeApiSetting.RpcNodeApiMaxDelay) || rpcConfig.RpcNodeApiSetting.RpcNodeApiMaxDelay <= 0)
                {
                    Console.WriteLine("The input port is invalid, please try again:");
                    nodeMaxDelay = Console.ReadLine();
                }

                #endregion

                #region RPC Wallet database settings part.

                Console.WriteLine("Do you want to enable the compression on the wallet database file? [Y/N]");
                rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseEnableCompression = Console.ReadLine()?.ToLower() == "y";

                Console.WriteLine("Do you want to enable the encryption on the wallet database file? [Y/N]");
                rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseEnableEncryption = Console.ReadLine()?.ToLower() == "y";

                Console.WriteLine("Do you want to enable the JSON format on the wallet database file? [Y/N]");
                rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseEnableJsonFormat = Console.ReadLine()?.ToLower() == "y";

                Console.WriteLine("Input the wallet database path: ");
                rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath = Console.ReadLine();

                while (rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseEnableEncryption)
                {
                    try
                    {
                        Directory.CreateDirectory(rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath);
                    }
                    catch
                    {
                        Console.WriteLine("Cannot create the rpc wallet database directory, please put another path: ");
                        rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath = Console.ReadLine();
                    }
                }

                #endregion
            }

            return rpcConfig;
        }

        /// <summary>
        /// Check the rpc wallet config file.
        /// </summary>
        /// <param name="rpcConfig"></param>
        /// <returns></returns>
        public static bool CheckRpcWalletConfig(ClassRpcConfig rpcConfig)
        {
            if (rpcConfig == null || rpcConfig?.RpcApiSetting == null || rpcConfig?.RpcNodeApiSetting == null || rpcConfig?.RpcWalletDatabaseSetting == null)
            {
                Console.WriteLine("A configuration of the RPC Wallet is empty.");
                return false;
            }

            if (rpcConfig.RpcApiSetting.RpcApiEnableSecretKey)
            {
                if (rpcConfig.RpcApiSetting.RpcApiSecretIvArray == null || rpcConfig.RpcApiSetting.RpcApiSecretKeyArray == null || !rpcConfig.RpcApiSetting.RpcApiSecretKey.IsNullOrEmpty(false, out _))
                {
                    Console.WriteLine("The RPC Wallet database directory path " + rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath + " is empty or empty.");
                    return false;
                }
            }

            if (!IPAddress.TryParse(rpcConfig.RpcApiSetting.RpcApiIp, out _) || !IPAddress.TryParse(rpcConfig.RpcNodeApiSetting.RpcNodeApiIp, out _))
            {
                Console.WriteLine("The RPC Wallet database directory path " + rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath + " is empty or empty.");
                return false;
            }

            if (rpcConfig.RpcApiSetting.RpcApiPort <= 0 || rpcConfig.RpcApiSetting.RpcApiPort > 65535 ||
                rpcConfig.RpcApiSetting.RpcApiMaxConnectDelay <= 0 || rpcConfig.RpcApiSetting.RpcApiSemaphoreTimeout <= 0 ||
                rpcConfig.RpcApiSetting.RpcApiMaxConnectDelay <= 0 ||
                rpcConfig.RpcApiSetting.RpcApiSemaphoreTimeout <= 0)
            {
                Console.WriteLine("One or some configurations of the RPC Wallet API configurations is/are invalid.");
                return false;
            }

            if (rpcConfig.RpcNodeApiSetting.RpcNodeApiPort <= 0 || rpcConfig.RpcNodeApiSetting.RpcNodeApiPort > 65535 || rpcConfig.RpcNodeApiSetting.RpcNodeApiMaxDelay <= 0)
            {
                Console.WriteLine("One or some configurations of the RPC Wallet Node API configuration is/are invalid.");
                return false;
            }

            if (!rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath.IsNullOrEmpty(false, out _))
            {
                Console.WriteLine("The RPC Wallet database directory path " + rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabasePath + " is null or empty.");
                return false;
            }

            if (!rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseFilename.IsNullOrEmpty(false, out _))
            {
                Console.WriteLine("The RPC Wallet database filename " + rpcConfig.RpcWalletDatabaseSetting.RpcWalletDatabaseFilename + " is null or empty.");
                return false;
            }

            return true;
        }
    }
}
