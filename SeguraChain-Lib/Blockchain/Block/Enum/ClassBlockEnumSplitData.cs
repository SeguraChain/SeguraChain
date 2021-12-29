namespace SeguraChain_Lib.Blockchain.Block.Enum
{
    /// <summary>
    /// Split character used on no-json block data.
    /// </summary>
    public class ClassBlockSplitDataConfig
    {
        public const string BlockSplitDataCharacterSeparator = "|";
    }

    public enum ClassBlockEnumSplitData
    {
        INDEX_BLOCK_HEIGHT = 0,
        INDEX_BLOCK_DIFFICULTY = 1,
        INDEX_BLOCK_HASH = 2,
        INDEX_BLOCK_MINING_SHARE_UNLOCK = 3, // Json serialized.
        INDEX_BLOCK_TIMESTAMP_CREATE = 4,
        INDEX_BLOCK_TIMESTAMP_FOUND = 5,
        INDEX_BLOCK_WALLET_ADDRESS_WINNER = 6,
        INDEX_BLOCK_STATUS = 7,
        INDEX_BLOCK_UNLOCK_VALID = 8,
        INDEX_BLOCK_LAST_CHANGE_TIMESTAMP = 9,
        INDEX_BLOCK_TRANSACTION_CONFIRMATION_TASK_DONE = 10,
        INDEX_BLOCK_TOTAL_TASK_TRANSACTION_CONFIRMATION_DONE = 11,
        INDEX_BLOCK_FINAL_TRANSACTION_HASH = 12,
        INDEX_BLOCK_LAST_BLOCK_HEIGHT_TRANSACTION_CONFIRMATION_DONE = 13,
        INDEX_BLOCK_NETWORK_AMOUNT_CONFIRMATIONS = 14,
        INDEX_BLOCK_TRANSACTION_FULLY_CONFIRMED = 15,
        INDEX_BLOCK_TOTAL_COIN_CONFIRMED = 16,
        INDEX_BLOCK_TOTAL_COIN_PENDING = 17,
        INDEX_BLOCK_TOTAL_COIN_FEE = 18,
        INDEX_BLOCK_TOTAL_TRANSACTION = 19,
        INDEX_BLOCK_TOTAL_TRANSACTION_CONFIRMED = 20,
    }
}
