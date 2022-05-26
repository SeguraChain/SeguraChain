using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeguraChain_Lib.Blockchain.Block.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using SeguraChain_Lib.Blockchain.Database;
using SeguraChain_Lib.Blockchain.Mining.Enum;
using SeguraChain_Lib.Blockchain.Mining.Function;
using SeguraChain_Lib.Blockchain.Mining.Object;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Blockchain.Stats.Function;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Utility;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Utility;

namespace SeguraChain_Lib.Blockchain.Block.Function
{

    public class ClassBlockUtility
    {
        #region About block hash, block final transaction hash.

        /// <summary>
        /// Generate block hash.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="blockDifficulty"></param>
        /// <param name="blockCountTransaction"></param>
        /// <param name="blockFinalTransactionHash"></param>
        /// <param name="previousWalletAddressWinner"></param>
        /// <returns>Block Hash HEX = [block height hash * block difficulty * block count transaction]</returns>
        public static string GenerateBlockHash(long blockHeight, BigInteger blockDifficulty, int blockCountTransaction, string blockFinalTransactionHash, string previousWalletAddressWinner)
        {
            byte[] blockHeightBytes = new byte[BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash];
            byte[] blockDifficultyBytes = new byte[BlockchainSetting.BlockDifficultyByteArrayLengthOnBlockHash];
            byte[] blockCountTransactionBytes = new byte[BlockchainSetting.BlockCountTransactionByteArrayLengthOnBlockHash];
            byte[] blockFinalTransactionHashBytes = new byte[BlockchainSetting.BlockFinalTransactionHashByteArrayLengthOnBlockHash];
            byte[] previousWalletAddressWinnerBytes = new byte[BlockchainSetting.WalletAddressByteArrayLength];

            // Copy informations.
            Array.Copy(BitConverter.GetBytes(blockHeight), 0, blockHeightBytes, 0, blockHeightBytes.Length);
            Array.Copy(BitConverter.GetBytes((double)blockDifficulty), 0, blockDifficultyBytes, 0, blockDifficultyBytes.Length);
            Array.Copy(BitConverter.GetBytes(blockCountTransaction), 0, blockCountTransactionBytes, 0, blockCountTransactionBytes.Length);
            Array.Copy(ClassUtility.GetByteArrayFromHexString(blockFinalTransactionHash), 0, blockFinalTransactionHashBytes, 0, blockFinalTransactionHashBytes.Length);
            Array.Copy(ClassBase58.DecodeWithCheckSum(previousWalletAddressWinner, true), 0, previousWalletAddressWinnerBytes, 0, previousWalletAddressWinnerBytes.Length);

            byte[] blockHashBytes = new byte[BlockchainSetting.BlockHashByteArraySize];

            // Merge informations.
            Array.Copy(blockHeightBytes, 0, blockHashBytes, 0, blockHeightBytes.Length);
            Array.Copy(blockDifficultyBytes, 0, blockHashBytes, blockHeightBytes.Length, blockDifficultyBytes.Length);
            Array.Copy(blockCountTransactionBytes, 0, blockHashBytes, blockHeightBytes.Length + blockDifficultyBytes.Length, blockCountTransactionBytes.Length);
            Array.Copy(blockFinalTransactionHashBytes, 0, blockHashBytes, blockHeightBytes.Length + blockDifficultyBytes.Length + blockCountTransactionBytes.Length, blockFinalTransactionHashBytes.Length);
            Array.Copy(previousWalletAddressWinnerBytes, 0, blockHashBytes, blockHeightBytes.Length + blockDifficultyBytes.Length + blockCountTransactionBytes.Length + blockFinalTransactionHashBytes.Length, previousWalletAddressWinnerBytes.Length);

            string blockHash = ClassUtility.GetHexStringFromByteArray(blockHashBytes).ToLower();

            // Clear.
            Array.Clear(blockHashBytes, 0, blockHashBytes.Length);
            Array.Clear(blockDifficultyBytes, 0, blockDifficultyBytes.Length);
            Array.Clear(blockCountTransactionBytes, 0, blockCountTransactionBytes.Length);
            Array.Clear(blockFinalTransactionHashBytes, 0, blockFinalTransactionHashBytes.Length);
            Array.Clear(previousWalletAddressWinnerBytes, 0, previousWalletAddressWinnerBytes.Length);

            return blockHash;
        }

