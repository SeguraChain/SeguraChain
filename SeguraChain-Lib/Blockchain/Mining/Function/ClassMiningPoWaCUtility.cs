using System;
using System.Numerics;
using System.Security.Cryptography;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Mining.Enum;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Wallet.Function;
using SeguraChain_Lib.Other.Object.Math;
using SeguraChain_Lib.Other.Object.SHA3;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Mining.Function
{
    /// <summary>
    /// The mining PoWaC (Proof of Compatibility) class. A combinaison of PoW and Compatibility with block tx data count.
    /// </summary>
    public class ClassMiningPoWaCUtility
    {
        public const string MathOperatorPlus = "+";
        public const string MathOperatorMinus = "-";
        public const string MathOperatorMultiplicate = "*";
        public const string MathOperatorModulo = "%";

        private const int ShaDefaultValue = 2;
        public const int ShaExponent = 512;

        public static readonly BigInteger ShaPowCalculation = BigInteger.Pow(ShaDefaultValue, ShaExponent);

        /// <summary>
        /// Do a PoC Share.
        /// </summary>
        /// <param name="currentMiningSetting"></param>
        /// <param name="walletAddress"></param>
        /// <param name="blockHeight"></param>
        /// <param name="blockHash"></param>
        /// <param name="blockDifficulty"></param>
        /// <param name="previousFinalBlockTransactionHashKey"></param>
        /// <param name="pocShareData"></param>
        /// <param name="nextNonce"></param>
        /// <param name="timestampShare"></param>
        /// <param name="sha3512Digest"></param>
        /// <returns></returns>
        public static ClassMiningPoWaCShareObject DoPoWaCShare(ClassMiningPoWaCSettingObject currentMiningSetting, string walletAddress, long blockHeight, string blockHash, BigInteger blockDifficulty, byte[] previousFinalBlockTransactionHashKey, byte[] pocShareData, long nextNonce, long timestampShare, ClassSha3512DigestDisposable sha3512Digest)
        {
            byte[] pocShareIv = BitConverter.GetBytes(nextNonce);

            // Do mining instructions listed on the current mining setting.
            foreach (var miningInstruction in currentMiningSetting.MiningIntructionsList)
            {
                switch (miningInstruction)
                {
                    case ClassMiningPoWaCEnumInstructions.DO_NONCE_IV:
                        {
                            for (int i = 0; i < currentMiningSetting.PocRoundShaNonce; i++)
                            {
                                sha3512Digest.Compute(pocShareIv, out pocShareIv);
                                sha3512Digest.Reset();
                            }
                        }
                        break;
                    case ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_XOR:
                        {
                            pocShareIv = GetNonceIvXorData(pocShareIv);
                        }
                        break;
                    case ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_EASY_SQUARE_MATH:
                        {
                            long newNonce = GetNonceIvComplexArithmetic(currentMiningSetting, pocShareIv, previousFinalBlockTransactionHashKey, blockHeight, blockDifficulty);

                            if (newNonce >= currentMiningSetting.PocShareNonceMin && newNonce <= currentMiningSetting.PocShareNonceMax)
                                pocShareIv = BitConverter.GetBytes(newNonce);
                            else
                                return null;
                        }
                        break;
                    case ClassMiningPoWaCEnumInstructions.DO_LZ4_COMPRESS_NONCE_IV:
                        {
                            pocShareIv = ClassUtility.WrapDataLz4(pocShareIv);
                        }
                        break;
                    case ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_ITERATIONS:
                        {
                            pocShareIv = ClassAes.GenerateIv(pocShareIv, currentMiningSetting.PocShareNonceIvIteration);
                        }
                        break;
                    case ClassMiningPoWaCEnumInstructions.DO_ENCRYPTED_POC_SHARE:
                        {
                            pocShareData = DoEncryptionPocShare(currentMiningSetting, pocShareData, previousFinalBlockTransactionHashKey, pocShareIv);
                        }
                        break;
                }
            }

            if (pocShareData == null)
                return null;

            string pocShare = ClassUtility.GetHexStringFromByteArray(pocShareData);

            BigInteger pocShareDifficulty = CalculateDifficultyShare(pocShareData, blockDifficulty);

            return new ClassMiningPoWaCShareObject()
            {
                WalletAddress = walletAddress,
                BlockHeight = blockHeight,
                BlockHash = blockHash,
                Nonce = nextNonce,
                PoWaCShare = pocShare,
                PoWaCShareDifficulty = pocShareDifficulty,
                NonceComputedHexString = ClassUtility.GetHexStringFromByteArray(pocShareIv),
                Timestamp = timestampShare
            };
        }

        /// <summary>
        /// Check a pow share.
        /// </summary>
        /// <param name="currentMiningSetting"></param>
        /// <param name="pocShareObject"></param>
        /// <param name="blockHeight"></param>
        /// <param name="blockHash"></param>
        /// <param name="blockDifficulty"></param>
        /// <param name="previousBlockTransactionCount"></param>
        /// <param name="previousFinalBlockTransactionHash"></param>
        /// <param name="jobDifficulty"></param>
        /// <param name="jobCompatibilityValue"></param>
        /// <returns></returns>
        public static ClassMiningPoWaCEnumStatus CheckPoWaCShare(ClassMiningPoWaCSettingObject currentMiningSetting, ClassMiningPoWaCShareObject pocShareObject, long blockHeight, string blockHash, BigInteger blockDifficulty, int previousBlockTransactionCount, string previousFinalBlockTransactionHash, out BigInteger jobDifficulty, out int jobCompatibilityValue)
        {
            // Initialized to 0.
            jobCompatibilityValue = -1;
            jobDifficulty = 0;
            bool validCompatibility = false;
            bool validDifficulty = false;

            if (pocShareObject == null)
                return ClassMiningPoWaCEnumStatus.EMPTY_SHARE;

            #region Check Wallet Address.

            if (pocShareObject.WalletAddress.IsNullOrEmpty(false, out _))
                return ClassMiningPoWaCEnumStatus.INVALID_WALLET_ADDRESS;

            if (pocShareObject.WalletAddress.Length < BlockchainSetting.WalletAddressWifLengthMin || pocShareObject.WalletAddress.Length > BlockchainSetting.WalletAddressWifLengthMax)
                return ClassMiningPoWaCEnumStatus.INVALID_WALLET_ADDRESS;

            if (ClassBase58.DecodeWithCheckSum(pocShareObject.WalletAddress, true) == null)
                return ClassMiningPoWaCEnumStatus.INVALID_WALLET_ADDRESS;

            #endregion

            #region Check Block Hash.

            if (pocShareObject.BlockHash.IsNullOrEmpty(false, out _))
                return ClassMiningPoWaCEnumStatus.INVALID_BLOCK_HASH;

            if (pocShareObject.BlockHash != blockHash)
                return ClassMiningPoWaCEnumStatus.INVALID_BLOCK_HASH;

            if (pocShareObject.BlockHeight != blockHeight)
                return ClassMiningPoWaCEnumStatus.INVALID_BLOCK_HEIGHT;

            #endregion

            #region Check Share Nonce.

            if (pocShareObject.NonceComputedHexString.IsNullOrEmpty(false, out _))
                return ClassMiningPoWaCEnumStatus.INVALID_NONCE_SHARE;

            if (!ClassUtility.CheckHexStringFormat(pocShareObject.NonceComputedHexString))
                return ClassMiningPoWaCEnumStatus.INVALID_NONCE_SHARE;

            byte[] pocShareNonceIv = ClassUtility.GetByteArrayFromHexString(pocShareObject.NonceComputedHexString);

            if (pocShareNonceIv.Length != ClassAes.IvSize)
                return ClassMiningPoWaCEnumStatus.INVALID_NONCE_SHARE;

            if (pocShareObject.Nonce < currentMiningSetting.PocShareNonceMin || pocShareObject.Nonce > currentMiningSetting.PocShareNonceMax)
                return ClassMiningPoWaCEnumStatus.INVALID_NONCE_SHARE;

            #endregion

            #region Check PoC Share format.

            if (pocShareObject.PoWaCShare.IsNullOrEmpty(false, out _))
                return ClassMiningPoWaCEnumStatus.EMPTY_SHARE;

            if (ClassUtility.CheckStringUseLowercaseOnly(pocShareObject.PoWaCShare))
                return ClassMiningPoWaCEnumStatus.INVALID_SHARE_FORMAT;

            if (!ClassUtility.CheckHexStringFormat(pocShareObject.PoWaCShare))
                return ClassMiningPoWaCEnumStatus.INVALID_SHARE_FORMAT;

            #endregion

            #region Check PoC share size.

            if (pocShareObject.PoWaCShare.Length != currentMiningSetting.ShareHexStringSize)
                return ClassMiningPoWaCEnumStatus.INVALID_SHARE_DATA;

            #endregion

            #region Convert hex string share data into byte array.

            byte[] pocShareBytes = ClassUtility.GetByteArrayFromHexString(pocShareObject.PoWaCShare);

            if (pocShareBytes == null)
                return ClassMiningPoWaCEnumStatus.INVALID_SHARE_DATA;

            #endregion

            #region Check share difficulty.

            BigInteger pocShareDifficultyCheck = CalculateDifficultyShare(pocShareBytes, blockDifficulty);

            if (pocShareDifficultyCheck != pocShareObject.PoWaCShareDifficulty)
                return ClassMiningPoWaCEnumStatus.INVALID_SHARE_DIFFICULTY;

            #endregion

            #region Check job difficulty with the current block difficulty.

            // Set the returned job difficulty.
            jobDifficulty = pocShareDifficultyCheck;

            // Valid share, but not enough difficult to unlock a block.
            if (pocShareDifficultyCheck < blockDifficulty)
            {
                // Can't be negative or equal of 0.
                if (pocShareDifficultyCheck <= 0)
                    return ClassMiningPoWaCEnumStatus.LOW_DIFFICULTY_SHARE;
            }
            else
                validDifficulty = true;

            #endregion

            #region Check the PoWaC share encryption with current data provided.

            byte[] pocShareDecryptedBytes = pocShareBytes;
            byte[] finalBlockTransactionHashMiningKey = GenerateFinalBlockTransactionHashMiningKey(previousFinalBlockTransactionHash);

            pocShareDecryptedBytes = DoDecryptionPocShare(currentMiningSetting, pocShareDecryptedBytes, finalBlockTransactionHashMiningKey, pocShareNonceIv, out bool result);

            if (!result)
                return ClassMiningPoWaCEnumStatus.INVALID_SHARE_ENCRYPTION;

            if (pocShareDecryptedBytes == null)
                return ClassMiningPoWaCEnumStatus.INVALID_SHARE_ENCRYPTION;

            #endregion

            #region Do the whole process of the mining share to ensure the work done.

            pocShareDecryptedBytes = pocShareBytes;
            byte[] pocShareIv = BitConverter.GetBytes(pocShareObject.Nonce);

            // Do mining instructions listed on the current mining setting.
            foreach (var miningInstruction in currentMiningSetting.MiningIntructionsList)
            {
                switch (miningInstruction)
                {
                    case ClassMiningPoWaCEnumInstructions.DO_NONCE_IV:
                        {
                            using (ClassSha3512DigestDisposable sha3512 = new ClassSha3512DigestDisposable())
                            {
                                for (int i = 0; i < currentMiningSetting.PocRoundShaNonce; i++)
                                {
                                    sha3512.Compute(pocShareIv, out pocShareIv);
                                    sha3512.Reset();
                                }
                            }
                        }
                        break;
                    case ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_XOR:
                        {
                            pocShareIv = GetNonceIvXorData(pocShareIv);
                        }
                        break;
                    case ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_EASY_SQUARE_MATH:
                        {
                            long newNonce = GetNonceIvComplexArithmetic(currentMiningSetting, pocShareIv, finalBlockTransactionHashMiningKey, blockHeight, blockDifficulty);

                            if (newNonce >= currentMiningSetting.PocShareNonceMin && newNonce <= currentMiningSetting.PocShareNonceMax)
                                pocShareIv = BitConverter.GetBytes(newNonce);
                            else
                                return ClassMiningPoWaCEnumStatus.INVALID_NONCE_SHARE;
                        }
                        break;
                    case ClassMiningPoWaCEnumInstructions.DO_LZ4_COMPRESS_NONCE_IV:
                        {
                            pocShareIv = ClassUtility.WrapDataLz4(pocShareIv);
                        }
                        break;
                    case ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_ITERATIONS:
                        {
                            pocShareIv = ClassAes.GenerateIv(pocShareIv, currentMiningSetting.PocShareNonceIvIteration);
                        }
                        break;
                    // Do again the decryption of the data of the share to ensure equality with the nonce generated.
                    case ClassMiningPoWaCEnumInstructions.DO_ENCRYPTED_POC_SHARE:
                        {
                            if (!pocShareIv.CompareArray(pocShareNonceIv))
                                return ClassMiningPoWaCEnumStatus.INVALID_NONCE_SHARE;

                            pocShareDecryptedBytes = DoDecryptionPocShare(currentMiningSetting, pocShareDecryptedBytes, finalBlockTransactionHashMiningKey, pocShareIv, out result);

                            if (!result)
                                return ClassMiningPoWaCEnumStatus.INVALID_SHARE_ENCRYPTION;

                            if (pocShareDecryptedBytes == null)
                                return ClassMiningPoWaCEnumStatus.INVALID_SHARE_ENCRYPTION;
                        }
                        break;
                }
            }

            #endregion

            #region Check PoC Share data decrypted.

            // Check Random PoC data size.
            // On this case we resize the share random data decrypted.
            if (pocShareDecryptedBytes.Length != currentMiningSetting.RandomDataShareSize)
                Array.Resize(ref pocShareDecryptedBytes, currentMiningSetting.RandomDataShareSize);

            #endregion

            #region Generate two numbers and the timestamp of the share from random data for calculate the proof of compatibility.

            GetCompatibilityDataFromPocRandomData(currentMiningSetting, pocShareDecryptedBytes, out long timestampShare, out long blockHeightShare, out int numberOne, out int numberTwo, out long nonce);

            if (timestampShare != pocShareObject.Timestamp)
                return ClassMiningPoWaCEnumStatus.INVALID_TIMESTAMP_SHARE;

            if (blockHeightShare != blockHeight)
                return ClassMiningPoWaCEnumStatus.INVALID_BLOCK_HEIGHT;

            if (nonce != pocShareObject.Nonce)
                return ClassMiningPoWaCEnumStatus.INVALID_NONCE_SHARE;

            if (CheckPoc(currentMiningSetting, numberOne, numberTwo, previousBlockTransactionCount, out jobCompatibilityValue))
                validCompatibility = true;

            #endregion

            #region Retrieve back the wallet address decoded from the poc random data share and compare it.

            if (ClassBase58.EncodeWithCheckSum(GetWalletAddressDecodedFromPocRandomData(currentMiningSetting, pocShareDecryptedBytes)) != pocShareObject.WalletAddress)
                return ClassMiningPoWaCEnumStatus.INVALID_WALLET_ADDRESS;

            #endregion

            #region Determine the final result of the share.

            if (validDifficulty)
            {
                if (validCompatibility)
                    return ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE;

                return ClassMiningPoWaCEnumStatus.INVALID_SHARE_COMPATIBILITY;
            }

            if (validCompatibility)
                return ClassMiningPoWaCEnumStatus.VALID_SHARE;

            #endregion

            return ClassMiningPoWaCEnumStatus.INVALID_SHARE_DIFFICULTY;
        }

        /// <summary>
        /// Generate the final block transaction hash mining key.
        /// </summary>
        /// <param name="previousFinalBlockTransactionHash"></param>
        /// <returns></returns>
        public static byte[] GenerateFinalBlockTransactionHashMiningKey(string previousFinalBlockTransactionHash)
        {
            if (!previousFinalBlockTransactionHash.IsNullOrEmpty(false, out _))
            {
                using (ClassSha3512DigestDisposable sha3512 = new ClassSha3512DigestDisposable())
                {
                    sha3512.Compute(previousFinalBlockTransactionHash.GetByteArray(true), out byte[] pocShareKey);
                    Array.Resize(ref pocShareKey, ClassAes.EncryptionKeyByteArraySize);
                    return pocShareKey;
                }
            }

            return null;
        }

        /// <summary>
        /// Generate random PoC data who return a perfect compatibility with the previous block transaction count provided.
        /// </summary>
        /// <param name="currentMiningSetting"></param>
        /// <param name="previousBlockTransactionCount">previous transaction count.</param>
        /// <param name="blockHeight"></param>
        /// <param name="timestampSecond">8 bytes length</param>
        /// <param name="walletAddressDecoded"></param>
        /// <param name="pocTxCount"></param>
        /// <returns></returns>
        public static byte[] GenerateRandomPocData(ClassMiningPoWaCSettingObject currentMiningSetting, int previousBlockTransactionCount, long blockHeight, long timestampSecond, byte[] walletAddressDecoded, long nonce, out int pocTxCount)
        {
            // timestamp of the share into bytes one time.
            byte[] timestampSecondBytes = BitConverter.GetBytes(timestampSecond);


            // Research the right amount of tx's target with the random data generated.
            // Update it until to found something equal before to encrypt the share.
            // The process can be long.
            while (true)
            {
                #region Generate two numbers from random data for calculate the proof of compatibility.

                int numberOne = ClassUtility.GetRandomBetweenInt(0, previousBlockTransactionCount); // 4 bytes length.
                int numberTwo = ClassUtility.GetRandomBetweenInt(0, previousBlockTransactionCount); // 4 bytes length.

                #endregion

                #region Test all proof of compatibilities with allowed math operators.

                if (CheckPoc(currentMiningSetting, numberOne, numberTwo, previousBlockTransactionCount, out pocTxCount) && pocTxCount == previousBlockTransactionCount)
                {
                    if (pocTxCount == previousBlockTransactionCount)
                    {
                        #region the Poc random data = Random numbers + random data.

                        // Initialize poc random data.
                        byte[] pocRandomData = new byte[currentMiningSetting.RandomDataShareSize];

                        // Fill Poc random data.
                        Array.Copy(BitConverter.GetBytes(numberOne), 0, pocRandomData, 0, currentMiningSetting.RandomDataShareNumberSize / 2);
                        Array.Copy(BitConverter.GetBytes(numberTwo), 0, pocRandomData, currentMiningSetting.RandomDataShareNumberSize / 2, currentMiningSetting.RandomDataShareNumberSize / 2);
                        Array.Copy(timestampSecondBytes, 0, pocRandomData, currentMiningSetting.RandomDataShareTimestampSize, currentMiningSetting.RandomDataShareTimestampSize);

                        // Fill Poc random data with random data on the checksum part. Increase by 32 bytes the length of the PoC share (Checksum 32 bytes size).
                        using (RNGCryptoServiceProvider rngCrypto = new RNGCryptoServiceProvider())
                            rngCrypto.GetBytes(pocRandomData, currentMiningSetting.RandomDataShareNumberSize + currentMiningSetting.RandomDataShareTimestampSize, currentMiningSetting.RandomDataShareChecksum);

                        // Copy wallet address decoded.
                        Array.Copy(walletAddressDecoded, 0, pocRandomData, currentMiningSetting.RandomDataShareNumberSize + currentMiningSetting.RandomDataShareTimestampSize + currentMiningSetting.RandomDataShareChecksum, currentMiningSetting.WalletAddressDataSize);

                        // Copy the block height.
                        Array.Copy(BitConverter.GetBytes(blockHeight), 0, pocRandomData, currentMiningSetting.RandomDataShareNumberSize + currentMiningSetting.RandomDataShareTimestampSize + currentMiningSetting.RandomDataShareChecksum + currentMiningSetting.WalletAddressDataSize, currentMiningSetting.RandomDataShareBlockHeightSize);

                        // Copy the nonce.
                        Array.Copy(BitConverter.GetBytes(nonce), 0, pocRandomData, currentMiningSetting.RandomDataShareNumberSize + currentMiningSetting.RandomDataShareTimestampSize + currentMiningSetting.RandomDataShareChecksum + currentMiningSetting.WalletAddressDataSize + currentMiningSetting.RandomDataShareBlockHeightSize, currentMiningSetting.RandomDataShareNumberSize);

                        return pocRandomData;

                        #endregion
                    }
                }


                #endregion
            }
        }

        /// <summary>
        /// Xor the nonce iv with his reverted one.
        /// </summary>
        /// <param name="pocShareIv"></param>
        /// <returns></returns>
        public static byte[] GetNonceIvXorData(byte[] pocShareIv)
        {
            byte[] pocShareIvMath = new byte[pocShareIv.Length];

            int index = 0;
            int end = pocShareIv.Length - 1;

            foreach (var data in pocShareIv)
            {
                pocShareIvMath[index] = (byte)(data ^ pocShareIv[end - index]);
                index++;
            }

            return pocShareIvMath;
        }

        /// <summary>
        /// Try to generate virtually a square from the nonce iv provided. If a square is found, some sha3-512 computations avoided, and speed up the mining process and the luck.
        ///             
        ///     *
        ///     *     x1               y1
        ///     *  x2 ******************* x3
        ///     *     *                 *                    
        ///     *     *                 *
        ///     *     *                 *     [------ Generate this virtually. At least we try, we don't try to found a perfect square, but something who provide a square from points generated.
        ///     *     *                 *
        ///     *     *                 *
        ///     *     *                 *      
        ///     *     *                 *
        ///     *  y2 ******************* y3
        ///     *     x4               y4
        ///     *
        ///     ******************************
        /// </summary>
        /// <param name="currentMiningSetting"></param>
        /// <param name="pocShareIv"></param>
        /// <param name="previousBlockFinalTransactionHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="blockDifficulty"></param>
        /// <returns></returns>
        public static uint GetNonceIvComplexArithmetic(ClassMiningPoWaCSettingObject currentMiningSetting, byte[] pocShareIv, byte[] previousBlockFinalTransactionHash, long blockHeight, BigInteger blockDifficulty)
        {
            byte[] blockDifficultyBytes = blockDifficulty.ToByteArray();
            byte[] blockHeightBytes = BitConverter.GetBytes(blockHeight);

            byte[] newNonce = new byte[sizeof(uint)];
            bool newNonceGenerated = false;
            int totalRetry = 0;

            using (ClassSha3512DigestDisposable sha3512Digest = new ClassSha3512DigestDisposable())
            {
                while (totalRetry < currentMiningSetting.PocShareNonceMaxSquareRetry)
                {
                    byte[] pocShareWorkToDoBytes = new byte[pocShareIv.Length + previousBlockFinalTransactionHash.Length + blockHeightBytes.Length + blockDifficultyBytes.Length];

                    // Merge some block informations and the previous work done on the nonce.
                    Array.Copy(pocShareIv, 0, pocShareWorkToDoBytes, 0, pocShareIv.Length);
                    Array.Copy(blockDifficultyBytes, 0, pocShareWorkToDoBytes, pocShareIv.Length, blockDifficultyBytes.Length);
                    Array.Copy(blockHeightBytes, 0, pocShareWorkToDoBytes, pocShareIv.Length + blockDifficultyBytes.Length, blockHeightBytes.Length);
                    Array.Copy(previousBlockFinalTransactionHash, 0, pocShareWorkToDoBytes, pocShareIv.Length + blockDifficultyBytes.Length + blockHeightBytes.Length, previousBlockFinalTransactionHash.Length);

                    // Compute it and generate a data of 64 bytes.
                    pocShareWorkToDoBytes = sha3512Digest.Compute(pocShareWorkToDoBytes);
                    sha3512Digest.Reset();

                    for (int i = 0; i < pocShareWorkToDoBytes.Length; i += 8)
                    {
                        byte[] x1Bytes = new byte[2];
                        byte[] y1Bytes = new byte[2];
                        Array.Copy(pocShareWorkToDoBytes, i, x1Bytes, 0, x1Bytes.Length);
                        Array.Copy(pocShareWorkToDoBytes, i, y1Bytes, 0, y1Bytes.Length);
                        Array.Reverse(y1Bytes, 0, y1Bytes.Length);
                        ushort x1 = BitConverter.ToUInt16(x1Bytes, 0);
                        ushort y1 = BitConverter.ToUInt16(y1Bytes, 0);


                        byte[] x2Bytes = new byte[2];
                        byte[] y2Bytes = new byte[2];
                        Array.Copy(pocShareWorkToDoBytes, i + 2, x2Bytes, 0, x2Bytes.Length);
                        Array.Copy(pocShareWorkToDoBytes, i + 2, y2Bytes, 0, y2Bytes.Length);
                        Array.Reverse(y2Bytes, 0, y2Bytes.Length);
                        ushort x2 = BitConverter.ToUInt16(x2Bytes, 0);
                        ushort y2 = BitConverter.ToUInt16(y2Bytes, 0);


                        byte[] x3Bytes = new byte[2];
                        byte[] y3Bytes = new byte[2];
                        Array.Copy(pocShareWorkToDoBytes, i + 4, x3Bytes, 0, x3Bytes.Length);
                        Array.Copy(pocShareWorkToDoBytes, i + 4, y3Bytes, 0, y3Bytes.Length);
                        Array.Reverse(y3Bytes, 0, y3Bytes.Length);
                        ushort x3 = BitConverter.ToUInt16(x3Bytes, 0);
                        ushort y3 = BitConverter.ToUInt16(y3Bytes, 0);


                        byte[] x4Bytes = new byte[2];
                        byte[] y4Bytes = new byte[2];
                        Array.Copy(pocShareWorkToDoBytes, i + 6, x4Bytes, 0, x4Bytes.Length);
                        Array.Copy(pocShareWorkToDoBytes, i + 6, y4Bytes, 0, y4Bytes.Length);
                        Array.Reverse(y4Bytes, 0, y4Bytes.Length);
                        ushort x4 = BitConverter.ToUInt16(x4Bytes, 0);
                        ushort y4 = BitConverter.ToUInt16(y4Bytes, 0);

                        MathPoint squareP1 = new MathPoint(x1, y1);
                        MathPoint squareP2 = new MathPoint(x2, y2);
                        MathPoint squareP3 = new MathPoint(x3, y3);
                        MathPoint squareP4 = new MathPoint(x4, y4);


                        if (IsASquare(squareP1, squareP2, squareP3, squareP4))
                        {
                            newNonce[0] = (byte)(x1Bytes[0] + y1Bytes[1]);
                            newNonce[1] = (byte)(x2Bytes[0] + y2Bytes[1]);
                            newNonce[2] = (byte)(x3Bytes[0] + y3Bytes[1]);
                            newNonce[3] = (byte)(x4Bytes[0] + y4Bytes[1]);
                            newNonceGenerated = true;
                            break;
                        }
                    }

                    if (newNonceGenerated)
                        break;

                    // On this case we do another sha3-512 computation.
                    pocShareIv = sha3512Digest.Compute(pocShareIv);
                    sha3512Digest.Reset();

                    totalRetry++;
                }


                if (!newNonceGenerated)
                {
                    for (int i = 0; i < currentMiningSetting.PocShareNonceNoSquareFoundShaRounds; i++)
                    {
                        pocShareIv = sha3512Digest.Compute(pocShareIv);
                        sha3512Digest.Reset();
                    }

                    Array.Resize(ref pocShareIv, newNonce.Length);
                    Array.Copy(pocShareIv, 0, newNonce, 0, newNonce.Length);
                }
            }

            return BitConverter.ToUInt32(newNonce, 0);
        }

        /// <summary>
        /// Update the random poc data timestamp and the block height target.
        /// </summary>
        /// <param name="currentMiningSetting"></param>
        /// <param name="pocRandomData"></param>
        /// <param name="blockHeight"></param>
        /// <param name="timestampShare"></param>
        /// <returns></returns>
        public static byte[] UpdateRandomPocDataTimestampAndBlockHeightTarget(ClassMiningPoWaCSettingObject currentMiningSetting, byte[] pocRandomData, long blockHeight, long nonce, out long timestampShare)
        {
            timestampShare = TaskManager.TaskManager.CurrentTimestampSecond;

            Buffer.BlockCopy(BitConverter.GetBytes(timestampShare), 0, pocRandomData, currentMiningSetting.RandomDataShareTimestampSize, currentMiningSetting.RandomDataShareTimestampSize);
            Buffer.BlockCopy(BitConverter.GetBytes(blockHeight), 0, pocRandomData, currentMiningSetting.RandomDataShareNumberSize + currentMiningSetting.RandomDataShareTimestampSize + currentMiningSetting.RandomDataShareChecksum + currentMiningSetting.WalletAddressDataSize, currentMiningSetting.RandomDataShareBlockHeightSize);
            Buffer.BlockCopy(BitConverter.GetBytes(nonce), 0, pocRandomData, currentMiningSetting.RandomDataShareNumberSize + currentMiningSetting.RandomDataShareTimestampSize + currentMiningSetting.RandomDataShareChecksum + currentMiningSetting.WalletAddressDataSize + currentMiningSetting.RandomDataShareBlockHeightSize, currentMiningSetting.RandomDataShareNumberSize);

            return pocRandomData;
        }

        /// <summary>
        /// Generate two numbers from the Poc Random Data.
        /// </summary>
        /// <param name="currentMiningSetting"></param>
        /// <param name="pocRandomData"></param>
        /// <param name="timestampShare"></param>
        /// <param name="blockHeightShare"></param>
        /// <param name="numberOne"></param>
        /// <param name="numberTwo"></param>
        /// <returns></returns>
        public static void GetCompatibilityDataFromPocRandomData(ClassMiningPoWaCSettingObject currentMiningSetting, byte[] pocRandomData, out long timestampShare, out long blockHeightShare, out int numberOne, out int numberTwo, out long nonce)
        {
            byte[] numberOneBytes = new byte[currentMiningSetting.RandomDataShareNumberSize / 2];
            byte[] numberTwoBytes = new byte[currentMiningSetting.RandomDataShareNumberSize / 2];
            byte[] timestampBytes = new byte[currentMiningSetting.RandomDataShareTimestampSize];
            byte[] blockHeightBytes = new byte[currentMiningSetting.RandomDataShareBlockHeightSize];
            byte[] nonceBytes = new byte[currentMiningSetting.RandomDataShareNumberSize];

            Array.Copy(pocRandomData, 0, numberOneBytes, 0, currentMiningSetting.RandomDataShareNumberSize / 2);
            Array.Copy(pocRandomData, currentMiningSetting.RandomDataShareNumberSize / 2, numberTwoBytes, 0, currentMiningSetting.RandomDataShareNumberSize / 2);
            Array.Copy(pocRandomData, currentMiningSetting.RandomDataShareTimestampSize, timestampBytes, 0, currentMiningSetting.RandomDataShareTimestampSize);
            Array.Copy(pocRandomData, currentMiningSetting.RandomDataShareNumberSize + currentMiningSetting.RandomDataShareTimestampSize + currentMiningSetting.RandomDataShareChecksum + currentMiningSetting.WalletAddressDataSize, blockHeightBytes, 0, currentMiningSetting.RandomDataShareBlockHeightSize);
            Array.Copy(pocRandomData, currentMiningSetting.RandomDataShareNumberSize + currentMiningSetting.RandomDataShareTimestampSize + currentMiningSetting.RandomDataShareChecksum + currentMiningSetting.WalletAddressDataSize + currentMiningSetting.RandomDataShareBlockHeightSize, nonceBytes, 0, currentMiningSetting.RandomDataShareNumberSize);

            numberOne = BitConverter.ToInt32(numberOneBytes, 0);
            numberTwo = BitConverter.ToInt32(numberTwoBytes, 0);
            timestampShare = BitConverter.ToInt64(timestampBytes, 0);
            blockHeightShare = BitConverter.ToInt64(blockHeightBytes, 0);
            nonce = BitConverter.ToInt64(nonceBytes, 0);
        }

        /// <summary>
        /// Retrieve back the wallet address decoded from the poc random data.
        /// </summary>
        /// <param name="currentMiningSetting"></param>
        /// <param name="pocRandomData"></param>
        /// <returns></returns>
        private static byte[] GetWalletAddressDecodedFromPocRandomData(ClassMiningPoWaCSettingObject currentMiningSetting, byte[] pocRandomData)
        {
            byte[] walletAddressDecoded = new byte[currentMiningSetting.WalletAddressDataSize];

            Array.Copy(pocRandomData, currentMiningSetting.RandomDataShareNumberSize + currentMiningSetting.RandomDataShareTimestampSize + currentMiningSetting.RandomDataShareChecksum, walletAddressDecoded, 0, currentMiningSetting.WalletAddressDataSize);

            return walletAddressDecoded;
        }

        /// <summary>
        /// Check the proof of compatibility.
        /// </summary>
        /// <param name="currentMiningSetting"></param>
        /// <param name="numberOne"></param>
        /// <param name="numberTwo"></param>
        /// <param name="previousBlockTransactionCount"></param>
        /// <param name="pocTxCount"></param>
        /// <returns></returns>
        public static bool CheckPoc(ClassMiningPoWaCSettingObject currentMiningSetting, int numberOne, int numberTwo, int previousBlockTransactionCount, out int pocTxCount)
        {

            if (numberTwo < 0 || numberTwo > previousBlockTransactionCount || numberOne < 0 || numberOne > previousBlockTransactionCount)
            {
                pocTxCount = 0;
                return false;
            }

            foreach (var mathOperator in currentMiningSetting.MathOperatorList)
            {
                switch (mathOperator)
                {
                    case MathOperatorPlus:
                        {
                            pocTxCount = numberOne + numberTwo;
                            if (pocTxCount == previousBlockTransactionCount)
                                return true;
                        }
                        break;
                    case MathOperatorMultiplicate:
                        {
                            pocTxCount = numberOne * numberTwo;
                            if (pocTxCount == previousBlockTransactionCount)
                                return true;
                        }
                        break;
                    case MathOperatorModulo:
                        {
                            if (numberTwo > 0)
                            {
                                pocTxCount = numberOne % numberTwo;
                                if (pocTxCount == previousBlockTransactionCount)
                                    return true;
                            }

                            if (numberOne > 0)
                            {
                                pocTxCount = numberTwo % numberOne;
                                if (pocTxCount == previousBlockTransactionCount)
                                    return true;
                            }
                        }
                        break;
                    case MathOperatorMinus:
                        {
                            pocTxCount = numberOne - numberTwo;
                            if (pocTxCount == previousBlockTransactionCount)
                                return true;

                            pocTxCount = numberTwo - numberOne;
                            if (pocTxCount == previousBlockTransactionCount)
                                return true;
                        }
                        break;
                }
            }
            pocTxCount = -1;

            return false;
        }

        /// <summary>
        /// Calculate the job difficulty share from the poc share data encrypted.
        /// </summary>
        /// <param name="powShareDataBytes"></param>
        /// <param name="blockDifficulty"></param>
        /// <returns></returns>
        private static BigInteger CalculateDifficultyShare(byte[] powShareDataBytes, BigInteger blockDifficulty)
        {
            using (ClassSha3512DigestDisposable sha3512Digest = new ClassSha3512DigestDisposable())
                return BigInteger.Divide(BigInteger.Divide(ShaPowCalculation, blockDifficulty), BigInteger.Divide(new BigInteger(sha3512Digest.Compute(powShareDataBytes)), blockDifficulty));
        }

        /// <summary>
        /// Compare two share data equality.
        /// </summary>
        /// <param name="pocShareObject1"></param>
        /// <param name="pocShareObject2"></param>
        /// <returns></returns>
        public static bool ComparePoWaCShare(ClassMiningPoWaCShareObject pocShareObject1, ClassMiningPoWaCShareObject pocShareObject2)
        {
            if (pocShareObject2 == null || pocShareObject1 == null)
                return false;

            return pocShareObject1.BlockHeight == pocShareObject2.BlockHeight &&
                pocShareObject1.Nonce == pocShareObject2.Nonce &&
                pocShareObject1.NonceComputedHexString == pocShareObject2.NonceComputedHexString &&
                pocShareObject1.PoWaCShare == pocShareObject2.PoWaCShare &&
                pocShareObject1.PoWaCShareDifficulty == pocShareObject2.PoWaCShareDifficulty &&
                pocShareObject1.Timestamp == pocShareObject2.Timestamp &&
                pocShareObject1.WalletAddress == pocShareObject2.WalletAddress &&
                pocShareObject1.BlockHash == pocShareObject2.BlockHash ? true : false;
        }

        /// <summary>
        /// Check a mining PoWaC setting.
        /// </summary>
        /// <param name="miningPoWaCSettingObject"></param>
        /// <returns></returns>
        public static bool CheckMiningPoWaCSetting(ClassMiningPoWaCSettingObject miningPoWaCSettingObject)
        {
            if (miningPoWaCSettingObject == null)
                return false;

            if (miningPoWaCSettingObject.PowRoundAesShare <= 0 ||
                miningPoWaCSettingObject.PocRoundShaNonce <= 0 ||
                miningPoWaCSettingObject.PocShareNonceMin <= 0 ||
                miningPoWaCSettingObject.PocShareNonceMax <= 0 ||
                miningPoWaCSettingObject.RandomDataShareNumberSize <= 0 ||
                miningPoWaCSettingObject.BlockHeightStart <= 0 ||
                miningPoWaCSettingObject.RandomDataShareTimestampSize <= 0 ||
                miningPoWaCSettingObject.RandomDataShareBlockHeightSize <= 0 ||
                miningPoWaCSettingObject.RandomDataShareChecksum <= 0 ||
                miningPoWaCSettingObject.WalletAddressDataSize <= 0 ||
                miningPoWaCSettingObject.PocShareNonceMaxSquareRetry <= 0 ||
                miningPoWaCSettingObject.PocShareNonceNoSquareFoundShaRounds <= 0 ||
                miningPoWaCSettingObject.MathOperatorList == null ||
                miningPoWaCSettingObject.MiningIntructionsList == null ||
                miningPoWaCSettingObject.MiningSettingContentDevPublicKey.IsNullOrEmpty(false, out _) ||
                miningPoWaCSettingObject.MiningSettingContentHash.IsNullOrEmpty(false, out _) ||
                miningPoWaCSettingObject.MiningSettingContentHashSignature.IsNullOrEmpty(false, out _))
            {
                return false;
            }

            if (miningPoWaCSettingObject.MathOperatorList.Count == 0 ||
                miningPoWaCSettingObject.RandomDataShareSize != (miningPoWaCSettingObject.RandomDataShareNumberSize + miningPoWaCSettingObject.RandomDataShareTimestampSize + miningPoWaCSettingObject.RandomDataShareBlockHeightSize + miningPoWaCSettingObject.RandomDataShareChecksum + miningPoWaCSettingObject.WalletAddressDataSize + miningPoWaCSettingObject.RandomDataShareNumberSize) ||
                miningPoWaCSettingObject.ShareHexStringSize != ClassAes.EncryptionKeySize + (32 * miningPoWaCSettingObject.PowRoundAesShare))
                return false;

            if (miningPoWaCSettingObject.MiningIntructionsList.Count < BlockchainSetting.MiningMinInstructionsCount)
                return false;

            if (!miningPoWaCSettingObject.MiningIntructionsList.Contains(ClassMiningPoWaCEnumInstructions.DO_NONCE_IV) || !miningPoWaCSettingObject.MiningIntructionsList.Contains(ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_ITERATIONS) || !miningPoWaCSettingObject.MiningIntructionsList.Contains(ClassMiningPoWaCEnumInstructions.DO_ENCRYPTED_POC_SHARE))
                return false;

            // First mandatory instruction.
            if (miningPoWaCSettingObject.MiningIntructionsList[0] != ClassMiningPoWaCEnumInstructions.DO_NONCE_IV)
                return false;

            // Just behind the last mandatory instruction.
            if (miningPoWaCSettingObject.MiningIntructionsList[miningPoWaCSettingObject.MiningIntructionsList.Count - 2] != ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_ITERATIONS)
                return false;

            // Latest mandatory instruction.
            if (miningPoWaCSettingObject.MiningIntructionsList[miningPoWaCSettingObject.MiningIntructionsList.Count - 1] != ClassMiningPoWaCEnumInstructions.DO_ENCRYPTED_POC_SHARE)
                return false;

            if (miningPoWaCSettingObject.BlockHeightStart == BlockchainSetting.GenesisBlockHeight)
            {
                // Default mining setting.
                if (miningPoWaCSettingObject.MiningSettingContentDevPublicKey != BlockchainSetting.DefaultWalletAddressDevPublicKey)
                    return false;
            }

            ClassMiningPoWaCSettingObject miningPoWaCSettingObjectCopy = new ClassMiningPoWaCSettingObject(false)
            {
                BlockHeightStart = miningPoWaCSettingObject.BlockHeightStart,
                MathOperatorList = miningPoWaCSettingObject.MathOperatorList,
                MiningIntructionsList = miningPoWaCSettingObject.MiningIntructionsList,
                RandomDataShareTimestampSize = miningPoWaCSettingObject.RandomDataShareTimestampSize,
                RandomDataShareBlockHeightSize = miningPoWaCSettingObject.RandomDataShareBlockHeightSize,
                WalletAddressDataSize = miningPoWaCSettingObject.WalletAddressDataSize,
                MiningSettingContentDevPublicKey = miningPoWaCSettingObject.MiningSettingContentDevPublicKey,
                RandomDataShareSize = miningPoWaCSettingObject.RandomDataShareSize,
                RandomDataShareChecksum = miningPoWaCSettingObject.RandomDataShareChecksum,
                MiningSettingContentHashSignature = null,
                MiningSettingContentHash = null,
                RandomDataShareNumberSize = miningPoWaCSettingObject.RandomDataShareNumberSize,
                PocShareNonceMaxSquareRetry = miningPoWaCSettingObject.PocShareNonceMaxSquareRetry,
                PocShareNonceMax = miningPoWaCSettingObject.PocShareNonceMax,
                MiningSettingTimestamp = miningPoWaCSettingObject.MiningSettingTimestamp,
                PocShareNonceMin = miningPoWaCSettingObject.PocShareNonceMin,
                ShareHexStringSize = miningPoWaCSettingObject.ShareHexStringSize,
                PowRoundAesShare = miningPoWaCSettingObject.PowRoundAesShare,
                PocShareNonceNoSquareFoundShaRounds = miningPoWaCSettingObject.PocShareNonceNoSquareFoundShaRounds,
                PocShareNonceIvIteration = miningPoWaCSettingObject.PocShareNonceIvIteration,
                ShareHexByteArraySize = miningPoWaCSettingObject.ShareHexByteArraySize,
                PocRoundShaNonce = miningPoWaCSettingObject.PocRoundShaNonce,

            };

            if (ClassUtility.GenerateSha3512FromString(ClassUtility.SerializeData(miningPoWaCSettingObjectCopy)) != miningPoWaCSettingObject.MiningSettingContentHash)
                return false;

            // Invalid signature.
            if (!ClassWalletUtility.WalletCheckSignature(miningPoWaCSettingObject.MiningSettingContentHash, miningPoWaCSettingObject.MiningSettingContentHashSignature, miningPoWaCSettingObject.MiningSettingContentDevPublicKey))
                return false;

            return true;
        }

        /// <summary>
        /// Do the encryption process of the PoC share data.
        /// </summary>
        /// <param name="currentMiningSetting"></param>
        /// <param name="pocShareData"></param>
        /// <param name="previousFinalBlockTransactionHashKey"></param>
        /// <param name="pocShareIv"></param>
        /// <returns></returns>
        private static byte[] DoEncryptionPocShare(ClassMiningPoWaCSettingObject currentMiningSetting, byte[] pocShareData, byte[] previousFinalBlockTransactionHashKey, byte[] pocShareIv)
        {
            try
            {
                using (RijndaelManaged aesObject = new RijndaelManaged())
                {
                    aesObject.KeySize = ClassAes.EncryptionKeySize;
                    aesObject.BlockSize = ClassAes.EncryptionBlockSize;
                    aesObject.Key = previousFinalBlockTransactionHashKey;
                    aesObject.IV = pocShareIv;
                    aesObject.Mode = CipherMode.CFB;
                    aesObject.Padding = PaddingMode.None;
                    using (ICryptoTransform encryptCryptoTransform = aesObject.CreateEncryptor(previousFinalBlockTransactionHashKey, pocShareIv))
                    {
                        for (int i = 0; i < currentMiningSetting.PowRoundAesShare; i++)
                        {
                            byte[] paddedPocShareData = ClassUtility.DoPadding(pocShareData);
                            pocShareData = encryptCryptoTransform.TransformFinalBlock(paddedPocShareData, 0, paddedPocShareData.Length);
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            return pocShareData;
        }

        /// <summary>
        /// Do the encryption process of the PoC share data.
        /// </summary>
        /// <param name="currentMiningSetting"></param>
        /// <param name="pocShareData"></param>
        /// <param name="previousFinalBlockTransactionHashKey"></param>
        /// <param name="pocShareIv"></param>
        /// <returns></returns>
        private static byte[] DoDecryptionPocShare(ClassMiningPoWaCSettingObject currentMiningSetting, byte[] pocShareData, byte[] previousFinalBlockTransactionHashKey, byte[] pocShareIv, out bool result)
        {
            // Successfully by default.
            result = true;
            try
            {
                using (RijndaelManaged aesObject = new RijndaelManaged())
                {
                    aesObject.KeySize = ClassAes.EncryptionKeySize;
                    aesObject.BlockSize = ClassAes.EncryptionBlockSize;
                    aesObject.Key = previousFinalBlockTransactionHashKey;
                    aesObject.IV = pocShareIv;
                    aesObject.Mode = CipherMode.CFB;
                    aesObject.Padding = PaddingMode.None;
                    using (ICryptoTransform decryptCryptoTransform = aesObject.CreateDecryptor(previousFinalBlockTransactionHashKey, pocShareIv))
                    {
                        for (int i = 0; i < currentMiningSetting.PowRoundAesShare; i++)
                        {
                            pocShareData = decryptCryptoTransform.TransformFinalBlock(pocShareData, 0, pocShareData.Length);
                            pocShareData = ClassUtility.UndoPadding(pocShareData);
                        }
                    }
                }
            }
            catch
            {
                result = false;
                pocShareData = null;
            }

            return pocShareData;
        }

        #region Math functions

        /// <summary>
        /// Determine if points provided do a valid square.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <returns></returns>
        private static bool IsASquare(MathPoint p1, MathPoint p2, MathPoint p3, MathPoint p4)
        {
            return (IsACorner(p1, p2, p3) && IsACorner(p4, p2, p3))
                || (IsACorner(p1, p2, p4) && IsACorner(p3, p2, p4))
                || (IsACorner(p1, p3, p4) && IsACorner(p2, p3, p4)) ? true : false;
        }

        public static bool IsACorner(MathPoint p1, MathPoint p2, MathPoint p3)
        {
            //pivot point is p1
            return Math.Abs(p2.Y - p1.Y) == Math.Abs(p3.X - p1.X)
                   && Math.Abs(p2.X - p1.X) == Math.Abs(p3.Y - p1.Y);
        }

        #endregion

    }
}