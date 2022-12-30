using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LZ4;
using Newtonsoft.Json;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Database.DatabaseSetting;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Sovereign.Enum;
using SeguraChain_Lib.Blockchain.Sovereign.Object;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Sovereign.Database
{
    public class ClassSovereignUpdateDataSetting
    {
        public const string SovereignUpdateDirectoryName = "\\SovereignUpdate\\";
        public const string SovereignUpdateDatabaseFilename = "sovereign-update.dat";
    }

    public class ClassSovereignUpdateDatabase
    {
        public static Dictionary<string, ClassSovereignUpdateObject> DictionarySovereignUpdateObject;
        public static Dictionary<ClassSovereignEnumUpdateType, SortedList<long, string>> DictionarySortedSovereignUpdateList;

        /// <summary>
        /// Database file properties.
        /// </summary>
        private static byte[] _sovereignDataStandardEncryptionKey;
        private static byte[] _sovereignDataStandardEncryptionKeyIv;
        private static string _sovereignDatabaseDirectoryPath;
        private static string _sovereignDatabaseFilePath;
        private static bool _useCompress;
        private static bool _useEncryption;

        #region Sovereign Update files managements functions.

        /// <summary>
        /// Load sovereign update data files.
        /// </summary>
        /// <returns></returns>
        public static bool LoadSovereignUpdateData(string encryptionKey, bool compress, bool encrypted)
        {
            _useEncryption = encrypted;
            _useCompress = compress;

            DictionarySovereignUpdateObject = new Dictionary<string, ClassSovereignUpdateObject>();
            DictionarySortedSovereignUpdateList = new Dictionary<ClassSovereignEnumUpdateType, SortedList<long, string>>();

            if (_sovereignDatabaseDirectoryPath.IsNullOrEmpty(false, out _))
            {
                _sovereignDatabaseDirectoryPath = ClassUtility.ConvertPath(AppContext.BaseDirectory + ClassSovereignUpdateDataSetting.SovereignUpdateDirectoryName);
                _sovereignDatabaseFilePath = ClassUtility.ConvertPath(_sovereignDatabaseDirectoryPath + ClassSovereignUpdateDataSetting.SovereignUpdateDatabaseFilename);
            }

            if (_useEncryption)
            {
                if (encryptionKey.IsNullOrEmpty(false, out _))
                {
                    if (!ClassAes.GenerateKey(BlockchainSetting.BlockchainMarkKey, true, out _sovereignDataStandardEncryptionKey))
                    {
                        ClassLog.WriteLine("Can't generate standard encryption key for decrypt peer list.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        return false;
                    }
                }
                else
                {
                    if (!ClassAes.GenerateKey(encryptionKey.GetByteArray(true), true, out _sovereignDataStandardEncryptionKey))
                    {
                        ClassLog.WriteLine("Can't generate standard encryption key for decrypt peer list.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                        return false;
                    }
                }
                _sovereignDataStandardEncryptionKeyIv = ClassAes.GenerateIv(_sovereignDataStandardEncryptionKey);
            }

            if (Directory.Exists(_sovereignDatabaseDirectoryPath))
            {
                if (File.Exists(_sovereignDatabaseFilePath))
                {
                    long totalSovereignUpdateFileLoaded = 0;
                    long totalSovereignUpdateApplied = 0;
                    long totalSovereignUpdateFailed = 0;

                    using (FileStream fileStream = new FileStream(_sovereignDatabaseFilePath, FileMode.Open))
                    {
                        StreamReader reader = _useCompress ? new StreamReader(new LZ4Stream(fileStream, LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression)) : new StreamReader(fileStream);


                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (_useEncryption)
                            {
                                if (ClassAes.DecryptionProcess(Convert.FromBase64String(line), _sovereignDataStandardEncryptionKey, _sovereignDataStandardEncryptionKeyIv, out byte[] decryptedResult))
                                    line = decryptedResult.GetStringFromByteArrayAscii();
                                else
                                {
                                    ClassLog.WriteLine("[ERROR] Can't decrypt Sovereign Update line contained into the database file with the encryption key selected.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY);
                                    return false;
                                }
                            }

                            if (!line.IsNullOrEmpty(false, out _))
                            {
                                ClassSovereignUpdateObject sovereignUpdateObject = JsonConvert.DeserializeObject<ClassSovereignUpdateObject>(line);
                                totalSovereignUpdateFileLoaded++;

                                ClassSovereignEnumUpdateCheckStatus sovereignUpdateCheckStatusResult = CheckSovereignUpdateObject(sovereignUpdateObject, out _);

                                switch (sovereignUpdateCheckStatusResult)
                                {
                                    case ClassSovereignEnumUpdateCheckStatus.VALID_SOVEREIGN_UPDATE:
                                        if (!DictionarySovereignUpdateObject.ContainsKey(sovereignUpdateObject.SovereignUpdateHash))
                                        {
                                            DictionarySovereignUpdateObject.Add(sovereignUpdateObject.SovereignUpdateHash, sovereignUpdateObject);
                                            if (!DictionarySortedSovereignUpdateList.ContainsKey(DictionarySovereignUpdateObject[sovereignUpdateObject.SovereignUpdateHash].SovereignUpdateType))
                                                DictionarySortedSovereignUpdateList.Add(DictionarySovereignUpdateObject[sovereignUpdateObject.SovereignUpdateHash].SovereignUpdateType, new SortedList<long, string>());

                                            if (!DictionarySortedSovereignUpdateList[DictionarySovereignUpdateObject[sovereignUpdateObject.SovereignUpdateHash].SovereignUpdateType].ContainsValue(sovereignUpdateObject.SovereignUpdateHash))
                                                DictionarySortedSovereignUpdateList[DictionarySovereignUpdateObject[sovereignUpdateObject.SovereignUpdateHash].SovereignUpdateType].Add(DictionarySovereignUpdateObject[sovereignUpdateObject.SovereignUpdateHash].SovereignUpdateTimestamp, sovereignUpdateObject.SovereignUpdateHash);
                                            
                                            totalSovereignUpdateApplied++;
                                        }
                                        break;
                                    default:
                                        totalSovereignUpdateFailed++;
                                        ClassLog.WriteLine("The sovereign update file: " + sovereignUpdateObject.SovereignUpdateHash + " object is invalid. Check result: " + sovereignUpdateCheckStatusResult, ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Red);
                                        break;

                                }
                            }
                        }
                    }

                    ClassLog.WriteLine(totalSovereignUpdateFileLoaded + " Sovereign update(s) file(s) loaded successfully.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);
                    ClassLog.WriteLine(totalSovereignUpdateApplied + " Sovereign update(s) applied successfully.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Magenta);
                    
                    if (totalSovereignUpdateFailed > 0)
                        ClassLog.WriteLine(totalSovereignUpdateFailed + " Sovereign update(s) failed.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                    else
                        ClassLog.WriteLine("No error on applying sovereign update(s).", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.Green);
                }
            }
            else
                Directory.CreateDirectory(_sovereignDatabaseDirectoryPath);

            return true;
        }

        /// <summary>
        /// Save sovereign update object data.
        /// </summary>
        /// <returns></returns>
        public static bool SaveSovereignUpdateObjectData(out int totalSaved)
        {
            totalSaved = 0;
            try
            {
                if (!Directory.Exists(_sovereignDatabaseDirectoryPath))
                    Directory.CreateDirectory(_sovereignDatabaseDirectoryPath);

                File.Create(_sovereignDatabaseFilePath).Close();

                using (StreamWriter writerSovUpdate = _useCompress ?
                    new StreamWriter(new LZ4Stream(new FileStream(_sovereignDatabaseFilePath, FileMode.Truncate), LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression, ClassBlockchainDatabaseDefaultSetting.Lz4CompressionBlockSize)) :
                    new StreamWriter(new FileStream(_sovereignDatabaseFilePath, FileMode.Truncate)))
                {

                    foreach (var sovereignUpdateObject in DictionarySovereignUpdateObject.ToArray())
                    {

                        if (_useEncryption)
                        {
                            if (ClassAes.EncryptionProcess(ClassUtility.SerializeData(sovereignUpdateObject.Value, Formatting.None).GetByteArray(true), _sovereignDataStandardEncryptionKey, _sovereignDataStandardEncryptionKeyIv, out byte[] encryptedResult))
                            {
                                writerSovUpdate.WriteLine(Convert.ToBase64String(encryptedResult));
                                totalSaved++;

                            }
                            else
                                ClassLog.WriteLine("Failed to save encrypted sovereign update data into database.", ClassEnumLogLevelType.LOG_LEVEL_GENERAL, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_MANDATORY_PRIORITY, false, ConsoleColor.DarkRed);
                        }
                        else
                        {
                            writerSovUpdate.WriteLine(ClassUtility.SerializeData(sovereignUpdateObject.Value, Formatting.None));
                            totalSaved++;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Register sovereign update object into the database.
        /// </summary>
        /// <param name="sovereignUpdateObject"></param>
        /// <returns></returns>
        public static bool RegisterSovereignUpdateObject(ClassSovereignUpdateObject sovereignUpdateObject)
        {
            if (!DictionarySovereignUpdateObject.ContainsKey(sovereignUpdateObject.SovereignUpdateHash))
            {
                DictionarySovereignUpdateObject.Add(sovereignUpdateObject.SovereignUpdateHash, sovereignUpdateObject);

                if (!DictionarySortedSovereignUpdateList.ContainsKey(sovereignUpdateObject.SovereignUpdateType))
                    DictionarySortedSovereignUpdateList.Add(sovereignUpdateObject.SovereignUpdateType, new SortedList<long, string>());
                

                DictionarySortedSovereignUpdateList[sovereignUpdateObject.SovereignUpdateType].Add(sovereignUpdateObject.SovereignUpdateTimestamp, sovereignUpdateObject.SovereignUpdateHash);

                return true;
            }

            return false;
        }

        #endregion

        #region Check Sovereign Update functions.

        /// <summary>
        /// Check the sovereign update object data.
        /// </summary>
        /// <param name="sovereignUpdateObject"></param>
        /// <param name="sovereignUpdateType"></param>
        /// <returns></returns>
        public static ClassSovereignEnumUpdateCheckStatus CheckSovereignUpdateObject(ClassSovereignUpdateObject sovereignUpdateObject, out ClassSovereignEnumUpdateType sovereignUpdateType)
        {
            // Default type of update who will provide no event of update if the checker provide an error.
            sovereignUpdateType = ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_NONE;

            #region Basic check sovereign content.

            if (sovereignUpdateObject == null)
                return ClassSovereignEnumUpdateCheckStatus.EMPTY_SOVEREIGN_UPDATE_OBJECT;

            if (sovereignUpdateObject.SovereignUpdateContent == null)
                return ClassSovereignEnumUpdateCheckStatus.EMPTY_SOVEREIGN_UPDATE_CONTENT;

            if (sovereignUpdateObject.SovereignUpdateHash.IsNullOrEmpty(false, out _))
                return ClassSovereignEnumUpdateCheckStatus.EMPTY_SOVEREIGN_UPDATE_HASH;

            if (sovereignUpdateObject.SovereignUpdateSignature.IsNullOrEmpty(false, out _))
                return ClassSovereignEnumUpdateCheckStatus.EMPTY_SOVEREIGN_UPDATE_SIGNATURE;

            if (sovereignUpdateObject.SovereignUpdateDevWalletAddress.IsNullOrEmpty(false, out _))
                return ClassSovereignEnumUpdateCheckStatus.EMPTY_SOVEREIGN_UPDATE_DEV_WALLET_ADDRESS;

            if (sovereignUpdateObject.SovereignUpdateTimestamp <= 0)
                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_TIMESTAMP;

            #endregion

            #region Check sovereign update type.

            switch (sovereignUpdateObject.SovereignUpdateType)
            {
                case ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_GRANT_RANK_UPDATE:
                case ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_REVOKE_RANK_UPDATE:
                case ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE:
                case ClassSovereignEnumUpdateType.SOVEREIGN_MINING_POWAC_SETTING_UPDATE:
                    sovereignUpdateType = sovereignUpdateObject.SovereignUpdateType;
                    break;
                default:
                    return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREING_UPDATE_TYPE;
            }

            #endregion

            #region Check sovereign content format.

            if (!ClassUtility.CheckHexStringFormat(sovereignUpdateObject.SovereignUpdateHash))
                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_HASH_FORMAT;

            if (sovereignUpdateObject.SovereignUpdateDevWalletAddress.Length < BlockchainSetting.WalletAddressWifLengthMin || sovereignUpdateObject.SovereignUpdateDevWalletAddress.Length > BlockchainSetting.WalletAddressWifLengthMax)
                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_DEV_WALLET_ADDRESS_LENGTH;

            if (ClassBase58.DecodeWithCheckSum(sovereignUpdateObject.SovereignUpdateDevWalletAddress, true) == null)
                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_DEV_WALLET_ADDRESS_BASE58_FORMAT;

            #endregion

            #region Check Dev Wallet Address with the blockchain setting.

            if (BlockchainSetting.WalletAddressDev(sovereignUpdateObject.SovereignUpdateTimestamp) != sovereignUpdateObject.SovereignUpdateDevWalletAddress)
                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_DEV_WALLET_ADDRESS;

            #endregion

            #region Check sovereign update content depending of the type.

            switch (sovereignUpdateObject.SovereignUpdateType)
            {
                case ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_GRANT_RANK_UPDATE:
                case ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_REVOKE_RANK_UPDATE:
                    {
                        if (sovereignUpdateObject.SovereignUpdateContent.PossibleContent1.IsNullOrEmpty(false, out _))
                            return ClassSovereignEnumUpdateCheckStatus.EMPTY_SOVEREIGN_UPDATE_SEED_NODE_NUMERIC_PUBLIC_KEY_CONTENT;

                        if (sovereignUpdateObject.SovereignUpdateContent.PossibleContent2.IsNullOrEmpty(false, out _))
                            return ClassSovereignEnumUpdateCheckStatus.EMPTY_SOVEREIGN_UPDATE_SEED_NODE_MAX_RANK_DELAY_CONTENT;

                        if (ClassBase58.DecodeWithCheckSum(sovereignUpdateObject.SovereignUpdateContent.PossibleContent1, false) == null)
                            return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_SEED_NODE_NUMERIC_PUBLIC_KEY;

                        if (long.TryParse(sovereignUpdateObject.SovereignUpdateContent.PossibleContent2, out var maxDelay))
                        {
                            if (maxDelay <= 0)
                                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_SEED_NODE_MAX_RANK_DELAY;
                        }
                        else
                            return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_SEED_NODE_MAX_RANK_DELAY;
                    }
                    break;
                case ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE:
                    {
                        if (sovereignUpdateObject.SovereignUpdateContent.PossibleContent1.IsNullOrEmpty(false, out _))
                            return ClassSovereignEnumUpdateCheckStatus.EMPTY_SOVEREIGN_UPDATE_DEV_WALLET_ADDRESS_CONTENT;

                        if (sovereignUpdateObject.SovereignUpdateContent.PossibleContent2.IsNullOrEmpty(false, out _))
                            return ClassSovereignEnumUpdateCheckStatus.EMPTY_SOVEREIGN_UPDATE_DEV_PUBLIC_KEY_CONTENT;

                        if (sovereignUpdateObject.SovereignUpdateContent.PossibleContent1.Length < BlockchainSetting.WalletAddressWifLengthMin || sovereignUpdateObject.SovereignUpdateContent.PossibleContent1.Length > BlockchainSetting.WalletAddressWifLengthMax)
                            return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_DEV_WALLET_ADDRESS_CONTENT;

                        if (sovereignUpdateObject.SovereignUpdateContent.PossibleContent2.Length != BlockchainSetting.WalletPublicKeyWifLength)
                            return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_DEV_PUBLIC_KEY_CONTENT;

                        if (ClassBase58.DecodeWithCheckSum(sovereignUpdateObject.SovereignUpdateContent.PossibleContent1, true) == null)
                            return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_DEV_WALLET_ADDRESS_CONTENT;

                        if (ClassBase58.DecodeWithCheckSum(sovereignUpdateObject.SovereignUpdateContent.PossibleContent2, false) == null)
                            return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_DEV_PUBLIC_KEY_CONTENT;

                        if (ClassWalletUtility.GenerateWalletAddressFromPublicKey(sovereignUpdateObject.SovereignUpdateContent.PossibleContent2) != sovereignUpdateObject.SovereignUpdateContent.PossibleContent1)
                            return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_DEV_PUBLIC_KEY_CONTENT;
                    }
                    break;
                case ClassSovereignEnumUpdateType.SOVEREIGN_MINING_POWAC_SETTING_UPDATE:
                    {
                        if (sovereignUpdateObject.SovereignUpdateContent.PossibleContent1.IsNullOrEmpty(false, out _))
                            return ClassSovereignEnumUpdateCheckStatus.EMPTY_SOVEREIGN_UPDATE_MINING_POW_SETTING;

                        try
                        {
                            if (long.TryParse(sovereignUpdateObject.SovereignUpdateContent.PossibleContent2, out long blockHeight))
                            {
                                if (blockHeight < BlockchainSetting.GenesisBlockHeight)
                                    return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_MINING_POWAC_SETTING_CONTENT;
                            }
                            else
                                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_MINING_POWAC_SETTING_CONTENT;

                            if (ClassUtility.TryDeserialize(sovereignUpdateObject.SovereignUpdateContent.PossibleContent1, out ClassMiningPoWaCSettingObject miningPoWaCSettingObject, ObjectCreationHandling.Replace))
                            {
                                if (!ClassMiningPoWaCUtility.CheckMiningPoWaCSetting(miningPoWaCSettingObject))
                                    return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_MINING_POWAC_SETTING_CONTENT;

                                if (miningPoWaCSettingObject.BlockHeightStart != blockHeight)
                                    return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_MINING_POWAC_SETTING_CONTENT;

                                if (GetLastDevWalletPublicKey(miningPoWaCSettingObject.MiningSettingTimestamp) != miningPoWaCSettingObject.MiningSettingContentDevPublicKey)
                                    return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_MINING_POWAC_SETTING_CONTENT;
                            }
                            else
                                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_MINING_POWAC_SETTING_CONTENT;
                        }
                        catch
                        {
                            return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_MINING_POWAC_SETTING_CONTENT;
                        }

                    }
                    break;
                default:
                    return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREING_UPDATE_TYPE;
            }

            #endregion

            #region Check sovereign content hash.

            if (DictionarySovereignUpdateObject.ContainsKey(sovereignUpdateObject.SovereignUpdateHash))
                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_HASH_ALREADY_LISTED;

            GenerateSovereignHashUpdate(sovereignUpdateObject, out var testSovereignUpdateHash);

            if (!testSovereignUpdateHash.Equals(sovereignUpdateObject.SovereignUpdateHash))
                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_HASH;

            #endregion

            #region Check sovereign update signature.

            if (!ClassWalletUtility.WalletCheckSignature(sovereignUpdateObject.SovereignUpdateHash, sovereignUpdateObject.SovereignUpdateSignature, BlockchainSetting.WalletAddressDevPublicKey(sovereignUpdateObject.SovereignUpdateTimestamp)))
                return ClassSovereignEnumUpdateCheckStatus.INVALID_SOVEREIGN_UPDATE_SIGNATURE;

            #endregion

            sovereignUpdateType = sovereignUpdateObject.SovereignUpdateType;

            return ClassSovereignEnumUpdateCheckStatus.VALID_SOVEREIGN_UPDATE;
        }

        #endregion

        #region Create Sovereign Update functions.

        /// <summary>
        /// Function for generate a sovereign update.
        /// </summary>
        /// <param name="statutBuild"></param>
        /// <returns></returns>
        public static ClassSovereignUpdateObject GenerateSovereignUpdate(out bool statutBuild)
        {
            Console.Clear();

            statutBuild = false;
            Console.WriteLine("Select the type of Sovereign Update: ");
            Console.WriteLine((int)ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_GRANT_RANK_UPDATE + " - Build grant rank Seed Node Update.");
            Console.WriteLine((int)ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_REVOKE_RANK_UPDATE + " - Build revoke rank Seed Node Update.");
            Console.WriteLine((int)ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE + " - Build change Dev Signature Update.");
            Console.WriteLine((int)ClassSovereignEnumUpdateType.SOVEREIGN_MINING_POWAC_SETTING_UPDATE + " - Build Mining Anti Bad AI Work Update.");

            if (int.TryParse(Console.ReadLine(), out var updateTypeInput))
            {
                switch (updateTypeInput)
                {
                    case (int)ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_GRANT_RANK_UPDATE:
                        {
                            string devWalletAddress = GetLastDevWalletAddress(TaskManager.TaskManager.CurrentTimestampSecond);

                            if (ClassBase58.DecodeWithCheckSum(devWalletAddress, true) != null)
                            {

                                Console.WriteLine("Input the Peer Numeric Public to grant it has Seed Node rank: ");
                                string peerNumericPublicKey = Console.ReadLine() ?? string.Empty;

                                if (ClassBase58.DecodeWithCheckSum(peerNumericPublicKey, false) != null)
                                {
                                    Console.WriteLine("Input the date in second of max delay of rank (Current date in second: " + TaskManager.TaskManager.CurrentTimestampSecond + "): ");
                                    if (long.TryParse(Console.ReadLine(), out var timestamp))
                                    {
                                        if (timestamp > TaskManager.TaskManager.CurrentTimestampSecond)
                                        {
                                            Console.WriteLine("Write a description about the sovereign update: ");

                                            string description = Console.ReadLine();
                                            var sovereignUpdateObject = new ClassSovereignUpdateObject()
                                            {
                                                SovereignUpdateContent = new ClassSovereignUpdateContentObject()
                                                {
                                                    PossibleContent1 = peerNumericPublicKey,
                                                    PossibleContent2 = timestamp.ToString(),
                                                    Description = description
                                                },
                                                SovereignUpdateTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                                                SovereignUpdateDevWalletAddress = devWalletAddress,
                                                SovereignUpdateType = ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_GRANT_RANK_UPDATE
                                            };

                                            Console.WriteLine("Generate sovereign update hash to sign..");
                                            GenerateSovereignHashUpdate(sovereignUpdateObject, out sovereignUpdateObject.SovereignUpdateHash);
                                            sovereignUpdateObject.SovereignUpdateSignature = GenerateSovereignSignatureUpdate(sovereignUpdateObject);

                                            Console.WriteLine("Sovereign update build and signed.");
                                            statutBuild = true;
                                            return sovereignUpdateObject;
                                        }
                                    }

                                    Console.WriteLine("Input timestamp invalid.");
                                }
                                else
                                    Console.WriteLine("Invalid public key address format.");
                            }
                            else
                                Console.WriteLine("Invalid dev wallet address format.");
                        }
                        break;
                    case (int)ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_REVOKE_RANK_UPDATE:
                        {
                            string devWalletAddress = GetLastDevWalletAddress(TaskManager.TaskManager.CurrentTimestampSecond);

                            if (ClassBase58.DecodeWithCheckSum(devWalletAddress, true) != null)
                            {
                                Console.WriteLine("Input the Peer Numeric Public of the peer who is ranked and who will be revoked: ");
                                string peerNumericPublicKey = Console.ReadLine() ?? string.Empty;

                                if (ClassBase58.DecodeWithCheckSum(peerNumericPublicKey, false) != null)
                                {
                                    Console.WriteLine("Write a description about the sovereign update: ");

                                    string description = Console.ReadLine();
                                    var sovereignUpdateObject = new ClassSovereignUpdateObject()
                                    {
                                        SovereignUpdateContent = new ClassSovereignUpdateContentObject()
                                        {
                                            PossibleContent1 = peerNumericPublicKey,
                                            PossibleContent2 = TaskManager.TaskManager.CurrentTimestampSecond.ToString(),
                                            Description = description
                                        },
                                        SovereignUpdateTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                                        SovereignUpdateDevWalletAddress = devWalletAddress,
                                        SovereignUpdateType = ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_REVOKE_RANK_UPDATE
                                    };

                                    Console.WriteLine("Generate sovereign update hash to sign..");
                                    GenerateSovereignHashUpdate(sovereignUpdateObject, out sovereignUpdateObject.SovereignUpdateHash);
                                    sovereignUpdateObject.SovereignUpdateSignature = GenerateSovereignSignatureUpdate(sovereignUpdateObject);
                                    Console.WriteLine("Sovereign update build and signed.");
                                    statutBuild = true;
                                    return sovereignUpdateObject;

                                }
                                Console.WriteLine("Invalid public key address format.");
                            }
                            else
                                Console.WriteLine("Invalid dev wallet address format.");
                        }
                        break;
                    case (int)ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE:
                        {
                            string lastDevWalletAddress = GetLastDevWalletAddress(TaskManager.TaskManager.CurrentTimestampSecond);

                            Console.WriteLine("Write the new dev wallet address: ");
                            string newDevWalletAddress = Console.ReadLine();

                            if (ClassBase58.DecodeWithCheckSum(newDevWalletAddress, true) != null)
                            {
                                Console.WriteLine("Write the new dev public key: ");
                                string newDevPublicKey = Console.ReadLine();

                                if (ClassBase58.DecodeWithCheckSum(newDevPublicKey, false) != null)
                                {
                                    Console.WriteLine("Write a description about the sovereign update: ");

                                    string description = Console.ReadLine();

                                    var sovereignUpdateObject = new ClassSovereignUpdateObject()
                                    {
                                        SovereignUpdateContent = new ClassSovereignUpdateContentObject()
                                        {
                                            PossibleContent1 = newDevWalletAddress,
                                            PossibleContent2 = newDevPublicKey,
                                            Description = description
                                        },
                                        SovereignUpdateTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                                        SovereignUpdateDevWalletAddress = lastDevWalletAddress,
                                        SovereignUpdateType = ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE
                                    };

                                    Console.WriteLine("Generate sovereign update hash to sign..");
                                    GenerateSovereignHashUpdate(sovereignUpdateObject, out sovereignUpdateObject.SovereignUpdateHash);
                                    sovereignUpdateObject.SovereignUpdateSignature = GenerateSovereignSignatureUpdate(sovereignUpdateObject);
                                    Console.WriteLine("Sovereign update build and signed.");
                                    statutBuild = true;
                                    return sovereignUpdateObject;
                                }

                                Console.WriteLine("Invalid public key address format.");
                            }
                            else
                                Console.WriteLine("Invalid dev wallet address format.");
                        }
                        break;
                    case (int)ClassSovereignEnumUpdateType.SOVEREIGN_MINING_POWAC_SETTING_UPDATE:
                        {
                            Console.WriteLine("Input Dev Wallet Address: ");
                            string devWalletAddress = Console.ReadLine() ?? string.Empty;

                            Console.WriteLine("Input the amount of AES Round(s) encryption share to do on mining: ");
                            if (int.TryParse(Console.ReadLine() ?? string.Empty, out int roundAesParsed))
                            {

                                Console.WriteLine("Input the amount of SHA3-512 Round(s) Nonce to do on mining:");
                                if (int.TryParse(Console.ReadLine() ?? string.Empty, out var roundShaParsed))
                                {

                                    Console.WriteLine("Input the min nonce range to use on mining:");
                                    if (long.TryParse(Console.ReadLine() ?? string.Empty, out var nonceMinParse))
                                    {

                                        Console.WriteLine("Input the max nonce range to use on mining:");
                                        if (long.TryParse(Console.ReadLine() ?? string.Empty, out var nonceMaxParse))
                                        {

                                            Console.WriteLine("Input the size of bytes for numbers compatibility part:");
                                            if (int.TryParse(Console.ReadLine() ?? string.Empty, out var compatibilityPartParsed))
                                            {

                                                Console.WriteLine("Write the size of bytes for timestamp share: ");
                                                if (int.TryParse(Console.ReadLine() ?? string.Empty, out int timestampPartParsed))
                                                {
                                                    Console.WriteLine("Write the of bytes for block height: ");

                                                    if (int.TryParse(Console.ReadLine() ?? string.Empty, out int blockHeightPartParsed))
                                                    {
                                                        Console.WriteLine("Write the size of bytes for checksum share: ");
                                                        if (int.TryParse(Console.ReadLine() ?? string.Empty, out int checksumPartParsed))
                                                        {

                                                            Console.WriteLine("Write the size of bytes for wallet address decoded: ");
                                                            if (int.TryParse(Console.ReadLine() ?? string.Empty, out int walletDecodedPartParsed))
                                                            {
                                                                Console.WriteLine("Write the block height start of effect: ");
                                                                if (long.TryParse(Console.ReadLine() ?? string.Empty, out var blockHeight))
                                                                {

                                                                    Console.WriteLine("Write a description about the sovereign update: ");
                                                                    string description = Console.ReadLine();

                                                                    int randomDataShareSize = compatibilityPartParsed + timestampPartParsed + blockHeightPartParsed + checksumPartParsed + walletDecodedPartParsed;
                                                                    int shareHexStringSize = ClassAes.EncryptionKeySize + (32 * (roundShaParsed - 1));

                                                                    var sovereignUpdateObject = new ClassSovereignUpdateObject()
                                                                    {
                                                                        SovereignUpdateContent = new ClassSovereignUpdateContentObject()
                                                                        {
                                                                            PossibleContent1 = ClassUtility.SerializeData(new ClassMiningPoWaCSettingObject(false)
                                                                            {
                                                                                PowRoundAesShare = roundAesParsed,
                                                                                PocRoundShaNonce = roundShaParsed,
                                                                                PocShareNonceMin = nonceMinParse,
                                                                                PocShareNonceMax = nonceMaxParse,
                                                                                RandomDataShareNumberSize = compatibilityPartParsed,
                                                                                RandomDataShareTimestampSize = timestampPartParsed,
                                                                                RandomDataShareBlockHeightSize = blockHeightPartParsed,
                                                                                RandomDataShareChecksum = checksumPartParsed,
                                                                                WalletAddressDataSize = walletDecodedPartParsed,
                                                                                RandomDataShareSize = randomDataShareSize,
                                                                                ShareHexStringSize = shareHexStringSize,
                                                                                BlockHeightStart = blockHeight
                                                                            }, Formatting.None),
                                                                            PossibleContent2 = blockHeight.ToString(),
                                                                            Description = description
                                                                        },
                                                                        SovereignUpdateTimestamp = TaskManager.TaskManager.CurrentTimestampSecond,
                                                                        SovereignUpdateDevWalletAddress = devWalletAddress,
                                                                        SovereignUpdateType = ClassSovereignEnumUpdateType.SOVEREIGN_MINING_POWAC_SETTING_UPDATE
                                                                    };

                                                                    Console.WriteLine("Generate sovereign update hash to sign..");
                                                                    GenerateSovereignHashUpdate(sovereignUpdateObject, out sovereignUpdateObject.SovereignUpdateHash);
                                                                    sovereignUpdateObject.SovereignUpdateSignature = GenerateSovereignSignatureUpdate(sovereignUpdateObject);
                                                                    Console.WriteLine("Sovereign update build and signed.");
                                                                    statutBuild = true;
                                                                    return sovereignUpdateObject;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            Console.WriteLine("Invalid input.");
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown type input: " + updateTypeInput);
                        break;
                }
            }
            else
                Console.WriteLine("Invalid input type.");

            return null;
        }

        /// <summary>
        /// Generate the sovereign hash update from the sovereign update object.
        /// </summary>
        /// <param name="sovereignUpdateObject"></param>
        /// <param name="sovereignUpdateHash"></param>
        /// <returns></returns>
        public static void GenerateSovereignHashUpdate(ClassSovereignUpdateObject sovereignUpdateObject, out string sovereignUpdateHash)
        {
            ClassSovereignUpdateObject copySovereignUpdateObject = new ClassSovereignUpdateObject
            {
                SovereignUpdateContent = sovereignUpdateObject.SovereignUpdateContent,
                SovereignUpdateDevWalletAddress = sovereignUpdateObject.SovereignUpdateDevWalletAddress,
                SovereignUpdateTimestamp = sovereignUpdateObject.SovereignUpdateTimestamp,
                SovereignUpdateType = sovereignUpdateObject.SovereignUpdateType,
                // Do not generate a hash with a hash already present on the sovereign update object.
                SovereignUpdateHash = string.Empty,
                // Do not generate a hash with a signature already present on the sovereign update object.
                SovereignUpdateSignature = string.Empty
            };

            sovereignUpdateHash = ClassUtility.GenerateSha3512FromString(ClassUtility.SerializeData(copySovereignUpdateObject, Formatting.None));
        }

        /// <summary>
        /// Generate the signature of the Sovereign Update with the Dev private key.
        /// </summary>
        /// <param name="sovereignUpdateObject"></param>
        /// <returns></returns>
        private static string GenerateSovereignSignatureUpdate(ClassSovereignUpdateObject sovereignUpdateObject)
        {
            Console.WriteLine("Input Dev Private key to sign the update: ");
            return ClassWalletUtility.WalletGenerateSignature(Console.ReadLine() ?? string.Empty, sovereignUpdateObject.SovereignUpdateHash);
        }

        #endregion

        #region Other Sovereign Update functions.

        /// <summary>
        /// Generate a list of hash of sovereign update(s).
        /// </summary>
        /// <returns></returns>
        public static DisposableList<string> GetSovereignUpdateListHash()
        {
            using (DisposableSortedList<long, string> listSovereignUpdatesHash = new DisposableSortedList<long, string>())
            {

                if (DictionarySovereignUpdateObject?.Count > 0)
                {
                    foreach (var sovereignSortedList in DictionarySortedSovereignUpdateList.Values.ToArray())
                    {
                        foreach (var sovereignUpdateHash in sovereignSortedList)
                            listSovereignUpdatesHash.Add(sovereignUpdateHash.Key, sovereignUpdateHash.Value);
                    }
                }

                return new DisposableList<string>(false, 0, listSovereignUpdatesHash.GetList.Values.ToList());
            }
        }

        /// <summary>
        /// Return back the last sovereign mining update setting object depending of the block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public static ClassMiningPoWaCSettingObject GetLastSovereignUpdateMiningPocSettingObject(long blockHeight)
        {
            if (DictionarySortedSovereignUpdateList?.Count > 0)
            {
                if (DictionarySortedSovereignUpdateList.ContainsKey(ClassSovereignEnumUpdateType.SOVEREIGN_MINING_POWAC_SETTING_UPDATE))
                {
                    if (DictionarySortedSovereignUpdateList[ClassSovereignEnumUpdateType.SOVEREIGN_MINING_POWAC_SETTING_UPDATE].Count > 0)
                    {
                        ClassMiningPoWaCSettingObject lastValidMiningPocSettingObject = null;
                        foreach (var sovereignUpdateHash in DictionarySortedSovereignUpdateList[ClassSovereignEnumUpdateType.SOVEREIGN_MINING_POWAC_SETTING_UPDATE])
                        {
                            if (DictionarySovereignUpdateObject.ContainsKey(sovereignUpdateHash.Value))
                            {
                                if (CheckSovereignUpdateObject(DictionarySovereignUpdateObject[sovereignUpdateHash.Value], out var typeUpdate) == ClassSovereignEnumUpdateCheckStatus.VALID_SOVEREIGN_UPDATE)
                                {
                                    if (typeUpdate == ClassSovereignEnumUpdateType.SOVEREIGN_MINING_POWAC_SETTING_UPDATE)
                                    {
                                        if (ClassUtility.TryDeserialize(DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent1, out ClassMiningPoWaCSettingObject miningPowSettingObject))
                                        {
                                            if (long.TryParse(DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent2, out long blockHeightUpdate))
                                            {
                                                if (blockHeight >= BlockchainSetting.GenesisBlockHeight && blockHeightUpdate == miningPowSettingObject.BlockHeightStart)
                                                {
                                                    if (blockHeight >= blockHeightUpdate)
                                                        lastValidMiningPocSettingObject = miningPowSettingObject;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (lastValidMiningPocSettingObject != null)
                            return lastValidMiningPocSettingObject;
                    }
                }
            }

            return BlockchainSetting.DefaultMiningPocSettingObject;
        }

        /// <summary>
        /// Get the last dev wallet address.
        /// </summary>
        /// <returns></returns>
        public static string GetLastDevWalletAddress(long timestampSovereignUpdate)
        {
            string lastWalletAddressDev = BlockchainSetting.DefaultWalletAddressDev;
            if (DictionarySortedSovereignUpdateList?.Count > 0)
            {
                if (DictionarySortedSovereignUpdateList.ContainsKey(ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE))
                {
                    foreach (var sovereignHash in DictionarySortedSovereignUpdateList[ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE])
                    {
                        if (DictionarySovereignUpdateObject.ContainsKey(sovereignHash.Value))
                        {
                            if (DictionarySovereignUpdateObject[sovereignHash.Value].SovereignUpdateType == ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE)
                            {
                                if (CheckSovereignUpdateObject(DictionarySovereignUpdateObject[sovereignHash.Value], out var typeUpdate) == ClassSovereignEnumUpdateCheckStatus.VALID_SOVEREIGN_UPDATE)
                                {
                                    if (typeUpdate == ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE)
                                    {
                                        if (timestampSovereignUpdate >= DictionarySovereignUpdateObject[sovereignHash.Value].SovereignUpdateTimestamp)
                                        {
                                            if (ClassBase58.DecodeWithCheckSum(DictionarySovereignUpdateObject[sovereignHash.Value].SovereignUpdateContent.PossibleContent1, true) != null)
                                                lastWalletAddressDev = DictionarySovereignUpdateObject[sovereignHash.Value].SovereignUpdateContent.PossibleContent1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return lastWalletAddressDev;
        }

        /// <summary>
        /// Get the last dev wallet public key.
        /// </summary>
        /// <returns></returns>
        public static string GetLastDevWalletPublicKey(long timestampSovereignUpdate)
        {
            string lastWalletAddressDevPublicKey = BlockchainSetting.DefaultWalletAddressDevPublicKey;
            if (DictionarySortedSovereignUpdateList?.Count > 0)
            {
                if (DictionarySortedSovereignUpdateList.ContainsKey(ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE))
                {
                    foreach (var sovereignHash in DictionarySortedSovereignUpdateList[ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE])
                    {
                        if (DictionarySovereignUpdateObject.ContainsKey(sovereignHash.Value) && DictionarySovereignUpdateObject[sovereignHash.Value].SovereignUpdateType == ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE)
                        {
                            if (CheckSovereignUpdateObject(DictionarySovereignUpdateObject[sovereignHash.Value], out var typeUpdate) == ClassSovereignEnumUpdateCheckStatus.VALID_SOVEREIGN_UPDATE)
                            {
                                if (typeUpdate == ClassSovereignEnumUpdateType.SOVEREIGN_DEV_SIGNATURE_CHANGE_UPDATE)
                                {
                                    if (timestampSovereignUpdate >= DictionarySovereignUpdateObject[sovereignHash.Value].SovereignUpdateTimestamp)
                                    {
                                        if (ClassBase58.DecodeWithCheckSum(DictionarySovereignUpdateObject[sovereignHash.Value].SovereignUpdateContent.PossibleContent2, false) != null)
                                            lastWalletAddressDevPublicKey = DictionarySovereignUpdateObject[sovereignHash.Value].SovereignUpdateContent.PossibleContent2;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return lastWalletAddressDevPublicKey;
        }

        /// <summary>
        /// Check if the sovereign update hash already exist.
        /// </summary>
        /// <param name="sovereignUpdateHash"></param>
        /// <returns></returns>
        public static bool CheckIfSovereignUpdateHashExist(string sovereignUpdateHash)
        {
            return DictionarySovereignUpdateObject.ContainsKey(sovereignUpdateHash);
        }

        /// <summary>
        /// Check if the numeric public key is ranked and also not revoked from another sovereign update.
        /// </summary>
        /// <param name="numericPublicKey"></param>
        /// <param name="timestampRankDelay"></param>
        /// <returns></returns>
        public static bool CheckIfNumericPublicKeyPeerIsRanked(string numericPublicKey, out long timestampRankDelay)
        {
            if (DictionarySortedSovereignUpdateList?.Count > 0)
            {
                if (DictionarySortedSovereignUpdateList.ContainsKey(ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_GRANT_RANK_UPDATE))
                {
                    bool canBeRevoked = DictionarySortedSovereignUpdateList.ContainsKey(ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_REVOKE_RANK_UPDATE);

                    bool peerIsRanked = false;
                    long sovereignUpdateTimestamp = 0;
                    long peerRankDelay = 0;

                    // Check if the numeric public key is ranked.
                    foreach (var sovereignUpdateHash in DictionarySortedSovereignUpdateList[ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_GRANT_RANK_UPDATE])
                    {
                        if (DictionarySovereignUpdateObject.ContainsKey(sovereignUpdateHash.Value))
                        {
                            if (DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateType == ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_GRANT_RANK_UPDATE)
                            {
                                if (!DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent1.IsNullOrEmpty(false, out _))
                                {
                                    if (!DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent2.IsNullOrEmpty(false, out _))
                                    {
                                        if (ClassBase58.DecodeWithCheckSum(DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent1, false) != null)
                                        {
                                            if (DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent1 == numericPublicKey)
                                            {
                                                if (long.TryParse(DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent2, out var maxDelay))
                                                {
                                                    if (maxDelay > TaskManager.TaskManager.CurrentTimestampSecond)
                                                    {
                                                        peerIsRanked = true;
                                                        sovereignUpdateTimestamp = DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateTimestamp;
                                                        peerRankDelay = maxDelay;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (peerIsRanked)
                    {
                        // Check if the numeric public key rank has been revoked.
                        if (canBeRevoked)
                        {
                            foreach (var sovereignUpdateHash in DictionarySortedSovereignUpdateList[ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_REVOKE_RANK_UPDATE])
                            {
                                if (DictionarySovereignUpdateObject.ContainsKey(sovereignUpdateHash.Value))
                                {
                                    if (DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateType == ClassSovereignEnumUpdateType.SOVEREIGN_SEED_NODE_REVOKE_RANK_UPDATE)
                                    {
                                        if (!DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent1.IsNullOrEmpty(false, out _))
                                        {

                                            if (!DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent2.IsNullOrEmpty(false, out _))
                                            {
                                                if (ClassBase58.DecodeWithCheckSum(DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent1, false) != null)
                                                {
                                                    if (DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateContent.PossibleContent1 == numericPublicKey)
                                                    {
                                                        // On this case, if the last sovereign update who rank the node has been revoked by an update much recent, we indicate the last rank update is revoked.
                                                        if (DictionarySovereignUpdateObject[sovereignUpdateHash.Value].SovereignUpdateTimestamp >= sovereignUpdateTimestamp)
                                                        {
                                                            timestampRankDelay = 0;
                                                            return false;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            timestampRankDelay = peerRankDelay;
                            return true;
                        }
                    }
                }
            }

            timestampRankDelay = 0;
            return false;
        }

        #endregion
    }

    /// <summary>
    /// Obtain a sovereign update.
    /// </summary>
    public class ClassSovereignUpdateGetter
    {
        /// <summary>
        /// Return a mining setting sovereign update, depending of the block height.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public ClassMiningPoWaCSettingObject GetLastSovereignUpdateMiningPocSettingObject(long blockHeight) => ClassSovereignUpdateDatabase.GetLastSovereignUpdateMiningPocSettingObject(blockHeight);
        
        /// <summary>
        /// Return the dev wallet address from a sovereign update listed.
        /// </summary>
        /// <param name="timestampSovereignUpdate">The timestamp is usefull to get a sovereign update depending of his timestamp.</param>
        /// <returns></returns>
        public string GetLastDevWalletAddress(long timestampSovereignUpdate) => ClassSovereignUpdateDatabase.GetLastDevWalletAddress(timestampSovereignUpdate);

        /// <summary>
        /// Return the latest dev wallet public key from a sovereign update listed.
        /// </summary>
        /// <param name="timestampSovereignUpdate">The timestamp is usefull to get a sovereign update depending of his timestamp.</param>
        /// <returns></returns>
        public string GetLastDevWalletPublicKey(long timestampSovereignUpdate) => ClassSovereignUpdateDatabase.GetLastDevWalletPublicKey(timestampSovereignUpdate);
    }
}
