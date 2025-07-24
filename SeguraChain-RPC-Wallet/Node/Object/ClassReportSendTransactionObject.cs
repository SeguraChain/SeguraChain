using SeguraChain_Lib.Blockchain.Transaction.Object;

namespace SeguraChain_RPC_Wallet.Node.Object
{
    public class ClassReportSendTransactionObject
    {
        public string wallet_address_sender;
        public string wallet_address_target;
        public long block_height;
        public long block_height_confirmation_target;
        public bool status;
        public string transaction_hash;
        public ClassTransactionObject transaction_object;


        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassReportSendTransactionObject()
        {
            status = false;
        }
    }
}
