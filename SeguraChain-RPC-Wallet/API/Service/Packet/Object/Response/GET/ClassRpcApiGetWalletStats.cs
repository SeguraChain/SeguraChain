using System.Numerics;

namespace SeguraChain_RPC_Wallet.API.Service.Packet.Object.Response.GET
{
    public class ClassRpcApiGetWalletStats
    {
        public int wallet_count;
        public long wallet_total_transactions;
        public BigInteger wallet_total_amount;
        public BigInteger wallet_total_pending_amount;
        public BigInteger wallet_total_fee_amount;
    }
}
