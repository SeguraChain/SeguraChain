using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SeguraChain_Desktop_Wallet.Common;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Desktop_Wallet.Wallet.Function.Enum;
using SeguraChain_Desktop_Wallet.Wallet.Object;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Blockchain.Wallet.Object.Wallet;
using SeguraChain_Lib.Other.Object.SHA3;
using SeguraChain_Lib.Utility;
using ZXing;
using ZXing.QrCode;

namespace SeguraChain_Desktop_Wallet.Wallet.Function
{
    public class ClassWalletDataFunction
    {
        /// <summary>
        /// Initialize a wallet data object to load.
        /// </summary>
        /// <param name="walletDataObjectLoad"></param>
        /// <param name="walletDataObjectLoaded"></param>
        /// <returns></returns>
        public static ClassWalletLoadFileEnumResult InitializeWalletDataObjectToLoad(ClassWalletDataObject walletDataObjectLoad, out ClassWalletDataObject walletDataObjectLoaded)
        {
            // Initialize at first the data of the wallet by the data loaded.
            walletDataObjectLoaded = walletDataObjectLoad;

            #region Check Wallet data content.

            if (walletDataObjectLoaded.WalletAddress.IsNullOrEmpty(false, out _) ||
                walletDataObjectLoaded.WalletPublicKey.IsNullOrEmpty(false, out _) ||
                walletDataObjectLoaded.WalletPrivateKey.IsNullOrEmpty(false, out _))
                return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_CONTENT;

            if (walletDataObjectLoaded.WalletEncrypted)
            {
                if (walletDataObjectLoaded.WalletEncryptionIv == null ||
                    walletDataObjectLoaded.WalletEncryptionRounds < ClassWalletDefaultSetting.DefaultWalletIterationCount)
                    return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_ENCRYPTION_CONTENT;

                if (!ClassUtility.CheckHexStringFormat(walletDataObjectLoaded.WalletEncryptionIv))
                    return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_ENCRYPTION_CONTENT;

                if (ClassUtility.GetByteArrayFromHexString(walletDataObjectLoaded.WalletEncryptionIv).Length != ClassAes.IvSize)
                    return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_ENCRYPTION_CONTENT;
                
                if (!ClassUtility.CheckHexStringFormat(walletDataObjectLoaded.WalletPrivateKey))
                    return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_ENCRYPTION_CONTENT;
            }
            else
            {
                if (walletDataObjectLoaded.WalletPrivateKey.Length != BlockchainSetting.WalletPrivateKeyWifLength)
                    return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_CONTENT;

                if (ClassBase58.DecodeWithCheckSum(walletDataObjectLoaded.WalletPrivateKey, true) == null)
                    return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_CONTENT;

                if (ClassWalletUtility.GenerateWalletPublicKeyFromPrivateKey(walletDataObjectLoaded.WalletPrivateKey) != walletDataObjectLoaded.WalletPublicKey)
                    return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_CONTENT;
            }

            if (ClassWalletUtility.GenerateWalletAddressFromPublicKey(walletDataObjectLoaded.WalletPublicKey) != walletDataObjectLoaded.WalletAddress)
                return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_CONTENT;

            if (walletDataObjectLoaded.WalletAddress.Length < BlockchainSetting.WalletAddressWifLengthMin || walletDataObjectLoaded.WalletAddress.Length > BlockchainSetting.WalletAddressWifLengthMax)
                return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_CONTENT;

            if (walletDataObjectLoaded.WalletPublicKey.Length != BlockchainSetting.WalletPublicKeyWifLength)
                return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_CONTENT;

            if (ClassBase58.DecodeWithCheckSum(walletDataObjectLoaded.WalletAddress, true) == null ||
                ClassBase58.DecodeWithCheckSum(walletDataObjectLoaded.WalletPublicKey, false) == null)
                return ClassWalletLoadFileEnumResult.WALLET_LOAD_INVALID_CONTENT;

            if (walletDataObjectLoaded.WalletLastBlockHeightSynced < 0)
                walletDataObjectLoaded.WalletLastBlockHeightSynced = 0;

            #endregion

            #region Initialize tx list or clean up tx above the last block height synced.

            if (walletDataObjectLoaded.WalletMemPoolTransactionList == null)
                walletDataObjectLoaded.WalletMemPoolTransactionList = new HashSet<string>();

            if (walletDataObjectLoaded.WalletTransactionList == null)
                walletDataObjectLoaded.WalletTransactionList = new SortedList<long, HashSet<string>>();
            else
            {
                foreach (var blockKey in walletDataObjectLoaded.WalletTransactionList.Keys.ToArray())
                {
                    if (blockKey > walletDataObjectLoaded.WalletLastBlockHeightSynced)
                        walletDataObjectLoaded.WalletTransactionList.Remove(blockKey);
                }
            }

            #endregion

            return ClassWalletLoadFileEnumResult.WALLET_LOAD_SUCCESS;
        }

