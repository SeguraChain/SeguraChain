using System.Numerics;

namespace SeguraChain_RPC_Wallet.API.Service.Packet.Object.Request
{
    public class ClassRpcApiPostTransactionObject
    {
        public string wallet_address_src;
        public string wallet_address_target;
        public int total_confirmation_target;
        public long payment_id;
        public bool transfer;
        public BigInteger amount;
        public BigInteger fee;
    }
}
