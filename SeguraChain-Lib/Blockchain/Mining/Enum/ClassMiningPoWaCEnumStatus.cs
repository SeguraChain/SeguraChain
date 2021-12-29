namespace SeguraChain_Lib.Blockchain.Mining.Enum
{
    public enum ClassMiningPoWaCEnumStatus
    {
        EMPTY_SHARE = 0,
        INVALID_WALLET_ADDRESS = 1,
        INVALID_BLOCK_HASH = 2,
        INVALID_BLOCK_HEIGHT = 3,
        INVALID_NONCE_SHARE = 4,
        INVALID_SHARE_FORMAT = 5,
        INVALID_SHARE_DIFFICULTY = 6,
        INVALID_SHARE_ENCRYPTION = 8,
        INVALID_SHARE_DATA = 9,
        INVALID_SHARE_DATA_SIZE = 10,
        INVALID_SHARE_COMPATIBILITY = 11,
        INVALID_TIMESTAMP_SHARE = 12,
        LOW_DIFFICULTY_SHARE = 13,
        BLOCK_ALREADY_FOUND = 14,
        VALID_SHARE = 15,
        VALID_UNLOCK_BLOCK_SHARE = 16,
        SUBMIT_NETWORK_ERROR = 17
    }
}
