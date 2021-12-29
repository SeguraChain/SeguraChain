using SeguraChain_Lib.Blockchain.Mining.Object;

namespace SeguraChain_Lib.Blockchain.MemPool.Object
{
    public class ClassMemPoolMiningPowShareObject
    {
        public ClassMiningPoWaCShareObject MiningPowShareObject;
        public int TotalNoConsensusCount; // Once the no consensus count reach the maximum of votes to do, the mining share is erased from the mempool.

        /// <summary>
        /// Constructor, set default value.
        /// </summary>
        public ClassMemPoolMiningPowShareObject()
        {
            TotalNoConsensusCount = 0;
        }
    }
}