        /// <summary>
        /// Check block hash format and his content.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="blockHeight"></param>
        /// <param name="blockDifficulty"></param>
        /// <param name="previousBlockTransactionCount"></param>
        /// <param name="blockTransactionHash"></param>
        /// <returns></returns>
        public static ClassBlockEnumCheckStatus CheckBlockHash(string blockHash, long blockHeight, BigInteger blockDifficulty, int previousBlockTransactionCount, string blockTransactionHash)
        {
            if (blockHash.Length != BlockchainSetting.BlockHashHexSize)
                return ClassBlockEnumCheckStatus.INVALID_BLOCK_HASH_LENGTH;

            if (!ClassUtility.CheckHexStringFormat(blockHash))
                return ClassBlockEnumCheckStatus.INVALID_BLOCK_HASH_FORMAT;

            try
            {
                byte[] blockHashBytes = ClassUtility.GetByteArrayFromHexString(blockHash);

                if (blockHashBytes.Length != BlockchainSetting.BlockHashByteArraySize)
                    return ClassBlockEnumCheckStatus.INVALID_BLOCK_HASH_LENGTH;

                byte[] blockHeightBytes = new byte[BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash];
                byte[] blockDifficultyBytes = new byte[BlockchainSetting.BlockDifficultyByteArrayLengthOnBlockHash];
                byte[] blockCountTransactionBytes = new byte[BlockchainSetting.BlockCountTransactionByteArrayLengthOnBlockHash];
                byte[] blockFinalTransactionHashBytes = new byte[BlockchainSetting.BlockFinalTransactionHashByteArrayLengthOnBlockHash];

                // Split informations.
                Array.Copy(blockHashBytes, 0, blockHeightBytes, 0, BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash);
                Array.Copy(blockHashBytes, BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash, blockDifficultyBytes, 0, BlockchainSetting.BlockDifficultyByteArrayLengthOnBlockHash);
                Array.Copy(blockHashBytes, BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash + BlockchainSetting.BlockDifficultyByteArrayLengthOnBlockHash, blockCountTransactionBytes, 0, BlockchainSetting.BlockCountTransactionByteArrayLengthOnBlockHash);
                Array.Copy(blockHashBytes, BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash + BlockchainSetting.BlockDifficultyByteArrayLengthOnBlockHash + BlockchainSetting.BlockCountTransactionByteArrayLengthOnBlockHash, blockFinalTransactionHashBytes, 0, BlockchainSetting.BlockFinalTransactionHashByteArrayLengthOnBlockHash);

                ClassBlockEnumCheckStatus result = ClassBlockEnumCheckStatus.VALID_BLOCK_HASH;

                long blockHeightFromHash = BitConverter.ToInt64(blockHeightBytes, 0);

                if (blockHeightFromHash != blockHeight)
                    result = ClassBlockEnumCheckStatus.INVALID_BLOCK_HEIGHT_HASH;
                else
                {
                    BigInteger blockDifficultyFromHash = (BigInteger)BitConverter.ToDouble(blockDifficultyBytes, 0);

                    if (blockDifficultyFromHash != blockDifficulty)
                        result = ClassBlockEnumCheckStatus.INVALID_BLOCK_DIFFICULTY;
                    else
                    {
                        int blockTransactionCountFromHash = BitConverter.ToInt32(blockCountTransactionBytes, 0);

                        if (blockTransactionCountFromHash != previousBlockTransactionCount)
                            result = ClassBlockEnumCheckStatus.INVALID_BLOCK_TRANSACTION_COUNT;
                        else
                        {
                            if (!string.Equals(ClassUtility.GetHexStringFromByteArray(blockFinalTransactionHashBytes), blockTransactionHash, StringComparison.CurrentCultureIgnoreCase))
                                result = ClassBlockEnumCheckStatus.INVALID_BLOCK_TRANSACTION_HASH;
                        }
                    }
                }

                // Clean up.
                Array.Clear(blockHeightBytes, 0, blockHeightBytes.Length);
                Array.Clear(blockDifficultyBytes, 0, blockDifficultyBytes.Length);
                Array.Clear(blockCountTransactionBytes, 0, blockCountTransactionBytes.Length);
                Array.Clear(blockFinalTransactionHashBytes, 0, blockFinalTransactionHashBytes.Length);

                return result;
            }
            catch
            {
                return ClassBlockEnumCheckStatus.INVALID_BLOCK_HASH;
            }

        }

        /// <summary>
        /// If their is transactions into the list we build the sha512 final transaction hash, if not we use the previous block hash.
        /// </summary>
        /// <param name="blockHashTransactionList"></param>
        /// <param name="previousBlockHash"></param>
        /// <returns></returns>
        public static string GetFinalTransactionHashList(List<string> blockHashTransactionList, string previousBlockHash)
        {
            if (blockHashTransactionList?.Count > 0)
                return ClassUtility.GenerateSha3512FromString(string.Join("", blockHashTransactionList));
            else if (!previousBlockHash.IsNullOrEmpty(false, out _))
                return ClassUtility.GenerateSha3512FromString(previousBlockHash);

            return string.Empty;
        }

