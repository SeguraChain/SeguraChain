using System.Numerics;

namespace SeguraChain_RPC_Wallet.API.Service.Packet.Object.Response
{
    public class ClassRpcApiSendWalletInformation
    {
        public string wallet_address;
        public string wallet_public_key;
        public string wallet_private_key;
        public BigInteger wallet_balance;
        public BigInteger wallet_pending_balance;
        public int wallet_transaction_count;
    }
}
