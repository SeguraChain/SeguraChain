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

			MiningSettingTimestamp = 1653493417;
			MiningSettingContentHash = "4D443AA4740A113F5D45FB86D81E1C23C093FF4F2E8D632E2FB4F2ECB144AD33FAD7CA51D2D84BAA374162E457A022950817503ADBF387609F07099722A55B69";
			MiningSettingContentHashSignature = "MIGUAkgC3J+lUeWBf9TmaTsFMSWZkhu3IS176Tyte0xjgwtj+ppXY+tWed24RZv+4BYmkbNVp3SkM6Iz4x4FcAiYoby28hXm6bchdGACSAHXFq34S0/FzQk6QdDi3CKvN8IFW+XEFZwaDslW3pQ5LZq6Uq77sf8zueJ0fPUe4DUv8SgkMpK3HFxxNNJvTTGaJVnmPLYmtg==";
			MiningSettingContentDevPublicKey = "YMBT7sXubw9ksQfJNYWxZvqAzyNob3yM3HvoReNBvJ8nYK7HeVAKVRfaqi1douAkmsPC3Tc1gFZjXrCzc2iGSn7jCtiiaGMm3n9k2eMsx3y2x4tQQCyfM876q83H8EdC9y9sL7WUGqZaHxsZnwNJ9TrFqvTj58y4FnaSxZAu3aiPXvjB7rYJF9ckX8BKTAJZ9reMrZ92Wun1Y34eG2wkK8dzoph";
        }
    }
}
