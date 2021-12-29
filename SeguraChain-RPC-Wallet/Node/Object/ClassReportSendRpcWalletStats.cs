using System.Numerics;


namespace SeguraChain_RPC_Wallet.Node.Object
{
    public class ClassReportSendRpcWalletStats
    {
        public int total_wallet_count;
        public long current_block_height;
        public long last_block_height_unlocked;
        public string current_block_hash;
        public BigInteger current_block_difficulty;
        public BigInteger total_balance;
        public BigInteger total_pending_balance;
    }
}
