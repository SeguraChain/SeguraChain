using System.Numerics;

namespace SeguraChain_Lib.Blockchain.Wallet.Object.Blockchain
{
    public class ClassBlockchainWalletBalanceCheckpointObject
    {
        public BigInteger LastWalletBalance;
        public BigInteger LastWalletPendingBalance;
        public long BlockHeight;
        public int TotalTx;

        public ClassBlockchainWalletBalanceCheckpointObject()
        {
            BlockHeight = 0;
            LastWalletBalance = 0;
            LastWalletPendingBalance = 0;
            TotalTx = 0;
        }
    }
}
