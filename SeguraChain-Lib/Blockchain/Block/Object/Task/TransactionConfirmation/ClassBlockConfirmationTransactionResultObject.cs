using SeguraChain_Lib.Blockchain.Block.Object.Structure;

namespace SeguraChain_Lib.Blockchain.Block.Object.Task.TransactionConfirmation
{
    public class ClassBlockConfirmationTransactionResultObject
    {
        public bool ConfirmationTaskStatus;
        public long TotalIncrementedTransactions;
        public long TotalTransactionsConfirmed;
        public long TotalTransactionInPending;
        public long TotalTransactionWrong;
        public ClassBlockObject BlockObjectUpdated;
    }
}
