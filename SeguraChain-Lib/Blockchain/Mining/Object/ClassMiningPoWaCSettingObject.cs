
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

			MiningSettingTimestamp = 1683539318;
			MiningSettingContentHash = "3E6E0006FA0FFB3A9EA481DC704D9FEFDE12F83A12915E11D9DE4A0CB598165EEDADFCF10A72733893DC75BAF064598EA74F27571E2622E257537D9B36CF95CD";
			MiningSettingContentHashSignature = "MIGUAkgAtb5AXJwzvPyRf0Gi94BH4s5f9nGx8za8pnzlZ58PDTp7d+OjecJN4ztA0Bl7MYRnnV+PT6wVgmf6GVnV2FiPMD6tA0c6XAECSAEh0xV06zrJCRwAKFJn/KUee7m9xnxRG//3XbiB3zzjB86Uts6AKnWLCIxIGIi6YMD30tmX9l8jwzmmSSyDl/IwoZlY3aq9VA==";
			MiningSettingContentDevPublicKey = "YAdhAT8r4C9jHZwVyAZh4PqKb8a1HECuLDTY5Aeusn65dcWggCnA1CkW1UC3AdCJisb123uCVfSRVTodgWwMtxAyTycZ2brWEzUVXsXkwEX6eUHiQWrvtSBjYodEk4jNx1xK9LKB6feFUmLwsVn7yd24xVRsTudt6Gu3fPWTAipgUy9eyR35LESZxCdLwqcHy8eEB7aBXSZxaKgbsFgXv3Uyk2q";
        }
    }
}