        /// <summary>
        /// Convert a block hash into a block template;
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="blockTemplateObject"></param>
        /// <returns></returns>
        public static bool GetBlockTemplateFromBlockHash(string blockHash, out ClassBlockTemplateObject blockTemplateObject)
        {
            try
            {
                byte[] blockHashBytes = ClassUtility.GetByteArrayFromHexString(blockHash);

                if (blockHashBytes.Length == BlockchainSetting.BlockHashByteArraySize)
                {
                    byte[] blockHeightBytes = new byte[BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash];
                    byte[] blockDifficultyBytes = new byte[BlockchainSetting.BlockDifficultyByteArrayLengthOnBlockHash];
                    byte[] blockCountTransactionBytes = new byte[BlockchainSetting.BlockCountTransactionByteArrayLengthOnBlockHash];
                    byte[] blockFinalTransactionHashBytes = new byte[BlockchainSetting.BlockFinalTransactionHashByteArrayLengthOnBlockHash];
                    byte[] previousWalletAddressWinnerBytes = new byte[BlockchainSetting.WalletAddressByteArrayLength];

                    // Split informations.
                    Array.Copy(blockHashBytes, 0, blockHeightBytes, 0, BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash);
                    Array.Copy(blockHashBytes, BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash, blockDifficultyBytes, 0, BlockchainSetting.BlockDifficultyByteArrayLengthOnBlockHash);
                    Array.Copy(blockHashBytes, BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash + BlockchainSetting.BlockDifficultyByteArrayLengthOnBlockHash, blockCountTransactionBytes, 0, BlockchainSetting.BlockCountTransactionByteArrayLengthOnBlockHash);
                    Array.Copy(blockHashBytes, BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash + BlockchainSetting.BlockDifficultyByteArrayLengthOnBlockHash + BlockchainSetting.BlockCountTransactionByteArrayLengthOnBlockHash, blockFinalTransactionHashBytes, 0, BlockchainSetting.BlockFinalTransactionHashByteArrayLengthOnBlockHash);
                    Array.Copy(blockHashBytes, BlockchainSetting.BlockHeightByteArrayLengthOnBlockHash + BlockchainSetting.BlockDifficultyByteArrayLengthOnBlockHash + BlockchainSetting.BlockCountTransactionByteArrayLengthOnBlockHash + BlockchainSetting.BlockFinalTransactionHashByteArrayLengthOnBlockHash, previousWalletAddressWinnerBytes, 0, BlockchainSetting.WalletAddressByteArrayLength);

                    blockTemplateObject = new ClassBlockTemplateObject()
                    {
                        BlockHash = blockHash,
                        BlockHeight = BitConverter.ToInt64(blockHeightBytes, 0),
                        BlockDifficulty = (BigInteger)BitConverter.ToDouble(blockDifficultyBytes, 0),
                        BlockPreviousTransactionCount = BitConverter.ToInt32(blockCountTransactionBytes, 0),
                        BlockPreviousFinalTransactionHash = ClassUtility.GetHexStringFromByteArray(blockFinalTransactionHashBytes),
                        BlockPreviousWalletAddressWinner = ClassBase58.EncodeWithCheckSum(previousWalletAddressWinnerBytes)
                    };

                    // Clean up.
                    Array.Clear(blockHeightBytes, 0, blockHeightBytes.Length);
                    Array.Clear(blockDifficultyBytes, 0, blockDifficultyBytes.Length);
                    Array.Clear(blockCountTransactionBytes, 0, blockCountTransactionBytes.Length);
                    Array.Clear(blockFinalTransactionHashBytes, 0, blockFinalTransactionHashBytes.Length);
                    Array.Clear(previousWalletAddressWinnerBytes, 0, previousWalletAddressWinnerBytes.Length);

                    return true;
                }

                blockTemplateObject = null;
            }
            catch
            {
                blockTemplateObject = null;
            }

            return false;
        }

        #endregion

        #region About block difficulty.

        /// <summary>
        /// Generate the next block difficulty.
        /// </summary>
        /// <param name="previousBlockHeight"></param>
        /// <param name="timestampBlockStart"></param>
        /// <param name="timestampBlockFound"></param>
        /// <param name="previousBlockDifficulty"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<BigInteger> GenerateNextBlockDifficulty(long previousBlockHeight, long timestampBlockStart, long timestampBlockFound, BigInteger previousBlockDifficulty, CancellationTokenSource cancellation)
        {
            if (previousBlockHeight > BlockchainSetting.GenesisBlockHeight)
            {

                #region Retrieve back block timespend, block difficulty, and calculate an average from them.

                double averageTotalTimespend = 0;
                double averageTimespendExpected = 0;
                BigInteger sumDifficulty = 0;
                int totalTravel = 0;
                long startHeight = (previousBlockHeight - BlockchainSetting.BlockDifficultyRangeCalculation);

                if (startHeight <= 0)
                    startHeight = BlockchainSetting.GenesisBlockHeight;

                using (DisposableList<long> listBlockHeight = new DisposableList<long>())
                {

                    for (long k = startHeight; k < previousBlockHeight; k++)
                    {
                        if (k >= BlockchainSetting.GenesisBlockHeight && k < previousBlockHeight)
                            listBlockHeight.Add(k);
                    }

                    foreach (ClassBlockObject blockObject in await ClassBlockchainDatabase.BlockchainMemoryManagement.GetListBlockInformationDataFromListBlockHeightStrategy(listBlockHeight, cancellation))
                    {
                        if (blockObject != null)
                        {
                            if (blockObject.BlockHeight >= BlockchainSetting.GenesisBlockHeight)
                            {

                                long timeSpend = blockObject.TimestampFound - blockObject.TimestampCreate;
                                averageTotalTimespend += timeSpend;
                                averageTimespendExpected += BlockchainSetting.BlockTime;
                                sumDifficulty += blockObject.BlockDifficulty;
                                totalTravel++;

                            }
                        }
                    }

                }


                #region Ensure to have any value lower than 1.

                if (averageTotalTimespend <= 0d)
                    averageTotalTimespend = 1;

                if (averageTimespendExpected <= 0d)
                    averageTimespendExpected = 1;

                if (sumDifficulty < 1)
                    sumDifficulty = 1;

                #endregion

                #endregion

                // Divide the sum of difficulty by the number of blocks travel.
                sumDifficulty = BigInteger.Divide(sumDifficulty, totalTravel);

                // Calculate the difficulty factor.
                BigInteger difficultyFactor = (BigInteger)((averageTimespendExpected / averageTotalTimespend) * BlockchainSetting.BlockDifficultyPrecision);

                // Calculate the new block difficulty.
                var newBlockDifficulty = ((sumDifficulty * difficultyFactor) / BlockchainSetting.BlockDifficultyPrecision);

                #region Do not let the next block difficulty lower than the min difficulty accepted by the chain.

                if (newBlockDifficulty < BlockchainSetting.MiningMinDifficulty)
                    newBlockDifficulty = BlockchainSetting.MiningMinDifficulty;

                #endregion

                return newBlockDifficulty;
            }

            return BlockchainSetting.MiningMinDifficulty;
        }

