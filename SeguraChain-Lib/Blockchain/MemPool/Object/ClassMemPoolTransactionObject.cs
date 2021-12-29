using SeguraChain_Lib.Blockchain.Transaction.Object;

namespace SeguraChain_Lib.Blockchain.MemPool.Object
{
    public class ClassMemPoolTransactionObject
    {
        public ClassTransactionObject TransactionObject;
        public int TotalNoConsensusCount; // Once the no consensus count reach the maximum of votes to do, the transaction is erased from the mempool.

        /// <summary>
        /// Constructor, set default value.
        /// </summary>
        public ClassMemPoolTransactionObject()
        {
            TotalNoConsensusCount = 0;
        }
    }
}
