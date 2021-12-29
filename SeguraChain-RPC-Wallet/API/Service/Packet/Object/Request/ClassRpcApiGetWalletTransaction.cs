namespace SeguraChain_RPC_Wallet.API.Service.Packet.Object.Request
{
    public class ClassRpcApiGetWalletTransaction
    {
        public string wallet_address;

        public bool by_transaction_by_index;
        public int transaction_start_index;
        public int transaction_end_index;

        public bool by_transaction_by_hash;
        public string transaction_hash;
    }
}