        #endregion

        #region Check block data.

        /// <summary>
        /// Check a block data, compare with previous block data object.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="blockHeightTarget"></param>
        /// <param name="refuseLockedBlock"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> CheckBlockDataObject(ClassBlockObject blockObject, long blockHeightTarget, bool refuseLockedBlock, CancellationTokenSource cancellation)
        {
            if (blockObject == null)
                return false;

            if (blockObject.BlockHash.Length != BlockchainSetting.BlockHashHexSize)
                return false;

            if (blockObject.BlockHeight != blockHeightTarget)
                return false;

            if (blockObject.BlockHeight < BlockchainSetting.GenesisBlockHeight)
                return false;

            if (blockObject.BlockDifficulty < BlockchainSetting.MiningMinDifficulty)
                return false;

            // The genesis block is always unlocked and not contain a mining share.
            if (blockObject.BlockHeight == BlockchainSetting.GenesisBlockHeight)
            {
                if (blockObject.BlockWalletAddressWinner != BlockchainSetting.WalletAddressDev(0))
                    return false;

                if (blockObject.BlockFinalHashTransaction != BlockchainSetting.GenesisBlockFinalTransactionHash)
                    return false;

                if (blockObject.BlockStatus != ClassBlockEnumStatus.UNLOCKED)
                    return false;
            }

            if (!GetBlockTemplateFromBlockHash(blockObject.BlockHash, out ClassBlockTemplateObject blockTemplateObject))
                return false;

            if (blockTemplateObject == null)
                return false;

            if (blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight)
            {
                if (CheckBlockHash(blockObject.BlockHash, blockHeightTarget, blockObject.BlockDifficulty, blockTemplateObject.BlockPreviousTransactionCount, blockTemplateObject.BlockPreviousFinalTransactionHash) != ClassBlockEnumCheckStatus.VALID_BLOCK_HASH)
                    return false;
            }
            else
            {
                if (CheckBlockHash(blockObject.BlockHash, blockHeightTarget, blockObject.BlockDifficulty, blockTemplateObject.BlockPreviousTransactionCount, BlockchainSetting.GenesisBlockFinalTransactionHash) != ClassBlockEnumCheckStatus.VALID_BLOCK_HASH)
                    return false;
            }

            if (blockTemplateObject.BlockHeight != blockObject.BlockHeight ||
                blockTemplateObject.BlockDifficulty != blockObject.BlockDifficulty ||
                blockTemplateObject.BlockHash != blockObject.BlockHash)
            {
                return false;
            }

            // Compare the blocktemplate with previous block object if synced.
            if (blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight)
            {
                long previousBlockHeight = blockHeightTarget - 1;

                if (ClassBlockchainStats.ContainsBlockHeight(previousBlockHeight))
                {
                    ClassBlockObject previousBlockObjectInformation = await ClassBlockchainStats.GetBlockInformationData(previousBlockHeight, cancellation);

                    if (previousBlockObjectInformation != null)
                    {
                        if (previousBlockObjectInformation.BlockStatus != ClassBlockEnumStatus.LOCKED)
                        {
                            if (previousBlockObjectInformation.BlockFinalHashTransaction != blockTemplateObject.BlockPreviousFinalTransactionHash)
                                return false;

                            if (previousBlockObjectInformation.TimestampFound != blockObject.TimestampCreate)
                                return false;
                        }

                        if (refuseLockedBlock)
                        {
                            if (blockObject.BlockMiningPowShareUnlockObject == null)
                                return false;
                        }

                        if (previousBlockObjectInformation.BlockStatus != ClassBlockEnumStatus.LOCKED)
                        {

                            int previousBlockTransactionCount = previousBlockObjectInformation.TotalTransaction;

                            if (previousBlockTransactionCount != blockTemplateObject.BlockPreviousTransactionCount)
                                return false;


                            if (previousBlockObjectInformation.BlockWalletAddressWinner != blockTemplateObject.BlockPreviousWalletAddressWinner)
                                return false;


                            if (await GenerateNextBlockDifficulty(previousBlockHeight, previousBlockObjectInformation.TimestampCreate, previousBlockObjectInformation.TimestampFound, previousBlockObjectInformation.BlockDifficulty, cancellation)
                                != blockObject.BlockDifficulty)
                            {
                                return false;
                            }

                        }
                    }
                    else
                        return false;
                }
            }

            if (refuseLockedBlock)
            {
                if (blockObject.BlockStatus == ClassBlockEnumStatus.LOCKED)
                    return false;

                if (blockObject.BlockWalletAddressWinner.IsNullOrEmpty(false, out _))
                    return false;

                if (blockObject.BlockWalletAddressWinner.Length < BlockchainSetting.WalletAddressWifLengthMin ||
                    blockObject.BlockWalletAddressWinner.Length > BlockchainSetting.WalletAddressWifLengthMax)
                    return false;

                if (blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight)
                {
                    if (blockObject.BlockMiningPowShareUnlockObject == null)
                        return false;

                    if (blockObject.BlockWalletAddressWinner != blockObject.BlockMiningPowShareUnlockObject.WalletAddress)
                        return false;

                    if (blockObject.BlockMiningPowShareUnlockObject.Timestamp != blockObject.TimestampFound)
                        return false;

                    if (ClassMiningPoWaCUtility.CheckPoWaCShare(BlockchainSetting.CurrentMiningPoWaCSettingObject(blockObject.BlockHeight),
                        blockObject.BlockMiningPowShareUnlockObject,
                        blockObject.BlockHeight,
                        blockObject.BlockHash,
                        blockObject.BlockDifficulty,
                        blockTemplateObject.BlockPreviousTransactionCount,
                        blockTemplateObject.BlockPreviousFinalTransactionHash, out BigInteger jobDifficulty, out int jobCompatibilityValue) != ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE)
                    {
                        return false;
                    }


                    if (jobDifficulty != blockObject.BlockMiningPowShareUnlockObject.PoWaCShareDifficulty)
                        return false;

                    if (jobCompatibilityValue != blockTemplateObject.BlockPreviousTransactionCount)
                        return false;
                }

                if (ClassBase58.DecodeWithCheckSum(blockObject.BlockWalletAddressWinner, true) == null)
                    return false;

                if (blockObject.BlockFinalHashTransaction.IsNullOrEmpty(false, out _))
                    return false;
            }
            else
            {
                if (blockObject.BlockStatus == ClassBlockEnumStatus.UNLOCKED)
                    return false;

                if (!blockObject.BlockFinalHashTransaction.IsNullOrEmpty(false, out _))
                    return false;

                if (!blockObject.BlockWalletAddressWinner.IsNullOrEmpty(false, out _))
                    return false;

                if (blockObject.BlockMiningPowShareUnlockObject != null)
                    return false;

                if (blockObject.TimestampFound > 0)
                    return false;

                if (blockObject.BlockTransactionFullyConfirmed)
                    return false;

                if (blockObject.BlockTransactions != null)
                {
                    if (blockObject.BlockTransactions.Count > 0)
                        return false;
                }
            }

            return true;
        }



