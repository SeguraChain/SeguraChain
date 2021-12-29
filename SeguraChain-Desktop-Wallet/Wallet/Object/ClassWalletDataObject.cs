using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SeguraChain_Desktop_Wallet.Wallet.Object
{
    public class ClassWalletDataObject
    {
        public string WalletAddress;
        public string WalletPublicKey;
        public string WalletPrivateKey;
        public SortedList<long, HashSet<string>> WalletTransactionList; // Tx hash | Block Height
        public HashSet<string> WalletMemPoolTransactionList; // Tx hash | Block Height
        public long WalletLastBlockHeightSynced;
        public bool WalletEncrypted;

        public string WalletPassphraseHash;
        public string WalletEncryptionIv;
        public int WalletEncryptionRounds;

        public ClassWalletBalanceObject WalletBalanceObject;

        [JsonIgnore]
        public bool WalletEnableRescan;

        [JsonIgnore]
        public bool WalletOnSync;

        [JsonIgnore]
        public bool WalletBalanceCalculated;

        [JsonIgnore]
        public string WalletFileName { get; set; }

        [JsonIgnore]
        public long WalletTotalTransaction;

        [JsonIgnore]
        public long WalletTotalMemPoolTransaction;

        [JsonIgnore]
        public FileStream WalletFileStream;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ClassWalletDataObject()
        {
            WalletTransactionList = new SortedList<long, HashSet<string>>();
            WalletMemPoolTransactionList = new HashSet<string>();
        }
    }
}
