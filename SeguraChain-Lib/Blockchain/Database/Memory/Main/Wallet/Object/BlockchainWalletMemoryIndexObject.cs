using System.Collections.Generic;

namespace SeguraChain_Lib.Blockchain.Database.Memory.Main.Wallet.Object
{
    public class BlockchainWalletMemoryIndexObject
    {
        public Dictionary<string, BlockchainWalletMemoryObject> DictionaryBlockchainWalletMemoryObject;

        public BlockchainWalletMemoryIndexObject()
        {
            DictionaryBlockchainWalletMemoryObject = new Dictionary<string, BlockchainWalletMemoryObject>();
        }
    }
}