        /// <summary>
        /// Check the block object content for the task of block transaction confirmation.
        /// </summary>
        /// <param name="previousBlockObject"></param>
        /// <returns></returns>
        public static bool DoCheckBlockTransactionConfirmation(ClassBlockObject blockObject, ClassBlockObject previousBlockObject)
        {
            if (!blockObject.IsConfirmedByNetwork)
                return false;

            if (blockObject.BlockTransactionConfirmationCheckTaskDone)
                return true;

            if (blockObject.BlockHeight > BlockchainSetting.GenesisBlockHeight)
            {
                if (previousBlockObject == null)
                    return false;

                ClassBlockEnumCheckStatus blockCheckStatus = CheckBlockHash(blockObject.BlockHash, blockObject.BlockHeight, blockObject.BlockDifficulty, previousBlockObject.TotalTransaction, previousBlockObject.BlockFinalHashTransaction);

                if (blockCheckStatus != ClassBlockEnumCheckStatus.VALID_BLOCK_HASH)
                    return false;

                if (blockObject.BlockStatus != ClassBlockEnumStatus.UNLOCKED)
                    return false;

                if (blockObject.BlockMiningPowShareUnlockObject == null)
                    return false;

                string blockHash = blockObject.BlockHash;
                BigInteger blockDifficulty = blockObject.BlockDifficulty;
                string walletAddressWinner = blockObject.BlockWalletAddressWinner;
                ClassMiningPoWaCShareObject miningPocShareObject = blockObject.BlockMiningPowShareUnlockObject;

                if (walletAddressWinner != miningPocShareObject.WalletAddress)
                    return false;

                string previousFinalBlockTransactionHash = previousBlockObject.BlockFinalHashTransaction;

                int previousBlockTransactionCount = previousBlockObject.TotalTransaction;

                var resultShare = ClassMiningPoWaCUtility.CheckPoWaCShare(BlockchainSetting.CurrentMiningPoWaCSettingObject(blockObject.BlockHeight), miningPocShareObject, blockObject.BlockHeight, blockHash, blockDifficulty, previousBlockTransactionCount, previousFinalBlockTransactionHash, out BigInteger jobDifficulty, out int jobCompabilityValue);

                if (resultShare != ClassMiningPoWaCEnumStatus.VALID_UNLOCK_BLOCK_SHARE)
                    return false;

                if (jobDifficulty != miningPocShareObject.PoWaCShareDifficulty || jobCompabilityValue != previousBlockTransactionCount)
                    return false;
            }
            else
            {
                if (blockObject.BlockStatus != ClassBlockEnumStatus.UNLOCKED)
                    return false;

                if (blockObject.BlockWalletAddressWinner != BlockchainSetting.WalletAddressDev(0))
                    return false;

                if (blockObject.BlockTransactions.Count != BlockchainSetting.GenesisBlockTransactionCount)
                    return false;

                foreach (var tx in blockObject.BlockTransactions)
                {
                    if (tx.Value.TransactionObject.TransactionType != ClassTransactionEnumType.BLOCK_REWARD_TRANSACTION)
                        return false;

                    if (tx.Value.TransactionObject.WalletAddressReceiver != BlockchainSetting.WalletAddressDev(0))
                        return false;

                    if (tx.Value.TransactionObject.Amount != BlockchainSetting.GenesisBlockAmount)
                        return false;
                }
            }

            return true;
        }


