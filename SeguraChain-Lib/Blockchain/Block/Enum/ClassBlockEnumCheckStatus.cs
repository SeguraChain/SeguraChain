namespace SeguraChain_Lib.Blockchain.Block.Enum
{
    public enum ClassBlockEnumCheckStatus
    {
        INVALID_BLOCK_HASH_FORMAT = 0,
        INVALID_BLOCK_HASH_LENGTH = 1,
        INVALID_BLOCK_HEIGHT_HASH = 2,
        INVALID_BLOCK_DIFFICULTY = 3,
        INVALID_BLOCK_TRANSACTION_COUNT = 4,
        INVALID_BLOCK_TRANSACTION_HASH = 5,
        INVALID_BLOCK_HASH = 6,
        VALID_BLOCK_HASH = 7
    }
}
