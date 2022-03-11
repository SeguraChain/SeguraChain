using System.Collections.Generic;
using SeguraChain_Lib.Algorithm;
using SeguraChain_Lib.Blockchain.Mining.Enum;
using SeguraChain_Lib.Blockchain.Setting;

namespace SeguraChain_Lib.Blockchain.Mining.Object
{
    /// <summary>
    /// This setting can be updated with a sovereign update.
    /// </summary>
    public class ClassMiningPoWaCSettingObject
    {
        /// <summary>
        /// Block height start.
        /// </summary>
        public long BlockHeightStart;

        /// <summary>
        /// About the encryption share.
        /// </summary>
        public int PowRoundAesShare;

        /// <summary>
        /// About the nonce.
        /// </summary>
        public int PocRoundShaNonce;
        public long PocShareNonceMin;
        public long PocShareNonceMax;
        public int PocShareNonceMaxSquareRetry;
        public int PocShareNonceNoSquareFoundShaRounds;
        public int PocShareNonceIvIteration;

        /// <summary>
        /// About the random PoC data.
        /// </summary>
        public int RandomDataShareNumberSize;
        public int RandomDataShareTimestampSize;
        public int RandomDataShareBlockHeightSize;
        public int RandomDataShareChecksum;
        public int WalletAddressDataSize;
        public int RandomDataShareSize;

        /// <summary>
        /// About the share encrypted.
        /// </summary>
        public int ShareHexStringSize;
        public int ShareHexByteArraySize;

        /// <summary>
        ///  Accepted math operators.
        /// </summary>
        public List<string> MathOperatorList;

        /// <summary>
        /// Every mining instruction asked.
        /// </summary>
        public List<ClassMiningPoWaCEnumInstructions> MiningIntructionsList;

        public long MiningSettingTimestamp;
        public string MiningSettingContentHash;
        public string MiningSettingContentHashSignature;
        public string MiningSettingContentDevPublicKey;

        /// <summary>
        /// Set default value if true.
        /// </summary>
        /// <param name="setDefaultValue"></param>
        public ClassMiningPoWaCSettingObject(bool setDefaultValue)
        {
            if (setDefaultValue)
                SetDefaultValue(); 
        }

        /// <summary>
        /// Set default value.
        /// </summary>
        public void SetDefaultValue()
        {
            BlockHeightStart = BlockchainSetting.GenesisBlockHeight;

            PowRoundAesShare = 3;
            PocRoundShaNonce = 48;
            PocShareNonceMin = 1;
            PocShareNonceMax = uint.MaxValue;
            PocShareNonceMaxSquareRetry = 10;
            PocShareNonceNoSquareFoundShaRounds = 20;
            PocShareNonceIvIteration = 10;

            RandomDataShareNumberSize = 8;
            RandomDataShareTimestampSize = 8;
            RandomDataShareBlockHeightSize = 8;
            RandomDataShareChecksum = 32;
            WalletAddressDataSize = 65;
            RandomDataShareSize = RandomDataShareNumberSize + RandomDataShareTimestampSize + RandomDataShareBlockHeightSize + RandomDataShareChecksum + WalletAddressDataSize + RandomDataShareNumberSize;

            ShareHexStringSize = ClassAes.EncryptionKeySize + (32 * PowRoundAesShare);
            ShareHexByteArraySize = ShareHexStringSize / 2;

            MathOperatorList = new List<string>  {
                "+",
                "*",
                "%",
                "-"
            };

            MiningIntructionsList = new List<ClassMiningPoWaCEnumInstructions>()
            {
                ClassMiningPoWaCEnumInstructions.DO_NONCE_IV,
                ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_XOR,
                ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_EASY_SQUARE_MATH,
                ClassMiningPoWaCEnumInstructions.DO_NONCE_IV,
                ClassMiningPoWaCEnumInstructions.DO_LZ4_COMPRESS_NONCE_IV,
                ClassMiningPoWaCEnumInstructions.DO_LZ4_COMPRESS_NONCE_IV,
                ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_XOR,
                ClassMiningPoWaCEnumInstructions.DO_NONCE_IV_ITERATIONS,
                ClassMiningPoWaCEnumInstructions.DO_ENCRYPTED_POC_SHARE,
            };

			MiningSettingTimestamp = 1646998161;
			MiningSettingContentHash = "069BF564A8AA9EECB5A8016B86F9739412E74A52F1ECEC620B023E864977905A1702BAF88EF87E5F0D20B74333A628C426F87ACE28F3325ABE795BB1E4B8DF8B";
			MiningSettingContentHashSignature = "MIGUAkgD3TZbz+/RPnqZd64zEnof6up6Q4PHKOR3fn4NNybSIW5jh6j02OCf+jAQZr+xn+u1TfPh+6iIkleaM6Miu9qroYaWIq5I89oCSAM+r4ast+z9YH4wFgmOnCmmSPd9pIua2GsPwUzpRPdLeJfTVV8IWqv96dLisJ7M6xS2ZXkRCXdaWseQKqQvWWrq9akAMwcDtQ==";
			MiningSettingContentDevPublicKey = "YMFv37pab9rX9Us9WuXE1VWN6rpsZn6Hwg6AKB2RHQvbFxWj8Tbs5YvzbafuqD5CEQZr7NWZC5q6qLrZhCV2pDv1JqyW6TGTMiQZcxV7Nmp9s3uFoZyM9sYJgkFR7boPdECESj2biaC3uDaV2QvNTodVYdFoLbeVgXHAehxpeVzARKZbZZJCAShubP3t8HdC7XZzpeCfd6813YtzwD4uuST9sfw";
        }
    }
}
