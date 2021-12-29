using System.Numerics;
using Newtonsoft.Json;
using SeguraChain_Lib.Blockchain.Transaction.Enum;
using SeguraChain_Lib.Blockchain.Transaction.Object;
using SeguraChain_Lib.Blockchain.Transaction.Utility;

namespace SeguraChain_Lib.Blockchain.Block.Object.Structure
{
    /// <summary>
    ///  Contains transactions id's and transactions confirmations numbers.
    /// </summary>
    public class ClassBlockTransaction
    {
        public long TransactionBlockHeightInsert;
        public long TransactionBlockHeightTarget;
        public long TransactionTotalConfirmation;

        [JsonIgnore]
        private ClassTransactionObject _transactionObject;
        

        public ClassTransactionObject TransactionObject
        {
            get
            {
                if (TransactionSize == 0)
                    TransactionSize = ClassTransactionUtility.GetTransactionMemorySize(_transactionObject, false);

                return _transactionObject;
            }
            set
            {
                _transactionObject = value;

                if (TransactionSize == 0)
                    TransactionSize = ClassTransactionUtility.GetTransactionMemorySize(_transactionObject, false);
            }
        }

        public bool TransactionStatus;
        public long TransactionInvalidRemoveTimestamp;
        public ClassTransactionEnumStatus TransactionInvalidStatus;
        public int IndexInsert;
        public bool Spent => TotalSpend >= TransactionObject.Amount;

        public BigInteger TotalSpend;
        public long TransactionSize;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="indexInsert"></param>
        /// <param name="transactionObject"></param>
        /// <param name="ip"></param>
        /// <param name="mac"></param>
        /// <param name="hwid"></param>
        public ClassBlockTransaction(int indexInsert, ClassTransactionObject transactionObject)
        {
            IndexInsert = indexInsert;
            _transactionObject = transactionObject;
            TransactionBlockHeightInsert = _transactionObject.BlockHeightTransaction;
            TransactionBlockHeightTarget = _transactionObject.BlockHeightTransactionConfirmationTarget;
            TransactionTotalConfirmation = 0;
            TransactionStatus = true;
            TotalSpend = 0;
            TransactionSize = ClassTransactionUtility.GetTransactionMemorySize(_transactionObject, false);
        }

        [JsonIgnore]
        public bool NeedUpdateAmountTransactionSource => TransactionTotalConfirmation == 0 &&
                                                                    (TransactionObject.TransactionType == ClassTransactionEnumType.NORMAL_TRANSACTION ||
                                                                    TransactionObject.TransactionType == ClassTransactionEnumType.TRANSFER_TRANSACTION);

        [JsonIgnore]
        public bool IsConfirmed => TransactionTotalConfirmation >= (TransactionObject.BlockHeightTransactionConfirmationTarget - TransactionObject.BlockHeightTransaction);

        public ClassBlockTransaction Clone()
        {
            ClassTransactionUtility.StringToBlockTransaction(ClassTransactionUtility.SplitBlockTransactionObject(this), out ClassBlockTransaction blockTransaction);

            return blockTransaction;
        }
    }
}