        #endregion

        #region Manage Block Data Object formating for database.

        private const int MaxTransactionPerLineOnStringBlockData = 1000;
        public const string StringBlockDataCharacterSeperator = "¤";
        public const string BlockDataBegin = "[BLOCK-BEGIN]";
        public const string BlockDataEnd = "[BLOCK-END]";

        /// <summary>
        /// Convert a BlockObject into string.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <returns></returns>
        public static string SplitBlockObject(ClassBlockObject blockObject)
        {
            string miningShareJson = "empty";

            if (blockObject.BlockMiningPowShareUnlockObject != null)
                miningShareJson = ClassUtility.SerializeData(blockObject.BlockMiningPowShareUnlockObject);

            string finalTransactionHash = "empty";

            if (!blockObject.BlockFinalHashTransaction.IsNullOrEmpty(false, out _) && blockObject.BlockFinalHashTransaction != "empty")
                finalTransactionHash = blockObject.BlockFinalHashTransaction;

            return blockObject.BlockHeight + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.BlockDifficulty + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.BlockHash + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   miningShareJson + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.TimestampCreate + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.TimestampFound + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.BlockWalletAddressWinner + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.BlockStatus + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   Convert.ToInt32(blockObject.BlockUnlockValid) + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.BlockLastChangeTimestamp + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   Convert.ToInt32(blockObject.BlockTransactionConfirmationCheckTaskDone) + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.BlockTotalTaskTransactionConfirmationDone + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   finalTransactionHash + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.BlockLastHeightTransactionConfirmationDone + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.BlockNetworkAmountConfirmations + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   Convert.ToInt32(blockObject.BlockTransactionFullyConfirmed) + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.TotalCoinConfirmed + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.TotalCoinPending + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.TotalFee + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.TotalTransaction + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator +
                   blockObject.TotalTransactionConfirmed + ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator;
        }

