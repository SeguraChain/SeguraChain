﻿namespace SeguraChain_Lib.Blockchain.Block.Enum
{
    public enum ClassBlockTransactionEnumStatus
    {
        TRANSACTION_BLOCK_HEIGHT_NOT_REACH = 0,
        TRANSACTION_BLOCK_ENOUGH_CONFIRMATIONS_REACH = 1,
        TRANSACTION_BLOCK_AMOUNT_OF_CONFIRMATIONS_WRONG = 2,
        TRANSACTION_BLOCK_INVALID_DATA = 3,
        TRANSACTION_BLOCK_CONFIRMATIONS_INCREMENTED = 4,
        TRANSACTION_BLOCK_INVALID = 5
    }
}
