using System.Numerics;

namespace SeguraChain_Lib.Blockchain.Block.Object.Structure
{
    public class ClassBlockTemplateObject
    {
        public long BlockHeight;
        public string BlockHash;
        public BigInteger BlockDifficulty;
        public int BlockPreviousTransactionCount;
        public string BlockPreviousFinalTransactionHash;
        public string BlockPreviousWalletAddressWinner;
    }
}