        /// <summary>
        /// Check if a transaction already exist.
        /// </summary>
        /// <param name="walletFilename"></param>
        /// <param name="transactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public static bool CheckTransactionExist(string walletFilename, string transactionHash, long blockHeight)
        {
            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletTransactionList.ContainsKey(blockHeight))
                return ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletTransactionList[blockHeight].Contains(transactionHash);

            return false;
        }

        /// <summary>
        /// Check if a transaction already exist.
        /// </summary>
        /// <param name="walletFilename"></param>
        /// <param name="transactionHash"></param>
        /// <returns></returns>
        public static bool CheckMemPoolTransactionExist(string walletFilename, string transactionHash)
        {
            return ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletMemPoolTransactionList.Contains(transactionHash);
        }

        /// <summary>
        /// Encrypt the wallet private key. This method combine SHA512 rounds for generate the next encryption key. And use of course AES to encrypt the private key.
        /// </summary>
        /// <param name="walletFilename"></param>
        /// <param name="password"></param>
        /// <param name="walletTotalIteration"></param>
        /// <returns></returns>
        public static ClassWalletEncryptWalletPrivateKeyEnumResult EncryptWalletPrivateKeyFromDatabase(string walletFilename, string password, int walletTotalIteration)
        {
            ClassWalletEncryptWalletPrivateKeyEnumResult result;
            if (walletTotalIteration < ClassWalletDefaultSetting.DefaultWalletIterationCount)
                result = ClassWalletEncryptWalletPrivateKeyEnumResult.WALLET_ENCRYPT_PRIVATE_KEY_BAD_ITERATION_COUNT;
            else
            {
                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletEncrypted)
                    result = ClassWalletEncryptWalletPrivateKeyEnumResult.WALLET_ENCRYPT_PRIVATE_KEY_ALREADY;
                else
                {
                    if (!ClassUtility.CheckBase64String(ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletPrivateKey))
                    {
                        result = EncryptWalletPrivateKey(password, ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletPrivateKey, walletTotalIteration, out string privateKeyEncryptedHex, out string walletPrivateKeyDecoded, out string passphraseHashHex);

                        if (result == ClassWalletEncryptWalletPrivateKeyEnumResult.WALLET_ENCRYPT_PRIVATE_KEY_SUCCESS)
                        {
                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletEncrypted = true;
                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletPrivateKey = privateKeyEncryptedHex;
                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletPassphraseHash = passphraseHashHex;
                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletEncryptionIv = walletPrivateKeyDecoded;
                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletEncryptionRounds = walletTotalIteration;
                        }
                    }
                    else
                        result = ClassWalletEncryptWalletPrivateKeyEnumResult.WALLET_ENCRYPT_PRIVATE_KEY_ALREADY;
                }
            }

