using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using SeguraChain_Desktop_Wallet.Enum;
using SeguraChain_Desktop_Wallet.Settings.Enum;
using SeguraChain_Lib.Blockchain.Block.Object.Structure;

namespace SeguraChain_Desktop_Wallet.MainForm.Object
{
    /// <summary>
    /// Transaction history object constantly updated to paint.
    /// </summary>
    public class ClassTransactionHistoryObject
    {
        /// <summary>
        /// Listing progress from sync.
        /// </summary>
        public long LastBlockHeight;
        public long LastTransactionCount;
        public long LastTransactionCountOnRead;
        public Dictionary<string, TransactionHistoryInformationObject> DictionaryTransactionHistoryHashListed;
        public ConcurrentDictionary<string, TransactionHistoryInformationShowedObject> DictionaryTransactionHistoryHashListedShowed;
        public long LastBlockTransactionShowTimestampUpdate;


        /// <summary>
        /// Transaction history draw & pages progress.
        /// </summary>
        public ClassEnumTransactionHistoryColumnType TransactionHistoryColumnOrdering;
        public bool EnableEventDrawPage;
        public bool OnLoad;
        public int TotalTransactionShowed;
        public int CurrentTransactionHistoryPage;        
        public string TransactionInformationSelectedByClick;
        public string TransactionInformationSelectedByPosition;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public ClassTransactionHistoryObject()
        {
            OnLoad = true;
            DictionaryTransactionHistoryHashListed = new Dictionary<string ,TransactionHistoryInformationObject>();
            DictionaryTransactionHistoryHashListedShowed = new ConcurrentDictionary<string, TransactionHistoryInformationShowedObject>();
            CurrentTransactionHistoryPage = 1;
            TransactionHistoryColumnOrdering = ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE;
            TransactionInformationSelectedByClick = string.Empty;
            TransactionInformationSelectedByPosition = string.Empty;
        }

        /// <summary>
        /// Clear the transaction history content.
        /// </summary>
        public void ClearTransactionHistoryContent()
        {
            TransactionHistoryColumnOrdering = ClassEnumTransactionHistoryColumnType.TRANSACTION_HISTORY_COLUMN_TRANSACTION_DATE;
            DictionaryTransactionHistoryHashListed.Clear();

#if NET5_0_OR_GREATER
            DictionaryTransactionHistoryHashListed.TrimExcess();
#endif
            DictionaryTransactionHistoryHashListedShowed.Clear();

            TotalTransactionShowed = 0;
            LastTransactionCount = 0;
            LastTransactionCountOnRead = 0;
            LastBlockHeight = 0;
            CurrentTransactionHistoryPage = 1;
            EnableEventDrawPage = true;
        }
    }

    /// <summary>
    /// Transaction history information object.
    /// </summary>
    public class TransactionHistoryInformationObject
    {
        public bool IsMemPool;
        public DateTime DateSent;
        public string TransactionType;
        public string WalletAddress;
        public string TransactionHash;
        public BigInteger Amount;
        public BigInteger Fee;
    }

    public class TransactionHistoryInformationShowedObject
    {
        public bool IsMemPool;
        public ClassBlockTransaction BlockTransaction;
        public TransactionHistoryInformationObject TransactionHistoryInformationObject;        
    }
}