        /// <summary>
        /// Convert a string block object line into a BlockObject.
        /// </summary>
        /// <param name="blockObjectLine"></param>
        /// <param name="blockObject"></param>
        /// <returns></returns>
        public static bool StringToBlockObject(string blockObjectLine, out ClassBlockObject blockObject)
        {

            try
            {
                using (DisposableList<string> blockObjectLineSplit = blockObjectLine.DisposableSplit(ClassBlockSplitDataConfig.BlockSplitDataCharacterSeparator))
                {

                    long blockHeight = long.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_HEIGHT]);
                    BigInteger blockDifficulty = BigInteger.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_DIFFICULTY]);
                    string blockHash = blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_HASH];
                    long timestampCreate = long.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_TIMESTAMP_CREATE]);
                    long timestampFound = long.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_TIMESTAMP_FOUND]);
                    string blockWalletAddressWinner = blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_WALLET_ADDRESS_WINNER];
                    ClassBlockEnumStatus blockStatus = (ClassBlockEnumStatus)System.Enum.Parse(typeof(ClassBlockEnumStatus), blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_STATUS]);

                    ClassMiningPoWaCShareObject blockMiningPowShareObject = null;

                    if (blockStatus == ClassBlockEnumStatus.UNLOCKED && blockHeight > BlockchainSetting.GenesisBlockHeight)
                    {
                        if (blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_MINING_SHARE_UNLOCK] != "empty")
                        {
                            if (ClassUtility.CheckBase64String(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_MINING_SHARE_UNLOCK]))
                            {
                                string miningShareJson = Encoding.UTF8.GetString(Convert.FromBase64String(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_MINING_SHARE_UNLOCK]));

                                if (!ClassUtility.TryDeserialize(miningShareJson, out blockMiningPowShareObject, ObjectCreationHandling.Reuse))
                                {
#if DEBUG
                                    Debug.WriteLine("Error on deserialize the data block line target: " + blockObjectLine + " -> Can't deserialize the mining block share from block height: " + blockHeight);
#endif

                                    blockObject = null;
                                    return false;
                                }
                            }
                            else
                            {
                                if (!ClassUtility.TryDeserialize(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_MINING_SHARE_UNLOCK], out blockMiningPowShareObject, ObjectCreationHandling.Reuse))
                                {
#if DEBUG
                                    Debug.WriteLine("Error on deserialize the data block line target: " + blockObjectLine + " -> Can't deserialize the mining block share from block height: " + blockHeight);
#endif

                                    blockObject = null;
                                    return false;
                                }
                            }
                        }
                    }
                    bool blockUnlockValid = long.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_UNLOCK_VALID]) == 1;
                    long blockLastChangeTimestamp = long.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_LAST_CHANGE_TIMESTAMP]);
                    bool blockTransactionConfirmationTaskDone = int.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_TRANSACTION_CONFIRMATION_TASK_DONE]) == 1;

                    long.TryParse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_TOTAL_TASK_TRANSACTION_CONFIRMATION_DONE], out var blockTotalTransactionConfirmationTaskDone);

                    string blockFinalTransactionHash = blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_FINAL_TRANSACTION_HASH];

                    if (blockFinalTransactionHash == "empty")
                        blockFinalTransactionHash = null;

                    long blockLastBlockHeightTransactionConfirmationDone = long.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_LAST_BLOCK_HEIGHT_TRANSACTION_CONFIRMATION_DONE]);

                    long blockNetworkAmountConfirmations = long.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_NETWORK_AMOUNT_CONFIRMATIONS]);
                    bool blockTransactionFullyConfirmed = int.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_TRANSACTION_FULLY_CONFIRMED]) == 1;


                    BigInteger blockTotalCoinConfirmed = 0;
                    BigInteger blockTotalCoinPending = 0;
                    BigInteger blockTotalFee = 0;
                    int blockTotalTransaction = 0;
                    int blockTotalTransactionConfirmed = 0;

                    if (blockStatus == ClassBlockEnumStatus.UNLOCKED)
                    {
                        blockTotalCoinConfirmed = BigInteger.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_TOTAL_COIN_CONFIRMED]);
                        blockTotalCoinPending = BigInteger.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_TOTAL_COIN_PENDING]);
                        blockTotalFee = BigInteger.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_TOTAL_COIN_FEE]);
                        blockTotalTransaction = int.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_TOTAL_TRANSACTION]);
                        blockTotalTransactionConfirmed = int.Parse(blockObjectLineSplit[(int)ClassBlockEnumSplitData.INDEX_BLOCK_TOTAL_TRANSACTION_CONFIRMED]);
                    }

                    blockObject = new ClassBlockObject(blockHeight, blockDifficulty, blockHash, timestampCreate, timestampFound, blockStatus, blockUnlockValid, blockTransactionConfirmationTaskDone)
                    {
                        BlockMiningPowShareUnlockObject = blockMiningPowShareObject,
                        BlockLastChangeTimestamp = blockLastChangeTimestamp,
                        BlockTotalTaskTransactionConfirmationDone = blockTotalTransactionConfirmationTaskDone,
                        BlockFinalHashTransaction = blockFinalTransactionHash,
                        BlockLastHeightTransactionConfirmationDone = blockLastBlockHeightTransactionConfirmationDone,
                        BlockNetworkAmountConfirmations = blockNetworkAmountConfirmations,
                        BlockTransactionFullyConfirmed = blockTransactionFullyConfirmed,
                        TotalCoinConfirmed = blockTotalCoinConfirmed,
                        TotalCoinPending = blockTotalCoinPending,
                        TotalFee = blockTotalFee,
                        TotalTransaction = blockTotalTransaction,
                        TotalTransactionConfirmed = blockTotalTransactionConfirmed
                    };


                    return true;
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine("Error on deserialize the data block line target: " + blockObjectLine + " -> Exception: " + error);
#endif

                blockObject = null;
                return false;
            }

        }

        /// <summary>
        /// Block object data.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <param name="isJson"></param>
        /// <returns></returns>
        public static IEnumerable<string> BlockObjectToStringBlockData(ClassBlockObject blockObject, bool isJson)
        {
            yield return BlockDataBegin;

            if (!isJson)
                yield return SplitBlockObject(blockObject);
            else
            {
                blockObject.DeepCloneBlockObject(false, out ClassBlockObject blockObjectCopy);
                yield return ClassUtility.SerializeData(blockObjectCopy);
            }


            if (blockObject.BlockTransactions.Count > 0)
            {
                int totalLineTransactionOnLine = 0;
                string transactionLine = string.Empty;
                foreach (string transactionHash in blockObject.BlockTransactions.Keys)
                {
                    if (!isJson)
                    {
                        transactionLine += ClassTransactionUtility.SplitBlockTransactionObject(blockObject.BlockTransactions[transactionHash]) + StringBlockDataCharacterSeperator;
                        totalLineTransactionOnLine++;
                    }
                    else
                    {
                        transactionLine += ClassUtility.SerializeData(blockObject.BlockTransactions[transactionHash]) + StringBlockDataCharacterSeperator;
                        totalLineTransactionOnLine++;
                    }

                    if (totalLineTransactionOnLine >= MaxTransactionPerLineOnStringBlockData)
                    {
                        yield return transactionLine + StringBlockDataCharacterSeperator;

                        transactionLine = string.Empty;
                        totalLineTransactionOnLine = 0;
                    }
                }

                if (!transactionLine.IsNullOrEmpty(false, out _))
                    yield return transactionLine + StringBlockDataCharacterSeperator;
            }

            yield return BlockDataEnd;
        }

        /// <summary>
        /// Convert a block transaction into a string format or into json string format.
        /// </summary>
        /// <param name="blockTransactionLine"></param>
        /// <param name="isJson"></param>
        /// <returns></returns>
        public static IEnumerable<ClassBlockTransaction> BlockTransactionLineSplit(string blockTransactionLine, bool isJson)
        {
            foreach (var transactionLine in blockTransactionLine.DisposableSplit(StringBlockDataCharacterSeperator).GetList.ToArray())
            {
                if (isJson)
                {
                    if (ClassUtility.TryDeserialize(transactionLine, out ClassBlockTransaction blockTransactionObject, ObjectCreationHandling.Reuse))
                        yield return blockTransactionObject;
                }
                else
                {
                    if (ClassTransactionUtility.StringToBlockTransaction(transactionLine, out ClassBlockTransaction blockTransactionObject))
                        yield return blockTransactionObject;
                }
            }
        }

        #endregion

        #region About block data size.

        /// <summary>
        /// Get the amount of memory spend by a block object provided.
        /// </summary>
        /// <param name="blockObject"></param>
        /// <returns></returns>
        public static long GetIoBlockSizeOnMemory(ClassBlockObject blockObject)
        {
            long totalMemoryUsage = 0;

            try
            {
                if (blockObject != null)
                {
                    // Block Height.
                    totalMemoryUsage += sizeof(long);

                    // Block difficulty.
                    totalMemoryUsage += blockObject.BlockDifficulty.ToByteArray().Length;

                    // Block hash.
                    totalMemoryUsage += blockObject.BlockHash.Length * sizeof(char);

                    // Block mining share who unlock it.
                    if (blockObject.BlockMiningPowShareUnlockObject != null)
                    {
                        // Block height from mining share.
                        totalMemoryUsage += sizeof(long);
                        totalMemoryUsage += blockObject.BlockMiningPowShareUnlockObject.WalletAddress.Length * sizeof(char);
                        totalMemoryUsage += blockObject.BlockMiningPowShareUnlockObject.BlockHash.Length * sizeof(char);
                        totalMemoryUsage += blockObject.BlockMiningPowShareUnlockObject.PoWaCShare.Length * sizeof(char);

                        // Nonce.
                        totalMemoryUsage += sizeof(long);
                        totalMemoryUsage += blockObject.BlockMiningPowShareUnlockObject.NonceComputedHexString.Length * sizeof(char);
                        totalMemoryUsage += blockObject.BlockMiningPowShareUnlockObject.PoWaCShareDifficulty.ToByteArray().Length;

                        // Timestamp.
                        totalMemoryUsage += sizeof(long);
                    }

                    // Timestamp create.
                    totalMemoryUsage += sizeof(long);

                    // Timestamp found.
                    totalMemoryUsage += sizeof(long);

                    // Wallet address who unlock the block.
                    totalMemoryUsage += blockObject.BlockWalletAddressWinner.Length * sizeof(char);

                    // Block last change timestamp.
                    totalMemoryUsage += sizeof(long);

                    // Block total network confirmations.
                    totalMemoryUsage += sizeof(long);

                    // Transactions stored into the block.
                    if (blockObject.BlockTransactions != null)
                    {
                        if (blockObject.BlockTransactions.Count > 0)
                        {
                            // Calculate allocations from hash.
                            totalMemoryUsage += (blockObject.BlockTransactions.Count * (BlockchainSetting.TransactionHashSize * sizeof(char)));

                            // Calculate allocations from transaction object stored.
                            foreach (string transactionHash in blockObject.BlockTransactions.Keys)
                                totalMemoryUsage += ClassTransactionUtility.GetBlockTransactionMemorySize(blockObject.BlockTransactions[transactionHash]);
                        }
                    }

                    // Block transaction confirmation task done.
                    totalMemoryUsage += sizeof(bool);

                    // Block last task transaction confirmation done.
                    totalMemoryUsage += sizeof(long);

                    // Block last block height transaction confirmation done.
                    totalMemoryUsage += sizeof(long);

                    // Block final transaction hash.
                    totalMemoryUsage += (blockObject.BlockFinalHashTransaction.Length * sizeof(char));

                    // Block transaction fully confirmed status.
                    totalMemoryUsage += sizeof(bool);

                    // Block total coin confirmed.
                    if (blockObject.TotalCoinConfirmed > 0)
                        totalMemoryUsage += blockObject.TotalCoinConfirmed.ToByteArray().Length;

                    // Block total coin pending.
                    if (blockObject.TotalCoinPending > 0)
                        totalMemoryUsage += blockObject.TotalCoinPending.ToByteArray().Length;

                    // Block total fee.
                    if (blockObject.TotalFee > 0)
                        totalMemoryUsage += blockObject.TotalFee.ToByteArray().Length;

                    // Block total transactions.
                    totalMemoryUsage += sizeof(int);

                    // Block total transactions confirmed.
                    totalMemoryUsage += sizeof(int);

                    // Block total transaction to sync.
                    totalMemoryUsage += sizeof(int);

                    // Block total slow amount of network confirmations done.
                    totalMemoryUsage += sizeof(int);
                }
            }
#if DEBUG
            catch (Exception error)
            {
                Debug.WriteLine("Error on calculating block object memory size. Exception: " + error.Message);
            }
#else
            catch
            {
                // Ignored.
            }
#endif
            return totalMemoryUsage;
        }

        #endregion
    }
}
