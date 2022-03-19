using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Numerics;
using Newtonsoft.Json;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Function;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Blockchain.Database.Memory.Main.Enum;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Blockchain.Wallet.Object.Wallet;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Client.Enum;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.API.Packet.SubPacket.Response;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Utility;
using System.Threading;


namespace SeguraChain_Test_Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("Select the function to test:");

                Console.WriteLine((int)EnumListTestMenu.TEST_WALLET_GENERATOR + " - Test Wallet Generator with Signature message system.");
                Console.WriteLine((int)EnumListTestMenu.TEST_TRANSACTION_BUILDER + " - Test Transaction builder + test transaction signature.");
                Console.WriteLine((int)EnumListTestMenu.TEST_TRANSACTION_TRANSFER_BUILDER + " - Test Transaction Transfer build + test both transaction signatures.");
                Console.WriteLine((int)EnumListTestMenu.TEST_PEER_API_REQUEST + " - Test to make a request and send it to a peer api server.");
                Console.WriteLine((int)EnumListTestMenu.TEST_GENERATE_FAKE_BLOCK + " - Test to make fake blocks. Remember, just the first block is valid, others generated manually with this function are not accepted because they are not mined propertly.");
                Console.WriteLine((int)EnumListTestMenu.BUILD_BLOCKCHAIN +" - Build your own blockchain. Remember the genesis block reward target the dev wallet address.");
                Console.WriteLine((int)EnumListTestMenu.EXIT + " - Exit.");
                string choose = Console.ReadLine();

                if (int.TryParse(choose, out var idChoose))
                {
                    switch (idChoose)
                    {
                        case (int)EnumListTestMenu.TEST_WALLET_GENERATOR:
                            TestWalletGenerator();
                            break;
                        case (int)EnumListTestMenu.TEST_TRANSACTION_BUILDER:
                            TestTransactionBuilderSigned();
                            break;
                        case (int)EnumListTestMenu.TEST_TRANSACTION_TRANSFER_BUILDER:
                            TestTransactionTransferBuilderSigned();
                            break;
                        case (int)EnumListTestMenu.TEST_PEER_API_REQUEST:
                            TestPeerApiServer();
                            break;
                        case (int)EnumListTestMenu.TEST_GENERATE_FAKE_BLOCK:
                            TestFakeBlockGenerator();
                            break;
                        case (int)EnumListTestMenu.BUILD_BLOCKCHAIN:
                            BuildBlockchain();
                            break;
                        case (int)EnumListTestMenu.EXIT:
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Invalid input, press a key to continue..");
                            Console.ReadLine();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input, press a key to continue..");
                    Console.ReadLine();
                }

                Console.Clear();
            }

            Console.WriteLine("Press a key to exit.");
            Console.ReadLine();
        }

        #region Functions dedicated to tests wallet, transactions in general.

        /// <summary>
        /// This function test the wallet generator and the signature message system.
        /// </summary>
        private static void TestWalletGenerator()
        {
            bool signatureCheckStatut;

            string baseWord = string.Empty;

            Console.WriteLine("Do you want to use Base word(s) for generate your wallet? [Y/N]");
            Console.WriteLine("Note: It's not recommended to use this option and let the normal system to generate a random private key.");

            ClassWalletGeneratorObject walletObject;

            string choose = Console.ReadLine() ?? string.Empty;

            if (choose.ToLower() == "y")
            {
                Console.Clear();
                Console.WriteLine("Write your Base word(s) for generate your private key:");
                baseWord = Console.ReadLine() ?? string.Empty;
                Console.WriteLine("Generate a valid wallet by Base word(s) + slow way, please wait..");
                walletObject = ClassWalletUtility.GenerateWallet(baseWord);

            }
            else
            {
                Console.WriteLine("Do you want to use the fast generator way? [Y/N]");
                choose = Console.ReadLine() ?? string.Empty;

                if (choose.ToLower() == "y")
                {
                    Console.WriteLine("Generate a valid wallet by fast way, please wait..");
                    walletObject = ClassWalletUtility.GenerateWallet(string.Empty, true);

                }
                else
                {
                    Console.WriteLine("Generate a valid wallet by slow way, please wait..");

                    walletObject = ClassWalletUtility.GenerateWallet(string.Empty);
                }
            }

            Console.WriteLine("Valid wallet generated: ");
            Console.WriteLine("");

            #region Show wallet informations generated.

            Console.WriteLine("Private Key WIF: " + walletObject.WalletPrivateKey);
            Console.WriteLine("Public Key WIF: " + walletObject.WalletPublicKey);
            Console.WriteLine("Wallet Address: " + walletObject.WalletAddress);
            if (!baseWord.IsNullOrEmpty(false, out _))
                Console.WriteLine("Base word(s) used: " + baseWord);

            #endregion

            Console.WriteLine("");
            Console.WriteLine("Save those informations on a paper or somewhere else.");
            Console.WriteLine("Press a key to continue.");
            Console.ReadLine();
            Console.Clear();

            Console.WriteLine("Do you want to test the signature system [Y/N]");

            choose = Console.ReadLine() ?? string.Empty;

            if (choose.ToLower() == "y")
            {
                while (true)
                {


                    Console.WriteLine("Write something to test yourself the signature system (write: \"exit\" to close the program):");

                    string message = Console.ReadLine() ?? string.Empty;

                    if (message.ToLower() == "exit")
                        break;

                    Console.WriteLine("Test sign content: " + message + " ..");

                    string messageSignature = ClassWalletUtility.WalletGenerateSignature(walletObject.WalletPrivateKey, message);

                    Console.WriteLine("Content signature: " + messageSignature + " from content: " + message);

                    signatureCheckStatut = ClassWalletUtility.WalletCheckSignature(message, messageSignature, walletObject.WalletPublicKey);


                    if (signatureCheckStatut)
                        Console.WriteLine("Content: " + message + " signed and checked successfully.");
                    else
                        Console.WriteLine("The content has been signed but the checker return an error.");

                    Console.WriteLine("Generate another wallet to test the signature security check..");

                    ClassWalletGeneratorObject walletObject2 = ClassWalletUtility.GenerateWallet(string.Empty, true);

                    Console.WriteLine("Wallet 2 - Private Key WIF: " + walletObject2.WalletPrivateKey);
                    Console.WriteLine("Wallet 2 - Public Key WIF: " + walletObject2.WalletPublicKey);
                    Console.WriteLine("Wallet 2 - Wallet Address: " + walletObject2.WalletAddress);

                    try
                    {
                        signatureCheckStatut = ClassWalletUtility.WalletCheckSignature(message, messageSignature, walletObject2.WalletPublicKey);
                    }
                    catch
                    {
                        // Ignoré.
                    }

                    if (signatureCheckStatut)
                        Console.WriteLine("Error the signature signed by the current wallet work with the other wallet generated..");
                    else
                        Console.WriteLine("Great, the other wallet can't valid the signature generated from the first wallet by his private key.");

                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("Press a key to continue.");
                    Console.ReadLine();
                    Console.Clear();
                }

                Console.Clear();
                Console.WriteLine("Remember to save those informations on a paper or somewhere else (last warning).");
                Console.WriteLine("");

                #region Show wallet informations generated.

                Console.WriteLine("Private Key WIF: " + walletObject.WalletPrivateKey);
                Console.WriteLine("Public Key WIF: " + walletObject.WalletPublicKey);
                Console.WriteLine("Wallet Address: " + walletObject.WalletAddress);

                if (!baseWord.IsNullOrEmpty(false, out _))
                    Console.WriteLine("Base word(s) used: " + baseWord);

                #endregion
            }

            Console.WriteLine("");
            Console.WriteLine("Press a key to return to the main menu..");
            Console.ReadLine();
            Console.Clear();
        }

        /// <summary>
        /// This function test the transaction builder and the transaction signature system.
        /// </summary>
        private static void TestTransactionBuilderSigned()
        {
            Console.Clear();
            Console.WriteLine("This function is a just test and not impact your wallet until you don't send the transaction yourself on the Blockchain.");

            #region Input Wallet address sender.

            Console.WriteLine("Please write your wallet address: ");
            string walletAddress = Console.ReadLine();
            Console.Clear();

            #endregion

            #region Input public key.

            Console.WriteLine("Write your public key:");
            string walletPublicKey = Console.ReadLine();
            Console.Clear();

            #endregion

            #region Input private key.

            Console.WriteLine("Write your private key: ");
            string walletPrivateKey = Console.ReadLine();
            Console.Clear();

            #endregion

            #region Input Wallet Address target.

            Console.WriteLine("Write the wallet address target: ");
            string walletAddressTarget = Console.ReadLine();
            Console.Clear();

            #endregion

            #region Input transaction amount.

            Console.WriteLine("Write the amount of " + BlockchainSetting.CoinMinName + " to send: ");
            decimal amount;

            while (!decimal.TryParse(Console.ReadLine(), out amount))
                Console.WriteLine("Error the input is invalid, write the amount of " + BlockchainSetting.CoinMinName + " to send: ");

            amount *= BlockchainSetting.CoinDecimal;
            amount = ClassUtility.RemoveDecimalPoint(amount);

            Console.Clear();

            #endregion

            #region Input transaction fee.

            Console.WriteLine("Write the amount fee of " + BlockchainSetting.CoinMinName + " to send: ");
            decimal fee;

            while (!decimal.TryParse(Console.ReadLine(), out fee))
                Console.WriteLine("Error the input is invalid, write the amount fee of " + BlockchainSetting.CoinMinName + " to send: ");

            fee *= BlockchainSetting.CoinDecimal;
            fee = ClassUtility.RemoveDecimalPoint(fee);

            Console.Clear();

            #endregion

            #region Input block height start target.

            Console.WriteLine("Write a block start height has you want: ");
            long blockHeight;
            while (!long.TryParse(Console.ReadLine(), out blockHeight))
                Console.WriteLine("Error the input is invalid, write a block height has you want: ");

            Console.Clear();

            #endregion

            #region Input Payment ID.
			
            Console.WriteLine("Write a payment ID, leave empty for set default value:");

            if (!long.TryParse(Console.ReadLine(), out var paymentId))
                paymentId = 0;

            #endregion

            long blockHeighTarget = blockHeight + BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations;

            Console.Write("To valid your transaction amount sources, please write each tx hash and the amount to spend: ");

            Dictionary<string, ClassTransactionHashSourceObject> amountSourceList = new Dictionary<string, ClassTransactionHashSourceObject>();

            while (true)
            {
                Console.WriteLine("Write a transaction hash: ");
                string txHash = Console.ReadLine();
                Console.WriteLine("Write the amount to spend from this tx hash: ");
                decimal txAmount;

                while (!decimal.TryParse(Console.ReadLine(), out txAmount))
                    Console.WriteLine("Error the input is invalid, write the amount to spend propertly: ");

                txAmount *= BlockchainSetting.CoinDecimal;

                long blockHeightTx;

                Console.WriteLine("Input the block height of this transaction: ");

                while (!long.TryParse(Console.ReadLine(), out blockHeightTx))
                    Console.WriteLine("Error the input is invalid, write the block height of the transaction propertly: ");

                if (!amountSourceList.ContainsKey(txHash))
                {
                    amountSourceList.Add(txHash, new ClassTransactionHashSourceObject()
                    {
                        Amount = (BigInteger)txAmount,
                    });
                }

                Console.WriteLine("Do you want to insert more tx sources?[Y/N]");

                if ((Console.ReadLine() ?? string.Empty).ToLower() != "y")
                    break;
            }


            ClassTransactionObject transactionObject = ClassTransactionUtility.BuildTransaction(blockHeight, blockHeighTarget, walletAddress, walletPublicKey, string.Empty, (BigInteger)amount, (BigInteger)fee, walletAddressTarget, ClassUtility.GetCurrentTimestampInSecond(), ClassTransactionEnumType.NORMAL_TRANSACTION, paymentId, string.Empty, string.Empty, walletPrivateKey, string.Empty, amountSourceList, 0, new CancellationTokenSource());

            if (transactionObject != null)
            {
                Console.WriteLine("Transaction generated and signed: ");

                Console.WriteLine(ClassUtility.SerializeData(transactionObject, Formatting.Indented));

                Console.WriteLine("Check transaction signature..");

                if (ClassWalletUtility.WalletCheckSignature(transactionObject.TransactionHash, transactionObject.TransactionSignatureSender, transactionObject.WalletPublicKeySender))
                    Console.WriteLine("The transaction signature has been checked and is valid.");
                else
                    Console.WriteLine("The transaction signature has been checked and is invalid.");

#if DEBUG
                Stopwatch watchProcessTimespend = new Stopwatch();
                watchProcessTimespend.Start();
#endif

                ClassTransactionEnumStatus resultCheckTransaction = ClassBlockchainStats.CheckTransaction(transactionObject, null, false, null, null, false).Result;


#if DEBUG
                watchProcessTimespend.Stop();
                Debug.WriteLine("Time spend to check the transaction by the normal function: " + watchProcessTimespend.ElapsedMilliseconds + " ms.");
#endif

                if (resultCheckTransaction == ClassTransactionEnumStatus.VALID_TRANSACTION)
                    Console.WriteLine("The whole transaction object data has been checked and is valid.");
                else
                    Console.WriteLine("The whole transaction object data is invalid, result: " + resultCheckTransaction);

            }

            Console.WriteLine("Press a key to return to the main menu.");
            Console.ReadLine();
        }

        /// <summary>
        /// This function test the transaction transfer builder and the transaction signature system.
        /// </summary>
        private static void TestTransactionTransferBuilderSigned()
        {
            Console.Clear();
            Console.WriteLine("This function is a just test and not impact your wallet until you don't send the transaction transfer yourself on the Blockchain.");

            Console.WriteLine("Please write the wallet address of the sender: ");
            string walletAddressSender = Console.ReadLine();
            Console.Clear();

            Console.WriteLine("Write the public key of the sender:");
            string walletPublicKeySender = Console.ReadLine();
            Console.Clear();

            Console.WriteLine("Write the private key of the sender: ");
            string walletPrivateKeySender = Console.ReadLine();
            Console.Clear();

            Console.WriteLine("Write the wallet address target: ");
            string walletAddressTarget = Console.ReadLine();
            Console.Clear();

            Console.WriteLine("Write the public key of the receiver:");
            string walletPublicKeyReceiver = Console.ReadLine();
            Console.Clear();

            Console.WriteLine("Write the private key of the receiver: ");
            string walletPrivateKeyReceiver = Console.ReadLine();
            Console.Clear();

            Console.WriteLine("Write the amount of " + BlockchainSetting.CoinMinName + " to send: ");
            decimal amount;

            while (!decimal.TryParse(Console.ReadLine(), out amount))
                Console.WriteLine("Error the input is invalid, write the amount of " + BlockchainSetting.CoinMinName + " to send: ");

            amount *= BlockchainSetting.CoinDecimal;

            Console.Clear();

            Console.WriteLine("Write a transaction id has you want: ");
            long transactionId;
            while (!long.TryParse(Console.ReadLine(), out transactionId))
                Console.WriteLine("Error the input is invalid, write a transaction id has you want: ");

            Console.Clear();

            Console.WriteLine("Write a block height has you want: ");
            long blockHeight;
            while (!long.TryParse(Console.ReadLine(), out blockHeight))
                Console.WriteLine("Error the input is invalid, write a block height has you want: ");
            

            Console.WriteLine("Write a payment ID, leave empty for set default value:");

            if (!long.TryParse(Console.ReadLine(), out var paymentId))
                paymentId = 0;
            
            long blockHeighTarget = blockHeight + BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations;


            Console.Write("To valid your transaction amount sources, please write each tx hash and the amount to spend: ");

            Dictionary<string, ClassTransactionHashSourceObject> amountSourceList = new Dictionary<string, ClassTransactionHashSourceObject>();

            while (true)
            {
                Console.WriteLine("Write a transaction hash: ");
                string txHash = Console.ReadLine();
                Console.WriteLine("Write the amount to spend from this tx hash: ");
                decimal txAmount;

                while (!decimal.TryParse(Console.ReadLine(), out txAmount))
                    Console.WriteLine("Error the input is invalid, write the amount to spend propertly: ");

                txAmount *= BlockchainSetting.CoinDecimal;

                long blockHeightTx;

                Console.WriteLine("Input the block height of this transaction: ");

                while (!long.TryParse(Console.ReadLine(), out blockHeightTx))
                    Console.WriteLine("Error the input is invalid, write the block height of the transaction propertly: ");

                if (!amountSourceList.ContainsKey(txHash))
                {
                    amountSourceList.Add(txHash, new ClassTransactionHashSourceObject()
                    {
                        Amount = (BigInteger)txAmount,
                    });
                }

                Console.WriteLine("Do you want to insert more tx sources?[Y/N]");

                if ((Console.ReadLine() ?? string.Empty).ToLower() != "y")
                    break;
            }


            ClassTransactionObject transactionObject = ClassTransactionUtility.BuildTransaction(blockHeight,blockHeighTarget, walletAddressSender, walletPublicKeySender, walletPublicKeyReceiver, (BigInteger)amount, 0, walletAddressTarget, ClassUtility.GetCurrentTimestampInSecond(), ClassTransactionEnumType.TRANSFER_TRANSACTION, paymentId, string.Empty, string.Empty, walletPrivateKeySender, walletPrivateKeyReceiver, amountSourceList, 0, new CancellationTokenSource());

            if (transactionObject != null)
            {

                Console.WriteLine("Transaction generated and signed: ");

                Console.WriteLine(ClassUtility.SerializeData(transactionObject, Formatting.Indented));

                Console.WriteLine("Check transaction signature..");

                if (ClassWalletUtility.WalletCheckSignature(transactionObject.TransactionHash, transactionObject.TransactionSignatureSender, transactionObject.WalletPublicKeySender))
                {
                    Console.WriteLine("The transaction signature of the sender has been checked and is valid.");

                    if (ClassWalletUtility.WalletCheckSignature(transactionObject.TransactionHash, transactionObject.TransactionSignatureReceiver, transactionObject.WalletPublicKeyReceiver))
                        Console.WriteLine("The transaction signature of the receiver has been checked and is valid.");
                    else
                        Console.WriteLine("The transaction signature of the receiver has been checked and is invalid.");
                }
                else
                    Console.WriteLine("The transaction signature of the sender has been checked and is invalid.");

#if DEBUG
                Stopwatch watchProcessTimespend = new Stopwatch();
                watchProcessTimespend.Start();
#endif
                ClassTransactionEnumStatus resultCheckTransaction = ClassBlockchainStats.CheckTransaction(transactionObject, null, false, null, null, false).Result;

#if DEBUG
                watchProcessTimespend.Stop();
                Debug.WriteLine("Time spend to check the transfer transaction by the normal function: " + watchProcessTimespend.ElapsedMilliseconds + " ms.");
#endif
                if (resultCheckTransaction == ClassTransactionEnumStatus.VALID_TRANSACTION)
                    Console.WriteLine("The whole transaction transfer object data has been checked and is valid.");
                else
                    Console.WriteLine("The whole transaction transfer object data is invalid, result: " + resultCheckTransaction);

            }

            Console.WriteLine("Press a key to return to the main menu.");
            Console.ReadLine();
        }

        #endregion

        #region Functions dedicated to tests peers servers like the API.

        /// <summary>
        /// This function test to do a request to a peer API Server.
        /// </summary>
        private static void TestPeerApiServer()
        {
            Console.WriteLine("Write the peer IP server: ");

            string peerIp = Console.ReadLine() ?? string.Empty;

            while (peerIp.IsNullOrEmpty(false, out _) || !IPAddress.TryParse(peerIp, out _))
            {
                Console.WriteLine("Invalid IP, please write a valid IP:");
                peerIp = Console.ReadLine() ?? string.Empty;
            }

            Console.WriteLine("Write the peer API Port of the server (By default the API port is " + BlockchainSetting.PeerDefaultApiPort + "):");

            string peerPort = Console.ReadLine() ?? string.Empty;

            while (peerPort.IsNullOrEmpty(false, out _) || !int.TryParse(peerPort, out _))
            {
                Console.WriteLine("Invalid port, please write a valid port:");
                peerPort = Console.ReadLine() ?? string.Empty;
            }


            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(("http://" + peerIp + ":" + peerPort + "/" + ClassPeerApiEnumGetRequest.GetBlockTemplate));
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.ServicePoint.Expect100Continue = false;
                request.KeepAlive = false;
                request.Timeout = BlockchainSetting.PeerApiMaxConnectionDelay * 1000;
                string result;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponseAsync().Result)
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                    result = reader.ReadToEndAsync().Result;

                if (!result.IsNullOrEmpty(false, out _))
                {
                    if (ClassUtility.TryDeserialize(result, out ClassApiPeerPacketObjetReceive apiPeerPacketObjetReceive))
                    {
                        if (apiPeerPacketObjetReceive != null)
                        {
                            if (ClassUtility.TryDeserialize(apiPeerPacketObjetReceive.PacketObjectSerialized, out ClassApiPeerPacketSendBlockTemplate apiPeerPacketSendNetworkStats))
                                Console.WriteLine(ClassUtility.SerializeData(apiPeerPacketSendNetworkStats));
                        }
                    }
                }
            }
            catch(Exception error)
            {
                Console.WriteLine("Exception on sending post request to the API Server. Details: " + error.Message);
            }
            Console.WriteLine("Press a key to return to the main menu.");
            Console.ReadLine();
        }

        #endregion

        #region Functions dedicated to tests fake block maker.

        /// <summary>
        /// Test to generate some fake blocks.
        /// </summary>
        private static void TestFakeBlockGenerator()
        {
            ClassLog.SimpleWriteLine("[Warning] Every blocks generated and tx's generated from this method are invalid, this method just permit to test the memory consumption.", ConsoleColor.Red);

            #region Input block height target.

            int blockCount = 0;

            Console.WriteLine("Write the amount of blocks to build: ");

            while (blockCount <= 0)
            {
                while (!(int.TryParse(Console.ReadLine() ?? string.Empty, out blockCount)))
                {
                    Console.WriteLine("Error the input is invalid. Write the amount of blocks to build propertly: ");

                }
                if (blockCount <= 0)
                {
                    Console.WriteLine("Error the amount of blocks cannot be lower than 1, Write the amount of blocks to build propertly:  ");
                }
            }

            #endregion

            #region Input the amount of threads who check and increment block transaction confirmation.

            int transactionCountPerBlock = 0;

            Console.WriteLine("Input the amount of transactions per block: ");

            while (transactionCountPerBlock <= 0)
            {
                while (!(int.TryParse(Console.ReadLine() ?? string.Empty, out transactionCountPerBlock)))
                    Console.WriteLine("Error the input is invalid. Write the amount of tx per block propertly: ");

                if (transactionCountPerBlock <= 0)
                    Console.WriteLine("Error the amount of transactions can't be set to 0, the benchmark would be unusefull.");
            }

            #endregion


            Dictionary<long, ClassBlockObject> dictionaryBlockObjects = new Dictionary<long, ClassBlockObject>();

            var ramAllocation = Process.GetCurrentProcess().WorkingSet64;

            long startMemory = ramAllocation / (1024 * 1024);

            string randomPrivateKey = ClassWalletUtility.GenerateWalletPrivateKey(null, true);
            string randomWalletAddressReceiver = ClassWalletUtility.GenerateWalletAddressFromPublicKey(ClassWalletUtility.GenerateWalletPublicKeyFromPrivateKey(ClassWalletUtility.GenerateWalletPrivateKey(null, true)));

            var blockTransaction = ClassTransactionUtility.BuildTransaction(0, BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations, BlockchainSetting.WalletAddressDev(0), BlockchainSetting.WalletAddressDevPublicKey(0), string.Empty, BlockchainSetting.GenesisBlockAmount, 0, randomWalletAddressReceiver, ClassUtility.GetCurrentTimestampInSecond(), ClassTransactionEnumType.NORMAL_TRANSACTION, 0, string.Empty, string.Empty, randomPrivateKey, string.Empty, null, 0, new CancellationTokenSource());


            for (int i = 0; i < blockCount; i++)
            {
                dictionaryBlockObjects.Add(i, new ClassBlockObject(i, 0, string.Empty, 0, 0, ClassBlockEnumStatus.LOCKED, false, false));

                for(int k = 0; k < transactionCountPerBlock; k++)
                {
                    dictionaryBlockObjects[i].BlockTransactions.Add(blockTransaction.TransactionHash, new ClassBlockTransaction(0, blockTransaction)
                    {
                        TransactionObject = blockTransaction,
                        TransactionBlockHeightInsert = i,
                        TransactionBlockHeightTarget = i + BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations,
                        TransactionStatus = true
                    });

                    blockTransaction.TransactionHash = ClassUtility.GenerateSha3512FromString(ClassUtility.GetRandomWord(32));
                }
            }

            ramAllocation = Process.GetCurrentProcess().WorkingSet64;

            long endMemory = ramAllocation / (1024 * 1024);

            Console.WriteLine("Memory usage: "+(endMemory - startMemory) + "MB(s) | Begin: "+startMemory+" MB(s) | End: "+endMemory+" MB(s)");

            Console.ReadLine();

            dictionaryBlockObjects.Clear(); 
        }

        #endregion

        #region Function dedicated to build a blockchain

        /// <summary>
        /// Test to generate the genesis block.
        /// </summary>
        private static void BuildBlockchain()
        {
            ClassLog.SimpleWriteLine("[Note] Be sure to have generate your dev wallet, and updating the BlockchainSetting.cs file before.", ConsoleColor.Red);
            ClassLog.SimpleWriteLine("[Note] You need at least 2 two public nodes to run your own decentralized network.", ConsoleColor.Red);
            ClassLog.SimpleWriteLine("[Warning] " + (BlockchainSetting.GenesisBlockAmount / BlockchainSetting.CoinDecimal) + " " + BlockchainSetting.CoinMinName + ", if you want to change the amount you need to edit the BlockchainSetting.cs file manually.", ConsoleColor.Red);
            Console.WriteLine("Write the wallet address of the developer: ");

            string walletAddress = Console.ReadLine();

            Console.WriteLine("Write the public key of the dev wallet address: ");

            string publicKey = Console.ReadLine();

            Console.WriteLine("Write the private key of the dev wallet address: ");

            string privateKey = Console.ReadLine();


            Console.WriteLine("Load or create blockchain database..");

            ClassBlockchainDatabaseSetting blockchainDatabaseSetting = new ClassBlockchainDatabaseSetting();

            if (Directory.Exists(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryPath))
                Directory.Delete(blockchainDatabaseSetting.BlockchainSetting.BlockchainDirectoryPath, true);

            // Load blockchain database + blockchain database cache.
            if (ClassBlockchainDatabase.LoadBlockchainDatabase(blockchainDatabaseSetting, string.Empty).Result)
            {
                if (ClassBlockchainStats.BlockCount == 0)
                {
                    long timestampCreate = ClassUtility.GetCurrentTimestampInSecond();
                    long blockHeight = BlockchainSetting.GenesisBlockHeight;
                    BigInteger blockDifficulty = BlockchainSetting.MiningMinDifficulty;

                    ClassTransactionObject blockTransaction = ClassTransactionUtility.BuildTransaction(blockHeight, blockHeight + BlockchainSetting.TransactionMandatoryMinBlockTransactionConfirmations, BlockchainSetting.BlockRewardName, BlockchainSetting.WalletAddressDevPublicKey(0), string.Empty, BlockchainSetting.GenesisBlockAmount, 0, walletAddress, timestampCreate, ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION, 0, string.Empty, string.Empty, privateKey, string.Empty, null, timestampCreate, new CancellationTokenSource());

                    Console.WriteLine("Genesis block transaction hash: "+blockTransaction.TransactionHash);

                    string finalTransactionHash = ClassBlockUtility.GetFinalTransactionHashList(new List<string>(){blockTransaction.TransactionHash}, string.Empty);

                    string blockHash = ClassBlockUtility.GenerateBlockHash(blockHeight, blockDifficulty, 1, finalTransactionHash, BlockchainSetting.WalletAddressDev(0));

                    if (ClassBlockchainDatabase.BlockchainMemoryManagement.Add(blockHeight, new ClassBlockObject(blockHeight, blockDifficulty, blockHash, timestampCreate, ClassUtility.GetCurrentTimestampInSecond(),  ClassBlockEnumStatus.UNLOCKED, false, false), CacheBlockMemoryInsertEnumType.INSERT_IN_ACTIVE_MEMORY_OBJECT, new CancellationTokenSource()).Result)
                    {
                        ClassBlockchainDatabase.BlockchainMemoryManagement[blockHeight, null].BlockTransactions.Add(blockTransaction.TransactionHash, new ClassBlockTransaction(0, blockTransaction)
                        {
                            IndexInsert = 0,
                            TransactionBlockHeightInsert = blockHeight,
                            TransactionBlockHeightTarget = blockTransaction.BlockHeightTransactionConfirmationTarget,
                            TransactionStatus = true,
                            TransactionObject = blockTransaction,
                        });

                        ClassBlockchainDatabase.BlockchainMemoryManagement[blockHeight, null].BlockUnlockValid = false;
                        ClassBlockchainDatabase.BlockchainMemoryManagement[blockHeight, null].BlockFinalHashTransaction = finalTransactionHash;

                        Console.WriteLine("Genesis block generated.");
                        Console.WriteLine("Sign default mining setting object..");

                        ClassMiningPoWaCSettingObject miningPoWaCSettingObject = BlockchainSetting.DefaultMiningPocSettingObject;

                        long miningSettingObjectTimestampSign = ClassUtility.GetCurrentTimestampInSecond();
                        miningPoWaCSettingObject.MiningSettingTimestamp = miningSettingObjectTimestampSign;
                        miningPoWaCSettingObject.MiningSettingContentHash = null;
                        miningPoWaCSettingObject.MiningSettingContentHashSignature = null;
                        miningPoWaCSettingObject.MiningSettingContentDevPublicKey = publicKey;

                        string miningPoWacSettingObjectContentHash = ClassUtility.GenerateSha3512FromString(ClassUtility.SerializeData(miningPoWaCSettingObject));
                        string miningPoWacSettingObjectContentSignature = ClassWalletUtility.WalletGenerateSignature(privateKey, miningPoWacSettingObjectContentHash);

                        Console.WriteLine("Please write, the blockchain setting file path: ");
                        string blockchainSettingFilePath = Console.ReadLine();

                        while (!File.Exists(blockchainSettingFilePath))
                        {
                            Console.WriteLine("The blockchain setting file path not exist, please try again: ");
                            blockchainSettingFilePath = Console.ReadLine();
                        }

                        List<string> blockchainData = new List<string>();

                        string line;
                        using(StreamReader reader = new StreamReader(blockchainSettingFilePath))
                        {
                            while((line = reader.ReadLine()) != null)
                                blockchainData.Add(line);
                        }

                        using (StreamWriter writer = new StreamWriter(blockchainSettingFilePath))
                        {
                            foreach (string blockchainLine in blockchainData)
                            {
                                if (blockchainLine.Contains("GenesisBlockFinalTransactionHash"))
                                    writer.WriteLine("\t\tpublic const string GenesisBlockFinalTransactionHash =\"" + finalTransactionHash + "\";");
                                else if (blockchainLine.Contains("DefaultWalletAddressDevPublicKey"))
                                    writer.WriteLine("\t\tpublic const string DefaultWalletAddressDevPublicKey =\"" + publicKey + "\";");
                                else if (blockchainLine.Contains("DefaultWalletAddressDev"))
                                    writer.WriteLine("\t\tpublic const string DefaultWalletAddressDev =\"" + walletAddress + "\";");
                                else if (blockchainLine.Contains(", new Dictionary<string, int>(){ { \""))
                                {
                                    Console.WriteLine("Please write a default node IP: ");
                                    ClassLog.SimpleWriteLine("[Note] - You can insert more default peers on the BlockchainSetting.cs", ConsoleColor.Red);

                                    string peerIp = Console.ReadLine();

                                    writer.WriteLine("\t\t\t{ \"" + peerIp + "\", new Dictionary<string, int>() { { \"D0BFF4A56F062828939E40E6DFD8A5EF58E28A10CB69E9E281C90802632D0345618CB5DA20736C2BAAC458A1EB5239F012621847B40F76C0CD10EA05CC4FD184\", PeerDefaultPort } }}");

                                }
                                else
                                    writer.WriteLine(blockchainLine);
                            }
                        }


                        Console.WriteLine("Please write the mining setting file path:");
                        string miningSettingFilePath = Console.ReadLine();

                        while(!File.Exists(miningSettingFilePath))
                        {
                            Console.WriteLine("The mining setting file not working, please try again:");
                            miningSettingFilePath = Console.ReadLine();
                        }

                        List<string> miningSettingContent = new List<string>();

                        line = string.Empty;
                        using (StreamReader reader = new StreamReader(miningSettingFilePath))
                        {
                            while ((line = reader.ReadLine()) != null)
                                miningSettingContent.Add(line);
                        }

                        using (StreamWriter writer = new StreamWriter(miningSettingFilePath))
                        {
                            foreach (string miningLine in miningSettingContent)
                            {
                                if (miningLine.Contains("MiningSettingTimestamp ="))
                                    writer.WriteLine("\t\t\tMiningSettingTimestamp = " + miningSettingObjectTimestampSign + ";");
                                else if (miningLine.Contains("MiningSettingContentHash ="))
                                    writer.WriteLine("\t\t\tMiningSettingContentHash = \"" + miningPoWacSettingObjectContentHash + "\";");
                                else if (miningLine.Contains("MiningSettingContentHashSignature ="))
                                    writer.WriteLine("\t\t\tMiningSettingContentHashSignature = \"" + miningPoWacSettingObjectContentSignature + "\";");
                                else if (miningLine.Contains("MiningSettingContentDevPublicKey ="))
                                    writer.WriteLine("\t\t\tMiningSettingContentDevPublicKey = \"" + publicKey + "\";");
                                else
                                    writer.WriteLine(miningLine);
                            }
                        }

                        Console.WriteLine("Genesis block generated, blockchain setting and mining setting are updated.");
                        ClassLog.SimpleWriteLine("[Note] You need to rebuild the source code and copy the Blockchain folder to the node build folder.", ConsoleColor.Red);

                    }
                }
                else
                    Console.WriteLine("Their is block(s) inside the database, task cancelled.");
                
                ClassBlockchainDatabase.SaveBlockchainDatabase(blockchainDatabaseSetting).Wait();
                ClassBlockchainDatabase.CloseBlockchainDatabase(blockchainDatabaseSetting).Wait();
            }

            Console.WriteLine("Press a key to return to the main menu.");
            Console.ReadLine();
        }

        #endregion

    }
}
