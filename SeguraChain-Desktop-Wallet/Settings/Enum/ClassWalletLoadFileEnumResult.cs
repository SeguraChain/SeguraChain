namespace SeguraChain_Desktop_Wallet.Settings.Enum
{
    public enum ClassWalletLoadFileEnumResult
    {
        WALLET_LOAD_SUCCESS = 0,
        WALLET_LOAD_FILE_NOT_FOUND_ERROR = 1,
        WALLET_LOAD_BAD_FILE_FORMAT_ERROR = 2,
        WALLET_LOAD_EMPTY_FILE_DATA_ERROR = 3,
        WALLET_LOAD_ALREADY_LOADED_ERROR = 4,
        WALLET_LOAD_INVALID_CONTENT = 5,
        WALLET_LOAD_INVALID_ENCRYPTION_CONTENT = 6
    }
}