            return result;
        }

        /// <summary>
        /// Decrypt the wallet private key of a wallet file from the database target. This method combine SHA512 rounds for generate the next decryption key. And use of course AES to decrypt the private key.
        /// </summary>
        /// <param name="walletFilename"></param>
        /// <param name="password"></param>
        /// <param name="walletPrivateKey"></param>
        /// <returns></returns>
        public static ClassWalletDecryptWalletPrivateKeyEnumResult DecryptWalletPrivateKeyFromDatabase(string walletFilename, string password, out string walletPrivateKey)
        {
            walletPrivateKey = null;
            ClassWalletDecryptWalletPrivateKeyEnumResult result = ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_SUCCESS;

            if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletEncrypted)
            {
                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletEncryptionRounds >= ClassWalletDefaultSetting.DefaultWalletIterationCount)
                {
                    if (!ClassUtility.CheckHexStringFormat(ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletPrivateKey)
                    || !ClassUtility.CheckHexStringFormat(ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletEncryptionIv))
                        result = ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_BAD_FORMAT;
                    else
                    {
                        var decryptResult = DecryptWalletPrivateKey(ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletPrivateKey, ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletPassphraseHash,
                            ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletEncryptionIv, password, ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletEncryptionRounds, out walletPrivateKey);

                        if (decryptResult == ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_SUCCESS)
                        {
                            if (ClassWalletUtility.GenerateWalletPrivateKey(walletPrivateKey) != ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData[walletFilename].WalletPublicKey)
                                result = ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_ERROR;
                        }
                    }
                }
                else
                    result = ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_BAD_ITERATION_COUNT;
            }
            else
                result = ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_NOT_ENCRYPTED;

            return result;
        }

        /// <summary>
        /// Encrypt a private key with a password and follow the amount of iterations of encryption to do.
        /// </summary>
        /// <param name="privateKey"></param>
        /// <param name="password"></param>
        /// <param name="walletTotalIteration"></param>
        /// <param name="privateKeyEncryptedHex"></param>
        /// <param name="walletEncryptionIvHex"></param>
        /// <param name="passphraseHashHex"></param>
        /// <returns>Return the private key hex encrypted and his iv</returns>
        public static ClassWalletEncryptWalletPrivateKeyEnumResult EncryptWalletPrivateKey(string privateKey, string password, int walletTotalIteration, out string privateKeyEncryptedHex, out string walletEncryptionIvHex, out string passphraseHashHex)
        {
            ClassWalletEncryptWalletPrivateKeyEnumResult result = ClassWalletEncryptWalletPrivateKeyEnumResult.WALLET_ENCRYPT_PRIVATE_KEY_SUCCESS;

            privateKeyEncryptedHex = null;
            walletEncryptionIvHex = null;
            passphraseHashHex = null;

            byte[] walletPrivateKeyDecoded = ClassBase58.DecodeWithCheckSum(privateKey, true);
            if (walletPrivateKeyDecoded != null)
            {
                int countIteration = 0;
                using (ClassSha3512DigestDisposable sha3512Digest = new ClassSha3512DigestDisposable())
                {
                    sha3512Digest.Compute(ClassUtility.GetByteArrayFromStringUtf8(password + countIteration), out byte[] walletPassphraseHashArray);
                    Array.Resize(ref walletPassphraseHashArray, ClassAes.EncryptionKeyByteArraySize);
                    sha3512Digest.Reset();

                    // The IV stay unique.
                    byte[] walletEncryptionIv = ClassAes.GenerateIv(ClassUtility.GetByteArrayFromStringUtf8(password));

                    while (countIteration < walletTotalIteration)
                    {
                        // Increase interation.
                        countIteration++;

                        // Update the encryption after each encryptions done.

                        byte[] argumentToHash = ClassUtility.GetByteArrayFromStringUtf8(password + countIteration);
                        byte[] passphaseToHash = new byte[walletPassphraseHashArray.Length + argumentToHash.Length];

                        Array.Copy(walletPassphraseHashArray, 0, passphaseToHash, 0, walletPassphraseHashArray.Length);
                        Array.Copy(argumentToHash, 0, passphaseToHash, walletPassphraseHashArray.Length, argumentToHash.Length);

                        sha3512Digest.Compute(passphaseToHash, out walletPassphraseHashArray);
                        sha3512Digest.Reset();
                        Array.Resize(ref walletPassphraseHashArray, ClassAes.EncryptionKeyByteArraySize);
                    }

                    if (!ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringUtf8(password), true, out byte[] passwordKey))
                        result = ClassWalletEncryptWalletPrivateKeyEnumResult.WALLET_ENCRYPT_PRIVATE_KEY_ERROR;
                    else
                    {
                        if (!ClassAes.EncryptionProcess(walletPassphraseHashArray, passwordKey, walletEncryptionIv, out byte[] passphraseHashKey))
                            result = ClassWalletEncryptWalletPrivateKeyEnumResult.WALLET_ENCRYPT_PRIVATE_KEY_ERROR;
                        else
                        {
                            Array.Resize(ref passphraseHashKey, ClassAes.EncryptionKeyByteArraySize);

                            if (!ClassAes.EncryptionProcess(walletPrivateKeyDecoded, passphraseHashKey, walletEncryptionIv, out byte[] privateKeyEncrypted))
                                result = ClassWalletEncryptWalletPrivateKeyEnumResult.WALLET_ENCRYPT_PRIVATE_KEY_ERROR;
                            else
                            {
                                passphraseHashHex = ClassUtility.GetHexStringFromByteArray(walletPassphraseHashArray);
                                privateKeyEncryptedHex = ClassUtility.GetHexStringFromByteArray(privateKeyEncrypted);
                                walletEncryptionIvHex = ClassUtility.GetHexStringFromByteArray(walletEncryptionIv);
                            }
                        }
                    }

                    if (walletEncryptionIv.Length > 0)
                        Array.Resize(ref walletEncryptionIv, 0) ;

                    if (walletPassphraseHashArray.Length > 0)
                        Array.Resize(ref walletPassphraseHashArray, 0);
                }

                if (walletPrivateKeyDecoded.Length > 0)
                    Array.Resize(ref walletPrivateKeyDecoded, 0);
            }
            else
                result = ClassWalletEncryptWalletPrivateKeyEnumResult.WALLET_ENCRYPT_PRIVATE_KEY_BAD_FORMAT;

            return result;
        }

        /// <summary>
        /// Decrypt the wallet private key target. This method combine SHA512 rounds for generate the next decryption key. And use of course AES to decrypt the private key.
        /// </summary>
        /// <param name="privateKeyEncryptedHex"></param>
        /// <param name="passphraseHashHex"></param>
        /// <param name="encryptionIvHex"></param>
        /// <param name="password"></param>
        /// <param name="totalWalletIteration"></param>
        /// <param name="walletPrivateKeyDecrypted"></param>
        /// <returns></returns>
        public static ClassWalletDecryptWalletPrivateKeyEnumResult DecryptWalletPrivateKey(string privateKeyEncryptedHex, string passphraseHashHex, string encryptionIvHex, string password, int totalWalletIteration, out string walletPrivateKeyDecrypted)
        {
            // Default value.
            walletPrivateKeyDecrypted = null;

            ClassWalletDecryptWalletPrivateKeyEnumResult result = ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_SUCCESS;

            byte[] walletPrivateKeyEncrypted = ClassUtility.GetByteArrayFromHexString(privateKeyEncryptedHex);
            byte[] walletEncryptionIv = ClassUtility.GetByteArrayFromHexString(encryptionIvHex);
            int countIteration = 0;

            using (ClassSha3512DigestDisposable sha3512Digest = new ClassSha3512DigestDisposable())
            {
                sha3512Digest.Compute(ClassUtility.GetByteArrayFromStringUtf8(password + countIteration), out byte[] walletPassphraseHashArray);
                Array.Resize(ref walletPassphraseHashArray, ClassAes.EncryptionKeyByteArraySize);
                sha3512Digest.Reset();

                while (countIteration < totalWalletIteration)
                {
                    // Increase iteration.
                    countIteration++;

                    // Update the encryption after each encryptions done.
                    byte[] argumentToHash = ClassUtility.GetByteArrayFromStringUtf8(password + countIteration);
                    byte[] passphaseToHash = new byte[walletPassphraseHashArray.Length + argumentToHash.Length];

                    Array.Copy(walletPassphraseHashArray, 0, passphaseToHash, 0, walletPassphraseHashArray.Length);
                    Array.Copy(argumentToHash, 0, passphaseToHash, walletPassphraseHashArray.Length, argumentToHash.Length);

                    sha3512Digest.Compute(passphaseToHash, out walletPassphraseHashArray);
                    sha3512Digest.Reset();
                    Array.Resize(ref walletPassphraseHashArray, ClassAes.EncryptionKeyByteArraySize);
                }


                if (ClassUtility.GetHexStringFromByteArray(walletPassphraseHashArray) != passphraseHashHex)
                    result = ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_ERROR;
                else
                {
                    if (!ClassAes.GenerateKey(ClassUtility.GetByteArrayFromStringUtf8(password), true, out byte[] passwordKey))
                        result = ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_ERROR;
                    else
                    {
                        if (!ClassAes.EncryptionProcess(walletPassphraseHashArray, passwordKey, walletEncryptionIv, out byte[] passphraseHashKey))
                            result = ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_ERROR;
                        else
                        {

                            Array.Resize(ref passphraseHashKey, ClassAes.EncryptionKeyByteArraySize);

                            if (!ClassAes.DecryptionProcess(walletPrivateKeyEncrypted, passphraseHashKey, walletEncryptionIv, out byte[] privateKeyDecrypted))
                                result = ClassWalletDecryptWalletPrivateKeyEnumResult.WALLET_DECRYPT_PRIVATE_KEY_ERROR;
                            else
                                walletPrivateKeyDecrypted = ClassBase58.EncodeWithCheckSum(privateKeyDecrypted);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Generate a new wallet data object who can be saved directly.
        /// </summary>
        /// <param name="walletFilename"></param>
        /// <returns></returns>
        public static async Task<bool> GenerateNewWalletDataToSave(string walletFilename)
        {
            bool createResultStatus = false;

            string walletFilePath = ClassUtility.ConvertPath(ClassDesktopWalletCommonData.WalletSettingObject.WalletDirectoryPath + walletFilename);

            if (!File.Exists(walletFilePath))
            {
                ClassWalletDataObject walletDataObject = GenerateNewWalletDataObject(null, true);
                walletDataObject.WalletFileName = walletFilename;

                // Lock the file.
                walletDataObject.WalletFileStream = new FileStream(walletFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);

                if (ClassDesktopWalletCommonData.WalletDatabase.DictionaryWalletData.TryAdd(walletDataObject.WalletFileName, walletDataObject))
                    createResultStatus = await ClassDesktopWalletCommonData.WalletDatabase.SaveWalletFileAsync(walletDataObject.WalletFileName);
            }

            return createResultStatus;
        }

        /// <summary>
        /// Generata new wallet data object.
        /// </summary>
        /// <returns></returns>
        public static ClassWalletDataObject GenerateNewWalletDataObject(string baseWords, bool useFastway)
        {
            ClassWalletGeneratorObject walletGeneratedObject = ClassWalletUtility.GenerateWallet(baseWords, useFastway);

            return new ClassWalletDataObject
            {
                WalletAddress = walletGeneratedObject.WalletAddress,
                WalletPublicKey = walletGeneratedObject.WalletPublicKey,
                WalletPrivateKey = walletGeneratedObject.WalletPrivateKey
            };
        }

        public static ClassWalletDataObject GenerateWalletFromPrivateKey(string privateKey)
        {
            string walletPublicKey = ClassWalletUtility.GenerateWalletPublicKeyFromPrivateKey(privateKey);
            string walletAddress = ClassWalletUtility.GenerateWalletAddressFromPublicKey(walletPublicKey);

            return new ClassWalletDataObject
            {
                WalletAddress = walletAddress,
                WalletPublicKey = walletPublicKey,
                WalletPrivateKey = privateKey
            };
        }

        /// <summary>
        /// Generate a qr code depending of the argument provided.
        /// </summary>
        /// <param name="walletContent"></param>
        /// <returns></returns>
        public static Bitmap GenerateBitmapWalletQrCode(string walletContent)
        {
            return new Bitmap(new BarcodeWriter()
            {
                Options = new QrCodeEncodingOptions
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = ClassWalletDefaultSetting.DefaultQrCodeLengthWidthSize,
                    Height = ClassWalletDefaultSetting.DefaultQrCodeLengthWidthSize
                },
                Format = BarcodeFormat.QR_CODE
            }.Write(walletContent.Trim()));
        }
    }
}
