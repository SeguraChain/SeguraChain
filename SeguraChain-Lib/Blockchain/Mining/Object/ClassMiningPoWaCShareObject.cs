using System.Numerics;

namespace SeguraChain_Lib.Blockchain.Mining.Object
{
    public class ClassMiningPoWaCShareObject
    {
        public string WalletAddress;
        public long BlockHeight;
        public string BlockHash;
        public string PoWaCShare;
        public long Nonce;
        public string NonceComputedHexString;
        public BigInteger PoWaCShareDifficulty;
        public long Timestamp;
    }
}
