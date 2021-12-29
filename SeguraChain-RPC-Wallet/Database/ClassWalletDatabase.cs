using LZ4;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;
using SeguraChain_RPC_Wallet.Database.Wallet;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace SeguraChain_RPC_Wallet.Database
{
    public class ClassWalletDatabase
    {
        private ConcurrentDictionary<string, ClassWalletData> _dictionaryWallet;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassWalletDatabase()
        {
            _dictionaryWallet = new ConcurrentDictionary<string, ClassWalletData>();
        }

        /// <summary>
        /// Initialization of the wallet database.
        /// </summary>
        /// <param name="walletDatabasePath"></param>
        /// <param name="walletFilename"></param>
        /// <returns></returns>
        private bool InitWalletDatabase(string walletDatabasePath, string walletFilename)
        {
            if (!Directory.Exists(walletDatabasePath))
            {
                try
                {
                    Directory.CreateDirectory(walletDatabasePath);
                    Console.WriteLine("The wallet database directory has been created successfully.");
                }
                catch (Exception error)
                {
                    Console.WriteLine("Can't create the wallet database path. Exception: " + error.Message);
                    return false;
                }
            }

            if (!File.Exists(walletFilename))
            {
                try
                {
                    File.Create(walletDatabasePath + walletFilename);

                    Console.WriteLine("The wallet database file has been created successfully.");
                }
                catch(Exception error)
                {
                    Console.WriteLine("Can't create the wallet database file. Exception: " + error.Message);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Load wallet database.
        /// </summary>
        /// <param name="walletDatabasePath"></param>
        /// <param name="walletFilePath"></param>
        /// <param name="walletDatabasePassword"></param>
        /// <returns></returns>
        public bool LoadWalletDatabase(string walletDatabasePath, string walletFilePath, string walletDatabasePassword)
        {
            if (!InitWalletDatabase(walletDatabasePath, walletFilePath))
            {
                Console.WriteLine("The initialization of the wallet database failed.");
                return false;
            }

            if (!ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringUtf8(walletDatabasePassword), true, out byte[] walletDatabaseEncryptionKey))
                return false;

            byte[] walletDatabaseEncryptionIv = ClassAes.GenerateIv(walletDatabaseEncryptionKey);

            using (FileStream fileStream = new FileStream(walletDatabasePath + walletFilePath, FileMode.OpenOrCreate))
            {
                using (StreamReader reader = new StreamReader(new LZ4Stream(fileStream, LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression)))
                {
                    string line;
                    int lineIndex = 0;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!ClassAes.DecryptionProcess(Convert.FromBase64String(line), walletDatabaseEncryptionKey, walletDatabaseEncryptionIv, out byte[] walletDataBytes))
                            continue;

                        if (!ClassUtility.TryDeserialize(walletDataBytes.GetStringFromByteArrayUtf8(), out ClassWalletData walletData))
                            continue;

                        if (_dictionaryWallet.ContainsKey(walletData.WalletAddress))
                            continue;

                        if (!_dictionaryWallet.TryAdd(walletData.WalletAddress, walletData))
                            continue;

                        lineIndex++;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Save wallet database.
        /// </summary>
        /// <param name="walletDatabasePath"></param>
        /// <param name="walletFilePath"></param>
        /// <param name="walletDatabasePassword"></param>
        /// <returns></returns>
        public bool SaveWalletDatabase(string walletDatabasePath, string walletFilePath, string walletDatabasePassword)
        {
            if (!ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringUtf8(walletDatabasePassword), true, out byte[] walletDatabaseEncryptionKey))
                return false;

            byte[] walletDatabaseEncryptionIv = ClassAes.GenerateIv(walletDatabaseEncryptionKey);

            using (FileStream fileStream = new FileStream(walletDatabasePath + walletFilePath, FileMode.OpenOrCreate))
            {
                using (StreamWriter writer = new StreamWriter(new LZ4Stream(fileStream, LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression)))
                {
                    foreach (ClassWalletData walletData in _dictionaryWallet.Values)
                    {
                        if (!ClassAes.EncryptionProcess(ClassUtility.GetByteArrayFromStringUtf8(ClassUtility.SerializeData(walletData)), walletDatabaseEncryptionKey, walletDatabaseEncryptionIv, out byte[] walletDataEncrypted))
                            continue;

                        writer.WriteLine(Convert.ToBase64String(walletDataEncrypted));
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Get wallet data from wallet address.
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <returns></returns>
        public ClassWalletData GetWalletDataFromWalletAddress(string walletAddress) => _dictionaryWallet.ContainsKey(walletAddress) ? _dictionaryWallet[walletAddress] : null;
        
        /// <summary>
        /// Return the amount of wallet inside of the wallet database.
        /// </summary>
        public int GetWalletCount => _dictionaryWallet.Count;

        /// <summary>
        /// Return every wallet address as a disposable list from the wallet database.
        /// </summary>
        public DisposableList<string> GetListWalletAddress => new DisposableList<string>(false, 0, _dictionaryWallet.Keys.ToList());

    }
}
