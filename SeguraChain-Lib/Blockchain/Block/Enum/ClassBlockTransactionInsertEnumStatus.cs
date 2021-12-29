namespace SeguraChain_Lib.Blockchain.Block.Enum
{
    public enum ClassBlockTransactionInsertEnumStatus
    {
        BLOCK_HEIGHT_NOT_EXIST = 0,
        MAX_BLOCK_TRANSACTION_PER_BLOCK_HEIGHT_TARGET_REACH = 1,
        BLOCK_TRANSACTION_HASH_ALREADY_EXIST = 2,
        BLOCK_TRANSACTION_INSERTED = 3,
        BLOCK_TRANSACTION_INVALID = 4,
    }
}
