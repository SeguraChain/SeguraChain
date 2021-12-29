using SeguraChain_Lib.Blockchain.Block.Object.Structure;
using System.Collections.Generic;

namespace SeguraChain_RPC_Wallet.API.Service.Packet.Object.Response
{
    public class ClassRpcApiSendWalletTransaction
    {
        public List<ClassBlockTransaction> block_transaction_object;
        public long packet_timestamp;
    }
}
