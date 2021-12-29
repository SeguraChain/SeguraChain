using System.Drawing;
using SeguraChain_Lib.Blockchain.Transaction.Enum;

namespace SeguraChain_Desktop_Wallet.MainForm.Object
{
    public class ClassRecentTransactionHistoryObject
    {
        public long BlockHeight;
        public bool IsMemPool;
        public bool IsConfirmed;
        public bool IsSender;
        public bool TransactionStatus;
        public ClassTransactionEnumType TransactionType;
        public long TransactionTotalConfirmations;
        public RectangleF TransactionDrawRectangle;
    }
}
