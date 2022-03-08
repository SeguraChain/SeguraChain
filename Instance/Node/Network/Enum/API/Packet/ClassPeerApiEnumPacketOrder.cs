namespace SeguraChain_Lib.Instance.Node.Network.Enum.API.Packet
{
    public enum ClassPeerApiPostPacketSendEnum
    {
        ASK_BLOCK_INFORMATION, // Argument: Block Height
        ASK_BLOCK_TRANSACTION, // Arguments: Transaction hash + Block Height
        ASK_BLOCK_TRANSACTION_BY_RANGE, // Arguments: Block Height, start, end.
        ASK_BLOCK_TRANSACTION_BY_HASH_LIST, // Arguments: List transaction hash, block height.
        ASK_GENERATE_BLOCK_HEIGHT_START_TRANSACTION_CONFIRMATION, // Arguments: Last block height unlocked, last block height
        ASK_FEE_COST_TRANSACTION, // Arguments: Last block height unlocked, block height confirmation start, block height confirmation target.
        ASK_MEMPOOL_TRANSACTION, // Arguments: Transaction hash + Block Height
        ASK_MEMPOOL_TRANSACTION_COUNT_BY_BLOCK_HEIGHT, // Argument: Block Height.
        ASK_MEMPOOL_TRANSACTION_BY_RANGE, // Arguments: block height, start, end.
        PUSH_WALLET_TRANSACTION, // Argument: Wallet Transaction Object signed.
        PUSH_MINING_SHARE,
        BLOCKCHAIN_EXPLORER
    }

    public enum ClassPeerApiPacketResponseEnum
    {
        SEND_NETWORK_STATS,
        SEND_BLOCK_INFORMATION,
        SEND_BLOCK_TRANSACTION,
        SEND_BLOCK_TRANSACTION_BY_RANGE,
        SEND_BLOCK_TRANSACTION_BY_HASH_LIST,
        SEND_MEMPOOL_TRANSACTION,
        SEND_MEMPOOL_TRANSACTION_COUNT,
        SEND_MEMPOOL_TRANSACTION_COUNT_BY_BLOCK_HEIGHT,
        SEND_MEMPOOL_TRANSACTION_BY_RANGE,
        SEND_LAST_BLOCK_HEIGHT_UNLOCKED,
        SEND_LAST_BLOCK_HEIGHT,
        SEND_LAST_BLOCK_HEIGHT_TRANSACTION_CONFIRMATION,
        SEND_GENERATE_BLOCK_HEIGHT_START_TRANSACTION_CONFIRMATION,
        SEND_FEE_COST_TRANSACTION,
        SEND_REPLY_WALLET_TRANSACTION_PUSHED,

        SEND_BLOCK_TEMPLATE,
        SEND_MINING_SHARE_RESPONSE,

        INVALID_PACKET,
        INVALID_PACKET_TIMESTAMP,
        INVALID_BLOCK_HEIGHT,
        INVALID_BLOCK_TRANSACTION_ID,
        INVALID_WALLET_ADDRESS,
        INVALID_WALLET_TRANSACTION_HASH,
        WALLET_ADDRESS_NOT_REGISTERED,
        INVALID_PUSH_TRANSACTION,
        INVALID_PUSH_MINING_SHARE,
        MAX_BLOCK_TRANSACTION_REACH,
        OK
    }
}
