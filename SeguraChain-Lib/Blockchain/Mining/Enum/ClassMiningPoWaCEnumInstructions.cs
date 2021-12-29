namespace SeguraChain_Lib.Blockchain.Mining.Enum
{
    public enum ClassMiningPoWaCEnumInstructions
    {
        DO_NONCE_IV = 0,
        DO_LZ4_COMPRESS_NONCE_IV = 1,
        DO_NONCE_IV_ITERATIONS = 2,
        DO_NONCE_IV_XOR = 3,
        DO_NONCE_IV_EASY_SQUARE_MATH = 4,
        DO_ENCRYPTED_POC_SHARE = 5
    }
}
